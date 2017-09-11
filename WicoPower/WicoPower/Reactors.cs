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
#region reactors
List<IMyTerminalBlock> reactorList = new List<IMyTerminalBlock>();

void initReactors()
{
	reactorList.Clear();
	maxReactorPower = -1;
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactorList, localGridFilter);
	if (reactorList.Count > 0)
		maxReactorPower = 0;
	foreach(var tb in reactorList)
	{
		IMyReactor r = tb as IMyReactor;
		maxReactorPower += r.MaxOutput;
	}
}

double getCurrentReactorOutput()
{
	double output = 0;
	foreach(var tb in reactorList)
	{
		IMyReactor r = tb as IMyReactor;
		output += r.CurrentOutput;
	}
	return output;
}

#endregion

    }
}