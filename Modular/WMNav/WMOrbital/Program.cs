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

        Displays _wicoDisplays;

        TravelMovement wicoTravelMovement;

//        OrbitalModes wicoOrbitalLaunch;
         Navigation wicoNavigation;

        WicoControl _wicoControl;

        void ModuleControlInit()
        {
            _wicoControl = new WicoControl(this);
//            _wicoControl.SetDebug(true);
        }

        void ModuleProgramInit()
        {
            wicoTravelMovement = new TravelMovement(this, _wicoControl);

            wicoThrusters = new WicoThrusters(this, wicoBlockMaster);
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

            _wicoDisplays = new Displays(this, wicoBlockMaster, wicoElapsedTime);

            wicoNavigation = new Navigation(this,_wicoControl,wicoBlockMaster, wicoIGC, wicoTravelMovement, wicoElapsedTime,
                wicoGyros,wicoWheels,wicoNavRotors, wicoThrusters,_wicoDisplays);

            _wicoControl.WantSlow(); // get updates so we can check for things like navigation commands in oldschool format

            /// DEBUG
            //            wicoIGC.SetDebug(true);
//            _wicoControl.SetDebug(true);
//             wicoElapsedTime.SetDebug(true);

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
//            Echo("UpdateSource=" + updateSource.ToString());
//            Echo(" Main=" + wicoControl.IamMain().ToString());
        }

        public void ModulePostMain()
        {
            _wicoControl.WantSlow(); // get updates so we can check for things like navigation commands in oldschool format
                                     //            Echo("#navRotors=" + wicoNavRotors.NavRotorCount());
            if (bInitDone)
            {
                _wicoDisplays.EchoInfo();
            }
            _wicoControl.AnnounceState();
            Echo("LastRun=" + LastRunMs.ToString("0.00")+"ms Max=" + MaxRunMs.ToString("0.00") + "ms");
        }

        public void ModulePostInit()
        {
            wicoSensors.SensorInit(wicoBlockMaster.GetMainController());
        }

    }
}
