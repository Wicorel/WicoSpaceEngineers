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

        #region travelmovement
        /*
         * 120617 Added delays for camera and scan checks
         * 
         */

            /// <summary>
            /// maximum speed of ship. 
            /// </summary>
        double shipSpeedMax = 100;

        bool dTMDebug = false;

        double tmCameraElapsedMs = -1;
        double tmCameraWaitMs = 0.50;

        double tmScanElapsedMs = -1;
        double tmScanWaitMs = 0.125;


        // below are private
        IMyShipController tmShipController = null;
        double tmMaxSpeed = 85;

        List<IMyTerminalBlock> thrustTmBackwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmForwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmRightList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmDownList = new List<IMyTerminalBlock>();

        IMySensorBlock tmSB=null;

        bool btmApproach = false;
        bool btmPrecision = false;
        bool btmClose = false;
        double dtmFar = 100;
        double dtmApproach = 50;
        double dtmPrecision = 15;
        double dtmFarSpeed = 100;
        double dtmApproachSpeed = 100* 0.5;
        double dtmPrecisionSpeed = 100*0.25;
        double dtmCloseSpeed = 5;

        float tmMaxSensorM = 50f;

        /// <summary>
        /// reset so the next call to doTravelMovement will re-initialize.
        /// </summary>
        void ResetTravelMovement()
        {
            // invalidates any previous tm calculations
            tmShipController = null;
            sleepAllSensors(); // set sensors to lower power
            minAngleRad = 0.01f; // reset Gyro aim tolerance to default
        }

        /// <summary>
        /// initialize the travel movement module.
        /// </summary>
        /// <param name="vTargetLocation"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="myShipController"></param>
        /// <param name="iThrustType"></param>
        void InitDoTravelMovement(Vector3D vTargetLocation, double maxSpeed, IMyTerminalBlock myShipController, int iThrustType=thrustAll)
        {
            tmMaxSpeed = maxSpeed;
            tmShipController =  myShipController as IMyShipController;
            Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;
            double distance = vVec.Length();

            thrustersInit(tmShipController, ref thrustTmForwardList, ref  thrustTmBackwardList,
            ref thrustTmDownList, ref thrustTmUpList,
            ref thrustTmLeftList, ref thrustTmRightList,iThrustType);
            sleepAllSensors();
            if (sensorsList.Count > 0)
            {
                tmSB = sensorsList[0];
                tmSB.DetectAsteroids = true;
                tmSB.DetectEnemy = true;
                tmSB.DetectLargeShips = true;
                tmSB.DetectSmallShips = true;
                tmSB.DetectStations = true;
                tmSB.DetectPlayers = false; // run them over!
                tmMaxSensorM = tmSB.GetMaximum<float>("Front");
            }
            else
            {
                tmSB = null;
                tmMaxSensorM = 0;
            }
            btmApproach = false; // we have reached approach range
            btmPrecision = false; // we have reached precision range
            btmClose = false; // we have reached close range

            double optimalV = CalculateOptimalSpeed( thrustTmBackwardList, distance);
            if (optimalV < tmMaxSpeed)
                tmMaxSpeed = optimalV;
//            sInitResults += "\nDistance="+niceDoubleMeters(distance)+" OptimalV=" + optimalV;

            dtmFarSpeed = tmMaxSpeed;
            dtmApproachSpeed = tmMaxSpeed * 0.50;
            dtmPrecisionSpeed = tmMaxSpeed*0.25;
            if (dtmPrecisionSpeed < 5) dtmPrecisionSpeed = 5;

            if(dtmPrecisionSpeed > dtmApproachSpeed)  dtmApproachSpeed = dtmPrecisionSpeed; 
            if (dtmPrecisionSpeed > dtmFarSpeed) dtmFarSpeed = dtmPrecisionSpeed;

//            dtmPrecision =calculateStoppingDistance(thrustTmBackwardList, dtmPrecisionSpeed*1.1, 0);
//            dtmApproach = calculateStoppingDistance(thrustTmBackwardList, dtmApproachSpeed*1.1, 0);

            dtmPrecision =calculateStoppingDistance(thrustTmBackwardList, dtmPrecisionSpeed +(dtmApproachSpeed-dtmPrecisionSpeed)/2, 0);
            dtmApproach = calculateStoppingDistance(thrustTmBackwardList, dtmApproachSpeed +(dtmFarSpeed-dtmApproachSpeed)/2, 0);

            dtmFar = calculateStoppingDistance(thrustTmBackwardList, dtmFarSpeed, 0); // calculate maximum stopping distance at full speed

            sInitResults += "\nFarSpeed=="+niceDoubleMeters(dtmFarSpeed)+" ASpeed=" + niceDoubleMeters(dtmApproachSpeed);

            sInitResults += "\nFar=="+niceDoubleMeters(dtmFar)+" A=" + niceDoubleMeters(dtmApproach) + " P="+niceDoubleMeters(dtmPrecision);

            tmCameraElapsedMs = -1; // no delay for next check  
            tmScanElapsedMs = 0;// do delay until check 

            minAngleRad = 0.01f; // reset Gyro aim tolerance to default
        }

        /// <summary>
        /// Does travel movement with collision detection and avoidance. On arrival, changes state to arrivalState. If collision, changes to colDetectState
        /// </summary>
        /// <param name="vTargetLocation">Location of target</param>
        /// <param name="arrivalDistance">minimum distance for 'arrival'</param>
        /// <param name="arrivalState">state to use when 'arrived'</param>
        /// <param name="colDetectState">state to use when 'collision'</param>
        void doTravelMovement(Vector3D vTargetLocation, float arrivalDistance, int arrivalState, int colDetectState)
        {
            if(dTMDebug) Echo("dTM:" + arrivalState);
            //		Vector3D vTargetLocation = vHome;// gpsCenter.GetPosition();
            //    gpsCenter.CubeGrid.
            if (tmShipController == null)
            {
                InitDoTravelMovement(vTargetLocation, shipSpeedMax, gpsCenter);
            }

            if(tmCameraElapsedMs>=0) tmCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if(tmScanElapsedMs>=0) tmScanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

            Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;
            //	Vector3D vVec = vTargetLocation - gpsCenter.GetPosition();
            //		debugGPSOutput("vTargetLocation", vTargetLocation);
            double distance = vVec.Length();

            if(dTMDebug) Echo("dTM:distance=" + niceDoubleMeters(distance));
            if(dTMDebug) Echo("dTM:velocity=" + velocityShip.ToString("0.00"));
            if(dTMDebug) Echo("dTM:tmMaxSpeed=" + tmMaxSpeed.ToString("0.00"));

            if (distance < arrivalDistance)
            {
                ResetMotion(); // start the stopping
                current_state = arrivalState; // we have arrived
                ResetTravelMovement(); // reset our brain so we re-calculate for the next time we're called
                bWantFast = true; // process this quickly
                return;
            }
            debugGPSOutput("TargetLocation", vTargetLocation);

            List<IMySensorBlock> aSensors = null;

            double stoppingDistance = calculateStoppingDistance(thrustTmBackwardList, velocityShip, 0);
//            Echo("StoppingD=" + niceDoubleMeters(stoppingDistance));

            bool bAimed = false;
            bAimed = GyroMain("forward", vVec, gpsCenter);

            tmShipController.DampenersOverride = true;

            if((distance - stoppingDistance) < arrivalDistance)
            { // we are within stopping distance, so start slowing
                minAngleRad = 0.005f;// aim tighter (next time)

                if (!bAimed) bWantFast = true;
                ResetMotion();
                return;
            }

            if (bAimed)
            {
                bWantMedium = true;
                // we are aimed at location
               if(dTMDebug)  Echo("Aimed");
                gyrosOff();

                if (sensorsList.Count > 0)
                {
                    //                    float fScanDist = Math.Min(1f, (float)stoppingDistance * 1.5f);
                    float fScanDist = Math.Min(50f, (float)stoppingDistance * 1.5f);
                    setSensorShip(tmSB, 0, 0, 0, 0, fScanDist, 0);
                }
                if (tmScanElapsedMs > tmScanWaitMs || tmScanElapsedMs<0)
                {
                    tmScanElapsedMs = 0;
                    aSensors = activeSensors();
                    if (aSensors.Count > 0)
                    {
                        int i = 0;

                        //                    for (int i = 0; i < aSensors.Count; i++)
                        {
                            string s = "";
                            s += "\nSensor TRIGGER!";
                            s += "\nName: " + aSensors[i].LastDetectedEntity.Name;
                            s += "\nType: " + aSensors[i].LastDetectedEntity.Type;
                            s += "\nRelationship: " + aSensors[i].LastDetectedEntity.Relationship;
                            s += "\n";
                            if (dTMDebug)
                            {
                                Echo(s);
                                StatusLog(s, textLongStatus);
                            }
                            // save what we detected
                            lastDetectedInfo = aSensors[i].LastDetectedEntity;
                            // something in way.
                            ResetTravelMovement();
                            current_state = colDetectState; // set the collision detetected state
                            bWantFast = true; // process next state quickly
                            ResetMotion(); // start stopping
                            return;
                        }
                    }
                   else lastDetectedInfo = new MyDetectedEntityInfo(); // since we found nothing, clear it.
                }
                double scanDistance = stoppingDistance*2;
                if (scanDistance < 100)
                    if (distance < 1000)
                        scanDistance = distance;
                    else scanDistance = 1000;
                scanDistance = Math.Min(distance, scanDistance);

                if (dTMDebug)
                {
                    Echo("Scanning distance=" + scanDistance);
                }
                if (
                    (tmCameraElapsedMs > tmCameraWaitMs || tmCameraElapsedMs<0) // it is time to scan..
                    && distance>tmMaxSensorM // if we are in sensor range, we don't need to scan with cameras
                    )
                {
                    tmCameraElapsedMs = 0;

                    if (doCameraScan(cameraForwardList, scanDistance))
                    {
                        // the routine sets lastDetetedInfo itself if scan succeeds
                        if (!lastDetectedInfo.IsEmpty())
                        {
                            if (dTMDebug)
                            {
                                //                            Echo(s);
                                StatusLog("Camera Trigger collision", textLongStatus);
                            }

                            // something in way.
                            ResetTravelMovement(); // reset our brain for next call
                            current_state = colDetectState; // set the detetected state
                            bWantFast = true; // process next state quickly
                            ResetMotion(); // start stopping
                            return;
                        }
                        else
                        {
                            if (dTMDebug)
                            {
                                //                            Echo(s);
                                StatusLog("Camera Scan Clear", textLongStatus);
                            }
                        }
                    }
                    else
                    {
                        if (dTMDebug)
                        {
                            //                            Echo(s);
                            StatusLog("No Scan Available", textLongStatus);
                        }
                    }
                }
                else Echo("Raycast delay");

                if(dTMDebug)
                    Echo("dtmFar=" + niceDoubleMeters(dtmFar));
                if(dTMDebug)
                    Echo("dtmApproach=" + niceDoubleMeters(dtmApproach));
                if(dTMDebug)
                    Echo("dtmPrecision=" + niceDoubleMeters(dtmPrecision));

                if (distance > dtmFar &&!btmApproach)
                {
                    // we are 'far' from target location.  use fastest movement
//                    if(dTMDebug)
                        Echo("dtmFar");
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 100);
                    else if (velocityShip < dtmFarSpeed * .75)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip < dtmFarSpeed * .85)
                        powerUpThrusters(thrustTmForwardList, 15f);
                    else if (velocityShip <= dtmFarSpeed * .98)
                    {
                        powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= dtmFarSpeed * 1.02)
                    {
                        powerDownThrusters(thrustAllList);
                    }
                    else // sweet spot
                    {
                        powerDownThrusters(thrustAllList); // turns ON all thrusters
                        powerDownThrusters(thrustTmBackwardList, thrustAll, true); // turns off the 'backward' thrusters... so we don't slow down
//                        tmShipController.DampenersOverride = false; // this would also work, but then we don't get ship moving towards aim point as we correct
                    }
                }
                else if (distance > dtmApproach && !btmPrecision)
                {
                    // we are on 'approach' to target location.  use a good speed
//                    if(dTMDebug)
                        Echo("Approach");
                    btmApproach = true;
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 100);
                    else if (velocityShip < dtmApproachSpeed * .75)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip < dtmApproachSpeed * .85)
                        powerUpThrusters(thrustTmForwardList, 15f);
                    else if (velocityShip <= dtmApproachSpeed * .98)
                    {
                        powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= dtmApproachSpeed * 1.02)
                    {
                        powerDownThrusters(thrustAllList);
                    }
                    else // sweet spot
                    {
                        powerDownThrusters(thrustAllList); // turns ON all thrusters
                        powerDownThrusters(thrustTmBackwardList, thrustAll, true); // turns off the 'backward' thrusters... so we don't slow down
//                        tmShipController.DampenersOverride = false; // this would also work, but then we don't get ship moving towards aim point as we correct
                    }
                }
                else if (distance > dtmPrecision && !btmClose)
                {
                    // we are getting nearto our target.  use a slower speed
//                    if(dTMDebug)
                        Echo("Precision");
                    if(!btmPrecision) minAngleRad = 0.005f;// aim tighter (next time)
                    btmPrecision = true;

                     if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 55f);
                    else if (velocityShip < dtmPrecisionSpeed * .75)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip < dtmPrecisionSpeed * .85)
                        powerUpThrusters(thrustTmForwardList, 15f);
                    else if (velocityShip <= dtmPrecisionSpeed * .98)
                    {
                        powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= dtmPrecisionSpeed * 1.02)
                    {
                        powerDownThrusters(thrustAllList);
                    }
                    else // sweet spot
                    {
                        powerDownThrusters(thrustAllList); // turns ON all thrusters
                        powerDownThrusters(thrustTmBackwardList, thrustAll, true); // turns off the 'backward' thrusters... so we don't slow down
//                        tmShipController.DampenersOverride = false; // this would also work, but then we don't get ship moving towards aim point as we correct
                    }
                   /*
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 55f);
                    else if (velocityShip < dtmPrecisionSpeed)
                        powerUpThrusters(thrustTmForwardList, 1f);
                    //				else if (velocityShip <= 25)
                    //					powerUpThrusters(thrustForwardList, 1);
                    else
                        powerDownThrusters(thrustAllList);
                        */
                }
                else
                {
                    // we are very close to our target. use a very small speed
//                    if(dTMDebug)
                        Echo("Close");
                     if(!btmClose) minAngleRad = 0.005f;// aim tighter (next time)
                   btmClose = true;
                     if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 55f);
                    else if (velocityShip < dtmCloseSpeed * .75)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip < dtmCloseSpeed * .85)
                        powerUpThrusters(thrustTmForwardList, 15f);
                    else if (velocityShip <= dtmCloseSpeed * .98)
                    {
                        powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= dtmCloseSpeed * 1.02)
                    {
                        powerDownThrusters(thrustAllList);
                    }
                    else // sweet spot
                    {
                        powerDownThrusters(thrustAllList); // turns ON all thrusters
                        powerDownThrusters(thrustTmBackwardList, thrustAll, true); // turns off the 'backward' thrusters... so we don't slow down
//                        tmShipController.DampenersOverride = false; // this would also work, but then we don't get ship moving towards aim point as we correct
                    }
