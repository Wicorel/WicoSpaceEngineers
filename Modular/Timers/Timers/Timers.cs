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
        public class Timers
        {
            List<IMyTerminalBlock> localTimerList = new List<IMyTerminalBlock>();

            // cached timer block by searched name
            Dictionary<string, List<IMyTerminalBlock>> dTimers = new Dictionary<string, List<IMyTerminalBlock>>();

            Program thisProgram;
            WicoBlockMaster wicoBlockMaster;

            public Timers(Program program, WicoBlockMaster wbm)
            {
                thisProgram = program;
                wicoBlockMaster = wbm;

                wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyTimerBlock)
                {
                    localTimerList.Add(tb);
                }
            }
            void LocalGridChangedHandler()
            {
                localTimerList.Clear();
                dTimers.Clear();
            }

            public void initTimers()
            {
                dTimers.Clear();
            }

            public bool TimerTriggers(string sKeyword)
            {
                bool bTriggered = false;
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

                IMyTimerBlock theTriggerTimer = null;

                if (dTimers.ContainsKey(sKeyword))
                {
                    blocks = dTimers[sKeyword];
                }
                else
                {
                    blocks = wicoBlockMaster.GetBlocksContains<IMyTimerBlock>(sKeyword);
                    dTimers.Add(sKeyword, blocks);
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    theTriggerTimer = blocks[i] as IMyTimerBlock;
                    if (theTriggerTimer != null)
                    {
                        //            Echo("dSMT:" + blocks[i].CustomName);
                        if (theTriggerTimer.Enabled)
                        {
                            theTriggerTimer.Trigger();
                            bTriggered = true;
                        }
                        else
                        {
                            thisProgram.Echo("Timer:" + theTriggerTimer.CustomName + " is OFF");
                        }
                    }
                }
                return bTriggered;
            }

        }
    }
}
