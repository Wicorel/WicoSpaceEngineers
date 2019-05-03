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
        #region WicoControl

        class WicoControl
        {
            #region MODES
            const string MODECHANGETAG = "[WICOMODECHANGE]";
            int _iMode = -1;
            int _iState = -1;
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

            public const int MODE_IDLE = 0;

            public const int MODE_DOCKING = 30;
            public const int MODE_DOCKED = 40;
            public const int MODE_LAUNCH = 50; // space launch

            public const int MODE_LAUNCHPREP = 100; // oribital launch prep
            public const int MODE_ORBITALLAUNCH = 120;
            public const int MODE_DESCENT = 150;
            public const int MODE_HOVER = 170;
            public const int MODE_LANDED = 180;

            public const int MODE_MINE = 500;


            public const int MODE_STARTNAV = 600; // start the navigation operations
            public const int MODE_GOINGTARGET = 650;
            public const int MODE_NAVNEXTTARGET = 670; // go to the next target
            public const int MODE_ARRIVEDTARGET = 699; // we have arrived at target

            public float fMaxWorldMps = 100f;

            public void SetMode(int theNewMode, int theNewState = 0)
            {
                // do nothing if we are already in that mode
                if (_iMode == theNewMode)
                    return;

                string sData = "";
                sData += _iMode.ToString() + "\n";
                sData += _iState.ToString() + "\n";
                sData += theNewMode.ToString() + "\n";
                sData += theNewState.ToString() + "\n";

                SendToAllSubscribers(MODECHANGETAG, sData);
                HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                _iMode = theNewMode;
                _iState = theNewState;
            }

            public void SetState(int theNewState)
            {
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

            #endregion

            #region Updates
            bool bWantOnce = false;
            bool bWantFast = false;
            bool bWantMedium = false;
            bool bWantSlow = false;
            
            public void ResetUpdates()
            {
                bWantOnce = false;
                bWantFast = false;
                bWantMedium = false;
                bWantSlow = false;
            }
            public void WantOnce()
            {
                bWantOnce = true;
            }
            public void WantFast()
            {
                bWantFast = true;
            }
            public void WantMedium()
            {
                bWantMedium = true;
            }
            public void WantSlow()
            {
                bWantSlow = true;
            }
            public UpdateFrequency GenerateUpdate()
            {
                UpdateFrequency desired = 0;
                if (bWantOnce) desired |= UpdateFrequency.Once;
                if (bWantFast) desired |= UpdateFrequency.Update1;
                if (bWantMedium) desired |= UpdateFrequency.Update10;
                if (bWantSlow) desired |= UpdateFrequency.Update100;
                return desired;
            }
            #endregion

            Program thisProgram;
            readonly TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;
            public WicoControl(Program program)
            {
                thisProgram = program;


                WicoControlInit();
            }


            /// <summary>
            /// List of Wico PB blocks on local construct
            /// </summary>
            List<long> _WicoMainSubscribers = new List<long>();
            bool bIAmMain = true; // assume we are main

            // WIco Main/Config stuff
            readonly string WicoMainTag = "WicoTagMain";
            readonly string YouAreSub = "YOUARESUB";
            readonly string UnicastTagTrigger = "TRIGGER";
            readonly string UnicastAnnounce = "IAMWICO";

            public void WicoControlInit()
            {
                // Wico Configuration system
                //TODO: Load defaults from CustomData
                //thisProgram._CustomDataIni;

                //TODO: load last mode/state from Storage
                //thisProgram._SaveIni;

                // send a messge to all local 'Wico' PBs to get configuration.  
                // This will be used to determine the 'master' PB and to know who to send requests to
                thisProgram.IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);

                _WicoMainSubscribers.Clear();

                thisProgram.wicoIGC.AddPublicHandler(WicoMainTag, WicoControlMessagehandler);
                thisProgram.wicoIGC.AddUnicastHandler(WicoConfigUnicastListener);

                thisProgram.UpdateTriggerHandlers.Add(ProcessTrigger);

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
            public void ProcessTrigger(MyCommandLine myCommandLine, UpdateType updateSource)
            {
                //                string[] args = argument.Trim().Split(' ');
                if (myCommandLine != null)
                {
                    if (myCommandLine.Argument(0) == "setmode")
                    {
                        int theNewState = 0;
                        if (myCommandLine.Argument(1)!=null)
                        {
                            int theNewMode = Convert.ToInt32(myCommandLine.Argument(1));
                            if (myCommandLine.Argument(2)!=null)
                            {
                                theNewState = Convert.ToInt32(myCommandLine.Argument(2));
                            }
                            SetMode(theNewMode, theNewState);
                        }
                        else thisProgram.Echo("Invalid Syntax");
                    }
                }
                // else no arguments

/*
                // Debugging/play information
                if (bIAmMain)
                {
                    SendToAllSubscribers(UnicastTagTrigger, argument);
                }
                else thisProgram.Echo("Trigger found, not not master");
                */
            }

            public void SendToAllSubscribers(string tag, string argument)
            {
                foreach (var submodule in _WicoMainSubscribers)
                {
                    if (submodule == thisProgram.Me.EntityId) continue; // skip ourselves if we are in the list.
                    thisProgram.IGC.SendUnicastMessage(submodule, tag, argument);
                }
            }

            /// <summary>
            /// Broadcast handler for Wico Control Messages
            /// </summary>
            /// <param name="msg"></param>
            public void WicoControlMessagehandler(MyIGCMessage msg)
            {
                var tag = msg.Tag;

                //            Echo("WMMH:"+tag);

                var src = msg.Source;
                if (tag == WicoMainTag)
                {
                    string data = (string)msg.Data;
                    if (data == "Configure")
                    {
                        thisProgram.IGC.SendUnicastMessage(src, UnicastAnnounce, "");
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
                    // another block announces themselves as one of our collective
                    if (_WicoMainSubscribers.Contains(src))
                    {
                        // already in the list
                    }
                    else
                    {
                        // not in the list
                        thisProgram.Echo("Adding new");
                        _WicoMainSubscribers.Add(src);
                    }
                    bIAmMain = true;
                    foreach (var other in _WicoMainSubscribers)
                    {
                        // if somebody has a lower ID, use them instead.
                        if (other < thisProgram.Me.EntityId)
                        {
                            bIAmMain = false;
                            thisProgram.Echo("Found somebody lower");
                        }
                    }
                }
                else if (tag == UnicastTagTrigger)
                {
                    // we are being informed that we were wanted to run for some reason (misc)
                    thisProgram.Echo("Trigger Received" + msg.Data);
                }
                else if (tag == MODECHANGETAG)
                {
                    string[] aLines = ((string)msg.Data).Split('\n');
                    // 0=old mode 1=old state. 2=new mode 3=new state
                    int theNewMode = Convert.ToInt32(aLines[2]);
                    int theNewState = Convert.ToInt32(aLines[3]);
                    _iMode = theNewMode;
                    _iState = theNewState;

                }
                // TODO: add more messages as needed
            }




        }
        #endregion

    }
}
