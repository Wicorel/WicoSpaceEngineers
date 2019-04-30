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

        List<Action<string, UpdateType>> UpdateTriggerHandlers = new List<Action<string, UpdateType>>();
        List<Action<string, UpdateType>> UpdateHandlers = new List<Action<string, UpdateType>>();


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


        public Program()
        {
            wicoIGC = new WicoIGC(this); // Must be first
            wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            wicoControl = new WicoControl(this);

            wicoThrusters = new WicoThrusters(this);
            wicoGyros = new WicoGyros(this,null);

            Runtime.UpdateFrequency |= UpdateFrequency.Once; // cause ourselves to run again to continue initialization

 //           wicoControl.WicoControlInit();

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
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (!WicoInit())
            {
                Echo("Init");
            }
            if ((updateSource & UpdateType.IGC) > 0)
            {
                Echo("IGC");
                wicoIGC.ProcessIGCMessages();
                if (wicoControl.IamMain())
                    mesurface1.WriteText("Master Module");
                else
                    mesurface1.WriteText("Sub Module");
            }
            if ((updateSource & (utTriggers)) > 0)
            {
                Echo("Triggers:"+argument);
                foreach(var handler in UpdateTriggerHandlers)
                {
                    handler(argument, updateSource);
                }
//                wicoControl.ProcessTrigger(argument, updateSource);

            }
            if ((updateSource & (utUpdates)) > 0)
            {
                Echo("Update");
                foreach (var handler in UpdateHandlers)
                {
                    handler(argument, updateSource);
                }
            }


            Echo("I Am Main=" + wicoControl.IamMain().ToString());
            Echo(wicoThrusters.ThrusterCount() + " Thrusters Found");

            if (wicoGyros.gyroControl == null)
                wicoGyros.SetController();
            Echo(wicoGyros.NumberAllGyros() + " Total Gyros Found");
            Echo(wicoGyros.NumberUsedGyros() + " Used Gyros");
        }

        bool bInitDone = false;
        bool WicoInit()
        {
            if (bInitDone) return true;


            // must come late as the above inits may add handlers
            wicoBlockMaster.LocalBlocksInit();

            bInitDone = true;
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