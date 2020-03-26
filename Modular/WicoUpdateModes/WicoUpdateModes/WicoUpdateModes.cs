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
        public class WicoUpdatesModes : WicoUpdates
        {
            public WicoUpdatesModes(Program program) : base(program)
            {
                thisProgram = program;
                WicoControlInit();
            }

            new void WicoControlInit()
            {
                thisProgram.AddSaveHandler(SaveHandler);
                thisProgram.UpdateTriggerHandlers.Add(ProcessTrigger);
            }
            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            internal void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if (myCommandLine.ArgumentCount > 1)
                {
                    if (myCommandLine.Argument(0) == "setmode")
                    {
                        int toMode = 0;
                        bool bOK = int.TryParse(myCommandLine.Argument(1), out toMode);
                        if (bOK)
                        {
                            SetMode(toMode);
                        }
                    }
                }
            }

            internal void SaveHandler(MyIni theIni)
            {
                theIni.Set("WicoControl", "Mode", _iMode);
                theIni.Set("WicoControl", "State", _iState);
            }

            internal int _iMode = -1;
            internal int _iState = -1;
            public int IMode
            {
                get
                {
                    return _iMode;
                }

                set
                {
                    SetMode(value);
                    //                    _iMode = value;
                }
            }

            public int IState
            {
                get
                {
                    return _iState;
                }

                set
                {
                    SetState(value);
                    //                    _iState = value;
                }
            }

            List<Action<int, int, int, int>> ControlChangeHandlers = new List<Action<int, int, int, int>>();
            List<Action> ModeAfterInitHandlers = new List<Action>();

            public const int MODE_IDLE = 0;

            public const int MODE_DOCKING = 30;
            public const int MODE_DOCKED = 40;
            public const int MODE_LAUNCH = 50; // space launch

            public const int MODE_LAUNCHPREP = 100; // oribital launch prep
            public const int MODE_ORBITALLAUNCH = 120;
            public const int MODE_DESCENT = 150; // descend into space and stop NN meters above surface
            public const int MODE_ORBITALLAND = 151; // land from orbit
            public const int MODE_HOVER = 170;
            public const int MODE_LANDED = 180;

            public const int MODE_MINE = 500;

            public const int MODE_STARTNAV = 600; // start the navigation operations
            public const int MODE_GOINGTARGET = 650;
            public const int MODE_NAVNEXTTARGET = 670; // go to the next target
            public const int MODE_ARRIVEDTARGET = 699; // we have arrived at target

            public const int MODE_ATTENTION = 9999;

            public void SetMode(int theNewMode, int theNewState = 0)
            {
                // do nothing if we are already in that mode
                if (_iMode == theNewMode)
                    return;

                //                SendToAllSubscribers(MODECHANGETAG, sData);
                HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                _iMode = theNewMode;
                _iState = theNewState;
            }

            public void SetState(int theNewState)
            {
                // not synced..
                _iState = theNewState;
            }
            public bool AddControlChangeHandler(Action<int, int, int, int> handler)
            {
                if (!ControlChangeHandlers.Contains(handler))
                    ControlChangeHandlers.Add(handler);
                return true;
            }
            public virtual void HandleModeChange(int fromMode, int fromState, int toMode, int toState)
            {
                foreach (var handler in ControlChangeHandlers)
                {
                    handler(fromMode, fromState, toMode, toState);
                }
                WantOnce();
            }

            public bool AddModeInitHandler(Action handler)
            {
                if (!ModeAfterInitHandlers.Contains(handler))
                    ModeAfterInitHandlers.Add(handler);
                return true;
            }
            new public void ModeAfterInit(MyIni theIni)
            {
                _iState = theIni.Get("WicoControl", "State").ToInt32(_iState);
                _iMode = theIni.Get("WicoControl", "Mode").ToInt32(_iMode);

                foreach (var handler in ModeAfterInitHandlers)
                {
                    handler();
                }
            }

            new public void AnnounceState()
            {
                thisProgram.Echo("Standalone: Mode=" + IMode.ToString() + " S=" + IState.ToString());
            }
        }
    }
}
