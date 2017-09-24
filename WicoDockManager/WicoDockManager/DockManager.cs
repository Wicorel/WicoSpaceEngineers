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
        #region DOCKING
        List<DockingInfo> dockingInfo = new List<DockingInfo>();

        public class DockingInfo
        {
            public IMyTerminalBlock tb;
            // state = 0 auto-manage
            // state = 1 player reserved
            // state = 2 assigned to incoming ship (entityid)
            public long State;
            public long assignedEntity;
            public List<IMyTerminalBlock> subBlocks;
            public long lAlign;
        }

        List<IMyTerminalBlock> dockingAlllights = new List<IMyTerminalBlock>();

        string initDockingInfo()
        {
            string s = "";

            dockingInfo.Clear();
            dockingAlllights = GetTargetBlocks<IMyLightingBlock>();

            if (localBaseConnectors.Count < 1) s += connectorsInit();

            Echo(localBaseConnectors.Count + " Base Connectors");
            for (int i = 0; i < localBaseConnectors.Count; i++)
            {
                //		IMyShipConnector connector = localBaseConnectors[i]  as IMyShipConnector;
                addDockingInfo(localBaseConnectors[i]);
            }
            // load from text panel...
            s += "DI:" + dockingInfo.Count;
            Echo("EO:iDI()");
            return s;
        }

        void addDockingInfo(IMyTerminalBlock tb, long State = 0)
        {
            List<IMyTerminalBlock> subBlocks = new List<IMyTerminalBlock>();

            DockingInfo di = new DockingInfo();
            di.tb = tb;
            di.State = State;
            di.assignedEntity = 0;
            di.lAlign = -1;

            //	Echo(di.tb.CustomName);

            //	Echo(lights.Count + " lights found");
            for (int i = 0; i < dockingAlllights.Count; i++)
            {
                double distance = (dockingAlllights[i].GetPosition() - di.tb.GetPosition()).Length();
                //		Echo(" " + lights[i].CustomName + ":" + distance.ToString("0.00") + "M");
                if (distance < 3) // should be based on grid size.
                {
                    subBlocks.Add(dockingAlllights[i]);
                }
            }
            //	lights.Clear();
            //	Echo("Found " + subBlocks.Count + " matching lights");
            di.subBlocks = subBlocks;

            string sData = tb.CustomData;
            string[] lines = sData.Trim().Split('\n');
            Echo(lines.Length + " Lines");
            for (int i = 0; i < lines.Length; i++)
            {
                Echo("|" + lines[i].Trim());
                string[] keys = lines[i].Trim().Split('=');
                if (lines[i].ToLower().Contains("align"))
                {

                    if (keys.Length > 1)
                    {
                        long l;
                        if (long.TryParse(keys[1], out l))
                            di.lAlign = l;
                        else Echo("Error Converting" + keys[1]);
                    }
                    else Echo("Error parsing");

                }
            }

            dockingInfo.Add(di);
        }

        Color AVAILABLE_COLOR = new Color(0f, 1.0f, 0.0f);
        Color INUSE_COLOR = new Color(0f, 0f, 1.0f);
        Color ASSIGNED_COLOR = new Color(1f, 0f, 0f);

        void processDockingStates()
        {
            if (dockingInfo.Count == 0)
            {
                //        Echo("No connecters assigned to [BASE]");
                return;
            }
            string output = "Docking Info:\n";

            foreach (var di in dockingInfo)
            {
                Color toSet = AVAILABLE_COLOR;
                float blinkInterval = 0;
                output += gpsName("", di.tb.CustomName) + ":";
                switch (di.State)
                {
                    case 0: // auto-manage

                        if (di.tb is IMyShipConnector)
                        {
                            IMyShipConnector sc = di.tb as IMyShipConnector;
                            if (sc.Status == MyShipConnectorStatus.Connectable)
                            {
                                toSet = INUSE_COLOR;
                                output += "In Range";
                            }
                            else
                            if (sc.Status == MyShipConnectorStatus.Connected)
                            {
                                toSet = INUSE_COLOR;
                                output += "CONNECTED";
                            }
                            else
                            {
                                // connector is available
                                output += "Available";
                            }
                        }
                        break;
                    case 1: // player reserved
                        output += "Player Reserved";
                        toSet = INUSE_COLOR;
                        break;
                    case 2: // assigned to incoming ship
                        output += "Incoming Ship";
                        toSet = ASSIGNED_COLOR;
                        blinkInterval = 0.5f;
                        if (di.tb is IMyShipConnector)
                        {
                            IMyShipConnector connector = di.tb as IMyShipConnector;
                            if (connector.Status == MyShipConnectorStatus.Connected)
                                di.State = 0;
                            if (connector.Status == MyShipConnectorStatus.Connectable)
                                di.State = 0;
                        }
                        break;
                    default:
                        Echo("unkonwn docking state");
                        break;
                }

                foreach (var tb in di.subBlocks)
                {
                    if (tb is IMyLightingBlock)
                    {
                        IMyLightingBlock lb = tb as IMyLightingBlock;
                        lb.Color = toSet;
                        lb.BlinkIntervalSeconds = blinkInterval;
                    }
                }
                output += "\n";
            }
            Echo(output);
        }

        int getAvailableDock(long incomingID)
        {
            int iDock = -1;
            for (int i = 0; i < dockingInfo.Count; i++)
            {
                if (dockingInfo[i].tb is IMyShipConnector)
                {
                    IMyShipConnector connector = dockingInfo[i].tb as IMyShipConnector;
                    if (dockingInfo[i].assignedEntity == incomingID)
                    {
                        if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            dockingInfo[i].assignedEntity = 0;
                            continue;
                        }
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                        {
                            dockingInfo[i].assignedEntity = 0;
                            continue;
                        }
                        iDock = i;
                        break;
                    }
                }
            }
            if (iDock < 0)
            {
                for (int i = 0; i < dockingInfo.Count; i++)
                {
                    if (dockingInfo[i].tb is IMyShipConnector)
                    {
                        // check if available.. 
                        // mark as 'pending' with timeout
                        // dumb selection for now..
                        IMyShipConnector connector = dockingInfo[i].tb as IMyShipConnector;
                        if (dockingInfo[i].State == 0)
                        {
                            if (connector.Status == MyShipConnectorStatus.Connected)
                                continue;
                            if (connector.Status == MyShipConnectorStatus.Connectable)
                                continue;
                            // else found one..
                            iDock = i;
                            //				bFound = true;
                            break;
                        }
                        else if (dockingInfo[i].State == 1)
                            continue;
                        else if (dockingInfo[i].State == 2)
                            continue;
                    }
                }
            }
            return iDock;
        }

        void sendDockInfo(int iDock, long incomingID)
        {
            if (iDock < 0 || iDock >= dockingInfo.Count) return;

            IMyTerminalBlock connector = dockingInfo[iDock].tb;

            Vector3D vPosition = connector.GetPosition();
//            Vector3D vVec = calcBlockForwardVector(connector);
	        MatrixD worldConnectortb = connector.WorldMatrix;

	        Vector3D vVec = worldConnectortb.Forward;
	        vVec.Normalize();

            dockingInfo[iDock].State = 2;
            dockingInfo[iDock].assignedEntity = incomingID;

            Vector3D vAlign;
            MatrixD worldtb = gpsCenter.WorldMatrix;

            vAlign = worldtb.Forward;
            switch (dockingInfo[iDock].lAlign)
            {
                case 0:
                    //			vAlign = worldtb.Forward;
                    break;
                case 1:
                    vAlign = worldtb.Up;
                    break;
                case 2:
                    vAlign = worldtb.Down;
                    break;
                case 3:
                    vAlign = worldtb.Left;
                    break;
                case 4:
                    vAlign = worldtb.Right;
                    break;
                case 5:
                    vAlign = worldtb.Backward;
                    break;

            }

            vAlign.Normalize();

            if (dockingInfo[iDock].lAlign < 0)
            {
                antSend("WICO:DOCK:" + incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName) + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
            }
            else
            {
                antSend("WICO:ADOCK:" + incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName)
                + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec) + ":" + Vector3DToString(vAlign));
            }

        }

        #endregion

        // return true if message processed, else false.
        bool processDockMessage(string sReceivedMessage)
        {
            string[] aMessage = sReceivedMessage.Trim().Split(':');
            if (aMessage.Length > 1)
            {
                if (aMessage[0] != "WICO")
                {
                    Echo("not wico system message");
                    return false;
                }
                if (aMessage.Length > 2)
                {
                    if (aMessage[1] == "DOCK?")
                    {
                        Echo("Docking Request!");

                        sReceivedMessage = ""; // we processed it.

                        int i = -1;
                        long incomingID = 0;
                        bool pOK = false;
                        pOK = long.TryParse(aMessage[3], out incomingID);

                        i = getAvailableDock(incomingID);
                        if (i >= 0 && pOK)
                        {
                            sendDockInfo(i, incomingID);
                        }
                        else
                        {
                            // docking request failed
                            // need to have 'target' for message based on request message.
                            antSend("WICO:DOCKF:" + aMessage[3] + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(gpsCenter.GetPosition()));
                        }
                        return true;
                    }
                }
            }
            return false;
        }

    }
}