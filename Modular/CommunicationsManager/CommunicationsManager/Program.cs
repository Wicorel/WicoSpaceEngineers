using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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


        WicoControl _wicoControl;
        Communications _communications;
        ModeAttention _modeAttention;

        IFF _iff;

        Displays _displays;

        void ModuleProgramInit()
        {
            moduleList += "\nCommunications Manager";

            _wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor

            _wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            _wicoBlockMaster.LoadLocalGrid();

            _wicoControl = new WicoControl(this, _wicoIGC);
            _wicoElapsedTime = new WicoElapsedTime(this, _wicoControl);

            _iff = new IFF(this, _wicoIGC, _wicoElapsedTime);
            _displays = new Displays(this, _wicoBlockMaster, _wicoElapsedTime);
            _communications = new Communications(this, _wicoBlockMaster, _wicoElapsedTime, _wicoIGC, _displays);
            _modeAttention = new ModeAttention(this, _wicoControl, _communications);
        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            if (_wicoControl != null)
                _wicoControl.AnnounceState();
        }

        public void ModulePostMain(UpdateType updateSource)
        {
            if (bInitDone)
            {
                _displays.EchoInfo();
                _wicoControl.WantSlow();
            }

            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();
            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
            EchoInstructions();
        }

        public void ModulePostInit()
        {
            if (_wicoControl != null)
                _wicoControl.ModeAfterInit(SaveIni);

        }

    }
}
