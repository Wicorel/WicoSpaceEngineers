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

        void doModeLaunchprep()
        {
            IMyTextPanel textBlock = textPanelReport;

            StatusLog("clear", textBlock);

            StatusLog(OurName + ":" + moduleName + ":Launch Prep", textBlock);
            StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
            //	StatusLog("Calculated Simspeed=" + fSimSpeed.ToString(velocityFormat), textBlock);
            //	StatusLog("->If Calculated Simspeed does not match \n actual, use SetSimSpeed command to \n set the actual current simspeed.\n", textBlock);

            if (dGravity <= 0)
            {
                if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
                else
                {
                    setMode(MODE_INSPACE);
                    gyrosOff();
                    StatusLog("clear", textPanelReport);
                }
                return;
            }


            if (anyGearIsLocked())
            {
                StatusLog("Landing Gear(s) LOCKED!", textBlock);
            }
            if (AnyConnectorIsConnected())
            {
                StatusLog("Connector connected!\n   auto-prepare for launch", textBlock);
            }
            else
            {
                if (!anyGearIsLocked())
                {
                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList, "Stockpile_Off");
                    setMode(MODE_HOVER);
                }
                ConnectAnyConnectors(false, "OnOff_On");
            }

            if (AnyConnectorIsLocked())
            {
                StatusLog("Connector Locked!", textBlock);
            }

            if (AnyConnectorIsLocked() || anyGearIsLocked())
            {
                Echo("Stable");
            }
            else
            {
                //prepareForSolo();
                setMode(MODE_HOVER);
                return;
            }

            if (AnyConnectorIsConnected())
            {
                if (current_state == 0)
                {
                    powerDownThrusters(thrustAllList, thrustAll, true);// blockApplyAction(thrustAllList, "OnOff_Off");
                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(true);// blockApplyAction(tankList, "Stockpile_On");

                    current_state = 1;
                }
                else if (current_state == 1)
                {
                    //			if ((craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0)
                    current_state = 4; // skip battery checks
                                       //			else
                                       //			if (!batteryCheck(30, true))
                                       //				current_state = 2;
                }
                else if (current_state == 2)
                {
                    //			if (!batteryCheck(80, true))
                    current_state = 3;
                }
                else if (current_state == 3)
                {
                    //			if (!batteryCheck(100, true))
                    current_state = 1;
                }
            }
            //	else batteryCheck(0, true); //,textBlock);

            //	StatusLog("C:" + progressBar(cargopcent), textBlock);

            //	if (bValidExtraInfo)
            {
                StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                if (oxyPercent >= 0)
                {
                    StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                    //Echo("O:" + oxyPercent.ToString("000.0%"));
                }
                else Echo("No Oxygen Tanks");

                if (hydroPercent >= 0)
                {
                    StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                    if(hydroPercent<0.20f)
                      StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);

                    Echo("H:" + hydroPercent.ToString("000.0%"));
                }
                else Echo("No Hydrogen Tanks");
                if (batteryPercentage < batterypctlow)
                    StatusLog(" WARNING: Low Battery Power", textPanelReport);

                //		if (iOxygenTanks > 0) StatusLog("O2:" + progressBar(tanksFill(iTankOxygen)), textPanelReport);
                //		if (iHydroTanks > 0) StatusLog("Hyd:" + progressBar(tanksFill(iTankHydro)), textPanelReport);
            }


            /*
                if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                {
                    thrustStage1UpList = thrustForwardList;
                    thrustStage1DownList = thrustBackwardList;
                }
                else
                {
                    thrustStage1UpList = thrustUpList;
                    thrustStage1DownList = thrustDownList;
                }
            */
        }

    }

}