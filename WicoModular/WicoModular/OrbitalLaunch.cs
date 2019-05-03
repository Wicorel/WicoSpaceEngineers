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
    
        class OrbitalLaunch
        {
            Program thisProgram;
            float orbitalAtmoMult = 5;
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

            string sOrbitalUpDirection = "";
            List<IMyTerminalBlock> thrustOrbitalUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustOrbitalDownList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> cameraOrbitalLandingList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();

            public OrbitalLaunch(Program program)
            {
                thisProgram = program;
                thisProgram.UpdateUpdateHandlers.Add(doModeOrbitalLaunch);
                thisProgram.wicoControl.AddControlChangeHandler(modeChangeHandler);
                thisProgram.UpdateTriggerHandlers.Add(ProcessTrigger);
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

            public void modeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if(fromMode== WicoControl.MODE_ORBITALLAUNCH)
                {
                }
                if (toMode==WicoControl.MODE_ORBITALLAUNCH)
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

            public void doModeOrbitalLaunch(UpdateType updateSource)
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

                bool bAligned = false;
                if (iState==0 || thrustOrbitalUpList.Count < 1)
                {
                    thisProgram.wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    calculateBestGravityThrust();
                    bHasAtmo = false;
                    bHasHydro = false;
                    bHasIon = false;
                    if(thisProgram.wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustion) != null)
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

                        if(bHasIon)
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
                double alt = elevation;
                if (iState == 10)
                {
                    thisProgram.wicoControl.WantOnce();
                    thisProgram.wicoThrusters.CalculateHoverThrust(shipController,thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                    thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);
                    thisProgram.wicoControl.SetState(20);
 //                   current_state = 20;
                    return;
                }
                double velocityShip = shipController.GetShipSpeed();
                double deltaV = velocityShip - dLastVelocityShip;
                double expectedV = deltaV * 5 + velocityShip;

                Vector3D vNG = shipController.GetNaturalGravity();
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

                    string sOrientation = "up";
//                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                        sOrientation = "rocket";

                    bAligned = thisProgram.wicoGyros.AlignGyros(sOrientation,vNG,shipController);
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
                        /*
                        if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                            StatusLog("Wico Gravity Alignment OFF", textPanelReport);
                        else
                        */
                        {

//                            StatusLog("Gravity Alignment Operational", textPanelReport);
                            /*
                            string sOrientation = "";
                            if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                                sOrientation = "rocket";

                            if (!GyroMain(sOrientation))
                                bWantFast = true;
                                */
//                            Echo("Align=" + sOrbitalUpDirection);
                            bAligned = thisProgram.wicoGyros.AlignGyros(sOrbitalUpDirection, vNG, shipController);
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
                    if (calculateBestGravityThrust(true))
                    {
                        // reset the old thrusters
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList);
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList);

                        calculateBestGravityThrust(); // do the change
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

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
                    if (calculateBestGravityThrust(true))
                    {
                        // reset the old thrusters
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList);
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalUpList);

                        calculateBestGravityThrust(); // do the change
                        thisProgram.wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

                        thisProgram.wicoControl.SetState(45);// current_state = 45;
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                        return;
                    }

//                    Log("Maintain max speed");
                    thisProgram.Echo("Maintain max speed");
  //                  if (bOrbitalLaunchDebug) StatusLog("Expectedv=" + expectedV.ToString("0.00") + " max=" + thisProgram.wicoControl.fMaxWorldMps.ToString("0.00"), textPanelReport);
//                    if (bOrbitalLaunchDebug)
//                        thisProgram.Echo("Expectedv=" + expectedV.ToString("0.00") + " max=" + thisProgram.wicoControl.fMaxWorldMps.ToString("0.00"));
                    double dMin = (thisProgram.wicoControl.fMaxWorldMps - thisProgram.wicoControl.fMaxWorldMps * .01); // within n% of max mps
//                    thisProgram.Echo("dMin=" + dMin.ToString("0.00"));
                    if (expectedV > dMin)
                    // if(velocityShip>(fMaxMps-5))
                    {
                        bool bThrustOK = thisProgram.wicoThrusters.CalculateHoverThrust(shipController,thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
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
                    bAligned = thisProgram.wicoGyros.AlignGyros(sOrbitalUpDirection, vNG, shipController); //GyroMain(sOrbitalUpDirection);

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
            /// <summary>
            /// Choose best thrusters and orientation to use in gravity to launch
            /// WANT: base on current power and hydrogen (and ice) availability 
            /// </summary>
            /// <returns>true if the 'best' has changed. Modifies thrustOrbitalUpList, thrustObritalDownList, and sOrbitalDirection</returns>
            ///
            bool calculateBestGravityThrust(bool PerformChangeOver = true)
            {
                double upThrust = thisProgram.wicoThrusters.CalculateTotalEffectiveThrust(thrustUpList, orbitalAtmoMult, orbitalIonMult, orbitalHydroMult);
                double fwThrust = thisProgram.wicoThrusters.CalculateTotalEffectiveThrust(thrustForwardList, orbitalAtmoMult, orbitalIonMult, orbitalHydroMult);
                bool bChanged = false;

                if (fwThrust > upThrust)
                {
                    if (sOrbitalUpDirection != "rocket")
                    {
                        if (PerformChangeOver)
                        {
                            thrustOrbitalUpList = thrustForwardList;
                            thrustOrbitalDownList = thrustBackwardList;
                            sOrbitalUpDirection = "rocket";
                            cameraOrbitalLandingList = thisProgram.wicoCameras.GetBackwardCameras();// cameraBackwardList;
                        }
                        bChanged = true;
                    }
                }
                else
                {
                    if (sOrbitalUpDirection != "down")
                    {
                        if (PerformChangeOver)
                        {
                            thrustOrbitalUpList = thrustUpList;
                            thrustOrbitalDownList = thrustDownList;
                            sOrbitalUpDirection = "down";
                            cameraOrbitalLandingList = thisProgram.wicoCameras.GetDownwardCameras(); //cameraDownList;
                        }
                        bChanged = true;
                    }
                }
                return bChanged;

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

        }
    }
}
