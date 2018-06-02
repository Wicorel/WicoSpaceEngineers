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
        double HoverCameraElapsedMs = -1;
        double HoverCameraWaitMs = 0.50;

        // states
        // 0 = init
        // 10 = powered hovering. No connections
        // 20 = landing gear locked. 
        // 

        void doModeHover()
        {
            StatusLog("clear", textPanelReport);
            Echo("Hover Mode:" + current_state);
            StatusLog(OurName + ":" + moduleName + ":Hover", textPanelReport);
            StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
            double elevation = 0;

            ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
            StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);

            if (current_state == 0)
            {
                calculateBestGravityThrust();

                float fAtmoPower, fHydroPower, fIonPower;
                calculateHoverThrust(thrustOrbitalUpList, out fAtmoPower, out fHydroPower, out fIonPower);
                if (fAtmoPower > 0) powerDownThrusters(thrustAllList, thrustatmo);
                if (fHydroPower > 0) powerDownThrusters(thrustAllList, thrusthydro);
                if (fIonPower > 0) powerDownThrusters(thrustAllList, thrustion);
                current_state = 10;
//                powerDownThrusters(thrustAllList, thrustAll); // turns ON thrusters
            }

            bool bGearsLocked = anyGearIsLocked();
            bool bConnectorsConnected = AnyConnectorIsConnected();
            bool bConnectorIsLocked = AnyConnectorIsLocked();
            bool bGearsReadyToLock = anyGearReadyToLock();


            /*
            Echo("Gears:");
            foreach(var gear in gearList)
            {
                Echo(gear.CustomName);
            }
            */
            if (bGearsLocked)
            {
                if (current_state != 20)
                {
                    // gears just became locked
                    Echo("Force thrusters Off!");
                    powerDownThrusters(thrustAllList, thrustAll, true);
//                    blockApplyAction(thrustAllList, "OnOff_Off");

                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                    {
                        TanksStockpile(true);
                        GasGensEnable();
//                        blockApplyAction(tankList, "Stockpile_On");
//                        blockApplyAction(gasgenList, "OnOff_On");
                    }
                    current_state = 20;
                }
                 landingDoMode(1);
           }
            else
            {
                if (current_state != 10)
                {
                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                    {
                        powerDownThrusters(thrustAllList); // turns ON all thusters
                        TanksStockpile(false);
//                        blockApplyAction(tankList, "Stockpile_Off");
                        //				blockApplyAction(gasgenList,"OnOff_On");
                    }

                    current_state = 10;
                }
                landingDoMode(0);
            }

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
                            Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                            if (!bGearsLocked) StatusLog("Hovering above: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                        }
                    }
                }
            }
            else Echo("Camera Scan delay");

            if (bGearsLocked)
            {
                StatusLog("Landing Gear(s) LOCKED!", textPanelReport);
                // we can turn off thrusters.. but that's about it..
                // stay in 'hover' iMode
            }
            else if (bGearsReadyToLock)
            {
                StatusLog("Landing Gear(s) Ready to lock.", textPanelReport);
            }
            if (bConnectorsConnected)
            {
                //prepareForSupported();
                StatusLog("Connector connected!\n   auto-prepare for launch", textPanelReport);
                setMode(MODE_LAUNCHPREP);
            }
            else
            {
                if (!bGearsLocked)
                {
                    //			blockApplyAction(thrustAllList, "OnOff_On");
                    //			if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_On");
                }
                ConnectAnyConnectors(false, true);// "OnOff_On");
            }

            if (bConnectorIsLocked)
            {
                StatusLog("Connector Locked!", textPanelReport);
            }

            if (bConnectorIsLocked || bGearsLocked)
            {
                Echo("Stable");
                landingDoMode(1); // landing mode
                gyrosOff();
            }
            else
            {
                if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                {
                    gyrosOff();
                    StatusLog("Wico Gravity Alignment OFF", textPanelReport);
                }
                else
                {
                    StatusLog("Gravity Alignment Operational", textPanelReport);

                    /*
                    string sOrientation = "";
                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                        sOrientation = "rocket";
                    */
                    bool bAimed = GyroMain(sOrbitalUpDirection);
                    if (bAimed)
                        bWantMedium = true;
                    else
                        bWantFast = true;
                }
            }

            //	StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

// done in premodes	        batteryCheck(0, false);//,textPanelReport);
            //	if (bValidExtraInfo)
            {
                if (batteryPercentage >= 0) StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                if (oxyPercent >= 0)
                {
                    StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                    //Echo("O:" + oxyPercent.ToString("000.0%"));
                }
                else Echo("No Oxygen Tanks");

                if (hydroPercent >= 0)
                {
                    StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
 //                   Echo("H:" + (hydroPercent*100).ToString("0.0") + "%");
                    if(hydroPercent<0.20f)
                      StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);
                }
                else Echo("No Hydrogen Tanks");
                if (batteryPercentage>=0 && batteryPercentage < batterypctlow)
                    StatusLog(" WARNING: Low Battery Power", textPanelReport);

                //		if (iOxygenTanks > 0) StatusLog("O2:" + progressBar(tanksFill(iTankOxygen)), textPanelReport);
                //		if (iHydroTanks > 0) StatusLog("Hyd:" + progressBar(tanksFill(iTankHydro)), textPanelReport);
            }

            if (dGravity <= 0)
            {
                setMode(MODE_INSPACE);
                gyrosOff();
                StatusLog("clear", textPanelReport);
            }

        }

    }
}