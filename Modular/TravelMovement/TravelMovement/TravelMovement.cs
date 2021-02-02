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
        public class TravelMovement
        {
            public bool bCollisionWasSensor = false;


            double tmCameraWaitMs = 0.25;
            double SensorSettleWaitMS = 0.175;

            /// <summary>
            /// Dont' use camera scan more than this time
            /// </summary>
            const string CameraTimer = "TMCameraTimer";
//            const string ScanTimer = "TMScanTimer";
/// <summary>
/// Allow this much time to go by after setting sensor settings before checking
/// </summary>
            const string SensorTimer = "TMSensorTimer";

            public Vector3D vAvoid;

            public string CurrentStatus = "UNINIT";

            // below are private
            IMyShipController tmShipController = null;
            double tmMaxSpeed = 100; // calculated max speed.

            List<IMyTerminalBlock> thrustTmBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmRightList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustTmDownList = new List<IMyTerminalBlock>();
            /// <summary>
            ///  GRID orientation to aim ship
            /// </summary>
            Vector3D vBestThrustOrientation;

            List<IMyTerminalBlock> thrustGravityUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustGravityDownList = new List<IMyTerminalBlock>();

            IMySensorBlock tmSB = null;

            bool btmApproach = false;
            bool btmPrecision = false;
            bool btmClose = false;
            double dtmFar = 100;
            double dtmApproach = 50;
            double dtmPrecision = 15;
            double dtmFarSpeed = 100;
            double dtmApproachSpeed = 100 * 0.5;
            double dtmPrecisionSpeed = 100 * 0.25;
            double dtmCloseSpeed = 5;

            float tmMaxSensorM = 50f;

            int dtmRayCastQuadrant = 0;
            // 0 = center. 1= TL, 2= TR, 3=BL, 4=BR


            // propulsion mode
            bool btmRotor = false;
            bool btmSled = false;
            bool btmWheels = false;
            bool btmHasGyros = false;
            // else it's gyros and thrusters

            bool dTMDebug = false;
            bool dTMUseCameraCollision = true;
            bool dTMUseSensorCollision = true;

            bool _InitialScanDone = false;

            string sSection = "Travel Movement";

            Program _program;
            WicoControl _wicoControl;
            WicoBlockMaster _wicoBlockMaster;

            WicoGyros _gyros;
            WicoThrusters _thrusters;
            Sensors _sensors;
            Cameras _cameras;
            Wheels _wheels;
            NavRotors _navRotors;

            public TravelMovement(Program program, WicoControl wicoControl
                , WicoBlockMaster wicoBlockMaster
                , WicoGyros wicoGyros
                , WicoThrusters wicoThrusters
                , Sensors wicoSensors
                , Cameras cameras
                ,Wheels wicoWheels
                ,NavRotors navRotors


                )
            {
                _program = program;
                _wicoControl = wicoControl;
                _wicoBlockMaster = wicoBlockMaster;

                _gyros = wicoGyros;
                _thrusters = wicoThrusters;
                _sensors = wicoSensors;
                _cameras = cameras;
                _wheels = wicoWheels;
                _navRotors = navRotors;

                dTMDebug = _program._CustomDataIni.Get(sSection, "Debug").ToBoolean(dTMDebug);
                _program._CustomDataIni.Set(sSection, "Debug", dTMDebug);

                dTMUseCameraCollision = _program._CustomDataIni.Get(sSection, "UseCameraCollision").ToBoolean(dTMUseCameraCollision);
                _program._CustomDataIni.Set(sSection, "UseCameraCollision", dTMUseCameraCollision);

                dTMUseSensorCollision=_program._CustomDataIni.Get(sSection, "UseSensorCollision").ToBoolean(dTMUseSensorCollision);
                _program._CustomDataIni.Set(sSection, "UseSensorCollision", dTMUseSensorCollision);

            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
            }
            void LocalGridChangedHandler()
            {
            }
            /// <summary>
            /// reset so the next call to doTravelMovement will re-initialize.
            /// </summary>
            public void ResetTravelMovement(WicoElapsedTime wicoElapsedTime)
            {
                _InitialScanDone = false;
                // invalidates any previous tm calculations
                tmShipController = null;
                _sensors.SensorsSleepAll(); // set sensors to lower power
                _gyros.SetMinAngle();
                //                minAngleRad = 0.01f; // reset Gyro aim tolerance to default
                wicoElapsedTime.ResetTimer(CameraTimer);
                wicoElapsedTime.ResetTimer(SensorTimer);
//                tmScanElapsedMs = 0;
//                tmCameraElapsedMs = -1;
                _wheels.WheelsPowerUp(0, 50);
                CurrentStatus = "RESET";

        }

        /// <summary>
        /// initialize the travel movement module.
        /// </summary>
        /// <param name="vTargetLocation"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="myShipController"></param>
        /// <param name="iThrustType"></param>
        public void InitDoTravelMovement(Vector3D vTargetLocation, double maxSpeed, float arrivalDistance, IMyShipController myShipController, int iThrustType = WicoThrusters.thrustAll)
            {
                _InitialScanDone = false;
                tmMaxSpeed = maxSpeed;
                if (tmMaxSpeed > _wicoControl.fMaxWorldMps)
                    tmMaxSpeed = _wicoControl.fMaxWorldMps;
                if (dTMDebug) _program.ErrorLog("Initial tmMax=" + _program.niceDoubleMeters(tmMaxSpeed));
                if (dTMDebug) _program.ErrorLog("Initial reqMax=" + _program.niceDoubleMeters(maxSpeed));

                if (myShipController == null) _program.Echo("NULL SHIP CONTROLLER");
                if (dTMDebug) _program.Echo("myShipController=" + myShipController);

                tmShipController = myShipController as IMyShipController;
                double velocityShip = tmShipController.GetShipSpeed();
                Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;

                double distance = vVec.Length();

                //TODO: add grav gen support
                if (_wheels.HasSledWheels())
                {
                    btmSled = true;
                    //                if (_shipSpeedMax > 45) _shipSpeedMax = 45;
                    //                    sStartupError += "\nI am a SLED!";
                    _wheels.PrepareSledTravel();
                    if (dTMDebug) _program.ErrorLog("I am a SLED!");
                }
                else btmSled = false;

                if (_wheels.HasWheels())
                {
                    btmWheels = true;
                    if (tmShipController is IMyShipController) tmShipController.HandBrake = false;
                }
                else btmWheels = false;

                if (_gyros.GyrosAvailable() > 0)
                {
                    btmHasGyros = true;
                }
                else btmHasGyros = false;

                if (_navRotors.NavRotorCount() > 0)
                {
                    btmRotor = true;
                    //                if (_shipSpeedMax > 15) _shipSpeedMax = 15;
                }
                else btmRotor = false;

                _thrusters.ThrustersCalculateOrientation(tmShipController, ref thrustTmForwardList, ref thrustTmBackwardList,
                ref thrustTmDownList, ref thrustTmUpList,
                ref thrustTmLeftList, ref thrustTmRightList);//, iThrustType);
                _sensors.SensorsSleepAll();
                if (dTMUseSensorCollision && _sensors.GetCount() > 0)
                {
                    tmSB = _sensors.GetForwardSensor();
                    //                    tmSB = sensorsList[0];
                    if (btmRotor || btmSled) tmSB.DetectAsteroids = false;
                    else
                        tmSB.DetectAsteroids = true;
                    tmSB.DetectEnemy = true;
                    tmSB.DetectLargeShips = true;
                    tmSB.DetectSmallShips = true;
                    tmSB.DetectStations = true;
                    tmSB.DetectPlayers = false; // run them over!
                    tmMaxSensorM = tmSB.GetMaximum<float>("Front");
//                    _program.ErrorLog("Found Sensors");
                    if (dTMUseCameraCollision && !_cameras.HasForwardCameras())//  cameraForwardList.Count < 1)
                    {
                        // sensors, but no cameras.
                        //                        if (!AllowBlindNav)
//                        _program.ErrorLog(" but no cameras");
                        tmMaxSpeed = tmMaxSensorM / 2;
                        //                        if (dTMUseCameraCollision) sStartupError += "\nNo Cameras for collision detection";
                    }
                }
                else
                {
                    // no sensors...
                    tmSB = null;
                    tmMaxSensorM = 0;
//                    _program.ErrorLog("Found NO Sensors");
                    if (dTMUseCameraCollision && !_cameras.HasForwardCameras())//  cameraForwardList.Count < 1)
                    {
                        //                        if (dTMUseCameraCollision || dTMUseSensorCollision) sStartupError += "\nNo Sensor nor cameras\n for collision detection";
                        //                        if (!AllowBlindNav)
//                        _program.ErrorLog(" and NO Cameras");
                        tmMaxSpeed = 5;
                    }
                    else
                    {
                        //                        if (dTMUseSensorCollision) sStartupError += "\nNo Sensor for collision detection";
                    }
                }
                btmApproach = false; // we have reached approach range
                btmPrecision = false; // we have reached precision range
                btmClose = false; // we have reached close range

                double optimalV = tmMaxSpeed;
                if (dTMDebug) _program.ErrorLog("Default OptimalV=" + _program.niceDoubleMeters(optimalV));

                // need to check other propulsion flags..
                //                if (!btmSled && !btmRotor) optimalV = CalculateOptimalSpeed(thrustTmBackwardList, distance);
                if (!btmRotor) optimalV = CalculateOptimalSpeed(thrustTmBackwardList, distance-arrivalDistance);

                if (optimalV < tmMaxSpeed)
                    tmMaxSpeed = optimalV;

                if (dTMDebug) _program.ErrorLog("Distance=" + _program.niceDoubleMeters(distance) + " OptimalV=" + _program.niceDoubleMeters(optimalV));

                dtmFarSpeed = tmMaxSpeed;
                dtmApproachSpeed = tmMaxSpeed * 0.50;
                dtmPrecisionSpeed = tmMaxSpeed * 0.25;

                /*
                // minimum speeds.
                if (dtmApproachSpeed < 5) dtmApproachSpeed = Math.Min(distance-arrivalDistance,5);
                if (dtmPrecisionSpeed < 5) dtmPrecisionSpeed = Math.Min(distance - arrivalDistance, 5);

                if (dtmPrecisionSpeed > dtmApproachSpeed) dtmApproachSpeed = dtmPrecisionSpeed;
                if (dtmPrecisionSpeed > dtmFarSpeed) dtmFarSpeed = dtmPrecisionSpeed;
                */

                //            dtmPrecision =calculateStoppingDistance(thrustTmBackwardList, dtmPrecisionSpeed*1.1, 0);
                //            dtmApproach = calculateStoppingDistance(thrustTmBackwardList, dtmApproachSpeed*1.1, 0);

                if (!(btmWheels || btmRotor))
                {
                    dtmPrecision = _thrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmPrecisionSpeed, 0);
                    dtmApproach = dtmPrecision + _thrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmApproachSpeed, 0);
                    dtmFar = dtmApproach + _thrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmFarSpeed, 0); 
