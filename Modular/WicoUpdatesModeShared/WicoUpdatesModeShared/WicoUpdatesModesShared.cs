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
        public class WicoUpdateModesShared: WicoUpdatesModes
        {
            public WicoUpdateModesShared(Program program) : base(program)
            {
                thisProgram = program;

                WicoControlInit();
            }
            new void WicoControlInit()
            {
                thisProgram.IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);

                _WicoMainSubscribers.Clear();
                thisProgram.wicoIGC.AddPublicHandler(WicoMainTag, WicoControlMessagehandler);
                thisProgram.wicoIGC.AddUnicastHandler(WicoConfigUnicastListener);
            }

            readonly TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;

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
            const string MODECHANGETAG = "[WICOMODECHANGE]";

            public override void HandleModeChange(int fromMode, int fromState, int toMode, int toState)
            {
                // possible optimization.. make modules register for what modes they care about...
                string sData = "";
                sData += _iMode.ToString() + "\n";
                sData += _iState.ToString() + "\n";
                sData += toMode.ToString() + "\n";
                sData += toState.ToString() + "\n";
                SendToAllSubscribers(MODECHANGETAG, sData);

                base.HandleModeChange(fromMode, fromState, toMode, toState);

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

                var src = msg.Source;
                if (tag == WicoMainTag)
                {
                    if (msg.Data is string)
                    {
                        string data = (string)msg.Data;
                        if (data == "Configure")
                        {
                            thisProgram.IGC.SendUnicastMessage(src, UnicastAnnounce, "");
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
                        if (other < thisProgram.Me.EntityId)
                        {
                            bIAmMain = false;
                            //                            _program.Echo("Found somebody lower");
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

                    if (_iMode != theNewMode)
                        HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                    _iMode = theNewMode;
                    _iState = theNewState;
                }
                // TODO: add more messages as needed
            }
            new public void AnnounceState()
            {
                if (_bDebug)
                {
                    thisProgram.Echo("Me=" + thisProgram.Me.EntityId.ToString("X"));
                    thisProgram.Echo("Subscribers=" + _WicoMainSubscribers.Count());
                }
                if (bIAmMain) thisProgram.Echo("MAIN. Mode=" + IMode.ToString() + " S=" + IState.ToString());
                else thisProgram.Echo("SUB. Mode=" + IMode.ToString() + " S=" + IState.ToString());
            }

        }
    }
}
