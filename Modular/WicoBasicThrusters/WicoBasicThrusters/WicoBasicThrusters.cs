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
        public class WicoBasicThrusters
        {
            protected List<IMyTerminalBlock> thrustAllList = new List<IMyTerminalBlock>();
            protected Program _program;
            protected WicoBlockMaster _wicoBlockMaster;

            readonly bool MeGridOnly = false;

            protected string sThrusterSection = "THRUSTERS";

            protected string sCutterThruster = "cutter";

            public int ThrusterCount()
            {
                return thrustAllList.Count;
            }

            public WicoBasicThrusters(Program program, WicoBlockMaster wicoBlockMaster, bool bMeGridOnly=false)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;

                ThrustersInit();
            }
            public void ThrustersInit()
            {
                // TODO: change to handler. pay attention to execution sequence that results. (we may need this defined before getting blocks)
                sCutterThruster = _program.CustomDataIni.Get(sThrusterSection, "CutterThruster").ToString(sCutterThruster);
                _program.CustomDataIni.Set(sThrusterSection, "CutterThruster", sCutterThruster);

                // Minimal init; just add handlers
                thrustAllList.Clear();
                _wicoBlockMaster.AddLocalBlockHandler(ThrusterParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                _program.AddResetMotionHandler(ResetMotionHandler);
            }

            void ResetMotionHandler(bool bNoDrills = false)
            {
                powerDownThrusters();
            }

            public void ThrusterParseHandler(IMyTerminalBlock tb)
            {
                if (MeGridOnly
                    && !(tb.CubeGrid.EntityId == _program.Me.CubeGrid.EntityId))
                    return;
                if (tb is IMyThrust)
                {
                    if (tb.CustomName.ToLower().Contains(sCutterThruster))
                        return; // don't add it.
                    thrustAllList.Add(tb);
                }
            }

            void LocalGridChangedHandler()
            {
                thrustAllList.Clear();
            }
            public int powerDownThrusters(int iTypes = thrustAll, bool bForceOff = false)
            {
                return powerDownThrusters(thrustAllList, iTypes, bForceOff);
            }

            public const int thrustatmo = 1;
            public const int thrusthydro = 2;
            public const int thrustion = 4;
            public const int thrusthover = 8;
            public const int thrustAll = 0xff;

            public int powerDownThrusters(List<IMyTerminalBlock> thrusters, int iTypes = thrustAll, bool bForceOff = false)
            {
                int iCount = 0;
                for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                {
                    int iThrusterType = ThrusterType(thrusters[thrusterIndex]);
                    if((iThrusterType & iTypes) > 0)
                    {
                        iCount++;
                        IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
                        thruster.ThrustOverride = 0;
                        if (thruster.IsWorking && bForceOff && thruster.Enabled == true)  // Yes, the check is worth it
                            thruster.Enabled = false;
                        else if (!thruster.IsWorking && !bForceOff && thruster.Enabled == false)
                            thruster.Enabled = true;
                    }
                }
                return iCount;
            }
            public int ThrusterType(IMyTerminalBlock theBlock)
            {
                if (theBlock is IMyThrust)
                {
                    // HoverEngines  http://steamcommunity.com/sharedfiles/filedetails/?id=1225107070
                    if (theBlock.BlockDefinition.SubtypeId.Contains("AtmosphericHover"))
                        return thrusthover;

                    if (theBlock.BlockDefinition.SubtypeId.Contains("Atmo"))
                        return thrustatmo;
                    if (theBlock.BlockDefinition.SubtypeId.Contains("Hydro"))
                        return thrusthydro;

                    // Hover Engines. SmallBlock_HoverEngine http://steamcommunity.com/sharedfiles/filedetails/?id=560731791 (last updated Dec 29, 2015)
                    if (theBlock.BlockDefinition.SubtypeId.Contains("SmallBlock_HoverEngine"))
                        return thrusthover;

                    // assume ion since its name is generic
                    return thrustion;
                }
                // else
                return 0;
            }

        }
    }
}
