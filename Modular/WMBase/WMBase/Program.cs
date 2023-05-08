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

        Manage local laser antennas
            handle connection requests over IGC.  
            Say no when connected to 'other' within 5000 (settable) meters.  Give location of 'other' in reply
            Turn off when not in use
            Control power/range
            handle 'low power' requests.
            handle setting regular antenna range to minimum needed
            heartbeat to verify connection
            disconnect when get within 5000 (settable) meters and switch to antenna communication
            handle lost connections (obscured, range issues, etc)

            power off laser antennas when this ship is docked
        
        */

        WicoIGC _wicoIGC;
        WicoBlockMaster _wicoBlockMaster;
        WicoElapsedTime _wicoElapsedTime;

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


        // functionality modules
        PowerManagement _powerManagement;
        BaseConnectors _baseConnectors;

        void ModuleControlInit()
        {
            // create the appropriate control system for this module
        }

        void ModuleProgramInit()
        {

            _wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor

            _wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            _wicoBlockMaster.LoadLocalGrid();

            _wicoControl = new WicoControl(this, _wicoIGC);

            _wicoElapsedTime = new WicoElapsedTime(this, _wicoControl);

            _timers = new Timers(this, _wicoBlockMaster);

            _baseConnectors = new BaseConnectors(this, _wicoBlockMaster, _wicoIGC, _wicoElapsedTime, _timers);

            _displays = new Displays(this, _wicoBlockMaster, _wicoElapsedTime);
            _asteroids = new Asteroids(this, _wicoControl, _wicoIGC, _displays);
            _oreInfoLocs = new OreInfoLocs(this, _wicoBlockMaster, _wicoIGC, _asteroids, _displays);

            _power = new PowerProduction(this, _wicoBlockMaster);
            _tanks = new GasTanks(this, _wicoBlockMaster);

            _powerManagement = new PowerManagement(this, _power, _tanks, _wicoElapsedTime, _wicoIGC, _displays);
        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
        }

        public void ModulePostMain(UpdateType updateSource)
        {
            if (bInitDone)
            {
                _displays.EchoInfo();
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow(); 
            }
            if (_wicoControl != null)
                _wicoControl.AnnounceState();
            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();
        }

        public void ModulePostInit()
        {

            //            _wicoSensors.SensorInit(_wicoBlockMaster.GetMainController());
        }

    }
}
