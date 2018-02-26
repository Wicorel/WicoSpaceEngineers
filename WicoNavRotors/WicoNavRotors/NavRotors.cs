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

        #region navrotors
        List<IMyTerminalBlock> rotorNavList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> rotorNavLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> rotorNavRightList = new List<IMyTerminalBlock>();

        string rotorsNavInit()
        {
            rotorNavList.Clear();
            rotorNavLeftList.Clear();
            rotorNavRightList.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(rotorNavList, localGridFilter);

            for (int i = 0; i < rotorNavList.Count; i++)
            {
                if (rotorNavList[i].CustomName.Contains("[LEFT]") || rotorNavList[i].CustomData.Contains("[LEFT]"))
                {
                    rotorNavLeftList.Add(rotorNavList[i]);
                }
                else if (rotorNavList[i].CustomName.Contains("[RIGHT]") || rotorNavList[i].CustomData.Contains("[RIGHT]"))
                {
                    rotorNavRightList.Add(rotorNavList[i]);
                }
            }
            return "NR:L" + rotorNavLeftList.Count.ToString("0") + "R" + rotorNavRightList.Count.ToString("0");
        }

        bool powerUpRotors(float targetPower) // move forward
        {
            if (rotorNavLeftList.Count < 1) return false;
            // need to ramp up/down rotor power or they will flip small vehicles and spin a lot
            float maxVelocity = rotorNavLeftList[0].GetMaximum<float>("Velocity");
//            float currentVelocity = rotorNavLeftList[0].GetValueFloat("Velocity");
            var rotor = rotorNavLeftList[0] as IMyMotorStator;
            float currentVelocity = rotor.TargetVelocityRPM;

            float cPower = (currentVelocity / maxVelocity * 100);
            cPower = Math.Abs(cPower);
            if (targetPower > (cPower + 5f))
                targetPower = cPower + 5;
            if (targetPower < (cPower - 5))
                targetPower = cPower - 5;

            if (targetPower < 0f) targetPower = 0f;
            if (targetPower > 100f) targetPower = 100f;

            if (Math.Abs(targetPower) > 0)
            {
                powerUpRotors(rotorNavLeftList, -targetPower);
                powerUpRotors(rotorNavRightList, targetPower);
                return true;
            }
            else return false;
        }
        bool powerUpRotors(List<IMyTerminalBlock> rotorList, float targetPower) // power is 0 to 100
        {
            for (int i = 0; i < rotorList.Count; i++)
            {
                var rotor = rotorList[i] as IMyMotorStator;
                float maxVelocity = rotor.GetMaximum<float>("Velocity");
                if (!rotor.Enabled) rotor.Enabled = true;
                float targetVelocity = maxVelocity * (targetPower / 100.0f);
//                Echo(rotor.CustomName + ":MV=" + maxVelocity.ToString("0.00") + ":V=" + targetVelocity.ToString("0.00"));
                /*
                        float rv = rotor.TargetVelocity;
                        if (rv > maxVelocity) rv = maxVelocity;
                        if (rv < -maxVelocity) rv = -maxVelocity;
                        if(rv<(targetVelocity))
                        {
                            targetVelocity = rv + 5;
                        }
                        if(rv>targetVelocity)
                        {
                //            targetVelocity = rv - 5;
                        }
                        if (targetVelocity > maxVelocity) targetVelocity = maxVelocity;
                        if (targetVelocity < -maxVelocity) targetVelocity = -maxVelocity;
                 Echo("CurrentV:"+rv.ToString("0.00")+":ADJV=" + targetVelocity.ToString("0.00"));
                 */
//                rotor.TargetVelocity = targetVelocity;
                rotor.TargetVelocityRPM = targetVelocity;
            }

            return true;
        }
        bool powerDownRotors()
        {
            powerDownRotors(rotorNavLeftList);
            powerDownRotors(rotorNavRightList);
            return true;
        }
        bool powerDownRotors(List<IMyTerminalBlock> rotorList)
        {
            for (int i = 0; i < rotorList.Count; i++)
            {
                IMyMotorStator rotor = rotorList[i] as IMyMotorStator;
                rotor.TargetVelocityRPM = 0;
//                rotor.TargetVelocity = 0;
            }
            return true;
        }
        #endregion

        bool DoRotorRotate(double yawAngle)
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