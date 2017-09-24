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
        #region quadrantcamerascan

        public class CameraQuadrantScan
        {
            private List<IMyTerminalBlock> blocks;

            public double SCAN_DISTANCE = 100; // default scan distance

            public float YAWSCANRANGE = 25f; // maximum scan range YAW (width)

            public float PITCHSCANRANGE = 25f; // maximum scan range YAW (height)

            public double SCAN_SCALE_ON_MISS = 5;// scale distance to go further when nothing is found by scanner.

            public float SCAN_CENTER_SCALE_FACTOR = 3; // scale factor for adjusting pitch and yaw based on distance from center scan. Higher numbers mean more scans

            public float SCAN_MINIMUMADJUST = 0.5f;

            public MyDetectedEntityInfo info;
            public IMyCameraBlock camera;


            private float PITCH = 0;
            private float YAW = 0;
            private float NEXTYAW = 0;
            private float NEXTPITCH = 0;
            private int quadrant = 0;

            public CameraQuadrantScan(List<IMyTerminalBlock> ltb)
            {
                blocks = ltb;
                foreach (var tb in blocks)
                {
                    IMyCameraBlock lcamera = tb as IMyCameraBlock;
                    if (lcamera == null) continue;
                    lcamera.EnableRaycast = true;
                    if (YAWSCANRANGE > lcamera.RaycastConeLimit) YAWSCANRANGE = lcamera.RaycastConeLimit;
                    if (PITCHSCANRANGE > lcamera.RaycastConeLimit) PITCHSCANRANGE = lcamera.RaycastConeLimit;
                }
            }

            public bool TryScan()
            {
                bool bFoundSomething = false;
                foreach (var tb in blocks)
                {
                    camera = tb as IMyCameraBlock;
                    if (camera == null) continue;
                    if (camera.CanScan(SCAN_DISTANCE))
                    {
                        info = camera.Raycast(SCAN_DISTANCE, NEXTPITCH, NEXTYAW);
                        quadrant++;

                        if (!info.IsEmpty())
                        {
                            bFoundSomething = true;
                            SCAN_DISTANCE = Vector3D.Distance(camera.GetPosition(), info.Position);
                        }

                        if (NEXTPITCH == 0 && NEXTYAW == 0)
                        { // no reason to rotate about 'center', so skipp to next scan
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
                                if (!bFoundSomething) // nothing found
                                {
                                    // scan further
                                    SCAN_DISTANCE *= SCAN_SCALE_ON_MISS; // scale distance to go further.
                                }
                                bFoundSomething = false;

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

                return bFoundSomething;
            }

        }

        #endregion

    }
}