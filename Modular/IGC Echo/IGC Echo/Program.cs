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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        /// <summary>
        /// A count of how many times this script has been run
        /// </summary>
        int _runcount = 0;

        /// <summary>
        /// This is our unique ID for our message.  We've defined the format for the message data (it's just a string)
        /// </summary>
        string _broadCastTag = "MDK IGC EXAMPLE 1";

        /// <summary>
        /// The broadcast listener for the channel we are interested in.
        /// </summary>
        IMyBroadcastListener _myBroadcastListener;

        public Program()
        {
            // let them know we are alive
            Echo("Creator");

            // register a broadcast channel for our tag
            _myBroadcastListener=IGC.RegisterBroadcastListener(_broadCastTag);

            // Ask to be called back --to Main()-- when a message is received
            _myBroadcastListener.SetMessageCallback(_broadCastTag); // the callback agrument does NOT need to be the same as the tag
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // Calculate and Output for informational purposes
            _runcount++;
            Echo(_runcount.ToString()+ ":"+updateSource.ToString());

            if (
                (updateSource & (UpdateType.Trigger | UpdateType.Terminal)) > 0 // run by a terminal action
                || (updateSource & (UpdateType.Mod)) > 0 // script run by a mod
                || (updateSource & (UpdateType.Script)) > 0 // this pb run by another script (PB)
                )
            { // script was run because of an action
                if (argument != "")
                {
                    // if we are given an argument, send it out over our broadcast channel
                    IGC.SendBroadcastMessage(_broadCastTag, argument);
                    Echo("Sending message:\n" + argument);
                }
            }

            if( (updateSource& UpdateType.IGC) >0)
            { // script was run because of incoming IGC message
                if (_myBroadcastListener.HasPendingMessage)
                {
                    MyIGCMessage myIGCMessage = _myBroadcastListener.AcceptMessage();
                    if(myIGCMessage.Tag==_broadCastTag)
                    { // This is our tag
                        if(myIGCMessage.Data is string)
                        {
                            string str = myIGCMessage.Data.ToString();
                            Echo("Received IGC Public Message");
                            Echo("Tag=" + myIGCMessage.Tag);
                            Echo("Data=" + myIGCMessage.Data.ToString());
                            Echo("Source=" + myIGCMessage.Source.ToString("X"));
                        }
                        else // if(msg.Data is XXX)
                        {
                            // handle other data types here...
                        }
                    }
                    else
                    {
                        // handle other tags here
                    }
                }
            }
        }

    }
}
