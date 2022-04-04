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
    partial class Program
    {
        List<IMyShipController> myShipControllers = new List<IMyShipController>();

        public void ShipMasterInit()
        {
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(myShipControllers
                , x1 => x1.IsSameConstructAs(Me));

        }
        private IMyShipController MainShipController;
        /// <summary>
        /// Returns the main ship controller
        /// </summary>
        /// <returns>best shipcontroller or null</returns>
        public IMyShipController GetMainController()
        {
            //  check for occupied, etc.
            foreach (var tb in myShipControllers)
            {
                if (tb.IsUnderControl && tb.CanControlShip)
                {
                    // found a good one
                    MainShipController = tb;
                    break;
                }
            }
            if (MainShipController == null)
            {
                // check in order of preference
                foreach (var tb in myShipControllers)
                {
                    if (tb is IMyRemoteControl && tb.CanControlShip)
                    {
                        // found a good one
                        MainShipController = tb;
                        break;
                    }
                }
                // we didn't find one
                if (MainShipController == null)
                {
                    foreach (var tb in myShipControllers)
                    {
                        if (tb is IMyCockpit && tb.CanControlShip)
                        {
                            // found a good one
                            MainShipController = tb;
                            break;
                        }
                    }
                }
            }
            return MainShipController;
        }

        public float GetShipMass()
        {
            GetMainController();

            var shipmass = MainShipController.CalculateShipMass();
            Echo("Mass=" + shipmass.PhysicalMass.ToString("0.00"));

            return shipmass.PhysicalMass;
        }


    }
}
