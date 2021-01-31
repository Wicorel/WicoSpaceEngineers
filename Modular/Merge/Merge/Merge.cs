using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Merge
        {
            List<IMyTerminalBlock> mergeList = new List<IMyShipMergeBlock>();

            Program _program;
            WicoBlockMaster wbm;

            public Merge(Program program, WicoBlockMaster wicoBlockMaster)
            {
                _program = program;
                wbm = wicoBlockMaster;

                wbm.AddLocalBlockHandler(BlockParseHandler);
                wbm.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            // from https://discord.com/channels/125011928711036928/216219467959500800/741840137524215818
            public static bool IsMergeConnected(IMyShipMergeBlock merge)
            {
                if (merge == null)
                    return false;

                Vector3I testDirn = Base6Directions.GetIntVector(Base6Directions.GetOppositeDirection(merge.Orientation.Left));
                Vector3I testPos = merge.Position + testDirn;

                IMySlimBlock other = merge.CubeGrid.GetCubeBlock(testPos);
                if (other == null)
                    return false;

                IMyCubeBlock fat = other.FatBlock;
                if (fat == null)
                    return false;

                return fat is IMyShipMergeBlock;
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyShipMergeBlock)
                {
                    mergeList.Add(tb as IMyShipMergeBlock);
                }
            }

            void LocalGridChangedHandler()
            {
                mergeList.Clear();
            }

        }
    }
}
