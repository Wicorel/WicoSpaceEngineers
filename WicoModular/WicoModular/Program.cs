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

        WicoIGC wicoIGC;

        TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;
        UpdateType utTriggers = UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod | UpdateType.Script;
        UpdateType utUpdates = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once;






        // Surface stuff
        IMyTextSurface mesurface0;
        IMyTextSurface mesurface1;


        public Program()
        {

            wicoIGC = new WicoIGC(this);

            Runtime.UpdateFrequency = UpdateFrequency.Once; // cause ourselves to run again to continue initialization

            WicoConfigurationInit();

            // Local PB Surface Init
            mesurface0 = Me.GetSurface(0);
            mesurface1 = Me.GetSurface(1);
            mesurface0.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            mesurface0.WriteText("Wicorel Modular");
            mesurface0.FontSize = 2;
            mesurface0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;

            mesurface1.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            mesurface1.WriteText("Version: 1");
            mesurface1.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            mesurface1.TextPadding = 0.25f;
            mesurface1.FontSize = 3.5f;

        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.IGC) > 0)
            {
                Echo("IGC");
                wicoIGC.ProcessIGCMessages();
                if (bIAmMain)
                    mesurface1.WriteText("Master Module");
                else
                    mesurface1.WriteText("Sub Module");
            }
            if ((updateSource & (utTriggers)) > 0)
            {
                Echo("Triggers");
                if (bIAmMain)
                {
                    foreach (var submodule in _WicoMainSubscribers)
                    {
                        IGC.SendUnicastMessage(submodule, UnicastTagTrigger, argument);
                    }
                }
            }
            if ((updateSource & (utUpdates)) > 0)
            {
                Echo("Update");
            }
            Echo("I Am Main=" + bIAmMain.ToString());
        }


        #region WicoConfiguration
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

        void WicoConfigurationInit()
        {
            // Wico Configuration system

            // send a messge to all local 'Wico' PBs to get configuration.  This will be used to determine the 'master' PB
            IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);

            _WicoMainSubscribers.Clear();

            wicoIGC.AddPublicHandler(WicoMainTag, WicoMainMessagehandler);
            wicoIGC.AddUnicastHandler(WicoConfigUnicastListener);
        }

        /// <summary>
        /// Broadcast handler for Wico Main Messages
        /// </summary>
        /// <param name="msg"></param>
        void WicoMainMessagehandler(MyIGCMessage msg)
        {
            var tag = msg.Tag;

            //            Echo("WMMH:"+tag);

            var src = msg.Source;
            if (tag == WicoMainTag)
            {
                string data = (string)msg.Data;
                if (data == "Configure")
                {
                    IGC.SendUnicastMessage(src, UnicastAnnounce, "");
                }
            }
        }

        /// <summary>
        /// Wico Unicast Handler for Wico Main
        /// </summary>
        /// <param name="msg"></param>
        void WicoConfigUnicastListener(MyIGCMessage msg)
        {
            var tag = msg.Tag;
            //`            Echo("WCUL:" + tag);
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
                    Echo("Adding new");
                    _WicoMainSubscribers.Add(src);
                }
                bIAmMain = true;
                foreach (var other in _WicoMainSubscribers)
                {
                    // if somebody as a lower ID, use them instead.
                    if (other < Me.EntityId)
                    {
                        bIAmMain = false;
                        Echo("Found somebody lower");
                    }
                }
            }
            else if (tag == UnicastTagTrigger)
            {
                // we are being informed that we were wanted to run for some reason (misc)
                Echo("Trigger Received" + msg.Data);
            }
            // TODO: add more messages for state changes, etc.
        }

        #endregion

        #region WicoIGC
        class WicoIGC
        {
            // the one and only unicast listener.  Must be shared amoung all interested parties
            IMyUnicastListener myUnicastListener;

            /// <summary>
            /// the list of unicast message handlers. All handlers will be called on pending messages
            /// </summary>
            List<Action<MyIGCMessage>> unicastMessageHandlers = new List<Action<MyIGCMessage>>();


            /// <summary>
            /// List of 'registered' broadcst message handlers.  All handlers will be called on each message received
            /// </summary>
            List<Action<MyIGCMessage>> broadcastMessageHandlers = new List<Action<MyIGCMessage>>();
            /// <summary>
            /// List of broadcast channels.  All channels will be checked for incoming messages
            /// </summary>
            List<IMyBroadcastListener> broadcastChanels = new List<IMyBroadcastListener>();

            MyGridProgram gridProgram;

            public WicoIGC(MyGridProgram myProgram)
            {
                gridProgram = myProgram;
            }

            public bool AddPublicHandler(string ChannelTag, Action<MyIGCMessage> handler, bool bCallBack = true)
            {
                IMyBroadcastListener _PublicChannel;
                // IGC Init
                _PublicChannel = gridProgram.IGC.RegisterBroadcastListener(ChannelTag); // What it listens for
                if(bCallBack) _PublicChannel.SetMessageCallback(ChannelTag); // What it will run the PB with once it has a message

                // add broadcast message handlers
                broadcastMessageHandlers.Add(handler);

                // add to list of channels to check
                broadcastChanels.Add(_PublicChannel);
                return true;
            }

            public bool AddUnicastHandler(Action<MyIGCMessage> handler)
            {
                myUnicastListener = gridProgram.IGC.UnicastListener;
                myUnicastListener.SetMessageCallback();
                unicastMessageHandlers.Add(handler);
                return true;

            }
            /// <summary>
            /// Process all pending IGC messages
            /// </summary>
            public void ProcessIGCMessages()
            {
                // TODO: make this a yield return thing if processing takes too long

                bool bFoundMessages = false;
                //            Echo(broadcastChanels.Count.ToString() + " broadcast channels");
                //            Echo(broadcastMessageHandlers.Count.ToString() + " broadcast message handlers");
                //            Echo(unicastMessageHandlers.Count.ToString() + " unicast message handlers");
                do
                {
                    bFoundMessages = false;
                    foreach (var channel in broadcastChanels)
                    {
                        if (channel.HasPendingMessage)
                        {
                            bFoundMessages = true;
                            var msg = channel.AcceptMessage();
                            foreach (var handler in broadcastMessageHandlers)
                            {
                                handler(msg);
                            }
                        }
                    }
                } while (bFoundMessages); // Process all pending messages

                if (myUnicastListener != null)
                {
                    do
                    {
                        // since there's only one channel, we could just use .HasPendingMessages directly.. but this keeps the code loops the same
                        bFoundMessages = false;

                        if (myUnicastListener.HasPendingMessage)
                        {
                            bFoundMessages = true;
                            var msg = myUnicastListener.AcceptMessage();
                            foreach (var handler in unicastMessageHandlers)
                            {
                                // Call each handler
                                handler(msg);
                            }
                        }
                    } while (bFoundMessages); // Process all pending messages
                }

            }
        }
        #endregion



    }
}