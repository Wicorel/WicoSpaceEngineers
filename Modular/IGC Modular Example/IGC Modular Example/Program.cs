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
        WicoIGC wicoIGC;

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
            wicoIGC = new WicoIGC(this); 

            // cause ourselves to run again so we can do the init
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        /// <summary>
        /// Has everything been initialized?
        /// </summary>
        bool bInit=false;

        public void Main(string argument, UpdateType updateSource)
        {
            // Echo some information aboue 'me' and why we were run
            Echo("Source=" + updateSource.ToString());
            Echo("Me=" + Me.EntityId.ToString("X"));
            Echo(Me.CubeGrid.CustomName);

            if (!bInit)
            {
                InitMessageHandlers();
                bInit = true;
            }

            // use if not setting callbacks for any of the desired channels
            //            if (bInit) wicoIGC.ProcessIGCMessages(); 

            if ((updateSource & UpdateType.IGC) > 0)
            {
                // we got a callback for an IGC message.  
                // There might be multiple incoming IGC messages
                wicoIGC.ProcessIGCMessages();
            }
            else if((updateSource & utTriggers) > 0)
            {
                // if we got a 'trigger' source, send out the received argument
                IGC.SendBroadcastMessage(sBroadCastTag, argument);
            }
            else if((updateSource & utUpdates) > 0)
            {
                // it was an automatic update
            }
        }

        void InitMessageHandlers()
        {
            wicoIGC.AddPublicHandler(sBroadCastTag, TestBroadcastHandler);
        }


        // Handler for the test brodcast messages.

        string sBroadCastTag = "TESTBROADCAST999";

        void TestBroadcastHandler(MyIGCMessage msg)
        {

            // NOTE: called on ALL received messages; not just 'our' tag

            // if (msg.Tag!=sBroadCastTag) return; // not our message
            Echo("Received Test Message");
            var src = msg.Source;
            Echo(" Source=" + src.ToString("X"));
            Echo(" Data=\"" + msg.Data + "\"");
            Echo(" Tag=" + msg.Tag);
        }
    }
}
