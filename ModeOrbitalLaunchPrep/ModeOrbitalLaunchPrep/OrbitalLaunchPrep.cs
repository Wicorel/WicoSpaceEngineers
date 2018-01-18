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
            StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textBlock);

            Echo(moduleName + ":LaunchPrep:" + current_state);
 //           Echo("BatteryPercentage=" + batteryPercentage);
//            Echo("batterypctlow=" + batterypctlow);

            if (dGravity <= 0)
            {
                if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
                else
                {
                    setMode(MODE_INSPACE);
                    gyrosOff();
                    StatusLog("clear", textBlock);
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
                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(false); // blockApplyAction(tankList, "Stockpile_Off");
                    setMode(MODE_HOVER);
                }
                ConnectAnyConnectors(false, true);// "OnOff_On");
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
            //	else             batteryCheck(0, true); //,textBlock);
            //TODO: Check reactors and pull uranium
            //TODO: Check gas gens and pull ice

            //	StatusLog("C:" + progressBar(cargopcent), textBlock);

            if (batteryList.Count > 0)
            {
                StatusLog("Bat:" + progressBar(batteryPercentage), textBlock);
                Echo("BatteryPercentage=" + batteryPercentage);
            }
            else StatusLog("Bat: <NONE>", textBlock);

            if (oxyPercent >= 0)
            {
                StatusLog("O2:" + progressBar(oxyPercent * 100), textBlock);
                //Echo("O:" + oxyPercent.ToString("000.0%"));
            }
            else Echo("No Oxygen Tanks");

            if (hydroPercent >= 0)
            {
                StatusLog("Hyd:" + progressBar(hydroPercent * 100), textBlock);
                if(hydroPercent<0.20f)
                    StatusLog(" WARNING: Low Hydrogen Supplies", textBlock);

                Echo("H:" + hydroPercent.ToString("000.0%"));
            }
            else Echo("No Hydrogen Tanks");
            if (batteryList.Count>0 && batteryPercentage < batterypctlow)
                StatusLog(" WARNING: Low Battery Power", textBlock);


        }

    }

}