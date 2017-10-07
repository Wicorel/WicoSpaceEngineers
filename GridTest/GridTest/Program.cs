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
  
        List<IMyTerminalBlock> gtsAllBlocks = new List<IMyTerminalBlock>();

        List<IMyCubeGrid> allGrids = new List<IMyCubeGrid>();

        IMyTextPanel statustextblock = null;

        public Program()
        {
            gridsInit();
            textPanelInit();
            Log("\n"+DateTime.Now.ToString() + " Constructor");
            Log("#Grids=" + allGrids.Count.ToString());
            if (allGrids.Count < 2) Log("  ^^^ INCORRECT!");
            Log("#Blocks=" + gtsAllBlocks.Count.ToString());
        }

        void Log(string text)
        {
            if (statustextblock == null)
            {
                Echo("NO TEXTBLOCK");
                Echo(text);
                return;
            }
            statustextblock.WritePublicText(text + "\n", true);
        }

        void textPanelInit()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            statustextblock = null;
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>( blocks);
            if (blocks.Count > 0)
                statustextblock = blocks[0] as IMyTextPanel;
        }

        void gridsInit()
        {
            gtsAllBlocks.Clear();
            allGrids.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsAllBlocks);

            foreach (var block in gtsAllBlocks)
            {
                var grid = block.CubeGrid;
                if (!allGrids.Contains(grid))
                {
                    allGrids.Add(grid);
                }
            }
        }

        public void Save()
        {
        }

        public void Main(string argument)
        {
            gridsInit();
            textPanelInit();
            Log("\n"+DateTime.Now.ToString() +" Main");
            Log("#Grids=" + allGrids.Count.ToString());
            Log("#Blocks=" + gtsAllBlocks.Count.ToString());
        }
    }
}