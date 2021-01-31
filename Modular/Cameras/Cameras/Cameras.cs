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
        public class Cameras
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

            public IMyTerminalBlock lastCamera = null;

            public MyDetectedEntityInfo lastDetectedInfo;
            IMyShipController ShipControl;
            Matrix fromGridToReference;

            List<long> localGrids = new List<long>();

            Program _program;

//            bool _debug = false;

            public Cameras(Program program)
            {
                _program = program;

                _program.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _program.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

//                _debug = _program._CustomDataIni.Get(_program.OurName, "CameraDebug").ToBoolean(_debug);
//                _program._CustomDataIni.Set(_program.OurName, "CameraDebug", _debug);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (!localGrids.Contains(tb.CubeGrid.EntityId))
                    localGrids.Add(tb.CubeGrid.EntityId);
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
                localGrids.Clear();
            }

            void CamerasInit()
            {
                if (bCamerasInit) return;
                if (ShipControl == null)
                {
                    // first time Init
                    ShipControl = _program.wicoBlockMaster.GetMainController();
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

            public bool CameraForwardScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraForwardList, scandistance, pitch, yaw);
            }
            public bool CameraBackwardScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraBackwardList, scandistance, pitch, yaw);
            }
            public bool CameraRightScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraRightList, scandistance, pitch, yaw);
            }
            public bool CameraLeftScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraLeftList, scandistance, pitch, yaw);
            }
            public bool CameraDownScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraDownList, scandistance, pitch, yaw);
            }
            public bool CameraUpScan(double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                return doCameraScan(cameraUpList, scandistance, pitch, yaw);
            }

            public bool doCameraScan(List<IMyTerminalBlock> cameraList, double scandistance = 100, float pitch = 0, float yaw = 0)
            {
                CamerasInit();
                double foundmax = 0;
                lastCamera = null;

                // TODO: Get min & max pitch, yaw from camera customdata to allow partial obscured cameras.

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

                    MyDetectedEntityInfo detectedInfo= camera.Raycast(scandistance, pitch, yaw);
                    if (localGrids.Contains(detectedInfo.EntityId))
                        detectedInfo = new MyDetectedEntityInfo();
                    lastDetectedInfo = detectedInfo;
                    lastCamera = camera;
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

            public bool CameraBackScan(Vector3D targetPos)
            {
                return doCameraScan(cameraBackwardList, targetPos);
            }
            public bool CameraRightScan(Vector3D targetPos)
            {
                return doCameraScan(cameraRightList, targetPos);
            }
            public bool CameraLeftScan(Vector3D targetPos)
            {
                return doCameraScan(cameraLeftList, targetPos);
            }
            public bool CameraUpScan(Vector3D targetPos)
            {
                return doCameraScan(cameraUpList, targetPos);
            }
            public bool CameraDownScan(Vector3D targetPos)
            {
                return doCameraScan(cameraDownList, targetPos);
            }

            bool doCameraScan(List<IMyTerminalBlock> cameraList, Vector3D targetPos)
            {
                CamerasInit();
//                if(_debug) _program.Echo("target Scan");
                double foundmax = 0;
                lastCamera = null;
                for (int i = 0; i < cameraList.Count; i++)
                {
                    double thismax = ((IMyCameraBlock)cameraList[i]).AvailableScanRange;
//                    if (_debug) _program.Echo(cameraList[i].CustomName + ":maxRange:" + thismax.ToString("N0"));
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
                    if (localGrids.Contains(lastDetectedInfo.EntityId))
                    {
                        lastDetectedInfo = new MyDetectedEntityInfo();
//                        if(_debug) _program.ErrorLog("Detected Self");
                        return true;
                    }
                    lastCamera = camera;

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

            public bool HasForwardCameras()
            {
                CamerasInit();
                return cameraForwardList.Count > 0;
            }
            public bool HasBackCameras()
            {
                CamerasInit();
                return cameraBackwardList.Count > 0;
            }
            public bool HasDownCameras()
            {
                CamerasInit();
                return cameraDownList.Count > 0;
            }
            public bool HasUpCameras()
            {
                CamerasInit();
                return cameraUpList.Count > 0;
            }
            public bool HasLeftCameras()
            {
                CamerasInit();
                return cameraLeftList.Count > 0;
            }
            public bool HasRightCameras()
            {
                CamerasInit();
                return cameraRightList.Count > 0;
            }

            public List<IMyTerminalBlock> GetBackwardCameras()
            {
                CamerasInit();
                return cameraBackwardList;
            }
            public List<IMyTerminalBlock> GetForwardCameras()
            {
                CamerasInit();
                return cameraForwardList;
            }
            public List<IMyTerminalBlock> GetDownwardCameras()
            {
                CamerasInit();
                return cameraDownList;
            }
            public List<IMyTerminalBlock> GetUpCameras()
            {
                CamerasInit();
                return cameraUpList;
            }
            public List<IMyTerminalBlock> GetLeftCameras()
            {
                CamerasInit();
                return cameraLeftList;
            }
            public List<IMyTerminalBlock> GetRightCameras()
            {
                CamerasInit();
                return cameraRightList;
            }

        }
    }
}
