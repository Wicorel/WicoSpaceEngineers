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
            IMyUnicastListener _UnicastListener;

            /// <summary>
            /// the list of unicast message handlers. All handlers will be called on pending messages
            /// </summary>
            List<Action<MyIGCMessage>> _unicastMessageHandlers = new List<Action<MyIGCMessage>>();

            /// <summary>
            /// List of 'registered' broadcst message handlers.  All handlers will be called on each message received
            /// </summary>
            List<Action<MyIGCMessage>> _broadcastMessageHandlers = new List<Action<MyIGCMessage>>();
            /// <summary>
            /// List of broadcast channels.  All channels will be checked for incoming messages
            /// </summary>
            List<IMyBroadcastListener> _broadcastChannels = new List<IMyBroadcastListener>();

            MyGridProgram _gridProgram;
            bool _Debug = false;
            IMyTextPanel _DebugTextPanel;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="myProgram"></param>
            /// <param name="debug"></param>
            public WicoIGC(MyGridProgram myProgram, bool debug = false)
            {
                _gridProgram = myProgram;
                _Debug = debug;
                _DebugTextPanel = _gridProgram.GridTerminalSystem.GetBlockWithName("IGC Report") as IMyTextPanel;
                if (_Debug) _DebugTextPanel?.WriteText("");
            }

            /// <summary>
            /// Call to add a handler for public messages.  Also registers the tag with IGC for reception.
            /// </summary>
            /// <param name="ChannelTag">The tag for the channel.  This should be unique to the use of the channel.</param>
            /// <param name="handler">The handler for messages when received. Note that this handler will be called with ALL broadcast messages; not just the one from ChannelTag</param>
            /// <param name="bCallBack">Should a callback be set on the channel. The system will call Main() when the IGC message is received.</param>
            /// <returns></returns>
            public bool AddPublicHandler(string ChannelTag, Action<MyIGCMessage> handler, bool bCallBack = true)
            {
                IMyBroadcastListener _PublicChannel;
                // IGC Init
                _PublicChannel = _gridProgram.IGC.RegisterBroadcastListener(ChannelTag); // What it listens for
                if (bCallBack) _PublicChannel.SetMessageCallback(ChannelTag); // What it will run the PB with once it has a message

                // add broadcast message handlers
                _broadcastMessageHandlers.Add(handler);

                // add to list of channels to check
                _broadcastChannels.Add(_PublicChannel);
                return true;
            }

            /// <summary>
            /// Add a unicast handler.
            /// </summary>
            /// <param name="handler">The handler for messages when received. Note that this handler will be called with ALL Unicast messages.</param>
            /// <returns></returns>
            public bool AddUnicastHandler(Action<MyIGCMessage> handler)
            {
                _UnicastListener = _gridProgram.IGC.UnicastListener;
                _UnicastListener.SetMessageCallback();
                _unicastMessageHandlers.Add(handler);
                return true;

            }
            /// <summary>
            /// Process all pending IGC messages.
            /// </summary>
            public void ProcessIGCMessages()
            {

                bool bFoundMessages = false;
                if (_Debug) _gridProgram.Echo(_broadcastChannels.Count.ToString() + " broadcast channels");
                if (_Debug) _gridProgram.Echo(_broadcastMessageHandlers.Count.ToString() + " broadcast message handlers");
                if (_Debug) _gridProgram.Echo(_unicastMessageHandlers.Count.ToString() + " unicast message handlers");
                // TODO: make this a yield return thing if processing takes too long
                do
                {
                    bFoundMessages = false;
                    foreach (var channel in _broadcastChannels)
                    {
                        if (channel.HasPendingMessage)
                        {
                            bFoundMessages = true;
                            var msg = channel.AcceptMessage();
                            if (_Debug)
                            {
                                _gridProgram.Echo("Broadcast received. TAG:" + msg.Tag);
                                _DebugTextPanel?.WriteText("IGC:" +msg.Tag+" SRC:"+msg.Source.ToString("X")+"\n",true);
                            }
                            foreach (var handler in _broadcastMessageHandlers)
                            {
                                handler(msg);
                            }
                        }
                    }
                } while (bFoundMessages); // Process all pending messages

                if (_UnicastListener != null)
                {
                    // TODO: make this a yield return thing if processing takes too long
                    do
                    {
                        // since there's only one channel, we could just use .HasPendingMessages directly.. but this keeps the code loops the same
                        bFoundMessages = false;

                        if (_UnicastListener.HasPendingMessage)
                        {
                            bFoundMessages = true;
                            var msg = _UnicastListener.AcceptMessage();
                            if (_Debug) _gridProgram.Echo("Unicast received. TAG:" + msg.Tag);
                            foreach (var handler in _unicastMessageHandlers)
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
