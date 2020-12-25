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
        Sensors wicoSensors;
        Wheels wicoWheels;
        HydrogenEngines wicoEngines;
        PowerProduction wicoPower;
        Timers wicoTimers;
        WicoBases wicoBases;
//        NavRemote navRemote;
        NavCommon navCommon;
        CargoCheck _cargoCheck;
        Displays _displays;

        SpaceDock spaceDock;
        WhenDocked _whenDocked;
        // OrbitalModes wicoOrbitalLaunch;
        //        Navigation wicoNavigation;
        PowerManagement _powerManagement;
        SystemsMonitor _systemsMonitor;

        //        WicoUpdateModesShared _wicoControl;
        WicoControl _wicoControl;

        void ModuleControlInit()
        {
//            _wicoControl = new WicoUpdateModesShared(this);
            _wicoControl = new WicoControl(this, wicoIGC);
        }

        void ModuleProgramInit()
        {
            //            wicoTravelMovement = new TravelMovement(this);
            //OurName = "";
            //moduleName += "\nOrbital V4";
            //sVersion = "4";

            wicoThrusters = new WicoThrusters(this, wicoBlockMaster);
            wicoGyros = new WicoGyros(this, wicoBlockMaster);
            wicoGasTanks = new GasTanks(this, wicoBlockMaster);
            wicoGasGens = new GasGens(this);
            wicoConnectors = new Connectors(this);
            wicoLandingGears = new LandingGears(this);
            wicoCameras = new Cameras(this);
            wicoParachutes = new Parachutes(this);
            wicoNavRotors = new NavRotors(this);
            wicoAntennas = new Antennas(this);
            wicoSensors = new Sensors(this, wicoBlockMaster);
            wicoWheels = new Wheels(this);
            wicoEngines = new HydrogenEngines(this);
            wicoPower = new PowerProduction(this,wicoBlockMaster);
            wicoTimers = new Timers(this, wicoBlockMaster);
            _displays = new Displays(this, wicoBlockMaster, wicoElapsedTime);
            wicoBases = new WicoBases(this, wicoIGC,_displays);
//            navRemote = new NavRemote(this);
            navCommon = new NavCommon(this, _wicoControl);
            _cargoCheck = new CargoCheck(this, wicoBlockMaster,_displays);

            _powerManagement = new PowerManagement(this, wicoPower, wicoGasTanks, wicoElapsedTime, wicoIGC);

            _systemsMonitor = new SystemsMonitor(this,  wicoThrusters, wicoConnectors,
                wicoAntennas, wicoGasTanks, wicoGyros, wicoPower, _cargoCheck);

            spaceDock = new SpaceDock(this, _wicoControl, wicoBlockMaster
//                , wicoThrusters, wicoConnectors,
                ,wicoAntennas
//                , wicoGasTanks, wicoGyros, wicoPower
                , wicoTimers, wicoIGC, wicoBases, navCommon
//                , _cargoCheck
                , _displays, _systemsMonitor);

            _whenDocked = new WhenDocked(this, _wicoControl, wicoBlockMaster, wicoIGC
//                , wicoThrusters, wicoConnectors
                , wicoAntennas
//                , wicoGasTanks, wicoGyros, wicoPower
                , wicoTimers, wicoBases
//                , _cargoCheck
                , _displays, _systemsMonitor);


        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            
//            Echo("Space Dock Module:");
//            Echo("UpdateType=" + updateSource.ToString() + " Init=" + bInitDone);
        }

        public void ModulePostMain()
        {
            if(bInitDone)
            {
                wicoPower.CalcPower();
                int engines = 0;
                engines = wicoPower.EnginesCount();
                Echo("H Engines: " + engines.ToString());
                if (engines > 0)
                {
 //                   Echo("Maxoutput=" + wicoPower.maxHydrogenPower.ToString() + " Current=" + wicoPower.currentEngineOutput.ToString());
                    var tanksfill = wicoPower.EnginesTanksFill();
                    Echo(" Tanks Filled=" + (tanksfill * 100).ToString() + "%");
                }
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow();
                
                _displays.EchoInfo();
                Echo(wicoBases.baseInfoString().Trim());
            }
            
            _wicoControl.AnnounceState();
            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
            EchoInstructions();
        }

    }
}
