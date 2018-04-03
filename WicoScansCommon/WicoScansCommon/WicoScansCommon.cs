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
        int ScansDoneMode = MODE_SCANCOMPLETED;
        int ScansDoneState = 0;

        string sScansSection = "SCANS";
        void ScansInitCustomData(INIHolder iNIHolder)
        {
 //           iNIHolder.GetValue(sScansSection, "CameraViewOnly", ref sCameraViewOnly, true);
        }
        void ScansSerialize(INIHolder iNIHolder)
        {
            //            iNIHolder.SetValue(sNavSection, "vNavHome", vNavHome);
            //            iNIHolder.SetValue(sNavSection, "ValidNavHome", bValidNavHome);
            iNIHolder.SetValue(sScansSection, "DoneMode", ScansDoneMode);
            iNIHolder.SetValue(sScansSection, "DoneState", ScansDoneState);

        }
        void ScansDeserialize(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sScansSection, "DoneMode", ref ScansDoneMode, true);
            iNIHolder.GetValue(sScansSection, "DoneState", ref ScansDoneState, true);
        }

        // TODO: Flags for other options (TBD)
        // TODO: scan range
        // TODO: stop on first hit (by type?)
        // TODO: all sides or specific sides?
        void StartScans(int doneMode = MODE_ARRIVEDTARGET, int doneState = 0)
        {
            ScansDoneMode = doneMode;
            ScansDoneState = doneState;
            current_state = 0;
            setMode(MODE_DOSCAN);
        }

    }
}
