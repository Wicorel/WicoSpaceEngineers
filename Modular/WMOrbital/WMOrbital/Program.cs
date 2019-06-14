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

        OrbitalModes wicoOrbitalLaunch;
        //        Navigation wicoNavigation;


        void ModuleProgramInit()
        {
            //            wicoTravelMovement = new TravelMovement(this);
            //OurName = "";
            //moduleName += "\nOrbital V4";
            //sVersion = "4";

            wicoThrusters = new WicoThrusters(this);
            wicoGyros = new WicoGyros(this, null);
            wicoGasTanks = new GasTanks(this);
            wicoGasGens = new GasGens(this);
            wicoConnectors = new Connectors(this);
            wicoLandingGears = new LandingGears(this);
            wicoCameras = new Cameras(this);
            wicoParachutes = new Parachutes(this);
            wicoNavRotors = new NavRotors(this);
            wicoAntennas = new Antennas(this);
            wicoSensors = new Sensors(this, wicoBlockMaster.GetMainController());
            wicoWheels = new Wheels(this);

            wicoOrbitalLaunch = new OrbitalModes(this);
            //            wicoNavigation = new Navigation(this, wicoBlockMaster.GetMainController());

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
        }

        public void ModulePostMain()
        {
        }

    }
}
