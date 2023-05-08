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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class DockBase
        {
            readonly Program _program;

            public DockBase(Program program) 
            {
                _program = program;
            }
            public const string WICOB_DOCKSETRELAUNCH= "WICOB_DOCKSETRELAUNCH";

            public virtual void SetRelaunch(bool bRelaunch=true)
            {
                _program.IGC.SendBroadcastMessage(WICOB_DOCKSETRELAUNCH, bRelaunch.ToString(), TransmissionDistance.CurrentConstruct);
            }

        }
    }
}
