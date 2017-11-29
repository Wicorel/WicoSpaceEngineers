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
        #region blockactions
        void groupApplyAction(string sGroup, string sAction)
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>(); GridTerminalSystem.GetBlockGroups(groups); for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                if (groups[groupIndex].Name == sGroup)
                {
                    List<IMyTerminalBlock> theBlocks = null;
                    groups[groupIndex].GetBlocks(theBlocks, (x1 => x1.CubeGrid == Me.CubeGrid));
                    ; for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
                    { theBlocks[iIndex].ApplyAction(sAction); }
                    return;
                }
            }
            return;
        }
        void listSetValueFloat(List<IMyTerminalBlock> theBlocks, string sProperty, float fValue)
        {
            for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
            {
                if (theBlocks[iIndex].CubeGrid == Me.CubeGrid)
                    theBlocks[iIndex].SetValueFloat(sProperty, fValue);
            }
            return;
        }
        void blockApplyAction(string sBlock, string sAction)
        { List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); blocks = GetBlocksNamed<IMyTerminalBlock>(sBlock); blockApplyAction(blocks, sAction); }
        void blockApplyAction(IMyTerminalBlock sBlock, string sAction)
        { ITerminalAction ita; ita = sBlock.GetActionWithName(sAction); if (ita != null) ita.Apply(sBlock); else Echo("Unsupported action:" + sAction); }
        void blockApplyAction(List<IMyTerminalBlock> lBlock, string sAction)
        {
            if (lBlock.Count > 0)
            {
                for (int i = 0; i < lBlock.Count; i++)
                { ITerminalAction ita; ita = lBlock[i].GetActionWithName(sAction); if (ita != null) ita.Apply(lBlock[i]); else Echo("Unsupported action:" + sAction); }
            }
        }
        #endregion

    }
}