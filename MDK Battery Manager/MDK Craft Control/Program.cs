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
        string LOW_BATTERY_TIMER = "Battery Low";  
        string FULL_BATTERY_TIMER = "Battery Full";  

        // shouldn't need to change anything below this line

        string OurName = "Battery";
        string moduleName = "Manager";
        string sVersion = "3.5 May 2019";

        const string sGPSCenter = "Craft Remote Control";
        int iMode = 0;

        IMyTerminalBlock gpsCenter = null;

//        Vector3D currentPosition;

        const string velocityFormat = "0.00";

//        IMyTerminalBlock anchorPosition;

        class OurException : Exception
        {
            public OurException(string msg) : base("BatteryManager" + ": " + msg) { }
        }
/*
        double dCargoCheckWait = 2; //seconds between checks
        double dCargoCheckLast = -1;
*/
        double dBatteryCheckWait = 5; //seconds between checks
        double dBatteryCheckLast = -1;


        void moduleDoPreModes()
        {
            StatusLog("clear", textPanelReport);

            string output = "";
            if (AnyConnectorIsConnected()) output += "Connected";
            else output += "Not Connected";

            if (AnyConnectorIsLocked()) output += "\nLocked";
            else output += " : Not Locked";

//            Echo(output);
            Log(output);
            output = "";

            if (bWantFast) Echo("FAST!");
/*
            if (dCargoCheckLast > dCargoCheckWait)
            {
                dCargoCheckLast = 0;


                doCargoCheck();
            }
            else
            {
                if (dCargoCheckLast < 0)
                {
                    // first-time init
                    //                    dProjectorCheckLast = Me.EntityId % dProjectorCheckWait; // randomize initial check
                    dCargoCheckLast = dCargoCheckWait + 5; // force check
                }
                dCargoCheckLast += Runtime.TimeSinceLastRun.TotalSeconds;
            }

            Echo("Cargo=" + cargopcent.ToString() + "%");
            //            Echo("Cargo Mult=" + cargoMult.ToString());
*/
            if (dBatteryCheckLast > dBatteryCheckWait)
            {
                dBatteryCheckLast = 0;
                batteryCheck(0, false);
            }
            else
            {
                if (dBatteryCheckLast < 0)
                {
                    // first-time init
                    dBatteryCheckLast = dBatteryCheckWait+5; // force check
                }
                dBatteryCheckLast += Runtime.TimeSinceLastRun.TotalSeconds;
            }

            output += "Batteries: #=" + batteryList.Count.ToString();
            if (batteryList.Count > 0 && maxBatteryPower > 0)
            {
                output += " : " + (getCurrentBatteryOutput() / maxBatteryPower * 100).ToString("0.00") + "%";
                output += "\n Storage=" + batteryPercentage.ToString() + "%";
            }

            Log(output);
            output = "";

            Log("Solar: #" + solarList.Count.ToString() + " " + currentSolarOutput.ToString("0.00" + "MW"));

            float fCurrentReactorOutput = 0;
            reactorCheck(out fCurrentReactorOutput);
            if (reactorList.Count > 0)
            {
                output = "Reactors: #" + reactorList.Count.ToString();
                output += " - " + maxReactorPower.ToString("0.00") + "MW\n";
                float fPer = (float)(fCurrentReactorOutput / totalMaxPowerOutput * 100);
                output += " Curr Output=" + fCurrentReactorOutput.ToString("0.00") + "MW" + " : " + fPer.ToString("0.00") + "%";

            }
 //           Echo(output);
            Log(output);
            output = "";

            Log("TotalMaxPower=" + totalMaxPowerOutput.ToString("0.00" + "MW"));
            if (!AnyConnectorIsConnected())
            {
                Log("Lonely Drone");
                // we are a lonely miner off doing work in the mines. 
                if (batteryPercentage < batterypctlow)    // at 20% battery left, stop mining 
                {
                    Log("Battery LOW!");
                    doSubModuleTimerTriggers(LOW_BATTERY_TIMER);
                }
                else if(batteryPercentage >99)
                {
                    Log("Battery FULL!");
                    doSubModuleTimerTriggers(FULL_BATTERY_TIMER);
                }

            }
            else
            {
                Log("Drone with a friend");

                // we are the miner, but must be attached to a ship through connector 
                if (batteryList.Count > 1)
                {
                    //                    if (!batteryCheck(10,false,getTextStatusBlock())) 
                    if (!batteryCheck(10, false))
                        if (!batteryCheck(40,false))
                            if (!batteryCheck(75,false))
                                batteryCheck(98,false);
                }

            }
        }


        void modulePostProcessing()
        {
 //           Echo(sInitResults);
            echoInstructions();
        }


    }
}