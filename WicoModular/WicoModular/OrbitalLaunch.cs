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
    
        class OrbitalModes
        {
            Program thisProgram;

            int retroStartAlt = 1300;

            float orbitalAtmoMult = 55;
            float orbitalIonMult = 2;
            float orbitalHydroMult = 1;

            double dLastVelocityShip = -1;

            float fOrbitalAtmoPower = 0;
            float fOrbitalHydroPower = 0;
            float fOrbitalIonPower = 0;

            bool bHasAtmo = false;
            bool bHasHydro = false;
            bool bHasIon = false;

            bool bOrbitalLaunchDebug = false;

            //            string sOrbitalUpDirection = "";
            Vector3D vGroundOrientation;

            List<IMyTerminalBlock> thrustOrbitalUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustOrbitalDownList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> cameraOrbitalLandingList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();

            public OrbitalModes(Program program)
            {
                thisProgram = program;
                thisProgram.UpdateUpdateHandlers.Add(UpdateHandler);
                thisProgram.wicoControl.AddControlChangeHandler(modeChangeHandler);
                thisProgram.UpdateTriggerHandlers.Add(ProcessTrigger);
            }

            public void modeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if(fromMode== WicoControl.MODE_ORBITALLAUNCH)
                {
                }
                if(fromMode==WicoControl.MODE_HOVER)
                {

                }
                if (toMode==WicoControl.MODE_ORBITALLAUNCH)
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
                if (iMode == WicoControl.MODE_ORBITALLAUNCH)
                {
                    ModeOrbitalLaunch(updateSource);
                }
                else if (iMode == WicoControl.MODE_LAUNCHPREP)
                {
                    doModeLaunchprep(updateSource);
                }
                else if (iMode == WicoControl.MODE_HOVER)
                {
                    ModeHover(updateSource);
                }
                else if (iMode == WicoControl.MODE_DESCENT)
                {
                    doModeDescent(updateSource);
                }
            }

            // MODE_ORBITAL_LAUNCH states
            // 0 init. prepare for solo
            // check all connections. hold launch until disconnected
            // 
            // 10 capture location and init thrust settings.
            // 20 initial thrust. trying to move
            // 30 initial lift-off achieved.  start landing config retraction
            // 31 continue to accelerate
            // 35 optimal alignment change.  wait  for new alignment

            // 40 have reached max; maintain
            // 150 wait for release..
            public void ModeOrbitalLaunch(UpdateType updateSource)
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                // need to check if this is us
                if (iMode != WicoControl.MODE_ORBITALLAUNCH)
                {
                    return;
                }
                thisProgram.Echo("MODE: Orbital Launch");
                //                thisProgram.Echo(updateSource.ToString());

                //                thisProgram.wicoControl.WantMedium();
                IMyShipController shipController = thisProgram.wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();

                bool bAligned = false;
                if (iState == 0 || thrustOrbitalUpList.Count < 1)
                {
                    thisProgram.wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    Vector3D vNGN = vNG;
                    vNGN.Normalize();
                    thisProgram.wicoThrusters.GetBestThrusters(vNGN,
                        thrustForwardList, thrustBackwardList,
                        thrustDownList, thrustUpList,
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList
                        );
                    Matrix or1;
                    thrustOrbitalUpList[0].Orientation.GetMatrix(out or1);
                    vGroundOrientation = or1.Forward; // start out aiming at whatever the up thrusters are aiming at..


                    /*
                    thisProgram.wicoThrusters.GetMaxScaledThrusters(
                        thrustForwardList, thrustBackwardList, 
                        thrustDownList, thrustUpList, 
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList,
                        orbitalAtmoMult, orbitalIonMult, orbitalHydroMult);

                    */


                    //                    calculateBestGravityThrust();

                    bHasAtmo = false;
                    bHasHydro = false;
                    bHasIon = false;
                    if (thisProgram.wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustion) != null)
                        bHasIon = true;
                    if (thisProgram.wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrusthydro) != null)
                        bHasHydro = true;
                    if (thisProgram.wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustatmo) != null)
                        bHasAtmo = true;

                }
                if (iState == 0)
                {
                    //		dtStartShip = DateTime.Now;
                    thisProgram.wicoControl.WantOnce();
                    //                    dLastVelocityShip = 0;
                    //                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) 
                    thisProgram.wicoGasTanks.TanksStockpile(false);
                    thisProgram.wicoGasGens.GasGensEnable(true);

                    if (thisProgram.wicoConnectors.AnyConnectorIsConnected()
                        || thisProgram.wicoConnectors.AnyConnectorIsLocked()
                        || thisProgram.wicoLandingGears.AnyGearIsLocked())
                    {
                        // launch from connected
                        //                       vOrbitalLaunch = shipOrientationBlock.GetPosition();
                        //                        bValidOrbitalLaunch = true;
                        //                        bValidOrbitalHome = false; // forget any 'home' waypoint.

                        thisProgram.wicoConnectors.ConnectAnyConnectors(false, false);

                        thisProgram.wicoLandingGears.GearsLock(false);
                        thisProgram.wicoControl.SetState(150);// = 150;
                        return;
                    }
                    else
                    {
                        // launch from hover mode
                        //                        bValidOrbitalLaunch = false;
                        //                        vOrbitalHome = shipOrientationBlock.GetPosition();
                        //                        bValidOrbitalHome = true;

                        // assume we are hovering; do FULL POWER launch.
                        fOrbitalAtmoPower = 0;
                        fOrbitalHydroPower = 0;
                        fOrbitalIonPower = 0;

                        if (bHasIon)
                            //                        if (bHasIon)
                            fOrbitalIonPower = 75;
                        if (bHasHydro)
                        //                            if (bHasHydro)
                        { // only use Hydro power if they are already turned on
                            for (int i = 0; i < thrustOrbitalUpList.Count; i++)
                            {
                                if (thisProgram.wicoThrusters.ThrusterType(thrustOrbitalUpList[i]) == WicoThrusters.thrusthydro)
                                    if (thrustOrbitalUpList[i].IsWorking)
                                    {
                                        fOrbitalHydroPower = 100;
                                        break;
                                    }
                            }
                        }
                        //                        if (bHasAtmo)
                        if (thisProgram.wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustatmo) != null)
                            fOrbitalAtmoPower = 100;

                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

                        thisProgram.wicoControl.SetState(30);
                        //                        current_state = 30;
                        return;
                    }
                }
                if (iState == 150)
                {
                    //                    StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + current_state.ToString(), textLongStatus, true);
                    if (thisProgram.wicoConnectors.AnyConnectorIsConnected() || thisProgram.wicoConnectors.AnyConnectorIsLocked() || thisProgram.wicoLandingGears.AnyGearIsLocked())
                    {
                        //                        StatusLog("Awaiting release", textPanelReport);
                        //                        Log("Awaiting release");
                    }
                    else
                    {
                        //                        StatusLog(DateTime.Now.ToString() + " " + OurName + ":Saved Position", textLongStatus, true);

                        // we launched from connected. Save position
                        //                        vOrbitalLaunch = shipOrientationBlock.GetPosition();
                        //                        bValidOrbitalLaunch = true;
                        //                        bValidOrbitalHome = false; // forget any 'home' waypoint.

                        thisProgram.wicoControl.SetState(10);

                        //                        next_state = 10;
                    }

                }
                double elevation = 0;

                shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                //               StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
                double alt = elevation; // note: if we use camera raycast, we can more accuratly determine altitude.

                if (iState == 10)
                {
                    thisProgram.wicoControl.WantOnce();
                    thisProgram.wicoThrusters.CalculateHoverThrust(shipController, thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);
                    thisProgram.wicoControl.SetState(20);
                    //                   current_state = 20;
                    return;
                }
                double velocityShip = shipController.GetShipSpeed();
                double deltaV = velocityShip - dLastVelocityShip;
                double expectedV = deltaV * 5 + velocityShip;

                double dLength = vNG.Length();
                double dGravity = dLength / 9.81;

                if (iState == 20)
                { // trying to move
                  //                    StatusLog("Attempting Lift-off", textPanelReport);
                  //                    Log("Attempting Lift-off");
                  // NOTE: need to NOT turn off atmo if we get all the way into using ions for this state.. and others?
                    if (velocityShip < 3f)
                    // if(velocityShip<1f)
                    {
                        increasePower(dGravity, alt);
                        increasePower(dGravity, alt);
                    }
                    else
                    {
                        thisProgram.wicoControl.SetState(30);// we have started to lift off.
                                                             //                        next_state = 30; // we have started to lift off.
                        dLastVelocityShip = 0;
                    }

                    //                  string sOrientation = "up";
                    //                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                    //                        sOrientation = "rocket";

                    //                    bAligned = thisProgram.wicoGyros.AlignGyros(sOrientation,vNG,shipController);
                    bAligned = thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
                    if (!bAligned)
                        thisProgram.wicoControl.WantFast();
                    //                        bWantFast = true;
                }
                else
                {
                    thisProgram.wicoControl.WantMedium();
                    //                    bWantMedium = true;
                    if (alt > 5)
                    {
                        {

                            //                            bAligned = thisProgram.wicoGyros.AlignGyros(sOrbitalUpDirection, vNG, shipController);
                            bAligned = thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
                            if (!bAligned)
                                thisProgram.wicoControl.WantFast();
                            //                            bWantFast = true;
                        }
                    }
                }
                if (iState == 30)
                { // Retract landing config
                  //                    StatusLog("Movement started. Retracting Landing config ", textPanelReport);
                  //                    Log("Movement started. Retracting Landing config ");
                    thisProgram.wicoControl.SetState(31);// next_state = 31;
                }
                if (iState == 31)
                { // accelerate to max speed

                    // TODO: only check every so often.
                    if (CheckAttitudeChange())
                    {
                        thisProgram.wicoControl.SetState(35);// current_state = 35;
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                        return;

                    }
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

                    //                    StatusLog("Accelerating to max speed (" + thisProgram.wicoControl.fMaxWorldMps.ToString("0") + ")", textPanelReport);
                    //                    Log("Accelerating to max speed");
                    thisProgram.Echo("Accelerating to max speed");
                    if (dLastVelocityShip < velocityShip)
                    { // we are Accelerating
                        if (bOrbitalLaunchDebug) thisProgram.Echo("Accelerating");
                        if (expectedV < (thisProgram.wicoControl.fMaxWorldMps / 2))
                        // if(velocityShip<(fMaxMps/2))
                        {
                            decreasePower(dGravity, alt); // take away from lowest provider
                            increasePower(dGravity, alt);// add it back
                        }
                        if (expectedV < (thisProgram.wicoControl.fMaxWorldMps / 5)) // add even more.
                            increasePower(dGravity, alt);

                        if (velocityShip > (thisProgram.wicoControl.fMaxWorldMps - 5))
                            thisProgram.wicoControl.SetState(40);// next_state = 40;
                    }
                    else
                    {
                        increasePower(dGravity, alt);//
                        increasePower(dGravity, alt);// and add some more
                    }
                }
                if (iState == 35)
                {
                    // re-align and then resume
                    thisProgram.wicoThrusters.powerDownThrusters();
                    //                bAligned = GyroMain(sOrbitalUpDirection);
                    if (bAligned)
                        thisProgram.wicoControl.SetState(31); // next_state = 31;
                }
                if (iState == 40)
                { // maintain max speed
                    thisProgram.Echo("Maintain max speed");
                    //                    Log("Maintain max speed");

                    // TODO: only check every so often.
                    if (CheckAttitudeChange())
                    {
                        thisProgram.wicoControl.SetState(35);// current_state = 35;
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                        return;

                    }
                    //                  if (bOrbitalLaunchDebug) StatusLog("Expectedv=" + expectedV.ToString("0.00") + " max=" + thisProgram.wicoControl.fMaxWorldMps.ToString("0.00"), textPanelReport);
                    //                    if (bOrbitalLaunchDebug)
                    //                        thisProgram.Echo("Expectedv=" + expectedV.ToString("0.00") + " max=" + thisProgram.wicoControl.fMaxWorldMps.ToString("0.00"));
                    double dMin = (thisProgram.wicoControl.fMaxWorldMps - thisProgram.wicoControl.fMaxWorldMps * .02); // within n% of max mps
                                                                                                                       //                    thisProgram.Echo("dMin=" + dMin.ToString("0.00"));
                    if (expectedV > dMin)
                    // if(velocityShip>(fMaxMps-5))
                    {
                        bool bThrustOK = thisProgram.wicoThrusters.CalculateHoverThrust(shipController, thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                        //                        if (bOrbitalLaunchDebug) 
                        //                        thisProgram.Echo("hover thrust:" + fOrbitalAtmoPower.ToString("0.00") + ":" + fOrbitalHydroPower.ToString("0.00") + ":" + fOrbitalIonPower.ToString("0.00"));
                        //                        if (bOrbitalLaunchDebug) StatusLog("hover thrust:" + fOrbitalAtmoPower.ToString("0.00") + ":" + fOrbitalHydroPower.ToString("0.00") + ":" + fOrbitalIonPower.ToString("0.00"), textPanelReport);

                    }
                    else if (expectedV < (thisProgram.wicoControl.fMaxWorldMps - 10))
                    {
                        //                      thisProgram.Echo("Increase power");
                        decreasePower(dGravity, alt); // take away from lowest provider
                        increasePower(dGravity, alt);// add it back
                        increasePower(dGravity, alt);// and add some more
                    }
                    if (velocityShip < (thisProgram.wicoControl.fMaxWorldMps / 2))
                        thisProgram.wicoControl.SetState(20);// next_state = 20;

                    thisProgram.wicoConnectors.ConnectAnyConnectors(false, true);// "OnOff_On");
                    thisProgram.wicoLandingGears.BlocksOnOff(true);// blocksOnOff(gearList, true);
                    //                blockApplyAction(gearList, "OnOff_On");
                }
                if (iState == 45)
                {
                    // re-align and then resume
                    thisProgram.wicoThrusters.powerDownThrusters();
                    bAligned = thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
                    //                    bAligned = thisProgram.wicoGyros.AlignGyros(sOrbitalUpDirection, vNG, shipController); //GyroMain(sOrbitalUpDirection);

                    if (bAligned)
                        thisProgram.wicoControl.SetState(40);// next_state = 40;
                }
                dLastVelocityShip = velocityShip;

                //               StatusLog("", textPanelReport);

                //	if (bValidExtraInfo)
                //                StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

                //	batteryCheck(0, false);//,textPanelReport);
                //	if (bValidExtraInfo)
                /*
                if (batteryList.Count > 0)
                {
                    StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                    //                Echo("BatteryPercentage=" + batteryPercentage);
                }
                else StatusLog("Bat: <NONE>", textPanelReport);
                */
                /*
                if (oxyPercent >= 0)
                {
                    StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                    //Echo("O:" + oxyPercent.ToString("000.0%"));
                }
                else Echo("No Oxygen Tanks");
                */
                /*
                if (hydroPercent >= 0)
                {
                    StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                    //                Echo("H:" + (hydroPercent*100).ToString("0.0") + "%");
                    if (hydroPercent < 0.20f)
                    {
                        StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);
                        Log(" WARNING: Low Hydrogen Supplies");
                    }
                }
                else Echo("No Hydrogen Tanks");
                */

                /*
                if (batteryList.Count > 0 && batteryPercentage < batterypctlow)
                {
                    StatusLog(" WARNING: Low Battery Power", textPanelReport);
                    Log(" WARNING: Low Battery Power");
                }
                */
                //                StatusLog("", textPanelReport);
                if (dGravity < 0.01)
                {
                    thisProgram.wicoThrusters.powerDownThrusters();
                    thisProgram.wicoGyros.gyrosOff();
                    //                startNavCommand("!;V");
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);// setMode(MODE_NAVNEXTTARGET);
                                                                                    //                    StatusLog("clear", textPanelReport);
                                                                                    //                    Log("clear");
                    return; //GPS:Wicorel #5:14690.86:106127.43:10724.23:
                }

                int iPowered = 0;

                //                thisProgram.Echo("IonPower=" + fOrbitalIonPower.ToString("0.00"));
                if (fOrbitalIonPower > 0.01)
                {
                    thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustatmo, true);
                    thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustion);
                    iPowered = thisProgram.wicoThrusters.powerUpThrusters(thrustOrbitalUpList, fOrbitalIonPower, WicoThrusters.thrustion);
                    //Echo("Powered "+ iPowered.ToString()+ " Ion Thrusters");
                }
                else
                {
                    thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustion, true);
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion);
                }

                //                thisProgram.Echo("HydroPower=" + fOrbitalHydroPower.ToString("0.00"));
                if (fOrbitalHydroPower > 0.01)
                {
                    //                Echo("Powering Hydro to " + fHydroPower.ToString());
                    thisProgram.wicoThrusters.powerUpThrusters(thrustOrbitalUpList, fOrbitalHydroPower, WicoThrusters.thrusthydro);
                }
                else
                { // important not to let them provide dampener power..
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrusthydro, true);
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro, true);
                }
                //                thisProgram.Echo("AtmoPower=" + fOrbitalAtmoPower.ToString("0.00"));
                if (fOrbitalAtmoPower > 0.01)
                {
                    thisProgram.wicoThrusters.powerUpThrusters(thrustOrbitalUpList, fOrbitalAtmoPower, WicoThrusters.thrustatmo);
                }
                else
                {
                    //                    closeDoors(outterairlockDoorList);

                    // iPowered=powerDownThrusters(thrustStage1UpList,thrustatmo,true);
                    iPowered = thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustatmo, true);
                    //Echo("Powered DOWN "+ iPowered.ToString()+ " Atmo Thrusters");
                }

                {
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);
                }

                /*
                StatusLog("Thrusters", textPanelReport);
                if (ionThrustCount > 0)
                {
                    if (fOrbitalIonPower < .01) StatusLog("ION: Off", textPanelReport);
                    else if (fOrbitalIonPower < 10) StatusLog("ION:\n/10:" + progressBar(fOrbitalIonPower * 10), textPanelReport);
                    else StatusLog("ION:" + progressBar(fOrbitalIonPower), textPanelReport);
                }
                else StatusLog("ION: None", textPanelReport);
                if (hydroThrustCount > 0)
                {
                    if (fOrbitalHydroPower < .01) StatusLog("HYD: Off", textPanelReport);
                    else if (fOrbitalHydroPower < 10) StatusLog("HYD\n/10:" + progressBar(fOrbitalHydroPower * 10), textPanelReport);
                    else StatusLog("HYD:" + progressBar(fOrbitalHydroPower), textPanelReport);
                }
                else StatusLog("HYD: None", textPanelReport);
                if (atmoThrustCount > 0)
                {
                    if (fOrbitalAtmoPower < .01) StatusLog("ATM: Off", textPanelReport);
                    else if (fOrbitalAtmoPower < 10)
                        StatusLog("ATM\n/10:" + progressBar(fOrbitalAtmoPower * 10), textPanelReport);
                    else
                        StatusLog("ATM:" + progressBar(fOrbitalAtmoPower), textPanelReport);
                }
                else StatusLog("ATM: None", textPanelReport);
                if (bOrbitalLaunchDebug)
                    StatusLog("I:" + fOrbitalIonPower.ToString("0.00") + "H:" + fOrbitalHydroPower.ToString("0.00") + " A:" + fOrbitalAtmoPower.ToString("0.00"), textPanelReport);
                    */
                //                current_state = next_state;

            }

            void ModeHover(UpdateType updateSource)
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

