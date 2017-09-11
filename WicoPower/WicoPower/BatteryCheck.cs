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
        #region batterycheck

        List<IMyTerminalBlock> batteryList = new List<IMyTerminalBlock>();

        bool isRechargeSet(IMyTerminalBlock block)
        {
            if (block is IMyBatteryBlock)
            {
                IMyBatteryBlock myb = block as IMyBatteryBlock;
                return myb.OnlyRecharge;// myb.GetValueBool("Recharge");
            }
            else return false;
        }
        bool isDischargeSet(IMyTerminalBlock block)
        {
            if (block is IMyBatteryBlock)
            {
                IMyBatteryBlock myb = block as IMyBatteryBlock;
                return myb.OnlyDischarge;// GetValueBool("Discharge");
            }
            else return false;
        }
        bool isRecharging(IMyTerminalBlock block)
        {
            if (block is IMyBatteryBlock)
            {
                IMyBatteryBlock myb = block as IMyBatteryBlock;
                return myb.IsCharging;// PowerProducer.IsRecharging(myb);
            }
            else return false;
        }
        void initBatteries()
        {
            batteryList.Clear();
            batteryPercentage = -1;
            maxBatteryPower = -1;
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryList, localGridFilter);
            if (batteryList.Count > 0)
                maxBatteryPower = 0;
            foreach (var tb in batteryList)
            {
                float output = 0;
                IMyBatteryBlock r = tb as IMyBatteryBlock;

                PowerProducer.GetMaxOutput(r, out output);
                //		output = r.MaxOutput;

                maxBatteryPower += output;
            }
        }

        double getCurrentBatteryOutput()
        {
            double output = 0;
            foreach (var tb in batteryList)
            {
                IMyBatteryBlock r = tb as IMyBatteryBlock;
                output += r.CurrentOutput;
            }
            return output;
        }

        bool batteryCheck(int targetMax, bool bEcho = true, IMyTextPanel textBlock = null, bool bProgress = false)
        {
            float totalCapacity = 0;
            float totalCharge = 0;
            bool bFoundRecharging = false;
            float f;

            if (batteryList.Count < 1) initBatteries();
            if (batteryList.Count < 1) return false;

            batteryPercentage = 0;
            for (int ib = 0; ib < batteryList.Count; ib++)
            {
                float charge = 0;
                float capacity = 0;
                int percentthisbattery = 100;
                IMyBatteryBlock b;
                b = batteryList[ib] as IMyBatteryBlock;
                f = b.MaxStoredPower;
                capacity += f;
                totalCapacity += f;
                f = b.CurrentStoredPower;
                charge += f;
                totalCharge += f;
                if (capacity > 0)
                {
                    f = ((charge * 100) / capacity);
                    f = (float)Math.Round(f, 0);
                    percentthisbattery = (int)f;
                }
                string s;
                s = "";
                if (isRechargeSet(batteryList[ib])) s += "R";
                else if (isDischargeSet(batteryList[ib])) s += "D";
                else s += "a";
                float fPower;
                fPower = b.CurrentInput;
                if (fPower > 0)
                    s += "+";
                else s += " ";
                fPower = b.CurrentOutput;
                if (fPower > 0)
                    s += "-";
                else s += " ";
                s += percentthisbattery + "%";
                s += ":" + batteryList[ib].CustomName;
                if (bEcho) Echo(s);
                if (textBlock != null) StatusLog(s, textBlock);
                if (bProgress)
                {
                    s = progressBar(percentthisbattery);
                    if (textBlock != null) StatusLog(s, textBlock);
                }
                if (isRechargeSet(batteryList[ib]))
                {
                    if (percentthisbattery < targetMax)
                        bFoundRecharging = true;
                    else if (percentthisbattery > 99)
                        batteryList[ib].ApplyAction("Recharge");
                }
                if (!isRechargeSet(batteryList[ib]) && percentthisbattery < targetMax && !bFoundRecharging)
                {
                    Echo("Turning on Recharge for " + batteryList[ib].CustomName);
                    batteryList[ib].ApplyAction("Recharge");
                    bFoundRecharging = true;
                }
            }
            if (totalCapacity > 0)
            {
                f = ((totalCharge * 100) / totalCapacity);
                f = (float)Math.Round(f, 0);
                batteryPercentage = (int)f;
            }
            else
                batteryPercentage = -1;
            return bFoundRecharging;
        }
        void batteryDischargeSet(bool bEcho = false)
        {
            Echo(batteryList.Count + " Batteries");
            for (int i = 0; i < batteryList.Count; i++)
            {
                string s = batteryList[i].CustomName + ": ";
                if (isRechargeSet(batteryList[i]))
                {
                    s += "RECHARGE/";
                    batteryList[i].ApplyAction("Recharge");
                }
                else s += "NOTRECHARGE/";
                if (isDischargeSet(batteryList[i]))
                {
                    s += "DISCHARGE";
                }
                else
                {
                    s += "NOTDISCHARGE";
                    batteryList[i].ApplyAction("Discharge");
                }
                if (bEcho) Echo(s);
            }
        }
        #endregion

    }
}