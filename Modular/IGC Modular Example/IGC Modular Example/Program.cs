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
        WicoIGC _wicoIGC;

        /// <summary>
        /// The combined set of UpdateTypes that count as a 'trigger'
        /// </summary>
        UpdateType _utTriggers = UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod | UpdateType.Script;
        /// <summary>
        /// the combined set of UpdateTypes and count as an 'Update'
        /// </summary>
        UpdateType _utUpdates = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once;

        public Program()
        {
            _wicoIGC = new WicoIGC(this); 

            // cause ourselves to run again so we can do the init
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Save()
        {
        }

        /// <summary>
        /// Has everything been initialized?
        /// </summary>
        bool _areWeInited=false;

        public void Main(string argument, UpdateType updateSource)
        {
            // Echo some information aboue 'me' and why we were run
            Echo("Source=" + updateSource.ToString());
            Echo("Me=" + Me.EntityId.ToString("X"));
            Echo(Me.CubeGrid.CustomName);

            if (!_areWeInited)
            {
                InitMessageHandlers();
                _areWeInited = true;
            }

            // use if not setting callbacks for any of the desired channels
            //            if (bInit) wicoIGC.ProcessIGCMessages(); 

            // always check for IGC messages in case some aren't using callbacks
            _wicoIGC.ProcessIGCMessages();
            if ((updateSource & UpdateType.IGC) > 0)
            {
                // we got a callback for an IGC message.  
                // but we already processed them.
            }
            else if((updateSource & _utTriggers) > 0)
            {
                // if we got a 'trigger' source, send out the received argument
                IGC.SendBroadcastMessage(_broadCastTag, argument);
                Echo("Sending Message:\n" + argument);
            }
            else if((updateSource & _utUpdates) > 0)
            {
                // it was an automatic update

                // this script doens't have anything to do
            }
        }

        /// <summary>
        /// This is our unique ID for our message.  We've defined the format for the message data (it's just a string)
        /// </summary>
        string _broadCastTag = "MDK IGC Example 3";

        void InitMessageHandlers()
        {
            // creates a broadcast channel with the specified tag and calls the handler when messages are processed
            _wicoIGC.AddPublicHandler(_broadCastTag, TestBroadcastHandler);
        }

        // Handler for the test broadcast messages.
        void TestBroadcastHandler(MyIGCMessage msg)
        {
            // NOTE: called on ALL received messages; not just 'our' tag
           
            if (msg.Tag!= _broadCastTag)
                return; // not our message

            if (msg.Data is string)
            {
                Echo("Received Test Message");
                Echo(" Source=" + msg.Source.ToString("X"));
                Echo(" Data=\"" + msg.Data + "\"");
                Echo(" Tag=" + msg.Tag);
            }
        }
    }
}
