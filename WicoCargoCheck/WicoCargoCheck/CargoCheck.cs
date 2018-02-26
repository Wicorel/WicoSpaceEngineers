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

        int cargopctmin = 5;
        int cargopcent = -1;
        double cargoMult = -1;


        // 1212018 Reduce common serialize to minimum

        // 1/24: SE 1.172

        string sCargoSection = "CARGO";
        void CargoInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sCargoSection, "cargopctmin", ref cargopctmin, true);
        }

        List<IMyTerminalBlock> lContainers = null;
        //List < IMyTerminalBlock > lDrills = new List < IMyTerminalBlock > ();

        bool bCreative = false;

        double totalCurrentVolume= 0.0; // volume

        void CargoCheckInit()
        {
            var blocks = new List<IMyTerminalBlock>();

            if (lContainers == null) lContainers = new List<IMyTerminalBlock>();
            else lContainers.Clear();

            //            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(grid, localGridFilter);
            GetTargetBlocks<IMyCargoContainer>(ref blocks);

            lContainers.AddRange(blocks);
            cargopcent = -1;
            cargoMult = -1;

        }

        void CargoCheckAddConnectors()
        {
            var blocks = new List<IMyTerminalBlock>();
            //            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(grid, localGridFilter);
            GetTargetBlocks<IMyShipConnector>(ref blocks);
            foreach (var c in blocks)
            { // don't count ejectors
                if (c.CustomName.Contains("Ejector") || c.CustomData.Contains("Ejector"))
                    continue;
                else
                    lContainers.Add(c);
            }

        }

        void CargoCheckAddDrills()
        {
            var blocks = new List<IMyTerminalBlock>();
            //            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(grid, localGridFilter);
            GetTargetBlocks<IMyShipDrill>(ref blocks);
            lContainers.AddRange(blocks);
        }
        void CargoCheckAddWelders()
        {
            var blocks = new List<IMyTerminalBlock>();
            //            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(grid, localGridFilter);
            GetTargetBlocks<IMyShipWelder>(ref blocks);
            lContainers.AddRange(blocks);
        }
        void CargoCheckAddGrinders()
        {
            var blocks = new List<IMyTerminalBlock>();
            //            GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grid, localGridFilter);
            GetTargetBlocks<IMyShipGrinder>(ref blocks);
            lContainers.AddRange(blocks);
        }

        bool bCargoCheckCached = true;

        void initCargoCheck()
        {
            var blocks = new List<IMyTerminalBlock>();

            if (lContainers == null) lContainers = new List<IMyTerminalBlock>();
            else lContainers.Clear();

            if(!bCargoCheckCached)
                GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(blocks, localGridFilter);
            else
               GetTargetBlocks<IMyCargoContainer>(ref blocks);

            lContainers.AddRange(blocks);

            blocks.Clear();

            if (!bCargoCheckCached)
                GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks, localGridFilter);
            else
                GetTargetBlocks<IMyShipConnector>(ref blocks);

            foreach (var c in blocks)
            { // don't count ejectors
                if (c.CustomName.Contains("Ejector") || c.CustomData.Contains("Ejector"))
                    continue;
                else
                    lContainers.Add(c);
            }
