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
            readonly WicoControl _wicoControl;


            public ScanBase(Program program,WicoControl wicoControl)
            {
                _program = program;
                _wicoControl = wicoControl;
            }

            // Module code:

            protected readonly string sScansTag = "WICOSCANS";
            protected readonly string sStartCommand = "START";

            // TODO: Flags for other options (TBD)
            // TODO: scan range
            // TODO: stop on first hit (by type?)
            // TODO: all sides or specific sides?
            public virtual void StartScans(int doneMode = WicoControl.MODE_ARRIVEDTARGET, int doneState = 0)
            {
                // scans are not in this module with base.
                // send IGC message to local construct to do scans.
                string sCommand = sStartCommand+":" + doneMode.ToString() + ":" + doneState.ToString();
                _wicoControl.SendToAllSubscribers(sScansTag, sCommand);
            }

        }
    }
}
