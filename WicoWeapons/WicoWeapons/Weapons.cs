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
        #region gatlings

        List<IMyTerminalBlock> gatlingsList = new List<IMyTerminalBlock>();
        string gatlingsInit()
        {
 //           List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
            gatlingsList.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySmallGatlingGun>(gatlingsList, localGridFilter);
            return "G" + gatlingsList.Count.ToString("00");
        }
        #endregion

    }
}