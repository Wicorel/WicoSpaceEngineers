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
        public class PowerManagement
        {
            // TODO:
            // Add handlers for events like power low, need hydrogen, need uranium, etc
            // Control solar panels and aim them
            // control reactors like engines (check for fuel)

            /// <summary>
            /// Turn Hydro Engines on/Off automatically to recharge batteries
            /// </summary>
            bool _ControlEngines = true;

            readonly Program _program;
            readonly WicoControl _wicoControl;
            readonly PowerProduction _power;
            readonly GasTanks _tanks;
            readonly WicoElapsedTime _elapsedTime;
            readonly WicoIGC _igc;
            readonly Displays _displays;

            readonly string PowerManagementSection="PowerManagement";
            const string ScreenTag = "POWERMANAGEMENT";
            readonly double PowerManagementCheckSeconds = 1;

            bool _bDebug = false;
            bool PowerManagementEnable = true;

            const string PowerManagementName = "PowerManagement";
            const string PowerManagementTimer = PowerManagementName+"Check";
            const string ControlEngines = "ControlEngines";
            const string PowerManagementeDebug = PowerManagementName + "Debug";
            const string PowerManagementeEnabled = PowerManagementName + "Enabled";

            public PowerManagement(Program program, WicoControl wicoControl, PowerProduction powerProduction, GasTanks tanks, WicoElapsedTime wicoElapsedTime, WicoIGC wicoIGC, Displays displays)
            {
                _program = program;
                _wicoControl = wicoControl;
                _power = powerProduction;
                _tanks = tanks;
                _elapsedTime = wicoElapsedTime;
                _igc = wicoIGC;
                _displays = displays;

                _program.moduleName += " PowerMgmt";
                _program.moduleList += "\nPower Management V4.2b";

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _ControlEngines = _program._CustomDataIni.Get(PowerManagementSection, ControlEngines).ToBoolean(_ControlEngines);
                _program._CustomDataIni.Set(PowerManagementSection, ControlEngines, _ControlEngines);

                PowerManagementCheckSeconds = _program._CustomDataIni.Get(_program.OurName, PowerManagementTimer).ToDouble(PowerManagementCheckSeconds);
                _program._CustomDataIni.Set(_program.OurName, PowerManagementTimer, PowerManagementCheckSeconds);

                _elapsedTime.AddTimer(PowerManagementTimer, PowerManagementCheckSeconds, ElapsedTimeHandler);
                _elapsedTime.StartTimer(PowerManagementTimer);

                _displays.AddSurfaceHandler(ScreenTag, SurfaceHandler);

                _bDebug = _program._CustomDataIni.Get(_program.OurName, PowerManagementeDebug).ToBoolean(_bDebug);
                _program._CustomDataIni.Set(_program.OurName, PowerManagementeDebug, _bDebug);

                PowerManagementEnable = _program._CustomDataIni.Get(_program.OurName, PowerManagementeEnabled).ToBoolean(PowerManagementEnable);
                _program._CustomDataIni.Set(_program.OurName, PowerManagementeEnabled, PowerManagementEnable);
                if (!PowerManagementEnable)
                {
                    _elapsedTime.StopTimer(PowerManagementTimer);
                }


            }
            void LoadHandler(MyIni Ini)
            {
            }

            void SaveHandler(MyIni Ini)
            {
            }
            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (!PowerManagementEnable) return;

                if (tag == ScreenTag)
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        sbNotices.Clear();
                        sbModeInfo.Clear();
//                        sbModeInfo.AppendLine("Power Management");

                        if(_power.HasBatteries())
                            sbModeInfo.AppendLine("Batteries=" + _power.batteryPercentage + " (" + _power.batterypctlow + ")");
                        if(_tanks.HasHydroTanks()) 
                            sbModeInfo.AppendLine("H Tanks=" + _tanks.hydroPercent.ToString("0") + "%");
//                        sbModeInfo.AppendLine("Control Engines=" + _ControlEngines);
                        if(_power.EnginesCount()>0) 
                            sbModeInfo.AppendLine("   Engines=" + (_power.EnginesAreOff() ? "Off" : "ON"));

                        if (_power.maxTotalPower > 0)
                        {
                            if(_power.HasBatteries())
                                sbNotices.AppendLine("Batteries=" + (_power.batteryTotalOutput / _power.maxTotalPower * 100).ToString("0") + "%");
                            if (_power.maxReactorPower > 0)
                                sbNotices.AppendLine("Reactors=" + (_power.currentReactorOutput / _power.maxTotalPower * 100).ToString("0") + "%");
                            if (_power.maxSolarPower > 0)
                                sbNotices.AppendLine("Solar=" + (_power.currentSolarOutput / _power.maxTotalPower * 100).ToString("0") + "%");
                            if (_power.currentTurbineOutput > 0)
                                sbNotices.AppendLine("Turbines=" + (_power.currentTurbineOutput / _power.maxTotalPower * 100).ToString("0") + "%");
                            if (_power.currentEngineOutput > 0)
                                sbNotices.AppendLine("Engines=" + (_power.currentEngineOutput / _power.maxTotalPower * 100).ToString("0") + "%");
                        }

                        tsurface.WriteText(sbModeInfo);
                        if (tsurface.SurfaceSize.Y < 512)
                        { // small/corner LCD

                        }
                        else
                        {
                            tsurface.WriteText(sbNotices, true);
                        }

                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 512)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 2;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.5f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
            }

            void ElapsedTimeHandler(string timer)
            {
                if (!PowerManagementEnable) return;
                // TODO: Control H2 generators.
                _power.CalcPower();
                _tanks.TanksCalculate();
                if (_ControlEngines && _power.EnginesCount()>0)
                {
                    // check if engines are needed to charge batteries
                    if (_power.batteryPercentage < _power.batterypctlow)
                    { // batteries are low; try to recharge them
                        _power.EngineControl(true); // turn engines on
                        _program.Echo("PWR:Batteries LOW!");
                    }
                    else if (_power.batteryTotalOutput > (_power.maxBatteryPower * .75)
                        && _tanks.hydroPercent>=_tanks.tankspctlow
                        && _power.batteryPercentage<_power.batterypcthigh
                        )
                    { // batteires are providing > 75% of their power and we are not critically low on hydro
                        _power.EngineControl(true);
                        _program.Echo("PWR:Batteries need help on output");
                    }
                    else if (
                        _power.HasBatteries()
                        && _power.batteryPercentage < _power.batterypcthigh
                        && _tanks.hydroPercent >= _tanks.tankspcthigh
                        )
                    {
                        // if we have tons of hydro and batteries are not maxed
                        _power.EngineControl(true);
                        _program.Echo("PWR:Extra hydro fuel; using for charging batteries to max");
                    }
                    else
                    {
                        if(!_power.EnginesAreOff()
                            &&
                            _tanks.hydroPercent >= _tanks.tankspcthigh)
                        {
                            if(_power.batteryPercentage < (_power.batterypcthigh*1.1))
                            {
                                _program.Echo("PWR:Keep running for a bit with extra hydro");
                                return;
                            }
                        }
                        _power.EngineControl(false);
                        _program.Echo("No need to have engines on.");
                    }
                }
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
            }

        }
    }
}
