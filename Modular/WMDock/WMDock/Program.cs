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

        SpaceDock spaceDock;
        // OrbitalModes wicoOrbitalLaunch;
        //        Navigation wicoNavigation;


        //        WicoUpdateModesShared _wicoControl;
        WicoControl _wicoControl;

        void ModuleControlInit()
        {
//            _wicoControl = new WicoUpdateModesShared(this);
            _wicoControl = new WicoControl(this);
        }

        void ModuleProgramInit()
        {
            //            wicoTravelMovement = new TravelMovement(this);
            //OurName = "";
            //moduleName += "\nOrbital V4";
            //sVersion = "4";

            wicoThrusters = new WicoThrusters(this);
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
            wicoBases = new WicoBases(this, wicoIGC);
//            navRemote = new NavRemote(this);
            navCommon = new NavCommon(this);
            _cargoCheck = new CargoCheck(this, wicoBlockMaster);

            spaceDock = new SpaceDock(this, _wicoControl, wicoBlockMaster, wicoThrusters, wicoConnectors, 
                wicoAntennas,  wicoGasTanks, wicoGyros, wicoPower, wicoTimers, wicoIGC, wicoBases, navCommon, _cargoCheck);
            //wicoOrbitalLaunch = new OrbitalModes(this);
            //            wicoNavigation = new Navigation(this, wicoBlockMaster.GetMainController());

            /// DEBUG
            wicoIGC.SetDebug(true);
            _wicoControl.SetDebug(true);
        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            Echo("Space Dock Module:");
//            Echo(" Main=" + wicoControl.IamMain().ToString());
//            Echo("");
        }

        public void ModulePostMain()
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
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow();
                Echo(wicoBases.baseInfoString());
            }
        }

    }
}
