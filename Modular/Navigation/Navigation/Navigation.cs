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
        class Navigation
        {
            Program thisProgram;
            IMyShipController shipController;

            bool bGoOption = false;
            Vector3D vAvoid;

            public Navigation(Program program, IMyShipController myShipController)
            {
                thisProgram = program;
                shipController = myShipController;

                thisProgram.moduleName += " Navigation";
                thisProgram.moduleList += "\nNavigation V4";

                thisProgram.AddUpdateHandler(UpdateHandler);
                thisProgram.AddTriggerHandler(ProcessTrigger);
                thisProgram.wicoControl.AddModeInitHandler(ModeInitHandler);
                thisProgram.wicoControl.AddControlChangeHandler(ModeChangeHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if (fromMode == WicoControl.MODE_LAUNCHPREP)
                {
                    thisProgram.wicoThrusters.powerDownThrusters();
                    thisProgram.wicoGasTanks.TanksStockpile(false);
                    thisProgram.wicoGasGens.GasGensEnable(true);
                }


                if (toMode == WicoControl.MODE_ORBITALLAUNCH)
                {
                    thisProgram.wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_HOVER)
                {
                    thisProgram.wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_LAUNCHPREP)
                {
                    thisProgram.wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_DESCENT)
                {
                    thisProgram.wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_ORBITALLAND)
                {
                    thisProgram.wicoControl.WantOnce();
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;
                if (iMode == WicoControl.MODE_ORBITALLAUNCH)
                {
                    thisProgram.wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_ORBITALLAND)
                {
                    thisProgram.wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_DESCENT)
                {
                    thisProgram.wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_HOVER)
                {
                    thisProgram.wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_LAUNCHPREP)
                {
                    thisProgram.wicoControl.SetState(0);
                    thisProgram.wicoControl.WantFast();
                }

            }
            void LocalGridChangedHandler()
            {
                shipController = null;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if (myCommandLine != null)
                {
                    if (myCommandLine.Argument(0) == "orbitallaunch")
                    {
                        thisProgram.wicoControl.SetMode(WicoControl.MODE_ORBITALLAUNCH);
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                // need to check if this is us
                if (iMode == WicoControl.MODE_GOINGTARGET)
                {
                    doModeGoTarget();
                }
            }



            /// <summary>
            /// We are a sled. Default false
            /// </summary>
            bool bSled = false;

            /// <summary>
            /// We are rotor-control propulsion. Default false
            /// </summary>
            bool bRotor = false;

            bool bWheels = false;

            // propulsion mode
            bool btmRotor = false;
            bool btmSled = false;
            bool btmWheels = false;
            bool btmHasGyros = false;
            // else it's gyros and thrusters
            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();
            /*
            States:
            0 -- Master Init


                150. (spawn) initialize command in gravity. first align to gravity (created for EFM)

            160 Main Travel to target



            *** below here are thruster-only routines (for now)

            300 Collision Detected From 160
                Calculate collision avoidance 
                then ->320

            301 dummy state for debugging.
            320 do travel movement for collision avoidance. 
            if arrive target, ->160
            if secondary collision ->340

            340 secondary collision
            if a type we can move around, try to move ->350
            else go back to collision detection ->300

            350 initilize escape plan
            ->360

            360 scan for an 'escape' route (pathfind)
            timeout of (default) 5 seconds ->MODE_ATTENTION
            after scans, ->380

            380 travel to avoidance waypoint
            on arrival ->160 (main travel)
            on collision ->340

            500 Arrived at target
            ->MODE_ARRIVEDTARGET

            */

            Vector3D GridUpVector;
            Vector3D GridRightVector;

            // Current entity
            Vector3D vNavTarget;// vTargetMine;
            bool bValidNavTarget = false;
            DateTime dtNavStartShip;

            /// <summary>
            /// Set maximum travel speed of ship. 
            /// Set this using S command for NAV
            /// </summary>
            double shipSpeedMax = 9999;

            /// <summary>
            /// the minimum distance to be from the target to be considered 'arrived'
            /// </summary>
            double arrivalDistanceMin = 50;

            int NAVArrivalMode = WicoControl.MODE_ARRIVEDTARGET;
            int NAVArrivalState = 0;

            string NAVTargetName = "";

            //        Vector3D vNavLaunch;
            //        bool bValidNavLaunch = false;
            //        Vector3D vNavHome;
            //        bool bValidNavHome = false;
            bool NAVEmulateOld = false;
            bool AllowBlindNav = false;
            //            float NAVGravityMinElevation = -1;

            bool bNavBeaconDebug = false;


            string sNavSection = "NAV";

            void doModeGoTarget()
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                //                StatusLog("clear", textPanelReport);

                //                StatusLog(moduleName + ":Going Target!", textPanelReport);
                //            StatusLog(moduleName + ":GT: iState=" + iState.ToString(), textPanelReport);
                //            bWantFast = true;
                thisProgram.Echo("Going Target: state=" + iState.ToString());
                if (NAVTargetName != "") thisProgram.Echo(NAVTargetName);

                string sNavDebug = "";
                sNavDebug += "GT:S=" + iState;
                //            sNavDebug += " MinE=" + NAVGravityMinElevation;
                //            ResetMotion();
                IMyShipController shipController = thisProgram.wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dGravity = vNG.Length();

                if(thrustForwardList.Count<1)
                {
                    thisProgram.wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }

                if (iState == 0)
                {
                    thisProgram.wicoTravelMovement.ResetTravelMovement();
                    //                sStartupError+="\nStart movemenet: ArrivalMode="+NAVArrivalMode+" State="+NAVArrivalState;
                    //                    if ((craft_operation & CRAFT_MODE_SLED) > 0)
                    if (thisProgram.wicoWheels.HasSledWheels())
                    {
                        bSled = true;
                        if (shipSpeedMax > 45) shipSpeedMax = 45;
                    }
                    else bSled = false;

                    //                    if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
                    if (thisProgram.wicoNavRotors.NavRotorCount() > 0)
                    {
                        bRotor = true;
                        if (shipSpeedMax > 15) shipSpeedMax = 15;
                    }
                    else bRotor = false;
                    //                    if ((craft_operation & CRAFT_MODE_WHEEL) > 0)
                    if (thisProgram.wicoWheels.HasWheels())
                    {
                        bWheels = true;
                        //                   if (shipSpeedMax > 15) shipSpeedMax = 15;
                    }
                    else bWheels = false;

                    //                    GyroControl.SetRefBlock(shipOrientationBlock);

                    // TODO: Put a timer on this so it's not done Update1
                    double elevation = 0;
                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);

                    if (!bSled && !bRotor)
                    { // if flying ship
                      // make sure set to default
                        if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation < 0)
                            thisProgram.wicoBlockMaster.DesiredMinTravelElevation = 75; // for EFM getting to target 'arrived' radius
                    }

                    if (bValidNavTarget)
                    {
                        if (elevation > thisProgram.wicoBlockMaster.HeightInMeters())
                        {
                            iState = 150;
                        }
                        else iState = 160;
                    }
                    else thisProgram.wicoControl.SetMode(WicoControl.MODE_ATTENTION);//else setMode(MODE_ATTENTION);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                else if (iState == 150)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (dGravity > 0)
                    {

                        double elevation = 0;

                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");

                        float fSaveAngle = thisProgram.wicoGyros.GetMinAngle();// minAngleRad;
                        thisProgram.wicoGyros.SetMinAngle(0.1f);// minAngleRad = 0.1f;

                        bool bAligned = GyroMain("", vNG, shipController);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        thisProgram.Echo("bAligned=" + bAligned.ToString());
                        thisProgram.wicoGyros.SetMinAngle(fSaveAngle); //minAngleRad = fSaveAngle;
                        if (bAligned || elevation < thisProgram.wicoBlockMaster.HeightInMeters() * 2)
                        {
                            thisProgram.wicoGyros.gyrosOff();
                            if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0)
                                iState = 155;
                            else iState = 160;
                        }
                    }
                    else iState = 160;

                }
                else if (iState == 151)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (dGravity > 0 || btmWheels)
                    {

                        double elevation = 0;

                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");

                        float fSaveAngle = thisProgram.wicoGyros.GetMinAngle();// minAngleRad;
                        thisProgram.wicoGyros.SetMinAngle(0.1f);// minAngleRad = 0.1f;

                        bool bAligned = GyroMain("", vNG, shipController);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        thisProgram.Echo("bAligned=" + bAligned.ToString());
                        thisProgram.wicoGyros.SetMinAngle(fSaveAngle); //minAngleRad = fSaveAngle;
                        if (bAligned || elevation < thisProgram.wicoBlockMaster.HeightInMeters() * 2)
                        {
                            thisProgram.wicoGyros.gyrosOff();
                            if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0)
                                iState = 155;
                            else iState = 160;
                        }
                        else iState = 150;// try again to be aligned.
                    }
                    else iState = 160;

                }
                else if (iState == 155)
                { // for use in gravity: aim at location using yaw only
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (bWheels)
                    {
                        iState = 160;
                        return;
                    }

                    if (dGravity > 0)
                    {
                        bool bAligned = GyroMain("", vNG, shipController);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        double yawangle = -999;
                        yawangle = thisProgram.CalculateYaw(vNavTarget, shipController);
                        bool bAimed = Math.Abs(yawangle) < 0.1; // NOTE: 2x allowance
                        thisProgram.Echo("yawangle=" + yawangle.ToString());
                        sNavDebug += " Yaw=" + yawangle.ToString("0.00");

                        if (!bAimed)
                        {
                            if (btmRotor)
                            {
                                thisProgram.Echo("Rotor");
                                thisProgram.wicoNavRotors.DoRotorRotate(yawangle);
                            }
                            else // use for both sled and flight
                            {
                                thisProgram.wicoGyros.DoRotate(yawangle, "Yaw");
                            }
                        }
                        if (bAligned && bAimed)
                        {
                            thisProgram.wicoGyros.gyrosOff();
                            iState = 160;
                        }
                        else if (bAligned && Math.Abs(yawangle) < 0.5)
                        {
                            float atmo;
                            float hydro;
                            float ion;

                            thisProgram.wicoThrusters.CalculateHoverThrust(shipController, thrustForwardList, out atmo, out hydro, out ion);
                            atmo += 1;
                            hydro += 1;
                            ion += 1;

                            thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, atmo, WicoThrusters.thrustatmo);
                            thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, hydro, WicoThrusters.thrusthydro);
                            thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, ion, WicoThrusters.thrustion);

                        }
                        else
                            thisProgram.wicoThrusters.powerDownThrusters(thrustForwardList);
                    }
                    else iState = 160;
                }
                else if (iState == 156)
                {
                    // realign gravity
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    bool bAimed = GyroMain("", grav, shipController);
                    if (bAimed)
                    {
                        thisProgram.wicoGyros.gyrosOff();
                        iState = 160;
                    }
                }
                else if (iState == 160)
                { //	160 move to Target
                    thisProgram.Echo("Moving to Target");
                    Vector3D vTargetLocation = vNavTarget;
                    double velocityShip = shipController.GetShipSpeed();

                    Vector3D vVec = vTargetLocation - shipController.GetPosition();
                    double distance = vVec.Length();
                    thisProgram.Echo("distance=" + thisProgram.niceDoubleMeters(distance));
                    thisProgram.Echo("velocity=" + velocityShip.ToString("0.00"));

                    //                    StatusLog("clear", sledReport);
                    string sTarget = "Moving to Target";
                    if (NAVTargetName != "") sTarget = "Moving to " + NAVTargetName;
                    //                    StatusLog(sTarget + "\nD:" + niceDoubleMeters(distance) + " V:" + velocityShip.ToString(velocityFormat), sledReport);
                    //                    StatusLog(sTarget + "\nDistance: " + niceDoubleMeters(distance) + "\nVelocity: " + niceDoubleMeters(velocityShip) + "/s", textPanelReport);


                    if (bGoOption && (distance < arrivalDistanceMin))
                    {
                        iState = 500;

                        thisProgram.Echo("we have arrived");
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                        return;
                    }

                    //                debugGPSOutput("TargetLocation", vTargetLocation);
                    bool bDoTravel = true;

                    if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0 && dGravity > 0)
                    {
                        double elevation = 0;

                        MyShipVelocities mysSV = shipController.GetShipVelocities();
                        Vector3D lv = mysSV.LinearVelocity;

                        // ASSUMES: -up = gravity down  Assuming ship orientation
                        var upVec = shipController.WorldMatrix.Up;
                        var vertVel = Vector3D.Dot(lv, upVec);

                        //                    thisProgram.Echo("LV=" + Vector3DToString(lv));
                        //                    sNavDebug += " LV=" + Vector3DToString(lv);
                        //                    sNavDebug += " vertVel=" + vertVel.ToString("0.0");
                        //                    sNavDebug += " Hvel=" + lv.Y.ToString("0.0");

                        // NOTE: Elevation is only updated by game every 30? ticks. so it can be WAY out of date based on movement
                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");
                        sNavDebug += " V=" + velocityShip.ToString("0.00");

                        thisProgram.Echo("Elevation=" + elevation.ToString("0.0"));
                        thisProgram.Echo("MinEle=" + thisProgram.wicoBlockMaster.DesiredMinTravelElevation.ToString("0.0"));

                        //                    double stopD = calculateStoppingDistance(thrustUpList, velocityShip, dGravity);
                        double stopD = 0;
                        if (vertVel < 0)
                        {
                            stopD = thisProgram.wicoThrusters.calculateStoppingDistance(shipController, thrustUpList, Math.Abs(vertVel), dGravity);
                        }
                        double maxStopD = thisProgram.wicoThrusters.calculateStoppingDistance(shipController, thrustUpList, thisProgram.wicoControl.fMaxWorldMps, dGravity);

                        float atmo;
                        float hydro;
                        float ion;
                        thisProgram.wicoThrusters.CalculateHoverThrust(shipController, thrustUpList, out atmo, out hydro, out ion);

                        //                    sNavDebug += " SD=" + stopD.ToString("0");

                        if (
                            //                        !bSled && !bRotor && 
                            thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0)
                        {
                            if (
                                vertVel < -0.5  // we are going downwards
                                && (elevation - stopD * 2) < thisProgram.wicoBlockMaster.DesiredMinTravelElevation)
                            { // too low. go higher
                              // Emergency thrust
                                sNavDebug += " EM UP!";

                                bool bAligned = GyroMain("", grav, shipController);

                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, 100);
                                bDoTravel = false;
                                thisProgram.wicoControl.WantFast();// bWantFast = true;
                            }
                            else if (elevation < thisProgram.wicoBlockMaster.DesiredMinTravelElevation)
                            {
                                // push upwards
                                atmo += Math.Min(5f, (float)shipSpeedMax);
                                hydro += Math.Min(5f, (float)shipSpeedMax);
                                ion += Math.Min(5f, (float)shipSpeedMax);
                                sNavDebug += " UP! A" + atmo.ToString("0.00");// + " H"+hydro.ToString("0.00") + " I"+ion.ToString("0.00");
                                                                              //powerUpThrusters(thrustUpList, 100);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, atmo, WicoThrusters.thrustatmo);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, hydro, WicoThrusters.thrusthydro);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, ion, WicoThrusters.thrustion);

                            }
                            else if (elevation > (maxStopD + thisProgram.wicoBlockMaster.DesiredMinTravelElevation * 1.25))
                            {
                                // if we are higher than maximum possible stopping distance, go down fast.
                                sNavDebug += " SUPERHIGH";

                                //                           Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                                //                            bool bAligned = GyroMain("", grav, shipOrientationBlock);

                                thisProgram.wicoThrusters.powerDownThrusters(thrustUpList, thrustAll, true);
                                bool bAligned = GyroMain("", grav, shipController);
                                if (!bAligned)
                                {
                                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                                    bDoTravel = false;
                                }
                                //                            powerUpThrusters(thrustUpList, 1f);
                            }
                            else if (
                                elevation > thisProgram.wicoBlockMaster.DesiredMinTravelElevation * 2  // too high
                                                                                                       //                            && ((elevation-stopD)>NAVGravityMinElevation) // we can stop in time.
                                                                                                       //                        && velocityShip < shipSpeedMax * 1.1 // to fast in any direction
                                                                                                       //                           && Math.Abs(lv.X) < Math.Min(25, shipSpeedMax) // not too fast 
                                                                                                       //                            && Math.Abs(lv.Y) < Math.Min(25, shipSpeedMax) // not too fast downwards (or upwards)
                                )
                            { // too high 
                                sNavDebug += " HIGH";
                                //DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");

                                if (vertVel > 2) // going up
                                { // turn off thrusters.
                                    sNavDebug += " ^";
                                    thisProgram.wicoThrusters.powerDownThrusters(thrustUpList, thrustAll, true);
                                }
                                else if (vertVel < -0.5) // going down
                                {
                                    sNavDebug += " v";
                                    if (vertVel > (-Math.Min(15, shipSpeedMax)))
                                    {
                                        // currently descending at less than desired
                                        atmo -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                        hydro -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                        ion -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                        sNavDebug += " DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");
                                                                                        //                                   bDoTravel = false;
                                    }
                                    else
                                    {
                                        // we are descending too fast.
                                        atmo += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                        hydro += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                        ion += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                        sNavDebug += " 2FAST! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");
                                        bool bAligned = GyroMain("", grav, shipController);
                                        if (!bAligned)
                                        {
                                            thisProgram.wicoControl.WantFast();// bWantFast = true;
                                            bDoTravel = false;
                                        }
                                        //                                    bDoTravel = false;
                                    }

                                }
                                else
                                {
                                    sNavDebug += " -";
                                    atmo -= 5;
                                    hydro -= 5;
                                    ion -= 5;
                                }

                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, atmo, WicoThrusters.thrustatmo);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, hydro, WicoThrusters.thrusthydro);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, ion, WicoThrusters.thrustion);

                            }
                            else
                            {
                                // normal hover
                                thisProgram.wicoThrusters.powerDownThrusters(thrustUpList);
                            }
                        }
                    }
                    if (bDoTravel)
                    {
                        thisProgram.Echo("Do Travel");
                        thisProgram.wicoTravelMovement.doTravelMovement(vTargetLocation, (float)arrivalDistanceMin, 500, 300);
                    }
                    else
                    {
                        thisProgram.wicoThrusters.powerDownThrusters(thrustForwardList);
                    }
                }

                else if (iState == 300)
                { // collision detection

                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    Vector3D vTargetLocation = vNavTarget;
                    thisProgram.wicoTravelMovement.ResetTravelMovement();
                    thisProgram.wicoTravelMovement.calcCollisionAvoid(vTargetLocation, lastDetectedInfo, out vAvoid);

                    //                iState = 301; // testing
                    iState = 320;
                }
                else if (iState == 301)
                {
                    // just hold this state
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }

                else if (iState == 320)
                {
                    //                 Vector3D vVec = vAvoid - shipOrientationBlock.GetPosition();
                    //                double distanceSQ = vVec.LengthSquared();
                    thisProgram.Echo("Primary Collision Avoid");
                    //                    StatusLog("clear", sledReport);
                    //                    StatusLog("Collision Avoid", sledReport);
                    //                    StatusLog("Collision Avoid", textPanelReport);
                    thisProgram.wicoTravelMovement.doTravelMovement(vAvoid, 5.0f, 160, 340);
                }
                else if (iState == 340)
                {       // secondary collision
                    if (
                        lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid
                        || lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid
                        )
                    {
                        iState = 345;
                    }
                    else if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid
                        )
                    {
                        iState = 350;
                    }
                    else iState = 300;// setMode(MODE_ATTENTION);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                else if (iState == 345)
                {
                    // we hit a grid.  align to it
                    Vector3D[] corners = new Vector3D[BoundingBoxD.CornerCount];

                    BoundingBoxD bbd = lastDetectedInfo.BoundingBox;
                    bbd.GetCorners(corners);

                    GridUpVector = thisProgram.wicoSensors.PlanarNormal(corners[3], corners[4], corners[7]);
                    GridRightVector = thisProgram.wicoSensors.PlanarNormal(corners[0], corners[1], corners[4]);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    iState = 348;
                }
                else if (iState == 348)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (GyroMain("up", GridUpVector, shipController))
                    {
                        iState = 349;
                    }
                }
                else if (iState == 349)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (GyroMain("right", GridRightVector, shipController))
                    {
                        iState = 350;
                    }
                }
                else if (iState == 350)
                {
                    //                initEscapeScan(bCollisionWasSensor, !bCollisionWasSensor);
                    thisProgram.wicoTravelMovement.initEscapeScan(bCollisionWasSensor);
                    thisProgram.wicoTravelMovement.ResetTravelMovement();
                    dtNavStartShip = DateTime.Now;
                    iState = 360;
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                else if (iState == 360)
                {
                    //                    StatusLog("Collision Avoid\nScan for escape route", textPanelReport);
                    DateTime dtMaxWait = dtNavStartShip.AddSeconds(5.0f);
                    DateTime dtNow = DateTime.Now;
                    if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                    {
                        thisProgram.wicoControl.SetMode(WicoControl.MODE_ATTENTION);// setMode(MODE_ATTENTION);
                        //                        doTriggerMain();
                        return;
                    }
                    if (thisProgram.wicoTravelMovement.scanEscape())
                    {
                        thisProgram.Echo("ESCAPE!");
                        iState = 380;
                    }
                    thisProgram.wicoControl.WantMedium(); // bWantMedium = true;
                    //                bWantFast = true;
                }
                else if (iState == 380)
                {
                    //                    StatusLog("Collision Avoid Travel", textPanelReport);
                    thisProgram.Echo("Escape Collision Avoid");
                    thisProgram.wicoTravelMovement.doTravelMovement(vAvoid, 1f, 160, 340);
                }
                else if (iState == 500)
                { // we have arrived at target

                    /*
                    // check for more nav commands
                    if(wicoNavCommands.Count>0)
                    {
                        wicoNavCommands.RemoveAt(0);
                    }
                    if(wicoNavCommands.Count>0)
                    {
                        // another command
                        wicoNavCommandProcessNext();
                    }
                    else
                    */
                    {

                        //                        StatusLog("clear", sledReport);
                        //                        StatusLog("Arrived at Target", sledReport);
                        //                        StatusLog("Arrived at Target", textPanelReport);
                        sNavDebug += " ARRIVED!";

                        ResetMotion();
                        bValidNavTarget = false; // we used this one up.
                                                 //                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                        thisProgram.wicoAntennas.SetMaxPower(false);
                        thisProgram.wicoSensors.SensorsSleepAll();
                        //                    sStartupError += "Finish WP:" + wicoNavCommands.Count.ToString()+":"+NAVArrivalMode.ToString();
                        // set to desired mode and state
                        thisProgram.wicoControl.SetMode(NAVArrivalMode);// setMode(NAVArrivalMode);
                        iState = NAVArrivalState;

                        // set up defaults for next run (in case they had been changed)
                        NAVArrivalMode = WicoControl.MODE_ARRIVEDTARGET;
                        NAVArrivalState = 0;
                        NAVTargetName = "";
                        bGoOption = true;

                        //                setMode(MODE_ARRIVEDTARGET);
                        if (NAVEmulateOld)
                        {
                            var tList = GetBlocksContains<IMyTerminalBlock>("NAV:");
                            for (int i1 = 0; i1 < tList.Count(); i1++)
                            {
                                // don't want to get blocks that have "NAV:" in customdata..
                                if (tList[i1].CustomName.StartsWith("NAV:"))
                                {
                                    thisProgram.Echo("Found NAV: command:");
                                    tList[i1].CustomName = "NAV: C Arrived Target";
                                }
                            }
                        }
                    }
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                                                       //                    doTriggerMain();
                }
                //                NavDebug(sNavDebug);
            }
            void ResetMotion(bool bNoDrills = false)
            {
                thisProgram.wicoThrusters.powerDownThrusters();
                thisProgram.wicoGyros.gyrosOff();
                thisProgram.wicoNavRotors.powerDownRotors();
                thisProgram.wicoWheels.WheelsPowerUp(0, 75);

                if (shipController is IMyRemoteControl) ((IMyRemoteControl)shipController).SetAutoPilotEnabled(false);
                if (shipController is IMyShipController) ((IMyShipController)shipController).DampenersOverride = true;
                //            if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).HandBrake = true;
            }
        }

    }

}
