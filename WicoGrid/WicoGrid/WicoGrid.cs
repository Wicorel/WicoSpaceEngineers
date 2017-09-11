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
  
// 04/08: NOFOLLOW for rotors (for print heads)
// 03/10 moved definition of allBlocksCount to serialize. Fixed piston localgrid
// check customdata for contains 01/07/17
// add allBlocksCount
// cross-grid 12/19
// split grids/blocks
#region getgrids
List<IMyTerminalBlock> gtsAllBlocks = new List<IMyTerminalBlock>();

List <IMyCubeGrid> localGrids =new List<IMyCubeGrid>();
List <IMyCubeGrid> remoteGrids =new List<IMyCubeGrid>();
List <IMyCubeGrid> dockedGrids =new List<IMyCubeGrid>();
List <IMyCubeGrid> allGrids =new List<IMyCubeGrid>();


bool calcGridSystemChanged()
{
	List<IMyTerminalBlock> gtsTestBlocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType < IMyTerminalBlock > (gtsTestBlocks);
	if (allBlocksCount != gtsTestBlocks.Count)
	{
		return true;
	}
	return false;
}
string gridsInit()
{
	gtsAllBlocks.Clear();
	allGrids.Clear();
	localGrids.Clear();
	remoteGrids.Clear();
	dockedGrids.Clear();

	GridTerminalSystem.GetBlocksOfType < IMyTerminalBlock > (gtsAllBlocks);
	allBlocksCount = gtsAllBlocks.Count;

	foreach (var block in gtsAllBlocks)
	{
		var grid = block.CubeGrid;
		if (!allGrids.Contains(grid))
		{
			allGrids.Add(grid);
		}
	}
	addGridToLocal(Me.CubeGrid); // the PB is known to be local..  Start there.

	foreach (var grid in allGrids)
	{
		if (localGrids.Contains(grid))
			continue; // already in the list;
		bool bConnected = false;

		List<IMyShipConnector> gridConnectors = new List<IMyShipConnector>();
		GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(gridConnectors, (x1 => x1.CubeGrid == grid));
		foreach (var connector in gridConnectors)
		{
			if (connector.Status==MyShipConnectorStatus.Connected)
			{
				if(localGrids.Contains(connector.OtherConnector.CubeGrid) || remoteGrids.Contains(connector.OtherConnector.CubeGrid))
				{ // if the other connector is connected to an already known grid, ignore it.
					continue;
				}
				if (localGrids.Contains(connector.OtherConnector.CubeGrid))
					bConnected = true;
				else bConnected = false;
			}
		}

		if(bConnected)
		{
			if (!dockedGrids.Contains(grid))
			{
				dockedGrids.Add(grid);
			}

		}
		if (!remoteGrids.Contains(grid))
		{
			remoteGrids.Add(grid);
		}
	}


	string s = "";
	s += "B"+gtsAllBlocks.Count.ToString();
	s += "G"+allGrids.Count.ToString();
	s += "L"+localGrids.Count.ToString();
	s += "D"+dockedGrids.Count.ToString();
	s += "R"+remoteGrids.Count.ToString();

	Echo("Found " + gtsAllBlocks.Count.ToString() + " Blocks");
	Echo("Found " + allGrids.Count.ToString() + " Grids");
	Echo("Found " + localGrids.Count.ToString() + " Local Grids");
	for (int i = 0; i < localGrids.Count; i++) Echo("|"+localGrids[i].CustomName);
	Echo("Found " + dockedGrids.Count.ToString() + " Docked Grids");
	for (int i = 0; i < dockedGrids.Count; i++) Echo("|"+dockedGrids[i].CustomName);
	Echo("Found " + remoteGrids.Count.ToString() + " Remote Grids");
	for (int i = 0; i < remoteGrids.Count; i++) Echo("|"+remoteGrids[i].CustomName);

	return s;
}

void addGridToLocal(IMyCubeGrid grid)
{
	if (grid == null) return;
	if (!localGrids.Contains(grid))
	{
		localGrids.Add(grid);

		addRotorsConnectedToGrids(grid);
		addPistonsConnectedToGrids(grid);
		addGridsToLocalRotors(grid);
		addGridsToLocalPistons(grid);
	}
}

void addRotorsConnectedToGrids(IMyCubeGrid grid)
{ 
	List<IMyMotorStator> gridRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(gridRotors, (x => x.TopGrid == grid));
	foreach (var rotor in gridRotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		addGridToLocal(rotor.CubeGrid);
	}
	List<IMyMotorAdvancedStator> gridARotors = new List<IMyMotorAdvancedStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(gridARotors, (x => x.TopGrid == grid));
	foreach (var rotor in gridARotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		addGridToLocal(rotor.CubeGrid);
	}
}

