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
        public class WicoUpdates
        {
            protected Program _program;

            public bool _bUpdateDebug = false;

            public WicoUpdates(Program program)
            {
                _program = program;

                _bUpdateDebug = _program.CustomDataIni.Get(_program.OurName, "UpdateDebug").ToBoolean(_bUpdateDebug);
                _program.CustomDataIni.Set(_program.OurName, "UpdateDebug", _bUpdateDebug);
            }

            public float fMaxWorldMps = 100f;

            bool bWantOnce = false;
            bool bWantFast = false;
            bool bWantMedium = false;
            bool bWantSlow = false;

            /// <summary>
            /// clear all requested updates.  Usually needs to be called on each run
            /// </summary>
            public void ResetUpdates()
            {
                bWantOnce = false;
                bWantFast = false;
                bWantMedium = false;
                bWantSlow = false;
            }

            /// <summary>
            /// Set the update to Once
            /// </summary>
            public void WantOnce()
            {
                bWantOnce = true;
            }

            /// <summary>
            /// Set the update to Update1
            /// </summary>
            public void WantFast()
            {
                bWantFast = true;
            }
            /// <summary>
            /// Set the update to Update10
            /// </summary>
            public void WantMedium()
            {
                bWantMedium = true;
            }
            /// <summary>
            /// Set the update to update100
            /// </summary>
            public void WantSlow()
            {
                bWantSlow = true;
            }

            /// <summary>
            /// Generate the desired updatefrequency from the settings
            /// </summary>
            /// <returns>desired updatefrequency </returns>
            public UpdateFrequency GenerateUpdate()
            {
                UpdateFrequency desired = 0;
                if (bWantOnce) desired |= UpdateFrequency.Once;
                if (bWantFast) desired |= UpdateFrequency.Update1;
                if (bWantMedium) desired |= UpdateFrequency.Update10;
                if (bWantSlow) desired |= UpdateFrequency.Update100;
                return desired;
            }

            public void AnnounceState()
            {
                _program.Echo("Standalone Control");
            }

        }
    }
}
