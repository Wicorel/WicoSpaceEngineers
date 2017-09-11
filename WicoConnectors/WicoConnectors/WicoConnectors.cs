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

//03/30 Fix connectany to use localdock
//03/27 cache optimizations
// starts looking for "[DOCK]"
// also looks for [BASE] and excludes those
// 01/07/2017
// 01/10 added other connector
// 01/24 1.172 PB API changes
#region connectors
List<IMyTerminalBlock> localConnectors = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> localDockConnectors = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> localBaseConnectors = new List<IMyTerminalBlock>();

bool bConnectorsInit = false;

string connectorsInit()
{
	bConnectorsInit = false;
	localConnectors.Clear();
	localDockConnectors.Clear();
	localBaseConnectors.Clear();
	getLocalConnectors();
	return "CL" + localConnectors.Count.ToString()+ "CD"+localDockConnectors.Count.ToString()+ "CB"+localBaseConnectors.Count.ToString();
}

void getLocalConnectors()
{
	if (localConnectors.Count < 1 && !bConnectorsInit) localConnectors = GetTargetBlocks<IMyShipConnector>();

	if (localDockConnectors.Count < 1 && !bConnectorsInit) localDockConnectors = GetBlocksContains<IMyShipConnector>("[DOCK]");
	if (localDockConnectors.Count < 1 && !bConnectorsInit) localDockConnectors = localConnectors;
	if (localBaseConnectors.Count < 1 && !bConnectorsInit) localBaseConnectors = GetBlocksContains<IMyShipConnector>("[BASE]");
	bConnectorsInit = true;
	return;
}
bool AnyConnectorIsLocked()
{
	getLocalConnectors();

	for (int i = 0; i < localDockConnectors.Count; i++)
	{
		IMyShipConnector sc = localDockConnectors[i] as IMyShipConnector;
		if (sc.Status == MyShipConnectorStatus.Connectable)
//		if (sc.IsLocked)
			return true;
	}
	return false;
}

bool AnyConnectorIsConnected()
{
	getLocalConnectors();

	for (int i = 0; i < localDockConnectors.Count; i++)
	{
		IMyShipConnector sc = localDockConnectors[i] as IMyShipConnector;
		if (sc.Status == MyShipConnectorStatus.Connected)
		{
			IMyShipConnector sco = sc.OtherConnector;
			if (sco.CubeGrid == sc.CubeGrid)
			{
				//Echo("Locked-but connected to 'us'");
				continue;
			}
			else return true;
		}
	}
	return false;
}

IMyTerminalBlock getDockingConnector() // maybe pass in prefered orientation?
{ // dumb mode for now.
	if(localDockConnectors.Count>0)
	{
	Echo("Found local Connector");
		return localDockConnectors[0];
	}
Echo("NO local connectors");
	return null;
}

IMyTerminalBlock getConnectedConnector()
{
	getLocalConnectors();

	for (int i = 0; i < localDockConnectors.Count; i++)
	{
		IMyShipConnector sc = localDockConnectors[i] as IMyShipConnector;
		if (sc.Status == MyShipConnectorStatus.Connected)
//		if (sc.Status == MyShipConnectorStatus.Connected)
		{
			IMyShipConnector sco = sc.OtherConnector;
			if (sco.CubeGrid == sc.CubeGrid)
			{
				//Echo("Locked-but connected to 'us'");
				continue;
			}
			else return sc.OtherConnector;
		}
	}
	return null;

}

void ConnectAnyConnectors(bool bConnect = true, string sAction = "")
{
	getLocalConnectors();
	Echo("CCA:"+ localDockConnectors.Count);
	for (int i = 0; i < localDockConnectors.Count; i++)
	{
		IMyShipConnector sc = localDockConnectors[i] as IMyShipConnector;
		if (sc.Status == MyShipConnectorStatus.Connected)
		{
			IMyShipConnector sco = sc.OtherConnector;
			if (sco.CubeGrid == sc.CubeGrid)
			{
				//Echo("Locked-but connected to 'us'");
				continue; // skip it.
			}
		}
		if (bConnect)
		{
			if (sc.Status == MyShipConnectorStatus.Connectable) sc.ApplyAction("SwitchLock");
		}
		else
		{
			if (sc.Status == MyShipConnectorStatus.Connected) sc.ApplyAction("SwitchLock");
		}

		if (sAction != "")
		{
			ITerminalAction ita;
			ita = sc.GetActionWithName(sAction);
			if (ita != null) ita.Apply(sc);
		}

	}
	return;
}

#endregion



    }
}