void addPistonsConnectedToGrids(IMyCubeGrid grid)
{ 
	List<IMyPistonBase> gridPistons = new List<IMyPistonBase>();
	GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(gridPistons, (x => x.TopGrid == grid));
	foreach (var piston in gridPistons)
	{
		addGridToLocal(piston.CubeGrid);
	}
}

void addGridsToLocalRotors(IMyCubeGrid grid)
{
	List<IMyMotorStator> gridRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(gridRotors, (x1 => x1.CubeGrid == grid));
	foreach (var rotor in gridRotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		IMyCubeGrid topGrid = rotor.TopGrid;
		if (topGrid != null && topGrid!=grid)
		{
			addGridToLocal(topGrid);
		}
	}
	gridRotors.Clear();

	List<IMyMotorAdvancedStator> gridARotors = new List<IMyMotorAdvancedStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(gridARotors, (x1 => x1.CubeGrid == grid));
	foreach (var rotor in gridARotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		IMyCubeGrid topGrid = rotor.TopGrid;
		if (topGrid != null && topGrid!=grid)
		{
			addGridToLocal(topGrid);
		}
	}

}
void addGridsToLocalPistons(IMyCubeGrid grid)
{
	List<IMyPistonBase> gridPistons = new List<IMyPistonBase>();
	GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(gridPistons, (x1 => x1.CubeGrid == grid));
	foreach (var piston in gridPistons)
	{
		IMyCubeGrid topGrid = piston.TopGrid;
//		if (topGrid != null) Echo(piston.CustomName + " Connected to grid:" + topGrid.CustomName);
		if (topGrid != null && topGrid != grid)
		{
			if (!localGrids.Contains(topGrid))
			{
				addGridToLocal(topGrid);
			}
		}
	}
}

List <IMyCubeGrid> calculateLocalGrids()
{
	if (localGrids.Count < 1)
	{
		gridsInit();
	}
	return localGrids;
}
List <IMyCubeGrid> calculateDockedGrids()
{
	if (localGrids.Count < 1)
	{
		gridsInit();
	}
	return dockedGrids;
}

bool localGridFilter(IMyTerminalBlock block)
{
	return calculateLocalGrids().Contains(block.CubeGrid);
}

bool dockedGridFilter(IMyTerminalBlock block)
{
	List <IMyCubeGrid> g=calculateDockedGrids();
	if(g==null) return false;
	return g.Contains(block.CubeGrid);
}

#endregion

// 05/12: Fix bug in GetBlocksContains()
// 03/09: Init grids on get if needed
// 02/25: use cached block list from grids
// split code into grids and blocks
#region getblocks
IMyTerminalBlock get_block(string name)
{
    IMyTerminalBlock block;
    block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(name);
    if (block == null)
        throw new Exception(name + " Not Found"); return block;
}

public List<T> GetTargetBlocks<T>(ref List<T> Output, string Keyword = null) where T : class
{
    if (gtsAllBlocks.Count < 1) gridsInit();
    Output.Clear();
    for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) && gtsAllBlocks[e] is T && ((Keyword == null) || (Keyword != null && gtsAllBlocks[e].CustomName.StartsWith(Keyword))))
        {
            Output.Add((T)gtsAllBlocks[e]);
        }
    }
    return Output;
}
public List<IMyTerminalBlock> GetTargetBlocks<T>(ref List<IMyTerminalBlock> Output, string Keyword = null) where T : class
{
    if (gtsAllBlocks.Count < 1) gridsInit();
    Output.Clear();
    for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) && gtsAllBlocks[e] is T && ((Keyword == null) || (Keyword != null && gtsAllBlocks[e].CustomName.StartsWith(Keyword))))
        {
            Output.Add(gtsAllBlocks[e]);
        }
    }
    return Output;
}
public List<IMyTerminalBlock> GetTargetBlocks<T>(string Keyword = null) where T : class
{
    List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
    GetTargetBlocks<T>(ref Output, Keyword);
    return Output;
}
public List<IMyTerminalBlock> GetBlocksContains<T>(string Keyword = null) where T : class
{
    if (gtsAllBlocks.Count < 1) gridsInit();
    List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
    for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) && gtsAllBlocks[e] is T && Keyword != null && (gtsAllBlocks[e].CustomName.Contains(Keyword) || gtsAllBlocks[e].CustomData.Contains(Keyword)))
        {
            Output.Add(gtsAllBlocks[e]);
        }
    }
    return Output;
}
public List<IMyTerminalBlock> GetBlocksNamed<T>(string Keyword = null) where T : class
{
    if (gtsAllBlocks.Count < 1) gridsInit();
    List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
    for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) && gtsAllBlocks[e] is T && Keyword != null && gtsAllBlocks[e].CustomName == Keyword)
        {
            Output.Add(gtsAllBlocks[e]);
        }
    }
    return Output;
}

#endregion


    }
}