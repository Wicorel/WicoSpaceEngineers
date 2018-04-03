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



        bool moduleProcessArguments(string sArgument)
        {

            if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
            {
                //		Echo("Arg=" + sArgument);
                //		Echo("PassedArg=" + sPassedArgument);
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

            string[] args = sArgument.Trim().Split(' ');

            if(DockProcessMessage(sArgument))
            {
                Echo("Processed");
            }
            else
            {
                int iDMode;
                if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                {
//                    sArgResults = "mode set to " + iDMode;
                    setMode(iDMode);
                    // return true;
                }
                else
                {
//                    sArgResults = "Unknown argument:" + args[0];
                }
            }

            return false; // keep processing in main
        }
        bool moduleProcessAntennaMessage(string sArgument)
        {
            // process an antenna message locally.  If processed, return true
            if (DockProcessMessage(sArgument))
                return true;
            if (AsteroidProcessMessage(sArgument))
                return true;
            if (OreProcessMessage(sArgument))
                return true;
            return false;
        }

    }
}