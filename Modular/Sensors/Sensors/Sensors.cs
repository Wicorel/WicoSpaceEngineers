using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        public class Sensors
        {
//            string sSensorUse = "[WICO]";
//            double dSensorSettleWaitMS = 0.175;
            const string sSensorSection = "SENSORS";

            List<IMySensorBlock> sensorsList = new List<IMySensorBlock>();
            public struct SensorInfo
            {
                public long EntityId;
                public double DistanceFront;
                public double DistanceBack;
                public double DistanceLeft;
                public double DistanceRight;
                public double DistanceUp;
                public double DistanceDown;
                // TODO: Orientation info cache?
            }

            List<SensorInfo> sensorInfos = new List<SensorInfo>();

            Program _program;
            IMyShipController shipController;
            WicoBlockMaster _wicoBlockMaster;
            List<IMyCubeGrid> localGrids = new List<IMyCubeGrid>();

            public Sensors(Program program,WicoBlockMaster wicoBlockMaster)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;
                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                _program.AddPostInitHandler(PostInitHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (!localGrids.Contains(tb.CubeGrid))
                    localGrids.Add(tb.CubeGrid);
                if (tb is IMySensorBlock)
                {
                    // TODO: Only use sSensorUse sensors...
                    sensorsList.Add(tb as IMySensorBlock);
                }
            }
            void LocalGridChangedHandler()
            {
                sensorsList.Clear();
                sensorInfos.Clear();
                shipController = null;
                localGrids.Clear();
            }

            void PostInitHandler()
            {
                shipController = _wicoBlockMaster.GetMainController();
            }

            public string SensorInit(IMyShipController orientationBlock, bool bSleep = false)
            {
                //                sensorsList.Clear();
                sensorInfos.Clear();
                shipController = orientationBlock;

                //                List<IMyTerminalBlock> ltb = GetBlocksContains<IMySensorBlock>(sSensorUse);

                OrientedBoundingBoxFaces orientedBoundingBox = new OrientedBoundingBoxFaces(shipController);
                Vector3D vFTL;
                Vector3D vFBL;
                Vector3D vFTR;
                Vector3D vFBR;

                Vector3D vBTL;
                Vector3D vBBL;
                Vector3D vBTR;
                Vector3D vBBR;

                Vector3D[] points = new Vector3D[4];
                orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, points); // front output order is BL, BR, TL, TR
                vFBL = points[0];
                vFBR = points[1];
                vFTL = points[2];
                vFTR = points[3];

                orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupBack, points); // face 4=back output order is BL, BR, TL, TR
                vBBL = points[0];
                vBBR = points[1];
                vBTL = points[2];
                vBTR = points[3];

                foreach (var sb1 in sensorsList)
                {
                    //                   sensorsList.Add(sb1 as IMySensorBlock);

                    SensorInfo si = new SensorInfo();

                    si.EntityId = sb1.EntityId;

                    Vector3D vPos = sb1.GetPosition();
                    double distanceFront = PlanarDistance(vPos, vFBL, vFBR, vFTR);
                    //                Echo("DistanceFront=" + distanceFront.ToString("0.00"));
                    //                Vector3D vFront = vPos + sb1.WorldMatrix.Forward * distanceFront;
                    //                debugGPSOutput("FRONT", vFront);

                    double distanceBack = PlanarDistance(vPos, vBBL, vBBR, vBTR);
                    //                Echo("DistanceBack=" + distanceBack.ToString("0.00"));
                    //                Vector3D vBack = vPos + sb1.WorldMatrix.Forward * distanceBack; // distance is negative for 'back'
                    //                debugGPSOutput("BACK", vBack);

                    double distanceLeft = PlanarDistance(vPos, vFBL, vFTL, vBBL);
                    double distanceRight = PlanarDistance(vPos, vFBR, vFTR, vBBR);
                    double distanceUp = PlanarDistance(vPos, vFTL, vFTR, vBTL);
                    double distanceDown = PlanarDistance(vPos, vFBL, vFBR, vBBR);

                    si.DistanceFront = distanceFront;
                    si.DistanceBack = distanceBack;
                    si.DistanceLeft = distanceLeft;
                    si.DistanceRight = distanceRight;
                    si.DistanceUp = distanceUp;
                    si.DistanceDown = distanceDown;

                    sensorInfos.Add(si);
                }
                if (bSleep) SensorsSleepAll();
                return "S" + sensorsList.Count.ToString("00");
            }

            public int GetCount()
            {
                return sensorsList.Count;
            }

            public IMySensorBlock GetForwardSensor(int iOffset=0)
            {
                // Todo: chose the sensor closest to 'forward' on the ship
                IMySensorBlock sb = null;
                if (sensorsList.Count > iOffset)
                    sb = sensorsList[iOffset];

                return sb;
            }

            public List<IMySensorBlock> SensorsGetActive(string sKey = null)
            {
                List<IMySensorBlock> activeSensors = new List<IMySensorBlock>();
                for (int i1 = 0; i1 < sensorsList.Count; i1++)
                {
                    IMySensorBlock s = sensorsList[i1] as IMySensorBlock;
                    if (s == null) continue;
                    if (s.IsActive && s.Enabled && !s.LastDetectedEntity.IsEmpty())
                    {
                        //                    Echo("Adding Active:" + s.CustomName + ":" + s.Enabled);
                        activeSensors.Add(sensorsList[i1]);
                    }
                }
                return activeSensors;
            }

            public void SensorsSleepAll()
            {
                for (int i1 = 0; i1 < sensorsList.Count; i1++)
                {
                    IMySensorBlock sb1 = sensorsList[i1] as IMySensorBlock;
                    if (sb1 == null) continue;
                    sb1.LeftExtend = sb1.RightExtend = sb1.TopExtend = sb1.BottomExtend = sb1.FrontExtend = sb1.BackExtend = 1;
                    sb1.Enabled = false;
                }
            }

            // TODO: These don't belong in sensors..

            /// <summary>
            /// Returns the shortest distance from the given point to the given plane
            /// </summary>
            /// <param name="vPos">the given point</param>
            /// <param name="v1">Point 1 to define the plane</param>
            /// <param name="v2">Point 2 to define the plane</param>
            /// <param name="v3">Point 3 to define the plane</param>
            /// <returns>shortest distance in meters from the plane to the point</returns>
            public double PlanarDistance(Vector3D vPos, Vector3D v1, Vector3D v2, Vector3D v3)
            {
                // code derived from formula at: https://mathinsight.org/distance_point_plane_examples

                double distance = 0;

                // now get the perpindicular normal of the plane
                Vector3D vPlanePerp = PlanarNormal(v1, v2, v3);// Vector3D.Cross(vN1, vN2);

                // use v1 as the known point on the plane to get vector
                Vector3D vVectorToPoint = vPos - v1;

                // distance is dot product of the plane's normal and the vector to point
                distance = Vector3D.Dot(vVectorToPoint, vPlanePerp);

                return distance;
            }

            /// <summary>
            /// Returned normal of a plane defined by 3 points
            /// </summary>
            /// <param name="v1">Point 1 on the plane</param>
            /// <param name="v2">Point 2 on the plane</param>
            /// <param name="v3">Point 3 on the plane</param>
            /// <returns>Normalized vector for the a perperindicular of the plane</returns>
            public Vector3D PlanarNormal(Vector3D v1, Vector3D v2, Vector3D v3)
            {
                // code derived from formula at: https://mathinsight.org/distance_point_plane_examples
                // get normalized vector between two points on the plane
                Vector3D vN1 = v1 - v2;
                vN1.Normalize();

                // and two more to define the plane
                Vector3D vN2 = v2 - v3;
                vN2.Normalize();

                // now get the perpindicular normal of the plane
                Vector3D vPlanePerp = Vector3D.Cross(vN1, vN2);
                vPlanePerp.Normalize();
                return vPlanePerp;
            }
            float fMinSensorSetting = 0.1f; //was 1.0f before 1.193.100

            public void SensorSetToShip(IMySensorBlock sb1, float fLeft, float fRight, float fUp, float fDown, float fFront, float fBack)
            {
                // need to use world matrix to get orientation correctly
                //                IMySensorBlock sb1 = tb1 as IMySensorBlock;

//                _program.Echo("SensorSetShip()");

                int iSensor = 0;
                for (; iSensor < sensorInfos.Count; iSensor++)
                {
                    if (sensorInfos[iSensor].EntityId == sb1.EntityId)
                        break;
                }
                if (iSensor < sensorInfos.Count)
                {
                    //                Echo("Using cached location information");
                    // we found cached info
                    float fSet = 0;
                    if (fLeft < 0) fSet = -fLeft;
                    else fSet = (float)Math.Abs(fLeft + Math.Abs(sensorInfos[iSensor].DistanceLeft));
                    sb1.LeftExtend = Math.Max(fSet, fMinSensorSetting);

                    if (fRight < 0) fSet = -fRight;
                    else fSet = (float)Math.Abs(fRight + Math.Abs(sensorInfos[iSensor].DistanceRight));
                    sb1.RightExtend = Math.Max(fSet, fMinSensorSetting);

                    if (fUp < 0) fSet = -fUp;
                    else fSet = (float)Math.Abs(fUp + Math.Abs(sensorInfos[iSensor].DistanceUp));
                    sb1.TopExtend = Math.Max(fSet, fMinSensorSetting);

                    if (fDown < 0) fSet = -fDown;
                    else fSet = (float)Math.Abs(fDown + Math.Abs(sensorInfos[iSensor].DistanceDown));
                    sb1.BottomExtend = Math.Max(fSet, fMinSensorSetting);

                    if (fFront < 0) fSet = -fFront;
                    else fSet = (float)Math.Abs(fFront + Math.Abs(sensorInfos[iSensor].DistanceFront));
                    sb1.FrontExtend = Math.Max(fSet, fMinSensorSetting);

                    if (fBack < 0) fSet = -fBack;
                    else fSet = (float)Math.Abs(fBack + Math.Abs(sensorInfos[iSensor].DistanceBack));
                    sb1.BackExtend = Math.Max(fSet, fMinSensorSetting);
                }
                else
                {
                    OrientedBoundingBoxFaces orientedBoundingBox = new OrientedBoundingBoxFaces(shipController);
                    Vector3D vFTL;
                    Vector3D vFBL;
                    Vector3D vFTR;
                    Vector3D vFBR;

                    Vector3D vBTL;
                    Vector3D vBBL;
                    Vector3D vBTR;
                    Vector3D vBBR;

                    Vector3D[] points = new Vector3D[4];
                    orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, points); // front output order is BL, BR, TL, TR
                    vFBL = points[0];
                    vFBR = points[1];
                    vFTL = points[2];
                    vFTR = points[3];
//                    _program.ErrorLog("vFBL=" + vFBL.ToString());
//                    _program.ErrorLog("vFBR=" + vFBR.ToString());

                    orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupBack, points); // face 4=back output order is BL, BR, TL, TR
                    vBBL = points[0];
                    vBBR = points[1];
                    vBTL = points[2];
                    vBTR = points[3];

                    //                    debugGPSOutput("FBL", vFBL);
                    //                    debugGPSOutput("FBR", vFBR);
                    //                    debugGPSOutput("FTL", vFTL);
                    //                    debugGPSOutput("FTR", vFTR);

                    //                    debugGPSOutput("BBL", vBBL);
                    //                    debugGPSOutput("BBR", vBBR);
                    //                    debugGPSOutput("BTL", vBTL);
                    //                    debugGPSOutput("BTR", vBTR);

                    if (sb1 == null) return;
                    //                    Echo(sb1.CustomName);

                    Vector3D vPos = sb1.GetPosition();

                    double distanceFront = PlanarDistance(vPos, vFBL, vFBR, vFTR);
