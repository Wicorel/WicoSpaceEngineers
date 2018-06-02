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

        List<IMyTerminalBlock> wheelList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelSledList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelRearSledList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelFrontSledList = new List<IMyTerminalBlock>();

//        List<IMyTerminalBlock> wheelList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelRearList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelFrontList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> wheelRightList = new List<IMyTerminalBlock>();

        string wheelsInit(IMyTerminalBlock orientationBlock)
        {
            wheelList.Clear();
            wheelSledList.Clear();
            wheelRearSledList.Clear();
            wheelFrontSledList.Clear();
            wheelRearList.Clear();
            wheelFrontList.Clear();
            wheelLeftList.Clear();
            wheelRightList.Clear();

// need to get wheels by orientation...

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
                else
                {
                    if (wheelList[i].CustomName.Contains("[LEFT]") || wheelList[i].CustomData.Contains("[LEFT]"))
                    {
                        wheelLeftList.Add(wheelList[i]);
                    }
                    else if (wheelList[i].CustomName.Contains("[RIGHT]") || wheelList[i].CustomData.Contains("[RIGHT]"))
                    {
                        wheelRightList.Add(wheelList[i]);
                    }
                    if (wheelList[i].CustomName.Contains("[REAR]") || wheelList[i].CustomData.Contains("[FRONT]"))
                    {
                        wheelRearList.Add(wheelList[i]);
                    }
                    if (wheelList[i].CustomName.Contains("[FRONT]") || wheelList[i].CustomData.Contains("[FRONT]"))
                    {
                        wheelFrontList.Add(wheelList[i]);
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
            foreach (var wh1 in wheelSledList)
            {
                var w1 = wh1 as IMyMotorSuspension;

                // BUG in 1.186.200.  using setter sets to MAX and not set value
                w1.SetValueFloat("Friction", 0);
                //w1.Friction = 0; // 1.186.201 02152018

                //                w1.SetValueFloat("Friction", 0);
                //                w1.SetValueFloat("Strength", 20);
                //                w1.Friction = 0;
            }
        }

        bool HasWheels()
        {
            if(wheelList.Count>0)
            {
                return true;
            }
            return false;
        }
        // 1.187
        //Propulsion override:Single (0)
        //Steer override:Single(0)
        bool WheelsPowerUp(float targetPower, float fFriction=-1)
        {
            Echo("WPP:" + targetPower.ToString() + ":" + fFriction.ToString());

            // TODO: left and right wheels need to have thrust reversed (negated)
            // TODO: Ramp power up like rotor. return false if not fully set to desired power

            bool bAtMax = true;
            if (targetPower < 0f) targetPower = 0f;
            if (targetPower > 100f) targetPower = 100f;

            foreach (var wh1 in wheelRightList)
            {
                var w1 = wh1 as IMyMotorSuspension;
                float currentPower = w1.GetValueFloat("Propulsion override");
                Echo("CPower:" + currentPower.ToString("0.00") + "\n" + w1.CustomName);
                float cPower = (currentPower );
                cPower = Math.Abs(cPower);
                if (cPower < 1) cPower *= 100f;
                if (targetPower > (cPower + 5f))
                {
                    // speed up
                    bAtMax = false;
                    cPower += 5;
                }
                else if (targetPower < (cPower - 5))
                {
                    // slow down
                    bAtMax = false;
                    cPower -= 5;
                }
                else cPower = targetPower;

                // BUG in 1.186.200.  using setter sets to MAX and not set value
                if (fFriction >= 0) w1.SetValueFloat("Friction", fFriction);
//                if (cPower > 1) cPower /= 100f;
                Echo("Setting override to" + cPower.ToString("0.000"));
                w1.SetValueFloat("Propulsion override", -cPower);
            }
            foreach (var wh1 in wheelLeftList)
            {
                var w1 = wh1 as IMyMotorSuspension;
                float currentPower = w1.GetValueFloat("Propulsion override");
                Echo("CPower:" + currentPower.ToString("0.00") + "\n"+w1.CustomName);
                float cPower = (currentPower);
                cPower = Math.Abs(cPower);
                if (cPower < 1) cPower *= 100f;
                if (targetPower > (cPower + 5f))
                {
                    // speed up
                    bAtMax = false;
                    cPower += 5;
                }
                else if (targetPower < (cPower - 5))
                {
                    // slow down
                    bAtMax = false;
                    cPower -= 5;
                }
                else cPower = targetPower;

                // BUG in 1.186.200.  using setter sets to MAX and not set value
                if (fFriction >= 0) w1.SetValueFloat("Friction", fFriction);
//                if (cPower > 1) cPower /= 100f;
                Echo("Setting override to" + (cPower).ToString("0.000"));
                w1.SetValueFloat("Propulsion override", cPower);
            }
            return bAtMax;
        }

        void WheelsSetFriction(float fFriction)
        {
            foreach (var wh1 in wheelList)
            {
                var w1 = wh1 as IMyMotorSuspension;

                // BUG in 1.186.200.  using setter sets to MAX and not set value
                w1.SetValueFloat("Friction", fFriction);
                //w1.Friction = fFriction; 

            }

        }
    }
}