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
        /* 
         * TODO:
        Add local resources checking like Hydrogen, Ore processing/storage/etc
        Add base requests based on item needed to get or wanted to get rid of
        Add asteroids, ores and known ships
        Add menu system to send ships to ores/asteroid/ships
        */
        BaseConnectors baseConnectors;
        Asteroids _asteroids;
        OreInfoLocs _oreInfoLocs;
//        OresRemote _ores;
        Displays _displays;

        /// <summary>
        /// The control system for this module.
        /// </summary>
        WicoControl _wicoControl;

        void ModuleControlInit()
        {
            // create the appropriate control system for this module
            _wicoControl = new WicoControl(this, wicoIGC);
        }

        void ModuleProgramInit()
        {
            // wicoThrusters = new WicoThrusters(this);
            baseConnectors = new BaseConnectors(this, wicoBlockMaster, wicoIGC, wicoElapsedTime);
            _displays = new Displays(this, wicoBlockMaster, wicoElapsedTime);
            _asteroids = new Asteroids(this, _wicoControl, wicoIGC, _displays);
            _oreInfoLocs = new OreInfoLocs(this, wicoBlockMaster, wicoIGC, _asteroids, _displays);

            // wicoOrbitalLaunch = new OrbitalModes(this);
            // wicoNavigation = new Navigation(this, wicoBlockMaster.GetMainController());

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
        }

        public void ModulePostMain()
        {
            if(bInitDone)
            {
                _displays.EchoInfo();
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow(); 
            }
        }

    }
}
