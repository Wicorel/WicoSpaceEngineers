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

        WicoIGC wicoIGC;
        WicoControl wicoControl;
        WicoBlockMaster wicoBlockMaster;

        WicoThrusters wicoThrusters;
        WicoGyros wicoGyros;
        GasTanks wicoGasTanks;
        GasGens wicoGasGens;
        Connectors wicoConnectors;
        LandingGears wicoLandingGears;
        Cameras wicoCameras;
        Parachutes wicoParachutes;

        OrbitalModes wicoOrbitalLaunch;


        // Handlers
        private List<Action<MyCommandLine, UpdateType>> UpdateTriggerHandlers = new List<Action<MyCommandLine, UpdateType>>();
        private List<Action<UpdateType>> UpdateUpdateHandlers = new List<Action<UpdateType>>();

        // https://github.com/malware-dev/MDK-SE/wiki/Handling-Script-Arguments
        private MyCommandLine myCommandLine = new MyCommandLine();

        private List<Action<MyIni>> SaveHandlers = new List<Action<MyIni>>();

        // https://github.com/malware-dev/MDK-SE/wiki/Handling-configuration-and-storage
        private MyIni _SaveIni = new MyIni();
        private MyIni _CustomDataIni = new MyIni();

        /// <summary>
        /// The combined set of UpdateTypes that count as a 'trigger'
        /// </summary>
        UpdateType utTriggers = UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod | UpdateType.Script;
        /// <summary>
        /// the combined set of UpdateTypes and count as an 'Update'
        /// </summary>
        UpdateType utUpdates = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once;


        // Surface stuff
        IMyTextSurface mesurface0;
        IMyTextSurface mesurface1;


        double tmGridCheckElapsedMs = -1;
        double tmGridCheckWaitMs = 3.0*1000;


        public Program()
        {
            MyIniParseResult result;
            if (!_CustomDataIni.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());
            if (!_SaveIni.TryParse(Storage, out result))
                throw new Exception(result.ToString());

            wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor
            wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            wicoControl = new WicoControl(this);

            wicoThrusters = new WicoThrusters(this);
            wicoGyros = new WicoGyros(this,null);
            wicoGasTanks = new GasTanks(this);
            wicoGasGens = new GasGens(this);
            wicoConnectors = new Connectors(this);
            wicoLandingGears = new LandingGears(this);
            wicoCameras = new Cameras(this);
            wicoParachutes = new Parachutes(this);

            wicoOrbitalLaunch = new OrbitalModes(this);

            Runtime.UpdateFrequency |= UpdateFrequency.Once; // cause ourselves to run again to continue initialization

            // Local PB Surface Init
            mesurface0 = Me.GetSurface(0);
            mesurface1 = Me.GetSurface(1);
            mesurface0.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            mesurface0.WriteText("Wicorel Modular");
            mesurface0.FontSize = 2;
            mesurface0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;

            mesurface1.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            mesurface1.WriteText("Version: 1");
            mesurface1.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            mesurface1.TextPadding = 0.25f;
            mesurface1.FontSize = 3.5f;

            if (!Me.Enabled)
            {
                Echo("I am turned OFF!");
            }

        }

        public void Save()
        {
            foreach(var handler in SaveHandlers)
            {
                handler(_SaveIni);
            }
            Storage = _SaveIni.ToString();

        }

        void AddSaveHandler(Action<MyIni> handler)
        {
            if (!SaveHandlers.Contains(handler))
                SaveHandlers.Add(handler);
        }

        void AddUpdateHandler(Action<UpdateType> handler)
        {
            if (!UpdateUpdateHandlers.Contains(handler))
                UpdateUpdateHandlers.Add(handler);
        }

        void AddTriggerHandler(Action<MyCommandLine, UpdateType> handler)
        {
            if (!UpdateTriggerHandlers.Contains(handler))
                UpdateTriggerHandlers.Add(handler);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            wicoControl.ResetUpdates();
            if (tmGridCheckElapsedMs >= 0) tmGridCheckElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (!WicoLocalInit())
            {
                Echo("Init");
            }
            else
            {
                // only do this on update, not triggers
                if ((updateSource & utUpdates) > 0)
                {
//                    Echo("Init and update. Elapsed:" +tmGridCheckElapsedMs.ToString("0.00"));
//                    Echo("Local:" + wicoBlockMaster.localBlocksCount.ToString() + " blocks");
                    if (tmGridCheckElapsedMs > tmGridCheckWaitMs || tmGridCheckElapsedMs < 0) // it is time to scan..
                    {
  //                      Echo("time to check");
                        tmGridCheckElapsedMs = 0;
                        if (wicoBlockMaster.CalcLocalGridChange())
                        {
//                            mesurface0.WriteText("GRID check!");
//                            Echo("GRID CHANGED!");
//                            mesurface0.WriteText("GRID CHANGED!", true);
                            bInitDone = false;
                            tmGridCheckElapsedMs = -1;
                            Runtime.UpdateFrequency |= UpdateFrequency.Once; // cause ourselves to run again to continue initialization
                            return;
                        }
//                        else Echo("No Grid Change");
                    }
                    //else Echo("Not Timeto check");
                }
//                else Echo("Init and NOTE update");
            }
            Echo(updateSource.ToString());
            if ((updateSource & UpdateType.IGC) > 0)
            {
//                Echo("IGC");
                wicoIGC.ProcessIGCMessages();
                if (wicoControl.IamMain())
                    mesurface1.WriteText("Master Module");
                else
                    mesurface1.WriteText("Sub Module");
            }
            if ((updateSource & (utTriggers)) > 0)
            {
//                Echo("Triggers:"+argument);
                MyCommandLine useCommandLine = null;
                if (myCommandLine.TryParse(argument))
                    useCommandLine = myCommandLine;
                foreach(var handler in UpdateTriggerHandlers)
                {
                    handler(useCommandLine, updateSource);
                }

            }
            if ((updateSource & (utUpdates)) > 0)
            {
//                Echo("Update");
                foreach (var handler in UpdateUpdateHandlers)
                {
                    handler(updateSource);
                }
            }

            /*
            Echo("I Am Main=" + wicoControl.IamMain().ToString());
            Echo(wicoThrusters.ThrusterCount() + " Thrusters Found");

            if (wicoGyros.gyroControl == null)
                wicoGyros.SetController();
            Echo(wicoGyros.NumberAllGyros() + " Total Gyros Found");
            Echo(wicoGyros.NumberUsedGyros() + " Used Gyros");

            var shipController= wicoBlockMaster.GetMainController();
            if (shipController != null)
                Echo("Controller = " + shipController.CustomName);
            */
            Echo("Mode=" + wicoControl.IMode.ToString());
            Echo("State=" + wicoControl.IState.ToString());

            Runtime.UpdateFrequency = wicoControl.GenerateUpdate() 
                | UpdateFrequency.Update100
                ;

        }

        bool bInitDone = false;
        bool WicoLocalInit()
        {
            if (bInitDone) return true;

            // must come late as the above inits may add handlers
            wicoBlockMaster.LocalBlocksInit();

            bInitDone = true;

            // last thing after init is done
            wicoControl.ModeAfterInit(_SaveIni);

            // Save it now so that any defaults are set after an initial run
            Me.CustomData = _CustomDataIni.ToString();

            return bInitDone;
        }

        void WicoInitReset()
        {
            bInitDone = false;
        }

    }
}

/*
  public event Action<WhateverArgumentYouNeed> SomeEvent;

public void SomethingHappens()
{
    SomeEvent?.Invoke(thatArgument);
}


class Subscribee
{
    public Subscribee(Program program)
    {
        program.SomeEvent += OnSomeEvent;
    }
    void OnSomeEvent(WhateverArgumentYouNeed argument)
    {
        DoThings();
    }
}
*/