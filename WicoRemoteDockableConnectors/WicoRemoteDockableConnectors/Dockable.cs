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
#region docklist

struct DockableConnector
{
	public long EntityId;
	public string sName;
	public Vector3D vPosition;
	public Vector3D vVector;
}

List<DockableConnector> dockableConnectors = new List<DockableConnector>();

bool getAvailableRemoteConnector(out DockableConnector dc)
{
	// get an available remote connector for docking
	// dumb for now:
	DockableConnector nDC=new DockableConnector();
	nDC.EntityId = 0;
	nDC.sName = "";
	dc = nDC;
/* Dont' use saved connectors for NOW. TODO:
	if (dockableConnectors.Count > 0)
	{
		dc = dockableConnectors[0];
		return true;
	}
	else
*/
	{
		Echo("No saved remote connectors available");
		return false;
	}
}

void loadDockableConnectors()
{
	IMyTextPanel mytp;
	List<IMyTerminalBlock> blocks =new List<IMyTerminalBlock>();
	blocks=GetBlocksContains<IMyTextPanel>("[DOCK]");
	if (blocks.Count > 0)
		mytp = blocks[0] as IMyTextPanel;
	else return;

	dockableConnectors.Clear();
	// TODO:
	/*
	{
	DockableConnector dc = new DockableConnector();
	dc.EntityId=id;
	dc.sName = sName;
	dc.vPosition = vPosition;
	dc.vVector = vVec;
	dockableConnectors.Add(dc);

	}
	*/
}

void saveDockableConnectors()
{
	IMyTextPanel mytp;
	List<IMyTerminalBlock> blocks =new List<IMyTerminalBlock>();
	blocks=GetBlocksContains<IMyTextPanel>("[DOCK]");
	if (blocks.Count > 0)
		mytp = blocks[0] as IMyTextPanel;
	else return;

	StringBuilder sb = new StringBuilder();

	sb.Append(dockableConnectors.Count.ToString()+ "\n");
	for(int i=0;i<dockableConnectors.Count;i++)
	{
		sb.Append(dockableConnectors[i].EntityId.ToString());
		sb.Append(":");
		sb.Append(gpsName("",dockableConnectors[i].sName));
		sb.Append(":");
		sb.Append(Vector3DToString(dockableConnectors[i].vPosition));
		sb.Append(":");
		sb.Append(Vector3DToString(dockableConnectors[i].vVector));
		sb.Append("\n");
	}
	mytp.WritePublicText(sb.ToString(), false);
}

void addDockableConnector(IMyTerminalBlock connector)
{
	if (connector == null) return;
	Vector3D vPosition = connector.GetPosition();
//	Vector3D vVec = calcBlockForwardVector(connector);

	MatrixD worldtb = connector.WorldMatrix;

	Vector3D vVec = worldtb.Forward;
	vVec.Normalize();

	addDockableConnector(connector.EntityId,connector.CustomName,vPosition, vVec);
}
void addDockableConnector(long EntityId, string sName, Vector3D vPosition, Vector3D vVec)
{
	for(int i=0;i<dockableConnectors.Count;i++)
	{
		if(dockableConnectors[i].EntityId==EntityId || EntityId==0)
		{
			// already in liast
			Echo("location already in list");
			return;
		}
	}
	DockableConnector dc = new DockableConnector();
	dc.EntityId=EntityId;
	dc.sName = sName;
	dc.vPosition = vPosition;
	dc.vVector = vVec;
	dockableConnectors.Add(dc);
	saveDockableConnectors();
}
#endregion


    }

}