//                StatusLog("clear", textPanelReport);
                thisProgram.Echo("Hover Mode:" + iState);
//                StatusLog(OurName + ":" + moduleName + ":Hover", textPanelReport);
//                StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
                double elevation = 0;

                IMyShipController shipController = thisProgram.wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dLength = vNG.Length();
                double dGravity = dLength / 9.81;
                shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
//                StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);

                if (iState == 0)
                {
                    thisProgram.wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    Vector3D vNGN = vNG;
                    vNGN.Normalize();
                    thisProgram.wicoThrusters.GetBestThrusters(vNGN,
                        thrustForwardList, thrustBackwardList,
                        thrustDownList, thrustUpList,
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList
                        );

                    float fAtmoPower, fHydroPower, fIonPower;
                    thisProgram.wicoThrusters.CalculateHoverThrust(shipController,thrustOrbitalUpList, out fAtmoPower, out fHydroPower, out fIonPower);
                    if (fAtmoPower > 0) thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustatmo);
                    if (fHydroPower > 0) thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrusthydro);
                    if (fIonPower > 0) thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustion);
                    thisProgram.wicoControl.SetState(10);
                    //iState = 10;
                    //                powerDownThrusters(thrustAllList, thrustAll); // turns ON thrusters
                }

                bool bGearsLocked = thisProgram.wicoLandingGears.AnyGearIsLocked();
                bool bConnectorsConnected = thisProgram.wicoConnectors.AnyConnectorIsConnected();
                bool bConnectorIsLocked = thisProgram.wicoConnectors.AnyConnectorIsLocked();
                bool bGearsReadyToLock = thisProgram.wicoLandingGears.anyGearReadyToLock();

                thisProgram.wicoControl.WantMedium();

                /*
                Echo("Gears:");
                foreach(var gear in gearList)
                {
                    Echo(gear.CustomName);
                }
                */
                if (bGearsLocked)
                {
                    if (iState != 20)
                    {
                        // gears just became locked
                        thisProgram.Echo("Force thrusters Off!");
                        thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustAll, true);
                        //                    blockApplyAction(thrustAllList, "OnOff_Off");

//                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        {
                            thisProgram.wicoGasTanks.TanksStockpile(true);
                            thisProgram.wicoGasGens.GasGensEnable();
                            //                        blockApplyAction(tankList, "Stockpile_On");
                            //                        blockApplyAction(gasgenList, "OnOff_On");
                        }
                        thisProgram.wicoControl.SetState(20);// iState = 20;
                    }
