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
        public class ModeAttention
        {
            private Program _program;
            private WicoControl _wicoControl;
            private Antennas _antennas;

            public ModeAttention(Program program, WicoControl wc
                , Antennas antennas
                )
            {
                _program = program;
                _wicoControl = wc;
                _antennas = antennas;

                _program.moduleName += " Att";
                _program.moduleList += "\nAttention V4.2k";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);
            }
            /// <summary>
            /// Modes have changed and we are being called as a handler
            /// </summary>
            /// <param name="fromMode"></param>
            /// <param name="fromState"></param>
            /// <param name="toMode"></param>
            /// <param name="toState"></param>
            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if (
                    fromMode == WicoControl.MODE_MINE
                    || fromMode == WicoControl.MODE_BORESINGLE
                    )
                {
                    _antennas.ClearAnnouncement();
                }
                // need to check if this is us
                if (
                    toMode == WicoControl.MODE_ATTENTION
                    )
                {
                    _wicoControl.WantOnce();
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (
                    iState == WicoControl.MODE_ATTENTION
                    )
                {
                    _wicoControl.WantFast();
                }
            }
            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                string[] varArgs = sArgument.Trim().Split(';');

                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
                    // Commands here:

                }
                if (myCommandLine != null)
                {
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_ATTENTION) { doModeAttention(); return; }
            }

            void doModeAttention()
            {
                _program.Echo("Mode Attention!");
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                switch (iState)
                {
                    case 0:
                        _wicoControl.SetState(10);
                        break;
                }
            }
        }
    }
}
