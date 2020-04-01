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

#region mdk preserve
#region mdk macros
// script was deployed at $MDK_DATETIME$
#endregion
#endregion

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
        //       NavRemote navRemote;
//        NavCommon navCommon;
        CargoCheck _cargoCheck;

        TravelMovement wicoTravelMovement;

        SpaceDock spaceDock;
//        OrbitalModes wicoOrbitalLaunch;
         Navigation wicoNavigation;
        

        //        WicoUpdateModesShared _wicoControl;
        WicoControl _wicoControl;

        void ModuleControlInit()
        {
            //            _wicoControl = new WicoUpdateModesShared(this);
            _wicoControl = new WicoControl(this);
        }

        void ModuleProgramInit()
        {
            wicoTravelMovement = new TravelMovement(this, _wicoControl);

            wicoThrusters = new WicoThrusters(this);
            wicoGyros = new WicoGyros(this, wicoBlockMaster);
            wicoGasTanks = new GasTanks(this,wicoBlockMaster);
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
            wicoPower = new PowerProduction(this, wicoBlockMaster);
            wicoTimers = new Timers(this, wicoBlockMaster);
            wicoBases = new WicoBases(this, wicoIGC);
            //            navRemote = new NavRemote(this);
//            navCommon = new NavCommon(this);
            _cargoCheck = new CargoCheck(this, wicoBlockMaster);

            wicoNavigation = new Navigation(this, _wicoControl, wicoBlockMaster, wicoIGC, wicoTravelMovement, wicoGyros, wicoWheels, wicoNavRotors);

            spaceDock = new SpaceDock(this, _wicoControl, wicoBlockMaster, wicoThrusters, wicoConnectors,
                wicoAntennas, wicoGasTanks, wicoGyros, wicoPower, wicoTimers, wicoIGC, wicoBases, wicoNavigation, _cargoCheck);

            _wicoControl.WantSlow(); // get updates so we can check for things like navigation commands in oldschool format

            /// DEBUG
//            wicoIGC.SetDebug(true);
//            _wicoControl.SetDebug(true);

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            Echo("UpdateSource=" + updateSource.ToString());
//            Echo(" Main=" + _wicoControl.IamMain().ToString());
        }

        public void ModulePostMain()
        {
            _wicoControl.WantSlow();
            Echo(wicoBases.baseInfoString());
        }

        public void ModulePostInit()
        {
            wicoSensors.SensorInit(wicoBlockMaster.GetMainController());
        }

    }
}
