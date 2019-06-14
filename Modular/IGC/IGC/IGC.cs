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
                if (bCallBack) _PublicChannel.SetMessageCallback(ChannelTag); // What it will run the PB with once it has a message

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
