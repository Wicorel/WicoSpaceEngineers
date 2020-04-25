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
        public class CargoCheck
        {
            public int cargopctmin = 5;
            public int cargopcent = -1;
            double cargoMult = -1;
            double totalCurrentVolume = 0.0; // volume
            bool bCreative = false;

            public List<IMyTerminalBlock> lContainers = new List<IMyTerminalBlock>();

            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            Displays _displays;

            string sCargoSection = "CARGO";
            public CargoCheck(Program program, WicoBlockMaster wbm, Displays displays)
            {
                _program = program;
                _wicoBlockMaster = wbm;
                _displays = displays;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                cargopctmin = _program._CustomDataIni.Get(sCargoSection, "cargopctmin").ToInt32(cargopctmin);
                _program._CustomDataIni.Set(sCargoSection, "cargopctmin", cargopctmin);

                if(_displays!=null) _displays.AddSurfaceHandler("CARGOCHECK", SurfaceHandler);
            }

            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "CARGOCHECK")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        tsurface.WriteText(sbModeInfo);
                        if (tsurface.SurfaceSize.Y < 512)
                        { // small/corner LCD

                        }
                        else
                        {
                            tsurface.WriteText(sbNotices, true);
                        }

                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 512)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 2;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.5f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
            }
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyCargoContainer)
                    lContainers.Add(tb);
                else if (tb is IMyShipDrill)
                    lContainers.Add(tb);
                else if (tb is IMyShipWelder)
                    lContainers.Add(tb);
                else if (tb is IMyShipGrinder)
                    lContainers.Add(tb);
                else if(tb is IMyShipConnector)
                {
                    if (tb.BlockDefinition.SubtypeName == "ConnectorSmall)")
                    { // don't add ejectors.
                        return;
                    }
                    else
                    {
                        lContainers.Add(tb);
                    }
                }
            }

            void LocalGridChangedHandler()
            {
                lContainers.Clear();
                cargopcent = -1;
                cargoMult = -1;
            }

            void LoadHandler(MyIni Ini)
            {
//                cargopctmin = Ini.Get(sCargoSection, "cargopctmin").ToInt32(cargopctmin);
            }

            void SaveHandler(MyIni Ini)
            {
//                Ini.Set(sCargoSection, "cargopctmin", cargopctmin);
            }



            //
            // Start of custom routines
            public void doCargoCheck()
            {

                sbNotices.Clear();
                sbModeInfo.Clear();
                if (lContainers.Count < 1)
                {
                    // No cargo containers found.
                    sbModeInfo.AppendLine("No Cargo Containers Found");
                    cargopcent = -1;
                    cargoMult = -1;
                    return;
                }
                totalCurrentVolume = 0.0;
                double totalMax = 0.0;
                double ratio = 0;

                bool bCargoFull = true;
                bool bDrillFull = false;
                bool bHasDrills = false;

                sbNotices.AppendLine(lContainers.Count+ " Cargo Containers");
                for (int i = 0; i < lContainers.Count; i++)
                {
                    if ((lContainers[i] is IMyShipDrill))
                    {
                        bHasDrills = true;
                    }
                    //                totalMax += cargoCapacity(lContainers[i]);
                    double capacity = -1;

                    var count = lContainers[i].InventoryCount;
                    for (var invcount = 0; invcount < count; invcount++)
                    {
                        var inv = lContainers[i].GetInventory(invcount);

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
                                double dCapacity = defaultCapacity(lContainers[i]);
                                if (dCapacity > 0) cargoMult = capacity / dCapacity;
                                //					Echo("lContainers="+theContainer.DefinitionDisplayNameText+"'"+inv.MaxVolume.ToString());
                            }
                            else
                            {
                                capacity = defaultCapacity(lContainers[i]) * 10;
                                cargoMult = 9999;
                            }

                            if ((double)inv.CurrentVolume < capacity)
                            {
                                // there is room
                                if (!(lContainers[i] is IMyShipDrill))
                                {
                                    bCargoFull = false;
                                }

                            }

                            else //if ((double)inv.CurrentVolume + 5 > capacity)
                            {
                                // we are full
                                if (lContainers[i] is IMyShipDrill)
                                {
                                    bDrillFull = true;
                                }
                            }

                        }
                        totalMax += capacity;
                    }
                    //	Echo("cargoCapacity=" + capacity.ToString());

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
                sbModeInfo.AppendLine(cargopcent + "% full");
                sbNotices.AppendLine("Cargo Full=" + bCargoFull);
                if(bHasDrills) sbNotices.AppendLine("Drills Full=" + bDrillFull);

                // if any drill is full and ALL cargo are full, call it 100%
                if (bCargoFull && bDrillFull)
                    cargopcent = 101;
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
                    //Echo("Unknown cargo for default Capacity:" + theContainer.DefinitionDisplayNameText + ":" + theContainer.BlockDefinition.SubtypeId);
                    capacity = 12;
                }
                return capacity;

            }

        }
    }
}
