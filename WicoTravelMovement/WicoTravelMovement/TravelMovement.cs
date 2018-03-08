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



        double tmCameraElapsedMs = -1;
        double tmCameraWaitMs = 0.50;

        double tmScanElapsedMs = -1;

        // below are private
        IMyShipController tmShipController = null;
        double tmMaxSpeed = 85; // calculated max speed.

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

        // propulsion mode
        bool btmRotor = false;
        bool btmSled = false;
        // else it's gyros and thrusters

        /// <summary>
        /// reset so the next call to doTravelMovement will re-initialize.
        /// </summary>
        void ResetTravelMovement()
        {
            // invalidates any previous tm calculations
            tmShipController = null;
            sleepAllSensors(); // set sensors to lower power
            minAngleRad = 0.01f; // reset Gyro aim tolerance to default
            tmScanElapsedMs = 0;
            tmCameraElapsedMs = -1;
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
            if(tmMaxSpeed>fMaxWorldMps)
                tmMaxSpeed=fMaxWorldMps;

            if ((craft_operation & CRAFT_MODE_SLED) > 0)
            {
                btmSled = true;
                //                if (shipSpeedMax > 45) shipSpeedMax = 45;
                PrepareSledTravel();
            }
            else btmSled = false;

            if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
            {
                btmRotor = true;
//                if (shipSpeedMax > 15) shipSpeedMax = 15;
            }
            else btmRotor = false;


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
                if (btmRotor || btmSled) tmSB.DetectAsteroids = false;
                else
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

            double optimalV = tmMaxSpeed;
            if(!btmSled && !btmRotor) optimalV=CalculateOptimalSpeed( thrustTmBackwardList, distance);
            if (optimalV < tmMaxSpeed)
                tmMaxSpeed = optimalV;
            sInitResults += "\nDistance="+niceDoubleMeters(distance)+" OptimalV=" + optimalV;

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

//  sInitResults += "\nFarSpeed=="+niceDoubleMeters(dtmFarSpeed)+" ASpeed=" + niceDoubleMeters(dtmApproachSpeed);

//            sInitResults += "\nFar=="+niceDoubleMeters(dtmFar)+" A=" + niceDoubleMeters(dtmApproach) + " P="+niceDoubleMeters(dtmPrecision);

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
        /// <param name="bAsteroidTarget">if True, target location is in/near an asteroid.  don't collision detect with it</param>
        void doTravelMovement(Vector3D vTargetLocation, float arrivalDistance, int arrivalState, int colDetectState, bool bAsteroidTarget=false)
        {
            if(dTMDebug) Echo("dTM:" + current_state + "->" + arrivalState +"-C>"+ colDetectState);
            //		Vector3D vTargetLocation = vHome;// shipOrientationBlock.GetPosition();
            //    shipOrientationBlock.CubeGrid.
            if (tmShipController == null)
            {
                InitDoTravelMovement(vTargetLocation, shipSpeedMax, shipOrientationBlock);
            }

            if(tmCameraElapsedMs>=0) tmCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if(tmScanElapsedMs>=0) tmScanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;


            Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;

            double distance = vVec.Length();

            if (dTMDebug)
            {
                Echo("dTM:distance=" + niceDoubleMeters(distance));
                Echo("dTM:velocity=" + velocityShip.ToString("0.00"));
                Echo("dTM:tmMaxSpeed=" + tmMaxSpeed.ToString("0.00"));
            }
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
            Echo("StoppingD=" + niceDoubleMeters(stoppingDistance));

            if (sensorsList.Count > 0)
            {
                //                    float fScanDist = Math.Min(1f, (float)stoppingDistance * 1.5f);
                float fScanDist = Math.Min(50f, (float)stoppingDistance * 1.5f);
                setSensorShip(tmSB, 0, 0, 0, 0, fScanDist, 0);
            }
            else Echo("No Sensors for Travel movement");
            bool bAimed = false;

            Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
            if (btmSled || btmRotor)
            {

                double yawangle = -999;
                yawangle = CalculateYaw(vTargetLocation, shipOrientationBlock);
                Echo("yawangle=" + yawangle.ToString());
                if (btmSled)
                {
                    Echo("Sled");
                    DoRotate(yawangle, "Yaw");
                }
                else if (btmRotor)
                {
                    Echo("Rotor");
                    DoRotorRotate(yawangle);
                }
                bAimed = Math.Abs(yawangle) < .05;
            }
            else
            {
                if (grav.Length() > 0)
                { // in gravity. try to stay aligned to gravity, but change yaw to aim at location.
                    bool bGravAligned = GyroMain("", grav, shipOrientationBlock);
//                    if (bGravAligned)
                    {
                        double yawangle = CalculateYaw(vTargetLocation, shipOrientationBlock);
                        DoRotate(yawangle, "Yaw");
                        bAimed = Math.Abs(yawangle) < .05;
                    }
                }
                else
                {
                    bAimed = GyroMain("forward", vVec, shipOrientationBlock);
                }
            }

            tmShipController.DampenersOverride = true;

            if((distance - stoppingDistance) < arrivalDistance)
            { // we are within stopping distance, so start slowing
                minAngleRad = 0.005f;// aim tighter (next time)
    Echo("Waiting for stop");
                if (!bAimed) bWantFast = true;
                ResetMotion();
                return;
            }
            if (bAimed)
            {
                bWantMedium = true;
                // we are aimed at location
                Echo("Aimed");
                gyrosOff();

                if (
                    dTMUseSensorCollision
                    && (tmScanElapsedMs > dSensorSettleWaitMS  || tmScanElapsedMs < 0 )
                    )
                {
                    tmScanElapsedMs = 0;
                    aSensors = activeSensors();
                    if (aSensors.Count > 0)
                    {

                        var entities = new List<MyDetectedEntityInfo>();
                        string s = "";
                        for (int i1 = 0; i1 < aSensors.Count; i1++) // we only use one sensor
                        {
                            aSensors[i1].DetectedEntities(entities);
                            int j1 = 0;
                            bool bValidCollision = false;
                            if (entities.Count > 0) bValidCollision = true;

                            for (; j1 < entities.Count; j1++)
                            {
                                
                                s = "\nSensor TRIGGER!";
                                s += "\nName: " + entities[j1].Name;
                                s += "\nType: " + entities[j1].Type;
                                s += "\nRelationship: " + entities[j1].Relationship;
                                s += "\n";
                                if (dTMDebug)
                                {
                                    Echo(s);
                                    StatusLog(s, textLongStatus);
                                }
                                if (entities[j1].Type == MyDetectedEntityType.Planet)
                                {
                                    bValidCollision = false;
                                }
                                if(entities[j1].Type==MyDetectedEntityType.LargeGrid
                                    || entities[j1].Type==MyDetectedEntityType.SmallGrid
                                    )
                                {
                                    if (entities[j1].BoundingBox.Contains(vTargetLocation) != ContainmentType.Disjoint)
                                    {
                                        if (dTMDebug)
                                            Echo("Ignoring collision because we want to be INSIDE");
                                        // if the target is inside the BB of the target, ignore the collision
                                        bValidCollision = false;
                                    }
                                }
                                if (bValidCollision) break;

                            }

                            if (bValidCollision)
                            {
                                // something in way.
                                // save what we detected
                                lastDetectedInfo = entities[j1];
                                ResetTravelMovement();
                                current_state = colDetectState; // set the collision detetected state
                                bWantFast = true; // process next state quickly
                                ResetMotion(); // start stopping
                                return;
                            }
                        }
                    }
                    else lastDetectedInfo = new MyDetectedEntityInfo(); // since we found nothing, clear it.
                }
                double scanDistance = stoppingDistance * 2;
 //               double scanDistance = stoppingDistance * 1.05;
//                if (bAsteroidTarget) scanDistance *= 2;
                //                if (btmRotor || btmSled)
                {
                    if (scanDistance < 100)
                        if (distance < 1000)
                            scanDistance = distance;
                        else scanDistance = 1000;
                    scanDistance = Math.Min(distance, scanDistance);
                }

                //               if (dTMDebug)
                if(dTMUseCameraCollision)
                {
                    Echo("Scanning distance=" + scanDistance);
                }
                if (
                    dTMUseCameraCollision
                    && (tmCameraElapsedMs > tmCameraWaitMs || tmCameraElapsedMs < 0) // it is time to scan..
                    && distance > tmMaxSensorM // if we are in sensor range, we don't need to scan with cameras
//                    && !bAsteroidTarget
                    )
                {

                    if (doCameraScan(cameraForwardList, scanDistance))
                    {
                        tmCameraElapsedMs = 0;
                        // the routine sets lastDetetedInfo itself if scan succeeds
                        if (!lastDetectedInfo.IsEmpty())
                        {
                            bool bValidCollision = true;
                            if (bAsteroidTarget)
                            {
                                if(lastDetectedInfo.Type==MyDetectedEntityType.Asteroid)
                                {
                                    if(lastDetectedInfo.BoundingBox.Contains(vTargetLocation)!=ContainmentType.Disjoint)
                                    { // if the target is inside the BB of the target, ignore the collision
                                        bValidCollision = false;
                                        // check to see if we are close enough to surface of asteroid
                                        double astDistance=((Vector3D)lastDetectedInfo.HitPosition-shipOrientationBlock.GetPosition()).Length();
                                        if((astDistance-stoppingDistance)<arrivalDistance)
                                        {
                                            ResetMotion();
                                            current_state = arrivalState;
                                            ResetTravelMovement();
                                            // don't need 'fast'...
                                            return;
                                        }
                                    }
                                }
                                else if (lastDetectedInfo.Type == MyDetectedEntityType.Planet)
                                {
                                    // ignore
                                    bValidCollision = false;
                                }

                                else
                                {
                                }
                            }
                            if (dTMDebug)
                            {
                                //                            Echo(s);
                                Echo("raycast hit:" + lastDetectedInfo.Type.ToString());
                                StatusLog("Camera Trigger collision", textLongStatus);
                            }
                            if (bValidCollision)
                            {
//                                sInitResults += "Camera collision: " + scanDistance + "\n" + lastDetectedInfo.Name + ":" + lastDetectedInfo.Type + "\n";
                                // something in way.
                                ResetTravelMovement(); // reset our brain for next call
                                current_state = colDetectState; // set the detetected state
                                bWantFast = true; // process next state quickly
                                ResetMotion(); // start stopping
                                return;
                            }
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

                    TmDoForward(dtmFarSpeed, 100f);
                }
                else if (distance > dtmApproach && !btmPrecision)
                {
                    // we are on 'approach' to target location.  use a good speed
//                    if(dTMDebug)
                        Echo("Approach");
                    btmApproach = true;
                    TmDoForward(dtmApproachSpeed, 100f);
                }
                else if (distance > dtmPrecision && !btmClose)
                {
                    // we are getting nearto our target.  use a slower speed
//                    if(dTMDebug)
                        Echo("Precision");
                    if(!btmPrecision) minAngleRad = 0.005f;// aim tighter (next time)
                    btmPrecision = true;
                    TmDoForward(dtmPrecisionSpeed, 55f);
                }
                else
                {
                    // we are very close to our target. use a very small speed
//                    if(dTMDebug)
                        Echo("Close");
                     if(!btmClose) minAngleRad = 0.005f;// aim tighter (next time)
                   btmClose = true;
                    TmDoForward(dtmCloseSpeed, 55f);
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
        /// <param name="thrustList">Thrusters to use</param>
        /// <param name="distance">current distance to target location</param>
        /// <returns>optimal max speed (may be game max speed)</returns>
        double CalculateOptimalSpeed(List<IMyTerminalBlock> thrustList, double distance)
        {
            //
            Echo("#thrusters=" + thrustList.Count.ToString());
            if (thrustList.Count < 1) return fMaxWorldMps;

            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double maxThrust = calculateMaxThrust(thrustList);
            double maxDeltaV = maxThrust / myMass.PhysicalMass;
            // Magic..
            double optimalV, secondstozero, stoppingM;
            optimalV = ((distance * .75) / 2) / (maxDeltaV); // determined by experimentation and black magic
            Echo("COS");
            do
            {
                Echo("COS:DO");
                secondstozero = optimalV / maxDeltaV;
                stoppingM = optimalV / 2 * secondstozero;
                if (stoppingM > distance)
                {
                    optimalV *=0.85;
                }
                Echo("stoppingM=" + stoppingM.ToString("F1") + " distance=" + distance.ToString("N1"));
            }
            while (stoppingM > distance);
            Echo("COS:X");
            return optimalV;
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
                vHit = shipOrientationBlock.GetPosition();
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
            //	 Vector3D shipOrientationBlock.GetPosition() - vAvoid;

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

            MatrixD worldtb = shipOrientationBlock.WorldMatrix;
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
                        vAvoid = shipOrientationBlock.GetPosition() + vVec * 200;
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
                        vAvoid = shipOrientationBlock.GetPosition() + vVec * 200;
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
                        vAvoid = shipOrientationBlock.GetPosition() + vVec * 200;
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
                        vAvoid = shipOrientationBlock.GetPosition() + vVec * 200;
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
                        vAvoid = shipOrientationBlock.GetPosition() + vVec * 200;
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
                        vAvoid = shipOrientationBlock.GetPosition() + vVec * 200;
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
            Vector3D currentpos = shipOrientationBlock.GetPosition();
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
            vAvoid = shipOrientationBlock.GetPosition() + vVec * distance / 2;
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

        void TmPowerForward(float fPower)
        {
            if (btmRotor)
            {
                /*
                // need to ramp up/down rotor power or they will flip small vehicles and spin a lot
                float maxVelocity = rotorNavLeftList[0].GetMaximum<float>("Velocity");
                float currentVelocity = rotorNavLeftList[0].GetValueFloat("Velocity");
                float cPower = (currentVelocity / maxVelocity * 100);
                cPower = Math.Abs(cPower);
                if (fPower > (cPower + 5f))
                    fPower = cPower + 5;
                if (fPower < (cPower - 5))
                    fPower = cPower - 5;

                if (fPower < 0f) fPower = 0f;
                if (fPower > 100f) fPower = 100f;
                */
                powerUpRotors(fPower);
            }
            else
                powerUpThrusters(thrustTmForwardList, fPower);
        }

        void TmDoForward(double maxSpeed, float maxThrust)
        {
            if (!btmRotor)
            {
                if (velocityShip < 1)
                    powerUpThrusters(thrustTmForwardList, maxThrust);
                else if (velocityShip < maxSpeed * .75)
                    powerUpThrusters(thrustTmForwardList, 25f);
                else if (velocityShip < maxSpeed * .85)
                    powerUpThrusters(thrustTmForwardList, 15f);
                else if (velocityShip <= maxSpeed * .98)
                {
                    powerUpThrusters(thrustTmForwardList, 1f);
                }
                else if (velocityShip >= maxSpeed * 1.02)
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
            else
            { // rotor control
                TmPowerForward(maxThrust);
            }

        }


    }
}
