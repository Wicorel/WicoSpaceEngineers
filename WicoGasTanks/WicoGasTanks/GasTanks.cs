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
#region tanks
List<IMyTerminalBlock> tankList = new List<IMyTerminalBlock>();
const int iTankOxygen = 1;
const int iTankHydro = 2;
int iHydroTanks = 0;
int iOxygenTanks = 0;
string tanksInit()
{
	tankList.Clear();

	GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tankList, localGridFilter);
//	Echo("tanksinit found" + tankList.Count.ToString() + " Tanks on localgrid");
	iHydroTanks = 0;
	iOxygenTanks = 0;
	for (int i = 0; i < tankList.Count; ++i)
	{
		if (tankType(tankList[i]) == iTankOxygen) iOxygenTanks++;
		else if (tankType(tankList[i]) == iTankHydro) iHydroTanks++;
	}
	return "T" + tankList.Count.ToString("00");
}
double tanksFill(int iTypes = 0xff)
{
	if (tankList.Count < 1) tanksInit();
	if (tankList.Count < 1) return -1;

	double totalLevel = 0;
	int iTanksCount = 0;
	for (int i = 0; i < tankList.Count; ++i)
	{
		int iTankType = tankType(tankList[i]);
		if ((iTankType & iTypes) > 0)
		{
			IMyGasTank tank = tankList[i] as IMyGasTank;
			float tankLevel = tank.FilledRatio;
			totalLevel += tankLevel;
			iTanksCount++;
		}
	}
	if (iTanksCount > 0)
	{
		return totalLevel / iTanksCount;
	}
	else return -1;
}
int tankType(IMyTerminalBlock theBlock)
{
	if (theBlock is IMyGasTank)
	{
		if (theBlock.BlockDefinition.SubtypeId.Contains("Hydro"))
			return iTankHydro;
		else return iTankOxygen;
	}
	return 0;
}

#endregion

    }
}