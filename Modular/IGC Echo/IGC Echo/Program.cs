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
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        WicoIGC iWicoIGC;
        int runcount = 0;


        public Program()
        {
            Echo("Creator");
            iWicoIGC = new WicoIGC(this, true);

            // Could add public and unicast handlers here..
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            runcount++;
            Echo(runcount.ToString()+ ":"+updateSource.ToString());
            if (
                (updateSource & (UpdateType.Trigger | UpdateType.Terminal)) > 0 // run by a terminal action
                || (updateSource & (UpdateType.Mod)) > 0 // script run by a mod
                || (updateSource & (UpdateType.Script)) > 0 // this pb run by another script (PB)
                )
            {
                if (argument != "")
                {
                    iWicoIGC.AddPublicHandler(argument, SimpleEchoHandler);
                    Echo("Added handler for:" + argument);
                }
            }
            iWicoIGC.ProcessIGCMessages();
            if( (updateSource& UpdateType.IGC) >0) // script was run because of incoming IGC message
            {
                // since messages can come without a callback, we process the IGC messages anyway.
            }
        }

        void SimpleEchoHandler(MyIGCMessage myIGCMessage)
        {
            Echo("Received IGC Public Message");
            Echo("Tag=" + myIGCMessage.Tag);
            Echo("Data=" + myIGCMessage.Data.ToString());
            Echo("Source=" + myIGCMessage.Source.ToString());
        }
    }
}
