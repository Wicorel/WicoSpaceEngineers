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
    partial class Program : MyGridProgram
    {
        IMyUnicastListener _uListener;// There is only ONE unicast listener.  This for THIS PB
        IMyBroadcastListener _bListener; // Can have multiple broadcast listeners.  Each will need to be registered.

        const string BroadcastTag = "[WICO_TESTBROADCAST]";
        const string UnicastTag = "[WICO_TESTUNICAST]";

        /// <summary>
        /// The combined set of UpdateTypes that count as a 'trigger'
        /// </summary>
        UpdateType utTriggers = UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod | UpdateType.Script;
        /// <summary>
        /// the combined set of UpdateTypes and count as an 'Update'
        /// </summary>
        UpdateType utUpdates = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once;

        public Program()
        {
            // Echo some information about 'me' when I'm compiled
            Echo("Creator.");
            Echo("Me=" + Me.EntityId.ToString());
            Echo(Me.CubeGrid.CustomName);

            // register a broadcast listener
            _bListener = IGC.RegisterBroadcastListener(BroadcastTag); // What it listens for
            _bListener.SetMessageCallback(BroadcastTag); // What it will run the PB with once it has a message

            // save the unicast listener
            _uListener = IGC.UnicastListener;
            _uListener.SetMessageCallback(UnicastTag); // set PB callback argument to be used
        }

        public void Save()
        {
        }


        int runcount = 0; // used to show running multiple times
        int incomingMessageCount = 0; // number of messages we have received

        public void Main(string argument, UpdateType updateSource)
        {
            // Echo some information aboue 'me' and why we were run
            Echo("Source=" + updateSource.ToString());
            Echo("Me=" + Me.EntityId.ToString("X"));
            Echo(Me.CubeGrid.CustomName);


            runcount++;
            Echo("Runs=" + runcount.ToString());

            Echo("-_-_-");
            // if there is a message pending, process it
            if ( _bListener.HasPendingMessage || _uListener.HasPendingMessage)
            {
                HandleMessages();
            }

            if((updateSource & utTriggers) >0)
            {
                // if we were run by a trigger, then send out a broadcast message with the argument
                Echo("Sending Broadcast Message");
                IGC.SendBroadcastMessage<string>(BroadcastTag, "Me=" + Me.EntityId.ToString() +":"+Me.CubeGrid.CustomName + "\n" + argument, TransmissionDistance.AntennaRelay);
            }

            Echo("Received " + incomingMessageCount + " total msgs");
        }

        // Handle the available messages
        bool  HandleMessages()
        {
            // check for a pending message
            if (!_bListener.HasPendingMessage && !_uListener.HasPendingMessage)
                return true;

            int incomingCount = 0; // keep a count of how many messages we've processed
            do
            {
                incomingCount++;
                incomingMessageCount++;

                Echo("Message "+incomingCount.ToString());

                // check broadcast first, then unicast
                bool bBroadcast = _bListener.HasPendingMessage;
                if (bBroadcast) Echo("Broadcast"); else Echo("Unicast");

                var msg = _bListener.HasPendingMessage ? _bListener.AcceptMessage() : _uListener.AcceptMessage();

                // information about the received message
                Echo("Received Message");
//                Echo(msg.ToString());
                var src = msg.Source;
                Echo("Source=" + src.ToString("X"));
                Echo("Data=\"" + msg.Data+"\"");
                Echo("Tag=" + msg.Tag);



                // If we got a brodcast message, reply with a unicast message to the sender
                if (bBroadcast)
                {
                    // we could check to see if the source of the message is still reachable (destroyed, out of range, etc)
                    // if(IGC.IsEndpointReachable(msg.Source)) {}
                    if (IGC.SendUnicastMessage<string>(msg.Source, UnicastTag, "Message received by:" + Me.EntityId.ToString("X")))
                    {
                        Echo("Response Sent");
                    }
                    else Echo("Error sending response");
                }
                Echo("----");
            } while (_bListener.HasPendingMessage || _uListener.HasPendingMessage); // Process all pending messages
            return true;
        }
    }

}