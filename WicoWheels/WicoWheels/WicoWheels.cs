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
        //20170107 Added sled and rear/front
        #region wheels

        List<IMyTerminalBlock> wheelList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelSledList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelRearSledList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelFrontSledList = new List<IMyTerminalBlock>();


        string wheelsInit(IMyTerminalBlock orientationBlock)
        {
            wheelList.Clear();
            wheelSledList.Clear();
            wheelRearSledList.Clear();
            wheelFrontSledList.Clear();
            /*
            if (orientationBlock == null) return "Wheels:NO CONTROLLER";

            GridTerminalSystem.GetBlocksOfType<IMyMotorSuspension>(wheelList, localGridFilter);

            Matrix fromGridToReference;
            orientationBlock.Orientation.GetMatrix(out fromGridToReference);
            Matrix.Transpose(ref fromGridToReference, out fromGridToReference);
            */
            GetTargetBlocks<IMyMotorSuspension>(ref wheelList);
            for (int i = 0; i < wheelList.Count; i++)
            {
                if (wheelList[i].CustomName.Contains("[SLED]") || wheelList[i].CustomData.Contains("[SLED]"))
                {
                    wheelSledList.Add(wheelList[i]);
                    if (wheelList[i].CustomName.Contains("[REAR]") || wheelList[i].CustomData.Contains("[FRONT]"))
                    {
                        wheelRearSledList.Add(wheelList[i]);
                    }
                    if (wheelList[i].CustomName.Contains("[FRONT]") || wheelList[i].CustomData.Contains("[FRONT]"))
                    {
                        wheelFrontSledList.Add(wheelList[i]);
                    }
                }
            }
            return "W" + wheelList.Count.ToString("0") + "WS" + wheelSledList.Count.ToString("0") + "SR" + wheelRearSledList.Count.ToString("0") + "SF" + wheelFrontSledList.Count.ToString("0");
        }

        bool HasSledWheels()
        {
            if (wheelSledList.Count > 0)
                return true;

            return false;
        }

        void PrepareSledTravel()
        {
            for(int i1=0;i1<wheelSledList.Count;i1++)
            {
                var w1 = wheelSledList[i1] as IMyMotorSuspension;
                w1.SetValueFloat("Friction", 0);
//                w1.SetValueFloat("Strength", 20);
//                w1.Friction = 0;
            }
        }

        #endregion

    }
}