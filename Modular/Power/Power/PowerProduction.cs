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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PowerProduction
        {
            List<IMyTerminalBlock> lHydrogenEngines = new List<IMyTerminalBlock>();
            public double maxHydrogenPower = -1;
            public float currentEngineOutput = -1;

            List<IMyTerminalBlock> reactorList = new List<IMyTerminalBlock>();
            public double maxReactorPower = -1;
            public float currentReactorOutput = -1;

            List<IMyTerminalBlock> solarList = new List<IMyTerminalBlock>();
            public double maxSolarPower = -1;
            public float currentSolarOutput = 0;


            List<IMyTerminalBlock> batteryList = new List<IMyTerminalBlock>();
            public double maxBatteryPower = -1;
            /// <summary>
            /// current battery STORAGE power. -1 means no batteries. percentage is 0->100
            /// </summary>
            public int batteryPercentage = -1;
            public double batteryTotalInput = 0;
            public double batteryTotalOutput = 0;

            public int batterypcthigh = 80;
            public int batterypctlow = 20;

            List<IMyTerminalBlock> turbineList = new List<IMyTerminalBlock>();
            public double maxTurbinePower = -1;
            public float currentTurbineOutput = 0;


            public double maxTotalPower = -1;
            public double currentTotalOutput = -1;


            string sPowerSection = "POWER";

            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            public PowerProduction(Program program, WicoBlockMaster wbm)
            {
                _program = program;
                _wicoBlockMaster = wbm;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);
                batterypcthigh = _program._CustomDataIni.Get(sPowerSection, "batterypcthigh").ToInt32(batterypcthigh);
                _program._CustomDataIni.Set(sPowerSection, "batterypcthigh", batterypcthigh);

                batterypctlow = _program._CustomDataIni.Get(sPowerSection, "batterypctlow").ToInt32(batterypctlow);
                _program._CustomDataIni.Set(sPowerSection, "batterypctlow", batterypctlow);

            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine")
                {
                    lHydrogenEngines.Add(tb);
                }
                if(tb is IMyReactor)
                {
                    reactorList.Add(tb);
                }
                if (tb is IMySolarPanel)
                {
                    solarList.Add(tb);
                }
                if (tb is IMyBatteryBlock)
                {
                    batteryList.Add(tb);
                }
                if (tb.BlockDefinition.TypeIdString == "MyObjectBuilder_WindTurbine")
                {
                    turbineList.Add(tb);
                }
            }

            void LocalGridChangedHandler()
            {
                lHydrogenEngines.Clear();
                maxHydrogenPower = -1;

                reactorList.Clear();
                maxReactorPower = -1;

                solarList.Clear();
                currentSolarOutput = 0;
                maxSolarPower = -1;

                batteryList.Clear();
                maxBatteryPower = -1;
                batteryPercentage = -1;
            }
            void LoadHandler(MyIni Ini)
            {
//                batterypcthigh = Ini.Get(sPowerSection, "batterypcthigh").ToInt32(batterypcthigh);
//                batterypctlow = Ini.Get(sPowerSection, "batterypctlow").ToInt32(batterypctlow);
            }

            void SaveHandler(MyIni Ini)
            {
//                Ini.Set(sPowerSection, "batterypcthigh", batterypcthigh);
//                Ini.Set(sPowerSection, "batterypctlow", batterypctlow);
            }

            /// <summary>
            /// gets current and max output and returns number of hydrogen engines
            /// </summary>
            /// <returns># of engines on ship</returns>
            public int CalcCurrentEngine()
            {
                currentEngineOutput = 0;
                maxHydrogenPower = 0;
                int count = 0;
                foreach (var tb in lHydrogenEngines)
                {
                    if (tb is IMyPowerProducer)
                    {
                        count++;
                        var pp = tb as IMyPowerProducer;

                        currentEngineOutput += pp.CurrentOutput;
                        maxHydrogenPower += pp.MaxOutput;
                    }
                }
                return count;
            }
            public int EnginesCount()
            {
                return lHydrogenEngines.Count;
            }

            public double EnginesTanksFill()
            {
                double totalLevel = 0;
                int iTanksCount = 0;

                foreach (var tb in lHydrogenEngines)
                {
                    if (tb.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine")
                    {
                        double tankLevel = 0;
                        string[] lines = tb.DetailedInfo.Trim().Split('\n');
                        if (lines.Length < 3) // not what we expected
                            continue;
                        string[] aParams = lines[3].Split(' ');
                        if (aParams.Length < 2) // not what we expected
                            continue;
                        string sPercent = aParams[1].Replace('%', ' ');
                        bool bOK = double.TryParse(sPercent.Trim(), out tankLevel);
                        tankLevel /= 100.0; // convert from 0->100 to 0->1.0
                        totalLevel += tankLevel;
                        iTanksCount++;
                    }
                }
                if (iTanksCount > 0)
                {
                    return totalLevel / iTanksCount;
                }
                else return -1;
            }

            void EngineControl(bool bOn=true)
            {
                foreach(var tb in lHydrogenEngines)
                {
                    if(tb is IMyFunctionalBlock)
                    {
                        var fb = tb as IMyFunctionalBlock;
                        fb.Enabled = bOn;
                    }
                }
            }

            public bool reactorCheck(out float currentOutput)
            {
                currentOutput = 0;
                maxReactorPower = -1;
                bool bNeedyReactor = false;
                if (reactorList.Count > 0)
                    maxReactorPower = 0;

                foreach (IMyReactor r in reactorList)
                {
                    // check inventory.
                    // check power modes

                    // if reactor is working, add up its values.
                    currentOutput += r.CurrentOutput;
                    maxReactorPower += r.MaxOutput;

                }
                return bNeedyReactor;
            }

            void calcCurrentSolar()
            {
                if (solarList.Count > 0)
                    maxSolarPower = 0;

                currentSolarOutput = 0;

                //	Echo("Solars:");
                foreach (var tb in solarList)
                {
                    IMySolarPanel r = tb as IMySolarPanel;
                    //		Echo(r.CustomName + " Max=" + r.MaxOutput.ToString("0.000") + " c=" + r.CurrentOutput.ToString("0.000"));
                    maxSolarPower += r.MaxOutput;
                    currentSolarOutput += r.CurrentOutput;
                }

            }
            void calcCurrentTurbine()
            {
                if (turbineList.Count > 0)
                    maxTurbinePower = 0; // leave at -1 if there are zero turbines available
                currentTurbineOutput = 0;

                foreach (var tb in turbineList)
                {
                    var pp= tb as IMyPowerProducer;
                    maxTurbinePower += pp.MaxOutput;
                    currentTurbineOutput += pp.CurrentOutput;
                }

            }
            bool isRechargeSet(IMyTerminalBlock block)
            {
                if (block is IMyBatteryBlock)
                {
                    IMyBatteryBlock myb = block as IMyBatteryBlock;
                    return (myb.ChargeMode == ChargeMode.Recharge);
                }
                else return false;
            }
            bool isDischargeSet(IMyTerminalBlock block)
            {
                if (block is IMyBatteryBlock)
                {
                    IMyBatteryBlock myb = block as IMyBatteryBlock;
                    return (myb.ChargeMode == ChargeMode.Discharge);
                }
                else return false;
            }
            /// <summary>
            /// This routine goes through the batteries and turns the lowest to recharge and the others to discharge
            /// This is to prevent overloading power on the mother ship
            /// </summary>
            /// <param name="targetMax">target max charge %.  0-100</param>
            /// <param name="bEcho">should the results be echoed</param>
            /// <param name="bProgress"></param>
            /// <returns></returns>
            public bool BatteryCheck(int targetMax, bool bEcho = true, bool bProgress = false)
            {
                float totalCapacity = 0;
                float totalCharge = 0;
                bool bFoundRecharging = false;
                float f1;
                //            Echo("BC():" + batteryList.Count + " batteries");

                if (batteryList.Count < 1) return false;

                batteryPercentage = 0;
                batteryTotalInput = 0;
                batteryTotalOutput = 0;
                maxBatteryPower = -1;

                for (int ib = 0; ib < batteryList.Count; ib++)
                {
                    float charge = 0;
                    float capacity = 0;
                    int percentthisbattery = 100;
                    IMyBatteryBlock b;

                    b = batteryList[ib] as IMyBatteryBlock;

                    maxBatteryPower += b.MaxOutput;
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
                    batteryTotalInput += fPower;
                    if (fPower > 0)
                        s += "+";
                    else s += " ";
                    fPower = b.CurrentOutput;
                    batteryTotalOutput += fPower;
                    if (fPower > 0)
                        s += "-";
                    else s += " ";
                    s += percentthisbattery + "%";
                    s += ":" + batteryList[ib].CustomName;
                    if (bEcho) _program.Echo(s);
                    if (isRechargeSet(batteryList[ib]) && targetMax > 0)
                    {
                        if (percentthisbattery < targetMax)
                            bFoundRecharging = true;
                        else if (percentthisbattery > 99)
                        {
                            b.ChargeMode = ChargeMode.Recharge;
                        }
                    }
                    if (!(b.ChargeMode == ChargeMode.Recharge) && percentthisbattery < targetMax && !bFoundRecharging)
                    {
                        //                    Echo("Turning on Recharge for " + b.CustomName);
                        b.ChargeMode = ChargeMode.Recharge;
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
            public void BatterySetNormal()
            {
                for (int i = 0; i < batteryList.Count; i++)
                {
                    IMyBatteryBlock b;
                    b = batteryList[i] as IMyBatteryBlock;

                    b.ChargeMode = ChargeMode.Auto;
                }
            }
            // Set the state of the batteries and optionally display state of the batteries
            public void BatteryDischargeSet(bool bEcho = false, bool bDischarge = true)
            {
                if (bEcho) _program.Echo(batteryList.Count + " Batteries");
                string s;
                for (int i = 0; i < batteryList.Count; i++)
                {
                    IMyBatteryBlock b;
                    b = batteryList[i] as IMyBatteryBlock;

                    if (bDischarge)
                    {
                        b.ChargeMode = ChargeMode.Discharge;
                    }
                    else b.ChargeMode = ChargeMode.Recharge;
                    s = b.CustomName + ": ";


                    if (b.ChargeMode == ChargeMode.Recharge)
                    {
                        s += "RECHARGE/";
                    }
                    else s += "NOTRECHARGE/";
                    if (b.ChargeMode == ChargeMode.Discharge)
                    {
                        s += "DISCHARGE";
                    }
                    else
                    {
                        s += "NOTDISCHARGE";
                    }
                    if (bEcho) _program.Echo(s);
                }
            }

            public void CalcPower()
            {
                calcCurrentSolar();
                reactorCheck(out currentReactorOutput);
                CalcCurrentEngine();
                BatteryCheck(0);
                calcCurrentTurbine();

                maxTotalPower = maxBatteryPower + maxHydrogenPower + maxReactorPower + maxSolarPower +maxTurbinePower;
                currentTotalOutput = batteryTotalOutput-batteryTotalInput+currentEngineOutput + currentReactorOutput + currentSolarOutput + currentTurbineOutput; 
            }

            void OptimizePower() // need a FOR....  for recharge.. for long-term.. for launch prep, minimize hydrogen usage, etc.
            {

            }

        }
    }
}
