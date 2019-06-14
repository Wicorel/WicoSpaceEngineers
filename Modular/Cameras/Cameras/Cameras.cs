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
        class Cameras
        {
            string sCameraViewOnly = "[VIEW]"; // do not use cameras with this in their name for scanning.

            readonly Matrix cameraidentityMatrix = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            List<IMyTerminalBlock> cameraForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> cameraBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> cameraDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> cameraUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> cameraLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> cameraRightList = new List<IMyTerminalBlock>();
            bool bCamerasInit = false;

            List<IMyTerminalBlock> cameraAllList = new List<IMyTerminalBlock>();

            IMyTerminalBlock lastCamera = null;

            private MyDetectedEntityInfo lastDetectedInfo;
            IMyShipController ShipControl;
            Matrix fromGridToReference;

            Program thisProgram;
            public Cameras(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyCameraBlock)
                {
                    if (tb.CustomName.Contains(sCameraViewOnly))
                        return; // don't add it to our list.

                    cameraAllList.Add(tb);
                }
            }
            void LocalGridChangedHandler()
            {
                cameraAllList.Clear();
                cameraLeftList.Clear();
                cameraRightList.Clear();
                cameraUpList.Clear();
                cameraDownList.Clear();
                cameraUpList.Clear();
                bCamerasInit = false;
            }

            void CamerasInit()
            {
                if (bCamerasInit) return;
                if (ShipControl == null)
                {
                    // first time Init
                    ShipControl = thisProgram.wicoBlockMaster.GetMainController();
                    if (ShipControl == null) return;

                    ShipControl.Orientation.GetMatrix(out fromGridToReference);
                    Matrix.Transpose(ref fromGridToReference, out fromGridToReference);
                }
                bCamerasInit = true;
                foreach (var tb in cameraAllList)
                {
                    IMyCameraBlock camera = tb as IMyCameraBlock;
                    if (camera == null) continue;

                    camera.EnableRaycast = true;

                    Matrix fromcameraToGrid;
                    camera.Orientation.GetMatrix(out fromcameraToGrid);
                    Vector3 ViewnDirection = Vector3.Transform(fromcameraToGrid.Forward, fromGridToReference);
                    if (ViewnDirection == cameraidentityMatrix.Left)
                    {
                        cameraLeftList.Add(tb);
                    }
                    else if (ViewnDirection == cameraidentityMatrix.Right)
                    {
                        cameraRightList.Add(tb);
                    }
                    else if (ViewnDirection == cameraidentityMatrix.Backward)
                    {
                        cameraBackwardList.Add(tb);
                    }
                    else if (ViewnDirection == cameraidentityMatrix.Forward)
                    {
                        cameraForwardList.Add(tb);
                    }
                    else if (ViewnDirection == cameraidentityMatrix.Up)
                    {
                        cameraUpList.Add(tb);
                    }
                    else if (ViewnDirection == cameraidentityMatrix.Down)
                    {
                        cameraDownList.Add(tb);
                    }
                }
            }

            public bool HasForwardCameras()
            {
                CamerasInit();
                if (cameraForwardList.Count > 0) return true;
                return false;
            }
            public bool CameraForwardScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraForwardList, scandistance, pitch, yaw);
            }
            public bool CameraBackwardScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraBackwardList, scandistance, pitch, yaw);
            }

            bool doCameraScan(List<IMyTerminalBlock> cameraList, double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                CamerasInit();
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

                var camera = lastCamera as IMyCameraBlock;
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
                    //               Echo(camera.CustomName + ":" + camera.AvailableScanRange.ToString("N0"));
                }

                return false;

            }

            public bool CameraForwardScan(Vector3D targetPos)
            {
                return doCameraScan(cameraForwardList, targetPos);
            }

            bool doCameraScan(List<IMyTerminalBlock> cameraList, Vector3D targetPos)
            {
                CamerasInit();
                //           Echo("target Scan");
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

                var camera = lastCamera as IMyCameraBlock;
                if (lastCamera == null)
                    return false;

                //	if (camera.CanScan(scandistance))
                {
                    //                Echo("Scanning with Camera:" + camera.CustomName);
                    lastDetectedInfo = camera.Raycast(targetPos);
                    lastCamera = camera;

                    //                    if (!lastDetectedInfo.IsEmpty())
                    //                        addDetectedEntity(lastDetectedInfo);

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

            public List<IMyTerminalBlock> GetBackwardCameras()
            {
                CamerasInit();
                return cameraBackwardList;
            }
            public List<IMyTerminalBlock> GetDownwardCameras()
            {
                CamerasInit();
                return cameraDownList;
            }

        }
    }
}
