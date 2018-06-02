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
        string moduleName = "Orbital Launch";
        string sVersion = "3.4C";

        const string sshipOrientationBlock = "Craft Remote Control";

//        Vector3I iForward = new Vector3I(0, 0, 0);
//        Vector3I iUp = new Vector3I(0, 0, 0);
//        Vector3I iLeft = new Vector3I(0, 0, 0);
//        Vector3D currentPosition/*, lastPosition, currentVelocity, lastVelocity*/;
        const string velocityFormat = "0.00";


        //        IMyTerminalBlock shipOrientationBlock = null;

        double dCargoCheckWait = 2; //seconds between checks
        double dCargoCheckLast = -1;

        double dBatteryCheckWait = 5; //seconds between checks
        double dBatteryCheckLast = -1;


        void moduleDoPreModes()
        {
//	        Echo("localDockConnectors.Count=" + localDockConnectors.Count);
	        string output = "";
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
            if (batteryList.Count > 0)
            {
                output += "Batteries: #=" + batteryList.Count.ToString();
                if (dBatteryCheckLast > dBatteryCheckWait)
                {
                    dBatteryCheckLast = 0;
                    batteryCheck(0, false);
                }
                else
                {
                    //                Echo("Battery Check Delay");
                    if (dBatteryCheckLast < 0)
                    {
                        // first-time init
                        dBatteryCheckLast = dBatteryCheckWait + 5; // force check
                    }
                    dBatteryCheckLast += Runtime.TimeSinceLastRun.TotalSeconds;
                }

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
            }
            if (output != "") Echo(output);
            output = "";

            if(solarList.Count>0) output+="Solar: #" + solarList.Count.ToString() + " " + currentSolarOutput.ToString("0.00" + "MW");
            if (output != "") Echo(output);

            output = "";
                float fCurrentReactorOutput = 0;
            reactorCheck(out fCurrentReactorOutput);
            if (reactorList.Count > 0)
            {
                output += "Reactors: #" + reactorList.Count.ToString();
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
            if(output!="") Echo(output);
            output = "";
            Echo("TotalMaxPower=" + totalMaxPowerOutput.ToString("0.00" + "MW"));

            TanksCalculate();
            output = "";
            if (oxyPercent >= 0)
            {
                output+="O:" + oxyPercent.ToString("000.0%");
            }
            else output+="No Oxygen Tanks";

            if (hydroPercent >= 0)
            {
                output+=" H:" + hydroPercent.ToString("000.0%");
            }
            else output+=" No Hydrogen Tanks";

            if (output != "") Echo(output);
            output = "";

            if (gasgenList.Count > 0)
            {
                Echo(gasgenList.Count + " Gas Gens");
            }

            output = "";
            if (AnyConnectorIsConnected()) output += "Connected";

	        else
	        {
		        output += "Not Connected";

		        if (AnyConnectorIsLocked()) output += " : Locked";
		        else output += " : Not Locked";
	        }

	        Echo(output);
        }

        void modulePostProcessing()
        {
//	        Echo(sInitResults);
	        echoInstructions();
        }
        void ModuleSerialize(INIHolder iNIHolder)
        {
            OrbitalSerialize(iNIHolder);
        }

        void ModuleDeserialize(INIHolder iNIHolder)
        {
            OrbitalDeserialize(iNIHolder);
        }

    }
}