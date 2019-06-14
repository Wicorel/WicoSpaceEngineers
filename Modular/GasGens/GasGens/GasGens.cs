using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        class GasGens
        {
            List<IMyTerminalBlock> gasgenList = new List<IMyTerminalBlock>();

            Program thisProgram;
            public GasGens(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyGasGenerator)
                {
                    gasgenList.Add(tb);
                }
            }
            public void GasGensEnable(bool bOn = true)
            {
                thisProgram.wicoBlockMaster.BlocksOnOff(gasgenList, bOn);
            }

        }
    }
}