//                  dtmApproach = _thrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmApproachSpeed + (dtmFarSpeed - dtmApproachSpeed) / 2, 0);
//                  dtmPrecision = _thrusters.calculateStoppingDistance(tmShipController, thrustTmBackwardList, dtmPrecisionSpeed + (dtmApproachSpeed - dtmPrecisionSpeed) / 2, 0);

                    if (dTMDebug) _program.ErrorLog("d=" + distance.ToString("0.0"));
                    if (dTMDebug) _program.ErrorLog("FW T=" + _thrusters.calculateMaxThrust(thrustTmForwardList));
                    if (dTMDebug) _program.ErrorLog("BW T=" + _thrusters.calculateMaxThrust(thrustTmBackwardList));
                    if (dTMDebug)
                    {
                        MyShipMass myMass;
                        myMass = tmShipController.CalculateShipMass();
                        double maxThrust = _thrusters.calculateMaxThrust(thrustTmForwardList);
                        double maxDeltaVFW = maxThrust / myMass.PhysicalMass;
                        _program.ErrorLog("FW DV=" + maxDeltaVFW.ToString("0.0"));

                        maxThrust = _thrusters.calculateMaxThrust(thrustTmBackwardList);
                        double maxDeltaVBW = maxThrust / myMass.PhysicalMass;
                        _program.ErrorLog("BW DV=" + maxDeltaVBW.ToString("0.0"));
                        _program.ErrorLog("BW/FW DV=" + (maxDeltaVBW / maxDeltaVFW).ToString("0.0"));
                    }
                }
                if (distance < dtmFar)
                    btmApproach = true;
                if (distance < dtmApproach)
                {
                    btmApproach = true;
                    btmPrecision = true;
                }
                if (distance < dtmPrecision)
                {
                    btmApproach = true;
                    btmPrecision = true;
                    btmClose = true;
                }

                bCollisionWasSensor = false;
