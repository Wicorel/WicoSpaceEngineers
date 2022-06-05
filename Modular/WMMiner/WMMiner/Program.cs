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

    // TODO: add support for OreDetector Raycast mod
    // https://steamcommunity.com/profiles/76561198014682032/myworkshopfiles/?appid=244850

    partial class Program : MyGridProgram
    {
        WicoIGC _wicoIGC;
        WicoBlockMaster _wicoBlockMaster;
        WicoElapsedTime _wicoElapsedTime;

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
        ScanBase _scanBase;
//        ScansMode _scanMode;
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

        void ModuleProgramInit()
        {
            _wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor

            _wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            _wicoBlockMaster.LoadLocalGrid();

            _wicoControl = new WicoControl(this, _wicoIGC);

            _wicoElapsedTime = new WicoElapsedTime(this, _wicoControl);

            wicoThrusters = new WicoThrusters(this, _wicoBlockMaster);
            wicoGyros = new WicoGyros(this, _wicoBlockMaster);
            wicoGasTanks = new GasTanks(this, _wicoBlockMaster);
            wicoGasGens = new GasGens(this, _wicoBlockMaster);
            wicoConnectors = new Connectors(this, _wicoBlockMaster);
            wicoCameras = new Cameras(this, _wicoBlockMaster);
            wicoAntennas = new Antennas(this, _wicoBlockMaster);
            wicoSensors = new Sensors(this, _wicoBlockMaster);
            wicoPower = new PowerProduction(this, _wicoBlockMaster);
            wicoTimers = new Timers(this, _wicoBlockMaster);
            //            navRemote = new NavRemote(this);
            navCommon = new NavCommon(this,_wicoControl, _wicoIGC);
            _sensors = new Sensors(this, _wicoBlockMaster);
            _drills = new Drills(this, _wicoBlockMaster);
            _displays = new Displays(this, _wicoBlockMaster, _wicoElapsedTime);
            _dock = new DockBase(this);
            _scanBase = new ScanBase(this, _wicoControl);
            _asteroids = new Asteroids(this, _wicoControl, _wicoIGC,_displays);
//            _scanMode = new ScansMode(this, _wicoControl, _wicoBlockMaster, _wicoIGC, wicoCameras, _asteroids, _displays);
               
            _oreInfoLocs = new OreInfoLocs(this, _wicoBlockMaster, _wicoIGC, _asteroids, _displays);
            _ores = new OresLocal(this, _wicoBlockMaster, _wicoControl, _wicoIGC, _asteroids, _oreInfoLocs, _displays);

//            cargoCheck = new CargoCheck(this, wicoBlockMaster, _displays);
            _systemsMonitor = new SystemsMonitor(this, _wicoElapsedTime, wicoThrusters, wicoConnectors, wicoAntennas, wicoGasTanks, wicoGyros, wicoPower, _ores);

            _miner = new Miner(this, _wicoControl, _wicoBlockMaster, _wicoElapsedTime, _wicoIGC
                , _scanBase, _asteroids
                , _systemsMonitor
//                , wicoThrusters, wicoConnectors
                , wicoSensors
                , wicoCameras, _drills
//                , wicoAntennas
//                , wicoGasTanks, wicoGyros, wicoPower
                , wicoTimers, navCommon, _oreInfoLocs, _ores, _dock
                ,_displays
                , wicoAntennas
                );
        /// DEBUG
        // wicoIGC.SetDebug(true);
//        _wicoControl.SetDebug(true);
        // wicoElapsedTime.SetDebug(true);

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
        }

        public void ModulePostMain(UpdateType updateSource)
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
            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();

            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
            EchoInstructions();
        }
        public void ModulePostInit()
        {
            if (_wicoControl != null)
                _wicoControl.ModeAfterInit(SaveIni);

            //            _wicoSensors.SensorInit(_wicoBlockMaster.GetMainController());
        }

    }
}
