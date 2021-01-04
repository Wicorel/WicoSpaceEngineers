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
        
        manage local fuel resources like power and hydrogen

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

        Timers _timers;
        PowerProduction _power;
        GasTanks _tanks;
        PowerManagement _powerManagement;

        void ModuleControlInit()
        {
            // create the appropriate control system for this module
            _wicoControl = new WicoControl(this, wicoIGC);
        }

        void ModuleProgramInit()
        {
            _timers = new Timers(this, wicoBlockMaster);

            baseConnectors = new BaseConnectors(this, wicoBlockMaster, wicoIGC, wicoElapsedTime, _timers);
            _displays = new Displays(this, wicoBlockMaster, wicoElapsedTime);
            _asteroids = new Asteroids(this, _wicoControl, wicoIGC, _displays);
            _oreInfoLocs = new OreInfoLocs(this, wicoBlockMaster, wicoIGC, _asteroids, _displays);

            _power = new PowerProduction(this, wicoBlockMaster);
            _tanks = new GasTanks(this, wicoBlockMaster);

            _powerManagement = new PowerManagement(this, _wicoControl, _power, _tanks, wicoElapsedTime, wicoIGC, _displays);
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