//            lContainers.AddRange(grid);

            blocks.Clear();
            if (!bCargoCheckCached)
                GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(blocks, localGridFilter);
            else
                GetTargetBlocks<IMyShipDrill>(ref blocks);
            lContainers.AddRange(blocks);

            blocks.Clear();
            if (!bCargoCheckCached)
                GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(blocks, localGridFilter);
            else
            GetTargetBlocks<IMyShipWelder>(ref blocks);

            lContainers.AddRange(blocks);

            blocks.Clear();
            if (!bCargoCheckCached)
                GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(blocks, localGridFilter);
            else
                GetTargetBlocks<IMyShipGrinder>(ref blocks);
            lContainers.AddRange(blocks);

            cargopcent = -1;
            cargoMult = -1;
        }

        void doCargoCheck()
        {
            if (lContainers == null)
                initCargoCheck();

            if (lContainers.Count < 1)
            {
                // No cargo containers found.
                cargopcent = -1;
                cargoMult = -1;
                return;
            }
            totalCurrentVolume = 0.0;
            double totalMax = 0.0;
            double ratio = 0;

            for (int i = 0; i < lContainers.Count; i++)
            {
                totalMax += cargoCapacity(lContainers[i]);
            }
            //	Echo("totalMax=" + totalMax.ToString("0.00"));
            if (totalMax > 0)
            {
                ratio = (totalCurrentVolume / totalMax) * 100;
            }
            else
            {
                ratio = 100;
            }
            //Echo("ratio="+ratio.ToString());
            cargopcent = (int)ratio;

        }

        double cargoCapacity(IMyTerminalBlock theContainer)
        {
            double capacity = -1;

            var count = theContainer.InventoryCount;
            for (var invcount = 0; invcount < count; invcount++)
            {
                var inv = theContainer.GetInventory(invcount);

                if (inv != null) // null means, no items in inventory.
                {
                    totalCurrentVolume += (double)inv.CurrentVolume;

                    if ((double)inv.MaxVolume > 9223372036854)
                    {
                        bCreative = true;
                    }
                    else
                    {
                        bCreative = false;
                    }

                    if (!bCreative)
                    {
                        //Echo("NCreateive");
                        capacity = (double)inv.MaxVolume;
                        double dCapacity = defaultCapacity(theContainer);
                        if (dCapacity > 0) cargoMult = capacity / dCapacity;
                        //					Echo("lContainers="+theContainer.DefinitionDisplayNameText+"'"+inv.MaxVolume.ToString());
                    }
                    else
                    {
                        capacity = defaultCapacity(theContainer) * 10;
                        cargoMult = 9999;
                    }
                }
            }
            //	Echo("cargoCapacity=" + capacity.ToString());
            return capacity;
        }

        double defaultCapacity(IMyTerminalBlock theContainer)
        {
            var inv = theContainer.GetInventory(0);

            string subtype = theContainer.BlockDefinition.SubtypeId;

            double capacity = (double)inv.MaxVolume;

            //Echo("name=" + theContainer.DefinitionDisplayNameText + "\'"+ subtype +"'\n" + "maxvol="+capacity.ToString());

            if (capacity < 999999999) return capacity;

            // else creative; use default 1x capacity
            if (theContainer is IMyCargoContainer)
            {
                // Keen Large Block
                if (subtype.Contains("LargeBlockLargeContainer")) capacity = 421.875008;
                else if (subtype.Contains("LargeBlockSmallContainer")) capacity = 15.625;

                // Keen Small Block
                else if (subtype.Contains("SmallBlockLargeContainer")) capacity = 15.625;
                else if (subtype.Contains("SmallBlockMediumContainer")) capacity = 3.375;
                else if (subtype.Contains("SmallBlockSmallContainer")) capacity = 0.125;

                // Azimuth Large Grid
                else if (subtype.Contains("Azimuth_LargeContainer")) capacity = 7780.8;
                else if (subtype.Contains("Azimuth_MediumLargeContainer")) capacity = 1945.2;

                // Azimuth Small Grid
                else if (subtype.Contains("Azimuth_MediumContainer")) capacity = 1878.6;
                else if (subtype.Contains("Azimuth_SmallContainer")) capacity = 10.125;
            }
            else if (subtype.Contains("SmallBlockDrill")) capacity = 3.375;
            else if (subtype.Contains("LargeBlockDrill")) capacity = 23.4375;
            else if (subtype.Contains("ConnectorMedium")) capacity = 1.152; // sg connector
            else if (subtype.Contains("ConnectorSmall")) capacity = 0.064; // sg ejector
            else if (subtype.Contains("Connector")) capacity = 8.000; // lg connector
            else if (subtype.Contains("LargeShipWelder")) capacity = 15.625;
            else if (subtype.Contains("LargeShipGrinder")) capacity = 15.625;
            else if (subtype.Contains("SmallShipWelder")) capacity = 3.375;
            else if (subtype.Contains("SmallShipGrinder")) capacity = 3.375;
            else
            {
                Echo("Unknown cargo for default Capacity:" + theContainer.DefinitionDisplayNameText + ":" + theContainer.BlockDefinition.SubtypeId);
                capacity = 12;
            }
            return capacity;

        }

    }
}