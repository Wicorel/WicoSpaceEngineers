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
// SE V1.172
#region gasgens
List<IMyTerminalBlock> gasgenList = new List<IMyTerminalBlock>();
string gasgenInit()
{
	gasgenList.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(gasgenList, (x1 => x1.CubeGrid == Me.CubeGrid));
	return "GG" + gasgenList.Count.ToString("00");
}
#endregion

    }
}