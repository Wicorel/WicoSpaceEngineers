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

        #region WicoBlockMaster

        class WicoBlockMaster
        {
            Program thisProgram;
            IMyGridTerminalSystem GridTerminalSystem;

            List<IMyShipController> shipControllers = new List<IMyShipController>();
            private IMyShipController MainShipController;


            public WicoBlockMaster(Program program)
            {
                thisProgram = program;
                GridTerminalSystem = thisProgram.GridTerminalSystem;

                AddLocalBlockHandler(BlockParseHandler);
            }
            List<IMyTerminalBlock> gtsLocalBlocks = new List<IMyTerminalBlock>();
            long localBlocksCount = 0;

            List<IMyTerminalBlock> gtsRemoteBlocks = new List<IMyTerminalBlock>();
            long remoteBlocksCount = 0;

            List<Action<IMyTerminalBlock>> WicoLocalBlockParseHandlers = new List<Action<IMyTerminalBlock>>();
            List<Action<IMyTerminalBlock>> WicoRemoteBlockParseHandlers = new List<Action<IMyTerminalBlock>>();


            public bool AddLocalBlockHandler(Action<IMyTerminalBlock> handler)
            {
                if (!WicoLocalBlockParseHandlers.Contains(handler))
                    WicoLocalBlockParseHandlers.Add(handler);
                return true;
            }
            public bool AddRemoteBlockHandler(Action<IMyTerminalBlock> handler)
            {
                if (!WicoRemoteBlockParseHandlers.Contains(handler))
                    WicoRemoteBlockParseHandlers.Add(handler);
                return true;
            }

            public void LocalBlocksInit()
            {
                //TODO: Load defaults from CustomData

                gtsLocalBlocks.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsLocalBlocks, (x1 => x1.IsSameConstructAs(thisProgram.Me)));
                localBlocksCount = gtsLocalBlocks.Count;

                foreach (var tb in gtsLocalBlocks)
                {
                    foreach (var handler in WicoLocalBlockParseHandlers)
                    {
                        handler(tb);
                    }
                }
            }

            public void RemoteBlocksInit()
            {
                gtsRemoteBlocks.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsRemoteBlocks, (x1 => !x1.IsSameConstructAs(thisProgram.Me)));
                remoteBlocksCount = gtsRemoteBlocks.Count;
                foreach (var tb in gtsRemoteBlocks)
                {
                    foreach (var handler in WicoRemoteBlockParseHandlers)
                    {
                        handler(tb);
                    }
                }
            }
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyCryoChamber)
                    return; // we don't want this.
                if (tb is IMyShipController)
                {
                    // TODO: Check for other things for ignoring
                    shipControllers.Add(tb as IMyShipController);
                }
            }

            /// <summary>
            /// Returns the main ship controller
            /// </summary>
            /// <returns></returns>
            public IMyShipController GetMainController()
            {
                // TODO: check for occupied, etc.
                if (MainShipController == null)
                {
                    // pick a controller
                    foreach (var tb in shipControllers)
                    {
                        if (tb is IMyRemoteControl)
                        {
                            // found a good one
                            MainShipController = tb;
                            break;
                        }
                    }
                    // we didn't find one
                    if (MainShipController == null)
                    {
                        foreach (var tb in shipControllers)
                        {
                            if (tb is IMyShipController)
                            {
                                // found a good one
                                MainShipController = tb;
                                break;
                            }
                        }
                    }
                }
                return MainShipController;

            }

        }



        #endregion
    }
}
