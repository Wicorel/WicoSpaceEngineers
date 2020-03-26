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
        /// The control system for this module.
        /// </summary>
        WicoUpdateModesShared _wicoControl;

        void ModuleControlInit()
        {
            // create the appropriate control system for this module
            _wicoControl = new WicoUpdateModesShared(this);
        }

        void ModuleProgramInit()
        {
            wicoIGC.SetDebug(true);
            _wicoControl.SetDebug(true);
        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            Echo("Test Rig for Shared modes");
            Echo("UpdateSource=" + updateSource.ToString());
        }

        public void ModulePostMain()
        {
            if (bInitDone)
            {
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow();
            }
        }
    }
}
