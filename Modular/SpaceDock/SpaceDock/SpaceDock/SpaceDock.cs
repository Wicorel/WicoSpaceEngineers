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
        /*
         * Bugs:
         * Got to ATTENTION on first dock attempt after recompile/(reload?): timeout is for NAV..
         * 
         * 
         * TODO:
         *   get list of docks and allow player selection
         *   Say what we need when requesting dock (dump ore, get hydrogen, power, etc
         *   remember assigned connector and just return to it (old dock method)
         *   save orientation... All docks should be oriented...
         *   
         *    
         */
        public class SpaceDock
        {
            private Program _program;
            private WicoControl _wicoControl;
            private WicoBlockMaster _wicoBlockMaster;
            private WicoElapsedTime _wicoElapsedTime;
//            private Connectors _connectors;
//            private WicoThrusters _thrusters;
            private Antennas _antennas;
//            private GasTanks _tanks;
//            private WicoGyros _gyros;
//            private PowerProduction _power;
            private Timers _timers;
            private WicoIGC _wicoIGC;
            private WicoBases _wicoBases;
            private NavCommon _navCommon;
//            private CargoCheck _cargoCheck;
            private Displays _displays;

            private SystemsMonitor _systemsMonitor;

            Vector3D vDockAlign;
            bool bDoDockAlign = false;
            Vector3D vDock;
            Vector3D vLaunch1;
            Vector3D vHome;
//            bool bValidDock = false;
            bool bValidLaunch1 = false;
            bool bValidHome = false;

            public bool _Debug = false;

            bool bAutoRefuel = true;

            // todo: Move to wicobases...  this is our 'current' base.
            long lTargetBase = -1;
            //            DateTime dtDockingActionStart;
            const string DockingAction = "DockingAction"; // ET timer name

            string sDockingSection = "DOCKING";

            double LaunchMaxVelocity = 20;
            double LaunchDistance = 45;

            const string CONNECTORAPPROACHTAG = "CONA";
            const string CONNECTORDOCKTAG = "COND";
            const string CONNECTORALIGNDOCKTAG = "ACOND";
            const string CONNECTORREQUESTFAILTAG = "CONF";
            const string CONNECTORREQUEST = "CON?";
            const string CONNECTORDOCKREQUEST = "COND?";

            /*
             * TODO:
             * Add 'mother' ship and keep it
             * Add remembered connector
             * Support atmo docking
             * 
            * (dock) set home dock
            * (dock) forget home dock
            * (dock) set fixed approach location (V1 'home')
            * 
             * Add reasons for docking request (need power, need hydro, dump ore, etc)
             * 
             * 
             */
            public SpaceDock(Program program, WicoControl wc, WicoBlockMaster wbm
                , WicoElapsedTime wicoET
//                , WicoThrusters thrusters
//                , Connectors connectors
                ,Antennas ant
//                , GasTanks gasTanks, WicoGyros wicoGyros, PowerProduction pp
                , Timers tim, WicoIGC iGC,
                WicoBases wicoBases, NavCommon navCommon
//                , CargoCheck cargoCheck
                ,Displays displays
                ,SystemsMonitor systemsMonitor
                )

            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
                _wicoElapsedTime = wicoET;
//                _thrusters = thrusters;
//                _connectors = connectors;
                _antennas = ant;
//                _tanks = gasTanks;
//                _gyros = wicoGyros;
//                _power = pp;
                _timers = tim;
                _wicoIGC = iGC;
                _wicoBases = wicoBases;
                _navCommon = navCommon;
//                _cargoCheck = cargoCheck;
                _displays = displays;
                _systemsMonitor = systemsMonitor;

                _program.moduleName += " Space Dock";
                _program.moduleList += "\nSpaceDock V4.2k";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _wicoIGC.AddUnicastHandler(DockingUnicastHandler);
//                _wicoIGC.AddPublicHandler(WICOB_DOCKSETRELAUNCH,RelaunchMessagehandler,true);

                // for backward compatibility
                _wicoIGC.AddPublicHandler(CONNECTORAPPROACHTAG, DockingUnicastHandler);
                _wicoIGC.AddPublicHandler(CONNECTORDOCKTAG, DockingUnicastHandler);
                _wicoIGC.AddPublicHandler(CONNECTORALIGNDOCKTAG, DockingUnicastHandler);

                _Debug = _program._CustomDataIni.Get(sDockingSection, "Debug").ToBoolean(_Debug);
                _program._CustomDataIni.Set(sDockingSection, "Debug", _Debug);

                _displays.AddSurfaceHandler("MODE", SurfaceHandler);
                _displays.AddSurfaceHandler("FUEL", SurfaceHandler);

                _wicoElapsedTime.AddTimer(DockingAction, 2, null, false);
            }


            StringBuilder sbModeInfo = new StringBuilder(100);
            StringBuilder sbNotices = new StringBuilder(300);

            StringBuilder sbFuelInfo = new StringBuilder(25);
            StringBuilder sbFuelNotices = new StringBuilder(200);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                if (tag == "MODE")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        if (
                            iMode == WicoControl.MODE_LAUNCH
                            || iMode == WicoControl.MODE_DOCKING
//                            || iMode == WicoControl.MODE_DOCKED
                         )
                        {
                            if (tsurface.SurfaceSize.Y < 256)
                            { // small/corner LCD
                                tsurface.WriteText(sbModeInfo);

                            }
                            else
                            {
                                tsurface.WriteText(sbModeInfo);
                                tsurface.WriteText(sbNotices, true);
                            }
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 256)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 3;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 2f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
                if (tag == "FUEL")
                {
                    if (ActionType == Displays.DODRAW)
                    {

                        {
                            _systemsMonitor.AirWorthy(false,false, _systemsMonitor.cargohighwater);
                            sbFuelInfo.Clear();
                            sbFuelNotices.Clear();
                            if(!_systemsMonitor.BatteryGo)
                            {
                                sbFuelInfo.AppendLine("BATTERY LOW");
                            }
                            if(!_systemsMonitor.TanksGo)
                            {
                                sbFuelInfo.AppendLine("HYDRO LOW");
                            }
                            if(!_systemsMonitor.ReactorsGo)
                            {
                                sbFuelInfo.AppendLine("REACTORS LOW");
                            }
                            if(!_systemsMonitor.CargoGo)
                            {
                                sbFuelInfo.AppendLine("CARGO FULL");
                            }
                            if(_systemsMonitor.BatteryGo && _systemsMonitor.TanksGo && _systemsMonitor.ReactorsGo && _systemsMonitor.CargoGo)
                            {
                                tsurface.BackgroundColor = Color.Black;
                            }
                            else
                            { // need to give fuel warning
                                tsurface.BackgroundColor = Color.Red;
                            }
                            if (_systemsMonitor.HasHydroTanks())
                            {
                                sbFuelInfo.AppendLine("H2 Tanks = " + (_systemsMonitor.hydroPercent).ToString("0.0")+"%");
                            }
                            if(_systemsMonitor.HasBatteries())
                            {
                                sbFuelInfo.AppendLine("Batteries = " + (_systemsMonitor.batteryPercentage).ToString("0.0") + "%");

                            }
                            if (_systemsMonitor.EnginesCount()>0)
                            {
                                sbFuelInfo.AppendLine("Engines = " + (_systemsMonitor.EnginesAreOff()?"Off": "ON"));
                            }
                            sbFuelNotices.AppendLine("Current Output = " + (_systemsMonitor.currentTotalOutput/ _systemsMonitor.maxTotalPower).ToString("0.00") +"%");

                            tsurface.WriteText(sbFuelInfo);
                            if (tsurface.SurfaceSize.Y < 256)
                            { // small/corner LCD
                            }
                            else
                            {
                                tsurface.WriteText(sbFuelNotices, true);
                            }
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 256)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 3;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 2f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
            }

            void LoadHandler(MyIni Ini)
            {
                bDoingDocking=Ini.Get(sDockingSection, "bDoingDocking").ToBoolean(bDoingDocking);
            }

            void SaveHandler(MyIni Ini)
            {
                Ini.Set(sDockingSection, "bDoingDocking", bDoingDocking);
//                _program.CustomDataChanged();
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
                    // we had stuff displayed; clear it.
                    _displays.ClearDisplays("MODE");

                    _systemsMonitor.ResetMotion();
                    if(fromMode == WicoControl.MODE_LAUNCH)
                    {
                        _systemsMonitor.ConnectAnyConnectors(false, true);
                    }
                }
                if (toMode == WicoControl.MODE_DOCKING)
                {
                    bDoingDocking = true;
                }
                if (
                    toMode == WicoControl.MODE_DOCKED
                    || toMode == 0
                    || toMode == WicoControl.MODE_ATTENTION
                    )
                {
                    bDoingDocking = false;
                }

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
//                        _program.ErrorLog("godock command");
                        _wicoControl.SetMode(WicoControl.MODE_DOCKING,0);
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

                //                if (iMode == 0 || iMode == WicoControl.MODE_ATTENTION) return;

                bool bAirWorthy = _systemsMonitor.AirWorthy(false, false, _systemsMonitor.cargohighwater);

                if (iMode == WicoControl.MODE_LAUNCH) { doModeLaunch(); return; }
                if (iMode == WicoControl.MODE_DOCKING) { doModeDocking(); return; }
                //                if (iMode == WicoControl.MODE_DOCKED) { doModeDocked(); return; }

                if (_systemsMonitor.AnyConnectorIsConnected() && iMode != WicoControl.MODE_DOCKED)
                {
                    _wicoControl.SetMode(WicoControl.MODE_DOCKED);
                }
                if (
                    (
                    iMode == WicoControl.MODE_GOINGTARGET
                    || iMode == WicoControl.MODE_ARRIVEDTARGET
                    || iMode == WicoControl.MODE_NAVNEXTTARGET
                    )
                    && !bDoingDocking
                    && bAutoRefuel
                  )
                {
                    if (_systemsMonitor.AnyConnectorIsConnected() && iMode != WicoControl.MODE_DOCKED)
                    {
                        if (!bAirWorthy)
                        {
                            _program.ErrorLog("Gasp! Need to DOCK! Doing=" + bDoingDocking + " Mode=" + iMode);

                            _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                        }
                    }
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
                    if(_Debug) _program.ErrorLog("Received APPROACH Tag");
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
                        // message format is the same for broadcast and unicast. so unicast messages have target ID even though it's in unicast info
                        long.TryParse(aMessage[iOffset++], out id);
                        if (id == _program.Me.EntityId)
                        {
                            // it's a message for us.
                            //                                    sReceivedMessage = ""; // we processed it.
                            // the sender id.
                            long.TryParse(aMessage[iOffset++], out id);
                            lTargetBase = id; // who said we could come home.

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
                    // connector approach response found
                }
                if((msg.Tag== CONNECTORREQUESTFAILTAG))
                {
                    // base said it could not take us..
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
                        if (_Debug) _program.ErrorLog("Received DOCK Tag");

                        string sMessage = (string)msg.Data;

                        string[] aMessage = sMessage.Trim().Split(':');

//                        _program.Echo(aMessage.Length + ": Length");
//                        for (int i = 0; i < aMessage.Length; i++)
//                            _program.Echo(i + ":" + aMessage[i]);

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
//                                    _program.ErrorLog("Received alignV=" + _program.Vector3DToString(vDockAlign));
                                    bDoDockAlign = true;
                                }
                                vDock = vPosition;
                                float maxSideMeters = (float)_wicoBlockMaster.LargestSideInMeters();
                                
                                vLaunch1 = vDock + vVec * (maxSideMeters * 2);
                                vHome = vDock + vVec * (maxSideMeters * 5);
//                                _program.ErrorLog("COND: vHome=" + _program.Vector3DToString(vHome));
//                                _program.ErrorLog("COND: vLaunch1=" + _program.Vector3DToString(vLaunch1));
//                                _program.ErrorLog("COND: vDock=" + _program.Vector3DToString(vDock));
//                                bValidDock = true;
                                bValidLaunch1 = true;
                                bValidHome = true;

                                _wicoControl.SetState(300);
                            }
                        }

                    }
                }
            }

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
            // 100 = disconnected.  turn on thrusters.
            // 1 thrusting
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
                // todo: WIP: test/make work in gravity

                //                StatusLog("clear", textPanelReport);
                //                StatusLog(moduleName + ":LAUNCH!", textPanelReport);
                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("Launch from Connector");
                _wicoControl.WantMedium();
                if (iState == 0)
                {
//                    StatusLog(DateTime.Now.ToString() + " ACTION: StartLaunch", textLongStatus, true);
//                    StatusLog(moduleName + ":Start Launch", textPanelReport);
// TODO: Add landing gear.  And rotor/hinge and mergeblock 'dock'

                    _timers.TimerTriggers("[LAUNCH]");
                    if (!_systemsMonitor.AnyConnectorIsConnected())
                    {
                        _program.ResetMotion();
                        _wicoControl.SetMode(WicoControl.MODE_IDLE);
                        return;
                    }
                    else
                    {
                        IMyTerminalBlock dockingConnector = _systemsMonitor.GetConnectedConnector(true);
                        _systemsMonitor.ConnectAnyConnectors(false);

                        //                    Echo("Using Connector=" + dockingConnector.CustomName);

                        // NOTE: Connectors are backwards.. so we want to fire thrusters in launchbackward list
                        _systemsMonitor.ThrustersCalculateOrientation(dockingConnector, ref thrustLaunchBackwardList, ref thrustLaunchForwardList,
                            ref thrustLaunchDownList, ref thrustLaunchUpList,
                            ref thrustLaunchLeftList, ref thrustLaunchRightList);
                    }
                    vDock = _wicoBlockMaster.CenterOfMass();
                    _systemsMonitor.RequestLaunchSettings();
                    _wicoControl.SetState(100);
                    return;
                }

                // TODO: support other 'docking' methods
                if (_systemsMonitor.AnyConnectorIsLocked() || _systemsMonitor.AnyConnectorIsConnected())
                {
//                    StatusLog(moduleName + ":Awaiting Disconnect", textPanelReport);
                    _program.Echo("Awaiting Disconnect");
                    _systemsMonitor.ConnectAnyConnectors(false, false); // "OnOff_Off");
                    return;
                }
                if (iState == 100)
                {
                    _systemsMonitor.powerUpThrusters(thrustLaunchForwardList);
                    _wicoControl.SetState(1);
                }

                Vector3D vPos = _wicoBlockMaster.CenterOfMass();

