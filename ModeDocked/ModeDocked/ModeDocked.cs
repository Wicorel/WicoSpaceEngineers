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
         * add 'memory' connector like MK3 does
         * 
         * Unload ship when docked..
         * 
         * Load uranium
         * stockpile tanks
         * battery charge
         * 
         * relaunch
         */



        // 0 = master init
        // 1 battery check 30%.  If no batteries->4
        // 2 battery check 80%
        // 3 battery check 100%
        // 4 no battery checks

        void doModeDocked()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":DOCKED!", textPanelReport);
            Echo("Docked!");
            Echo("Autorelaunch=" + bAutoRelaunch.ToString());
            bWantFast = false;
            bWantMedium = false;
            bWantSlow = true;

            //TODO: autounload

            if (bAutoRelaunch )
            {
                Echo("Docked. Checking Relaunch");
/*
                doCargoCheck();
                bool BatteryGo = true;
                bool TanksGo = true;
                bool ReactorsGo = true;
                bool CargoGo = true;

                // Check battery charge
                if (batteryPercentage >= 0 && batteryPercentage < batterypcthigh)
                    BatteryGo = false;

                // check cargo emptied
                if (cargopcent > cargopctmin)
                    CargoGo = false;

                // TODO: Check H2 tanks
                // TODO: check reactor fuel

                if (BatteryGo && TanksGo && ReactorsGo && CargoGo)
                    */
                if(DockAirWorthy())
                {
                    Echo("RELAUNCH!");
                    setMode(MODE_RELAUNCH);
                    bWantFast = true;
                    return;
                }
                else
                {
                    Echo(" Awaiting Relaunch Criteria");
                    StatusLog("Awaiting Relaunch Criteria", textPanelReport);
//                    if (!BatteryGo)
                    {
                        StatusLog(" Battery " + batteryPercentage + "% (" + batterypcthigh + "%)", textPanelReport);
                        Echo(" Battery " + batteryPercentage + "% (" + batterypcthigh + "%)");
                    }
 //                   if(!CargoGo)
                    {
                        StatusLog(" Cargo: " + cargopcent + "% (" + cargopctmin + ")", textPanelReport);
                        Echo(" Cargo: " + cargopcent + "% (" + cargopctmin + ")");
                    }
                    if(TanksHasHydro())
                    {
                        StatusLog(" Hydro: " + hydroPercent + "% (" + cargopctmin + ")", textPanelReport);
                        Echo(" Hydro: " + hydroPercent + "% (" + cargopctmin + ")");
                    }
                }
            }
            if (!AnyConnectorIsConnected())
            {
                // we magically got disconnected..
                setMode(MODE_IDLE);
                powerDownThrusters(thrustAllList); // turn thrusters ON
                if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(false); // turn tanks ON
                // TODO: allow for relay ships that are NOT bases..
                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false,range);
                BatterySetNormal();
            }
            else
            {

                StatusLog(moduleName + ":Power Saving Mode", textPanelReport);
                Echo("Power Saving Mode");
                if (current_state == 0)
                {
                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) TanksStockpile(true);
                    powerDownThrusters(thrustAllList, thrustAll, true);
                    antennaLowPower();
                    SensorsSleepAll();
                    // TODO: ??? turn gyos off?
                    batteryCheck(0, true);
                    doSubModuleTimerTriggers("[DOCKED]");
                    current_state = 1;
                }
                else if (current_state == 1)
                {
                    batteryCheck(0, true);
                    if (batteryPercentage < 0 || (craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0)
                        current_state = 4; // skip battery checks
                    else if (!batteryCheck(30, true))
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
                else //state 4
                {
                    batteryCheck(0, true); //,textBlock);
                }

                // all states
                {
//                    if (bAutoRelaunch)
                    {
                        doCargoCheck();
                        TanksCalculate();
                    }

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
                        // TODO: use setting for 'low' (and 'enough')
                        if (hydroPercent < 0.20f)
                            StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);

                        Echo("H:" + (hydroPercent*100).ToString("000.0%"));
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