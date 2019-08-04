using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        //stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
        /*
         *REFERENCE ONLY.  This is not what this code does
         * 
         * Random Point on a sphere
         var z = Rand(-radius, radius)
        var long = Rand(0, 2*pi)
        var xyr = sqrt(1-z*z)
        return <x*cos(long), y*sin(long), z>
        */
        public class QuadrantCameraScanner
        {
            bool bDoneScanning = false;
            bool bScanForExit = false; // are we scanning for an exit (ie, most distance available).  Say we're done if scan and hit nothing
            public bool bFoundExit = false;
            public Vector3D vEscapeTarget;

            Program _pg;
            public double SCAN_DISTANCE = 1250; // default scan distance
            double _maxScanDist = 5000; // maximum scan distance.

            float YAWSCANRANGE = 25f; // maximum scan range YAW (width)

            float PITCHSCANRANGE = 25f; // maximum scan range PITCH (height)

            double SCAN_SCALE_ON_MISS = 5;// scale distance to go further when nothing is found by scanner.

            float SCAN_CENTER_SCALE_FACTOR = 3; // scale factor for adjusting pitch and yaw based on distance from center scan. Higher numbers mean more scans

            float SCAN_MINIMUMADJUST = 0.5f;

            public float PITCH = 0;
            public float YAW = 0;
            float NEXTYAW = 0;
            float NEXTPITCH = 0;

            List<IMyTerminalBlock> cameras = new List<IMyTerminalBlock>();

            private int quadrant = 0;

            private int scansPerCall = 1;

            public MyDetectedEntityInfo lastDetectedInfo;
            public List<MyDetectedEntityInfo> myLDEI = new List<MyDetectedEntityInfo>();


            public QuadrantCameraScanner(Program pg, List<IMyTerminalBlock> blocks, double startScanDist = 1250, float defaultYawRange = 45f, float defaultPitchRange = 45f,
            float defaultScaleOnMiss = 2, float defaultScanCenterScale = 1, float defaultMinAdjust = 0.5f, double maxScanDist = 5000, bool bScanExit = false)
            {
                _pg = pg;
                bDoneScanning = false;
                bScanForExit = bScanExit;
                bFoundExit = false;

                cameras.Clear();
                myLDEI.Clear();
                lastDetectedInfo = new MyDetectedEntityInfo();
                foreach (var b in blocks)
                {
                    if (b is IMyCameraBlock)
                    {
                        cameras.Add(b);
                        IMyCameraBlock c = b as IMyCameraBlock;
                        c.EnableRaycast = true;
                        if (YAWSCANRANGE > c.RaycastConeLimit) YAWSCANRANGE = c.RaycastConeLimit;
                        if (PITCHSCANRANGE > c.RaycastConeLimit) PITCHSCANRANGE = c.RaycastConeLimit;
                    }

                }
                //                cameras = blocks;
                if (startScanDist > maxScanDist)
                    maxScanDist = startScanDist; // don't stop with zero scans..

                SCAN_DISTANCE = startScanDist;
                YAWSCANRANGE = defaultYawRange;
                PITCHSCANRANGE = defaultPitchRange;
                SCAN_SCALE_ON_MISS = defaultScaleOnMiss;
                SCAN_CENTER_SCALE_FACTOR = defaultScanCenterScale;
                SCAN_MINIMUMADJUST = defaultMinAdjust;
                _maxScanDist = maxScanDist;

                PITCH = 0;
                YAW = 0;
                NEXTYAW = 0;
                NEXTPITCH = 0;
                quadrant = 0;

                scansPerCall = cameras.Count;
            }

            public bool DoneScanning()
            {
                return bDoneScanning;
            }

            void AddLocalEntity(MyDetectedEntityInfo lastDetectedInfo)
            {
                //                _pg.Echo("ALE");
                bool bFoundNew = true;
                for (int i = 0; i < myLDEI.Count; i++)
                {
                    if (myLDEI[i].EntityId == lastDetectedInfo.EntityId)
                        bFoundNew = false;
                }
                if (bFoundNew)
                {
                    myLDEI.Add(lastDetectedInfo);
                    //                    _pg.Echo("Added");
                }
            }

            /// <summary>
            /// Returns true if more scanning is needed
            /// Continue to call until all scans are completed
            /// </summary>
            /// <returns></returns>
            public bool DoScans()
            {
                if (cameras.Count < 1) bDoneScanning = true; // we have nothing to scan with...

                if (bDoneScanning) return false;

                bool bSomethingFound = false;
                for (int scan = 0; scan < scansPerCall; scan++)
                {
                    if (_pg.wicoCameras.doCameraScan(cameras, SCAN_DISTANCE, NEXTPITCH, NEXTYAW))
                    {
                        lastDetectedInfo = _pg.wicoCameras.lastDetectedInfo;

                        if (!lastDetectedInfo.IsEmpty())
                        {
                            bool bValidScan = true;
                            // CHECK FOR US and not count it.
                            if (
                                (lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid)
                                || (lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid)
                                )
                            {
                                /*
                                if (_pg.Me.IsIsGridLocal(lastDetectedInfo.EntityId))
                                {
                                    // we scanned ourselves
                                    bValidScan = false;
                                }
                                */
                            }
                            if (bValidScan)
                            {
                                //                                _pg.sInitResults += "\nDoScan HIT!";
                                AddLocalEntity(lastDetectedInfo);
                                //                                if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                //                                    _pg.addAsteroid(lastDetectedInfo);
                                bSomethingFound = true;
                                //                                SCAN_DISTANCE = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.Position);
                                // keep searching                                break;
                            }
                        }
                        else if (bScanForExit)
                        {
                            // we did NOT hit anything and we want to know about that.
                            bDoneScanning = true;
                            // should save current pitch,yaw,scandistance as a target
                            // Vector3.TransformNormal(Vector3.CreateFromAzimuthAndElevation(...), Block.WorldMatrix)
                            Vector3D vNormal;
                            Vector3D.CreateFromAzimuthAndElevation(MathHelper.ToRadians(YAW), MathHelper.ToRadians(PITCH), out vNormal);
                            vEscapeTarget = Vector3D.TransformNormal(vNormal, _pg.wicoCameras.lastCamera.WorldMatrix);
                            bFoundExit = true;
                            return false;
                        }
                        quadrant++;

                        if (NEXTPITCH == 0 && NEXTYAW == 0)
                        { // no reason to rotate about 'center', so skip to next scan
                            PITCH = SCAN_MINIMUMADJUST;
                            YAW = SCAN_MINIMUMADJUST;
                            quadrant = 0;
                        }

                        if (quadrant > 3)
                        {
                            quadrant = 0;
                            YAW += Math.Abs(YAW / SCAN_CENTER_SCALE_FACTOR) + SCAN_MINIMUMADJUST;
                            if (Math.Abs(YAW) > YAWSCANRANGE)
                            {
                                // end of line. move to next line.
                                quadrant = 0;
                                YAW = 0;
                                PITCH += Math.Abs(PITCH / SCAN_CENTER_SCALE_FACTOR) + SCAN_MINIMUMADJUST;
                            }
                            if (Math.Abs(PITCH) > PITCHSCANRANGE)
                            {
                                // end of scan box.. restart at 'center'
                                PITCH = 0;
                                YAW = 0;
                                quadrant = 0;
                                //                               if (!bSomethingFound) // nothing found
                                {
                                    // scan further
                                    SCAN_DISTANCE *= SCAN_SCALE_ON_MISS; // scale distance to go further.
                                    if (SCAN_DISTANCE > _maxScanDist)
                                    {
                                        bDoneScanning = true;
                                        return false;
                                    }
                                }
                                //                                bSomethingFound = false;

                            }
                        }
                        switch (quadrant)
                        {
                            case 0:
                                NEXTPITCH = PITCH;
                                NEXTYAW = YAW;
                                break;
                            case 1:
                                NEXTPITCH = -PITCH;
                                NEXTYAW = YAW;
                                break;
                            case 2:
                                NEXTPITCH = PITCH;
                                NEXTYAW = -YAW;
                                break;
                            case 3:
                                NEXTPITCH = -PITCH;
                                NEXTYAW = -YAW;
                                break;
                        }
                    }
                }
                return bSomethingFound;
            }


        }


    }
}