//                    landingDoMode(1);
                }
                else
                {
                    if (iState != 10)
                    {
//                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        {
                            thisProgram.wicoThrusters.powerDownThrusters(); // turns ON all thusters
                            thisProgram.wicoGasTanks.TanksStockpile(false);
                            //                        blockApplyAction(tankList, "Stockpile_Off");
                            //				blockApplyAction(gasgenList,"OnOff_On");
                        }

                        thisProgram.wicoControl.SetState(10);// iState = 10;
                    }
//                    landingDoMode(0);
                }
                /*
                // add to delay time
                if (HoverCameraElapsedMs >= 0) HoverCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

                // check for delay
                if (HoverCameraElapsedMs > HoverCameraWaitMs || HoverCameraElapsedMs < 0) // it is time to scan..
                {
                    if (doCameraScan(cameraOrbitalLandingList, elevation * 2)) // scan down 2x current alt
                    {
                        HoverCameraElapsedMs = 0;
                        // we are able to do a scan
                        if (!lastDetectedInfo.IsEmpty())
                        { // we got something
                            double distance = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.HitPosition.Value);
                            //			if (distance < elevation)
                            { // try to land on found thing below us.
                                thisProgram.Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                                if (!bGearsLocked) StatusLog("Hovering above: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                            }
                        }
                    }
                }
                else thisProgram.Echo("Camera Scan delay");
                */

                if (bGearsLocked)
                {
//                    StatusLog("Landing Gear(s) LOCKED!", textPanelReport);
                    // we can turn off thrusters.. but that's about it..
                    // stay in 'hover' iMode
                }
                else if (bGearsReadyToLock)
                {
//                    StatusLog("Landing Gear(s) Ready to lock.", textPanelReport);
                }
                if (bConnectorsConnected)
                {
                    //prepareForSupported();
//                    StatusLog("Connector connected!\n   auto-prepare for launch", textPanelReport);
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_LAUNCHPREP);
                }
                else
                {
                    if (!bGearsLocked)
                    {
                        //			blockApplyAction(thrustAllList, "OnOff_On");
                        //			if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_On");
                    }
                    thisProgram.wicoConnectors.ConnectAnyConnectors(false, true);// "OnOff_On");
                }

                if (bConnectorIsLocked)
                {
 //                   StatusLog("Connector Locked!", textPanelReport);
                }

                if (bConnectorIsLocked || bGearsLocked)
                {
                    thisProgram.Echo("Stable");
//                    landingDoMode(1); // landing mode
                    thisProgram.wicoGyros.gyrosOff();
                }
                else
                {
                    /*
                    if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    {
                        gyrosOff();
                        StatusLog("Wico Gravity Alignment OFF", textPanelReport);
                    }
                    else
                    */
                    {
//                       StatusLog("Gravity Alignment Operational", textPanelReport);

                        /*
                        string sOrientation = "";
                        if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                            sOrientation = "rocket";
                        */
//                        bool bAimed = GyroMain(sOrbitalUpDirection);
                        bool bAimed = thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
                        if (bAimed) thisProgram.wicoControl.WantMedium(); //                            bWantMedium = true;
                        else
                            thisProgram.wicoControl.WantFast();//                    bWantFast = true;
                    }
                }

                //	StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

                // done in premodes	        batteryCheck(0, false);//,textPanelReport);
                /*
                //	if (bValidExtraInfo)
                {
                    if (batteryPercentage >= 0) StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                    if (oxyPercent >= 0)
                    {
                        StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                        //Echo("O:" + oxyPercent.ToString("000.0%"));
                    }
                    else thisProgram.Echo("No Oxygen Tanks");

                    if (hydroPercent >= 0)
                    {
                        StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                        //                   Echo("H:" + (hydroPercent*100).ToString("0.0") + "%");
                        if (hydroPercent < 0.20f)
                            StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);
                    }
                    else thisProgram.Echo("No Hydrogen Tanks");
                    if (batteryPercentage >= 0 && batteryPercentage < batterypctlow)
                        StatusLog(" WARNING: Low Battery Power", textPanelReport);

                    //		if (iOxygenTanks > 0) StatusLog("O2:" + progressBar(tanksFill(iTankOxygen)), textPanelReport);
                    //		if (iHydroTanks > 0) StatusLog("Hyd:" + progressBar(tanksFill(iTankHydro)), textPanelReport);
                }
                */
                if (dGravity <= 0)
                {
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                    thisProgram.wicoGyros.gyrosOff();
//                    StatusLog("clear", textPanelReport);
                }

            }
            void doModeLaunchprep(UpdateType updateSource)
            {
                //           IMyTextPanel textPanelReport = this.textPanelReport;

                int iMode = thisProgram.wicoControl.IMode;
                int  iState = thisProgram.wicoControl.IState;

                //                StatusLog("clear", textPanelReport);

                //                StatusLog(OurName + ":" + moduleName + ":Launch Prep", textPanelReport);
                //                StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);

                thisProgram.Echo(":LaunchPrep:" + iState);
                //           Echo("BatteryPercentage=" + batteryPercentage);
                //            Echo("batterypctlow=" + batterypctlow);
//                double elevation = 0;

                IMyShipController shipController = thisProgram.wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dLength = vNG.Length();
                double dGravity = dLength / 9.81;

                if (dGravity <= 0)
                {
                    if (thisProgram.wicoConnectors.AnyConnectorIsConnected()) thisProgram.wicoControl.SetMode(WicoControl.MODE_DOCKED);
                    else
                    {
                        thisProgram.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                        thisProgram.wicoGyros.gyrosOff();
//                        StatusLog("clear", textPanelReport);
                    }
                    return;
                }


                if (thisProgram.wicoLandingGears.AnyGearIsLocked())
                {
//                    StatusLog("Landing Gear(s) LOCKED!", textPanelReport);
                }
                if (thisProgram.wicoConnectors.AnyConnectorIsConnected())
                {
//                    StatusLog("Connector connected!\n   auto-prepare for launch", textPanelReport);
                }
                else
                {
                    if (!thisProgram.wicoLandingGears.AnyGearIsLocked())
                    {
//                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                            thisProgram.wicoGasTanks.TanksStockpile(false); // blockApplyAction(tankList, "Stockpile_Off");
                        thisProgram.wicoControl.SetMode(WicoControl.MODE_HOVER);
                    }
                    thisProgram.wicoConnectors.ConnectAnyConnectors(false, true);// "OnOff_On");
                }

                if (thisProgram.wicoConnectors.AnyConnectorIsLocked())
                {
//                    StatusLog("Connector Locked!", textPanelReport);
                }

                if (thisProgram.wicoConnectors.AnyConnectorIsLocked() || thisProgram.wicoLandingGears.AnyGearIsLocked())
                {
                    thisProgram.Echo("Stable");
                }
                else
                {
                    //prepareForSolo();
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_HOVER);
                    return;
                }

                if (thisProgram.wicoConnectors.AnyConnectorIsConnected())
                {
                    if (iState == 0)
                    {
                        thisProgram.wicoThrusters.powerDownThrusters(WicoThrusters.thrustAll, true);// blockApplyAction(thrustAllList, "OnOff_Off");
//                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                            thisProgram.wicoGasTanks.TanksStockpile(true);// blockApplyAction(tankList, "Stockpile_On");

                        thisProgram.wicoControl.SetState(1); // current_state = 1;
                    }
                    else if (iState == 1)
                    {
                        //			if ((craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0)
                        thisProgram.wicoControl.SetState(4);// current_state = 4; // skip battery checks
                                           //			else
                                           //			if (!batteryCheck(30, true))
                                           //				current_state = 2;
                    }
                    else if (iState == 2)
                    {
                        //			if (!batteryCheck(80, true))
                        thisProgram.wicoControl.SetState(3);// current_state = 3;
                    }
                    else if (iState == 3)
                    {
                        //			if (!batteryCheck(100, true))
                        thisProgram.wicoControl.SetState(1);// current_state = 1;
                    }
                }
                //	else             batteryCheck(0, true); //,textBlock);
                // TODO: same thing we do when docked in space.....
                //TODO: Check reactors and pull uranium
                //TODO: Check gas gens and pull ice

                //	StatusLog("C:" + progressBar(cargopcent), textBlock);

                /*
                if (batteryList.Count > 0)
                {
                    StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                    Echo("BatteryPercentage=" + batteryPercentage);
                }
                else StatusLog("Bat: <NONE>", textPanelReport);

                if (oxyPercent >= 0)
                {
                    StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                    //Echo("O:" + oxyPercent.ToString("000.0%"));
                }
                else Echo("No Oxygen Tanks");

                if (hydroPercent >= 0)
                {
                    StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                    if (hydroPercent < 0.20f)
                        StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);

                    Echo("H:" + hydroPercent.ToString("000.0%"));
                }
                else Echo("No Hydrogen Tanks");
                if (batteryList.Count > 0 && batteryPercentage < batterypctlow)
                    StatusLog(" WARNING: Low Battery Power", textPanelReport);

                */
            }


            // we have entered gravity well 
            // 0=initialize 
            // 10=dampeners on. aim towards target 
            // 11=aligned check 
            // 20=dampeners on. minor thrust fowards to align motion to target 
            // 21 hold alignment 
            // 22 hold alignment 
            // 23 hold alignment 
            // 30=dampeners off 
            // 40=free-falll. continue alignment. when in range for 180. start 180  
            // 60= check for 180 completed 
            // 61= perform align to gravity vector. ->70 if complete
            // 70=check for in retro-burn range of target in range; Dampeners on 
            // 90=wait for zero velocity 
            // 100 ... user control.. 
            // 200 orient top toward location 
            // 201 thrust toward location 
            // 202 'over' location 
            // 203 >1k 'over' location 
            // 204 >500 'over' location 
            // 205 >100 'over' location 
            // 206 >25 'over' location 
            // 207 final descent 


            //bool bOverTarget=false; 
            void doModeDescent(UpdateType updateSource)
            {
                // todo: handle parachutes.  
                // TODO: Check parachute orientation
                // TODO: handle 'stop XX meters above surface' (then hover, next nav command)
                // TODO: Arrived Target in gravity -> hover mode (should be in Nav, I guess)
                // TODO: Calculate best orientation through descent.
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                //               StatusLog("clear", textPanelReport);
                //               StatusLog(OurName + ":" + moduleName + ":Descent", textPanelReport);
                //               StatusLog("Gravity=" + dGravity.ToString(velocityFormat), textPanelReport);
                IMyShipController shipController = thisProgram.wicoBlockMaster.GetMainController();
                double velocityShip = shipController.GetShipSpeed();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dLength = vNG.Length();
                double dGravity = dLength / 9.81;
                thisProgram.Echo("Gravity=" + dGravity.ToString());
                double alt = 0;
                double halt = 0;

                Vector3D vTarget = new Vector3D(0, 0, 0);
                bool bValidTarget = false;

                MyShipMass myMass;
                myMass = shipController.CalculateShipMass();

                /*
                if (bValidOrbitalLaunch)
                {
                    // source position was connector
                    bValidTarget = true;
                    vTarget = vOrbitalLaunch;
                }
                else if (bValidOrbitalHome)
                {
                    // source position was hover
                    bValidTarget = true;
                    vTarget = vOrbitalHome;
                }
                else
                */
                {
                    bValidTarget = false;
                    //	if(dGravity>0) 
                    // assume we are pointed at planet... push forward until in gravity.


                    //	StatusLog(OurName+":"+moduleName+":Cannot Descend: No Waypoint present.",textPanelReport); 
                    //		setMode(MODE_IDLE); 
                    //		return; 
                }

                if (bValidTarget)
                {

                    alt = (shipController.GetPosition() - vTarget).Length();

 //                   StatusLog("Distance: " + alt.ToString("N0") + " Meters", textPanelReport);

                    if (dGravity > 0)
                    {
                        if (shipController is IMyShipController)
                        {
//                            Vector3D vNG = shipController.GetNaturalGravity();

                            //double Pitch,Yaw; 
                            Vector3D groundPosition;
                            groundPosition = shipController.GetPosition();
                            vNG.Normalize();
                            groundPosition += vNG * alt;

                            halt = (groundPosition - vTarget).Length();
 //                           StatusLog("Hor distance: " + halt.ToString("N0") + " Meters", textPanelReport);

                        }

                    }
                }
                else
                {
                    // doing a blind landing
                    thisProgram.Echo("Blind Landing");
                    //                    StatusLog("Blind Landing", textPanelReport);

                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out alt);
                    halt = 0;
//                    minAltRotate = 39000;
                }
                if (dGravity > 0)
                {
                    double elevation = 0;

                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
//                    StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
                    thisProgram.Echo("Elevation: " + elevation.ToString("N0") + " Meters");
                }

                thisProgram.Echo("Descent Mode:" + iState.ToString() );

                if (thisProgram.wicoLandingGears.anyGearReadyToLock())
                {
                    thisProgram.wicoLandingGears.GearsLock();
                }
                double progress = 0;
                if (velocityShip <= 0) progress = 0;
                else if (velocityShip > thisProgram.wicoControl.fMaxWorldMps) progress = 100;
                else progress = ((velocityShip - 0) / (thisProgram.wicoControl.fMaxWorldMps - 0) * 100.0f);

                /*
                string sProgress = progressBar(progress);
                StatusLog("V:" + sProgress, textPanelReport);

                if (batteryPercentage >= 0) StatusLog("B:" + progressBar(batteryPercentage), textPanelReport);
                if (oxyPercent >= 0) StatusLog("O:" + progressBar(oxyPercent * 100), textPanelReport);
                if (hydroPercent >= 0) StatusLog("H:" + progressBar(hydroPercent * 100), textPanelReport);
                */
                /*
                            string sOrbitalUpDirection = "";
                            if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                                sOrbitalUpDirection = "rocket";
                                */
                IMyShipController imsc = shipController as IMyShipController;
                if (imsc != null && imsc.DampenersOverride)
                {
 //                   StatusLog("DampenersOverride ON", textPanelReport);

                    thisProgram.Echo("DampenersOverride ON");
                }
                else
                {
//                   StatusLog("DampenersOverride OFF", textPanelReport);
                    thisProgram.Echo("DampenersOverride OFF");
                }

                if (thisProgram.wicoConnectors.AnyConnectorIsConnected())
                {
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_LAUNCHPREP);// setMode(MODE_IDLE);
                    return;
                }
                if (thisProgram.wicoConnectors.AnyConnectorIsLocked())
                {
                    thisProgram.wicoConnectors.ConnectAnyConnectors(true);
                    thisProgram.wicoLandingGears.GearsLock(true);
                    //                blockApplyAction(gearList, "Lock");
                }
                if(iState==0)
                {
                    thisProgram.wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    Vector3D vNGN = vNG;
                    Matrix or1;
                    if (vNG == Vector3D.Zero)
                    {
                        shipController.Orientation.GetMatrix(out or1);
                        vNGN = or1.Forward;
                    }

                    vNGN.Normalize();
                    thisProgram.wicoThrusters.GetBestThrusters(vNGN,
                        thrustForwardList, thrustBackwardList,
                        thrustDownList, thrustUpList,
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList
                        );
                    shipController.Orientation.GetMatrix(out or1);
                    vGroundOrientation = or1.Forward; // start out aiming at whatever the ship is aiming at..
                }
 //               calculateBestGravityThrust();
                /*
                if (thrustStage1UpList.Count < 1)
                {  // one-time init.
                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                    {
                        thrustStage1UpList = thrustForwardList;
                        thrustStage1DownList = thrustBackwardList;

                        cameraStage1LandingList = cameraBackwardList;
                    }
                    else
                    {
                        thrustStage1UpList = thrustUpList;
                        thrustStage1DownList = thrustDownList;

                        cameraStage1LandingList = cameraDownList;
                    }
                }
                */
                ////

                if (dGravity > 0)
                {
                    float fMPS = thisProgram.wicoControl.fMaxWorldMps;
                    if (velocityShip > fMPS) fMPS = (float)velocityShip;

                    retroStartAlt = (int)thisProgram.wicoThrusters.calculateStoppingDistance(shipController,thrustOrbitalUpList, fMPS, dGravity);
                    thisProgram.Echo("dGravity: " + dGravity.ToString("0.00"));

//                    startReverseAlt = Math.Max(retroStartAlt * 5, minAltRotate);
//                    thisProgram.Echo("calc retroStartAlt=" + retroStartAlt.ToString());

                    retroStartAlt += (int)((thisProgram.wicoBlockMaster.HeightInMeters() + 1)); // add calc point of height for altitude.. NOTE 'height' is not necessarily correct..
                                                                            //		retroStartAlt += (int)((shipDim.HeightInMeters()+1)/2) ; // add calc point of height for altitude.. NOTE 'height' is not necessarily correct..
                                                                            //		retroStartAlt += (int)fMaxMps; // one second of speed (1s timer delay)
                    thisProgram.Echo("adj retroStartAlt=" + retroStartAlt.ToString());
                }
                thisProgram.wicoControl.WantMedium();

                if (iState == 0)
                {
                    thisProgram.Echo("Init State");
                    //powerDownThrusters(thrustAllList,thrustAll,true);
                    if (dGravity > 0)
                    { // we are starting in gravity
 //                       if (alt < (startReverseAlt * 1.5))
                        { // just do a landing
                            thisProgram.wicoControl.SetState(40);// current_state = 40;
                        }
                        /*
                        else
                        {
                            current_state = 10;
                        }
                        */
                    }
                    else
                    {
                        if (imsc != null && imsc.DampenersOverride)
                            imsc.DampenersOverride = false;
                        //                        blockApplyAction(shipOrientationBlock, "DampenersOverride"); //DampenersOverride 
                        //                    ConnectAnyConnectors(false, "OnOff_On");
                        thisProgram.wicoConnectors.ConnectAnyConnectors(false, true);
                        if (!bValidTarget)
                        {
                            //                            StatusLog("No target landing waypoint set.", textPanelReport);
                            thisProgram.wicoControl.SetState(10);// current_state = 10;
                            //			setMode(MODE_IDLE); 
                        }
                        else
                            thisProgram.wicoControl.SetState(10);// current_state = 10;
                    }
                    thisProgram.wicoControl.WantFast();
                }
                //           thisProgram.Echo("After init check=" + current_state.ToString());
                if (iState == 10)
                {
                    thisProgram.Echo("Dampeners to on. Aim toward target");
                    //		bOverTarget=false; 
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, false);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (bValidTarget)
                    {
//                        GyroMain(sOrbitalUpDirection, vTarget, shipOrientationBlock);
                        //			startNavWaypoint(vTarget, true);
                        thisProgram.wicoControl.SetState(11);// current_state = 11;
                    }
                    else thisProgram.wicoControl.SetState(20);// current_state = 20;
                }
                if (iState == 11)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    //if (GyroMain(sOrbitalUpDirection, vTarget, shipOrientationBlock))
                        thisProgram.wicoControl.SetState(20);//current_state = 20;
                }
                if (iState == 20)
                {
                    // TODO: fix the connector landings
                    bValidTarget = false; // TEMP: just do surface landings..
                                          //                   bValidOrbitalHome = false;
                                          //                    bValidOrbitalLaunch = false;

                    /*
                    if (bValidTarget)
                        StatusLog("Move towards recorded landing location", textPanelReport);
                    else
                        StatusLog("Move towards surface for landing", textPanelReport);
                    */

                    if (imsc != null && !imsc.DampenersOverride)
                        imsc.DampenersOverride = true;
                    //                    blockApplyAction(shipOrientationBlock, "DampenersOverride");
                    //		current_state=30; 
                    if (dGravity <= 0 || velocityShip < (thisProgram.wicoControl.fMaxWorldMps* .8))
                        thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, 5);
                    else thisProgram.wicoThrusters.powerDownThrusters(thrustForwardList);
                    thisProgram.wicoThrusters.powerDownThrusters(thrustBackwardList, WicoThrusters.thrustAll, true);
                    if (velocityShip > 50 && dGravity > 0)
                        thisProgram.wicoControl.SetState(30);// current_state = 30;
                    return;
                }
                if (iState == 21)
                {
                    //                    StatusLog("Alignment", textPanelReport);
                    thisProgram.wicoControl.SetState(22);//current_state = 22;
                    return; // give at least one tick of dampeners 
                }
                if (iState == 22)
                {
//                    StatusLog("Alignment", textPanelReport);
                    thisProgram.wicoControl.SetState(23);//current_state = 23;
                    return; // give at least one tick of dampeners 
                }
                if (iState == 23)
                {
//                    StatusLog("Alignment", textPanelReport);
                    thisProgram.wicoControl.SetState(30);//current_state = 30;
                    return; // give at least one tick of dampeners 
                }
                if (iState == 30)
                {
                    thisProgram.wicoThrusters.powerDownThrusters(thrustBackwardList, WicoThrusters.thrustAll, true);

                    if (imsc != null && imsc.DampenersOverride)
                        imsc.DampenersOverride = false;
                    //                    blockApplyAction(shipOrientationBlock, "DampenersOverride");
                    thisProgram.wicoControl.SetState(40);//current_state = 40;
                }
                if (iState == 40)
                {
//                    StatusLog("Free Fall", textPanelReport);
                    thisProgram.Echo("Free Fall");
                    if (dGravity > 0)
                    {
                        if(CheckAttitudeChange())
                            thisProgram.wicoControl.SetState(60);//current_state = 60;

                    }
                    if (imsc != null && imsc.DampenersOverride)
                        imsc.DampenersOverride = false;
                    //                    blockApplyAction(shipOrientationBlock, "DampenersOverride");

//                    if (alt < startReverseAlt)
                    {
//                        thisProgram.wicoControl.SetState(60);//current_state = 60;
                    }
                    /*
                    else
                    {
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList);
 //                       StatusLog("Waiting for reverse altitude: " + startReverseAlt.ToString("N0") + " meters", textPanelReport);

                        if (alt > 44000 && alt < 45000)
                            thisProgram.wicoControl.SetState(10);//current_state = 10; // re-align 
                        else if (alt > 34000 && alt < 35000)
                            thisProgram.wicoControl.SetState(10);//current_state = 10; // re-align 
                        else if (alt > 24000 && alt < 25000)
                            thisProgram.wicoControl.SetState(10);//current_state = 10; // re-align 
                        else if (alt > 14000 && alt < 15000)
                            thisProgram.wicoControl.SetState(10);//current_state = 10; // re-align 
                    }
                    */
                }
                if (iState == 60)
                {
                    if (dGravity <= 0)
                    {
                        thisProgram.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);//setMode(MODE_IDLE);
                        return;
                    }
                    CheckAttitudeChange();
                        //		string sStatus=navStatus.CustomName; 
                        //                    StatusLog("Waiting for alignment with gravity", textPanelReport);

                    if (imsc != null && imsc.DampenersOverride)
                        imsc.DampenersOverride = false;

                    thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
                    thisProgram.wicoControl.WantFast();//bWantFast = true;
                    thisProgram.wicoControl.SetState(61);//current_state = 61;
                    return;
                }

                if (iState == 61)
                {  // we are rotating ship to gravity..
//                    CheckAttitudeChange();
                    if (thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController) || alt < retroStartAlt)
                    {
                        thisProgram.wicoControl.SetState(70);// current_state = 70;
                    }
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                if (iState == 70)
                {
                    //                   StatusLog("Waiting for range for retro-thrust:" + retroStartAlt.ToString("N0") + " meters", textPanelReport);
                    if (CheckAttitudeChange())
                    {
                        thisProgram.wicoControl.SetState(50);
                        thisProgram.Echo("attitude change");
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                    }

                    //                    bool bAligned = GyroMain(sOrbitalUpDirection);
                    bool bAligned = thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
                    if (bAligned)
                    {
                        if (imsc != null && imsc.DampenersOverride)
                            imsc.DampenersOverride = false;

                        thisProgram.wicoControl.WantMedium();// bWantMedium = true;
                        double scandistance = alt;
                        if (scandistance > retroStartAlt)
                            scandistance = retroStartAlt;
/*
                        if (doCameraScan(cameraOrbitalLandingList, scandistance * 2)) // scan down 2x current alt
                        {
                            // we are able to do a scan
                            if (!lastDetectedInfo.IsEmpty())
                            { // we got something
                                double distance = Vector3D.Distance(shipOrientationBlock.GetPosition(), lastDetectedInfo.HitPosition.Value);
                                if (distance < alt)
                                { // try to land on found thing below us.
                                    thisProgram.Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                                    StatusLog("Landing on: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                                    alt = distance;
                                }
                            }
                        }
                        */
                        thisProgram.wicoThrusters.powerDownThrusters();
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustAll, true);
                    }
                    else
                    {
                        if (imsc != null && !imsc.DampenersOverride)
                            imsc.DampenersOverride = true;
                        thisProgram.Echo("NOT aligned");
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                        thisProgram.wicoThrusters.powerDownThrusters();
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustAll, true);
                    }

                    if (alt < (retroStartAlt + thisProgram.wicoControl.fMaxWorldMps * 2)) thisProgram.wicoControl.WantFast();// bWantFast = true;

                    if ((alt) < retroStartAlt)
                    {
                        if (imsc != null && !imsc.DampenersOverride)
                        {
                            imsc.DampenersOverride = true;
                            thisProgram.wicoThrusters.powerDownThrusters();
                        }
                        //                        blockApplyAction(shipOrientationBlock, "DampenersOverride");
                        thisProgram.wicoControl.SetState(90);//current_state = 90;
                    }
                }
                /*
                double roll = 0;
                string s;
                if (bValidTarget)
                {
                    roll = CalculateRoll(vTarget, shipOrientationBlock);
                    s = "Roll=" + roll.ToString("0.00");
                    thisProgram.Echo(s);
                    StatusLog(s, textPanelReport);
                }
                */
                if (iState == 90)
                {
 //                   StatusLog("RETRO! Waiting for ship to slow", textPanelReport);
                    if (velocityShip < 1)
                    {
 //                       thisProgram.wicoControl.SetState(200);//current_state = 200;
                        thisProgram.wicoControl.SetState(100);
                    }
                    bool bAimed = thisProgram.wicoGyros.AlignGyros(vGroundOrientation, vNG, shipController);
//                    if (GyroMain(sOrbitalUpDirection))
                    if(bAimed)
                    {
                        thisProgram.wicoControl.WantMedium(); // bWantMedium = true;
                    }
                    else thisProgram.wicoControl.WantFast(); // bWantFast = true;
                }

                if (iState == 100)
                {
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                }

            }

            void increasePower(double dGravity, double alt)
            {
                double dAtmoEff = thisProgram.wicoThrusters.AtmoEffectiveness();
                /*
                Echo("atmoeff=" + dAtmoEff.ToString());
                Echo("hydroThrustCount=" + hydroThrustCount.ToString());
                Echo("fHydroPower=" + fHydroPower.ToString());
                Echo("fAtmoPower=" + fAtmoPower.ToString());
                Echo("fIonPower=" + fIonPower.ToString());
                */
                if (dGravity > .5 && (!bHasAtmo || dAtmoEff > 0.10))
                //                if (dGravity > .5 && alt < dAtmoCrossOver)
                {
                    if (fOrbitalAtmoPower < 100 && bHasAtmo)
                        fOrbitalAtmoPower += 5;
                    else if (fOrbitalHydroPower == 0 && fOrbitalIonPower > 0)
                    { // we are using ion already...
                        if (fOrbitalIonPower < 100 && bHasIon)
                            fOrbitalIonPower += 5;
                        else
                            fOrbitalHydroPower += 5;
                    }
                    else if (fOrbitalIonPower < 100 && bHasIon)
                        fOrbitalIonPower += 5;
                    else if (fOrbitalHydroPower < 100 && bHasHydro)
                    {
                        // fAtmoPower=100;
                        fOrbitalHydroPower += 5;
                    }
                    else // no power left to give, captain!
                    {
//                        StatusLog("Not Enough Thrust!", textPanelReport);
//                        Echo("Not Enough Thrust!");
                    }
                }
                else if (dGravity > .5 || dAtmoEff < 0.10)
                {
                    if (fOrbitalIonPower < fOrbitalAtmoPower && bHasAtmo && bHasIon)
                    {
                        float f = fOrbitalIonPower;
                        fOrbitalIonPower = fOrbitalAtmoPower;
                        fOrbitalAtmoPower = f;
                    }
                    if (fOrbitalIonPower < 100 && bHasIon)
                        fOrbitalIonPower += 10;
                    else if (fOrbitalHydroPower < 100 && bHasHydro)
                    {
                        fOrbitalHydroPower += 5;
                    }
                    else if (dAtmoEff > 0.10 && fOrbitalAtmoPower < 100 && bHasAtmo)
                        fOrbitalAtmoPower += 10;
                    else if (dAtmoEff > 0.10 && bHasAtmo)
                        fOrbitalAtmoPower -= 5; // we may be sucking power from ion
                    else // no power left to give, captain!
                    {
 //                       StatusLog("Not Enough Thrust!", textPanelReport);
//                        Echo("Not Enough Thrust!");
                    }
                }
                else if (dGravity > .01)
                {
                    if (fOrbitalIonPower < 100 && bHasIon)
                        fOrbitalIonPower += 15;
                    else if (fOrbitalHydroPower < 100 && bHasHydro)
                    {
                        fOrbitalHydroPower += 5;
                    }
                    else if (dAtmoEff > 0.10 && fOrbitalAtmoPower < 100 && bHasAtmo)
                        fOrbitalAtmoPower += 10;
                    else // no power left to give, captain!
                    {
//                        StatusLog("Not Enough Thrust!", textPanelReport);
//                        Echo("Not Enough Thrust!");
                    }

                }

                if (fOrbitalIonPower > 100) fOrbitalIonPower = 100;
                if (fOrbitalAtmoPower > 100) fOrbitalAtmoPower = 100;
                if (fOrbitalAtmoPower < 0) fOrbitalAtmoPower = 0;
                if (fOrbitalHydroPower > 100) fOrbitalHydroPower = 100;

            }

            void decreasePower(double dGravity, double alt)
            {
                if (dGravity > .85 && thisProgram.wicoThrusters.AtmoEffectiveness() > 0.10)
                {
                    if (fOrbitalHydroPower > 0)
                    {
                        fOrbitalHydroPower -= 5;
                    }
                    else if (fOrbitalIonPower > 0)
                        fOrbitalIonPower -= 5;
                    else if (fOrbitalAtmoPower > 10)
                        fOrbitalAtmoPower -= 5;
                }
                else if (dGravity > .3)
                {
                    if (fOrbitalAtmoPower > 0)
                        fOrbitalAtmoPower -= 10;
                    else if (fOrbitalHydroPower > 0)
                    {
                        fOrbitalHydroPower -= 5;
                    }
                    else if (fOrbitalIonPower > 10)
                        fOrbitalIonPower -= 5;

                }
                else if (dGravity > .01)
                {
                    if (fOrbitalAtmoPower > 0)
                        fOrbitalAtmoPower -= 5;
                    else if (fOrbitalHydroPower > 0)
                    {
                        fOrbitalHydroPower -= 5;
                    }
                    else if (fOrbitalIonPower > 10)
                        fOrbitalIonPower -= 5;
                }

                if (fOrbitalIonPower < 0) fOrbitalIonPower = 0;
                if (fOrbitalAtmoPower < 0) fOrbitalAtmoPower = 0;
                if (fOrbitalHydroPower < 0) fOrbitalHydroPower = 0;

            }

            bool CheckAttitudeChange()
            {
                List<IMyTerminalBlock> oldup = thrustOrbitalUpList;
                List<IMyTerminalBlock> olddown = thrustOrbitalDownList;

                thisProgram.wicoThrusters.GetMaxScaledThrusters(
                    thrustForwardList, thrustBackwardList,
                    thrustDownList, thrustUpList,
                    thrustLeftList, thrustRightList,
                    out thrustOrbitalUpList, out thrustOrbitalDownList,
                    orbitalAtmoMult, orbitalIonMult, orbitalHydroMult
                    );
                if (thrustOrbitalUpList != oldup) // something changed
                {
                    thisProgram.Echo("Change in attitude needed");
                    thisProgram.wicoThrusters.powerDownThrusters(olddown);
                    thisProgram.wicoThrusters.powerDownThrusters(oldup);
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);
                    Matrix or1;
                    thrustOrbitalUpList[0].Orientation.GetMatrix(out or1);
                    vGroundOrientation = or1.Forward;
                    return true;
                }
                return false;


            }

        }
    }
}
