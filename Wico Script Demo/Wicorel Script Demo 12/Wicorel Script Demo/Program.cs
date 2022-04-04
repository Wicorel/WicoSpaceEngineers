using Sandbox.Game.EntityComponents;
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

        long runcount = 0;

        public Program()
        {
            ShipMasterInit();
            GyroInit();

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
        }

        bool bDoAlign = true;

        public void Main(string argument, UpdateType updateSource)
        {
            runcount++;
            Echo("Runcount=" + runcount);

            if (argument == "toggle")
            {
                if (bDoAlign)
                    gyrosOff();
                bDoAlign = !bDoAlign;
            }

            IMyShipController myShipController = GetMainController();
            Echo("ShipController=" + myShipController.CustomName);

            Vector3D vNGN = myShipController.GetNaturalGravity();
            Echo(vNGN.ToString());

            Matrix or1;
            myShipController.Orientation.GetMatrix(out or1);

            var vDown = or1.Down;
            if (
                bDoAlign
                && GetShipMass() > 0 
                )
            {
                if (!AlignGyros(vDown, vNGN))
                {
                    Echo("Needs Alignment");
                    Runtime.UpdateFrequency |= UpdateFrequency.Update1;
                }
                else
                {
                    Echo("Aligned");
                    Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
                }
            }

        }

    }
}
