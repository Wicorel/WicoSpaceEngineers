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
        // 1/24: SE 1.172
        #region cargocheck

        List<IMyTerminalBlock> lContainers = null;
        //List < IMyTerminalBlock > lDrills = new List < IMyTerminalBlock > ();

        bool bCreative = false;

        double totalCurrent = 0.0; // volume

        void initCargoCheck()
        {
            List<IMyTerminalBlock> grid = new List<IMyTerminalBlock>();

            if (lContainers == null) lContainers = new List<IMyTerminalBlock>();
            else lContainers.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(grid, localGridFilter);
                        lContainers.AddRange(grid);

            grid.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(grid, localGridFilter);
            // should probably eliminate ejectors..
            lContainers.AddRange(grid);

            grid.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(grid, localGridFilter);
            lContainers.AddRange(grid);

            grid.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(grid, localGridFilter);
            lContainers.AddRange(grid);

            grid.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grid, localGridFilter);
            lContainers.AddRange(grid);

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
            totalCurrent = 0.0;
            double totalMax = 0.0;
            double ratio = 0;

            for (int i = 0; i < lContainers.Count; i++)
            {
                totalMax += cargoCapacity(lContainers[i]);
            }
            //	Echo("totalMax=" + totalMax.ToString("0.00"));
            if (totalMax > 0)
            {
                ratio = (totalCurrent / totalMax) * 100;
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
                IMyInventory inv = theContainer.GetInventory(invcount);

                if (inv != null) // null means, no items in inventory.
                {
                    totalCurrent += (double)inv.CurrentVolume;

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
            IMyInventory inv = theContainer.GetInventory(0);

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
                Echo("Not cargo:" + theContainer.DefinitionDisplayNameText + ":" + theContainer.BlockDefinition.SubtypeId);
                capacity = 0.125;
            }
            return capacity;

        }


        #endregion


    }
}