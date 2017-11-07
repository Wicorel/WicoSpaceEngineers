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
        // 11/06 return true if timer was found and triggered
        //03/27: Added caching for performance
        #region triggers

        Dictionary<string, List<IMyTerminalBlock>> dTimers = new Dictionary<string, List<IMyTerminalBlock>>();

        void initTimers()
        {
            dTimers.Clear();
        }

        bool doSubModuleTimerTriggers(string sKeyword = "[WCCS]")
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
                blocks = GetBlocksContains<IMyTerminalBlock>(sKeyword);
                dTimers.Add(sKeyword, blocks);
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                theTriggerTimer = blocks[i] as IMyTimerBlock;
                if (theTriggerTimer != null)
                {
                    //            Echo("dSMT:" + blocks[i].CustomName);
                    theTriggerTimer.ApplyAction("TriggerNow");
                    bTriggered = true;
                }
            }
            return bTriggered;
        }

        #endregion

    }
}