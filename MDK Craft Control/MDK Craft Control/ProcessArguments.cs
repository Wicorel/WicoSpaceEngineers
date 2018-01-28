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



        // multi-arg
        #region arguments

        bool processArguments(string sArgument)
        {
            string[] varArgs = sArgument.Trim().Split(';');

            for (int iArg = 0; iArg < varArgs.Length; iArg++)
            {
                string[] args = varArgs[iArg].Trim().Split(' ');

                if (args[0] == "timer")
                {
                    processTimerCommand();

                }
                else if (args[0] == "idle")
                    ResetToIdle();
                else if (args[0] == "masterreset")
                    MasterReset();
                else if (args[0].ToLower() == "coast")
                {
                    //	Echo("Coast: backward =" + thrustBackwardList.Count.ToString());
                    if (thrustBackwardList.Count > 1)
                    {
                        blocksToggleOnOff(thrustBackwardList);
//                        blockApplyAction(thrustBackwardList, "OnOff");
                        //				blockApplyAction(thrustBackwardList, "OnOff_Off");
                    }
                }
                else if (args[0] == "setvaluef")
                {
                    Echo("SetValueFloat");
                    //Miner Advanced Rotor:UpperLimit:-24
                    string sArg = "";
                    for (int i = 1; i < args.Length; i++)
                    {
                        sArg += args[i];
                        if (i < args.Length - 1)
                        {
                            sArg += " ";
                        }
                    }
                    string[] cargs = sArg.Trim().Split(':');

                    if (cargs.Length < 3)
                    {
                        Echo("Invalid Args");
                        continue;
                    }
                    IMyTerminalBlock block;
                    block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(cargs[0]);
                    if (block == null)
                    {
                        Echo("Block not found:" + cargs[0]);
                        continue;
                    }
                    float fValue = 0;
                    bool fOK = float.TryParse(cargs[2].Trim(), out fValue);
                    if (!fOK)
                    {
                        Echo("invalid float value:" + cargs[2]);
                        continue;
                    }
                    Echo("SetValueFloat:" + cargs[0] + " " + cargs[1] + " to:" + fValue.ToString());
                    block.SetValueFloat(cargs[1], fValue);
                }
                else if (args[0] == "brake")
                {
                    Echo("brake");
                    //toggle brake
                    if (shipOrientationBlock is IMyShipController)
                    {
                        IMyShipController msc = shipOrientationBlock as IMyShipController;
                        bool bBrake = msc.HandBrake;
                        msc.ApplyAction("HandBrake");
                    }
                    else Echo("No Ship Controller found");

                }
                else if (args[0] == "wcct" || args[0] == "")
                {
                    // do nothing special
                }
                else
                {
                    int iDMode;
                    if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                    {
                        setMode(iDMode);
                    }
                    else Echo("Unrecognized Command:" + varArgs[iArg]);
                }
            }
            return false; // keep processing in main
        }
        #endregion


    }
}