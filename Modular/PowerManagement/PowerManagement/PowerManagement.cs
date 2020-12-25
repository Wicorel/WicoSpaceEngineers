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
            /// <summary>
            /// Turn Hydro Engines on/Off automatically to recharge batteries
            /// </summary>
            bool _ControlEngines = true;

            readonly Program _program;
            readonly PowerProduction _power;
            readonly GasTanks _tanks;
            readonly WicoElapsedTime _elapsedTime;
            readonly WicoIGC _igc;

            readonly string PowerManagementSection="PowerManagement";

            public PowerManagement(Program program, PowerProduction powerProduction, GasTanks tanks, WicoElapsedTime wicoElapsedTime, WicoIGC wicoIGC)
            {
                _program = program;
                _power = powerProduction;
                _tanks = tanks;
                _elapsedTime = wicoElapsedTime;
                _igc = wicoIGC;

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

//                _program.AddUpdateHandler(UpdateHandler);
//                _program.AddTriggerHandler(ProcessTrigger);

                _ControlEngines = _program._CustomDataIni.Get(PowerManagementSection, "ControlEngines").ToBoolean(_ControlEngines);
                _program._CustomDataIni.Set(PowerManagementSection, "ControlEngines", _ControlEngines);

                _elapsedTime.AddTimer("PowerManagementCheck", 1, ElapsedTimeHandler);
                _elapsedTime.StartTimer("PowerManagementCheck");
            }
            void LoadHandler(MyIni Ini)
            {
            }

            void SaveHandler(MyIni Ini)
            {
            }

            void ElapsedTimeHandler(string timer)
            {
                if (_ControlEngines)
                {
                    _power.CalcPower();
                    // check if engines are needed to charge batteries
                    if (_power.batteryPercentage < _power.batterypctlow)
                    { // batteries are low; try to recharge them
                        _power.EngineControl(true); // turn engines on
_program.Echo("Batteries LOW!");
                    }
                    else if (_power.batteryTotalOutput > (_power.maxBatteryPower * .75))
                    { // batteires are providing > 75% of power
                        _power.EngineControl(true);
_program.Echo("Batteries need help on output");
                    }
                    else if (_power.batteryPercentage < _power.batterypcthigh
                        && _tanks.hydroPercent > 95
                        )
                    {
                        // if we have tons of hydro and batteries are not maxed
                        _power.EngineControl(true);
_program.Echo("Extra hydro fuel; using for charging batteries to max");
                    }
                    else _power.EngineControl(false);
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
