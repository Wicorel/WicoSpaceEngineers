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
        public class ScanBase
        {
            readonly Program _program;
            public ScanBase(Program program)
            {
                _program = program;
            }
            // TODO: Flags for other options (TBD)
            // TODO: scan range
            // TODO: stop on first hit (by type?)
            // TODO: all sides or specific sides?
            public virtual void StartScans(int doneMode = WicoControl.MODE_ARRIVEDTARGET, int doneState = 0)
            {
                // scans are not in this module with base.
                // send IGC message to local construct to do scans.

                // TODO: Implementation: see navcommon
                /*
                ScansDoneMode = doneMode;
                ScansDoneState = doneState;
                current_state = 0;
                setMode(MODE_DOSCAN);
                */
            }

        }
    }
}
