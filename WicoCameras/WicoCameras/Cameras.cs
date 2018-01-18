using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region camerasensors 

        string sCameraViewOnly = "[VIEW]"; // do not use cameras with this in their name for scanning.

        readonly Matrix cameraidentityMatrix = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        List<IMyTerminalBlock> cameraForwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> cameraBackwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> cameraDownList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> cameraUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> cameraLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> cameraRightList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> cameraAllList = new List<IMyTerminalBlock>();

        IMyTerminalBlock lastCamera = null;

        private MyDetectedEntityInfo lastDetectedInfo;

        string sCameraSection = "CAMERAS";
        void CamerasInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sCameraSection, "CameraViewOnly", ref sCameraViewOnly, true);

        }

        bool doCameraScan(List<IMyTerminalBlock> cameraList, double scandistance = 100, float pitch = 0, float yaw = 0)
        {
            double foundmax = 0;
            lastCamera = null;
            for (int i = 0; i < cameraList.Count; i++)
            {
                double thismax = ((IMyCameraBlock)cameraList[i]).AvailableScanRange;
                //		Echo(cameraList[i].CustomName + ":maxRange:" + thismax.ToString("N0"));
                // find camera with highest scan range.
                if (thismax > foundmax)
                {
                    foundmax = thismax;
                    lastCamera = cameraList[i];
                }
            }

            IMyCameraBlock camera = lastCamera as IMyCameraBlock;
            if (lastCamera == null)
            {
                return false;
            }

            if (camera.CanScan(scandistance))
            {
                //		Echo("simple Scan with Camera:" + camera.CustomName);

                lastDetectedInfo = camera.Raycast(scandistance, pitch, yaw);
                lastCamera = camera;

                if (!lastDetectedInfo.IsEmpty())
                    addDetectedEntity(lastDetectedInfo);

                return true;
            }
            else
            {
                Echo(camera.CustomName + ":" + camera.AvailableScanRange.ToString("N0"));
            }

            return false;

        }

        bool doCameraScan(List<IMyTerminalBlock> cameraList, Vector3D targetPos)
        {
            Echo("target Scan");
            double foundmax = 0;
            lastCamera = null;
            for (int i = 0; i < cameraList.Count; i++)
            {
                double thismax = ((IMyCameraBlock)cameraList[i]).AvailableScanRange;
                //		Echo(cameraList[i].CustomName + ":maxRange:" + thismax.ToString("N0"));
                // find camera with highest scan range.
                if (thismax > foundmax)
                {
                    foundmax = thismax;
                    lastCamera = cameraList[i];
                }
            }

            IMyCameraBlock camera = lastCamera as IMyCameraBlock;
            if (lastCamera == null)
                return false;

            //	if (camera.CanScan(scandistance))
            {
                Echo("Scanning with Camera:" + camera.CustomName);
                lastDetectedInfo = camera.Raycast(targetPos);
                lastCamera = camera;

                if (!lastDetectedInfo.IsEmpty())
                    addDetectedEntity(lastDetectedInfo);

                return true;
            }
            /*
            else
            {
                Echo(camera.CustomName + ":" + camera.AvailableScanRange.ToString("N0"));
            }
            return false;
                */
        }

        double findMaxCameraRange(List<IMyTerminalBlock> cameraList)
        {
            double maxCameraRangeAvailable = 0;
            for (int i = 0; i < cameraList.Count; i++)
            {
                IMyCameraBlock camera = cameraList[i] as IMyCameraBlock;
                if (maxCameraRangeAvailable < camera.AvailableScanRange)
                    maxCameraRangeAvailable = camera.AvailableScanRange;

            }
            return maxCameraRangeAvailable;
        }
        string camerasensorsInit(IMyTerminalBlock orientationBlock)
        {
            cameraForwardList.Clear();

            cameraBackwardList.Clear();
            cameraDownList.Clear();
            cameraUpList.Clear();
            cameraLeftList.Clear();
            cameraRightList.Clear();
            cameraAllList.Clear();

            if (orientationBlock == null) return "\nCameras:No OrientationBlock";

            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cameraAllList, (x1 => x1.CubeGrid == Me.CubeGrid));
            Matrix fromGridToReference;
            orientationBlock.Orientation.GetMatrix(out fromGridToReference);
            Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

            for (int i = 0; i < cameraAllList.Count; ++i)
            {
                if (cameraAllList[i].CustomName.Contains(sCameraViewOnly))
                    continue; // don't add it to our list.

                IMyCameraBlock camera = cameraAllList[i] as IMyCameraBlock;

                camera.EnableRaycast = true;

                Matrix fromcameraToGrid;
                camera.Orientation.GetMatrix(out fromcameraToGrid);
                Vector3 accelerationDirection = Vector3.Transform(fromcameraToGrid.Forward, fromGridToReference);
                if (accelerationDirection == cameraidentityMatrix.Left)
                {
                    cameraLeftList.Add(cameraAllList[i]);
                }
                else if (accelerationDirection == cameraidentityMatrix.Right)
                {
                    cameraRightList.Add(cameraAllList[i]);
                }
                else if (accelerationDirection == cameraidentityMatrix.Backward)
                {
                    cameraBackwardList.Add(cameraAllList[i]);
                }
                else if (accelerationDirection == cameraidentityMatrix.Forward)
                {
                    cameraForwardList.Add(cameraAllList[i]);
                }
                else if (accelerationDirection == cameraidentityMatrix.Up)
                {
                    cameraUpList.Add(cameraAllList[i]);
                }
                else if (accelerationDirection == cameraidentityMatrix.Down)
                {
                    cameraDownList.Add(cameraAllList[i]);
                }
            }
            string s;
            s = "CS:<";
            s += "F" + cameraForwardList.Count.ToString("00");
            s += "B" + cameraBackwardList.Count.ToString("00");
            s += "D" + cameraDownList.Count.ToString("00");
            s += "U" + cameraUpList.Count.ToString("00");
            s += "L" + cameraLeftList.Count.ToString("00");
            s += "R" + cameraRightList.Count.ToString("00");
            s += ">";
            return s;

        }

        void nameCameras(List<IMyTerminalBlock> cameraList, string sDirection)
        {
            for (int i = 0; i < cameraList.Count; i++)
            {
                cameraList[i].CustomName = "Camera " + (i + 1).ToString() + " " + sDirection;
            }
        }

        #endregion

        #region hovercameras
        List<IMyTerminalBlock> cameraHoverForeDownList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> cameraHoverAftDownList = new List<IMyTerminalBlock>();

        string hovercamerasInit(IMyTerminalBlock orientationBlock)
        {
            string s = "";
            if (cameraAllList.Count < 1)
                s += camerasensorsInit(orientationBlock);
            cameraHoverForeDownList.Clear();
            cameraHoverAftDownList.Clear();

            foreach (var camera in cameraDownList)
            {
                if (camera.CustomName.ToLower().Contains("fore") || camera.CustomData.ToLower().Contains("fore"))
                    cameraHoverForeDownList.Add(camera);
                else if (camera.CustomName.ToLower().Contains("aft") || camera.CustomData.ToLower().Contains("aft"))
                    cameraHoverAftDownList.Add(camera);
            }
            s += "HCS:<";
            s += "F" + cameraHoverForeDownList.Count.ToString("00");
            s += "A" + cameraHoverAftDownList.Count.ToString("00");
            s += ">";
            return s;

        }

        #endregion

        //stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
        /* Random Point on a sphere
         var z = Rand(-radius, radius)
        var long = Rand(0, 2*pi)
        var xyr = sqrt(1-z*z)
        return <x*cos(long), y*sin(long), z>
        */
        public class QuadrantCameraScanner
        {
            bool bDoneScanning = false;
            Program _pg;
            public double SCAN_DISTANCE = 1250; // default scan distance
            double _maxScanDist = 5000; // maximum scan distance.

            float YAWSCANRANGE = 25f; // maximum scan range YAW (width)

            float PITCHSCANRANGE = 25f; // maximum scan range YAW (height)

            double SCAN_SCALE_ON_MISS = 5;// scale distance to go further when nothing is found by scanner.

            float SCAN_CENTER_SCALE_FACTOR = 3; // scale factor for adjusting pitch and yaw based on distance from center scan. Higher numbers mean more scans

            float SCAN_MINIMUMADJUST = 0.5f;

            float PITCH = 0;
            float YAW = 0;
            float NEXTYAW = 0;
            float NEXTPITCH = 0;

            List<IMyTerminalBlock> cameras = new List<IMyTerminalBlock>();

            private int quadrant = 0;

            private int scansPerCall = 1;
            //            public MyDetectedEntityInfo info;

            public MyDetectedEntityInfo lastDetectedInfo;
            public List<MyDetectedEntityInfo> myLDEI = new List<MyDetectedEntityInfo>();
            public IMyTerminalBlock lastCamera = null;

            public QuadrantCameraScanner(Program pg, List<IMyTerminalBlock> blocks, double startScanDist = 1250, float defaultYawRange = 45f, float defaultPitchRange = 45f,
            float defaultScaleOnMiss = 2, float defaultScanCenterScale = 1, float defaultMinAdjust = 0.5f, double maxScanDist = 5000)
            {
                _pg = pg;
                bDoneScanning = false;
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

            public bool DoScans()
            {
                if (cameras.Count < 1) bDoneScanning = true; // we have nothing to scan with...

                if (bDoneScanning) return false;

                bool bSomethingFound = false;
                for (int scan = 0; scan < scansPerCall; scan++)
                {
                    if (doCameraScan(cameras, SCAN_DISTANCE, NEXTPITCH, NEXTYAW))
                    {
//                        lastDetectedInfo = _pg.lastDetectedInfo;

                        if (!lastDetectedInfo.IsEmpty())
                        {
                            bool bValidScan = true;
                            // CHECK FOR US and not count it.
                            if (
                                (lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid)
                                || (lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid)
                                )
                            {
                                if(_pg.IsGridLocal(lastDetectedInfo.EntityId))
                                {
                                    // we scanned ourselves
                                    bValidScan = false;
                                }
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

            bool doCameraScan(List<IMyTerminalBlock> cameraList, double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                double foundmax = 0;
                lastCamera = null;
                for (int i = 0; i < cameraList.Count; i++)
                {
                    double thismax = ((IMyCameraBlock)cameraList[i]).AvailableScanRange;
                    //		Echo(cameraList[i].CustomName + ":maxRange:" + thismax.ToString("N0"));
                    // find camera with highest scan range.
                    if (thismax > foundmax)
                    {
                        foundmax = thismax;
                        lastCamera = cameraList[i];
                    }
                }

                IMyCameraBlock camera = lastCamera as IMyCameraBlock;
                if (lastCamera == null)
                {
                    return false;
                }

                if (camera.CanScan(scandistance))
                {
                    //		Echo("simple Scan with Camera:" + camera.CustomName);

                    lastDetectedInfo = camera.Raycast(scandistance, pitch, yaw);
                    lastCamera = camera;

                    //                    if (!lastDetectedInfo.IsEmpty())
                    //                        addDetectedEntity(lastDetectedInfo);

                    return true;
                }
                else
                {
                    //                    Echo(camera.CustomName + ":" + camera.AvailableScanRange.ToString("N0"));
                }

                return false;
            }

        }



    }
}
