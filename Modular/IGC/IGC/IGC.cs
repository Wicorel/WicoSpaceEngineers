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
        // Source is available from: https://github.com/Wicorel/WicoSpaceEngineers/tree/master/Modular/IGC
        public class WicoIGC
        {
            // the one and only unicast listener.  Must be shared amoung all interested parties
            IMyUnicastListener _unicastListener;

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

            Program _program;
            bool _debug = false;
            IMyTextPanel _debugTextPanel;

            string WicoIGCSection = "WicoIGC";
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="myProgram"></param>
            /// <param name="debug"></param>
            public WicoIGC(Program myProgram)
            {
                _program = myProgram;

                //                _debug = debug;
                _debug = _program._CustomDataIni.Get(WicoIGCSection, "Debug").ToBoolean(_debug);
                _program._CustomDataIni.Set(WicoIGCSection, "Debug", _debug);

                _debugTextPanel = _program.GridTerminalSystem.GetBlockWithName("IGC Report") as IMyTextPanel;
                if (_debug) _debugTextPanel?.WriteText("");
            }

            /// <summary>
            /// Call to add a handler for public messages.  Also registers the tag with IGC for reception.
            /// </summary>
            /// <param name="channelTag">The tag for the channel.  This should be unique to the use of the channel.</param>
            /// <param name="handler">The handler for messages when received. Note that this handler will be called with ALL broadcast messages; not just the one from ChannelTag</param>
            /// <param name="setCallback">Should a callback be set on the channel. The system will call Main() when the IGC message is received.</param>
            /// <returns></returns>
            public bool AddPublicHandler(string channelTag, Action<MyIGCMessage> handler, bool setCallback = true)
            {
                IMyBroadcastListener publicChannel;
                // IGC Init
                publicChannel = _program.IGC.RegisterBroadcastListener(channelTag); // What it listens for
                if (setCallback) publicChannel.SetMessageCallback(channelTag); // What it will run the PB with once it has a message

                // add broadcast message handlers
                if(!_broadcastMessageHandlers.Contains(handler))
                    _broadcastMessageHandlers.Add(handler);

                // add to list of channels to check
                if(!_broadcastChannels.Contains(publicChannel))
                   _broadcastChannels.Add(publicChannel);

                return true;
            }

            /// <summary>
            /// Add a unicast handler.
            /// </summary>
            /// <param name="handler">The handler for messages when received. Note that this handler will be called with ALL Unicast messages. Always sets a callback handler</param>
            /// <returns></returns>
            public bool AddUnicastHandler(Action<MyIGCMessage> handler)
            {
                _unicastListener = _program.IGC.UnicastListener;
                _unicastListener.SetMessageCallback("UNICAST");
                if(!_unicastMessageHandlers.Contains(handler))
                    _unicastMessageHandlers.Add(handler);
                return true;

            }
            /// <summary>
            /// Process all pending IGC messages.
            /// </summary>
            public void ProcessIGCMessages()
            {
                bool bFoundMessages = false;
                if (_debug) _program.Echo(_broadcastChannels.Count.ToString() + " broadcast channels");
                if (_debug) _program.Echo(_broadcastMessageHandlers.Count.ToString() + " broadcast message handlers");
                if (_debug) _program.Echo(_unicastMessageHandlers.Count.ToString() + " unicast message handlers");


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
                            if (_debug)
                            {
                                _program.Echo("Broadcast received. TAG:" + msg.Tag);
                                _debugTextPanel?.WriteText("IGC:" +msg.Tag+" SRC:"+msg.Source.ToString("X")+"\n",true);
                            }
                            foreach (var handler in _broadcastMessageHandlers)
                            {
                                if (_debug) _program.Echo("Calling handler");
                                handler(msg);
                            }
                            if (_debug) _program.Echo("Broadcast Handlers completed");
                        }
                    }
                } while (bFoundMessages); // Process all pending messages

                if (_unicastListener != null)
                {
                    if (_debug) _program.Echo("Unicast check");

                    // TODO: make this a yield return thing if processing takes too long
                    do
                    {
                        // since there's only one channel, we could just use .HasPendingMessages directly.. but this keeps the code loops the same
                        bFoundMessages = false;

                        if (_unicastListener.HasPendingMessage)
                        {
                            bFoundMessages = true;
                            var msg = _unicastListener.AcceptMessage();
                            if (_debug) _program.Echo("Unicast received. TAG:" + msg.Tag);
                            foreach (var handler in _unicastMessageHandlers)
                            {
                                if (_debug) _program.Echo(" Unicast Handler");
                                // Call each handler
                                handler(msg);
                            }
                            if (_debug) _program.Echo("Broadcast Handlers completed");
                        }
                    } while (bFoundMessages); // Process all pending messages
                    if (_debug) _program.Echo("Unicast check completed");
                }

            }

            /// <summary>
            /// Set debug mode
            /// </summary>
            /// <param name="debug"></param>
            public void SetDebug(bool debug)
            {
                _debug = debug;
                if (_debug) _debugTextPanel?.WriteText("");
            }
        }
    }
}
