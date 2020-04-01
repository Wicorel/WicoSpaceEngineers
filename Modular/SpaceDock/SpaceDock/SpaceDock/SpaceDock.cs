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
        public class SpaceDock
        {
            private Program _program;
            private WicoControl _wicoControl;
            private WicoBlockMaster _wicoBlockMaster;
            private Connectors _connectors;
            private WicoThrusters _thrusters;
            private Antennas _antennas;
            private GasTanks _tanks;
            private WicoGyros _gyros;
            private PowerProduction _power;
            private Timers _timers;
            private WicoIGC _wicoIGC;
            private WicoBases _wicoBases;
            private NavCommon _navCommon;
//            private NavRemote _navRemote;
            private CargoCheck _cargoCheck;

            bool bAutoRelaunch = false;
            bool bAutoRefuel = true;

            Vector3D vDockAlign;
            bool bDoDockAlign = false;
            Vector3D vDock;
            Vector3D vLaunch1;
            Vector3D vHome;
            bool bValidDock = false;
            bool bValidLaunch1 = false;
            bool bValidHome = false;

            long lTargetBase = -1;
            DateTime dtDockingActionStart;

            string sDockingSection = "DOCKING";

            double LaunchMaxVelocity = 20;
            double LaunchDistance = 45;

            const string CONNECTORAPPROACHTAG = "CONA";
            const string CONNECTORDOCKTAG = "COND";
            const string CONNECTORALIGNDOCKTAG = "ACOND";
            const string CONNECTORREQUESTFAILTAG = "CONF";

            public SpaceDock(Program program, WicoControl wc, WicoBlockMaster wbm, WicoThrusters thrusters, Connectors connectors, 
                Antennas ant, GasTanks gasTanks, WicoGyros wicoGyros, PowerProduction pp, Timers tim, WicoIGC iGC,
                WicoBases wicoBases, NavCommon navCommon, CargoCheck cargoCheck)
            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
                _thrusters = thrusters;
                _connectors = connectors;
                _antennas = ant;
                _tanks = gasTanks;
                _gyros = wicoGyros;
                _power = pp;
                _timers = tim;
                _wicoIGC = iGC;
                _wicoBases = wicoBases;
                _navCommon = navCommon;
                _cargoCheck = cargoCheck;


                //                shipController = myShipController;

                _program.moduleName += " Space Dock";
                _program.moduleList += "\nSpaceDock V4";

//                _program._CustomDataIni.Get(sNavSection, "NAVEmulateOld").ToBoolean(NAVEmulateOld);
//                _program._CustomDataIni.Set(sNavSection, "NAVEmulateOld", NAVEmulateOld);

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _wicoIGC.AddUnicastHandler(DockingUnicastHandler);

                // for backward compatibility
                _wicoIGC.AddPublicHandler(CONNECTORAPPROACHTAG, DockingUnicastHandler);
                _wicoIGC.AddPublicHandler(CONNECTORDOCKTAG, DockingUnicastHandler);
                _wicoIGC.AddPublicHandler(CONNECTORALIGNDOCKTAG, DockingUnicastHandler);

                bAutoRefuel=_program._CustomDataIni.Get(sDockingSection, "AutoRefuel").ToBoolean(bAutoRefuel);
                _program._CustomDataIni.Set(sDockingSection, "AutoRefuel", bAutoRefuel);

                bAutoRelaunch=_program._CustomDataIni.Get(sDockingSection, "AutoRelaunch").ToBoolean(bAutoRelaunch);
                _program._CustomDataIni.Set(sDockingSection, "AutoRelaunch", bAutoRelaunch);

            }

            void LoadHandler(MyIni Ini)
            {
                _program._CustomDataIni.Get(sDockingSection, "bDoingDocking").ToBoolean(bDoingDocking);
            }

            void SaveHandler(MyIni Ini)
            {
                _program._CustomDataIni.Set(sDockingSection, "bDoingDocking", bDoingDocking);
                _program.CustomDataChanged();
            }

            /// <summary>
            /// Modes have changed and we are being called as a handler
            /// </summary>
            /// <param name="fromMode"></param>
            /// <param name="fromState"></param>
            /// <param name="toMode"></param>
            /// <param name="toState"></param>
            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if (fromMode == WicoControl.MODE_DOCKED
                    || fromMode == WicoControl.MODE_DOCKING
                    || fromMode == WicoControl.MODE_LAUNCH
                    )
                {
                    _program.wicoGyros.gyrosOff();
                    _program.wicoThrusters.powerDownThrusters();
                }
                if (toMode == WicoControl.MODE_DOCKING)
                    bDoingDocking = true;
                else if (toMode == WicoControl.MODE_GOINGTARGET
                    || toMode == WicoControl.MODE_NAVNEXTTARGET
                    || toMode == WicoControl.MODE_STARTNAV
                    )
                { // don't assume..

                }
                else
                    bDoingDocking = false;

                // need to check if this is us
                if (toMode == WicoControl.MODE_DOCKED
                    || toMode == WicoControl.MODE_DOCKING
                    || toMode == WicoControl.MODE_LAUNCH
                    )
                {
                    _wicoControl.WantOnce();
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_LAUNCH)
                {
                    _wicoControl.WantFast();
                }
                /*
                if (iMode == WicoControl.MODE_RELAUNCH)
                {
                    _wicoControl.WantFast();
                }
                */
                if (iMode == WicoControl.MODE_DOCKING)
                {
                    _wicoControl.SetState(0);
                    _wicoControl.WantFast();
                }
                if (iMode == WicoControl.MODE_DOCKED)
                {
                    _wicoControl.WantFast();
                }
                /*
                if (iMode == WicoControl.MODE_LAUNCHED)
                {
                    _wicoControl.WantFast();
                }
                */
            }
            void LocalGridChangedHandler()
            {
                //               shipController = null;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                string[] varArgs = sArgument.Trim().Split(';');

                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
                    // Commands here:

                }
                if (myCommandLine != null)
                {
                    if (myCommandLine.Argument(0) == "godock")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_DOCKING,0);
                    }
                    if (myCommandLine.Argument(0) == "relaunch")
                    {
                        bAutoRelaunch = !bAutoRelaunch;
                        string s = "AutoRelaunch=" + bAutoRelaunch.ToString();
                        _program.Echo(s);
                        _program.ErrorLog(s);
                        _program._CustomDataIni.Set(sDockingSection, "AutoRelaunch", bAutoRelaunch);
                        _program.CustomDataChanged();
                    }
                    if (myCommandLine.Argument(0) == "refuel")
                    {
                        bAutoRefuel = !bAutoRefuel;
                        string s = "Autorefuel=" + bAutoRefuel.ToString();
                        _program.Echo(s);
                        _program.ErrorLog(s);
                        _program._CustomDataIni.Set(sDockingSection, "AutoRefuel", bAutoRefuel);
                        _program.CustomDataChanged();
                    }
                    if (myCommandLine.Argument(0) == "launch")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_LAUNCH);
                    }
                    for (int arg = 0; arg < myCommandLine.ArgumentCount; arg++)
                    {
                        string sArg = myCommandLine.Argument(arg);
                        // commands here:
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == 0 || iMode == WicoControl.MODE_ATTENTION) return;

                if (iMode == WicoControl.MODE_LAUNCH) { doModeLaunch(); return; }
                if (iMode == WicoControl.MODE_DOCKING) { doModeDocking(); return; }
                if (iMode == WicoControl.MODE_DOCKED) { doModeDocked(); return; }

                // we are NOT DOCKED, DOCKING or LAUNCHING
                if(!bDoingDocking && bAutoRefuel)
                {
                    bool bAirWorthy = DockAirWorthy(false, false);
                    if (!bAirWorthy)
                        _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                }
            }

            bool bDoingDocking = false;

            // CON?                    Echo("Connector Approach Request!");
            // CONF request fail
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

            void DockingUnicastHandler(MyIGCMessage msg)
            {
                // NOTE: Called for ALL received unicast messages
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

//                _program.sMasterReporting+="\nMsg Received:"+msg.Tag;

                if (msg.Tag== CONNECTORAPPROACHTAG && msg.Data is string)
                {
                    if (iMode != WicoControl.MODE_DOCKING) return;
                    if(iState!=120) return;

                    string sMessage = (string)msg.Data;
                    //                    Echo("Received Message=\n" + sMessage);
                    string[] aMessage = sMessage.Trim().Split(':');
                    /*
                    _program.Echo(aMessage.Length + ": Length");
                    for (int i = 0; i < aMessage.Length; i++)
                        _program.Echo(i + ":" + aMessage[i]);
                        */
                    if (aMessage.Length > 1)
                    {
                        _program.Echo("Approach answer!");
                        //antSend("WICO:CONA:" + droneId +":" + SaveFile.EntityId.ToString(), +":"+Vector3DToString(vApproachPosition))
                        int iOffset = 0;

                        long id = 0;
                        long.TryParse(aMessage[iOffset++], out id);
                        if (id == _program.Me.EntityId)
                        {
                            // it's a message for us.
                            //                                    sReceivedMessage = ""; // we processed it.
                            long.TryParse(aMessage[iOffset++], out id);
                            double x, y, z;
                            //                                    int iOff = iOffset++;
                            x = Convert.ToDouble(aMessage[iOffset++]);
                            y = Convert.ToDouble(aMessage[iOffset++]);
                            z = Convert.ToDouble(aMessage[iOffset++]);
                            Vector3D vPosition = new Vector3D(x, y, z);

                            vHome = vPosition;
//                            _program.ErrorLog("CONA: vHome=" + _program.Vector3DToString(vHome));
                            bValidHome = true;
                            //                                        StatusLog("clear", gpsPanel);
                            //                                        debugGPSOutput("Home", vHome);

                            _wicoControl.SetState(150);
                        }
                    }
                    // connector approach reponse found
                }
                if ((msg.Tag == CONNECTORDOCKTAG || msg.Tag == CONNECTORALIGNDOCKTAG) && msg.Data is string)
                {
                    ProcessConnectorDockMsg(msg);
                }
            }

            void ProcessConnectorDockMsg(MyIGCMessage msg)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

