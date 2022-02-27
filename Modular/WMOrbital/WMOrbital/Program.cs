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
        WicoControl _wicoControl;

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

        Timers _timers;
        Displays _displays;

        OrbitalModes wicoOrbitalLaunch;


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
            wicoSensors = new Sensors(this, _wicoBlockMaster);
            wicoWheels = new Wheels(this, _wicoBlockMaster);
            wicoEngines = new HydrogenEngines(this, _wicoBlockMaster);
            wicoPower = new PowerProduction(this, _wicoBlockMaster);
            _displays = new Displays(this, _wicoBlockMaster, _wicoElapsedTime);
            _timers = new Timers(this, _wicoBlockMaster);

            wicoOrbitalLaunch = new OrbitalModes(this, _wicoControl, _wicoBlockMaster
                , wicoThrusters, wicoGyros, wicoConnectors, wicoLandingGears
                , wicoGasTanks, wicoGasGens
                , _timers
                , _displays
                , wicoCameras
                );

        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            Echo("Commands:");
            Echo(" hover: start hover mode");
            Echo(" orbitallaunch: Start launch mode");
            Echo(" descend [#]: descend to land or # meters");
            Echo(" orbitalland: land on planet in front of ship");
            Echo("");
            if (_wicoControl != null)
                _wicoControl.AnnounceState();
        }

        public void ModulePostMain(UpdateType updateSource)
        {
            if(bInitDone)
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

                _displays.EchoInfo();

                //                wicoConnectors.DisplayInfo();
//                wicoBlockMaster.DisplayInfo();

                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow();

                Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();
            }
        }
        public void ModulePostInit()
        {
            if (_wicoControl != null)
                _wicoControl.ModeAfterInit(SaveIni);

            //            _wicoSensors.SensorInit(_wicoBlockMaster.GetMainController());
        }

    }
}
