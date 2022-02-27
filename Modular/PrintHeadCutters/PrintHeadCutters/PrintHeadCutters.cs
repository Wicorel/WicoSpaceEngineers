using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PrintHeadCutters
        {
            List<IMyTerminalBlock> cutterList = new List<IMyTerminalBlock>();

            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            bool MeGridOnly = false;

            public PrintHeadCutters(Program program, WicoBlockMaster wicoBlockMaster, bool bMeGridOnly=false)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;
                MeGridOnly = bMeGridOnly;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
//                _wicoBlockMaster.AddLocalBlockParseDone(LocalGridParseDone);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (MeGridOnly
                    && !(tb.CubeGrid.EntityId == _program.Me.CubeGrid.EntityId))
                    return;
                if (
                    ( tb is IMyThrust || tb is IMySmallGatlingGun) 
                    &&
                    (tb.CustomName.ToLower().Contains("cutter") || tb.CustomData.ToLower().Contains("cutter"))
                    )
                {
                    cutterList.Add(tb);
                }
            }
            void LocalGridChangedHandler()
            {
                cutterList.Clear();
            }
            void LocalGridParseDone()
            {

            }

            public int AvailableCutters()
            {
                return cutterList.Count;
            }

            public bool DoCut(bool bCut = true)
            {
                bool bDoingCut = false;
                //            Echo("CUTTING!");
                foreach(var cutter in cutterList)
                {
                    if(cutter is IMyThrust)
                    {
                        var thrust = cutter as IMyThrust;
                        if(bCut)
                        {
                            thrust.Enabled = true;
                            thrust.ThrustOverridePercentage = 100f;
                            bDoingCut = true;
                        }
                        else
                        {
                            thrust.Enabled = false;
                            thrust.ThrustOverride = 0;
                        }
                    }
                    else if(cutter is IMySmallGatlingGun)
                    {
                        var gatling = cutter as IMySmallGatlingGun;
                        if (bCut)
                        {
                            gatling.Enabled = true;

                            gatling.ApplyAction("ShootOnce");
                            bDoingCut = true;
                        }
                        else
                        {
                            gatling.Enabled = false;
                        }

                    }
                }
                return bDoingCut;
            }


        }
    }
}
