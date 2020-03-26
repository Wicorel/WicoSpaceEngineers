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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class BaseConnectors
        {
            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            WicoIGC _wicoIGC;
            WicoElapsedTime _wicoElapsedTime;

            readonly string _BaseTransmit = "BaseTransmit";

            public BaseConnectors(Program program, WicoBlockMaster wbm, WicoIGC wicoIGC, WicoElapsedTime wicoElapsedTime)
            {
                _program = program;
                _wicoBlockMaster = wbm;
                _wicoIGC = wicoIGC;
                _wicoElapsedTime = wicoElapsedTime;

                _program.moduleName += " Base Connectors";
                _program.moduleList += "\nBase Connectors V4.0";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);
                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);
                _program.AddPostInitHandler(PostInitHandler);
//                _program.AddResetMotionHandler(ResetMotionHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);

//                _program._CustomDataIni.Get(sBaseSection, "BaseTransmitWait").ToDouble(dBaseTransmitWait);
//                _program._CustomDataIni.Set(sBaseSection, "BaseTransmitWait", dBaseTransmitWait);

                _wicoIGC.AddPublicHandler("BASE?", BroadcastHandler);
                _wicoIGC.AddPublicHandler("CON?", BroadcastHandler);
                _wicoIGC.AddPublicHandler("COND?", BroadcastHandler);

                // wicoControl.AddModeInitHandler(ModeInitHandler);
                // wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoElapsedTime.AddTimer(_BaseTransmit, 55, BaseTransmitTimerHandler);
                _wicoElapsedTime.StartTimer(_BaseTransmit);
            }
            void ResetMotionHandler(bool bNoDrills=false)
            {
            }

            void LoadHandler(MyIni Ini)
            {
            }

            void SaveHandler(MyIni Ini)
            {
            }

            void PostInitHandler()
            {

            }

            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
            }

            List<IMyTerminalBlock> _localConnectors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> _localDockConnectors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> _localBaseConnectors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> _localEjectors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> _allLights = new List<IMyTerminalBlock>();

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyShipConnector)
                {
                    if (tb.BlockDefinition.SubtypeName == "ConnectorSmall)")
                    {
                        _localEjectors.Add(tb);
                    }
                    else
                    {
                        _localConnectors.Add(tb);
                        if (tb.CustomName.Contains("[DOCK]") || tb.CustomData.Contains("[DOCK]"))
                            _localDockConnectors.Add(tb);
                        if (tb.CustomName.Contains("[BASE]") || tb.CustomData.Contains("[BASE]"))
                            _localBaseConnectors.Add(tb);
                    }
                }
                else if(tb is IMyLightingBlock)
                {
                    _allLights.Add(tb);
                }
            }

            void LocalGridChangedHandler()
            {
                _localEjectors.Clear();
                _localConnectors.Clear();
                _localDockConnectors.Clear();
                _localBaseConnectors.Clear();

                _allLights.Clear();

                dockingInfo.Clear();
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
            }


            void UpdateHandler(UpdateType updateSource)
            {
                _program.Echo("Base Connectors=" + _localBaseConnectors.Count.ToString());
                if (dockingInfo.Count < 1 && _localBaseConnectors.Count > 0)
                {
                    foreach(var tb in _localBaseConnectors)
                    {
                        addDockingInfo(tb);
                    }
                }
                processDockingStates();
            }

            void BroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
                if(msg.Tag=="BASE?")//if (_BASE_IGCChannel.HasPendingMessage)
                {
                    _program.Echo("Base Request");
                    string sMessage = (string)msg.Data;
                    string[] aMessage = sMessage.Trim().Split(':');
                    long incomingID = 0;
                    bool pOK = false;
                    pOK = long.TryParse(aMessage[0], out incomingID);
                    doBaseAnnounce(true);
                }
                if(msg.Tag=="CON?")//if (_CON_IGCChannel.HasPendingMessage)
                {
                    _program.Echo("Connector Approach Request!");
                    string sMessage = (string)msg.Data;
                    string[] aMessage = sMessage.Trim().Split(':');
                    //_program.IGC.SendBroadcastMessage("WICO:CON?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +
                    int iOffset = 0;
                    bool pOK = false;
                    long baseID = 0;
                    pOK = long.TryParse(aMessage[iOffset++], out baseID);
                    if (baseID != _program.Me.EntityId)
                    {
                        // not our message.  Not Jenny's boat
                        _program.Echo("Not our approach request");
                        return;
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
                        _program.Echo("Sending Dock Info");
                        sendDockInfo(i, incomingID, sDroneName, true);
                    }
                    else
                    {
                        _program.Echo("Sending Dock Fail");
                        // docking request failed
                        // need to have 'target' for message based on request message.
                        //                           _program.IGC.SendBroadcastMessage("WICO:CONF:" + incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        _program.IGC.SendBroadcastMessage("CONF", incomingID + ":" + _program.Me.CubeGrid.CustomName + ":" + _program.Me.EntityId.ToString() + ":" + _program.Vector3DToString(_program.Me.GetPosition()));
                    }
                }
                if(msg.Tag=="COND?") //if (_COND_IGCChannel.HasPendingMessage)
                {
                    _program.Echo("Connector Dock Request!");
                    string sMessage = (string)msg.Data;
                    string[] aMessage = sMessage.Trim().Split(':');

                    int iOffset = 0;

                    bool pOK = false;
                    long baseID = 0;
                    pOK = long.TryParse(aMessage[iOffset++], out baseID);
                    if (baseID != _program.Me.EntityId)
                    {
                        // not our message.  Not Jenny's boat
                        return;
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
                        //                            _program.IGC.SendBroadcastMessage("WICO:CONF:" + incomingID + ":" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(antennaPosition()));
                        _program.IGC.SendBroadcastMessage("CONF", incomingID + ":" + _program.Me.CubeGrid.CustomName + ":" + _program.Me.EntityId.ToString() + ":" + _program.Vector3DToString(_program.Me.GetPosition()));
                    }
                }


            }


            /*
             * Specific funtions
             * 
             */


            void BaseTransmitTimerHandler(string sName)
            {
                BaseAnnounce();
            }

            void BaseAnnounce()
            {
                if (dockingInfo.Count > 0)
                {
                    bool bJumpCapable = false;
                    string sname = _program.Me.CubeGrid.CustomName;
                    Vector3D vPosition = _program.Me.CubeGrid.GetPosition();// antennaPosition();

                    _program.IGC.SendBroadcastMessage("BASE", _program.toGpsName("", sname) + ":" + _program.Me.EntityId.ToString() +
                        ":" + _program.Vector3DToString(vPosition) + ":" + bJumpCapable.ToString());
                }
            }

            public void doBaseAnnounce(bool bForceAnnounce = false)
            {
                if (bForceAnnounce)
                {
                    BaseAnnounce();
                    _wicoElapsedTime.ResetTimer(_BaseTransmit);
                }
            }

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
                for (int i = 0; i < _allLights.Count; i++)
                {
                    double distance = (_allLights[i].GetPosition() - di.tb.GetPosition()).Length();
                    //		Echo(" " + lights[i].CustomName + ":" + distance.ToString("0.00") + "M");
                    if (distance < 3) // should be based on grid size.
                    {
                        subBlocks.Add(_allLights[i]);
                    }
                }
                //	lights.Clear();
                //	Echo("Found " + subBlocks.Count + " matching lights");
                di.subBlocks = subBlocks;

                // TODO: Use MyINI Parsing

                string sData = tb.CustomData;
                string[] lines = sData.Trim().Split('\n');
                _program.Echo(lines.Length + " Lines");
                for (int i = 0; i < lines.Length; i++)
                {
                    _program.Echo("|" + lines[i].Trim());
                    string[] keys = lines[i].Trim().Split('=');
                    if (lines[i].ToLower().Contains("align"))
                    {

                        if (keys.Length > 1)
                        {
                            long l;
                            if (long.TryParse(keys[1], out l))
                                di.lAlign = l;
                            else _program.Echo("Error Converting" + keys[1]);
                        }
                        else _program.Echo("Error parsing");

                    }
                }

                dockingInfo.Add(di);
            }

            void sendDockInfo(int iDock, long incomingID, string sName, bool bApproach = false)
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
//                if (shipOrientationBlock != null) worldtb = shipOrientationBlock.WorldMatrix;
//                else
                    worldtb = _program.Me.WorldMatrix;

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
                    //                _program.IGC.SendBroadcastMessage("WICO:CONA:" + incomingID + ":" + connector.EntityId + ":" + Vector3DToString(vApproach));
                    _program.IGC.SendBroadcastMessage("CONA", incomingID + ":" + connector.EntityId + ":" + _program.Vector3DToString(vApproach));
                }
                else
                {
                    if (dockingInfo[iDock].lAlign < 0)
                    {
                        //                    _program.IGC.SendBroadcastMessage("WICO:COND:" + incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName) + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
                        _program.IGC.SendBroadcastMessage("COND", incomingID + ":" + connector.EntityId + ":" + _program.toGpsName("", connector.CustomName) + ":" + _program.Vector3DToString(vPosition) + ":" + _program.Vector3DToString(vVec));
                    }
                    else
                    {
                        //                    _program.IGC.SendBroadcastMessage("WICO:ACOND:" + incomingID + ":" + connector.EntityId + ":" + gpsName("", connector.CustomName)
                        _program.IGC.SendBroadcastMessage("ACOND", incomingID + ":" + connector.EntityId + ":" + _program.toGpsName("", connector.CustomName)
                        + ":" + _program.Vector3DToString(vPosition) + ":" + _program.Vector3DToString(vVec) + ":" + _program.Vector3DToString(vAlign));
                    }
                }

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
                    output += _program.toGpsName("", di.tb.CustomName) + ":";
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
                            _program.Echo("unkonwn docking state");
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
                _program.Echo(output);
            }

        }
    }
}
