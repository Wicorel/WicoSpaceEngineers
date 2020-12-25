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
        public class WicoElapsedTime
        {
            readonly Program _program;
            readonly WicoControl _wicoControl;

            bool _bDebug = false;

            string wicoETString = "WicoET";

            public WicoElapsedTime(Program program, WicoControl wicoControl)
            {
                _program = program;
                _wicoControl = wicoControl;

                _bDebug = _program._CustomDataIni.Get(wicoETString, "Debug").ToBoolean(_bDebug);
                _program._CustomDataIni.Set(wicoETString, "Debug", _bDebug);
            }

            List<ElapsedTimers> TimerList = new List<ElapsedTimers>();

            class ElapsedTimers
            {
                public string sName;
                public double dWaitSeconds;
                public double dElapsedSeconds;
                public bool bActive;
                public Action<string> handler;
            }

            public bool AddTimer(string sName, double dDefaultWaitSeconds=1, Action<string> handler = null, bool bAllowPlayerControl=true)
            {
                ElapsedTimers et = new ElapsedTimers
                {
                    sName = sName,
                    dWaitSeconds = dDefaultWaitSeconds,
                    dElapsedSeconds = -1,
                    bActive = false,
                    handler = handler
                };

                foreach (var et1 in TimerList)
                {
                    if(et1.sName==sName)
                    {
                        et1.dWaitSeconds = dDefaultWaitSeconds;
                        et1.dElapsedSeconds = -1;
                        et1.handler = handler;
                        return false;
                    }
                }
                TimerList.Add(et);

                return true;
            }

            public bool StartTimer(string sName)
            {

                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        et.bActive = true;
                        return true;
                    }
                }
                return false;
            }

            public bool StopTimer(string sName)
            {
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        et.bActive = false;
                        return true;
                    }
                }
                return false;
            }
            /// <summary>
            /// Stops the timer and resets to un-run time.
            /// </summary>
            /// <param name="sName"></param>
            /// <returns></returns>
            public bool ResetTimer(string sName)
            {
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        et.dElapsedSeconds =-1;
                        et.bActive = false;
                        return true;
                    }
                }
                return false;

            }

            /// <summary>
            /// reset the timer to zero elapsed time
            /// </summary>
            /// <param name="sName"></param>
            /// <returns></returns>
            public bool RestartTimer(string sName)
            {
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        et.dElapsedSeconds = 0;
                        return true;
                    }
                }
                return false;

            }

            /// <summary>
            /// Returns if the timer is epxired.  False if the timer is not active.
            /// </summary>
            /// <param name="sName"></param>
            /// <returns></returns>
            public bool IsExpired(string sName)
            {
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        if (!et.bActive) return false;
                        if (et.dElapsedSeconds < 0) return true;
                        if (et.dElapsedSeconds > et.dWaitSeconds) return true;
                        else return false;
                    }
                }
                return false;
            }

            public bool IsActive(string sName)
            {
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        if (et.bActive) return true;
                    }
                }
                return false;

            }
            public bool GetTime(string sName, out double Elapsed, out double Wait)
            {
                Elapsed = -1;
                Wait = -1;
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        Elapsed = et.dElapsedSeconds;
                        Wait = et.dWaitSeconds;
                        return true;
                    }
                }
                return false;
            }
            public double GetElapsed(string sName)
            {
                foreach (var et in TimerList)
                {
                    if (et.sName == sName)
                    {
                        return et.dElapsedSeconds;
                    }
                }
                return -1;
            }

            /// <summary>
            /// Call from main loop for EVERY update source type.
            /// </summary>
            public void CheckTimers()
            {
                if (_bDebug) _program.Echo("CheckTimers() " + TimerList.Count.ToString() + " Timers");
                foreach (var et in TimerList)
                {
                    if (_bDebug) _program.Echo("Timer:" + et.sName + " Active="+ et.bActive+" " + et.dElapsedSeconds.ToString("0.00") + "/" + et.dWaitSeconds.ToString("0.00"));
                    if (et.bActive)
                    {
                        if (et.dElapsedSeconds >= 0)
                        {
                            et.dElapsedSeconds += _program.Runtime.TimeSinceLastRun.TotalMilliseconds/1000;
                        }
                        if (et.dElapsedSeconds<0 || et.dElapsedSeconds>et.dWaitSeconds)
                        {
                            if (_bDebug) _program.Echo("Trigger:" + et.sName);
                            if (et.handler != null)
                            {
                                et.dElapsedSeconds = 0;
                                et.handler(et.sName);
                            }
                        }
                        if (et.dElapsedSeconds < 0) et.dElapsedSeconds = 0;
                        // TODO: calculate remaining time and request faster trigger..  but only if timer is set to 'accuracy' mode
                        _wicoControl.WantSlow();
                    }
                }
            }

            public void SetDebug(bool bDebug=false)
            {
                _bDebug = bDebug;
            }
        }
    }
}
