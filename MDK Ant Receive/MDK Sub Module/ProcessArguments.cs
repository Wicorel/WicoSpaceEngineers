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



        bool processArguments(string sArgument)
        {

	        if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
	        {
		        Echo("Arg=" + sArgument);
	        }
	        else antReceive(sArgument);

	        return false; // keep processing in main
        }
  

    }
}