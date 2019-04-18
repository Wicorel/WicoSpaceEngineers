using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //        double dAtmoCrossOver = 7000;


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

        double dLastVelocityShip = -1;
        //        double atmoEffectiveness = 1;

        float fOrbitalAtmoPower = 0;
        float fOrbitalHydroPower = 0;
        float fOrbitalIonPower = 0;

        bool bOrbitalLaunchDebug = false;

        void doModeOrbitalLaunch()
        {
            int next_state = current_state;
            bool bAligned = false;

 //          IMyTextPanel textPanelReport= textPanelReport;

            StatusLog("clear", textPanelReport);
            Log("clear");

            StatusLog(OurName + ":" + moduleName + ":Oribital Launch", textPanelReport);
            Log(OurName + ":" + moduleName + ":Oribital Launch");

            StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
            StatusLog(velocityShip.ToString(velocityFormat) + " m/s", textPanelReport);
            Echo("Orbital Launch. State=" + current_state.ToString());
            if (thrustOrbitalUpList.Count < 1)
            {
                calculateBestGravityThrust();
            }

            if (current_state == 0)
            {
                calculateBestGravityThrust();
                //		dtStartShip = DateTime.Now;
                bWantFast = true;
                dLastVelocityShip = 0;
                if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(false);
                GasGensEnable(true);

                if (AnyConnectorIsConnected() || AnyConnectorIsLocked() || anyGearIsLocked())
                {
                    // launch from connected
                    vOrbitalLaunch = shipOrientationBlock.GetPosition();
                    bValidOrbitalLaunch = true;
                    bValidOrbitalHome = false; // forget any 'home' waypoint.

                    ConnectAnyConnectors(false, false);// "OnOff_Off");
                    //			blockApplyAction(gearList, "OnOff_Off"); // in case autolock is set.
                    gearsLock(false);// blockApplyAction(gearList, "Unlock");
                    current_state = 150;
                    return;
                }
                else
                {
                    // launch from hover mode
                    bValidOrbitalLaunch = false;
                    vOrbitalHome = shipOrientationBlock.GetPosition();
                    bValidOrbitalHome = true;

                    // assume we are hovering; do FULL POWER launch.
                    fOrbitalAtmoPower = 0;
                    fOrbitalHydroPower = 0;
                    fOrbitalIonPower = 0;

                    if (ionThrustCount > 0) fOrbitalIonPower = 75;
                    if (hydroThrustCount > 0)
                    { // only use Hydro power if they are already turned on
                        for (int i = 0; i < thrustOrbitalUpList.Count; i++)
                        {
                            if (thrusterType(thrustOrbitalUpList[i]) == thrusthydro)
                                if (thrustOrbitalUpList[i].IsWorking)
                                {
                                    fOrbitalHydroPower = 100;
                                    break;
                                }
                        }
                    }
                    if (atmoThrustCount > 0) fOrbitalAtmoPower = 100;

                    powerDownThrusters(thrustOrbitalDownList, thrustAll, true);

                    current_state = 30;
                    return;
                }
            }
            if (current_state == 150)
            {
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + current_state.ToString(), textLongStatus, true);
                if (AnyConnectorIsConnected() || AnyConnectorIsLocked() || anyGearIsLocked())
                {
                    StatusLog("Awaiting release", textPanelReport);
                    Log("Awaiting release");
                }
                else
                {
                    StatusLog(DateTime.Now.ToString() + " " + OurName + ":Saved Position", textLongStatus, true);

                    // we launched from connected. Save position
                    vOrbitalLaunch = shipOrientationBlock.GetPosition();
                    bValidOrbitalLaunch = true;
                    bValidOrbitalHome = false; // forget any 'home' waypoint.

                    next_state = 10;
                }

                current_state = next_state;
            }
            Vector3D vTarget = new Vector3D(0, 0, 0);
            bool bValidTarget = false;
            if (bValidOrbitalLaunch)
            {
                bValidTarget = true;
                vTarget = vOrbitalLaunch;
            }
            else if (bValidOrbitalHome)
            {
                bValidTarget = true;
                vTarget = vOrbitalHome;
            }

            double elevation = 0;

            ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
            StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
            double alt = elevation;
            Echo("Alt=" + alt.ToString("0.00"));

            if (bValidTarget)
            {
 //               alt = (vCurrentPos - vTarget).Length();
 //               StatusLog("Distance: " + alt.ToString("N0") + " Meters", textPanelReport);
            }

            if (current_state == 10)
            {
                bWantFast = true;
                calculateHoverThrust(thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                powerDownThrusters(thrustOrbitalDownList, thrustAll, true);
                current_state = 20;
                return;
            }

            double deltaV = velocityShip - dLastVelocityShip;
            double expectedV = deltaV * 5 + velocityShip;

            if (current_state == 20)
            { // trying to move
                StatusLog("Attempting Lift-off", textPanelReport);
                Log("Attempting Lift-off");
                // NOTE: need to NOT turn off atmo if we get all the way into using ions for this state.. and others?
                if (velocityShip < 3f)
                // if(velocityShip<1f)
                {
                    increasePower(dGravity, alt);
                    increasePower(dGravity, alt);
                }
                else
                {
                    next_state = 30; // we have started to lift off.
                    dLastVelocityShip = 0;
                }
                
                string sOrientation = "up";
                if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                    sOrientation = "rocket";

                bAligned = GyroMain(sOrientation);
                if (!bAligned)
                    bWantFast = true;
            }
            else
            {
                bWantMedium = true;
                if (alt > 5)
                {
                    /*
                    if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                        StatusLog("Wico Gravity Alignment OFF", textPanelReport);
                    else
                    */
                    {

                        StatusLog("Gravity Alignment Operational", textPanelReport);
                        /*
                        string sOrientation = "";
                        if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                            sOrientation = "rocket";

                        if (!GyroMain(sOrientation))
                            bWantFast = true;
                            */
                        Echo("Align=" + sOrbitalUpDirection);
                        bAligned = GyroMain(sOrbitalUpDirection);
                        if (!bAligned)
                            bWantFast = true;
                    }
                }
            }

            if (current_state == 30)
            { // Retract landing config
                StatusLog("Movement started. Retracting Landing config ", textPanelReport);
                Log("Movement started. Retracting Landing config ");
                next_state = 31;
            }
            if (current_state == 31)
            { // accelerate to max speed
                if (calculateBestGravityThrust(true))
                {
                    // reset the old thrusters
                    powerDownThrusters(thrustOrbitalDownList);
                    powerDownThrusters(thrustOrbitalUpList);

                    calculateBestGravityThrust(); // do the change
                    powerDownThrusters(thrustOrbitalDownList, thrustAll, true);

                    current_state = 35;
                    bWantFast = true;
                    return;
                }
                powerDownThrusters(thrustOrbitalDownList, thrustAll, true);

                StatusLog("Accelerating to max speed (" + fMaxWorldMps.ToString("0") + ")", textPanelReport);
                Log("Accelerating to max speed");
                Echo("Accelerating to max speed");
                if (dLastVelocityShip < velocityShip)
                { // we are Accelerating
                    if (bOrbitalLaunchDebug) Echo("Accelerating");
                    if (expectedV < (fMaxWorldMps / 2))
                    // if(velocityShip<(fMaxMps/2))
                    {
                        decreasePower(dGravity, alt); // take away from lowest provider
                        increasePower(dGravity, alt);// add it back
                    }
                    if (expectedV < (fMaxWorldMps / 5)) // add even more.
                        increasePower(dGravity, alt);

                    if (velocityShip > (fMaxWorldMps - 5))
                        next_state = 40;
                }
                else
                {
                    increasePower(dGravity, alt);//
                    increasePower(dGravity, alt);// and add some more
                }
            }
            if(current_state==35)
            {
                // re-align and then resume
                powerDownThrusters(thrustAllList, thrustAll);
//                bAligned = GyroMain(sOrbitalUpDirection);
                if (bAligned)
                    next_state = 31;
            }
            if (current_state == 40)
            { // maintain max speed
                if (calculateBestGravityThrust(true))
                {
                    // reset the old thrusters
                    powerDownThrusters(thrustOrbitalDownList);
                    powerDownThrusters(thrustOrbitalUpList);

                    calculateBestGravityThrust(); // do the change
                    powerDownThrusters(thrustOrbitalDownList, thrustAll, true);

                    current_state = 45;
                    bWantFast = true;
                    return;
                }

                Log("Maintain max speed");
                Echo("Maintain max speed");
                if (bOrbitalLaunchDebug) StatusLog("Expectedv=" + expectedV.ToString("0.00") + " max=" + fMaxWorldMps.ToString("0.00"), textPanelReport);
                if (bOrbitalLaunchDebug) Echo("Expectedv=" + expectedV.ToString("0.00") + " max=" + fMaxWorldMps.ToString("0.00"));
                double dMin = (fMaxWorldMps - fMaxWorldMps * .01); // within n% of max mps
                if (expectedV > dMin)
                // if(velocityShip>(fMaxMps-5))
                {
                    bool bThrustOK=calculateHoverThrust(thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                    if (bOrbitalLaunchDebug) Echo("hover thrust:" + fOrbitalAtmoPower.ToString("0.00") + ":" + fOrbitalHydroPower.ToString("0.00") + ":" + fOrbitalIonPower.ToString("0.00"));
                    if (bOrbitalLaunchDebug) StatusLog("hover thrust:" + fOrbitalAtmoPower.ToString("0.00") + ":" + fOrbitalHydroPower.ToString("0.00") + ":" + fOrbitalIonPower.ToString("0.00"), textPanelReport);

                }
                else if (expectedV < (fMaxWorldMps - 10))
                {
                    decreasePower(dGravity, alt); // take away from lowest provider
                    increasePower(dGravity, alt);// add it back
                    increasePower(dGravity, alt);// and add some more
                }
                if (velocityShip < (fMaxWorldMps / 2))
                    next_state = 20;

                ConnectAnyConnectors(false, true);// "OnOff_On");
                blocksOnOff(gearList, true);
                //                blockApplyAction(gearList, "OnOff_On");
            }
            if (current_state == 45)
            {
                // re-align and then resume
                powerDownThrusters(thrustAllList, thrustAll);
                bAligned = GyroMain(sOrbitalUpDirection);

                if (bAligned)
                    next_state = 40;
            }
            dLastVelocityShip = velocityShip;

            StatusLog("", textPanelReport);

            //	if (bValidExtraInfo)
            StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

            //	batteryCheck(0, false);//,textPanelReport);
            //	if (bValidExtraInfo)
            if (batteryList.Count > 0)
            {
                StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
//                Echo("BatteryPercentage=" + batteryPercentage);
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
//                Echo("H:" + (hydroPercent*100).ToString("0.0") + "%");
                if (hydroPercent < 0.20f)
                {
                    StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);
                    Log(" WARNING: Low Hydrogen Supplies");
                }
            }
            else Echo("No Hydrogen Tanks");
            if (batteryList.Count > 0 && batteryPercentage < batterypctlow)
            {
                StatusLog(" WARNING: Low Battery Power", textPanelReport);
                Log(" WARNING: Low Battery Power");
            }

            StatusLog("", textPanelReport);
            if (dGravity < 0.01)
            {
                powerDownThrusters(thrustAllList);
                gyrosOff();
                //                startNavCommand("!;V");
                setMode(MODE_NAVNEXTTARGET);
                StatusLog("clear", textPanelReport);
                Log("clear");
                return; //GPS:Wicorel #5:14690.86:106127.43:10724.23:
            }

            int iPowered = 0;

            Echo("IonPower=" + fOrbitalIonPower.ToString("0.00"));
            if (fOrbitalIonPower > 0.01)
            {
                powerDownThrusters(thrustAllList, thrustatmo, true);
                powerDownThrusters(thrustAllList, thrustion);
                iPowered = powerUpThrusters(thrustOrbitalUpList, fOrbitalIonPower , thrustion);
                //Echo("Powered "+ iPowered.ToString()+ " Ion Thrusters");
            }
            else
            {
                powerDownThrusters(thrustAllList, thrustion, true);
                powerDownThrusters(thrustOrbitalUpList, thrustion);
            }

            Echo("HydroPower=" + fOrbitalHydroPower.ToString("0.00"));
            if (fOrbitalHydroPower > 0.01)
            {
//                Echo("Powering Hydro to " + fHydroPower.ToString());
                powerUpThrusters(thrustOrbitalUpList, fOrbitalHydroPower , thrusthydro);
            }
            else
            { // important not to let them provide dampener power..
                powerDownThrusters(thrustOrbitalDownList, thrusthydro, true);
                powerDownThrusters(thrustOrbitalUpList, thrusthydro, true);
            }
            Echo("AtmoPower=" + fOrbitalAtmoPower.ToString("0.00"));
            if (fOrbitalAtmoPower > 0.01)
            {
                powerUpThrusters(thrustOrbitalUpList, fOrbitalAtmoPower , thrustatmo);
            }
            else
            {
                closeDoors(outterairlockDoorList);

                // iPowered=powerDownThrusters(thrustStage1UpList,thrustatmo,true);
                iPowered = powerDownThrusters(thrustAllList, thrustatmo, true);
                //Echo("Powered DOWN "+ iPowered.ToString()+ " Atmo Thrusters");
            }

            {
                powerDownThrusters(thrustOrbitalDownList, thrustAll, true);
            }

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
                else if (fOrbitalHydroPower<10)   StatusLog("HYD\n/10:" + progressBar(fOrbitalHydroPower*10), textPanelReport);
                else                        StatusLog("HYD:" + progressBar(fOrbitalHydroPower), textPanelReport);
            }
            else StatusLog("HYD: None", textPanelReport);
            if (atmoThrustCount > 0)
            {
                if (fOrbitalAtmoPower < .01) StatusLog("ATM: Off", textPanelReport);
                else if (fOrbitalAtmoPower<10)
                StatusLog("ATM\n/10:" + progressBar(fOrbitalAtmoPower*10), textPanelReport);
                else
                StatusLog("ATM:" + progressBar(fOrbitalAtmoPower), textPanelReport);
            }
            else StatusLog("ATM: None", textPanelReport);
            if (bOrbitalLaunchDebug)
                StatusLog("I:" + fOrbitalIonPower.ToString("0.00") + "H:" + fOrbitalHydroPower.ToString("0.00") + " A:" + fOrbitalAtmoPower.ToString("0.00"), textPanelReport);
            current_state = next_state;

        }

        void increasePower(double dGravity, double alt)
        {
            double dAtmoEff = AtmoEffectiveness();
            /*
            Echo("atmoeff=" + dAtmoEff.ToString());
            Echo("hydroThrustCount=" + hydroThrustCount.ToString());
            Echo("fHydroPower=" + fHydroPower.ToString());
            Echo("fAtmoPower=" + fAtmoPower.ToString());
            Echo("fIonPower=" + fIonPower.ToString());
            */
            if (dGravity > .5 && (atmoThrustCount ==0 || dAtmoEff > 0.10))
            //                if (dGravity > .5 && alt < dAtmoCrossOver)
            {
                if (fOrbitalAtmoPower < 100 && atmoThrustCount > 0)
                    fOrbitalAtmoPower += 5;
                else if (fOrbitalHydroPower == 0 && fOrbitalIonPower > 0)
                { // we are using ion already...
                    if (fOrbitalIonPower < 100 && ionThrustCount > 0)
                        fOrbitalIonPower += 5;
                    else
                        fOrbitalHydroPower += 5;
                }
                else if (fOrbitalIonPower < 100 && ionThrustCount > 0)
                    fOrbitalIonPower += 5;
                else if (fOrbitalHydroPower < 100 && hydroThrustCount > 0)
                {
                    // fAtmoPower=100;
                    fOrbitalHydroPower += 5;
                }
                else // no power left to give, captain!
                {
                    StatusLog("Not Enough Thrust!", textPanelReport);
                    Echo("Not Enough Thrust!");
                }
            }
            else if (dGravity > .5 || dAtmoEff < 0.10)
            {
                if (fOrbitalIonPower < fOrbitalAtmoPower && atmoThrustCount > 0 && ionThrustCount > 0)
                {
                    float f = fOrbitalIonPower;
                    fOrbitalIonPower = fOrbitalAtmoPower;
                    fOrbitalAtmoPower = f;
                }
                if (fOrbitalIonPower < 100 && ionThrustCount > 0)
                    fOrbitalIonPower += 10;
                else if (fOrbitalHydroPower < 100 && hydroThrustCount > 0)
                {
                    fOrbitalHydroPower += 5;
                }
                else if (dAtmoEff > 0.10 && fOrbitalAtmoPower < 100 && atmoThrustCount > 0)
                    fOrbitalAtmoPower += 10;
                else if (dAtmoEff > 0.10 && atmoThrustCount > 0)
                    fOrbitalAtmoPower -= 5; // we may be sucking power from ion
                else // no power left to give, captain!
                {
                    StatusLog("Not Enough Thrust!", textPanelReport);
                    Echo("Not Enough Thrust!");
                }
            }
            else if (dGravity > .01)
            {
                if (fOrbitalIonPower < 100 && ionThrustCount > 0)
                    fOrbitalIonPower += 15;
                else if (fOrbitalHydroPower < 100 && hydroThrustCount > 0)
                {
                    fOrbitalHydroPower += 5;
                }
                else if (dAtmoEff > 0.10 && fOrbitalAtmoPower < 100 && atmoThrustCount > 0)
                    fOrbitalAtmoPower += 10;
                else // no power left to give, captain!
                {
                    StatusLog("Not Enough Thrust!", textPanelReport);
                    Echo("Not Enough Thrust!");
                }

            }

            if (fOrbitalIonPower > 100) fOrbitalIonPower = 100;
            if (fOrbitalAtmoPower > 100) fOrbitalAtmoPower = 100;
            if (fOrbitalAtmoPower < 0) fOrbitalAtmoPower = 0;
            if (fOrbitalHydroPower > 100) fOrbitalHydroPower = 100;

        }

        void decreasePower(double dGravity, double alt)
        {
            if (dGravity > .85 && AtmoEffectiveness() > 0.10)
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