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
        #region solar
        List<IMyTerminalBlock> solarList = new List<IMyTerminalBlock>();

        float currentSolarOutput = 0;

        double maxSolarPower = -1;

        void initSolars()
        {
            solarList.Clear();
            maxSolarPower = -1;
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarList, localGridFilter);
            calcCurrentSolar();
        }

        void calcCurrentSolar()
        {
            if (solarList.Count > 0)
                maxSolarPower = 0;

            currentSolarOutput = 0;

            //	Echo("Solars:");
            foreach (var tb in solarList)
            {
                IMySolarPanel r = tb as IMySolarPanel;
                //		Echo(r.CustomName + " Max=" + r.MaxOutput.ToString("0.000") + " c=" + r.CurrentOutput.ToString("0.000"));
                maxSolarPower += r.MaxOutput;
                currentSolarOutput += r.CurrentOutput;
            }

        }

        #endregion

    }
}