//                _program.ErrorLog("Rcvd Message tag:" + msg.Tag);
//                _program.ErrorLog(msg.Data.ToString());
                if (iMode == WicoControl.MODE_DOCKING)
                {
//                    if (iState != 210) return;
                    if ((msg.Tag == CONNECTORDOCKTAG || msg.Tag == CONNECTORALIGNDOCKTAG) && msg.Data is string)
                    {
                        string sMessage = (string)msg.Data;

                        string[] aMessage = sMessage.Trim().Split(':');
                        _program.Echo(aMessage.Length + ": Length");
                        for (int i = 0; i < aMessage.Length; i++)
                            _program.Echo(i + ":" + aMessage[i]);
                        int iOffset = 0;
                        //                                if (aMessage[1] == "DOCK" || aMessage[1] == "ADOCK")
                        //                               if (aMessage[1] == "COND" || aMessage[1] == "ACOND")
                        {
                            _program.Echo("Docking answer!");

                            long id = 0;
                            long.TryParse(aMessage[iOffset++], out id);
                            //                           if (id == SaveFile.EntityId)
                            if (id == _program.Me.EntityId)
                            {
                                // it's a message for us.
                                //                                sReceivedMessage = ""; // we processed it.
                                long.TryParse(aMessage[iOffset++], out id);
                                string sName = aMessage[iOffset++];
                                double x, y, z;
                                //                                        int iOff = 5;
                                x = Convert.ToDouble(aMessage[iOffset++]);
                                y = Convert.ToDouble(aMessage[iOffset++]);
                                z = Convert.ToDouble(aMessage[iOffset++]);
                                Vector3D vPosition = new Vector3D(x, y, z);

                                x = Convert.ToDouble(aMessage[iOffset++]);
                                y = Convert.ToDouble(aMessage[iOffset++]);
                                z = Convert.ToDouble(aMessage[iOffset++]);
                                Vector3D vVec = new Vector3D(x, y, z);
//                                _program.ErrorLog("COND: vVec=" + _program.Vector3DToString(vVec));

                                //                                        if (aMessage[1] == "ACOND")
                                if (msg.Tag == CONNECTORALIGNDOCKTAG)
                                {
                                    x = Convert.ToDouble(aMessage[iOffset++]);
                                    y = Convert.ToDouble(aMessage[iOffset++]);
                                    z = Convert.ToDouble(aMessage[iOffset++]);
                                    vDockAlign = new Vector3D(x, y, z);
                                    bDoDockAlign = true;
                                }
                                vDock = vPosition;
                                vLaunch1 = vDock + vVec * (Math.Min(4,_wicoBlockMaster.LengthInMeters()) * 3);
                                vHome = vDock + vVec * (Math.Min(4, _wicoBlockMaster.LengthInMeters()) * 6);
//                                _program.ErrorLog("COND: vHome=" + _program.Vector3DToString(vHome));
//                                _program.ErrorLog("COND: vLaunch1=" + _program.Vector3DToString(vLaunch1));
//                                _program.ErrorLog("COND: vDock=" + _program.Vector3DToString(vDock));
                                bValidDock = true;
                                bValidLaunch1 = true;
                                bValidHome = true;
                                //                                    StatusLog("clear", gpsPanel);
                                //                                    debugGPSOutput("dock", vDock);
                                //                                    debugGPSOutput("launch1", vLaunch1);
                                //                                    debugGPSOutput("Home", vHome);

                                _wicoControl.SetState(300);
                            }
                        }

                    }
                }
            }

            #region LAUNCH
            /*
                * TODO:
                * 
                * add 'memory' connector like MK3 does
                * 
                * Unload ship when docked..
                * 
                * Load uranium
                * stockpile tanks
                * battery charge
                * 
                * relaunch
                */



            // 0 = master init
            // 1 battery check 30%.  If no batteries->4
            // 2 battery check 80%
            // 3 battery check 100%
            // 4 no battery checks
            List<IMyTerminalBlock> thrustLaunchBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLaunchForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLaunchLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLaunchRightList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLaunchUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLaunchDownList = new List<IMyTerminalBlock>();

            void doModeLaunch()
            {

                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                // todo: waypoint sequence for launch (complicated hangars)
                // todo: test/make work in gravity

//                StatusLog("clear", textPanelReport);
//                StatusLog(moduleName + ":LAUNCH!", textPanelReport);

                _wicoControl.WantMedium();
                if (iState == 0)
                {
//                    StatusLog(DateTime.Now.ToString() + " ACTION: StartLaunch", textLongStatus, true);
//                    StatusLog(moduleName + ":Start Launch", textPanelReport);
                    _timers.TimerTriggers("[LAUNCH]");
                    /*
                                    Echo("#LocalDock=" + localDockConnectors.Count);
                                    for (int i = 0; i < localDockConnectors.Count; i++)
                                    {
                                        Echo(i + ":" + localDockConnectors[i].CustomName);
                                    }
                                    */
                    if (!_connectors.AnyConnectorIsConnected())
                    {
//                        StatusLog("Can't perform action unless docked", textLongStatus, true);
                        _program.ResetMotion();
                        _wicoControl.SetMode(WicoControl.MODE_IDLE);
//                        setMode(MODE_IDLE);
                        return;
                    }
                    else
                    {
                        IMyTerminalBlock dockingConnector = _connectors.GetConnectedConnector(true);
                        //                    Echo("Using Connector=" + dockingConnector.CustomName);

                        _thrusters.ThrustersCalculateOrientation(dockingConnector, ref thrustLaunchForwardList, ref thrustLaunchBackwardList,
                            ref thrustLaunchDownList, ref thrustLaunchUpList,
                            ref thrustLaunchLeftList, ref thrustLaunchRightList);
                    }
                    vDock = _wicoBlockMaster.CenterOfMass();
                    _tanks.TanksStockpile(false);
                    _power.BatterySetNormal();
                    _connectors.TurnEjectorsOff();
                    //                vDock = shipOrientationBlock.GetPosition();
                    _thrusters.powerDownThrusters(); // turns ON all thrusters.
                                                       // TODO: allow for relay ships that are NOT bases..
                    float range = _wicoBases.RangeToNearestBase() + 100f + (float)_wicoBlockMaster.GetShipSpeed() * 5f;
                    _antennas.SetMaxPower(false, range);
                    _wicoControl.SetState(100);
                    return;
                }
                if (_connectors.AnyConnectorIsLocked() || _connectors.AnyConnectorIsConnected())
                {
//                    StatusLog(moduleName + ":Awaiting Disconnect", textPanelReport);
                    _program.Echo("Awaiting Disconnect");
                    _connectors.ConnectAnyConnectors(false, false); // "OnOff_Off");
                    return;
                }
                if (iState == 100)
                {
                    _thrusters.powerUpThrusters(thrustLaunchBackwardList);

                    _wicoControl.SetState(1);
                }

                //            Vector3D vPos = shipOrientationBlock.GetPosition();
                Vector3D vPos = _wicoBlockMaster.CenterOfMass();

                _program.Echo("vDock=" + _program.Vector3DToString(vDock));
                _program.Echo("vPos=" + _program.Vector3DToString(vPos));

                double dist = (vPos - vDock).LengthSquared();
//                StatusLog(moduleName + ":Distance Launched=" + dist.ToString("0.00") + "m", textPanelReport);
//                _program.Echo(moduleName + ":Distance Launched=" + dist.ToString("0.00") + "m");

                if (_wicoBlockMaster.GetShipSpeed() > LaunchMaxVelocity * 0.9)
                {
                    _thrusters.powerDownThrusters(thrustLaunchForwardList);
                    _thrusters.powerDownThrusters(thrustLaunchBackwardList, WicoThrusters.thrustAll, true);
                }
                else if (_wicoBlockMaster.GetShipSpeed() > 2)
                {
                    _thrusters.powerUpThrusters(thrustLaunchBackwardList, 25);
                }
                double stoppingD = _thrusters.calculateStoppingDistance((float)_wicoBlockMaster.GetPhysicalMass(),thrustLaunchBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                if ((dist + stoppingD) > LaunchDistance)
                {
                    _connectors.ConnectAnyConnectors(true, true);
                    _program.ResetMotion();
                    _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                }
            }
            #endregion

            #region DOCKING
            /*
            state
            0 master inint
            100 init antenna power
            110 wait for slow speed
              choose base.  Send request.
            120 wait for reply from base
            125  timeout while waiting for a reply from a base; try to move closer to base ->110
            130 No known bases.  wait for reply

            //antSend("WICO:CON?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +

            Use other connector position and vector for docking
            150	Move to 'wait' location (or current location) ?request 'wait' location? ->175 or ->200
        
            175 do travel to 'base' location  ->200

            200	request available docking connector

            210 wait for available
            250	when available, calculate approach locations
            300  Start:	Move through locations
            'Back' Connector:
            310 NAV move to Home Arrive->340

            340 Delay for motion
            350 slow move rest of way to Home. Arrival->400
            400 NAV move to Launch1
            410 slow move rest of way to Launch1 Arrival->430
            430 Arrived @Launch1 ->450 Reset docking distance check (future checks)
            450, 452 align to dock
                Aligned ->451 If no align, directly->500
            451 align to docking alignment align to dock 
                ->452
            500 'reverse' to dock, aiming our connector at target connector
                    supports 'back' connector
                    supports 'down' connector (kneeling required for wheeled vehicles?)
                    supports 'forward' connector

                    if error with align, etc, ->590
             590 abort dock.  Move away and try again.

            Always:	Lock connector iMode->MODE_DOCKED
                */
            IMyTerminalBlock dockingConnector;
            double dockingLastDistance = -1;

            List<IMyTerminalBlock> thrustDockBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDockForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDockLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDockRightList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDockUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDockDownList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();

            //            IMyBroadcastListener _CONAIGCChannel;
            //            IMyBroadcastListener _CONDIGCChannel;
            //            IMyBroadcastListener _ACONDIGCChannel;

            void doModeDocking()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
//                StatusLog("clear", textPanelReport);
//                StatusLog(moduleName + ":DOCKING!", textPanelReport);
                //            StatusLog(moduleName + ":Docking: iState=" + iState, textPanelReport);
                //            StatusLog(moduleName + ":Docking: iState=" + iState, textLongStatus, true);
                //           _wicoControl.WantFast();
                _program.Echo("DOCKING: state=" + iState);

                _wicoControl.WantSlow();

//                IMySensorBlock sb;

                if (dockingConnector == null) _wicoControl.SetState(0);

                //            sInitResults += "DOCKING: state=" + iState+"\n";

                if (dockingConnector == null) dockingConnector = _connectors.GetDockingConnector();
                if (dockingConnector == null) _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                if (iState == 0)
                {
                    //                sInitResults = "DOCKING: state=" + iState+"\n";
                    if (_connectors.AnyConnectorIsConnected())
                    {
                        _wicoControl.SetMode(WicoControl.MODE_DOCKED);
                        return;
                    }

                    _timers.TimerTriggers("[DOCKING]");

                    _program.wicoThrusters.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    if (dockingConnector == null)// || getAvailableRemoteConnector(out targetConnector))
                    {
                        _program.Echo("No local connector for docking");
                        //                        StatusLog(moduleName + ":No local Docking Connector Available!", textLongStatus, true);
                        // we could check for merge blocks.. or landing gears..
                        //                        sStartupError += "\nNo local Docking Connector Available!";
                        _program.ErrorLog("No Local Connector available");
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        return;
                    }
                    else
                    {
                        _program.ResetMotion(false);

                        _thrusters.ThrustersCalculateOrientation(dockingConnector, ref thrustDockForwardList, ref thrustDockBackwardList,
                            ref thrustDockDownList, ref thrustDockUpList,
                            ref thrustDockLeftList, ref thrustDockRightList);
                        _wicoControl.SetState(100);
                    }
                    lTargetBase = -1;// iTargetBase = -1;
                }


                Vector3D vPos = dockingConnector.GetPosition();
                if (!_connectors.AnyConnectorIsConnected() && _connectors.AnyConnectorIsLocked())
                {
                    _connectors.ConnectAnyConnectors();
                    _program.ResetMotion();
                    _wicoControl.SetMode(WicoControl.MODE_DOCKED);
//                    setMode(MODE_DOCKED);
                    _thrusters.powerDownThrusters(WicoThrusters.thrustAll, true);
                    return;
                }
                if (iState == 100)
                {
                    // TODO: allow for relay ships that are NOT bases..
                    // TODO: if memory docking, don't need to adjust antenna
                    // TODO: if stealth mode, don't mess with antenna
                    float range = _wicoBases.RangeToNearestBase() + 100f + (float)_wicoBlockMaster.GetShipSpeed() * 5f;
                    _antennas.SetMaxPower(false, range);
                    /*
                    if (sensorsList.Count > 0)
                    {
                        sb = sensorsList[0];
                        //			setSensorShip(sb, 1, 1, 1, 1, 50, 1);
                    }
                    */
                    _wicoControl.SetState(110);
                    _wicoControl.WantOnce();
                }
                else if (iState == 110)
                { // wait for slow
                    if (_wicoBlockMaster.GetShipSpeed() < 10)
                    {
                        if (lTargetBase < 0) lTargetBase = _wicoBases.BaseFindBest();
//                        _program.ErrorLog("110: Base=" + lTargetBase);
                        //                   sInitResults += "110: Base=" + iTargetBase;
                        dtDockingActionStart = DateTime.Now;
                        if (lTargetBase >= 0)
                        {
                            OrientedBoundingBoxFaces orientedBoundingBoxFaces=new OrientedBoundingBoxFaces(dockingConnector);
                            Vector3D[] points = new Vector3D[4];
                            orientedBoundingBoxFaces.GetFaceCorners(5, points); // 5 = front
                                                             // front output order is BL, BR, TL, TR
                            double width = (points[0] - points[1]).Length();
                            double height = (points[0] - points[2]).Length();
                            orientedBoundingBoxFaces.GetFaceCorners(0, points);
                            // face 0=right output order is  BL, TL, BR, TR ???
                            double length = (points[0] - points[2]).Length();

                            string sMessage = "";// = "WICO:CON?:";
                            string sTag = "CON?"; // TODO: move to static definitions
                            sMessage += lTargetBase.ToString() + ":";
                            sMessage += height.ToString("0.0") + "," + width.ToString("0.0") + "," + length.ToString("0.0") + ":";
                            sMessage += _program.Me.CubeGrid.CustomName + ":";
                            sMessage += _program.Me.EntityId.ToString() + ":"; // needs to match when receiving messages for 'us'
                            sMessage += _program.Vector3DToString(_wicoBlockMaster.CenterOfMass());

                            _program.IGC.SendBroadcastMessage(sTag, sMessage);// antSend(sMessage);
                                                                              //                        antSend("WICO:CON?:" + baseIdOf(iTargetBase).ToString() + ":" + "mini" + ":" + shipOrientationBlock.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                            _wicoControl.SetState(120);
                        }
                        else // No available base
                        {
                            // try to get a base to respond
//                            _wicoBases.checkBases(true);
                            _wicoBases.checkBases(true);
                            // TODO: Change to elapsedtime handler
                            dtDockingActionStart = DateTime.Now;
                            _wicoControl.SetState(130);
                            //                        setMode(MODE_ATTENTION);
                        }
                    }
                    else
                        _program.ResetMotion();
                }
                else if (iState == 120)
                { // wait for reply from base
//                    StatusLog("Awaiting Response from Base", textPanelReport);

//                    _wicoControl.WantFast();
                    DateTime dtMaxWait = dtDockingActionStart.AddSeconds(5.0f);
                    DateTime dtNow = DateTime.Now;
                    if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                    {
//                        sStartupError += "\nTime out awaiting CONA";
                        _wicoControl.SetState(125);
                        _wicoControl.WantFast();
                        return;
                    }
                    // message handled in message handler (duh)
                    { // uses timeout from above
                        _program.Echo("Awaiting reply message");
                    }
                }
                else if (iState == 125)
                { // timeout waiting for reply from base..
                  // move closer to the chosen base's last known position.
                    if (lTargetBase < 0)
                    {
                        // TODO: remove base from list and try again.  ATTENTION if no remaining bases
                        //                        sStartupError += "\nNo Base in range";
                        _program.ErrorLog("No Base in range");
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        return;
                    }
                    else if (_wicoBases.RangeToNearestBase() < 3000)
                    {
                        // we think we are close enough
                        // force recheck
                        //                    sStartupError += "\nForce Recheck";
                        lTargetBase = -1;
                        _wicoBases.checkBases(false);
//                        _wicoBases.checkBases(true);
                        _wicoControl.SetState(110);
                    }
                    else
                    {
                        // get closer
                        //                    sStartupError += "\nGet Closer";
                        _wicoControl.SetState(126);
                        _wicoControl.WantFast();
                        _navCommon.NavGoTarget(_wicoBases.BasePositionOf(lTargetBase), iMode, 110, 3100, "DOCK Base Proximity");
                    }
                    //                doTravelMovement(BasePositionOf(lTargetBase), 3100, 110, 106);
                }
                else if (iState == 126)
                {
                    // we are waiting for NAV module to get message and start
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 130)
                {
                    // no known bases. requested response. wait for a while to see if we get one
//                    StatusLog("Trying to find a base", textPanelReport);
//                    bWantFast = false;
                    DateTime dtMaxWait = dtDockingActionStart.AddSeconds(5.0f);
                    DateTime dtNow = DateTime.Now;
                    if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                    {
                        _program.ErrorLog("Timeout finding base");
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        return;
                    }
                    if (_wicoBases.BaseFindBest() >= 0)
                        _wicoControl.SetState(110);
                }

                else if (iState == 150)
                { //150	Move to 'approach' location (or current location) ?request 'wait' location?
                    _wicoControl.SetState(175);
                    _wicoControl.WantFast();
                    /*
                    if (bValidHome)
                    {
                        double distancesqHome = Vector3D.DistanceSquared(vHome, shipOrientationBlock.GetPosition());
                        if (distancesqHome > 25000) // max SG antenna range //TODO: get max from antenna module
                        {
                            _wicoControl.SetState(175;
                        }
                        else _wicoControl.SetState(200;
                    }
                    else _wicoControl.SetState(200;
                    */
                }
                else if (iState == 175)
                { // get closer to approach location
                    _navCommon.NavGoTarget(vHome, iMode, 200, 5, "DOCK Base Approach");
                    _wicoControl.SetState(176);
                }
                else if (iState == 176)
                {
                    // we are waiting for NAV module to get message and start
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 200)
                {//200	Arrived at approach location
                 // request available docking connector
//                    StatusLog("Requsting Docking Connector", textPanelReport);
                    if (_wicoBlockMaster.GetShipSpeed() < 1)
                    {

                        OrientedBoundingBoxFaces orientedBoundingBoxFaces = new OrientedBoundingBoxFaces(dockingConnector);
                        Vector3D[] points = new Vector3D[4];
                        orientedBoundingBoxFaces.GetFaceCorners(5, points); // 5 = front
                                                         // front output order is BL, BR, TL, TR
                        double width = (points[0] - points[1]).Length();
                        double height = (points[0] - points[2]).Length();
                        orientedBoundingBoxFaces.GetFaceCorners(0, points);
                        // face 0=right output order is  BL, TL, BR, TR ???
                        double length = (points[0] - points[2]).Length();

                        string sMessage = "";// "WICO:COND?:";
                        string sTag = "COND?";
                        sMessage += lTargetBase.ToString() + ":";
                        sMessage += height.ToString("0.0") + "," + width.ToString("0.0") + "," + length.ToString("0.0") + ":";
                        //                    sMessage += shipDim.HeightInMeters() + "," + shipDim.WidthInMeters() + "," + shipDim.LengthInMeters() + ":";
                        sMessage += _program.Me.CubeGrid.CustomName + ":";
                        sMessage += _program.Me.EntityId.ToString() + ":";
                        sMessage += _program.Vector3DToString(_wicoBlockMaster.CenterOfMass());
                        _program.IGC.SendBroadcastMessage(sTag, sMessage);// antSend(sMessage);

                        //                    antSend("WICO:COND?:" + baseIdOf(iTargetBase) + ":" + "mini" + ":" + shipOrientationBlock.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        {
                            dtDockingActionStart = DateTime.Now;
                            _wicoControl.SetState(210);
                        }
                    }
                    else _program.ResetMotion();
                }
                else if (iState == 210)
                { //210	wait for available connector
//                    StatusLog("Awaiting reply with Docking Connector", textPanelReport);
//                    bWantFast = false;
                    DateTime dtMaxWait = dtDockingActionStart.AddSeconds(5.0f);
                    DateTime dtNow = DateTime.Now;
                    if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                    {
//                        sStartupError += "\nTime out awaiting COND";
                        _wicoControl.SetState(100);
                        return;
                    }
                    /*
                     * get a saved connector... from hard docking, etc.
                    if (getAvailableRemoteConnector(out targetConnector))
                    {
                        _wicoControl.SetState(250;
                    }
                    else
                    */
                    {
                        { // uses timeout from above
                            _program.Echo("Awaiting reply message");
                        }
                    }
                }
                else if (iState == 250)
                { //250	when available, calculate approach locations from a saved targetconnector
                    /*
                    vDock = targetConnector.vPosition;
                    vLaunch1 = vDock + targetConnector.vVector * (shipDim.LengthInMeters() * 1.5);
                    vHome = vDock + targetConnector.vVector * (shipDim.LengthInMeters() * 3);
                    bValidDock = true;
                    bValidLaunch1 = true;
                    bValidHome = true;

                    _wicoControl.SetState(300;
//                    StatusLog("clear", gpsPanel);
//                    debugGPSOutput("dock", vDock);
//                    debugGPSOutput("launch1", vLaunch1);
//                    debugGPSOutput("Home", vHome);
                    thrusters.MoveForwardSlowReset();

                    _wicoControl.WantFast();
                    */
                }
                else if (iState == 300)
                { //300  Start:	Move through locations
                    _wicoControl.SetState(310);
                    _thrusters.MoveForwardSlowReset();
                    //                iDockingPushCount = 0;
                    _wicoControl.WantFast();
                }
                else if (iState == 310)
                { //	310 move to home
                    _program.Echo("Moving to Home");
                    //		if(iPushCount<60) iPushCount++;
                    //		else

                    _wicoControl.SetState(311);
                    _navCommon.NavGoTarget(vHome, iMode, 340, 3, "DOCK Home",10);
                }
                else if (iState == 311)
                {
                    // we are waiting for NAV module to get message and start
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 340)
                { // arrived at 'home' from NAV
                    _program.ResetMotion();
                    _program.Echo("Waiting for ship to stop");
                    _connectors.TurnEjectorsOff();
                    _thrusters.MoveForwardSlowReset();
                    //                iDockingPushCount = 0;
                    if (_wicoBlockMaster.GetShipSpeed() < 0.1f)
                    {
                        _wicoControl.WantFast();
                        _wicoControl.SetState(350);
                    }
                    else
                    {
                        _wicoControl.WantMedium();
                        //                    bWantFast = false;
                    }
                }
                else if (iState == 350)
                {
                    // move connector closer to home
                    /*
                    double distanceSQ = (vHome - _wicoBlockMaster.CenterOfMass()).LengthSquared();
                    _program.Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                    double stoppingDistance = _thrusters.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(), thrustBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                    _program.Echo("blockmult=" + _wicoBlockMaster.BlockMultiplier());
                    if (distanceSQ > _wicoBlockMaster.BlockMultiplier() * 3)
                    {
                        _thrusters.MoveForwardSlow(3, 5, thrustForwardList, thrustBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                        _wicoControl.WantMedium();
                    }
                    else
                    */
                    {
                        _program.ResetMotion();
                        _timers.TimerTriggers("[DOCKING:APPROACH]");
                        _thrusters.MoveForwardSlowReset();
                        _wicoControl.SetState(400);
                        _wicoControl.WantFast();
                    }
                }
                else if (iState == 400)
                {
                    // move to Launch1
                    _program.Echo("Moving to Launch1");

                    _navCommon.NavGoTarget(vLaunch1, iMode, 410, 3, "DOCK Connector Entry");
                    _wicoControl.SetState(401);
                }
                else if (iState == 401)
                {
                    // we are waiting for NAV module to get message and start
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 410)
                {
                    // move closer to Launch1
                    double distanceSQ = (vLaunch1 - _wicoBlockMaster.CenterOfMass() ).LengthSquared();
                    _program.Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                    double stoppingDistance = _thrusters.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(), thrustBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                    if (distanceSQ > _wicoBlockMaster.BlockMultiplier() * 3)
                    {
                        _thrusters.MoveForwardSlow(3, 5, thrustForwardList, thrustBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                        
                        _wicoControl.WantMedium();
                    }
                    else
                    {
                        _program.ResetMotion();
                        _thrusters.MoveForwardSlowReset();
                        _wicoControl.SetState(430);
                        _wicoControl.WantFast();
                    }
                }
                else if (iState == 430)
                {
                    // arrived at launch1
                    _wicoControl.WantFast();
                    dockingLastDistance = -1;
                    _wicoControl.SetState(450);
                    // TODO: do/waitfor mechanical changes needed for docking
                }
                else if (iState == 450 || iState == 452)
                { //450 452 'reverse' to dock, aiming connector at dock location
                  // align to docking alignment if needed
//                    StatusLog("Align Up to Docking Connector", textPanelReport);
                    _wicoControl.WantFast();
                    //                turnEjectorsOff();
                    if (!bDoDockAlign)
                    {
                        _wicoControl.SetState(500);
                        return;
                    }
                    _program.Echo("Aligning to dock");
                    bool bAimed = false;
                    _gyros.SetMinAngle(0.03f);

                    // TODO: need to change direction if non- 'back' connector
                    bAimed = _gyros.AlignGyros("up", vDockAlign, _wicoBlockMaster.GetMainController());
                    _wicoControl.WantFast();
                    if (iState == 452) _wicoControl.SetState(500);
                    else if (bAimed) _wicoControl.SetState(451); ; // 450->451 
                }
                else if (iState == 451)
                { //451 align to dock
//                    StatusLog("Align to Docking Connector", textPanelReport);
                    _wicoControl.WantFast();
                    Vector3D vTargetLocation = vDock;
                    Vector3D vVec = vTargetLocation - dockingConnector.GetPosition();

                    if (!bDoDockAlign)
                        _wicoControl.SetState(452);

                    //		Vector3D vTargetLocation = shipOrientationBlock.GetPosition() +vDockAlign;
                    //		Vector3D vVec = vTargetLocation - shipOrientationBlock.GetPosition();
                    _program.Echo("Aligning to dock");
                    bool bAimed = false;
                    _gyros.SetMinAngle(0.03f);
                    bAimed = _gyros.AlignGyros("forward", vVec, dockingConnector);
                    if (bAimed) _wicoControl.SetState(452);
                    else _wicoControl.WantFast();

                }
                else if (iState == 500)
                { //500 'reverse' to dock, aiming connector at dock location (really it's connector-forward)
                  // TODO: needs a time-out for when misaligned or base connector moves.
                  //               _wicoControl.WantFast();
                    //StatusLog("Reversing to Docking Connector", textPanelReport);
                    _program.Echo("bDoDockAlign=" + bDoDockAlign);
                    //                StatusLog(moduleName + ":Docking: Reversing to dock! Velocity=" + wicoBlockMaster.GetShipSpeed().ToString("0.00"), textPanelReport);
                    _program.Echo("Reversing to Dock");
// CHECK HERE IF DOCKING SPAZZES                    CTRL_COEFF = 0.75;
                    _gyros.SetMinAngle(0.01f);

                    Vector3D vTargetLocation = vDock;
                    Vector3D vVec = vTargetLocation - dockingConnector.GetPosition();
                    double distance = vVec.Length();
                    _program.Echo("distance=" + _program.niceDoubleMeters(distance));
                    _program.Echo("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
//                    StatusLog("Distance=" + niceDoubleMeters(distance), textPanelReport);
//                    StatusLog("Velocity=" + niceDoubleMeters(wicoBlockMaster.GetShipSpeed()) + "/s", textPanelReport);

                    if (dockingLastDistance < 0) dockingLastDistance = distance;
                    if (dockingLastDistance < distance)
                    {
                        // we are farther away than last time... something is wrong..
                        //                    sStartupError += "\nLast=" + niceDoubleMeters(dockingLastDistance) + " Cur=" + niceDoubleMeters(distance);
                        _wicoControl.SetState(590);
                    }
                    if (distance > 10)
                        _gyros.SetMinAngle(0.03f);
                    else
                        _gyros.SetMinAngle(0.05f);

                    //                debugGPSOutput("DockLocation", vTargetLocation);

                    bool bAimed = false;
                    /*
                            if ((craft_operation & CRAFT_MODE_SLED) > 0)
                            {
                                double yawangle = CalculateYaw(vTargetLocation, dockingConnector);
                                DoRotate(yawangle, "Yaw");
                                if (Math.Abs(yawangle) < .05) bAimed = true;
                            }
                            else
                    */
                    if (distance > 15)
                        bAimed = _gyros.BeamRider(vLaunch1, vDock, dockingConnector);
                    else
                        bAimed = _gyros.AlignGyros("forward", vVec, dockingConnector);

                    /*
                    double maxThrust = calculateMaxThrust(thrustDockForwardList);
                    MyShipMass myMass;
                    myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
                    double effectiveMass = myMass.PhysicalMass;
                    double maxDeltaV = (maxThrust) / effectiveMass;
                    if (iDockingPushCount < 1)
                    {
                        if (maxDeltaV < 2)
                            iDockingPushCount = 75;
                        else if (maxDeltaV < 5)
                            iDockingPushCount = 25;
                    }
                    */
                    //               Echo("dockingPushCount=" + iDockingPushCount);
                    // TODO: if we aren't moving and dockingpushcount>100, then we need to wiggle.

                    if (bAimed)
                    {
                        // we are aimed at location
                        _program.Echo("Aimed");
                        if (distance > 15)
                        {
                            _wicoControl.WantMedium();
                            _program.Echo(">15");
                            _thrusters.MoveForwardSlow(5, 10, thrustDockForwardList, thrustDockBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                            /*
                            if (wicoBlockMaster.GetShipSpeed() < .5)
                            {
                                iDockingPushCount++;
                                powerUpThrusters(thrustDockForwardList, 25 + iDockingPushCount);
                            }
                            else if (wicoBlockMaster.GetShipSpeed() < 5)
                            {
                                powerUpThrusters(thrustDockForwardList, 1);
                            }
                            else
                                powerDownThrusters(thrustAllList);
                                */
                        }
                        else
                        {
                            _program.Echo("<=15");
                            _wicoControl.WantFast();
                            _thrusters.MoveForwardSlow(.5f, 1.5f, thrustDockForwardList, thrustDockBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                            /*
                            if (wicoBlockMaster.GetShipSpeed() < .5)
                            {
                                iDockingPushCount++;
                                powerUpThrusters(thrustDockForwardList, 25 + iDockingPushCount);
                            }
                            else if (wicoBlockMaster.GetShipSpeed() < 1.4)
                            {
                                powerUpThrusters(thrustDockForwardList, 1);
                                if (iDockingPushCount > 0) iDockingPushCount--;
                            }
                            else
                                powerDownThrusters(thrustAllList);
                                */
                        }
                    }
                    else
                    {
                        _program.Echo("Aiming");
                        _thrusters.powerDownThrusters();
                        _wicoControl.WantFast();
                    }
                }
                else if (iState == 590)
                {
                    // abort dock and try again
                    _program.ResetMotion();
                    Vector3D vVec = vDock - dockingConnector.GetPosition();
                    double distance = vVec.Length();
                    if (distance > _wicoBlockMaster.LengthInMeters() * 1.25)
                    {
                        // we are far enough away.  Try again
                        _wicoControl.SetState(0);
                        _wicoControl.WantFast();
                        return;
                    }
                    bool bAimed = _gyros.AlignGyros("forward", vVec, dockingConnector);
                    if (!bAimed) _wicoControl.WantFast();
                    else _wicoControl.WantMedium();
                    _thrusters.MoveForwardSlow(5, 10, thrustDockBackwardList, thrustDockForwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                }
            }
            #endregion

            #region DOCKED
            /*
              * TODO:
              * 
              * add 'memory' connector like MK3 does
              * 
              * Unload ship when docked..
              * 
              * Load uranium
              * stockpile tanks
              * battery charge
              * 
              * relaunch
              */



            // 0 = master init
            // 1 battery check 30%.  If no batteries->4
            // 2 battery check 80%
            // 3 battery check 100%
            // 4 no battery checks

            void doModeDocked()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

//                StatusLog("clear", textPanelReport);
//                StatusLog(moduleName + ":DOCKED!", textPanelReport);
                _program.Echo("Docked!");
                _program.Echo("Autorelaunch=" + bAutoRelaunch.ToString());

                _wicoControl.WantSlow();
                //TODO: autounload

                if (bAutoRelaunch)
                {
                    _program.Echo("Docked. Checking Relaunch");
                    if (DockAirWorthy())
                    {
                        _program.Echo("RELAUNCH!");
                        _wicoControl.SetMode(WicoControl.MODE_LAUNCH);
                        return;
                    }
                    else
                    {
                        _program.Echo(" Awaiting Relaunch Criteria");
//                        StatusLog("Awaiting Relaunch Criteria", textPanelReport);
                        //                    if (!BatteryGo)
                        {
//                            StatusLog(" Battery " + batteryPercentage + "% (" + batterypcthigh + "%)", textPanelReport);
                            _program.Echo(" Battery " + _power.batteryPercentage + "% (" + _power.batterypcthigh + "%)");
                        }
                        //                   if(!CargoGo)
                        {
                            //                            StatusLog(" Cargo: " + cargopcent + "% (" + cargopctmin + ")", textPanelReport);
                            _program.Echo(" Cargo: " + _cargoCheck.cargopcent + "% (" + _cargoCheck.cargopctmin + ")");
                        }
                        if (_tanks.HasHydroTanks())
                        {
//                            StatusLog(" Hydro: " + hydroPercent + "% (" + cargopctmin + ")", textPanelReport);
                            _program.Echo(" Hydro: " + _tanks.hydroPercent + "% (" + _cargoCheck.cargopctmin + ")");
                        }
                    }
                }
                if (!_connectors.AnyConnectorIsConnected())
                {
                    // we magically got disconnected..
                    // assume user did it.
                    _wicoControl.SetMode(WicoControl.MODE_IDLE);

                    _thrusters.powerDownThrusters(); // turn thrusters ON
//                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        _tanks.TanksStockpile(false); // turn tanks ON
                                                                                           // TODO: allow for relay ships that are NOT bases..
                    float range = _wicoBases.RangeToNearestBase() + 100f + (float)_wicoBlockMaster.GetShipSpeed() * 5f;
                    _antennas.SetMaxPower(false, range);
                    _power.BatterySetNormal();
                }
                else
                {

//                    StatusLog(moduleName + ":Power Saving Mode", textPanelReport);
                    _program.Echo("Power Saving Mode");
                    if (iState == 0)
                    {
//                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                            _tanks.TanksStockpile(true);

                        // make a 'lower power' handler?
                        _thrusters.powerDownThrusters(WicoThrusters.thrustAll, true);
                        _antennas.SetLowPower();
//                        SensorsSleepAll();
                        // TODO: ??? turn gyos off?

                        _power.BatteryCheck(0, true);
                        _timers.TimerTriggers("[DOCKED]");
                        _wicoControl.SetState(1);
                    }
                    else if (iState == 1)
                    {
                        _power.BatteryCheck(0, true);
//                        if (batteryPercentage < 0 || (craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0)
//                            _wicoControl.SetState(4; // skip battery checks
//                        else 
                            if (!_power.BatteryCheck(30, true))
                                _wicoControl.SetState(2);
                    }
                    else if (iState == 2)
                    {
                        if (!_power.BatteryCheck(80, true))
                            _wicoControl.SetState(3);
                    }
                    else if (iState == 3)
                    {
                        if (!_power.BatteryCheck(100, true))
                            _wicoControl.SetState(1); // go back and check again
                    }
                    else //state 4
                    {
                        _power.BatteryCheck(0, true); //,textBlock);
                    }

                    // all states
                    {
                        //                    if (bAutoRelaunch)
                        {
                            _cargoCheck.doCargoCheck();
                            _tanks.TanksCalculate();
                        }

//                        if (power.batteryPercentage >= 0) StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
 //                       else _program.Echo("No Batteries");
                        if (_tanks.oxyPercent >= 0)
                        {
//                            StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                            //Echo("O:" + oxyPercent.ToString("000.0%"));
                        }
                        else _program.Echo("No Oxygen Tanks");

                        if (_tanks.hydroPercent >= 0)
                        {
//                            StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                            // TODO: use setting for 'low' (and 'enough')
//                            if (hydroPercent < 0.20f)
//                                StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);

                            _program.Echo("H:" + (_tanks.hydroPercent * 100).ToString("000.0%"));
                        }
                        else _program.Echo("No Hydrogen Tanks");
                        if (_power.batteryPercentage >= 0 && _power.batteryPercentage < _power.batterypctlow)
                        {
//                            StatusLog(" WARNING: Low Battery Power", textPanelReport);
                        }

                        // TODO: get uranium into reactors; take out excess ingots; turn off conveyor usage (like TIM)
                        // TODO: get ore OUT of ship and into base (including stone)
                        // TODO: Handle ore carrier/shuttle
                    }
                }
            }

            double _airworthyChecksElapsedMs = -1;
            bool DockAirWorthy(bool bForceCheck = false, bool bLaunchCheck = true, int cargohighwater = 1)
            {
                bool BatteryGo = true;
                bool TanksGo = true;
                bool ReactorsGo = true;
                bool CargoGo = true;

                if (_airworthyChecksElapsedMs >= 0)
                    _airworthyChecksElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                bool bDoChecks = bForceCheck;
                if (_airworthyChecksElapsedMs < 0 || _airworthyChecksElapsedMs > 0.5 * 1000)
                {
                    _airworthyChecksElapsedMs = 0;
                    bDoChecks = true;
                }

                // Check battery charge
                if (bDoChecks) _power.BatteryCheck(0, false);
                if (bLaunchCheck)
                {
                    if (_power.batteryPercentage >= 0 && _power.batteryPercentage < _power.batterypcthigh)
                    {
//                        _program.ErrorLog("Battery not airworthy (launch)");
                        BatteryGo = false;
                    }

                }
                else
                {
                    // check if we need to go back and refill
                    if (_power.batteryPercentage >= 0 && _power.batteryPercentage < _power.batterypctlow)
                    {
//                        _program.ErrorLog("Battery not airworthy");
                        BatteryGo = false;
                    }
                }

                // check cargo emptied
                if (bDoChecks) _cargoCheck.doCargoCheck();
                if (bLaunchCheck)
                {
                    if (_cargoCheck.cargopcent > _cargoCheck.cargopctmin)
                    {
//                        _program.ErrorLog("Cargo not airworthy (launch)");
                        CargoGo = false;
                    }
                }
                else
                {
                    if (_cargoCheck.cargopcent > cargohighwater)
                    {
//                        _program.ErrorLog("Cargo not airworthy");
                        CargoGo = false;
                    }
                }
                // TODO: Check H2 tanks
                if (bDoChecks) _tanks.TanksCalculate();
                if (bLaunchCheck)
                {
                    if (_tanks.HasHydroTanks() && _tanks.hydroPercent * 100 < _tanks.tankspcthigh)
                    {
//                        _program.ErrorLog("Tanks not airworthy (launch) "+_tanks.hydroPercent.ToString("0.00"));
                        TanksGo = false;
                    }
                }
                else
                {
                    if (_tanks.HasHydroTanks() && _tanks.hydroPercent * 100 < _tanks.tankspctlow)
                    {
//                        _program.ErrorLog("Tanks not airworthy " + _tanks.hydroPercent.ToString("0.00"));
                        TanksGo = false;
                    }
                }
                // TODO: check reactor fuel

                if (BatteryGo && TanksGo && ReactorsGo && CargoGo)
                {
                    return true;
                }
                else return false;

            }

            #endregion

        }
    }
}
