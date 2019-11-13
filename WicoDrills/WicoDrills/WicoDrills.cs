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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region drills

        List<IMyTerminalBlock> drillList = new List<IMyTerminalBlock>();
        string drillInit()
        {
            List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();

            drillList.Clear();
            Output = GetTargetBlocks<IMyShipDrill>();
            foreach (var b in Output)
                drillList.Add(b as IMyTerminalBlock);
            return "D" + drillList.Count.ToString("00");
        }

        void turnDrillsOn()
        {
            foreach(IMyFunctionalBlock b in drillList)
            {
                b.Enabled = true;
            }

        }

        void turnDrillsOff()
        {
            if (drillList.Count < 1) drillInit();

            foreach(IMyFunctionalBlock b in drillList)
            {
                b.Enabled = false;
            }

        }

        bool HasDrills()
        {
            if (drillList.Count < 1)
                drillInit();

            if (drillList.Count < 1)
                return false;

            return true;
        }
        #endregion
        #region ejectors

        List<IMyTerminalBlock> ejectorList = new List<IMyTerminalBlock>();
        string ejectorsInit()
        {
            List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
            ejectorList.Clear();
            Output = GetBlocksContains<IMyShipConnector>("Ejector");
            foreach (var b in Output)
                ejectorList.Add(b as IMyTerminalBlock);
            return "E" + ejectorList.Count.ToString("00");
        }

        void turnEjectorsOn()
        {
            if (ejectorList.Count < 1) ejectorsInit();

            foreach (IMyFunctionalBlock b in ejectorList)
            {
                if(!b.Enabled)
                    b.Enabled = true;
            }

        }

        void turnEjectorsOff()
        {
            if (ejectorList.Count < 1)    ejectorsInit();

            foreach(IMyFunctionalBlock b in ejectorList)
            {
                if(b.Enabled)
                    b.Enabled = false;
            }

        }
        #endregion

    }
}