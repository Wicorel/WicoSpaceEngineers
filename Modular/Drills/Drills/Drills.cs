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
        public class Drills
        {
            List<IMyTerminalBlock> drillList = new List<IMyTerminalBlock>();


            Program thisProgram;
            WicoBlockMaster WicoBlockMaster;

            public Drills(Program program, WicoBlockMaster wicoBlockMaster)
            {
                thisProgram = program;
                WicoBlockMaster = wicoBlockMaster;

                WicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                WicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
//                thisProgram.AddPostInitHandler(PostInitHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyShipDrill)
                {
                    drillList.Add(tb as IMyShipDrill);
                }
            }
            void LocalGridChangedHandler()
            {
                drillList.Clear();
            }

            void PostInitHandler()
            {
            }

            public void turnDrillsOn()
            {
                foreach (IMyFunctionalBlock b in drillList)
                {
                    b.Enabled = true;
                }
            }

            public void turnDrillsOff()
            {
                foreach (IMyFunctionalBlock b in drillList)
                {
                    b.Enabled = false;
                }

            }

            public bool HasDrills()
            {
                if (drillList.Count < 1)
                    return false;

                return true;
            }

        }
    }
}
