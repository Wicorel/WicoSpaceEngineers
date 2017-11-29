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

        // check for gears with "[DOCK]"
        #region gears 

        List<IMyTerminalBlock> gearList = new List<IMyTerminalBlock>();

        void getLocalGears()
        {
            if (gearList.Count < 1) gearList = GetBlocksContains<IMyLandingGear>("[DOCK]");
            if (gearList.Count < 1) gearList = GetTargetBlocks<IMyLandingGear>();

            return;
        }


        string gearsInit()
        {
            {
                gearList.Clear();
                getLocalGears();
            }
            return "LG" + gearList.Count.ToString("00");
        }
        bool anyGearIsLocked()
        {
            for (int i = 0; i < gearList.Count; i++)
            {
                IMyLandingGear lGear;
                lGear = gearList[i] as IMyLandingGear;
                if (lGear != null && lGear.IsLocked)
                    return true;
            }
            return false;
        }

        bool gearReadyToLock(IMyTerminalBlock block)
        {
            IMyLandingGear g = block as IMyLandingGear;
            if (g == null) return false;
            return ((int)g.LockMode == 1);// LandingGearMode.ReadyToLock);
            /*
            StringBuilder temp = new StringBuilder();
            ITerminalAction theAction;

            temp.Clear();
            theAction = block.GetActionWithName("Lock");
            block.GetActionWithName(theAction.Id.ToString()).WriteValue(block, temp);
            if (temp.ToString().Contains("Ready"))
                return true;
            return false;
            */
        }
        bool anyGearReadyToLock()
        {
            StringBuilder temp = new StringBuilder();
            for (int i = 0; i < gearList.Count; i++)
            {
                if (gearReadyToLock(gearList[i]))
                    return true;
            }
            return false;
        }

        void gearsLock(bool bLock = true)
        {
            for (int i = 0; i < gearList.Count; i++)
            {
                IMyLandingGear g = gearList[i] as IMyLandingGear;
                if (g == null) continue;
                if (bLock)
                    g.Lock();
                else
                    g.Unlock();

//                blockApplyAction(gearList[i], "Lock");
            }
        }
        #endregion


    }
}
