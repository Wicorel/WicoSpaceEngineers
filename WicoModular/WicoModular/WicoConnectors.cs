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
        class Connectors
        {
            List<IMyTerminalBlock> localConnectors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> localDockConnectors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> localBaseConnectors = new List<IMyTerminalBlock>();

            Program thisProgram;
            public Connectors(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyShipConnector)
                {
                    localConnectors.Add(tb);
                    if (tb.CustomName.Contains("[DOCK]") || tb.CustomData.Contains("[DOCK]"))
                        localDockConnectors.Add(tb);
                    if (tb.CustomName.Contains("[BASE]") || tb.CustomData.Contains("[BASE]"))
                        localBaseConnectors.Add(tb);
                }
            }
            public bool AnyConnectorIsLocked()
            {
                List<IMyTerminalBlock> useConnectors = localDockConnectors;
                if (useConnectors.Count < 1) useConnectors = localConnectors;
                for (int i = 0; i < useConnectors.Count; i++)
                {
                    var sc1 = useConnectors[i] as IMyShipConnector;
                    if (sc1 == null) continue;
                    if (sc1.Status == MyShipConnectorStatus.Connectable)
                        //		if (sc.IsLocked)
                        return true;
                }
                return false;
            }

            public bool AnyConnectorIsConnected()
            {
                List<IMyTerminalBlock> useConnectors = localDockConnectors;
                if (useConnectors.Count < 1) useConnectors = localConnectors;
                for (int i = 0; i < useConnectors.Count; i++)
                {
                    var sc1 = useConnectors[i] as IMyShipConnector;
                    if (sc1 == null) continue;
                    if (sc1.Status == MyShipConnectorStatus.Connected)
                    {
                        var sco = sc1.OtherConnector;
//                        if (sco.CubeGrid == sc1.CubeGrid)
                            if (sco.IsSameConstructAs( sc1))
                            {
                                //Echo("Locked-but connected to 'us'");
                                continue;
                        }
                        else return true;
                    }
                }
                return false;
            }
            public void ConnectAnyConnectors(bool bConnect = true, bool bOn = true)
            {
                List<IMyTerminalBlock> useConnectors = localDockConnectors;
                if (useConnectors.Count < 1) useConnectors = localConnectors;
                for (int i = 0; i < useConnectors.Count; i++)
                {
                    var sc1 = useConnectors[i] as IMyShipConnector;
                    if (sc1 == null) continue;
                    if (sc1.Status == MyShipConnectorStatus.Connected)
                    {
                        var sco = sc1.OtherConnector;
                        if (sco.CubeGrid == sc1.CubeGrid)
                        {
                            //Echo("Locked-but connected to 'us'");
                            continue; // skip it.
                        }
                    }
                    if (bConnect)
                    {
                        if (sc1.Status == MyShipConnectorStatus.Connectable)
                            //sc1.ApplyAction("SwitchLock");
                            sc1.Connect();
                    }
                    else
                    {
                        if (sc1.Status == MyShipConnectorStatus.Connected)
                            //sc1.ApplyAction("SwitchLock");
                            sc1.Disconnect();
                    }
                    sc1.Enabled = bOn;
                    /*
                        if (sAction != "")
                        {
                            ITerminalAction ita;
                            ita = sc.GetActionWithName(sAction);
                            if (ita != null) ita.Apply(sc);
                        }
                        */
                }
                return;
            }
        }
    }
}