/*
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip < 5)
                        powerUpThrusters(thrustTmForwardList, 5f);
                    //				else if (velocityShip <= 15)
                    //					powerUpThrusters(thrustForwardList, 1);
                    else
                        powerDownThrusters(thrustAllList);
                        */
                }
            }
            else
            {
                if(dTMDebug) Echo("Aiming");
                bWantFast = true;
                tmShipController.DampenersOverride = true;
                if (velocityShip < 5)
                {
                    // we are probably doing precision maneuvers.  Turn on all thrusters to avoid floating past target
                    powerDownThrusters(thrustAllList);
                }
                else
                {
                    powerDownThrusters(thrustTmBackwardList, thrustAll, true); // coast
                }
                //		sleepAllSensors();
            }

        }
        #endregion

        /// <summary>
        /// returns the optimal max speed based on available braking thrust and ship mass
        /// </summary>
        /// <param name="thrustUpList">Thrusters to use</param>
        /// <param name="distance">current distance to target location</param>
        /// <returns>optimal max speed (may be game max speed)</returns>
        double CalculateOptimalSpeed(List<IMyTerminalBlock> thrustUpList, double distance)
        {
            // 
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double maxThrust = calculateMaxThrust(thrustUpList);
            double maxDeltaV = maxThrust / myMass.PhysicalMass;
            // Magic..
            double optimalV, secondstozero, stoppingM;
            optimalV = ((distance * .75) / 2) / (maxDeltaV); // determined by experimentation and black magic

            do
            {
                secondstozero = optimalV / maxDeltaV;
                stoppingM = optimalV / 2 * secondstozero;
                if (stoppingM > distance)
                {
                    optimalV *=0.85;
                }
            }
            while (stoppingM > distance);
            return optimalV;
        }

        /// <summary>
        /// Stopping distance based on thrust available, mass, current velocity and an optional gravity factor
        /// </summary>
        /// <param name="thrustUpList">list of thrusters to use</param>
        /// <param name="currentV">velocity to calculage</param>
        /// <param name="dGrav">optional gravity factor</param>
        /// <returns>stopping distance in meters</returns>
        double calculateStoppingDistance(List<IMyTerminalBlock> thrustUpList, double currentV, double dGrav=0)
        {
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double hoverthrust = 0;
            hoverthrust = myMass.PhysicalMass * dGrav * 9.810;
            double maxThrust = calculateMaxThrust(thrustUpList);
            double maxDeltaV = (maxThrust - hoverthrust) / myMass.PhysicalMass;
            double secondstozero = currentV / maxDeltaV;
//            Echo("secondstozero=" + secondstozero.ToString("0.00"));
            // velocity will drop as we brake. at half way we should be at half speed
            double stoppingM = currentV / 2 * secondstozero; 
//            Echo("stoppingM=" + stoppingM.ToString("0.00"));
            return stoppingM;
        }



        // Collision Avoidance routines:

            /// <summary>
            /// the location we want to go to to 'avoid' the collision
            /// </summary>
        Vector3D vAvoid; // 

        /// <summary>
        /// calculate vAvoid based on the current detected entity and the desired target location
        /// </summary>
        /// <param name="vTargetLocation"></param>
        void calcCollisionAvoid(Vector3D vTargetLocation)
        {
            if(tmCameraElapsedMs>=0) tmCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if(tmScanElapsedMs>=0) tmScanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

//            Echo("Collsion Detected");
            Vector3D vHit;
            if (lastDetectedInfo.HitPosition.HasValue)
            {
//                StatusLog("Has hitposition", gpsPanel);
                vHit = (Vector3D)lastDetectedInfo.HitPosition;
            }
            else
            {
//                StatusLog("NO hitposition", gpsPanel);
                vHit = gpsCenter.GetPosition();
            }

            Vector3D vCenter = lastDetectedInfo.Position;
            //	Vector3D vTargetLocation = vHome;
            //vTargetLocation;
//            debugGPSOutput("TargetLocation", vTargetLocation);
//            debugGPSOutput("HitPosition", vHit);
//            debugGPSOutput("CCenter", vCenter);

            // need to check if vector is straight through the object.  then choose 90% vector or something
            Vector3D vVec = (vCenter - vHit);
            vVec.Normalize();
 //           double ang;

            Vector3D vMinBound = lastDetectedInfo.BoundingBox.Min;
//            debugGPSOutput("vMinBound", vMinBound);
            Vector3D vMaxBound = lastDetectedInfo.BoundingBox.Max;
//            debugGPSOutput("vMaxBound", vMaxBound);

            double radius = (vCenter - vMinBound).Length();
//            Echo("Radius=" + radius.ToString("0.00"));

            double modRadius = radius + shipDim.WidthInMeters() * 5;

            // the OLD way.

            //            vAvoid = vCenter - vVec * (radius + shipDim.WidthInMeters() * 5);
            //	 Vector3D gpsCenter.GetPosition() - vAvoid;

            Vector3D cross;

             cross= Vector3D.Cross(vTargetLocation, vHit);
             cross.Normalize();
             cross = vHit + cross * modRadius;
//            debugGPSOutput("crosshit", cross);
            
            vAvoid = cross;

//            debugGPSOutput("vAvoid", vAvoid);
        }


        // pathfinding routines.  Try to escape from inside an asteroid.

        bool bScanLeft = true;
        bool bScanRight = true;
        bool bScanUp = true;
        bool bScanDown = true;
        bool bScanBackward = true;
        bool bScanForward = true;

        MyDetectedEntityInfo leftDetectedInfo = new MyDetectedEntityInfo();
        MyDetectedEntityInfo rightDetectedInfo = new MyDetectedEntityInfo();
        MyDetectedEntityInfo upDetectedInfo = new MyDetectedEntityInfo();
        MyDetectedEntityInfo downDetectedInfo = new MyDetectedEntityInfo();
        MyDetectedEntityInfo backwardDetectedInfo = new MyDetectedEntityInfo();
        MyDetectedEntityInfo forwardDetectedInfo = new MyDetectedEntityInfo();

