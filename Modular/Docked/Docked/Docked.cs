using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class WhenDocked: DockBase
        {
            /*
             * BUGS:
             * 
             * 
             * TODO:
             *   Pull U if needed
             *   Push ore/etc if configured
             *   
             *   
             * 
             * Transport mode:
             *  load from one dock and unload at another dock
             *  Pull ore/whatever when loading
             *  Handle 'loading' power into batteries
             *  handle some batteries emptied as 'cargo'
             *  
             */
            private Program _program;
            private WicoControl _wicoControl;
            private WicoBlockMaster _wicoBlockMaster;
//            private Connectors _connectors;
//            private WicoThrusters _thrusters;
            private Antennas _antennas;
//            private GasTanks _tanks;
//            private WicoGyros _gyros;
//            private PowerProduction _power;
            private Timers _timers;
            private WicoIGC _wicoIGC;
            private WicoBases _wicoBases;
//            private NavCommon _navCommon;
//            private CargoCheck _cargoCheck;
            private Displays _displays;
            private SystemsMonitor _systemsMonitor;

            string sDockingSection = "DOCKED";
            public bool _Debug = false;

            bool bAutoRelaunch = false;
//            bool bAutoRefuel = true;

            StringBuilder sbModeInfo = new StringBuilder(100);
            StringBuilder sbNotices = new StringBuilder(300);

            public WhenDocked(Program program, WicoControl wc, WicoBlockMaster wbm
                , WicoIGC iGC
                //                , WicoThrusters thrusters
                //                , Connectors connectors
                , Antennas ant
//                , GasTanks gasTanks
//                , WicoGyros wicoGyros
//                , PowerProduction pp
                , Timers tim
                , WicoBases wicoBases
//                , NavCommon navCommon
//                , CargoCheck cargoCheck
                , Displays displays
                , SystemsMonitor systemsMonitor

                ) : base(program)

            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
//                _thrusters = thrusters;
//                _connectors = connectors;
                _antennas = ant;
//                _tanks = gasTanks;
//                _gyros = wicoGyros;
//                _power = pp;
                _timers = tim;
                _wicoIGC = iGC;
                _wicoBases = wicoBases;
//                _navCommon = navCommon;
//                _cargoCheck = cargoCheck;
                _displays = displays;
                _systemsMonitor = systemsMonitor;

                _program.moduleName += " Docked";
                _program.moduleList += "\nDocked V4.2h";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoIGC.AddPublicHandler(WICOB_DOCKSETRELAUNCH, RelaunchMessagehandler, true);

                _Debug = _program._CustomDataIni.Get(sDockingSection, "Debug").ToBoolean(_Debug);
                _program._CustomDataIni.Set(sDockingSection, "Debug", _Debug);

                bAutoRelaunch = _program._CustomDataIni.Get(sDockingSection, "AutoRelaunch").ToBoolean(bAutoRelaunch);
                _program._CustomDataIni.Set(sDockingSection, "AutoRelaunch", bAutoRelaunch);

                if (_displays != null)
                    _displays.AddSurfaceHandler("MODE", SurfaceHandler);
                else _program.Echo("Display is NULL!");
            }


            private void RelaunchMessagehandler(MyIGCMessage msg)
            {
                if (msg.Tag == WICOB_DOCKSETRELAUNCH && msg.Data is string)
                {
                    if (_Debug) _program.ErrorLog("Received DockSet relaunch =" + (string)msg.Data);

                    bool bResult = false;
                    bool bOK = bool.TryParse((string)msg.Data, out bResult);
                    if (bOK)
                    {
                        if(bResult!=bAutoRelaunch)
                        {
                            _program._CustomDataIni.Set(sDockingSection, "AutoRelaunch", bResult);
                            _program.CustomDataChanged();
                        }
                        bAutoRelaunch = bResult;
                    }
                }
            }

            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_DOCKED)
                {
                    _wicoControl.WantFast();
                }
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
                    )
                {
                    _displays.ClearDisplays("MODE");
                    _systemsMonitor.ResetMotion();
                }
                if (toMode == 0
                    || toMode== WicoControl.MODE_ATTENTION
                    )
                {
                    bAutoRelaunch = false;
                }
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
                    if (myCommandLine.Argument(0) == "relaunch")
                    {
                        bAutoRelaunch = !bAutoRelaunch;
//                        string s = "AutoRelaunch=" + bAutoRelaunch.ToString();
 //                       _program.Echo(s);
//                        _program.ErrorLog(s);
                        _program._CustomDataIni.Set(sDockingSection, "AutoRelaunch", bAutoRelaunch);
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

                _program.Echo("Autorelaunch=" + bAutoRelaunch.ToString());

                if (iMode == WicoControl.MODE_DOCKED) { doModeDocked(); return; }

                if (_systemsMonitor.AnyConnectorIsConnected() && iMode != WicoControl.MODE_DOCKED)
                {
                    _wicoControl.SetMode(WicoControl.MODE_DOCKED);
                }

            }


            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                if (tag == "MODE")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        if (
                            iMode == WicoControl.MODE_DOCKED
                         )
                        {
                            tsurface.WriteText(sbModeInfo);
                            if (tsurface.SurfaceSize.Y < 256)
                            { // small/corner LCD

                            }
                            else
                            {
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
            }

            // 0 = master init
            // 1 battery check 30%.  If no batteries->4
            // 2 battery check 40%
            // 3 battery check 50%
            // 4 battery check 60%
            //   75%
            // 90%
            //  battery check 100%
            // 50 no battery checks.. 

            void doModeDocked()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbModeInfo.Clear();

                //                StatusLog("clear", textPanelReport);
                //                StatusLog(moduleName + ":DOCKED!", textPanelReport);
                sbModeInfo.AppendLine("Docked");
                _program.Echo("Docked!");
                if(bAutoRelaunch) sbNotices.AppendLine("Will Autorelaunch");
                _program.Echo("Autorelaunch=" + bAutoRelaunch.ToString());

                _wicoControl.WantSlow();
                //TODO: autounload
                bool bAirWorthy = _systemsMonitor.AirWorthy(false, true);

                if (bAutoRelaunch)
                {
                    sbModeInfo.AppendLine(" Checking Relaunch");
                    _program.Echo("Docked. Checking Relaunch");
                    if (bAirWorthy)
                    {
                        _program.Echo("RELAUNCH!");
                        _wicoControl.SetMode(WicoControl.MODE_LAUNCH);
                        return;
                    }
                }
                if (!bAirWorthy && bAutoRelaunch)
                {
                    sbModeInfo.AppendLine(" Awaiting Relaunch Criteria");
                    _program.Echo(" Awaiting Relaunch Criteria");
                }
                else if(bAirWorthy)
                {
                    sbModeInfo.AppendLine(" Worthy of relaunch");
                    _program.Echo(" Worthy of relaunch");
                }
                if(_systemsMonitor.HasBatteries())
                {
                    //                            StatusLog(" Battery " + batteryPercentage + "% (" + batterypcthigh + "%)", textPanelReport);
                    sbNotices.AppendLine(" Battery " + _systemsMonitor.batteryPercentage + "% (" + _systemsMonitor.batterypcthigh + "%)");
                    _program.Echo(" Battery " + _systemsMonitor.batteryPercentage + "% (" + _systemsMonitor.batterypcthigh + "%)");
                }
                {
                    sbNotices.AppendLine(" Cargo: " + _systemsMonitor.cargopcent + "% (" + _systemsMonitor.cargopctmin + ")");
                    _program.Echo(" Cargo: " + _systemsMonitor.cargopcent + "% (" + _systemsMonitor.cargopctmin + ")");
                }
                if (_systemsMonitor.HasHydroTanks())
                {
                    sbNotices.AppendLine(" Hydro: " + (_systemsMonitor.hydroPercent ).ToString("0") + "% (" + _systemsMonitor.tankspcthigh + ")");
                    _program.Echo(" Hydro: " + (_systemsMonitor.hydroPercent).ToString("0") + "% (" + _systemsMonitor.tankspcthigh + ")");
                }
                if (!_systemsMonitor.AnyConnectorIsConnected())
                {
                    // we magically got disconnected..
                    // assume user did it.
                    _wicoControl.SetMode(WicoControl.MODE_IDLE);

                    _systemsMonitor.powerDownThrusters(); // turn thrusters ON

                    _systemsMonitor.TanksStockPile(false); // turn tanks ON

                    // Communications manager does this now..

                    // TODO: allow for relay ships that are NOT bases..
//                    float range = _wicoBases.RangeToNearestBase() + 100f + (float)_wicoBlockMaster.GetShipSpeed() * 5f;
//                    _antennas.SetMaxPower(false, range);
                    _systemsMonitor.BatterySetNormal();
                }
                else
                {
                    sbModeInfo.AppendLine("Power Saving Mode");
                    _program.Echo("Power Saving Mode");
                    if (iState == 0)
                    {
                        _systemsMonitor.RequestRefuel();

                        // make a 'lower power' handler?
                        _systemsMonitor.powerDownThrusters(WicoThrusters.thrustAll, true);
                        _antennas.SetLowPower();
                        //                        SensorsSleepAll();
                        // TODO: ??? turn gyos off?

                        _timers.TimerTriggers("[DOCKED]");
                        if (_systemsMonitor.HasBatteries() && _systemsMonitor.bAutoRefuel)
                        {
                            _wicoControl.SetState(1);
                        }
                        else _wicoControl.SetState(50);

                    }
                    else if (iState == 1)
                    {
                        sbNotices.AppendLine("Charging to 10%");
                        _systemsMonitor.BatteryCheck(0, true);
                        if (!_systemsMonitor.BatteryCheck(10, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 2)
                    {
                        sbNotices.AppendLine("Charging to 30%");
                        if (!_systemsMonitor.BatteryCheck(30, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 3)
                    {
                        sbNotices.AppendLine("Charging to 40%");
                        if (!_systemsMonitor.BatteryCheck(40, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 4)
                    {
                        sbNotices.AppendLine("Charging to 50%");
                        if (!_systemsMonitor.BatteryCheck(50, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 5)
                    {
                        sbNotices.AppendLine("Charging to 60%");
                        if (!_systemsMonitor.BatteryCheck(60, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 6)
                    {
                        sbNotices.AppendLine("Charging to 75%");
                        if (!_systemsMonitor.BatteryCheck(75, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 7)
                    {
                        sbNotices.AppendLine("Charging to 90%");
                        if (!_systemsMonitor.BatteryCheck(90, true))
                            _wicoControl.SetState(iState + 1);
                    }
                    else if (iState == 8)
                    {
                        sbNotices.AppendLine("Charging to 100%");
                        if (!_systemsMonitor.BatteryCheck(100, true))
                            if(_systemsMonitor.batteryPercentage<99)
                                _wicoControl.SetState(1);
                    }
                    else // allow display of info without setting
                    {
                        _systemsMonitor.BatteryCheck(0, true); //,textBlock);
                    }

                    // all states
                    {
                        //                    if (bAutoRelaunch)
                        {
                            _systemsMonitor.doCargoCheck();
                            _systemsMonitor.TanksCalculate();
                        }

                        //                        if (power.batteryPercentage >= 0) StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                        //                       else _program.Echo("No Batteries");
                        if (_systemsMonitor.oxyPercent >= 0)
                        {
                            //                            StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                            //Echo("O:" + oxyPercent.ToString("000.0%"));
                        }
                        else _program.Echo("No Oxygen Tanks");

                        if (_systemsMonitor.hydroPercent >= 0)
                        {
                            //                            StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                            // TODO: use setting for 'low' (and 'enough')
                            //                            if (hydroPercent < 0.20f)
                            //                                StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);

                            _program.Echo("H:" + _systemsMonitor.hydroPercent.ToString("000.0")+"%");
                        }
                        else _program.Echo("No Hydrogen Tanks");
                        if (_systemsMonitor.batteryPercentage >= 0 && _systemsMonitor.batteryPercentage < _systemsMonitor.batterypctlow)
                        {
                            //                            StatusLog(" WARNING: Low Battery Power", textPanelReport);
                        }

                        // TODO: get uranium into reactors; take out excess ingots; turn off conveyor usage (like TIM)
                        // TODO: get ore OUT of ship and into base (including stone)
                        // TODO: Handle ore carrier/shuttle
                    }
                }
            }
            public override void SetRelaunch(bool bRelaunch = true)
            {
                bAutoRelaunch = bRelaunch;
            }

        }
    }
}
