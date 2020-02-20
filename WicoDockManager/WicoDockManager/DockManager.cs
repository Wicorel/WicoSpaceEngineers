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

            // TODO:
 //           public string sType; // types of drones supported

            // TODO: 
            public string sDroneName; // name of drone assigned to this dock
        }

        List<IMyTerminalBlock> dockingAlllights = new List<IMyTerminalBlock>();
        IMyBroadcastListener _CON_IGCChannel;
        IMyBroadcastListener _COND_IGCChannel;
        IMyBroadcastListener _BASE_IGCChannel;

        string initDockingInfo()
        {
            string s = "";

            _BASE_IGCChannel = IGC.RegisterBroadcastListener("BASE?");
            _BASE_IGCChannel.SetMessageCallback(_BASE_IGCChannel.Tag);

            _CON_IGCChannel = IGC.RegisterBroadcastListener("CON?");
            _CON_IGCChannel.SetMessageCallback(_CON_IGCChannel.Tag);

            _COND_IGCChannel = IGC.RegisterBroadcastListener("COND?");
            _COND_IGCChannel.SetMessageCallback(_COND_IGCChannel.Tag);

            dockingInfo.Clear();
            dockingAlllights = GetBlocksContains<IMyLightingBlock>(sBaseConnector);
                //GetTargetBlocks<IMyLightingBlock>();

            if (localBaseConnectors.Count < 1) s += connectorsInit();

            Echo(localBaseConnectors.Count + " Base Connectors");
            for (int i = 0; i < localBaseConnectors.Count; i++)
            {
                //		IMyShipConnector connector = localBaseConnectors[i]  as IMyShipConnector;
                addDockingInfo(localBaseConnectors[i]);
            }
            // load from text panel...
            s += "DI:" + dockingInfo.Count;
//            Echo("EO:iDI()");
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

            // TODO: Use MyINI Parsing

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

        int getAvailableDock(long incomingID, string sType, double height, double width, double length)
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

                        // TODO: Check if type is supported
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

        void sendDockInfo(int iDock, long incomingID, string sName, bool bApproach=false)
        {
            if (iDock < 0 || iDock >= dockingInfo.Count) return;

            IMyTerminalBlock connector = dockingInfo[iDock].tb;

            Vector3D vPosition = connector.GetPosition();
	        MatrixD worldConnectortb = connector.WorldMatrix;

	        Vector3D vVec = worldConnectortb.Forward;
	        vVec.Normalize();

            dockingInfo[iDock].State = 2;
            dockingInfo[iDock].assignedEntity = incomingID;
            dockingInfo[iDock].sDroneName = sName;

            Vector3D vAlign;
            MatrixD worldtb;
            if (shipOrientationBlock != null) worldtb = shipOrientationBlock.WorldMatrix;
            else worldtb = Me.WorldMatrix;

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

            if (bApproach)
            {
                Vector3D vApproach = vPosition + vVec * 30;
//                antSend("WICO:CONA:" + incomingID + ":" + connector.EntityId + ":" + Vector3DToString(vApproach));
                antSend("CONA", incomingID + ":" + connector.EntityId + ":" + Vector3DToString(vApproach));
            }
            else
            {
                if (dockingInfo[iDock].lAlign < 0)
                {
//                    antSend("WICO:COND:" + incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName) + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
                    antSend("COND", incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName) + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
                }
                else
                {
//                    antSend("WICO:ACOND:" + incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName)
                    antSend("ACOND", incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName)
                    + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec) + ":" + Vector3DToString(vAlign));
                }
            }

        }

        #endregion


        //
        //  OLD
        //
        // antSend("WICO:DOCK?:" + shipOrientationBlock.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));

        //antSend("WICO:DOCK:" + aMessage[3] + ":" + connector.EntityId + ":" + connector.CustomName + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
        //antSend("WICO:ADOCK:" + incomingID + ":" + connector.EntityId + ":" + connector.CustomName 	+ ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec)+":" + Vector3DToString(vAlign));


        //antSend("WICO:HELLO:" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));

        //antSend("WICO:MOM:" + Me.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition()));

        // TODO:
        //
        // NEW
        //
        // drone request base info
        //antSend("WICO:BASE?:" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));

        // base reponds with BASE information
        //antSend("WICO:BASE:" + Me.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition())XXX

        // name, ID, position, velocity, Jump Capable, Source, Sink
        // source and sink need to have "priorities".  support vechicle can take ore from a miner drone.  and then it can deliver to a base.
        //
        // 

        // Request docking connector
        // 
        // give: base ID for request, drone ship size/type?, source wanted, sink wanted

        //antSend("WICO:CON?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +
        // ship width, height, length
        /*
                        string sMessage = "WICO:CON?:";
                        sMessage += baseIdOf(iTargetBase).ToString() + ":";
                        sMessage += height.ToString("0.0") + "," + width.ToString("0.0") + "," + length.ToString("0.0") + ":";
                        //                    sMessage += shipDim.HeightInMeters() + "," + shipDim.WidthInMeters() + "," + shipDim.LengthInMeters() + ":";
                        sMessage += shipOrientationBlock.CubeGrid.CustomName + ":";
                        sMessage += SaveFile.EntityId.ToString() + ":";
                        sMessage += Vector3DToString(shipOrientationBlock.GetPosition());
                        */
        // NACK response to request
        // approach GPS?
        // Reason:  
        // no available connectors
        // source temp not available
        // no room for sink
        // CONF=CONnector Fail
        //antSend("WICO:CONF:" + droneId +":" + SaveFile.EntityId.ToString(), +":"+ ":"+Vector3DToString(vApproachPosition))


        // ACK response to request
        // approach gps for hold

        // base replies to drone with CONnector Approach
        //antSend("WICO:CONA:" + droneId +":" + SaveFile.EntityId.ToString(), +":"+ ":"+Vector3DToString(vApproachPosition))

        // NOTE: Updates can be send to drone with updated approach position...

        // Drone arrives at dock position
        // then drone asks for docking
        //antSend("WICO:COND?:" + baseId +":" + SaveFile.EntityId.ToString(), +":"+ ":"+Vector3DToString(shipOrientationBlock.GetPosition())
        //antSend("WICO:COND?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +


        // base delays for full stop, opening hangar, etc
        // then sends: CONnector Dock : connector + vector [+ align]  
        //antSend("WICO:COND:" + droneId + ":" + SaveFile.EntityId.ToString() + ":" + connector.EntityId + ":" + connector.CustomName + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
        //antSend("WICO:ACOND:" + droneId + ":" + SaveFile.EntityId.ToString() + ":" + connector.EntityId + ":" + connector.CustomName 	+ ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec)+":" + Vector3DToString(vAlign));

        // Recover All Command
        // all drones should attempt to return to base with jump capability

        // Recover Specific Command
        // base asks drone to return

        bool DockProcessIGCMessage()
        {
            if(_BASE_IGCChannel.HasPendingMessage)
            {
                Echo("Base Request");
                var igcMessage = _BASE_IGCChannel.AcceptMessage();
                string sMessage = (string)igcMessage.Data;
                string[] aMessage = sMessage.Trim().Split(':');
                long incomingID = 0;
                bool pOK = false;
                pOK = long.TryParse(aMessage[0], out incomingID);
                doBaseAnnounce(true);
            }
            if(_CON_IGCChannel.HasPendingMessage)
            {
                Echo("Connector Approach Request!");
                var igcMessage = _CON_IGCChannel.AcceptMessage();
                string sMessage = (string)igcMessage.Data;
                string[] aMessage = sMessage.Trim().Split(':');
                //antSend("WICO:CON?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +
                int iOffset = 0;
                bool pOK = false;
                long baseID = 0;
                pOK = long.TryParse(aMessage[iOffset++], out baseID);
                if (baseID != SaveFile.EntityId)
                {
                    // not our message.  Not Jenny's boat
                    return false;
                }
                string sType = aMessage[iOffset++];
                double height = -1;
                double width = -1;
                double length = -1;
                string[] aSize = sType.Trim().Split(',');
                if (aSize.Length > 2)
                {
                    pOK = double.TryParse(aSize[0], out height);
                    pOK = double.TryParse(aSize[1], out width);
                    pOK = double.TryParse(aSize[2], out length);

                }

                string sDroneName = aMessage[iOffset++];

//                sReceivedMessage = ""; // we processed it.
                int i = -1;
                long incomingID = 0;
                pOK = long.TryParse(aMessage[iOffset++], out incomingID);

                i = getAvailableDock(incomingID, sType, height, width, length);
                if (i >= 0 && pOK)
                {
                    sendDockInfo(i, incomingID, sDroneName, true);
                }
                else
                {
                    // docking request failed
                    // need to have 'target' for message based on request message.
                    //                           antSend("WICO:CONF:" + incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                    antSend("CONF", incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                }

            }
            if(_COND_IGCChannel.HasPendingMessage)
            {
                Echo("Connector Dock Request!");
                var igcMessage = _COND_IGCChannel.AcceptMessage();
                string sMessage = (string)igcMessage.Data;
                string[] aMessage = sMessage.Trim().Split(':');

                int iOffset = 0;

                bool pOK = false;
                long baseID = 0;
                pOK = long.TryParse(aMessage[iOffset++], out baseID);
                if (baseID != SaveFile.EntityId)
                {
                    // not our message.  Not Jenny's boat
                    return false;
                }

//                sReceivedMessage = ""; // we processed it.

                string sType = aMessage[iOffset++];
                double height = -1;
                double width = -1;
                double length = -1;
                string[] aSize = sType.Trim().Split(',');
                if (aSize.Length > 2)
                {
                    pOK = double.TryParse(aSize[0], out height);
                    pOK = double.TryParse(aSize[1], out width);
                    pOK = double.TryParse(aSize[2], out length);

                }

                string sDroneName = aMessage[iOffset++];
                int i = -1;
                long incomingID = 0;
                pOK = long.TryParse(aMessage[iOffset++], out incomingID);
                i = getAvailableDock(incomingID, sType, height, width, length);
                if (i >= 0 && pOK)
                {
                    sendDockInfo(i, incomingID, sDroneName);
                }
                else
                {
                    // docking request failed
                    //                            antSend("WICO:CONF:" + incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(antennaPosition()));
                    antSend("CONF", incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(antennaPosition()));
                }
            }

            return false;
        }

        // return true if message processed, else false.
        bool DockProcessMessage(string sReceivedMessage)
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
                        Echo("[OBSOLETE] Docking Request!");
                    /* OBSOLETE

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
                            antSend("WICO:DOCKF:" + aMessage[3] + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        }
                        return true;
                        */
                    }
                    if (aMessage[1] == "CON?")
                    {
                        Echo("Connector Approach Request!");
        //antSend("WICO:CON?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +

                        bool pOK = false;
                        long baseID = 0;
                        pOK = long.TryParse(aMessage[2], out baseID);
                        if(baseID!=SaveFile.EntityId)
                        {
                            // not our message.  Not Jenny's boat
                            return false;
                        }
                        string sType = aMessage[3];
                        double height = -1;
                        double width = -1;
                        double length = -1;
                        string[]  aSize = sType.Trim().Split(',');
                        if(aSize.Length>2)
                        {
                            pOK = double.TryParse(aSize[0], out height);
                            pOK = double.TryParse(aSize[1], out width);
                            pOK = double.TryParse(aSize[2], out length);
                            
                        }

                        string sDroneName = aMessage[4];

                        sReceivedMessage = ""; // we processed it.
                        int i = -1;
                        long incomingID = 0;
                        pOK = long.TryParse(aMessage[5], out incomingID);

                        i = getAvailableDock(incomingID, sType, height, width, length);
                        if (i >= 0 && pOK)
                        {
                            sendDockInfo(i, incomingID, sDroneName, true);
                        }
                        else
                        {
                            // docking request failed
                            // need to have 'target' for message based on request message.
 //                           antSend("WICO:CONF:" + incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                            antSend("CONF", incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        }
                        return true;
                    }
                    if (aMessage[1] == "COND?")
                    {
                        Echo("Connector Dock Request!");
        //antSend("WICO:COND?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +

                        bool pOK = false;
                        long baseID = 0;
                        pOK = long.TryParse(aMessage[2], out baseID);
                        if(baseID!=SaveFile.EntityId)
                        {
                            // not our message.  Not Jenny's boat
                            return false;
                        }

                        sReceivedMessage = ""; // we processed it.

                        string sType = aMessage[3];
                        double height = -1;
                        double width = -1;
                        double length = -1;
                        string[]  aSize = sType.Trim().Split(',');
                        if(aSize.Length>2)
                        {
                            pOK = double.TryParse(aSize[0], out height);
                            pOK = double.TryParse(aSize[1], out width);
                            pOK = double.TryParse(aSize[2], out length);
                            
                        }

                        string sDroneName = aMessage[4];
                        int i = -1;
                        long incomingID = 0;
                        pOK = long.TryParse(aMessage[5], out incomingID);
                        i = getAvailableDock(incomingID,sType, height, width, length);
                        if (i >= 0 && pOK)
                        {
                            sendDockInfo(i, incomingID, sDroneName);
                        }
                        else
                        {
                            // docking request failed
//                            antSend("WICO:CONF:" + incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(antennaPosition()));
                            antSend("CONF", incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(antennaPosition()));
                        }
                        return true;
                    }
                    if (aMessage[1] == "BASE?")
                    {
        //antSend("WICO:BASE?:" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        Echo("Base Request!");
                        sReceivedMessage = ""; // we processed it.

                        long incomingID = 0;
                        bool pOK = false;
                        pOK = long.TryParse(aMessage[3], out incomingID);

                        doBaseAnnounce(true);
                        return true;
                    }
                }
            }
            return false;
        }

    }
}