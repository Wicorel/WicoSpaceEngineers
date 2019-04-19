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

        // the one and only unicast listener.  Must be shared amoung all interested parties
        IMyUnicastListener myUnicastListener;


        IMyBroadcastListener _WicoMainTag;
        string WicoMainTag="WicoTagMain";
        //TODO: make list of listeners and the 'handlers' for those listeners


        TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;
        UpdateType utTriggers = UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod | UpdateType.Script;
        UpdateType utUpdates = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once;


        List<long> _WicoMainSubscribers = new List<long>();

        bool bIAmMain = true; // assume we are main
        string YouAreSub = "YOUARESUB";
        string UnicastTagTrigger = "TRIGGER";
        string UnicastAnnounce = "IAMWICO";


        // Surface stuff
        IMyTextSurface mesurface0;
        IMyTextSurface mesurface1;


        public Program()
        {
            // IGC Init
            _WicoMainTag = IGC.RegisterBroadcastListener(WicoMainTag); // What it listens for
            _WicoMainTag.SetMessageCallback(WicoMainTag); // What it will run the PB with once it has a message
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);

            myUnicastListener=IGC.UnicastListener;
            myUnicastListener.SetMessageCallback();


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
 //           Echo("I Am Main=" + bIAmMain.ToString());
            if ((updateSource & UpdateType.IGC) > 0)
            {
                Echo("IGC");
                ProcessIGCMessages();
                if(bIAmMain)
                    mesurface1.WriteText("Master Module");
                else
                    mesurface1.WriteText("Sub Module");
            }
            if ((updateSource&(utTriggers)) >0 )
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
            /*
            Echo(_WicoMainSubscribers.Count.ToString());
            foreach(var subscriber in _WicoMainSubscribers)
            {
                Echo("  " + subscriber.ToString());
            }
            */
        }

        void ProcessIGCMessages()
        {
            // TODO: Make list of all broadcast listeners and 'handlers' for each
            do
            {
                if (_WicoMainTag.HasPendingMessage)
                {
                    var msg = _WicoMainTag.AcceptMessage();
                    var src = msg.Source;
                    string data = (string)msg.Data;
                    var tag = msg.Tag;
                    if(data=="Configure")
                    {
                        IGC.SendUnicastMessage(src, UnicastAnnounce, "");
                    }

                }
            } while (_WicoMainTag.HasPendingMessage); // Process all pending messages

            do
            {
                if (myUnicastListener.HasPendingMessage)
                {
                    var msg = myUnicastListener.AcceptMessage();
                    var tag = msg.Tag;
                    var src = msg.Source;
                    if(tag== YouAreSub)
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
                        foreach(var other in _WicoMainSubscribers)
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
                        Echo("Trigger Received" + msg.Data);
                    }
                }
            } while (myUnicastListener.HasPendingMessage); // Process all pending messages

        }
    }
}