//        bool bEscapeGrid = false;

        /// <summary>
        /// Initialize the escape scanning (mini-pathfinding)
        /// Call once to setup
        /// </summary>
        void initEscapeScan()
        {
            if(tmCameraElapsedMs>=0) tmCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if(tmScanElapsedMs>=0) tmScanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

            bScanLeft = true;
            bScanRight = true;
            bScanUp = true;
            bScanDown = true;
            bScanBackward = false;// don't rescan where we just came from..
                                  //	bScanBackward = true;
            bScanForward = true;
            leftDetectedInfo = new MyDetectedEntityInfo();
            rightDetectedInfo = new MyDetectedEntityInfo();
            upDetectedInfo = new MyDetectedEntityInfo();
            downDetectedInfo = new MyDetectedEntityInfo();
            backwardDetectedInfo = new MyDetectedEntityInfo();
            forwardDetectedInfo = new MyDetectedEntityInfo();

//            bEscapeGrid = false;
            if ( lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid
                || lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid
                )
            {
 //               bEscapeGrid = true;
            }

            // don't assume all drones have all cameras..
            if (cameraLeftList.Count < 1) bScanLeft = false;
            if (cameraRightList.Count < 1) bScanRight = false;
            if (cameraUpList.Count < 1) bScanUp = false;
            if (cameraDownList.Count < 1) bScanDown = false;
            if (cameraForwardList.Count < 1) bScanForward = false;
            if (cameraBackwardList.Count < 1) bScanBackward = false;

        }
        /// <summary>
        /// Perform the pathfinding. Call until it returns true
        /// </summary>
        /// <returns>true if vAvoid now contains the location to go to to (try to) escapet</returns>
        bool scanEscape()
        {
            if(tmCameraElapsedMs>=0) tmCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if(tmScanElapsedMs>=0) tmScanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

            MatrixD worldtb = gpsCenter.WorldMatrix;
            Vector3D vVec = worldtb.Forward;
            Echo("ScanEscape()");
            if (bScanLeft)
            {
                if (doCameraScan(cameraLeftList, 200))
                {
                    bScanLeft = false;
                    leftDetectedInfo = lastDetectedInfo;
                    if (lastDetectedInfo.IsEmpty())
                    {
                        vVec = worldtb.Left;
                        vVec.Normalize();
                        vAvoid = gpsCenter.GetPosition() + vVec * 200;
                        return true;
                    }
                }
            }
            if (bScanRight)
            {
                if (doCameraScan(cameraRightList, 200))
                {
                    bScanRight = false;
                    rightDetectedInfo = lastDetectedInfo;
                    if (lastDetectedInfo.IsEmpty())
                    {
                        vVec = worldtb.Right;
                        vVec.Normalize();
                        vAvoid = gpsCenter.GetPosition() + vVec * 200;
                        return true;
                    }
                }
            }
            if (bScanUp)
            {
                if (doCameraScan(cameraUpList, 200))
                {
                    upDetectedInfo = lastDetectedInfo;
                    bScanUp = false;
                    if (lastDetectedInfo.IsEmpty())
                    {
                        vVec = worldtb.Up;
                        vVec.Normalize();
                        vAvoid = gpsCenter.GetPosition() + vVec * 200;
                        return true;
                    }
                }
            }
            if (bScanDown)
            {
                if (doCameraScan(cameraDownList, 200))
                {
                    downDetectedInfo = lastDetectedInfo;
                    bScanDown = false;
                    if (lastDetectedInfo.IsEmpty())
                    {
                        vVec = worldtb.Down;
                        vVec.Normalize();
                        vAvoid = gpsCenter.GetPosition() + vVec * 200;
                        return true;
                    }
                }
            }
            if (bScanBackward)
            {
                if (doCameraScan(cameraBackwardList, 200))
                {
                    backwardDetectedInfo = lastDetectedInfo;
                    bScanBackward = false;
                    if (lastDetectedInfo.IsEmpty())
                    {
                        vVec = worldtb.Backward;
                        vVec.Normalize();
                        vAvoid = gpsCenter.GetPosition() + vVec * 200;
                        return true;
                    }
                }
            }
            if (bScanForward)
            {
                if (doCameraScan(cameraForwardList, 200))
                {
                    bScanForward = false;
                    if (lastDetectedInfo.IsEmpty())
                    {
                        vVec = worldtb.Forward;
                        vVec.Normalize();
                        vAvoid = gpsCenter.GetPosition() + vVec * 200;
                        return true;
                    }
                }
            }

            if (bScanForward || bScanBackward || bScanUp || bScanDown || bScanLeft || bScanRight)
            {
                Echo("More scans");
                return false; // still more scans to go
            }

            // nothing was 'clear'.  find longest vector and try to go that direction
            Echo("Scans done. Choose longest");
            MyDetectedEntityInfo furthest = backwardDetectedInfo;
            Vector3D currentpos = gpsCenter.GetPosition();
            vVec = worldtb.Backward;
            if (furthest.HitPosition == null || leftDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)leftDetectedInfo.HitPosition))
            {
                vVec = worldtb.Left;
                furthest = leftDetectedInfo;
            }
            if (furthest.HitPosition == null || rightDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)rightDetectedInfo.HitPosition))
            {
                vVec = worldtb.Right;
                furthest = rightDetectedInfo;
            }
            if (furthest.HitPosition == null || upDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)upDetectedInfo.HitPosition))
            {
                vVec = worldtb.Up;
                furthest = upDetectedInfo;
            }
            if (furthest.HitPosition == null || downDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)downDetectedInfo.HitPosition))
            {
                vVec = worldtb.Down;
                furthest = downDetectedInfo;
            }
            if (furthest.HitPosition == null || forwardDetectedInfo.HitPosition != null && Vector3D.DistanceSquared(currentpos, (Vector3D)furthest.HitPosition) < Vector3D.DistanceSquared(currentpos, (Vector3D)forwardDetectedInfo.HitPosition))
            {
                vVec = worldtb.Forward;
                furthest = forwardDetectedInfo;
            }
            if (furthest.HitPosition == null) return false;

            double distance = Vector3D.Distance(currentpos, (Vector3D)furthest.HitPosition);
            Echo("Distance=" + niceDoubleMeters(distance));
            vVec.Normalize();
            vAvoid = gpsCenter.GetPosition() + vVec * distance / 2;
