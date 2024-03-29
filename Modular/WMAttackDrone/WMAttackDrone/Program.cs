﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
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
        Cameras wicoCameras;
        /*
        GasTanks wicoGasTanks;
        GasGens wicoGasGens;
        Connectors wicoConnectors;
        LandingGears wicoLandingGears;
        Parachutes wicoParachutes;
        NavRotors wicoNavRotors;
        Antennas wicoAntennas;
        Sensors wicoSensors;
        Wheels wicoWheels;

        Displays _wicoDisplays;

        TravelMovement wicoTravelMovement;
        */

        //        OrbitalModes wicoOrbitalLaunch;
        //Navigation wicoNavigation;

        WicoControl _wicoControl;

        Weapons _weapons;

        AttackDrone _attackDrone;

        void ModuleProgramInit()
        {
            _wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor

            _wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            _wicoBlockMaster.LoadLocalGrid();

            _wicoControl = new WicoControl(this, _wicoIGC);

            _wicoElapsedTime = new WicoElapsedTime(this, _wicoControl);


            wicoThrusters = new WicoThrusters(this, _wicoBlockMaster);
            wicoGyros = new WicoGyros(this, _wicoBlockMaster);
            wicoCameras = new Cameras(this, _wicoBlockMaster);
            _weapons = new Weapons(this, _wicoBlockMaster);

            /*
                        wicoGasTanks = new GasTanks(this, _wicoBlockMaster);
                        wicoGasGens = new GasGens(this, _wicoBlockMaster);
                        wicoConnectors = new Connectors(this, _wicoBlockMaster);
                        wicoLandingGears = new LandingGears(this, _wicoBlockMaster);
                        wicoParachutes = new Parachutes(this, _wicoBlockMaster);
                        wicoNavRotors = new NavRotors(this, _wicoBlockMaster);
                        wicoAntennas = new Antennas(this, _wicoBlockMaster);
                        wicoSensors = new Sensors(this, _wicoBlockMaster);
                        wicoWheels = new Wheels(this, _wicoBlockMaster);

                        _wicoDisplays = new Displays(this, _wicoBlockMaster, _wicoElapsedTime);

                        wicoTravelMovement = new TravelMovement(this, _wicoControl, _wicoBlockMaster, wicoGyros, wicoThrusters, wicoSensors, wicoCameras, wicoWheels, wicoNavRotors);
            */

            /*
            wicoNavigation = new Navigation(this, _wicoControl, _wicoBlockMaster, _wicoIGC, wicoTravelMovement, _wicoElapsedTime,
                wicoGyros, wicoWheels, wicoNavRotors, wicoThrusters, wicoAntennas, _wicoDisplays);
            */
            _attackDrone = new AttackDrone(this, _wicoControl, _wicoBlockMaster, _wicoIGC, _wicoElapsedTime, wicoGyros, wicoThrusters, wicoCameras, _weapons);


            _wicoControl.WantSlow(); // get updates so we can check for things like navigation commands in oldschool format

            /// DEBUG
            //            wicoIGC.SetDebug(true);
            //            _wicoControl.SetDebug(true);
            //             wicoElapsedTime.SetDebug(true);

        }
        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            if (_wicoControl != null && _wicoControl._bUpdateDebug) Echo("Update=" + updateSource.ToString());
            //            Echo("UpdateSource=" + updateSource.ToString());
            //            Echo(" Main=" + wicoControl.IamMain().ToString());

            if (bInitDone)
            {
                //                Echo("Inited");
                if (_wicoControl != null)
                    _wicoControl.AnnounceState();
            }
        }

        public void ModulePostMain(UpdateType updateSource)
        {

            _wicoControl.WantSlow(); // get updates so we can check for things like navigation commands in oldschool format
            if (bInitDone)
            {
//                _wicoDisplays.EchoInfo();
            }
            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();

            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
        }

        public void ModulePostInit()
        {
            if (_wicoControl != null)
                _wicoControl.ModeAfterInit(SaveIni);

//            wicoSensors.SensorInit(_wicoBlockMaster.GetMainController());
        }


    }
}
