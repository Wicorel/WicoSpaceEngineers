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
        #region domodes 
        void doModes()
        {
            Echo("mode=" + iMode.ToString());
            doModeAlways();
            /*            
                        if (AnyConnectorIsConnected() && !((craft_operation & CRAFT_MODE_ORBITAL) > 0))
                        {
                            Echo("DM:docked");
                            setMode(MODE_DOCKED);
                        }
            */

//            if (iMode == MODE_DOSCAN) doModeScans();

            if (iMode == MODE_FINDORE) doModeFindOre();
            if (iMode == MODE_GOTOORE) doModeGotoOre();
//            if (iMode == MODE_BORINGMINE) doModeBoringMine();
            if (iMode == MODE_EXITINGASTEROID) doModeExitingAsteroid();

            if (iMode == MODE_SEARCHORIENT) doModeSearchOrient();
            if (iMode == MODE_SEARCHSHIFT) doModeSearchShift();
            if (iMode == MODE_SEARCHVERIFY) doModeSearchVerify();
            if (iMode == MODE_SEARCHCORE) doModeSearchCore();

/*
            if (iMode == MODE_IDLE) doModeIdle();
            else if (iMode == MODE_DESCENT) doModeDescent();
*/
        }
        #endregion


        #region modeidle 
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            setMode(MODE_IDLE);
//            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
              StatusLog(moduleName + " Manual Control", textPanelReport);
        }
        #endregion

        double dCargoCheckWait = 2; //seconds between checks
        double dCargoCheckLast = -1;

        double dBatteryCheckWait = 5; //seconds between checks
        double dBatteryCheckLast = -1;

        void doModeAlways()
        {
	        processPendingSends();
	        processReceives();
        }
         void moduleDoPreModes()
        {
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
                    dBatteryCheckLast = dBatteryCheckWait + 5; // force check
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

            TanksCalculate();
        }

        void modulePostProcessing()
        {
            if (init)
            {
            // only need to do these like once per second. or if something major changes.
                OreDoCargoCheck();
                OreDumpFound();

                //dumpOreLocs();

                double maxThrust = calculateMaxThrust(thrustForwardList);
                Echo("maxThrust=" + maxThrust.ToString("N0"));

                MyShipMass myMass;
                myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass;
                Echo("effectiveMass=" + effectiveMass.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;
                Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

                Echo("Cargo=" + cargopcent.ToString() + "%");
            }

            Echo(sInitResults);
            echoInstructions();
        }

        void processReceives()
        {
            if (sReceivedMessage != "")
            {
                Echo("Received Message=\n" + sReceivedMessage);

                if (AsteroidProcessMessage(sReceivedMessage))
                    return;

                string[] aMessage = sReceivedMessage.Trim().Split(':');

                if (aMessage.Length > 1)
                {
                    if (aMessage[0] != "WICO")
                    {
                        Echo("not wico system message");
                        return;
                    }
                    if (aMessage.Length > 2)
                    {
                        /*
                        if (aMessage[1] == "MOM")
                        {
                        }
                        */
                    }
                }
            }
        }

    }
}