/*
            if (distance<15)
            {
                if (doCameraScan(cameraForwardList, 20,35,35))
                {
                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                    if (distance < vVec.Length())
                    {
                    Echo("35,35");
                        distance = vVec.Length();
                        vVec.Normalize();
                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                    }

                }
                if (doCameraScan(cameraForwardList, 20,-35,-35))
                {
                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                    if (distance < vVec.Length())
                    {
                    Echo("-35,-35");
                        distance = vVec.Length();
                        vVec.Normalize();
                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                    }

                }
                if (doCameraScan(cameraForwardList, 20,35,-35))
                {
                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                    if (distance < vVec.Length())
                    {
                    Echo("35,-35");
                        distance = vVec.Length();
                        vVec.Normalize();
                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                    }

                }
                if (doCameraScan(cameraForwardList, 20,-35,35))
                {
                    vVec = lastCamera.GetPosition() - (Vector3D)lastDetectedInfo.HitPosition;
                    if (distance < vVec.Length())
                    {
                    Echo("-35,35");
                        distance = vVec.Length();
                        vVec.Normalize();
                        vAvoid = lastCamera.GetPosition() + vVec * 5;
                    }
                }

            }
*/
//            if (distance > 15)
            if (distance > 4)
            {
                return true;
            }

            return false;
        }

    }
}