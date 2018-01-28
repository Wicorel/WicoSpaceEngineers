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
        #region tanks
        List<IMyTerminalBlock> tankList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> oxytankList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> hydrotankList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> isolatedoxytankList = new List<IMyTerminalBlock>();


        const int iTankOxygen = 1;
        const int iTankHydro = 2;
        int iHydroTanks = 0;
        int iOxygenTanks = 0;

        double hydroPercent = -1;
        double oxyPercent = -1;

        void TanksCalculate()
        {
            hydroPercent = tanksFill(iTankHydro);
            oxyPercent = tanksFill(iTankOxygen);

        }
        string tanksInit()
        {
            {
                tankList = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tankList, (x => x.CubeGrid == Me.CubeGrid));
            }
            iHydroTanks = 0;
            iOxygenTanks = 0;
            for (int i = 0; i < tankList.Count; ++i)
            {
                if (tankType(tankList[i]) == iTankOxygen)
                {
                    if (tankList[i].CustomName.ToLower().Contains("isolated"))
                        isolatedoxytankList.Add(tankList[i]);
                    else
                        oxytankList.Add(tankList[i]);

                    iOxygenTanks++;
                }
                else if (tankType(tankList[i]) == iTankHydro)
                {
                    hydrotankList.Add(tankList[i]);

                    iHydroTanks++;
                }
            }
            return "T" + tankList.Count.ToString("00");
        }
        double tanksFill(List<IMyTerminalBlock> tankList)
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
        double tanksFill(int iTypes = 0xff)
        {
            if (tankList.Count < 1) tanksInit();
            if (tankList.Count < 1) return -1;

            double totalLevel = 0;
            int iTanksCount = 0;
            for (int i = 0; i < tankList.Count; ++i)
            {
                int iTankType = tankType(tankList[i]);
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

        // returns the 'type' of the tank
        int tankType(IMyTerminalBlock theBlock)
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

        void TanksStockpile(bool bStockPile = true,int iTypes = 0xff)
        {
            if (tankList.Count < 1) tanksInit();
            if (tankList.Count < 1) return;

            for (int i = 0; i < tankList.Count; ++i)
            {
                int iTankType = tankType(tankList[i]);
                if ((iTankType & iTypes) > 0)
                {
                    IMyGasTank tank = tankList[i] as IMyGasTank;
                    if (tank == null) continue; // not a tank
                    tank.Stockpile=bStockPile;
                }
            }

        }

        #endregion

    }
}