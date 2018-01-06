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

        Dictionary<string, int> modeCommands = new Dictionary<string, int>();
        string sBanner = "";
//        UpdateFrequency ufFast = UpdateFrequency.Update1; // default value for "Fast" for this module
        UpdateFrequency ufFast = UpdateFrequency.Once; // default value for "Fast" for this module

        public Program()
        {
            doModuleConstructor();
            sBanner = OurName + ":" + moduleName + " V" + sVersion + " ";
            Echo(sBanner + "Creator");
            //            gridsInit(); //GridTerminalSystem cannot be relied on at initial compile
            //            initLogging();
 // Only needed for 'main' module           Runtime.UpdateFrequency |= UpdateFrequency.Once;
            if (!Me.CustomName.Contains(moduleName))
                Me.CustomName = "PB " + OurName + " " + moduleName;
        }

        // added UpdateType and UpdateFrequency
        // sub-module common main
#region MODULEMAIN

        bool init = false;
        bool bWasInit = false;
        bool bWantFast = false;
        bool bWantMedium = false;

        bool bWorkingProjector = false;

        double velocityShip = -1;

        //       void Main(string sArgument)
        void Main(string sArgument, UpdateType ut)
        {
            Echo(sBanner + tick());
            Echo(ut.ToString());
            bWantFast = false;
            bWantMedium = false;

            bWorkingProjector = false;
            var list = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(list, localGridFilter);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsWorking)
                {
                    Echo("Projector:" + list[i].CustomName);
                    bWorkingProjector = true;
                }
            }
            if (bWorkingProjector)
                Echo("Working local Projector found!");

            if (sArgument != "" && sArgument != "timer" && sArgument != "wccs") Echo("Arg=" + sArgument);

            if (sArgument == "init")
            {
                sInitResults = "";
                init = false;
                currentRun = 0;
            }

            if (!init)
            {
                if (bWorkingProjector)
                {
                    StatusLog("clear", getTextBlock(sTextPanelReport));

                    StatusLog(moduleName + ":Construction in Progress\nTurn off projector to continue", textPanelReport);
                }
                bWantFast = true;
                doInit();
                bWasInit = true;
            }
            else
            {
                if (bWasInit) StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + sInitResults, textLongStatus, true);

                Deserialize();

                Echo(craftOperation());
                if (gpsCenter != null)
                {
                    vCurrentPos = gpsCenter.GetPosition();
                    velocityShip = ((IMyShipController)gpsCenter).GetShipSpeed();
                }
                if ((ut & (UpdateType.Trigger | UpdateType.Terminal)) > 0)
                {
                    // pay attention to argument
                    if (moduleProcessArguments(sArgument))
                    {
                        Serialize();
                        return;
                    }

                }
                else if ((ut & (UpdateType.Mod)) > 0)
                {
                    // script run by a mod
                    if (moduleProcessArguments(sArgument))
                    {
                        Serialize();
                        return;
                    }

                }
                else if ((ut & (UpdateType.Script)) > 0)
                {
                    // script run by another PB
                    if (moduleProcessArguments(sArgument))
                    {
                        Serialize();
                        return;
                    }

                }
                else if ((ut & (UpdateType.Antenna)) > 0)
                {
                    // antenna message
                    if (!moduleProcessAntennaMessage(sArgument))
                    {
                        antReceive(sArgument);
                    }
                    Serialize();
                    doTriggerMain();
                    return;
                }
                else
                {
                    //            if ((ut & (UpdateType.Once | UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) > 0)
                    sArgument = ""; // else ignore argument
                }

                processPendingReceives();
                processPendingSends();


                moduleDoPreModes();

                doModes();
            }

            Serialize();

            if (bWantFast)
            {
                Echo("FAST!");
                Runtime.UpdateFrequency |= ufFast;
            }
            else
            {
                Runtime.UpdateFrequency &= ~(ufFast);
            }
            if (bWantMedium)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            }
            else
            {
                Runtime.UpdateFrequency &= ~(UpdateFrequency.Update10);
            }

            modulePostProcessing();

            bWasInit = false;
        }
#endregion

        void echoInstructions(string sBanner = null)
        {
            float fper = 0;
            fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
            if (sBanner == null) sBanner = "Instructions=";
            Echo(sBanner + " " + (fper * 100).ToString("0.00") + "%");

        }



    }
}