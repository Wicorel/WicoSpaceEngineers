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

        public class Wheels
        {
            List<IMyTerminalBlock> wheelList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> wheelSledList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> wheelRearSledList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> wheelFrontSledList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> wheelRearList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> wheelFrontList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> wheelLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> wheelRightList = new List<IMyTerminalBlock>();

            Program thisProgram;
            public Wheels(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                thisProgram.AddResetMotionHandler(ResetMotionHandler);
            }

            void ResetMotionHandler(bool bNoDrills = false)
            {
                WheelsPowerUp(0, 75);
            }

            const string SledWheels = "[SLED]";
            const string RearWheels = "[REAR]";
            const string FrontWheels = "[FRONT]";
            const string RightWheels = "[RIGHT]";
            const string LeftWheels = "[LEFT]";

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyMotorSuspension)
                {
                    if (tb.CustomName.Contains(SledWheels) || tb.CustomData.Contains(SledWheels))
                    {
                        wheelSledList.Add(tb);
                        if (tb.CustomName.Contains(RearWheels) || tb.CustomData.Contains(FrontWheels))
                        {
                            wheelRearSledList.Add(tb);
                        }
                        if (tb.CustomName.Contains(FrontWheels) || tb.CustomData.Contains(FrontWheels))
                        {
                            wheelFrontSledList.Add(tb);
                        }
                    }
                    else
                    {
                        wheelList.Add(tb);
                        if (tb.CustomName.Contains(LeftWheels) || tb.CustomData.Contains(LeftWheels))
                        {
                            wheelLeftList.Add(tb);
                        }
                        else if (tb.CustomName.Contains(RightWheels) || tb.CustomData.Contains(RightWheels))
                        {
                            wheelRightList.Add(tb);
                        }
                        if (tb.CustomName.Contains(RearWheels) || tb.CustomData.Contains(FrontWheels))
                        {
                            wheelRearList.Add(tb);
                        }
                        if (tb.CustomName.Contains(FrontWheels) || tb.CustomData.Contains(FrontWheels))
                        {
                            wheelFrontList.Add(tb);
                        }
                    }
                }
            }
            void LocalGridChangedHandler()
            {
                wheelList.Clear();
                wheelSledList.Clear();
                wheelRearSledList.Clear();
                wheelFrontSledList.Clear();

                wheelRearList.Clear();
                wheelFrontList.Clear();
                wheelLeftList.Clear();
                wheelRightList.Clear();
            }
            public bool HasSledWheels()
            {
                if (wheelSledList.Count > 0)
                    return true;

                return false;
            }

            public void PrepareSledTravel()
            {
                foreach (var wh1 in wheelSledList)
                {
                    var w1 = wh1 as IMyMotorSuspension;

                    // BUG in 1.186.200.  using setter sets to MAX and not set value
                    w1.SetValueFloat("Friction", 0);
                    //w1.Friction = 0; // 1.186.201 02152018
                }
            }

            public bool HasWheels()
            {
                if (wheelList.Count > 0)
                {
                    return true;
                }
                return false;
            }
            // 1.187
            //Propulsion override:Single (0)
            //Steer override:Single(0)
            public bool WheelsPowerUp(float targetPower, float fFriction = -1)
            {
                thisProgram.Echo("WPP:" + targetPower.ToString() + ":" + fFriction.ToString());

                // TODO: left and right wheels need to have thrust reversed (negated)
                // TODO: Ramp power up like rotor. return false if not fully set to desired power

                bool bAtMax = true;
                if (targetPower < 0f) targetPower = 0f;
                if (targetPower > 100f) targetPower = 100f;

                foreach (var wh1 in wheelRightList)
                {
                    var w1 = wh1 as IMyMotorSuspension;
                    float currentPower = w1.GetValueFloat("Propulsion override");
                    thisProgram.Echo("CPower:" + currentPower.ToString("0.00") + "\n" + w1.CustomName);
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
                    thisProgram.Echo("Setting override to" + cPower.ToString("0.000"));
                    w1.SetValueFloat("Propulsion override", -cPower);
                }
                foreach (var wh1 in wheelLeftList)
                {
                    var w1 = wh1 as IMyMotorSuspension;
                    float currentPower = w1.GetValueFloat("Propulsion override");
                    thisProgram.Echo("CPower:" + currentPower.ToString("0.00") + "\n" + w1.CustomName);
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
                    thisProgram.Echo("Setting override to" + (cPower).ToString("0.000"));
                    w1.SetValueFloat("Propulsion override", cPower);
                }
                return bAtMax;
            }

            public void WheelsSetFriction(float fFriction, bool bSetSled=false)
            {
                if(bSetSled)
                {
                    foreach (var wh1 in wheelSledList)
                    {
                        var w1 = wh1 as IMyMotorSuspension;

                        // BUG in 1.186.200.  using setter sets to MAX and not set value
                        w1.SetValueFloat("Friction", fFriction);
                        //w1.Friction = fFriction; 
                    }
                }
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
}
