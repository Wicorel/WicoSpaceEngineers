using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {

        public class WicoControl : WicoUpdates
        {
            bool _bControlDebug = false;

            #region MODES
            const string MODECHANGETAG = "[WICOMODECHANGE]";
            int _iMode = -1;
            int _iState = -1;

            string ControlSection = "WicoControl";
            public int IMode
            {
                get
                {
                    return _iMode;
                }

                set
                {
                    SetMode(value);
                    //                    _iMode = value;
                }
            }

            public int IState
            {
                get
                {
                    return _iState;
                }

                set
                {
                    SetState(value);
                    //                    _iState = value;
                }
            }

            List<Action<int, int, int, int>> ControlChangeHandlers = new List<Action<int, int, int, int>>();
            List<Action> ModeAfterInitHandlers = new List<Action>();

            public const int MODE_IDLE = 0;

            public const int MODE_DOCKING = 30;
            public const int MODE_DOCKED = 40;
            public const int MODE_LAUNCH = 50; // space launch

            public const int MODE_LAUNCHPREP = 100; // oribital launch prep
            public const int MODE_ORBITALLAUNCH = 120;
            public const int MODE_DESCENT = 150; // descend from space and stop NN meters above surface
            public const int MODE_ORBITALLAND = 151; // land from orbit
            public const int MODE_HOVER = 170;
            public const int MODE_LANDED = 180;

            public const int MODE_MINE = 500;
            public const int MODE_GOTOORE = 510;
            public const int MODE_BORESINGLE = 520;

            public const int MODE_EXITINGASTEROID = 590;


            public const int MODE_STARTNAV = 600; // start the navigation operations
            public const int MODE_GOINGTARGET = 650;
            public const int MODE_NAVNEXTTARGET = 670; // go to the next target
            public const int MODE_ARRIVEDTARGET = 699; // we have arrived at target


            public const int MODE_DOSCANS = 900; // Start scanning

            public const int MODE_UNDERCONSTRUCTION = 1000;

            public const int MODE_ATTACKDRONE = 2000;

            public const int MODE_ATTENTION = 9999;

            StringBuilder sbData = new StringBuilder(100);
            public void SetMode(int theNewMode, int theNewState = 0)
            {
                // do nothing if we are already in that mode
                if (_iMode == theNewMode)
                    return;
//                if(_bControlDebug) _program.ErrorLog("Set M=" + theNewMode + " S=" + theNewState+" OM="+IMode+" OS="+_iState);

                // possible optimization.. make modules register for what modes they care about...
                sbData.Clear();
                sbData.AppendLine(_iMode.ToString());
                sbData.AppendLine(_iState.ToString());
                sbData.AppendLine(theNewMode.ToString());
                sbData.AppendLine(theNewState.ToString());

                SendToAllSubscribers(MODECHANGETAG, sbData.ToString());
                HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                _iMode = theNewMode;
                _iState = theNewState;
                WantOnce();
            }

            public void SetState(int theNewState)
            {
                // not synced..
//                if (_bControlDebug) _program.ErrorLog("Set S=" + theNewState);

                _iState = theNewState;
            }
            public bool AddControlChangeHandler(Action<int, int, int, int> handler)
            {
                if (!ControlChangeHandlers.Contains(handler))
                    ControlChangeHandlers.Add(handler);
                return true;
            }
            void HandleModeChange(int fromMode, int fromState, int toMode, int toState)
            {
                foreach (var handler in ControlChangeHandlers)
                {
                    handler(fromMode, fromState, toMode, toState);
                }
            }

            public bool AddModeInitHandler(Action handler)
            {
                if (!ModeAfterInitHandlers.Contains(handler))
                    ModeAfterInitHandlers.Add(handler);
                return true;
            }
            public void ModeAfterInit(MyIni theIni)
            {
                _iState = theIni.Get(ControlSection, "State").ToInt32(_iState);
                _iMode = theIni.Get(ControlSection, "Mode").ToInt32(_iMode);

//                _program.ErrorLog("MAI:M=" + _iMode.ToString() + " S=" + _iState.ToString());
//                _program.ErrorLog(_program.Storage);

                foreach (var handler in ModeAfterInitHandlers)
                {
                    handler();
                }
            }

            void SaveHandler(MyIni theIni)
            {
//                _program.ErrorLog("wicocontrol save handler");
//                _program.ErrorLog("WCSH:M=" + _iMode.ToString() + " S=" + _iState.ToString());
                theIni.Set(ControlSection, "Mode", _iMode);
                theIni.Set(ControlSection, "State", _iState);
            }

            #endregion


//            Program _program;
            WicoIGC _wicoIGC;

            readonly TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;
            public WicoControl(Program program, WicoIGC wicoIGC): base(program)
            {
                _program = program;
                _wicoIGC = wicoIGC;

                WicoControlInit();
            }

            /// <summary>
            /// List of Wico PB blocks on local construct
            /// </summary>
            List<long> _WicoMainSubscribers = new List<long>();
            bool bIAmMain = true; // assume we are main

            // Wico Main/Config stuff
            readonly string WicoMainTag = "WicoTagMain";
            readonly string YouAreSub = "YOUARESUB";
            readonly string UnicastTagTrigger = "TRIGGER";
            readonly string UnicastAnnounce = "IAMWICO";

            public void WicoControlInit()
            {
                // Wico Configuration system
                _WicoMainSubscribers.Clear();
                bIAmMain = true;

                // send a messge to all local 'Wico' PBs to get configuration.  
                // This will be used to determine the 'master' PB and to know who to send requests to
                _program.IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);

                _wicoIGC.AddPublicHandler(WicoMainTag, WicoControlMessagehandler, true);
                _wicoIGC.AddUnicastHandler(WicoConfigUnicastListener);

                _program.AddTriggerHandler(ProcessTrigger);

                _bControlDebug = _program.CustomDataIni.Get(_program.OurName, "ControlDebug").ToBoolean(_bControlDebug);
                _program.CustomDataIni.Set(_program.OurName, "ControlDebug", _bControlDebug);

                // ModeAfterInit gets called by main
                _program.AddSaveHandler(SaveHandler);

            }
            public bool IamMain()
            {
                return bIAmMain;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument,MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if (myCommandLine != null && myCommandLine.ArgumentCount > 1)
                {
                    if (myCommandLine.Argument(0) == "setmode")
                    {
                        int toMode = 0;
                        bool bOK = int.TryParse(myCommandLine.Argument(1), out toMode);
                        if (bOK)
                        {
                            SetMode(toMode);
                            WantOnce();
                        }
                    }
                }

            }

            public void SendToAllSubscribers(string tag, string argument)
            {
                foreach (var submodule in _WicoMainSubscribers)
                {
                    if (submodule == _program.Me.EntityId) continue; // skip ourselves if we are in the list.
                    _program.IGC.SendUnicastMessage(submodule, tag, argument);
                }
            }

            /// <summary>
            /// Broadcast handler for Wico Control Messages
            /// </summary>
            /// <param name="msg"></param>
            public void WicoControlMessagehandler(MyIGCMessage msg)
            {
                var tag = msg.Tag;

                var src = msg.Source;
                if (tag == WicoMainTag)
                {
//                    if (_bControlDebug) _program.ErrorLog("WCC:WMT Rvcd from " + src.ToString("X"));
                    if (msg.Data is string)
                    {
                        string data = (string)msg.Data;
                        if (data == "Configure")
                        {
                            _program.IGC.SendUnicastMessage(src, UnicastAnnounce, "");
                        }
                    }
                }
            }

            /// <summary>
            /// Wico Unicast Handler for Wico Main
            /// </summary>
            /// <param name="msg"></param>
            public void WicoConfigUnicastListener(MyIGCMessage msg)
            {
                var tag = msg.Tag;
                var src = msg.Source;
                if (tag == YouAreSub)
                {
                    bIAmMain = false;
                }
                else if (tag == UnicastAnnounce)
                {
//                    if (_bControlDebug) _program.ErrorLog("WCC:UCA Rvcd from " + src.ToString("X"));
                    // another block announces themselves as one of our collective
                    if (_WicoMainSubscribers.Contains(src))
                    {
                        // already in the list
                    }
                    else
                    {
                        // not in the list
//                        _program.Echo("Adding new");
                        _WicoMainSubscribers.Add(src);
                    }
                    bIAmMain = true; // assume we are the main module
                    foreach (var other in _WicoMainSubscribers)
                    {
                        // if somebody has a lower ID, use them instead.
                        if (other < _program.Me.EntityId)
                        {
                            bIAmMain = false;
//                            _program.Echo("Found somebody lower");
                        }
                    }
                }
                else if (tag == UnicastTagTrigger)
                {
                    // we are being informed that we were wanted to run for some reason (misc)
//                    _program.Echo("Trigger Received:" + msg.Data);
                }
                else if (tag == MODECHANGETAG)
                {
                    string[] aLines = ((string)msg.Data).Split('\n');
                    // 0=old mode 1=old state. 2=new mode 3=new state
                    int theNewMode = Convert.ToInt32(aLines[2]);
                    int theNewState = Convert.ToInt32(aLines[3]);

//                    if (_bControlDebug) _program.ErrorLog("IGCS M=" + theNewMode + " S=" + theNewState + " OM=" + IMode + " OS=" + _iState);
                    if (_iMode != theNewMode)
                        HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                    _iMode = theNewMode;
                    _iState = theNewState;
                }
                // TODO: add more messages as needed
            }

            public new void AnnounceState()
            {
                if (_bControlDebug)
                {
//                    _program.Echo("Me=" + _program.Me.EntityId.ToString("X"));
//                    _program.Echo("Subscribers=" + _WicoMainSubscribers.Count());
                }
                if (bIAmMain) _program.Echo("MAIN. Mode=" + IMode.ToString() + " S=" + IState.ToString());
                else _program.Echo("SUB. Mode=" + IMode.ToString() + " S=" + IState.ToString());
            }
        }

    }
}
