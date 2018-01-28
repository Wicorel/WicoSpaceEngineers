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
        double maxReactorPower = -1;
        List<IMyTerminalBlock> reactorList = new List<IMyTerminalBlock>();

        void initReactors()
        {
            reactorList.Clear();
            GetTargetBlocks<IMyReactor>(ref reactorList);
//            GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactorList, localGridFilter);

            float currentOutput;
            reactorCheck(out currentOutput);
        }

        double getCurrentReactorOutput()
        {
            double output = 0;
            foreach (var tb in reactorList)
            {
                IMyReactor r = tb as IMyReactor;
                output += r.CurrentOutput;
            }
            return output;
        }

        bool reactorCheck(out float currentOutput)
        {
            currentOutput = 0;
            maxReactorPower = -1;
            bool bNeedyReactor = false;
            if (reactorList.Count > 0)
                maxReactorPower = 0;

            foreach(IMyReactor r in reactorList)
            {
                // check inventory.
                // check power modes

                // if reactor is working, add up its values.
                currentOutput += r.CurrentOutput;
                maxReactorPower += r.MaxOutput;

            }
            return bNeedyReactor;
        }


    }
}