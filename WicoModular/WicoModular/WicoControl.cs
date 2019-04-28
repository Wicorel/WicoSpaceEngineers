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

            // WIco Main/Config stuff
            string WicoMainTag = "WicoTagMain";

            bool bIAmMain = true; // assume we are main
            string YouAreSub = "YOUARESUB";
            string UnicastTagTrigger = "TRIGGER";
            string UnicastAnnounce = "IAMWICO";

            public void WicoControlInit()
            {
                // Wico Configuration system
                //TODO: Load defaults from CustomData


                // send a messge to all local 'Wico' PBs to get configuration.  This will be used to determine the 'master' PB
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
            public void ProcessTrigger(string argument, UpdateType updateSource)
            {
                if (bIAmMain)
                {
                    SendToAllSubscribers(UnicastTagTrigger, argument);
                }
                else thisProgram.Echo("Trigger found, not not master");
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
                // TODO: add more messages for state changes, etc.
            }

        }
        #endregion

    }
}