//                    _program.ErrorLog("DistanceFront=" + distanceFront.ToString("0.00"));
                    Vector3D vFront = vPos + sb1.WorldMatrix.Forward * distanceFront;
                    //                    debugGPSOutput("FRONT", vFront);

                    double distanceBack = PlanarDistance(vPos, vBBL, vBBR, vBTR);
//                    _program.ErrorLog("DistanceBack=" + distanceBack.ToString("0.00"));
                    Vector3D vBack = vPos + sb1.WorldMatrix.Forward * distanceBack; // distance is negative for 'back'
                                                                                    //                    debugGPSOutput("BACK", vBack);

                    double distanceLeft = PlanarDistance(vPos, vFBL, vFTL, vBBL);
                    double distanceRight = PlanarDistance(vPos, vFBR, vFTR, vBBR);
                    double distanceUp = PlanarDistance(vPos, vFTL, vFTR, vBTL);
                    double distanceDown = PlanarDistance(vPos, vFBL, vFBR, vBBR);

                    float fSet = 0;
                    fSet = (float)Math.Abs(fLeft + Math.Abs(distanceLeft));
                    sb1.LeftExtend = Math.Max(fSet, fMinSensorSetting);
                    fSet = (float)Math.Abs(fRight + Math.Abs(distanceRight));
                    sb1.RightExtend = Math.Max(fSet, fMinSensorSetting);
                    fSet = (float)Math.Abs(fUp + Math.Abs(distanceUp));
                    sb1.TopExtend = Math.Max(fSet, fMinSensorSetting);
                    fSet = (float)Math.Abs(fDown + Math.Abs(distanceDown));
                    sb1.BottomExtend = Math.Max(fSet, fMinSensorSetting);
                    fSet = (float)Math.Abs(fFront + Math.Abs(distanceFront));
                    sb1.FrontExtend = Math.Max(fSet, fMinSensorSetting);
                    fSet = (float)Math.Abs(fBack + Math.Abs(distanceBack));
                    sb1.BackExtend = Math.Max(fSet, fMinSensorSetting);
                }
                sb1.Enabled = true;

            }

            public bool SensorIsActive(IMySensorBlock s1, ref bool bAsteroidFound, ref bool bLargeFound, ref bool bSmallFound)
            {
                //           bool bAnyFound = false;
                bAsteroidFound = false;
                bLargeFound = false;
                bSmallFound = false;

                if (s1 != null && s1.IsActive && s1.Enabled && !s1.LastDetectedEntity.IsEmpty())
                {
                    List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                    s1.DetectedEntities(lmyDEI);
                    for (int j1 = 0; j1 < lmyDEI.Count; j1++)
                    {
                        if (lmyDEI[j1].Type == MyDetectedEntityType.Asteroid)
                        {
                            //                        Echo(s1.CustomName + "SIA: Asteroid");
                            bAsteroidFound = true;
                        }
                        else if (lmyDEI[j1].Type == MyDetectedEntityType.LargeGrid)
                        {
                            bLargeFound = true;
                        }
                        else if (lmyDEI[j1].Type == MyDetectedEntityType.SmallGrid)
                        {
                            bSmallFound = true;
                        }
                    }
                }
                //            else Echo(s1.CustomName + " SIA() Inactive");
                return bAsteroidFound || bLargeFound || bSmallFound;// bAnyFound;
            }

        }
    }
}
