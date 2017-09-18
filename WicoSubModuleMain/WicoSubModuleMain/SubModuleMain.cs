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
        public Program()
        {
            sBanner = OurName + ":" + moduleName + " V" + sVersion + " ";
            Echo(sBanner + "Creator");
            initLogging();
            if (!Me.CustomName.Contains(moduleName))
                Me.CustomName = "PB " + OurName + " " + moduleName;
        }



        // sub-module common main
#region MODULEMAIN

        bool init = false;
        bool bWasInit = false;
        bool bWantFast = false;
        bool bWorkingProjector = false;

        double velocityShip = -1;

        void Main(string sArgument)
        {
            Echo(sBanner + tick());
            bWantFast = false;

            bWorkingProjector = false;
            var list = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(list, localGridFilter);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsWorking)
                {
                    Echo("Working local Projector found!");
                    bWorkingProjector = true;
                }
            }

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
                if (gpsCenter !=null)
                {
                    vCurrentPos = gpsCenter.GetPosition();
                    velocityShip = ((IMyShipController)gpsCenter).GetShipSpeed();
                }
                if (processArguments(sArgument))
                    return;

                if (bWantFast) Echo("FAST!");

                moduleDoPreModes();

                doModes();
            }

            Serialize();

            if (bWantFast)
                doSubModuleTimerTriggers("[WCCT]");

            modulePostProcessing();

            bWasInit = false;
        }
#endregion

        void echoInstructions(string sBanner = null)
        {
            float fper = 0;
            fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
            if (sBanner == null) sBanner = "Instructions=";
            Echo(sBanner + (fper * 100).ToString("0.00") + "%");

        }



    }
}