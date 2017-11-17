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
                Echo("Arg=" + sArgument);
            }
            // try to process the message ourselves

            else if(processDockMessage(sArgument))
            {
                Echo("Processed");
            }
            // we don't know this message.  Pass it on to other modules
    //        else antReceive(sArgument);

	        return false; // keep processing in main
        }
        bool moduleProcessAntennaMessage(string sArgument)
        {
            // process an antenna message locally.  If processed, return true
            return (processDockMessage(sArgument));
        }

    }
}