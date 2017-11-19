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
        #region shipcontrollers

        List<IMyTerminalBlock> controllersList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> remoteControl1List = new List<IMyTerminalBlock>();

        string controllersInit()
        {
            controllersList.Clear();
            remoteControl1List.Clear();
            //	controllersList=GetTargetBlocks<IMyShipController>(ref controllersList);
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllersList, localGridFilter);

            for (int i = 0; i < controllersList.Count; i++)
            {
                if (controllersList[i] is IMyRemoteControl)
                    remoteControl1List.Add(controllersList[i]);
            }
            return "SC" + controllersList.Count.ToString("0");
        }

        IMyShipController GetActiveController()
        {
            IMyShipController sc = null;

            bool bHasMain = false;
            for (int i = 0; i < controllersList.Count; i++)
            {
                IMyCockpit imyc = controllersList[i] as IMyCockpit;
                if (imyc != null)
                {
                    if (imyc.IsMainCockpit)
                    {
                        bHasMain = true;
                        if (imyc.IsUnderControl)
                            return imyc;
                        else Echo("Main cockpit not occupied:" + imyc.CustomName);
                    }
                }
            }
            if (bHasMain) return sc; // there IS a main and it's not occupied.

            for (int i = 0; i < controllersList.Count; i++)
            {
                if (((IMyShipController)controllersList[i]).IsUnderControl)
                {
                    sc = controllersList[i] as IMyShipController;
                    break;
                }
            }
            return sc;
        }

        #endregion

    }
}