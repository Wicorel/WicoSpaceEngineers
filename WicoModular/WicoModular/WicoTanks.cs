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
        class GasTanks
        {
            List<IMyTerminalBlock> tankList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> oxytankList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> hydrotankList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> isolatedoxytankList = new List<IMyTerminalBlock>();

            Program thisProgram;
            public GasTanks(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyGasTank)
                {
                    // TODO: Ignore cutters, etc
                    tankList.Add(tb);
                    if (TankType(tb) == iTankOxygen)
                    {
                        if (tb.CustomName.ToLower().Contains("isolated"))
                            isolatedoxytankList.Add(tb);
                        else
                            oxytankList.Add(tb);

                        //                            iOxygenTanks++;
                    }
                    else if (TankType(tb) == iTankHydro)
                    {
                        hydrotankList.Add(tb);
                        //                            iHydroTanks++;
                    }
                }
            }

            void LocalGridChangedHandler()
            {
                tankList.Clear();
                isolatedoxytankList.Clear();
                oxytankList.Clear();
                hydrotankList.Clear();
            }

            public double TanksFill(List<IMyTerminalBlock> tankList)
            {
                double totalPercent = 0;
                int iTanksCount = 0;
                for (int i = 0; i < tankList.Count; ++i)
                {
                    //		int iTankType = tankType(tankList[i]); 
                    //		if ((iTankType & iTypes) > 0)  
                    {
                        IMyGasTank tank = tankList[i] as IMyGasTank;
                        if (tank == null) continue; // not a tank
                        float tankLevel = (float)tank.FilledRatio;
                        totalPercent += tankLevel;
                        iTanksCount++;
                    }
                }
                if (iTanksCount > 0)
                {
                    return totalPercent * 100 / iTanksCount;
                }
                else return 0;
            }
            public double tanksFill(int iTypes = 0xff)
            {
//                if (tankList.Count < 1) tanksInit();
                if (tankList.Count < 1) return -1;

                double totalLevel = 0;
                int iTanksCount = 0;
                for (int i = 0; i < tankList.Count; ++i)
                {
                    int iTankType = TankType(tankList[i]);
                    if ((iTankType & iTypes) > 0)
                    {
                        IMyGasTank tank = tankList[i] as IMyGasTank;
                        if (tank == null) continue; // not a tank
                        float tankLevel = (float)tank.FilledRatio;
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
            const int iTankOxygen = 1;
            const int iTankHydro = 2;
            int TankType(IMyTerminalBlock theBlock)
            {

                /*
                var sink = block.Components.Get<MyResourceSinkComponent>();
                bool oxygen = sink.AcceptedResources.Any(r => r.SubtypeName == "Oxygen");
                */
                if (theBlock is IMyGasTank)
                {
                    // could also check the provider type...

                    if (theBlock.BlockDefinition.SubtypeId.Contains("Hydro"))
                        return iTankHydro;
                    else return iTankOxygen;
                }
                return 0;
            }
            public void TanksStockpile(bool bStockPile = true, int iTypes = 0xff)
            {
//                if (tankList.Count < 1) tanksInit();
                if (tankList.Count < 1) return;

                for (int i = 0; i < tankList.Count; ++i)
                {
                    int iTankType = TankType(tankList[i]);
                    if ((iTankType & iTypes) > 0)
                    {
                        IMyGasTank tank = tankList[i] as IMyGasTank;
                        if (tank == null) continue; // not a tank
                        tank.Stockpile = bStockPile;
                    }
                }

            }

        }
    }
}
