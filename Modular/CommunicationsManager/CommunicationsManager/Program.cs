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
        WicoControl _wicoControl;
        Communications _communications;
        ModeAttention _modeAttention;
        //        Antennas wicoAntennas;
        IFF _iff;

        Displays _displays;

        void ModuleControlInit()
        {
            //            _wicoControl = new WicoUpdateModesShared(this);
            _wicoControl = new WicoControl(this, wicoIGC);
        }

        void ModuleProgramInit()
        {
            moduleList += "\nCommunications Manager";

            _iff = new IFF(this, wicoIGC, wicoElapsedTime);
//            wicoAntennas = new Antennas(this, wicoBlockMaster);
            _displays = new Displays(this, wicoBlockMaster, wicoElapsedTime);
            _communications = new Communications(this, wicoBlockMaster, wicoElapsedTime, wicoIGC, _displays);
            _modeAttention = new ModeAttention(this, _wicoControl, _communications);
        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
        }

        public void ModulePostMain()
        {
            if (bInitDone)
            {
                _displays.EchoInfo();
                _wicoControl.WantSlow();
            }

            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
            EchoInstructions();
        }


    }
}
