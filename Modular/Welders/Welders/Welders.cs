using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Welders
        {
            List<IMyShipWelder> welderList = new List<IMyShipWelder>();

            List<ManagedWelders> managedWelders = new List<ManagedWelders>();

            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            bool MeGridOnly = false;

            public  Welders(Program program, WicoBlockMaster wicoBlockMaster, bool bMeGridOnly=false)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;
                MeGridOnly = bMeGridOnly;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            MyIni blockIni = new MyIni();
            const string ProjectorSection = "PrintHeadControl";

            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (MeGridOnly
                    && !(tb.CubeGrid.EntityId == _program.Me.CubeGrid.EntityId))
                    return;

                if (tb is IMyShipWelder)
                {
//                    if (Local[i].CustomName.Contains("!WCC") || Local[i].CustomData.Contains("!WCC")) continue; // ignore
                    welderList.Add(tb as IMyShipWelder );

                    MyIniParseResult result;
                    if (!blockIni.TryParse(tb.CustomData, out result))
                    {
                        //                        tb.CustomData = "";
                        blockIni.Clear();
                    }

                    ManagedWelders managedWelder = new ManagedWelders();
                    managedWelder.welder = tb as IMyShipWelder;

                    managedWelder.bControlled = blockIni.Get(ProjectorSection, "Controlled").ToBoolean(false);
                    blockIni.Set(ProjectorSection, "Controlled", managedWelder.bControlled);

                    managedWelder.PrinterName = blockIni.Get(ProjectorSection, "PrinterName").ToString("none");
                    blockIni.Set(ProjectorSection, "PrinterName", managedWelder.PrinterName);

                    if (managedWelder.bControlled)
                        managedWelders.Add(managedWelder);

                    tb.CustomData = blockIni.ToString();
                }
            }

            void LocalGridChangedHandler()
            {
                welderList.Clear();
                managedWelders.Clear();
            }

            public int Count()
            {
                return welderList.Count;
            }
            public int ManagedCount(string PrinterName)
            {
                if(string.IsNullOrEmpty(PrinterName))
                    return managedWelders.Count;
                int count=0;
                foreach (var managedWelder in managedWelders)
                {
                    if (managedWelder.PrinterName == PrinterName)
                    {
                        count++;
                    }
                }
                return count;
            }
            public class ManagedWelders
            {
                public bool bControlled;
                public IMyShipWelder welder;
                public string PrinterName;
            }

            public void RequestEnable(string printhead, bool bEnabled = true)
            {
                foreach(var managedWelder in managedWelders)
                {
                    if(managedWelder.PrinterName==printhead)
                    {
                        managedWelder.welder.Enabled = bEnabled;
                    }
                }
            }

        }
    }
}