//                _program.Echo("vDock=" + _program.Vector3DToString(vDock));
//                _program.Echo("vPos=" + _program.Vector3DToString(vPos));

                double dist = (vPos - vDock).LengthSquared();
//                StatusLog(moduleName + ":Distance Launched=" + dist.ToString("0.00") + "m", textPanelReport);
//                _program.Echo(moduleName + ":Distance Launched=" + dist.ToString("0.00") + "m");

                if (_wicoBlockMaster.GetShipSpeed() > LaunchMaxVelocity * 0.9)
                {
                    if(_wicoBlockMaster.GetNaturalGravity().LengthSquared()>0)
                    {
                        _systemsMonitor.powerDownThrusters(thrustLaunchForwardList);
                    }
                    else
                    {
                        _systemsMonitor.powerDownThrusters(thrustLaunchBackwardList);
                        _systemsMonitor.powerDownThrusters(thrustLaunchForwardList, WicoThrusters.thrustAll, true);
                    }
                }
                else if (_wicoBlockMaster.GetShipSpeed() > 2)
                {
                    if (_wicoBlockMaster.GetNaturalGravity().LengthSquared() > 0)
                        _systemsMonitor.powerUpThrusters(thrustLaunchBackwardList, 25);
                }
                double stoppingD = _systemsMonitor.calculateStoppingDistance((float)_wicoBlockMaster.GetPhysicalMass(), thrustLaunchForwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                if ((dist + stoppingD) > LaunchDistance)
                {
                    sbModeInfo.AppendLine("Launch Completed");
                    // TODO: handle other connection methods.
                    _systemsMonitor.ConnectAnyConnectors(true, true);
                    _program.ResetMotion();

                    _program.ErrorLog("Auto Launch");

                    _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                }
            }

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
            <Msg received>->150
            150	Move to 'wait' location (or current location) ?request 'wait' location? ->175 or ->200
        
            175 do travel to 'base' location  ->200
            176 wait for nav module to start

            200	request available docking connector

            210 wait for available
            250	when available, calculate approach locations
            300  Start:	Move through locations
            'Back' Connector:
            310 NAV move to Home Arrive->340
            311 waiting for nav

            340 Delay for motion ->350
            350 slow move rest of way to Home. Arrival->400

            400 NAV move to Launch1
            401 wait for nav to start

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

            List<IMyTerminalBlock> thrustToward = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustAway = new List<IMyTerminalBlock>();

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
                sbNotices.Clear();
                sbModeInfo.Clear();

                sbModeInfo.AppendLine("Docking");

                _wicoControl.WantSlow();

                if (dockingConnector == null)
                {
                    dockingConnector = _systemsMonitor.GetDockingConnector();
                    _wicoControl.SetState(0);
                    iState = 0;
                }

                if (iState == 0)
                {
                    //                sInitResults = "DOCKING: state=" + iState+"\n";
                    if (_systemsMonitor.AnyConnectorIsConnected())
                    {
                        _wicoControl.SetMode(WicoControl.MODE_DOCKED);
                        return;
                    }

                    _timers.TimerTriggers("[DOCKING]");

                    _systemsMonitor.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
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

                        _systemsMonitor.ThrustersCalculateOrientation(dockingConnector, ref thrustDockForwardList, ref thrustDockBackwardList,
                            ref thrustDockDownList, ref thrustDockUpList,
                            ref thrustDockLeftList, ref thrustDockRightList);
                        _wicoControl.SetState(100);
                    }

                    // TODO: Save this.. it's our mom....
                    lTargetBase = -1;
                }


                Vector3D vPos = dockingConnector.GetPosition();
                if (!_systemsMonitor.AnyConnectorIsConnected() && _systemsMonitor.AnyConnectorIsLocked())
                {
                    _systemsMonitor.ConnectAnyConnectors();
                    _program.ResetMotion();
                    _wicoControl.SetMode(WicoControl.MODE_DOCKED);
                    _systemsMonitor.powerDownThrusters(WicoThrusters.thrustAll, true);
                    return;
                }
                if (iState == 100)
                {
                    // TODO: allow for relay ships that are NOT bases..
                    // TODO: if memory docking, don't need to adjust antenna
                    // TODO: if stealth mode, don't mess with antenna
                    // antennas now handled by communications module
//                    float range = _wicoBases.RangeToNearestBase() + 100f + (float)_wicoBlockMaster.GetShipSpeed() * 5f;
//                    _antennas.SetMaxPower(false, range);
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
                        sbModeInfo.AppendLine("Finding Base");
                        if (lTargetBase < 0)
                        {
                            // no base set before

                            _wicoElapsedTime.RestartTimer(DockingAction);

                            List<long> baseList = new List<long>();
                            _wicoBases.GetDockingBases(ref baseList);

                            //                            lTargetBase = _wicoBases.BaseFindBest();
                            if (baseList.Count < 1)
                            {
                                // try to get a base to respond
                                _wicoBases.checkBases(true);

                                _wicoControl.SetState(130);
                            }

                            foreach (var lPossibleBase in baseList)
                            {
                                string sMessage = "";// = "WICO:CON?:";
                                string sTag = CONNECTORREQUEST;
                                sMessage += lPossibleBase.ToString() + ":";
                                sMessage += _wicoBlockMaster.HeightInMeters().ToString("0.0") +
                                    "," + _wicoBlockMaster.WidthInMeters().ToString("0.0") +
                                    "," + _wicoBlockMaster.LengthInMeters().ToString("0.0") +
                                    ":";
                                sMessage += _program.Me.CubeGrid.CustomName + ":";
                                sMessage += _program.Me.EntityId.ToString() + ":"; // needs to match when receiving messages for 'us'
                                sMessage += _program.Vector3DToString(_wicoBlockMaster.CenterOfMass());

                                _program.IGC.SendBroadcastMessage(sTag, sMessage);// antSend(sMessage);
                            }
                            _wicoControl.SetState(120);

                        }
                        else
                        {
                            {
                                // duplicated code! from above
                                string sMessage = "";// = "WICO:CON?:";
                                string sTag = CONNECTORREQUEST;
                                sMessage += lTargetBase.ToString() + ":";
                                sMessage += _wicoBlockMaster.HeightInMeters().ToString("0.0") +
                                    "," + _wicoBlockMaster.WidthInMeters().ToString("0.0") +
                                    "," + _wicoBlockMaster.LengthInMeters().ToString("0.0") +
                                    ":";
                                sMessage += _program.Me.CubeGrid.CustomName + ":";
                                sMessage += _program.Me.EntityId.ToString() + ":"; // needs to match when receiving messages for 'us'
                                sMessage += _program.Vector3DToString(_wicoBlockMaster.CenterOfMass());

                                _program.IGC.SendBroadcastMessage(sTag, sMessage);// antSend(sMessage);
                                _wicoControl.SetState(120);
                            }
                            //                            _wicoElapsedTime.RestartTimer(DockingAction);
                            //                            _wicoControl.SetState(126);
                        }
                    }
                    else
                    {
                        sbModeInfo.AppendLine("Waiting for slower speed");
                        _program.ResetMotion();
                    }
                }
                else if (iState == 120)
                { // wait for reply from base
                    sbModeInfo.AppendLine("Awaiting Response from Bases");

                    //                    _wicoControl.WantFast();

                    if (_wicoElapsedTime.IsExpired(DockingAction))
                    { // we've waited long enough to receive all base messages
                        if (lTargetBase < 0) lTargetBase = _wicoBases.BaseFindBest();
                        _wicoControl.SetState(125);
                        _wicoControl.WantFast();
                        return;
                    }
                    // message handled in message handler (duh)
                    { // uses timeout from above
                        _program.Echo("Awaiting reply messages");
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
                        //                        lTargetBase = -1;
                        _wicoBases.checkBases(false);
                        _wicoControl.SetState(100);
                    }
                    else
                    {
                        // get closer
                        _wicoElapsedTime.RestartTimer(DockingAction);
                        _wicoControl.SetState(126);
                        _wicoControl.WantFast();
                        _navCommon.NavGoTarget(_wicoBases.BasePositionOf(lTargetBase), iMode, 110, 3100, "DOCK Base Proximity");
                    }
                }
                else if (iState == 126)
                {
                    if (_wicoElapsedTime.IsExpired(DockingAction))
                    {
                        _program.ErrorLog("126:Timeout waiting for NAV");
                        // timeout
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        _program.Echo("Timeout");
                    }
                    // we are waiting for NAV module to get message and start
                    sbModeInfo.AppendLine("Waiting for NAV to start");
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 130)
                {
                    // no known bases. requested response. wait for a while to see if we get one
                    sbModeInfo.AppendLine("Trying to find a base");
                    //                    StatusLog("Trying to find a base", textPanelReport);
                    //                    bWantFast = false;

                    if (_wicoElapsedTime.GetElapsed(DockingAction)>1.5f)
                    {
                        // we should have all the replies by now
                        if (_wicoBases.BaseFindBest() >= 0)
                            _wicoControl.SetState(110);
                    }
                    if (_wicoElapsedTime.IsExpired(DockingAction))
                    {
                        _program.ErrorLog("Timeout finding base");
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        return;
                    }
                    // wait 1.5 seconds to receive all replies from bases
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
                    sbModeInfo.AppendLine("Waiting for NAV to start");
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 200)
                {//200	Arrived at approach location
                 // request available docking connector
                 //                    StatusLog("Requsting Docking Connector", textPanelReport);
                    sbModeInfo.AppendLine("Requsting Docking Connector");
                    if (_wicoBlockMaster.GetShipSpeed() < 1)
                    {

                        string sMessage = "";// "WICO:COND?:";
                        string sTag = CONNECTORDOCKREQUEST;
                        sMessage += lTargetBase.ToString() + ":";
                        sMessage += _wicoBlockMaster.HeightInMeters().ToString("0.0") +
                           "," + _wicoBlockMaster.WidthInMeters().ToString("0.0") +
                           "," + _wicoBlockMaster.LengthInMeters().ToString("0.0") +
                           ":";
                        //                    sMessage += shipDim.HeightInMeters() + "," + shipDim.WidthInMeters() + "," + shipDim.LengthInMeters() + ":";
                        sMessage += _program.Me.CubeGrid.CustomName + ":";
                        sMessage += _program.Me.EntityId.ToString() + ":";
                        sMessage += _program.Vector3DToString(_wicoBlockMaster.CenterOfMass());
                        _program.IGC.SendBroadcastMessage(sTag, sMessage);// antSend(sMessage);

                        //                    antSend("WICO:COND?:" + baseIdOf(iTargetBase) + ":" + "mini" + ":" + shipOrientationBlock.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        {
                            _wicoElapsedTime.RestartTimer(DockingAction);
//                            dtDockingActionStart = DateTime.Now;
                            _wicoControl.SetState(210);
                        }
                    }
                    else _program.ResetMotion();
                }
                else if (iState == 210)
                { //210	wait for available connector
                    sbModeInfo.AppendLine("Awaiting reply with Docking Connector");
                    //                    StatusLog("Awaiting reply with Docking Connector", textPanelReport);
                    //                    bWantFast = false;
                    if (_wicoElapsedTime.IsExpired(DockingAction))
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
                            _program.Echo("BaseID=" + lTargetBase.ToString() + ":" + _wicoBases.BaseName(lTargetBase));
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
 //                 _wicoControl.SetState(450);
                  //                    _wicoControl.SetState(305);
                  _wicoControl.SetState(310);
                    _systemsMonitor.MoveForwardSlowReset();
                    _wicoControl.WantFast();
                }
                else if(iState==305)
                { // test: raycast target locations
                    _program.Echo("Max side=" + _wicoBlockMaster.LargestSideInMeters());
                    _program.Echo("Delta=" + (vHome - vLaunch1).Length().ToString("0.00"));
                    if(bValidHome)
                    {
                        _program.Echo("Home");
                        if (_program.wicoCameras.CameraForwardScan(vHome))
                            _program.Echo(" Cast");
                    }
                    if(bValidLaunch1)
                    {
                        _program.Echo("Launch1");
                        if (_program.wicoCameras.CameraForwardScan(vLaunch1))
                            _program.Echo(" Cast");
                    }
                }
                else if (iState == 310)
                { //	310 move to home
                    sbModeInfo.AppendLine("Moving to Home");
                    _program.Echo("Moving to Home");
                    //		if(iPushCount<60) iPushCount++;
                    //		else

                    _wicoControl.SetState(311);
                    _navCommon.NavGoTarget(vHome, iMode, 340, 3, "DOCK Home", 10);
                }
                else if (iState == 311)
                {
                    // we are waiting for NAV module to get message and start
                    sbModeInfo.AppendLine("Waiting for NAV to start");
                    _program.Echo("Waiting for NAV to start");
                }
                else if (iState == 340)
                { // arrived at 'home' from NAV
                    _program.ResetMotion();
                    sbModeInfo.AppendLine("Waiting for ship to stop");
                    _program.Echo("Waiting for ship to stop");
                    _systemsMonitor.TurnEjectorsOff();
                    _systemsMonitor.MoveForwardSlowReset();
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
                        _systemsMonitor.MoveForwardSlowReset();
                        _wicoControl.SetState(400);
                        _wicoControl.WantFast();
                    }
                }
                else if (iState == 400)
                {
                    // move to Launch1
                    _program.Echo("Moving to Launch1");
                    sbModeInfo.AppendLine("Moving To Connector Entry");

                    _navCommon.NavGoTarget(vLaunch1, iMode, 410, 3, "DOCK Connector Entry");
                    _wicoElapsedTime.RestartTimer(DockingAction);
                    _wicoControl.SetState(401);
                }
                else if (iState == 401)
                {
                    // we are waiting for NAV module to get message and start
                    _program.Echo("Waiting for NAV to start");
                    if (_wicoElapsedTime.IsExpired(DockingAction))
                    {
                        _program.ErrorLog("401:Timeout waiting for NAV");
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                    }
                }
                else if (iState == 410)
                {
                    // initial: NAV got us to launch1

                    // move closer to Launch1
                    sbModeInfo.AppendLine("Moving closer Connector Entry");
                    Vector3D vVec = vLaunch1 - _wicoBlockMaster.CenterOfMass();
                    bool bAimed = _systemsMonitor.AlignGyros("forward", vVec, _wicoBlockMaster.GetMainController());
                    // distance, and account for the size of the ship
                    // length is the whole thing.  center should be half.  but squared to get compare value.. so just use the original value
                    double distanceSQ = (vLaunch1 - _wicoBlockMaster.CenterOfMass()).LengthSquared() - _wicoBlockMaster.LengthInMeters();
                    _program.Echo("COMDistanceSQ=" + distanceSQ.ToString("0.0"));
                    double stoppingDistance = _systemsMonitor.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(), thrustBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                    _program.Echo("check Closesq=" + (_wicoBlockMaster.LengthInMeters() / 2
                          + stoppingDistance * 2));
                    bool bCloseEnough = distanceSQ <
                          (
//                          _wicoBlockMaster.BlockMultiplier() * 3
                            _wicoBlockMaster.LengthInMeters()/2
                          + stoppingDistance * 2
                          )
                          ;
                    if (bCloseEnough
                       )
                    {
                        _program.ResetMotion();
                        _systemsMonitor.MoveForwardSlowReset();
                        _wicoControl.SetState(430);
                        _wicoControl.WantFast();
                    }
                    if (bAimed)
                    {
                            _systemsMonitor.MoveForwardSlow(3, 5, thrustForwardList, thrustBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                            _wicoControl.WantMedium();
                    }
                    else
                    {
                        _wicoControl.WantFast();
                    }
                }
                else if(iState== 420)
                {
                    // get the connector close to the launch point
                    sbModeInfo.AppendLine("Moving Connector to start position");
                    Vector3D vVec = vLaunch1 - dockingConnector.GetPosition();
                    bool bAimed = _systemsMonitor.AlignGyros("forward", vVec, _wicoBlockMaster.GetMainController());
                    _program.Echo("vvec=" + vVec.ToString());

                    double distanceSQ = (vLaunch1 - dockingConnector.GetPosition()).LengthSquared();
                    _program.Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));

                    var rot = Vector3D.Cross(dockingConnector.WorldMatrix.Forward, vVec);
                    double dot2 = Vector3D.Dot(dockingConnector.WorldMatrix.Forward, vVec);
                    double ang = rot.Length();
                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                    _program.Echo("Ang=" + MathHelper.ToDegrees(ang).ToString("0.00"));
                    //bool bAimed = false;
                    bAimed = false;  /// TESTING

                    if (distanceSQ < _wicoBlockMaster.BlockMultiplier() * 1)
                    {
                        _program.ErrorLog("420 Close enough:"+distanceSQ.ToString("0.00"));
                        _program.ResetMotion();
                        _systemsMonitor.MoveForwardSlowReset();
                        _wicoControl.SetState(430);
                        _wicoControl.WantFast();
                    }
                    if (bAimed)
                    {
//                        _program.ErrorLog("420 Moving foward");
                        _systemsMonitor.MoveForwardSlow(3, 5, thrustForwardList, thrustBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                        _wicoControl.WantMedium();
                    }
                    else
                    {
                        _wicoControl.WantFast();
                    }


                }
                else if (iState == 430)
                {
                    sbModeInfo.AppendLine("arrived at launch1");
                    _program.ResetMotion();
                    // arrived at launch1
                    //                    _wicoControl.WantFast();
                    // TODO: do/waitfor mechanical changes needed for docking
                    if (_wicoBlockMaster.GetShipSpeed() < 0.2)
                    {
                        dockingLastDistance = -1;
                        _wicoElapsedTime.RestartTimer(DockingAction);
                        _wicoControl.SetState(450);
                    }
                    else sbModeInfo.AppendLine("Waiting for stop");

                }
                else if (iState == 450 || iState == 452)
                { //450 452 'reverse' to dock, aiming connector at dock location
                  // align to docking alignment if needed
                    sbModeInfo.AppendLine("Align to Direction");
                    //                    StatusLog("Align Up to Docking Connector", textPanelReport);
                    if (!bDoDockAlign)
                    {
                        _wicoControl.SetState(500);
                        return;
                    }
                    _program.Echo("Aligning to dock");
//                    _systemsMonitor.SetMinAngle(0.03f);
                    _systemsMonitor.SetMinAngle();

                    _program.Echo(_program.Vector3DToString(vDockAlign));
                    // TODO: need to change if non vanilla connector
                    bool bAimed = _systemsMonitor.AlignGyros("up", vDockAlign, dockingConnector);
                    _wicoControl.WantFast();

                    if (bAimed)
                    {
//                        _program.ResetMotion();
                        _program.Echo("Dock Aligned");
                        //                        _program.ErrorLog("Dock Aligned:" + iState);
                        //                        _program.ErrorLog(_program.Vector3DToString(vDockAlign));
//                        if (_wicoElapsedTime.IsExpired(DockingAction))
                        {
                            _wicoElapsedTime.RestartTimer(DockingAction);
                            if (iState == 452) _wicoControl.SetState(500);
                            else
                                _wicoControl.SetState(451); ; // 450->451 
                        }
                    }
                    else
                    {
                        _program.Echo("Not dockaligned");
                    }
                }
                else if (iState == 451)
                { //451 align to dock
                  //                    StatusLog("Align to Docking Connector", textPanelReport);
                    sbModeInfo.AppendLine("Align to Dock");
                    _wicoControl.WantFast();
                    Vector3D vTargetLocation = vDock;
                    Vector3D vVec = vTargetLocation - dockingConnector.GetPosition();

                    if (!bDoDockAlign) // we shouldn't really be here.
                        _wicoControl.SetState(500);

                    _program.Echo("Aligning to dock");
                    bool bAimed = false;
//                    _systemsMonitor.SetMinAngle(0.03f);
                    // TODO: Handle non-vanilla connectors and other blocks..
                    bAimed = _systemsMonitor.AlignGyros("forward", vVec, dockingConnector);
                    if (bAimed)
                    {
                        //                        _program.ErrorLog("connector Aligned:" + iState);
//                        if (_wicoElapsedTime.IsExpired(DockingAction))
                        {
                            _wicoElapsedTime.RestartTimer(DockingAction);
                            _wicoControl.SetState(452);
                        }
                    }
                }
                else if (iState == 500)
                { //500 'reverse' to dock, aiming connector at dock location (really it's connector-forward)
                    // TODO: Adjust for non-center aligned connectors..
                  // TODO: needs a time-out for when misaligned or base connector moves.
                  //               _wicoControl.WantFast();
                    sbModeInfo.AppendLine("Reversing to Dock");
                    //StatusLog("Reversing to Docking Connector", textPanelReport);
                    _program.Echo("bDoDockAlign=" + bDoDockAlign);
                    //                StatusLog(moduleName + ":Docking: Reversing to dock! Velocity=" + wicoBlockMaster.GetShipSpeed().ToString("0.00"), textPanelReport);
                    _program.Echo("Reversing to Dock");
// CHECK HERE IF DOCKING SPAZZES                    CTRL_COEFF = 0.75;
                    _systemsMonitor.SetMinAngle();

                    Vector3D vTargetLocation = vDock;

                    Vector3D vTargetLine =  vLaunch1 - vTargetLocation;
                    Vector3D vVec = vTargetLocation - dockingConnector.GetPosition();
                    double distance = vVec.Length();
                    sbNotices.AppendLine("distance=" + _program.niceDoubleMeters(distance));
                    sbNotices.AppendLine("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
                    _program.Echo("distance=" + _program.niceDoubleMeters(distance));
                    _program.Echo("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
                    /*
                    var rot = Vector3D.Cross(vTargetLine, -dockingConnector.WorldMatrix.Forward);
                    double dot2 = Vector3D.Dot(vTargetLine, -dockingConnector.WorldMatrix.Forward);
                    double ang = rot.Length();
                    _program.Echo("RotL=" + ang.ToString("0.000"));

                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                    if (dot2 < 0) ang = Math.PI - ang; // compensate for >+/-90
                    _program.Echo("Ang=" + MathHelper.ToDegrees(ang).ToString("0.00"));
                    */
                    Vector3D vNTLine = vTargetLine;
                    double dLineDist=vNTLine.Normalize();
                    Vector3D vTargetCalc = vDock + vNTLine * distance;
                    Vector3D vOffset = dockingConnector.GetPosition() - vTargetCalc;
                    double offsetL = vOffset.Length();
                    _program.Echo("OffsetL=" + offsetL.ToString("0.000") + " ("+ _wicoBlockMaster.gridsize.ToString("0.000")+")");

                    //                    _program.Echo("rot=" + rot.ToString());
                    //                    _program.Echo("dot2=" + (dot2).ToString("0.00"));

                    if (dockingLastDistance < 0) dockingLastDistance = distance+1;
                    if (dockingLastDistance < distance || offsetL>(distance+_wicoBlockMaster.gridsize))
                    {
                        // we are farther away than last time... something is wrong..
                        _wicoControl.SetState(590);
                    }
                    if (distance > 10)
                        _systemsMonitor.SetMinAngle(0.03f);
                    else
                        _systemsMonitor.SetMinAngle(0.05f);

                    bool bAimed = false;

                    if (distance > 15)
                        bAimed = _systemsMonitor.BeamRider(vTargetLocation, vDock, dockingConnector);
                    else
                        // TODO: Handle non-vanilla connectors and other blocks..
                        bAimed = _systemsMonitor.AlignGyros("forward", vVec, dockingConnector);

                    if (bAimed)
                    { // only check if we are aimed at desired connector
//                        _program.Echo("Angle Between=" + _systemsMonitor._gyros.VectorAngleBetween(dockingConnector.WorldMatrix.Forward, -vTargetLine));

                        if (offsetL > _wicoBlockMaster.gridsize*2)
                        { // we are farther then desired point that we want to be.
                            _program.Echo("Farther than desired");

                            _systemsMonitor.powerDownThrusters();

                            if (thrustToward.Count == 0) // first time we should reset movement
                                _systemsMonitor.MoveForwardSlowReset();

                            if(thrustForwardList.Count<1)
                            {
                                _systemsMonitor.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                                    ref thrustForwardList, ref thrustBackwardList, ref thrustDownList, ref thrustUpList,
                                    ref thrustLeftList, ref thrustRightList);
                            }
                            _systemsMonitor.GetBestThrusters(vOffset,
                                thrustForwardList, thrustBackwardList, thrustDownList, thrustUpList, thrustLeftList, thrustRightList,
                                out thrustToward, out thrustAway);
                            _program.Echo("FW #=" + thrustToward.Count);
                            _wicoControl.WantFast();

                            _systemsMonitor.MoveForwardSlow(1, 3, thrustToward, thrustAway, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                        }
                        else if (thrustToward.Count > 0)
                        {
                            _program.Echo("Reset movement");
                            // Once
                            _systemsMonitor.MoveForwardSlowReset();
                            _systemsMonitor.powerDownThrusters();
                            thrustAway.Clear();
                            thrustToward.Clear();
                            _wicoControl.WantFast();
                        }
                        else 
                        {
                            // we are aimed at location

                            double stoppingD =_systemsMonitor.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(), thrustDockBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                            if (stoppingD < 1) stoppingD = 1;

                            double distanceSlow = Math.Max(stoppingD*5, _wicoBlockMaster.gridsize * 5);

                            _program.Echo("Aimed");
                            if (distance > distanceSlow)
                            {
                                _wicoControl.WantMedium();
                                _program.Echo(">"+distanceSlow.ToString("0.0"));
                                _systemsMonitor.MoveForwardSlow(5, 10, thrustDockForwardList, thrustDockBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                            }
                            else
                            {
                                _program.Echo("<=" + distanceSlow.ToString("0.0"));
                                _wicoControl.WantFast();
                                _systemsMonitor.MoveForwardSlow(.5f, 1.5f, thrustDockForwardList, thrustDockBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                            }
                        }
                    }
                    else
                    {
                        _program.Echo("Aiming");
                        _systemsMonitor.powerDownThrusters();
                        _wicoControl.WantFast();
                    }
                }
                else if (iState == 590)
                {
                    sbModeInfo.AppendLine("Abort and try again");
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
                    bool bAimed = _systemsMonitor.AlignGyros("forward", vVec, dockingConnector);
                    if (!bAimed) _wicoControl.WantFast();
                    else _wicoControl.WantMedium();
                    _systemsMonitor.MoveForwardSlow(5, 10, thrustDockBackwardList, thrustDockForwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                }
            }

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






        }
    }
}