//                tmCameraElapsedMs = -1; // no delay for next check  
//                tmScanElapsedMs = 0;// do delay until check 
                dtmRayCastQuadrant = 0;

                _gyros.SetMinAngle(0.01f);// minAngleRad = 0.01f; // reset Gyro aim tolerance to default

                Vector3D vNG = tmShipController.GetNaturalGravity();

                if (!btmSled && vNG.Length() > 0) // we have gravity
                {
                    Vector3D vNGN = vNG;
                    vNGN.Normalize();
                    _thrusters.GetBestThrusters(vNGN,
                        thrustTmForwardList, thrustTmBackwardList,
                        thrustTmDownList, thrustTmUpList,
                        thrustTmLeftList, thrustTmRightList,
                        out thrustGravityUpList, out thrustGravityDownList
                        );
                }
                else
                {
                    // TODO: Could also choose 'best thrust' instead of assuming 'forward'
                    Matrix or1;
                    tmShipController.Orientation.GetMatrix(out or1);
                    vBestThrustOrientation = or1.Forward;
                }
                if(dTMDebug) _program.ErrorLog(" TM Init: max=" + tmMaxSpeed.ToString("0") + ":far=" + dtmFar.ToString("0"));
                if (dTMDebug) _program.ErrorLog("FarSpeed=" + _program.niceDoubleMeters(dtmFarSpeed) + " ASpeed=" + _program.niceDoubleMeters(dtmApproachSpeed));
                if (dTMDebug) _program.ErrorLog("Far =" + _program.niceDoubleMeters(dtmFar) + " A=" + _program.niceDoubleMeters(dtmApproach) + " P=" + _program.niceDoubleMeters(dtmPrecision));
                if (dTMDebug) _program.ErrorLog("BtmA =" + btmApproach + " P=" + btmPrecision + " C=" + btmClose);
                CurrentStatus = "Initialized";
            }

            /// <summary>
            /// Does travel movement with collision detection and avoidance. On arrival, changes state to arrivalState. If collision, changes to colDetectState
            /// </summary>
            /// <param name="vTargetLocation">Location of target</param>
            /// <param name="arrivalDistance">minimum distance for 'arrival'</param>
            /// <param name="arrivalState">state to use when 'arrived'</param>
            /// <param name="colDetectState">state to use when 'collision'</param>
            /// <param name="bAsteroidTarget">if True, target location is in/near an asteroid.  don't collision detect with it</param>
            public void doTravelMovement(WicoElapsedTime wicoElapsedTime, Vector3D vTargetLocation, float arrivalDistance, int arrivalState, int colDetectState, 
                double maxSpeed, int iThrustType = WicoThrusters.thrustAll, bool bAsteroidTarget = false)
            {
                bool bArrived = false;
//                _program.EchoInstructions("DTM Start");
                if (dTMDebug)
                {
                    _program.Echo("dTM:" + _wicoControl.IState + "->" + arrivalState + "-C>" + colDetectState + " A:" + arrivalDistance);
                    //                    _program.Echo("dTM:" + current_state + "->" + arrivalState + "-C>" + colDetectState + " A:" + arrivalDistance);
                    _program.Echo("W=" + btmWheels.ToString() + " S=" + btmSled.ToString() + " R=" + btmRotor.ToString());
                }
//                _program.EchoInstructions("DTM A");

                if (tmShipController == null)
                {
                    if (dTMDebug) _program.Echo("FIRST! (do Init)");
                    // first (for THIS target) time init
                    InitDoTravelMovement(vTargetLocation, maxSpeed, arrivalDistance,_wicoBlockMaster.GetMainController(), iThrustType);
                    wicoElapsedTime.AddTimer(CameraTimer, tmCameraWaitMs);
                    wicoElapsedTime.StartTimer(CameraTimer);

                    wicoElapsedTime.AddTimer(SensorTimer, SensorSettleWaitMS);
                    wicoElapsedTime.StartTimer(SensorTimer);
                }
                Vector3D vg = tmShipController.GetNaturalGravity();
                double dGravity = vg.Length();
                double velocityShip = tmShipController.GetShipSpeed();

                Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;

                double distance = vVec.Length();

                // TODO: Adjust targetlocation for gravity and min altitude.
                //                if (NAVGravityMinElevation > 0 && dGravity > 0)
                if (_wicoBlockMaster.DesiredMinTravelElevation > 0 && dGravity > 0)
                {
                    // Need some trig here to calculate angle/distance to ground and target above ground
                    // Have: our altitude. Angle to target. Distance to target.
                    // iff target is 'below' us, then raise effective target to maintain min alt
                    // iff target is 'above' us..  then raise up to it..

                    // cheat for now.
                    if (distance < (arrivalDistance + _wicoBlockMaster.DesiredMinTravelElevation))
                    {
                        CurrentStatus = "Air Arrived";

                        bArrived = true;
                    }
                }

                if (dTMDebug)
                {
                    _program.Echo("dTM:distance=" + _program.niceDoubleMeters(distance) + " (" + arrivalDistance.ToString() + ")");
                    _program.Echo("dTM:velocity=" + velocityShip.ToString("0.00"));
                    _program.Echo("dTM:tmMaxSpeed=" + tmMaxSpeed.ToString("0.00"));
                }
                if (distance < arrivalDistance)
                    bArrived = true;

                if (bArrived)
                {
                    CurrentStatus = "Arrived";
                    _program.ResetMotion(); // start the stopping
                    _wicoControl.SetState(arrivalState);// current_state = arrivalState; // we have arrived
                    ResetTravelMovement(wicoElapsedTime); // reset our brain so we re-calculate for the next time we're called
                                           // TODO: Turn brakes ON (if not done in ResetMotion())
                    _wicoControl.WantFast();// bWantFast = true; // process this quickly
                    return;
                }

                List<IMySensorBlock> aSensors = null;

                double stoppingDistance = 0;
                if (!(btmWheels || btmRotor))
                {
                    var myMass = tmShipController.CalculateShipMass();
                    
                    stoppingDistance = _thrusters.calculateStoppingDistance(myMass.PhysicalMass,thrustTmBackwardList, velocityShip, 0);
                }
                // TODO: calculate stopping D for wheels


                if (tmSB!=null && dTMUseSensorCollision && _sensors.GetCount() > 0)
                {
                    //                    float fScanDist = Math.Min(1f, (float)stoppingDistance * 1.5f);
                    float fScanDist = Math.Min(tmMaxSensorM, (float)stoppingDistance * 1.5f);
                    if(!_InitialScanDone)
                    {
                        fScanDist = Math.Min(tmMaxSensorM, (float) distance);
                    }

                    if (!dTMUseCameraCollision)
                    {
                        if (_sensors.SensorSetToShip(tmSB, 1, 1, 1, 1, fScanDist, 0))
                            wicoElapsedTime.RestartTimer(SensorTimer); // if we change sensor, reset delay timer
                    }
                    else
                    {
                        if(_sensors.SensorSetToShip(tmSB, 0, 0, 0, 0, fScanDist, 0))
                            wicoElapsedTime.RestartTimer(SensorTimer); // if we change sensor, reset delay timer
                    }

                }
                //            else Echo("No Sensor for Travel movement");
                bool bAimed = false;

                Vector3D grav = tmShipController.GetNaturalGravity();
                if (
                    btmSled
                    || btmRotor
                    )
                {

                    double yawangle = -999;
                    yawangle = _gyros.CalculateYaw(vTargetLocation, tmShipController);
                    _program.Echo("yawangle=" + yawangle.ToString());
                    if (btmSled)
                    {
                        _program.Echo("Sled Rotate: "+_gyros.NumberUsedGyros()+" used Gyros");
                        _gyros.DoRotate(yawangle, "Yaw");
                    }
                    else if (btmRotor)
                    {
                        _program.Echo("Rotor Rotate");
                       
                        _navRotors.DoRotorRotate(yawangle);
                    }
                    bAimed = Math.Abs(yawangle) < .05;
                    /*
                    if(!bAimed && Math.Abs(yawangle)<0.2)
                    {
                        _program.Echo("Close Aim");
                        if (distance > dtmFar)
                        {
                            _program.Echo(" close enough to start forward");
                            TmDoForward(dtmApproachSpeed, 100f);
                        }
                    }
                    */
                }
                else if (btmWheels && btmHasGyros)
                {
                    _program.Echo("Wheels W/ Gyro");
                    double yawangle = -999;
                    yawangle = _gyros.CalculateYaw(vTargetLocation, tmShipController);
                    _program.Echo("yawangle=" + yawangle.ToString());
                    bAimed = Math.Abs(yawangle) < .05;
                    if (!bAimed)
                    {
                        _wheels.WheelsPowerUp(0, 5);
                        //                    WheelsSetFriction(0);
                        _gyros.DoRotate(yawangle, "Yaw");
                    }
                    else _wheels.WheelsSetFriction(50);
                }
                else if (btmWheels) // & ! btmHasGyro)
                {
                    _program.Echo("Wheels with no gyro...");
                    double yawangle = _program.CalculateYaw(vTargetLocation, tmShipController);
                    _program.Echo("yawangle=" + yawangle.ToString());
                    bAimed = Math.Abs(yawangle) < .05;
                    if (!bAimed)
                    {
                        // TODO: rotate with wheels..
                    }
                    else _wheels.WheelsSetFriction(50);
                }
                else
                {
                    if (grav.LengthSquared() > 0)
                    { // in gravity. try to stay aligned to gravity, but change yaw to aim at location.
                        _program.Echo("In Gravity Alignment");
                        if (_gyros.AlignGyros(vBestThrustOrientation, grav))
//                            bool bGravAligned = GyroMain("", grav, tmShipController);
                        //                    if (bGravAligned)
                        {
                            _program.Echo("AlignedG: Align target");
                            double yawangle = _gyros.CalculateYaw(vTargetLocation, tmShipController);
                            _gyros.DoRotate(yawangle, "Yaw");
                            bAimed = Math.Abs(yawangle) < .05;
                        }
                    }
                    else
                    {
                        Matrix or1;
                        tmShipController.Orientation.GetMatrix(out or1);
                        bAimed=_gyros.AlignGyros(or1.Forward, vVec);// bAimed = GyroMain("forward", vVec, tmShipController);
                    }
                }
                tmShipController.DampenersOverride = true;

                if ((distance - stoppingDistance) < arrivalDistance)
                { // we are within stopping distance, so start slowing
                    _gyros.SetMinAngle(0.0005f);// minAngleRad = 0.005f;// aim tighter (next time)
                                                            //                    StatusLog("\"Arriving at target.  Slowing", textPanelReport);
                    CurrentStatus = "Arrival Immenient\n  Waiting for stop";
                    _program.Echo("Waiting for stop");
                    if (!bAimed) _wicoControl.WantFast();// bWantFast = true;
                    else _wicoControl.WantMedium();
                    _program.ResetMotion();
                    return;
                }
                if (bAimed)
                {
                    _wicoControl.WantMedium(); // bWantMedium = true;
                    // we are aimed at location
                    _program.Echo("Aimed");
                    _gyros.gyrosOff();
                    //                    if (btmWheels) _wheels.WheelsSetFriction(50);
                    if (
                        dTMUseSensorCollision
                        && wicoElapsedTime.IsExpired(SensorTimer)
                        )
                    {
                        // DO NOT restart it.  only start timer when we set sensor ranges to be different
//                        wicoElapsedTime.RestartTimer(SensorTimer);
                        _InitialScanDone = true;

//                       tmScanElapsedMs = 0;
                        aSensors = _sensors.SensorsGetActive();

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
                                        _program.Echo(s);
                                        //                                        StatusLog(s, textLongStatus);
                                    }
                                    if (entities[j1].Type == MyDetectedEntityType.Planet)
                                    {
                                        bValidCollision = false;
                                    }
                                    if (entities[j1].Type == MyDetectedEntityType.LargeGrid
                                        || entities[j1].Type == MyDetectedEntityType.SmallGrid
                                        )
                                    {
                                        if (entities[j1].BoundingBox.Contains(vTargetLocation) != ContainmentType.Disjoint)
                                        {
                                            if (dTMDebug)
                                                _program.Echo("Ignoring collision because we want to be INSIDE");
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
                                    ResetTravelMovement(wicoElapsedTime);
                                    _wicoControl.SetState(colDetectState);// current_state = colDetectState; // set the collision detetected state
                                    bCollisionWasSensor = true;
                                    _wicoControl.WantFast();// bWantFast = true; // process next state quickly
                                    _program.ResetMotion(); // start stopping
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
                        {
                            if (distance < 500)
                                scanDistance = distance;
                            else scanDistance = 500;
                        }
                        scanDistance = Math.Min(distance, scanDistance);
                    }
                    if (dTMDebug)
                    {
                        _program.Echo("InitialScanDone=" + _InitialScanDone.ToString());
                        _program.Echo("Forward Cameras=" + _cameras.HasForwardCameras());
                        _program.Echo("Active=" + wicoElapsedTime.IsActive(CameraTimer).ToString());
                        _program.Echo("Expired=" + wicoElapsedTime.IsExpired(CameraTimer).ToString());
                        _program.Echo("distance=" + _program.niceDoubleMeters(distance));
                        _program.Echo("maxsensor=" + _program.niceDoubleMeters(tmMaxSensorM));
                        _program.Echo("distance>max" + (distance > tmMaxSensorM).ToString());
                    }
                    //               if (dTMDebug)
                    if (dTMUseCameraCollision)
                    {
                        //                    Echo("Scanning distance=" + niceDoubleMeters(scanDistance));
                    }
                    if (
                        dTMUseCameraCollision
                        && (
                                (
                                wicoElapsedTime.IsExpired(CameraTimer) //(tmCameraElapsedMs > tmCameraWaitMs || tmCameraElapsedMs < 0) // it is time to scan..
                                && distance > tmMaxSensorM // if we are in sensor range, we don't need to scan with cameras
                                )
                                || !_InitialScanDone
                            )
                        )
                    {
                        wicoElapsedTime.RestartTimer(CameraTimer);

                        OrientedBoundingBoxFaces orientedBoundingBox = new OrientedBoundingBoxFaces(tmShipController);
                        Vector3D[] points = new Vector3D[4];
                        orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, points); // front output order is BL, BR, TL, TR

                        // assumes moving forward...
                        // May 29, 2018 do a BB forward scan instead of just center..
                        bool bDidScan = false;
                        Vector3D vTarget;
                        switch (dtmRayCastQuadrant)
                        {
                            case 0:
                                if (_cameras.CameraForwardScan(scanDistance))
                                { // center scan.
                                    bDidScan = true;
                                }
                                break;
                            case 1:
                                vTarget = points[2] + tmShipController.WorldMatrix.Forward * distance;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 2:
                                vTarget = points[3] + tmShipController.WorldMatrix.Forward * distance;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 3:
                                vTarget = points[0] + tmShipController.WorldMatrix.Forward * distance;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 4:
                                vTarget = points[1] + tmShipController.WorldMatrix.Forward * distance;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 5:
                                // check center again.  always full length
                                if (_cameras.CameraForwardScan(scanDistance))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 6:
                                vTarget = points[2] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 7:
                                vTarget = points[3] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 8:
                                vTarget = points[0] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                }
                                break;
                            case 9:
                                vTarget = points[1] + tmShipController.WorldMatrix.Forward * distance / 2;
                                if (_cameras.CameraForwardScan(vTarget))
                                {
                                    bDidScan = true;
                                    _InitialScanDone = true;
                                }
                                break;
                        }

                        if (bDidScan)
                        {
                            dtmRayCastQuadrant++;
                            if (dtmRayCastQuadrant > 9) dtmRayCastQuadrant = 0;

                            wicoElapsedTime.RestartTimer(CameraTimer);// tmCameraElapsedMs = 0;
                            lastDetectedInfo = _cameras.lastDetectedInfo;
                            if (!lastDetectedInfo.IsEmpty())
                            {
                                bool bValidCollision = true;
                                // assume it MIGHT be asteroid and check
                                //                            if (bAsteroidTarget)
                                {
                                    if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                    {
                                        if (lastDetectedInfo.BoundingBox.Contains(vTargetLocation) != ContainmentType.Disjoint)
                                        { // if the target is inside the BB of the target, ignore the collision
                                            bValidCollision = false;
                                            // check to see if we are close enough to surface of asteroid
                                            double astDistance = ((Vector3D)lastDetectedInfo.HitPosition - tmShipController.GetPosition()).Length();
                                            if ((astDistance - stoppingDistance) < arrivalDistance)
                                            {
                                                _program.ResetMotion();
                                                _wicoControl.SetState(arrivalState);// current_state = arrivalState;
                                                ResetTravelMovement(wicoElapsedTime);
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
                                    _program.Echo("raycast hit:" + lastDetectedInfo.Type.ToString());
                                    //                                    StatusLog("Camera Trigger collision", textPanelReport);
                                }
                                if (bValidCollision)
                                {
                                    //                                sInitResults += "Camera collision: " + scanDistance + "\n" + lastDetectedInfo.Name + ":" + lastDetectedInfo.Type + "\n";
                                    // something in way.
                                    ResetTravelMovement(wicoElapsedTime); // reset our brain for next call
                                    _wicoControl.SetState(colDetectState);// current_state = colDetectState; // set the detetected state
                                    bCollisionWasSensor = false;
                                    _wicoControl.WantFast();// bWantFast = true; // process next state quickly
                                    _program.ResetMotion(); // start stopping
                                    return;
                                }
                            }
                            else
                            {
                                if (dTMDebug)
                                {
                                    //                            Echo(s);
                                    //                                   StatusLog("Camera Scan Clear", textPanelReport);
                                }
                            }
                        }
                        else
                        {
                            if (dTMDebug)
                            {
                                //                            Echo(s);
                                //                               StatusLog("No Scan Available", textPanelReport);
                            }
                        }
                    }
                    else
                    {
                        _program.Echo("Raycast delay");
                    }

                    if (dTMDebug)
                        _program.Echo("dtmFar=" + _program.niceDoubleMeters(dtmFar));
                    if (dTMDebug)
                        _program.Echo("dtmApproach=" + _program.niceDoubleMeters(dtmApproach));
                    if (dTMDebug)
                        _program.Echo("dtmPrecision=" + _program.niceDoubleMeters(dtmPrecision));

                    if (dTMDebug) _program.ErrorLog("D=" + _program.niceDoubleMeters(distance) + " AD=" + _program.niceDoubleMeters(arrivalDistance));

                    // before starting up full blast thrust, check for CLOSE collision 

                    if (_InitialScanDone || !(dTMUseCameraCollision || dTMUseSensorCollision)  )
                    {
                        if ((distance + arrivalDistance + velocityShip * 2) > dtmFar && !btmApproach)
                        {
                            CurrentStatus = "Far Travel";
                            // we are 'far' from target location.  use fastest movement
                            //                    if(dTMDebug)
                            _program.Echo("dtmFar.=" + _program.niceDoubleMeters(dtmFar) + " Target Vel=" + dtmFarSpeed.ToString("N0"));
                            if (dTMDebug) _program.ErrorLog("dtmFar.=" + _program.niceDoubleMeters(dtmFar) + " Target Vel=" + dtmFarSpeed.ToString("N0"));
                            //                        StatusLog("\"Far\" from target\n Target Speed=" + dtmFarSpeed.ToString("N0") + "m/s", textPanelReport);

                            MyShipMass myMass;
                            myMass = tmShipController.CalculateShipMass();
                            double maxThrust = _thrusters.calculateMaxThrust(thrustTmForwardList);
                            double maxDeltaVFW = maxThrust / myMass.PhysicalMass;
                            maxThrust = _thrusters.calculateMaxThrust(thrustTmBackwardList);
                            double maxDeltaVBW = maxThrust / myMass.PhysicalMass;
                            if (dTMDebug) _program.ErrorLog("BW/FW DV=" + (maxDeltaVBW / maxDeltaVFW).ToString("0.0"));
                            float fThrustPercent = 100;
                            if (maxDeltaVBW < distance) fThrustPercent = (float)Math.Min(100, (maxDeltaVBW / maxDeltaVFW) * 100);
                            _program.Echo("Far thrust%=" + fThrustPercent.ToString());
                            TmDoForward(dtmFarSpeed, fThrustPercent);
                        }
                        else if ((distance + arrivalDistance + velocityShip * 2) > dtmApproach && !btmPrecision)
                        {
                            // we are on 'approach' to target location.  use a good speed
                            //                    if(dTMDebug)
                            CurrentStatus = "Approach Travel";
                            _program.Echo("Approach. Target Vel=" + dtmApproachSpeed.ToString("N0"));
                            if (dTMDebug) _program.ErrorLog("Approach. Target Vel=" + dtmApproachSpeed.ToString("N0"));
                            //                        StatusLog("\"Approach\" distance from target\n Target Speed=" + dtmApproachSpeed.ToString("N0") + "m/s", textPanelReport);
                            btmApproach = true;
                            TmDoForward(dtmApproachSpeed, 100f);
                        }
                        else if ((distance + arrivalDistance + velocityShip * 2) > dtmPrecision && !btmClose)
                        {
                            CurrentStatus = "Precision Travel";
                            //                    if(dTMDebug)
                            _program.Echo("Precision. Target Vel=" + dtmPrecisionSpeed.ToString("N0"));
                            if (dTMDebug) _program.ErrorLog("Precision. Target Vel=" + dtmPrecisionSpeed.ToString("N0"));
                            //                        StatusLog("\"Precision\" distance from target\n Target Speed=" + dtmPrecisionSpeed.ToString("N0") + "m/s", textPanelReport);
                            if (!btmPrecision)
                            { // first time entering distance
                                MyShipMass myMass;
                                myMass = tmShipController.CalculateShipMass();
                                double maxThrust = _thrusters.calculateMaxThrust(thrustTmForwardList);
                                double maxDeltaVFW = maxThrust / myMass.PhysicalMass;
                                maxThrust = _thrusters.calculateMaxThrust(thrustTmBackwardList);
                                double maxDeltaVBW = maxThrust / myMass.PhysicalMass;
                                if (dTMDebug) _program.ErrorLog("BW/FW DV=" + (maxDeltaVBW / maxDeltaVFW).ToString("0.0"));

                                _gyros.SetMinAngle(0.005f);// minAngleRad = 0.005f;// aim tighter (next time)
                                btmPrecision = true;
                            }
                            TmDoForward(dtmPrecisionSpeed, 25);
                            _wicoControl.WantFast();
                        }
                        else
                        {
                            // we are very close to our target. use a very small speed
                            //                    if(dTMDebug)
                            CurrentStatus = "Close Travel";
                            _program.Echo("Close. Target Speed=" + dtmCloseSpeed.ToString("N0") + "m/s");
                            if (dTMDebug) _program.ErrorLog("Close. Target Speed=" + dtmCloseSpeed.ToString("N0") + "m/s");
                            //                        StatusLog("\"Close\" distance from target\n Target Speed=" + dtmCloseSpeed.ToString("N0") + "m/s", textPanelReport);
                            if (!btmClose)
                            { // first time entering this distance
                                MyShipMass myMass;
                                myMass = tmShipController.CalculateShipMass();
                                double maxThrust = _thrusters.calculateMaxThrust(thrustTmForwardList);
                                double maxDeltaVFW = maxThrust / myMass.PhysicalMass;
                                maxThrust = _thrusters.calculateMaxThrust(thrustTmBackwardList);
                                double maxDeltaVBW = maxThrust / myMass.PhysicalMass;
                                if (dTMDebug) _program.ErrorLog("BW/FW DV=" + (maxDeltaVBW / maxDeltaVFW).ToString("0.0"));

                                _gyros.SetMinAngle(0.005f);//minAngleRad = 0.005f;// aim tighter (next time)
                                btmClose = true;
                            }
                            TmDoForward(dtmCloseSpeed, 15);
                            _wicoControl.WantFast();
                        }
                    }
                    else // we are waiting for initial scan to complete
                    {
                        CurrentStatus = "Waiting for Initial Scan";
                    }
                }
                else
                {
                    CurrentStatus = "Aiming";
                    //                    StatusLog("Aiming at target", textPanelReport);
                    if (dTMDebug) _program.Echo("Aiming");
                    _wicoControl.WantMedium();
//                    _wicoControl.WantFast();// bWantFast = true;
                    tmShipController.DampenersOverride = true;
                    if (velocityShip < 5)
                    {
                        // we are probably doing precision maneuvers.  Turn on all thrusters to avoid floating past target
                        _thrusters.powerDownThrusters();
                    }
                    else
                    {
                        _thrusters.powerDownThrusters(thrustTmBackwardList, WicoThrusters.thrustAll, true); // coast
                    }
                    //		sleepAllSensors();
                }

            }
            /// <summary>
            /// returns the optimal max speed to go the distnace specied based on available braking thrust and ship mass
            /// </summary>
            /// <param name="thrustList">Thrusters to use</param>
            /// <param name="distance">current distance to target location</param>
            /// <returns>optimal max speed (may be game max speed)</returns>
            public double CalculateOptimalSpeed(List<IMyTerminalBlock> thrustList, double distance)
            {
                if(thrustList==null)
                {
                    _program.ErrorLog("COS: NULL list");
                    return 0;
                }
                if (thrustList.Count < 1) return _wicoControl.fMaxWorldMps;
                distance = Math.Abs(distance);

                IMyShipController myShip = tmShipController;
                if (myShip == null) myShip = _wicoBlockMaster.GetMainController();

                MyShipMass myMass;
                myMass = myShip.CalculateShipMass();
                double maxThrust = _thrusters.calculateMaxThrust(thrustList);
                double maxDeltaV = maxThrust / myMass.PhysicalMass;
                double optimalV, secondstozero, stoppingM;

                if (dTMDebug)
                {
                    _program.ErrorLog("distance=" + distance.ToString("N1") + " MaxdeltaV=" + maxDeltaV.ToString("F1"));
                    _program.Echo("distance=" + distance.ToString("N1") + " MaxdeltaV=" + maxDeltaV.ToString("F1"));
                }

                optimalV = tmMaxSpeed;
                if (dTMDebug) _program.ErrorLog("initial optimalV=" + _program.niceDoubleMeters(optimalV));

                do
                {
                    secondstozero = optimalV / maxDeltaV;
                    stoppingM = optimalV / 2 * secondstozero;
                    if (dTMDebug)
                    {
                        _program.Echo("stopm=" + stoppingM.ToString("F1") + " distance=" + distance.ToString("N1"));
                    }
                    if (stoppingM > distance)
                    {
                        optimalV *= 0.85;
                    }

                }
                while (stoppingM > 0.01 && stoppingM > distance);

                return optimalV;
            }

            /// <summary>
            /// Calculate a new waypoint given our wanted target waypoint and the entity we detected.
            /// </summary>
            /// <param name="vTargetLocation">where we want to go</param>
            /// <param name="lastDetectedInfo">the entity detected</param>
            /// <param name="vAvoid">set the the waypoint to avoid the collision</param>
            public void calcCollisionAvoid(Vector3D vTargetLocation, MyDetectedEntityInfo lastDetectedInfo, out Vector3D vAvoid)
            {
                Vector3D vHit;
                if (lastDetectedInfo.HitPosition.HasValue)
                {
                    vHit = (Vector3D)lastDetectedInfo.HitPosition;
                }
                else
                {
                    vHit = _wicoBlockMaster.GetMainController().GetPosition();
                }

                Vector3D vCenter = lastDetectedInfo.Position;

                // need to check if vector is straight through the object.  then choose 90% vector or something
                Vector3D vVec = (vCenter - vHit);
                vVec.Normalize();

                Vector3D vMinBound = lastDetectedInfo.BoundingBox.Min;
                Vector3D vMaxBound = lastDetectedInfo.BoundingBox.Max;

                double radius = (vCenter - vMinBound).Length();

                // adjust the radius to allow the craft to miss the collision object
                double modRadius = radius + _wicoBlockMaster.WidthInMeters() * 5;

                Vector3D cross;
                cross = Vector3D.Cross(vTargetLocation, vHit);
                cross.Normalize();

                // vHit location start position and then adjusted in cross directoy by modified radius length
                vAvoid = vHit + cross * modRadius;
            }


            // pathfinding routines.  Try to escape from inside an asteroid.

            bool bScanLeft = true;
            bool bScanRight = true;
            bool bScanUp = true;
            bool bScanDown = true;
            bool bScanBackward = true;
            bool bScanForward = true;

            MyDetectedEntityInfo lastDetectedInfo;

            MyDetectedEntityInfo leftDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo rightDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo upDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo downDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo backwardDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo forwardDetectedInfo = new MyDetectedEntityInfo();

            //        bool bEscapeGrid = false;

            QuadrantCameraScanner ScanEscapeFrontScanner;
            QuadrantCameraScanner ScanEscapeBackScanner;
            QuadrantCameraScanner ScanEscapeLeftScanner;
            QuadrantCameraScanner ScanEscapeRightScanner;
            QuadrantCameraScanner ScanEscapeTopScanner;
            QuadrantCameraScanner ScanEscapeBottomScanner;

            public MyDetectedEntityInfo LastDetectedInfo
            {
                get
                {
                    return lastDetectedInfo;
                }

                set
                {
                    lastDetectedInfo = value;
                }
            }

            IMyShipController _escapeController;
            /// <summary>
            /// Initialize the escape scanning (mini-pathfinding)
            /// Call once to setup
            /// </summary>
            public void initEscapeScan(IMyShipController escapeController, bool bWantBack = false, bool bWantForward = true)
            {
                _escapeController = escapeController;
                bScanLeft = true;
                bScanRight = true;
                bScanUp = true;
                bScanDown = true;
                bScanBackward = bWantBack;// don't rescan where we just came from..
                bScanForward = bWantForward;

                leftDetectedInfo = new MyDetectedEntityInfo();
                rightDetectedInfo = new MyDetectedEntityInfo();
                upDetectedInfo = new MyDetectedEntityInfo();
                downDetectedInfo = new MyDetectedEntityInfo();
                backwardDetectedInfo = new MyDetectedEntityInfo();
                forwardDetectedInfo = new MyDetectedEntityInfo();

                //            bEscapeGrid = false;
                if (lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid
                    || lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid
                    )
                {
                    //               bEscapeGrid = true;
                }
                
                // don't assume all drones have all cameras..
                if (_cameras.HasLeftCameras()) bScanLeft = false;
                if (_cameras.HasRightCameras()) bScanRight = false;
                if (_cameras.HasUpCameras()) bScanUp = false;
                if (_cameras.HasDownCameras()) bScanDown = false;
                if (_cameras.HasForwardCameras()) bScanForward = false;
                if (_cameras.HasBackCameras()) bScanBackward = false;
                ScanEscapeFrontScanner = new QuadrantCameraScanner(_program, _cameras.GetForwardCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeBackScanner = new QuadrantCameraScanner(_program, _cameras.GetBackwardCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeLeftScanner = new QuadrantCameraScanner(_program, _cameras.GetLeftCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeRightScanner = new QuadrantCameraScanner(_program, _cameras.GetRightCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeTopScanner = new QuadrantCameraScanner(_program, _cameras.GetUpCameras(), 200, 45, 45, 2, 1, 5, 200, true);
                ScanEscapeBottomScanner = new QuadrantCameraScanner(_program, _cameras.GetDownwardCameras(), 200, 45, 45, 2, 1, 5, 200, true);

            }
            /// <summary>
            /// Perform the pathfinding. Call this until it returns true
            /// </summary>
            /// <returns>true if vAvoid now contains the location to go to to (try to) escape</returns>
            public bool scanEscape()
            {
                //               if (tmCameraElapsedMs >= 0) tmCameraElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                //                if (tmScanElapsedMs >= 0) tmScanElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                _program.Echo("ScanEscape() actual");

                MatrixD worldtb = _escapeController.WorldMatrix;
                Vector3D vVec = worldtb.Forward;
                if (bScanLeft)
                {
                    //                sStartupError+="\nLeft";
                    if (_cameras.CameraLeftScan(200))//   doCameraScan(_cameras.cameraLeftList, 200))
                    {
                        bScanLeft = false;
                        leftDetectedInfo = lastDetectedInfo;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            //                        sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Left;
                            vVec.Normalize();
                            vAvoid = _escapeController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanLeft = ScanEscapeLeftScanner.DoScans();
                    if (ScanEscapeLeftScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        leftDetectedInfo = lastDetectedInfo;
                        vAvoid = _escapeController.GetPosition() + ScanEscapeLeftScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanRight)
                {
                    //                sStartupError += "\nRight";
                    if (_cameras.CameraRightScan(200))// if (doCameraScan(cameraRightList, 200))
                    {
                        bScanRight = false;
                        rightDetectedInfo = lastDetectedInfo;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            //                        sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Right;
                            vVec.Normalize();
                            vAvoid = _escapeController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanRight = ScanEscapeRightScanner.DoScans();
                    if (ScanEscapeRightScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        rightDetectedInfo = lastDetectedInfo;
                        vAvoid = _escapeController.GetPosition() + ScanEscapeRightScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanUp)
                {
                    //                sStartupError += "\nUp";
                    if (_cameras.CameraUpScan(200))// if (doCameraScan(cameraUpList, 200))
                    {
                        //                  upDetectedInfo = lastDetectedInfo;
                        bScanUp = false;
                        if (lastDetectedInfo.IsEmpty())
                        {
//                            sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Up;
                            vVec.Normalize();
                            vAvoid = _escapeController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanUp = ScanEscapeTopScanner.DoScans();
                    if (ScanEscapeTopScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        upDetectedInfo = lastDetectedInfo;
                        vAvoid = _escapeController.GetPosition() + ScanEscapeTopScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanDown)
                {
                    //                sStartupError += "\nDown";
                    if (_cameras.CameraDownScan(200))// if (doCameraScan(cameraDownList, 200))
                    {
                        //                    sStartupError += "\n Straight Camera HIT!";
                        downDetectedInfo = lastDetectedInfo;
                        bScanDown = false;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            vVec = worldtb.Down;
                            vVec.Normalize();
                            vAvoid = _escapeController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanDown = ScanEscapeBottomScanner.DoScans();
                    if (ScanEscapeBottomScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        downDetectedInfo = lastDetectedInfo;
                        vAvoid = _escapeController.GetPosition() + ScanEscapeBottomScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanBackward)
                {
                    //                sStartupError += "\nBack";
                    if (_cameras.CameraBackwardScan(200))// if (doCameraScan(cameraBackwardList, 200))
                    {
                        //                    sStartupError += "\n Straight Camera HIT!";
                        backwardDetectedInfo = lastDetectedInfo;
                        bScanBackward = false;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            vVec = worldtb.Backward;
                            vVec.Normalize();
                            vAvoid = _escapeController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanBackward = ScanEscapeBackScanner.DoScans();
                    if (ScanEscapeBackScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        backwardDetectedInfo = lastDetectedInfo;
                        vAvoid = _escapeController.GetPosition() + ScanEscapeBackScanner.vEscapeTarget * 200;
                        return true;
                    }
                }
                if (bScanForward)
                {
                    //                sStartupError += "\nForward";
                    if (_cameras.CameraForwardScan(200))// if (doCameraScan(cameraForwardList, 200))
                    {
                        bScanForward = false;
                        forwardDetectedInfo = lastDetectedInfo;
                        if (lastDetectedInfo.IsEmpty())
                        {
                            //                        sStartupError += "\n Straight Camera HIT!";
                            vVec = worldtb.Forward;
                            vVec.Normalize();
                            vAvoid = _escapeController.GetPosition() + vVec * 200;
                            return true;
                        }
                    }
                    bScanForward = ScanEscapeFrontScanner.DoScans();
                    if (ScanEscapeFrontScanner.bFoundExit)
                    {
                        //                    sStartupError += "\n Quadrant Camera HIT!";
                        forwardDetectedInfo = lastDetectedInfo;
                        vAvoid = ScanEscapeFrontScanner.vEscapeTarget * 200;
                        vAvoid = _escapeController.GetPosition() + ScanEscapeFrontScanner.vEscapeTarget * 200;
                        return true;
                    }
                }

                if (bScanForward || bScanBackward || bScanUp || bScanDown || bScanLeft || bScanRight)
                {
                    _program.Echo("More scans");
                    return false; // still more scans to go
                }

                // nothing was 'clear'.  find longest vector and try to go that direction
                _program.Echo("Scans done. Choose longest");
                MyDetectedEntityInfo furthest = backwardDetectedInfo;
                Vector3D currentpos = _escapeController.GetPosition();
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
                _program.Echo("Distance=" + _program.niceDoubleMeters(distance));
                vVec.Normalize();
                vAvoid = _escapeController.GetPosition() + vVec * distance / 2;
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

                _program.Echo("not FAR enough: ERROR!");
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
                    _navRotors.powerUpRotors(fPower);
                }
                else
                    _thrusters.powerUpThrusters(thrustTmForwardList, fPower);
            }

            void TmDoForward(double maxSpeed, float maxThrust)
            {
                double velocityShip = tmShipController.GetShipSpeed();
                if(btmRotor)
                { // rotor control
                    TmPowerForward(maxThrust);
                }
                else if (btmWheels)
                {
                    if (velocityShip < 1)
                    {
                        // full power, captain!
                        _wheels.WheelsPowerUp(maxThrust);
                    }
                    // if we need to go much faster or we are FAR and not near max speed
                    else if (velocityShip < maxSpeed * .75 || (!btmApproach && velocityShip < maxSpeed * .98))
                    {
                        float delta = (float)maxSpeed / _wicoControl.fMaxWorldMps * maxThrust;
                        _wheels.WheelsPowerUp(maxThrust);
                    }
                    else if (velocityShip < maxSpeed * .85)
                        _wheels.WheelsPowerUp(maxThrust);
                    else if (velocityShip <= maxSpeed * .98)
                    {
                        _wheels.WheelsPowerUp(maxThrust);
                    }
                    else if (velocityShip >= maxSpeed * 1.02)
                    {
                        // TOO FAST
                        _wheels.WheelsPowerUp(0);
                    }
                    else // sweet spot
                    {
                        _wheels.WheelsPowerUp(1);
                    }

                }
                else // if (!btmRotor)
                {
                    if (velocityShip < 1)
                    {
                        // full power, captain!
                        CurrentStatus += "\n Start Movement";
                        _thrusters.powerUpThrusters(thrustTmForwardList, maxThrust*1.2f);
                    }
                    // if we need to go much faster or we are FAR and not near max speed
                    else if (velocityShip < maxSpeed * .75 || (!btmApproach && velocityShip < maxSpeed * .98))
                    {
                        CurrentStatus += "\n Accelerating";

                        float delta = (float)maxSpeed / _wicoControl.fMaxWorldMps * maxThrust;
                        _thrusters.powerUpThrusters(thrustTmForwardList, delta);
                    }
                    else if (velocityShip < maxSpeed * .85)
                    {
                        CurrentStatus += "\n Small Accel";
                        _thrusters.powerUpThrusters(thrustTmForwardList, 15f);
                    }
                    else if (velocityShip <= maxSpeed * .98)
                    {
                        CurrentStatus += "\n Tiny Accel";
                        _thrusters.powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= maxSpeed * 1.02)
                    {
                        CurrentStatus += "\n Too fast";
                        _thrusters.powerDownThrusters();
                    }
                    else // sweet spot
                    {
                        CurrentStatus += "\n Coasting";
                        _thrusters.powerDownThrusters(); // turns ON all thrusters
                                                                     // turns off the 'backward' thrusters... so we don't slow down
                        _thrusters.powerDownThrusters(thrustTmBackwardList, WicoThrusters.thrustAll, true);
                        //                 tmShipController.DampenersOverride = false; // this would also work, but then we don't get ship moving towards aim point as we correct
                    }
                }

            }
        }
    }
}
