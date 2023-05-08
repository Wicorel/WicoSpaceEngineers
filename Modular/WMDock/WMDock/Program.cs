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
        WicoIGC _wicoIGC;
        WicoBlockMaster _wicoBlockMaster;
        WicoElapsedTime _wicoElapsedTime;

        WicoThrusters wicoThrusters;
        WicoGyros wicoGyros;
        GasTanks wicoGasTanks;
        GasGens wicoGasGens;
        Connectors wicoConnectors;
        LandingGears wicoLandingGears;
        Cameras wicoCameras;
        Parachutes wicoParachutes;
        NavRotors wicoNavRotors;
        Antennas wicoAntennas;
//        Sensors wicoSensors;
//        Wheels wicoWheels;
//        HydrogenEngines wicoEngines;
        PowerProduction wicoPower;
        Timers wicoTimers;
        WicoBases wicoBases;
//        NavRemote navRemote;
        NavCommon navCommon;
        CargoCheck _cargoCheck;
        Displays _displays;

        SpaceDock spaceDock;
        WhenDocked _whenDocked;
        PowerManagement _powerManagement;
        SystemsMonitor _systemsMonitor;

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
            wicoLandingGears = new LandingGears(this, _wicoBlockMaster);
            wicoCameras = new Cameras(this, _wicoBlockMaster);
            wicoParachutes = new Parachutes(this, _wicoBlockMaster);
            wicoNavRotors = new NavRotors(this, _wicoBlockMaster);
            wicoAntennas = new Antennas(this, _wicoBlockMaster);
//            wicoSensors = new Sensors(this, wicoBlockMaster);
//            wicoWheels = new Wheels(this);
//            wicoEngines = new HydrogenEngines(this);
            wicoPower = new PowerProduction(this,_wicoBlockMaster);
            wicoTimers = new Timers(this, _wicoBlockMaster);
            _displays = new Displays(this, _wicoBlockMaster, _wicoElapsedTime);
            wicoBases = new WicoBases(this, _wicoIGC,_displays);
//            navRemote = new NavRemote(this);
            navCommon = new NavCommon(this, _wicoControl, _wicoIGC);
            _cargoCheck = new CargoCheck(this, _wicoBlockMaster,_displays);

            _powerManagement = new PowerManagement(this 
                , wicoPower, wicoGasTanks, _wicoElapsedTime
                ,  _wicoIGC, _displays
                );

            _systemsMonitor = new SystemsMonitor(this, _wicoElapsedTime
                , wicoThrusters, wicoConnectors
                , wicoAntennas, wicoGasTanks, wicoGyros, wicoPower
                , _cargoCheck
                );

            spaceDock = new SpaceDock(this, _wicoControl, _wicoBlockMaster
                , _wicoElapsedTime
                , wicoAntennas
                , wicoTimers, _wicoIGC, wicoBases, navCommon
                , _displays, _systemsMonitor
                );

            _whenDocked = new WhenDocked(this, _wicoControl, _wicoBlockMaster, _wicoIGC
                , wicoAntennas
                , wicoTimers, wicoBases
                , _displays, _systemsMonitor
                );


        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            
//            Echo("Space Dock Module:");
//            Echo("UpdateType=" + updateSource.ToString() + " Init=" + bInitDone);
        }

        public void ModulePostMain(UpdateType updateSource)
        {
            if (bInitDone)
            {
                // TODO: Add ET
                wicoPower.CalcPower();
                int engines = 0;
                engines = wicoPower.EnginesCount();
                if (engines > 0)
                {
                    Echo("H Engines: " + engines.ToString());
                    //                   Echo("Maxoutput=" + wicoPower.maxHydrogenPower.ToString() + " Current=" + wicoPower.currentEngineOutput.ToString());
                    var tanksfill = wicoPower.EnginesTanksFill();
                    Echo(" Engine Tanks Filled=" + (tanksfill * 100).ToString() + "%");
                }
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow();
                
//                _displays.EchoInfo();
                Echo(wicoBases.baseInfoString().Trim());
            }
            
            _wicoControl.AnnounceState();
            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();
            Echo("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));

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
