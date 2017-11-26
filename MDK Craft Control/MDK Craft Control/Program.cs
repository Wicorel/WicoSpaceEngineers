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
        string OurName = "Wico Craft";
        string moduleName = "Master";
        string sVersion = "3.1a";

        const string sGPSCenter = "Craft Remote Control";

        IMyTerminalBlock gpsCenter = null;

        Vector3D currentPosition;

        const string velocityFormat = "0.00";

        IMyTerminalBlock anchorPosition;

        class OurException : Exception
        {
            public OurException(string msg) : base("WicoCraft" + ": " + msg) { }
        }

        double dCargoCheckWait = 2; //seconds between checks
        double dCargoCheckLast = -1;

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

            Echo(output);
            Log(output);
            output = "";

            if (bWantFast) Echo("FAST!");

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
                /*
                // Debug Info:
                foreach (var tb in batteryList)
                {
                    float foutput = 0;
                    IMyBatteryBlock r = tb as IMyBatteryBlock;

                    MyResourceSourceComponent source;
                    r.Components.TryGet<MyResourceSourceComponent>(out source);

                    if (source != null)
                    {
                        foutput = source.MaxOutput;
                    }

//                    PowerProducer.GetMaxOutput(r, out foutput);
                    output+=foutput.ToString() + "MW " + r.CustomName;
                }
                */
            }

            Echo(output);
            output = "";

            Echo("Solar: #" + solarList.Count.ToString() + " " + currentSolarOutput.ToString("0.00" + "MW"));

            float fCurrentReactorOutput = 0;
            reactorCheck(out fCurrentReactorOutput);
            if (reactorList.Count > 0)
            {
                output = "Reactors: #" + reactorList.Count.ToString();
                output += " - " + maxReactorPower.ToString("0.00") + "MW\n";
                float fPer = (float)(fCurrentReactorOutput / totalMaxPowerOutput * 100);
                output += " Curr Output=" + fCurrentReactorOutput.ToString("0.00") + "MW" + " : " + fPer.ToString("0.00") + "%";
                //			Echo("Reactor total usage=" + fPer.ToString("0.00") + "%");

                /*
                // debug output
                foreach (var tb in reactorList)
                {
                    IMyReactor r = tb as IMyReactor;
                    Echo(r.MaxOutput.ToString() + " " + r.CustomName);
                }
                */

            }
            Echo(output);
            output = "";

            Echo("TotalMaxPower=" + totalMaxPowerOutput.ToString("0.00" + "MW"));

            hydroPercent = tanksFill(iTankHydro);
            oxyPercent = tanksFill(iTankOxygen);
            if (oxyPercent >= 0)
            {
                Echo("O:" + oxyPercent.ToString("000.0%"));
            }
            else Echo("No Oxygen Tanks");

            if (hydroPercent >= 0)
            {
                Echo("H:" + hydroPercent.ToString("000.0%"));
            }
            else Echo("No Hydrogen Tanks");

            if(gasgenList.Count >0)
            {
                Echo(gasgenList.Count + " Gas Gens");
            }

            if (dGravity >= 0)
            {
                Echo("Grav=" + dGravity.ToString(velocityFormat));
                Log("Planet Gravity " + dGravity.ToString(velocityFormat) + " g");
                Log(progressBar((int)(dGravity / 1.1 * 100)));
            }
            else Log("ERROR: No Remote Control found!");

            StatusLog("clear", gpsPanel);
        }


        void modulePostProcessing()
        {
            Echo(sInitResults);
            echoInstructions();
        }

        void ResetMotion(bool bNoDrills = false)
        {
            powerDownThrusters(thrustAllList);
            gyrosOff();
            powerDownRotors(rotorNavLeftList);
            powerDownRotors(rotorNavRightList);
	        if (gpsCenter is IMyRemoteControl) ((IMyRemoteControl)gpsCenter).SetAutoPilotEnabled(false);
	        if (gpsCenter is IMyShipController) ((IMyShipController)gpsCenter).DampenersOverride = true;
            if(!bNoDrills) turnDrillsOff();
        }

        void MasterReset()
        {
            ResetToIdle();
            ResetMotion();
            bValidDock = false;
            bValidLaunch1 = false;
            bValidHome = false;
            bValidInitialContact = false;
            bValidInitialExit = false;
            bValidTarget = false;
            bValidAsteroid = false;
            bValidNextTarget = false;

            // operation flags
            bAutopilotSet = true;
            bAutoRelaunch = false;
            iAlertStates = 0;
            iDetects = 0;
            sReceivedMessage = "";
            sLastLoad = "";
            Serialize();
        }

        // need to use me.CustomData
        #region autoconfig
        void autoConfig()
        {
            craft_operation = CRAFT_MODE_AUTO;
            if ((craft_operation & CRAFT_MODE_MASK) == CRAFT_MODE_AUTO)
            {
                int iThrustModes = 0;

                if (Me.CustomName.ToLower().Contains("nad"))
                    craft_operation |= CRAFT_MODE_NAD;
                if (Me.CustomName.ToLower().Contains("rotor"))
                    craft_operation |= CRAFT_MODE_ROTOR;
                else if (/*wheelList.Count>0 || */ Me.CustomName.ToLower().Contains("sled"))
                    craft_operation |= CRAFT_MODE_SLED;

                 if (ionThrustCount > 0)
                {
                    iThrustModes++;
                }
                if (hydroThrustCount > 0)
                {
                    iThrustModes++;
                }
                if (atmoThrustCount > 0)
                {
                    iThrustModes++;
                }

                // if it has NAV ROTORS, assume rotor propulsion
                if(rotorNavLeftList.Count>0 && rotorNavRightList.Count>0) craft_operation |= CRAFT_MODE_ROTOR;

                // if it has SLED wheels (and some thrusters), assume SLED propulsion
                if(wheelSledList.Count>0 && iThrustModes>0) craft_operation |= CRAFT_MODE_SLED;


                if (iThrustModes > 1 || Me.CustomName.ToLower().Contains("orbital"))
                    craft_operation |= CRAFT_MODE_ORBITAL;
                if (Me.CustomName.ToLower().Contains("rocket"))
                    craft_operation |= CRAFT_MODE_ROCKET;
                if (Me.CustomName.ToLower().Contains("pet"))
                    craft_operation |= CRAFT_MODE_PET;
                if (Me.CustomName.ToLower().Contains("noautogyro"))
                    craft_operation |= CRAFT_MODE_NOAUTOGYRO;
                if (Me.CustomName.ToLower().Contains("nopower"))
                    craft_operation |= CRAFT_MODE_NOPOWERMGMT;
                if (Me.CustomName.ToLower().Contains("notank"))
                    craft_operation |= CRAFT_MODE_NOTANK;
            }
        }
        #endregion

        void processTimerCommand()
        {
            string output = "";
            currentPosition = anchorPosition.GetPosition();
            output += velocityShip.ToString(velocityFormat) + " m/s";
            output += " (" + (velocityShip * 3.6).ToString(velocityFormat) + "km/h)";
            Log(output);
        }


    }
}