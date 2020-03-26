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
            protected Program thisProgram;

            public WicoUpdates(Program program)
            {
                thisProgram = program;

                WicoControlInit();
            }

            internal void WicoControlInit()
            {
            }
            public float fMaxWorldMps = 100f;

            bool bWantOnce = false;
            bool bWantFast = false;
            bool bWantMedium = false;
            bool bWantSlow = false;

            public void ResetUpdates()
            {
                bWantOnce = false;
                bWantFast = false;
                bWantMedium = false;
                bWantSlow = false;
            }
            public void WantOnce()
            {
                bWantOnce = true;
            }
            public void WantFast()
            {
                bWantFast = true;
            }
            public void WantMedium()
            {
                bWantMedium = true;
            }
            public void WantSlow()
            {
                bWantSlow = true;
            }
            public UpdateFrequency GenerateUpdate()
            {
                UpdateFrequency desired = 0;
                if (bWantOnce) desired |= UpdateFrequency.Once;
                if (bWantFast) desired |= UpdateFrequency.Update1;
                if (bWantMedium) desired |= UpdateFrequency.Update10;
                if (bWantSlow) desired |= UpdateFrequency.Update100;
                return desired;
            }

            public void ModeAfterInit(MyIni LoadIni)
            {
            }

            public void AnnounceState()
            {
                thisProgram.Echo("Standalone Control");
            }

            internal bool _bDebug = false;

            public void SetDebug(bool bDebug)
            {
                _bDebug = bDebug;
            }

        }
    }
}
