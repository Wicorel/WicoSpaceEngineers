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
        class LandingGears
        {
            List<IMyTerminalBlock> gearList = new List<IMyTerminalBlock>();

            Program _Program;
            WicoBlockMaster _wicoBlockMaster;

            public LandingGears(Program program, WicoBlockMaster wicoBlockMaster)
            {
                _Program = program;
                _wicoBlockMaster = wicoBlockMaster;

                wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyLandingGear)
                {
                    gearList.Add(tb);
                }
            }
            public bool AnyGearIsLocked()
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

            public bool GearReadyToLock(IMyTerminalBlock block)
            {
                var g = block as IMyLandingGear;
                if (g == null) return false;
                return ((int)g.LockMode == 1);

            }
            public bool anyGearReadyToLock()
            {
                var temp = new StringBuilder();
                for (int i = 0; i < gearList.Count; i++)
                {
                    if (GearReadyToLock(gearList[i]))
                        return true;
                }
                return false;
            }

            public void GearsLock(bool bLock = true)
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

            public void BlocksOnOff(bool bOn)
            {
                _wicoBlockMaster.BlocksOnOff(gearList, bOn);
            }
        }
    }
}
