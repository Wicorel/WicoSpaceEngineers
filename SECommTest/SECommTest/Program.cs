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
        IMyUnicastListener _uListener;
        IMyBroadcastListener _bListener;

        const string BroadcastTag = "[WICO_BROADCAST]";
        const string UnicastTag = "[WICO_UNICAST]";

        public Program()
        {
            //           Runtime.UpdateFrequency = UpdateFrequency.Once;
            Echo("Creator.");
            Echo("Me=" + Me.EntityId.ToString());
            Echo(Me.CubeGrid.CustomName);

            _bListener = IGC.RegisterBroadcastListener(BroadcastTag); // What it listens for
            _bListener.SetMessageCallback(BroadcastTag); // What it will run the PB with once it has a message

            _uListener = IGC.UnicastListener;
            _uListener.SetMessageCallback(UnicastTag);
        }

        public void Save()
        {
        }

        int runcount = 0;

        public void Main(string argument, UpdateType updateSource)
        {
            Echo(updateSource.ToString());
            Echo("Me=" + Me.EntityId.ToString());
            Echo(Me.CubeGrid.CustomName);
            runcount++;
            Echo("Runs=" + runcount.ToString());

            if ( _bListener.HasPendingMessage || _uListener.HasPendingMessage)
//                if (argument == UnicastTag || _bListener.HasPendingMessage || _uListener.HasPendingMessage)
            {
                    if (!HandleMessages())
                    return;
            }
            else
            {
                Echo("Sending Broadcast Message");
//                IGC.SendBroadcastMessage<string>(BroadcastTag, "Me=" + Me.EntityId.ToString() + "\n" + argument, TransmissionDistance.CurrentConstruct);
                IGC.SendBroadcastMessage<string>(BroadcastTag, "Me=" + Me.EntityId.ToString() +":"+Me.CubeGrid.CustomName + "\n" + argument, TransmissionDistance.AntennaRelay);
            }
        }
        bool  HandleMessages()
        {
            int incomingCount = 1;
            do
            {
                Echo("Message "+incomingCount.ToString());
                bool bBroadcast = _bListener.HasPendingMessage;
                if (bBroadcast) Echo("Broadcast"); else Echo("Unicast");
                var msg = _bListener.HasPendingMessage ? _bListener.AcceptMessage() : _uListener.AcceptMessage();
                Echo("Received Message");
                Echo(msg.ToString());
                var src = msg.Source;
                Echo("Source=" + src.ToString());
                Echo("Data=" + msg.Data);
                Echo("Tag=" + msg.Tag);
                // if(IGC.IsEndpointReachable(msg.Source)) {}
                if (bBroadcast)
                    if (IGC.SendUnicastMessage<string>(msg.Source, UnicastTag, "Message received by:" + Me.EntityId.ToString()))
                    {
                        Echo("Response Sent");

                    }
                    else Echo("Error sending response");
                Echo("----");
                incomingCount++;
            } while (_bListener.HasPendingMessage || _uListener.HasPendingMessage);
            return true;
        }
    }

}