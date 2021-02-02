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

        WicoThrusters wicoThrusters;
        WicoGyros wicoGyros;
        GasTanks wicoGasTanks;
        GasGens wicoGasGens;
        Connectors wicoConnectors;
//            LandingGears wicoLandingGears;
        Cameras wicoCameras;
        Antennas wicoAntennas;
        Sensors wicoSensors;
        PowerProduction wicoPower;
        Timers wicoTimers;
        //        NavRemote navRemote;
        NavCommon navCommon;
// have Ores        CargoCheck _cargoCheck;
        Sensors _sensors;
        Drills _drills;
//        ScanBase _scanBase;
        ScansMode _scanMode;
        Asteroids _asteroids;
        OreInfoLocs _oreInfoLocs;
        OresLocal _ores;
        DockBase _dock;
        Displays _displays;
        SystemsMonitor _systemsMonitor;
//        CargoCheck cargoCheck;

        Miner _miner;
//            SpaceDock spaceDock;
        // OrbitalModes wicoOrbitalLaunch;
        //        Navigation wicoNavigation;


        //        WicoUpdateModesShared _wicoControl;
        WicoControl _wicoControl;

        void ModuleControlInit()
        {
            //            _wicoControl = new WicoUpdateModesShared(this);
            _wicoControl = new WicoControl(this, wicoIGC);
        }

        void ModuleProgramInit()
        {
            wicoThrusters = new WicoThrusters(this, wicoBlockMaster);
            wicoGyros = new WicoGyros(this, wicoBlockMaster);
            wicoGasTanks = new GasTanks(this, wicoBlockMaster);
            wicoGasGens = new GasGens(this);
            wicoConnectors = new Connectors(this);
            wicoCameras = new Cameras(this);
            wicoAntennas = new Antennas(this, wicoBlockMaster);
            wicoSensors = new Sensors(this, wicoBlockMaster);
            wicoPower = new PowerProduction(this, wicoBlockMaster);
            wicoTimers = new Timers(this, wicoBlockMaster);
            //            navRemote = new NavRemote(this);
            navCommon = new NavCommon(this,_wicoControl, wicoIGC);
            _sensors = new Sensors(this, wicoBlockMaster);
            _drills = new Drills(this, wicoBlockMaster);
            _displays = new Displays(this, wicoBlockMaster, wicoElapsedTime);
            _dock = new DockBase(this);
//            _scanBase = new ScanBase(this, _wicoControl);
            _asteroids = new Asteroids(this, _wicoControl, wicoIGC,_displays);
            _scanMode = new ScansMode(this, _wicoControl, wicoBlockMaster, wicoIGC, wicoCameras, _asteroids);
            _oreInfoLocs = new OreInfoLocs(this, wicoBlockMaster, wicoIGC, _asteroids, _displays);
            _ores = new OresLocal(this, wicoBlockMaster, _wicoControl, wicoIGC, _asteroids, _oreInfoLocs, _displays);

//            cargoCheck = new CargoCheck(this, wicoBlockMaster, _displays);
            _systemsMonitor = new SystemsMonitor(this, wicoElapsedTime, wicoThrusters, wicoConnectors, wicoAntennas, wicoGasTanks, wicoGyros, wicoPower, _ores);

            _miner = new Miner(this, _wicoControl, wicoBlockMaster, wicoElapsedTime, wicoIGC
                , _scanMode, _asteroids
                , _systemsMonitor
//                , wicoThrusters, wicoConnectors
                , wicoSensors
                , wicoCameras, _drills
//                , wicoAntennas
//                , wicoGasTanks, wicoGyros, wicoPower
                , wicoTimers, navCommon, _oreInfoLocs, _ores, _dock
                ,_displays
                );
        /// DEBUG
        // wicoIGC.SetDebug(true);
//        _wicoControl.SetDebug(true);
        // wicoElapsedTime.SetDebug(true);

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
        }

        public void ModulePostMain()
        {
            if (bInitDone)
            {
                int engines = 0;
                /* Testing hydrogen engine processing
                double currentoutput = 0;
                double maxoutput = 0;
                engines = wicoEngines.CurrentOutput(ref currentoutput, ref maxoutput);
                Echo("Engines: " + engines.ToString());
                if(engines>0)
                {
                    Echo("XMaxoutput=" + maxoutput.ToString() + " Current=" + currentoutput.ToString());
                    Echo("Tank Filled=" + (wicoEngines.tanksFill()*100).ToString() + "%");
                }
                */

                wicoPower.CalcPower();
                engines = wicoPower.EnginesCount();
                Echo("H Engines: " + engines.ToString());
                if (engines > 0)
                {
                    //                   Echo("Maxoutput=" + wicoPower.maxHydrogenPower.ToString() + " Current=" + wicoPower.currentEngineOutput.ToString());
                    var tanksfill = wicoPower.EnginesTanksFill();
                    Echo(" Tanks Filled=" + (tanksfill * 100).ToString() + "%");
                }
                // ensure we run at least at slow speed for updates.
                _displays.EchoInfo();
            }
            _wicoControl.WantSlow();

            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
            EchoInstructions();
        }
    }
}
