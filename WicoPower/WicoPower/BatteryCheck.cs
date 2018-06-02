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
        double maxBatteryPower = -1;
        int batteryPercentage = -1;

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
 //               float output = 0;
                IMyBatteryBlock r = tb as IMyBatteryBlock;

                // 1.185
                maxBatteryPower += r.MaxOutput;
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

        /*
         * This routine goes through the batteries and turns the lowest to recharge and the others to discharge
         * 
         * This is to prevent overloading power on the mother ship
         */
//        bool batteryCheck(int targetMax, bool bEcho = true, IMyTextPanel textBlock = null, bool bProgress = false)
        bool batteryCheck(int targetMax, bool bEcho = true, bool bProgress = false)
        {
            float totalCapacity = 0;
            float totalCharge = 0;
            bool bFoundRecharging = false;
            float f1;
//            Echo("BC():" + batteryList.Count + " batteries");

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
                f1 = b.MaxStoredPower;
                capacity += f1;
                totalCapacity += f1;
                f1 = b.CurrentStoredPower;
                charge += f1;
                totalCharge += f1;
                if (capacity > 0)
                {
                    f1 = ((charge * 100) / capacity);
                    f1 = (float)Math.Round(f1, 0);
                    percentthisbattery = (int)f1;
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
                /*
                if (textBlock != null)
                {
                    StatusLog(s, textBlock);
                    if (bProgress)
                    {
                        s = progressBar(percentthisbattery);
                        StatusLog(s, textBlock);
                    }
                }
                */
                if (isRechargeSet(batteryList[ib]) && targetMax>0)
                {
                    if (percentthisbattery < targetMax)
                        bFoundRecharging = true;
                    else if (percentthisbattery > 99)
                        b.OnlyRecharge = false;
                }
                if (!b.OnlyRecharge && percentthisbattery < targetMax && !bFoundRecharging)
                {
//                    Echo("Turning on Recharge for " + b.CustomName);
                    b.OnlyDischarge = false;
                    b.OnlyRecharge = true;
                    b.SemiautoEnabled = false;
                    bFoundRecharging = true;
                }
            }
            if (totalCapacity > 0)
            {
                f1 = ((totalCharge * 100) / totalCapacity);
                f1 = (float)Math.Round(f1, 0);
                batteryPercentage = (int)f1;
            }
            else
                batteryPercentage = -1;
            return bFoundRecharging;
        }

        void BatterySetNormal()
        {
            for (int i = 0; i < batteryList.Count; i++)
            {
                IMyBatteryBlock b;
                b = batteryList[i] as IMyBatteryBlock;
                b.OnlyRecharge = false;
                b.OnlyDischarge = false;
                b.SemiautoEnabled = false;
            }
        }
        // Set the state of the batteries and optionally display state of the batteries
        void batteryDischargeSet(bool bEcho = false, bool bDischarge=true)
        {
            if(bEcho)  Echo(batteryList.Count + " Batteries");
            string s;
            for (int i = 0; i < batteryList.Count; i++)
            {
                IMyBatteryBlock b;
                b = batteryList[i] as IMyBatteryBlock;
                b.OnlyRecharge = !bDischarge;
                b.OnlyDischarge = bDischarge;
                b.SemiautoEnabled = false;

                s = b.CustomName + ": ";
                if (b.OnlyRecharge)
                {
                    s += "RECHARGE/";
                }
                else s += "NOTRECHARGE/";
                if (b.OnlyDischarge)
                {
                    s += "DISCHARGE";
                }
                else
                {
                    s += "NOTDISCHARGE";
                }
                if (bEcho) Echo(s);
            }
        }

    }
}