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



        // multi-arg
        #region arguments

        bool processArguments(string sArgument)
        {
            string[] varArgs = sArgument.Trim().Split(';');

            for (int iArg = 0; iArg < varArgs.Length; iArg++)
            {
                string[] args = varArgs[iArg].Trim().Split(' ');

                /*
		if (args[0] == "timer")
		{
			processTimerCommand();

		}
        */
            }
            return false; // keep processing in main
        }
        #endregion


    }
}