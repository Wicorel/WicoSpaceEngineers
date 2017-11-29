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



        #region arguments

        bool moduleProcessArguments(string sArgument)
        {
            if (sArgument == "" || sArgument == "timer" || sArgument == "wccs")
            {
                Echo("Arg=" + sArgument);
                if (sPassedArgument != "" && sPassedArgument != "timer")
                {
                    Echo("Using Passed Arg=" + sPassedArgument);
                    sArgument = sPassedArgument;
                }
            }
            if (sArgument == "init")
            {
                sInitResults = "";
                init = false;
                currentInit = 0;
                doInit(); // do first pass.
                return false;
            }

            string[] varArgs = sArgument.Trim().Split(';');

            for (int iArg = 0; iArg < varArgs.Length; iArg++)
            {
                string[] args = varArgs[iArg].Trim().Split(' ');
                if (args[0] == "timer")
                {
                    // do nothing for sub-module (should not receive this argument)
                }
                else if (args[0] == "wccs")
                {

                }
                else if (args[0] == "wcct")
                {

                }
                else
                {
                    int iDMode;
                    if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                    {
                        sArgResults = "mode set to " + iDMode;
                        setMode(iDMode);
                        // return true;
                    }
                    else
                    {
                        sArgResults = "Unknown argument:" + args[0];
                    }
                }
            }
            return false; // keep processing in main
        }
        #endregion
        bool moduleProcessAntennaMessage(string sArgument)
        {
            // we directly received an antenna message
            return false;
        }


    }
}