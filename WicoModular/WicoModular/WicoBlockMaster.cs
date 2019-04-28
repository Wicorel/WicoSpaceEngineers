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
            public WicoBlockMaster(Program program)
            {
                thisProgram = program;
                GridTerminalSystem = thisProgram.GridTerminalSystem;
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

        }
        #endregion
    }
}
