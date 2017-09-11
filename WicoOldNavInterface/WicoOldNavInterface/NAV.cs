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
        #region NAV
        const string sNavTimer = "NAV Timer";
        const string sNavEnable = "NAV Enable";
        const string sNavStatus = "NAV Status:";
        const string sNavCmd = "NAV:";
        const string sNavInstructions = "NAV Instructions";
        const string sRemoteControl = "NAV Remote Control";
        IMyTimerBlock navTriggerTimer = null;
        IMyTerminalBlock navCommand = null;
        IMyTerminalBlock navStatus = null;
        bool bNavCmdIsTextPanel = false;
//        IMyTerminalBlock gpsCenter = null;
        IMyTerminalBlock navEnable = null;
        IMyRemoteControl remoteControl = null;

        string NAVInit()
        {
            Echo("Navinit()");
            string sInitResults = "";
            if (!(gpsCenter is IMyRemoteControl))
            {
                Echo("NO RC!");
                return "No Remote Control for NAV";
            }
            remoteControl = (IMyRemoteControl)gpsCenter;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blocks = GetTargetBlocks<IMyTerminalBlock>(sNavStatus);
            if (blocks.Count > 0)
            {
                for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
                {
                    string name = blocks[blockIndex].CustomName;
                    if (name.StartsWith(sNavStatus))
                    {
                        sInitResults += "S";
                        navStatus = blocks[blockIndex];
                        break;
                    }
                }
            }
            else sInitResults += "-";
            blocks = GetTargetBlocks<IMyTerminalBlock>(sNavCmd);
            if (blocks.Count > 0)
            {
                for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
                {
                    string name = blocks[blockIndex].CustomName;
                    if (name.StartsWith(sNavCmd))
                    {
                        sInitResults += "C";
                        navCommand = blocks[blockIndex];
                        bNavCmdIsTextPanel = false;
                        break;
                    }
                }
            }
            else
            {
                blocks = GetBlocksNamed<IMyTextPanel>(sNavInstructions);
                if (blocks.Count > 0)
                {
                    sInitResults += "T";
                    navCommand = blocks[0];
                }
                bNavCmdIsTextPanel = true;
            }
            blocks = GetBlocksNamed<IMyTerminalBlock>(sNavTimer);
            if (blocks.Count > 1)
                throw new OurException("Multiple blocks found: \"" + sNavTimer + "\"");
            else if (blocks.Count == 0)
                Echo("Missing: " + sNavTimer);
            else navTriggerTimer = blocks[0] as IMyTimerBlock;
            blocks = GetBlocksNamed<IMyTerminalBlock>(sNavEnable);
            if (blocks.Count > 1)
                throw new OurException("Multiple blocks found: \"" + sNavEnable + "\"");
            else if (blocks.Count == 0)
                Echo("Missing: " + sNavEnable);
            else navEnable = blocks[0] as IMyTerminalBlock;
            Echo("EO:Navinit()");

            return sInitResults;
        }

        void startNavCommand(string sCmd)
        {
            string sNav = sCmd;
            if (navCommand == null || navStatus == null)
            {
                Echo("No nav Command/Status blocks found");
                return;
            }
            if (navCommand is IMyTextPanel)
            {
                ((IMyTextPanel)navCommand).WritePublicText(sNav);
            }
            else navCommand.CustomName = sNavCmd + " " + sNav;
            navStatus.CustomName = sNavStatus + " Command Set";
            if (navEnable != null) blockApplyAction(navEnable, "OnOff_On");
            if (navTriggerTimer != null) navTriggerTimer.ApplyAction("Start");
        }

        void startNavWaypoint(Vector3D vWaypoint, bool bOrient = false, int iRange = 10)
        {
            string sNav;
            sNav = "";
            sNav = "D " + iRange;
            if (bNavCmdIsTextPanel) sNav += "\n";
            else sNav += "; ";
            if (bOrient) sNav += "O ";
            else sNav += "W ";

            sNav += Vector3DToString(vWaypoint);
            if (navCommand == null || navStatus == null)
            {
                throw new OurException("No nav Command/Status blocks found");
            }
            if (navCommand is IMyTextPanel)
            {
                ((IMyTextPanel)navCommand).WritePublicText(sNav);
            }
            else navCommand.CustomName = sNavCmd + " " + sNav;
            navStatus.CustomName = sNavStatus + " Command Set";
            if (navEnable != null) blockApplyAction(navEnable, "OnOff_On");
            if (navTriggerTimer != null)
            {
                navTriggerTimer.SetValueFloat("TriggerDelay", 1.0f);
                navTriggerTimer.ApplyAction("Start");
            }
        }

        void startNavRotate(Vector3D vWaypoint)
        {
            string sNav;
            sNav = "";
            sNav += "r ";
            sNav += Vector3DToString(vWaypoint);
            if (navCommand is IMyTextPanel)
            {
                ((IMyTextPanel)navCommand).WritePublicText(sNav);
            }
            else navCommand.CustomName = sNavCmd + " " + sNav;
            navStatus.CustomName = sNavStatus + " Command Set";
            blockApplyAction(navEnable, "OnOff_On");
            navTriggerTimer.ApplyAction("Start");
        }

        #endregion

    }
}