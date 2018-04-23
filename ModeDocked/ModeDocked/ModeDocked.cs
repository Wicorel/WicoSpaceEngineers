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

        /*
         * TODO:
         * 
         * Unload ship when docked..
         * 
         * Load uranium
         * stockpile tanks
         * battery charge
         */

 //       string sDockedSection = "DOCKED";

        void DockedInitCustomData(INIHolder iNIHolder)
        {
//            iNIHolder.GetValue(sDockedSection, "ActionStart", ref dtDockedActionStart);
        }
        void DockedSerialize(INIHolder iNIHolder)
        {
        }

        void DockedDeserialize(INIHolder iNIHolder)
        {
        }


        // 0 = master init
        // 1 = inited.

        void doModeDocked()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":DOCKED!", textPanelReport);
            Echo("Docked!");
            /*
            if (bAutoRelaunch && bValidDock)
            {
                Echo("Docked. Checking Relaunch");
                if (batteryPercentage > batterypcthigh && cargopcent < cargopctmin)
                {
                    setMode(MODE_RELAUNCH);//StartRelaunch();
                    return;
                }
                else
                    Echo(" Awaiting Relaunch Criteria");
            }
            */
            if (!AnyConnectorIsConnected())
            {
                // we magically got disconnected..
                setMode(MODE_IDLE);
                powerDownThrusters(thrustAllList);
                // TODO: allow for relay ships that are NOT bases..
                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false,range);
                // Need battery management.
            }
            else
            {

                StatusLog(moduleName + ":Power Saving Mode", textPanelReport);
                Echo("Power Saving Mode");
                if (current_state == 0)
                {
                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(true);// blockApplyAction(tankList, "Stockpile_On");
                    powerDownThrusters(thrustAllList, thrustAll, true);
                    antennaLowPower();
                    sleepAllSensors();
                    // TODO: ??? turn gyos off?
                    batteryCheck(0, true);
                    current_state = 1;
                }
                else if (current_state == 1)
                {
                    batteryCheck(0, true);
                    if (batteryPercentage < 0 || (craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0 )
                        current_state = 4; // skip battery checks
                    else
                                                   if (!batteryCheck(30, true))
                        current_state = 2;
                }
                else if (current_state == 2)
                {
                    if (!batteryCheck(80, true))
                        current_state = 3;
                }
                else if (current_state == 3)
                {
                    if (!batteryCheck(100, true))
                        current_state = 1; // go back and check again
                }
                else batteryCheck(0, true); //,textBlock);

                ///
                {
                    if (batteryPercentage >= 0) StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                    else Echo("No Batteries");
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
                    if (batteryPercentage >=0 && batteryPercentage < batterypctlow)
                        StatusLog(" WARNING: Low Battery Power", textPanelReport);

                    // TODO: get uranium into reactors; take out excess ingots; turn off conveyor usage (like TIM)
                    // TODO: get ore OUT of ship and into base (including stone)
                    // TODO: Handle ore carrier/shuttle
                }
            }

    }

    }
}