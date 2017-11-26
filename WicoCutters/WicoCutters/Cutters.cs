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
        // cutters for projection builds.

//        List<IMyThrust> thrustCutterList = new List<IMyThrust>();

        List<IMyTerminalBlock> cutterList = new List<IMyTerminalBlock>();

        void initCutters()
        {
            cutterList.Clear();
            List<IMyTerminalBlock> cutterLocal = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cutterLocal, (x1 => x1.CubeGrid == Me.CubeGrid));
            for (int i = 0; i < cutterLocal.Count; i++)
            {
                if (cutterLocal[i].CustomName.ToLower().Contains("cutter") || cutterLocal[i].CustomData.ToLower().Contains("cutter"))
                {
                    cutterList.Add(cutterLocal[i]);
                }
            }

        }

        bool doCut(bool bCut = true)
        {
            bool bDoingCut = false;
//            Echo("CUTTING!");
		    for (int i = 0; i < cutterList.Count; i++)
		    {
                if (cutterList[i] is IMyThrust)
                {
                    Echo("Thruster!");
                    var t = cutterList[i] as IMyThrust;
                    if (bCut)
                    {
                        t.Enabled = true;
                        t.SetValueFloat("Override", 100.0f);
                        bDoingCut = true;
                    }
                    else
                    {
                        t.Enabled = false;
                        t.SetValueFloat("Override", 0f);
                    }
                }
                else if (cutterList[i] is IMySmallGatlingGun)
                {
                    Echo("Gatling!");
                    var g = cutterList[i] as IMySmallGatlingGun;
                    if (bCut)
                    {
                        g.Enabled = true;
                        g.ApplyAction("ShootOnce");
                        bDoingCut = true;
                    }
                }
                else if (cutterList[i] is IMyFunctionalBlock)
                {
                   var f = cutterList[i] as IMyFunctionalBlock;
                   f.Enabled = bCut;
                }
 
		    }
            return bDoingCut;
        }
    }
}
