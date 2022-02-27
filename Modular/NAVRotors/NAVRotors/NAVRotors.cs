﻿using Sandbox.Game.EntityComponents;
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

        public class NavRotors
        {
            List<IMyMotorStator> rotorNavList = new List<IMyMotorStator>();
            List<IMyMotorStator> rotorNavLeftList = new List<IMyMotorStator>();
            List<IMyMotorStator> rotorNavRightList = new List<IMyMotorStator>();

            Program _program;
            public NavRotors(Program program, WicoBlockMaster wicoBlockMaster)
            {
                _program = program;

                wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddResetMotionHandler(ResetMotionHandler);
            }

            void ResetMotionHandler(bool bNoDrills = false)
            {
                powerDownRotors();
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyMotorStator)
                {
                    var rotor = tb as IMyMotorStator;
                    rotorNavList.Add(rotor);
                    if (rotor.CustomName.Contains("[LEFT]") || rotor.CustomData.Contains("[LEFT]"))
                    {
                        rotorNavLeftList.Add(rotor);
                    }
                    else if (rotor.CustomName.Contains("[RIGHT]") || rotor.CustomData.Contains("[RIGHT]"))
                    {
                        rotorNavRightList.Add(rotor);
                    }
                }
            }
            void LocalGridChangedHandler()
            {
                rotorNavList.Clear();
                rotorNavLeftList.Clear();
                rotorNavRightList.Clear();
            }

            public int NavRotorCount()
            {
                return rotorNavLeftList.Count + rotorNavRightList.Count;
            }
            public bool powerUpRotors(float targetPower) // move forward
            {
                if (rotorNavLeftList.Count < 1) return false;
                // need to ramp up/down rotor power or they will flip small vehicles and spin a lot

                float maxVelocity = rotorNavLeftList[0].GetMaximum<float>("Velocity");

                _program.Echo("MaxVel=" + maxVelocity + " Targetpower=" + targetPower);

                var rotor = rotorNavLeftList[0] as IMyMotorStator;
                float currentVelocity = rotor.TargetVelocityRPM;
                _program.Echo("Current Velocity=" + currentVelocity);

                float cPower = (currentVelocity / maxVelocity * 100);
                cPower = Math.Abs(cPower);
                /*
                if (targetPower > (cPower + 5f))
                    targetPower = cPower + 5;
                if (targetPower < (cPower - 5))
                    targetPower = cPower - 5;
                    */
                targetPower = maxVelocity * 100 / targetPower;
                if (targetPower < 0f) targetPower = 0f;
                if (targetPower > 100f) targetPower = 100f;
                _program.Echo("cPower=" + cPower + " targetPower=" + targetPower);
                if (Math.Abs(targetPower) > 0)
                {
                    powerUpRotors(rotorNavLeftList, -(targetPower*maxVelocity/100));
                    powerUpRotors(rotorNavRightList, (targetPower * maxVelocity / 100));
                    _program.Echo("Set value=" + rotor.TargetVelocityRPM);
                    return true;
                }
                else return false;
            }
            public bool powerUpRotors(List<IMyMotorStator> rotorList, float targetPower) // power is 0 to 100
            {
                for (int i = 0; i < rotorList.Count; i++)
                {
                    var rotor = rotorList[i] as IMyMotorStator;
                    float maxVelocity = rotor.GetMaximum<float>("Velocity");
                    if (!rotor.Enabled) rotor.Enabled = true;
                    float targetVelocity = maxVelocity * (targetPower / 100.0f);
                    //                Echo(rotor.CustomName + ":MV=" + maxVelocity.ToString("0.00") + ":V=" + targetVelocity.ToString("0.00"));
                    rotor.TargetVelocityRPM = targetVelocity;
                }

                return true;
            }
            public bool powerDownRotors()
            {
                powerDownRotors(rotorNavLeftList);
                powerDownRotors(rotorNavRightList);
                return true;
            }
            public bool powerDownRotors(List<IMyMotorStator> rotorList)
            {
                for (int i = 0; i < rotorList.Count; i++)
                {
                    IMyMotorStator rotor = rotorList[i] as IMyMotorStator;
                    rotor.TargetVelocityRPM = 0;
                }
                return true;
            }
            public bool DoRotorRotate(double yawAngle)
            {
                //            Echo("DRR:" + yawAngle.ToString());
                float targetPower;
                if (Math.Abs(yawAngle) > 1.0)
                {
                    targetPower = 50;
                }
                else if (Math.Abs(yawAngle) > .7)
                {
                    targetPower = 50;
                }
                else if (Math.Abs(yawAngle) > 0.5)
                {
                    targetPower = 30;
                }
                else if (Math.Abs(yawAngle) > 0.1)
                {
                    targetPower = 20;
                }
                else if (Math.Abs(yawAngle) > 0.01)
                {
                    targetPower = 5;
                }
                else if (Math.Abs(yawAngle) > 0.001)
                {
                    targetPower = 0;
                }
                else targetPower = 0;

                targetPower /= 3; // reduce power

                targetPower = targetPower * -Math.Sign(yawAngle);

                if (Math.Abs(targetPower) > 0)
                {
                    //                Echo("PUPLEFT:" + targetPower.ToString());
                    powerUpRotors(rotorNavLeftList, targetPower);
                }
                if (Math.Abs(targetPower) > 0)
                {
                    //                Echo("PUPRIGHT:" + targetPower.ToString());
                    powerUpRotors(rotorNavRightList, targetPower);
                }
                if (Math.Abs(targetPower) > 0)
                    return false;
                else
                    return true;
            }

        }
    }
}
