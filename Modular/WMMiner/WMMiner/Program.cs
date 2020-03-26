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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

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
            CargoCheck _cargoCheck;
            Sensors _sensors;
            Drills _drills;

//            SpaceDock spaceDock;
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
                wicoCameras = new Cameras(this);
                wicoAntennas = new Antennas(this);
                wicoSensors = new Sensors(this, wicoBlockMaster);
                wicoPower = new PowerProduction(this, wicoBlockMaster);
                wicoTimers = new Timers(this, wicoBlockMaster);
                //            navRemote = new NavRemote(this);
                navCommon = new NavCommon(this);
                _cargoCheck = new CargoCheck(this, wicoBlockMaster);
                _sensors = new Sensors(this, wicoBlockMaster);
            _drills = new Drills(this, wicoBlockMaster);

//                spaceDock = new SpaceDock(this, _wicoControl, wicoBlockMaster, wicoThrusters, wicoConnectors,
//                    wicoAntennas, wicoGasTanks, wicoGyros, wicoPower, wicoTimers, wicoIGC, wicoBases, navCommon, _cargoCheck);
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
                    _wicoControl.WantSlow();
                }
            }
        }
    }
