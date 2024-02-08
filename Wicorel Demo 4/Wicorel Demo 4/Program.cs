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
        List<IMyShipController> myShipControllers = new List<IMyShipController>();
        List<IMyGyro> myGyros = new List<IMyGyro>();

        public Program()
        {
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(myShipControllers);
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(myGyros);
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            foreach (var shipcontroller in myShipControllers)
            {
                Echo("ShipController=" + shipcontroller.CustomName);
            }
            foreach(var gyro in myGyros)
            {
                Echo("Gyro=" + gyro.CustomName);
            }
        }
    }
}
