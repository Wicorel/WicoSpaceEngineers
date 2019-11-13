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
        #region WicoControl

        class WicoControl
        {
            public float fMaxWorldMps = 100f;

            #region MODES
            const string MODECHANGETAG = "[WICOMODECHANGE]";
            int _iMode = -1;
            int _iState = -1;
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

                string sData = "";
                sData += _iMode.ToString() + "\n";
                sData += _iState.ToString() + "\n";
                sData += theNewMode.ToString() + "\n";
                sData += theNewState.ToString() + "\n";

                SendToAllSubscribers(MODECHANGETAG, sData);
                HandleModeChange(_iMode, _iState, theNewMode, theNewState);

                _iMode = theNewMode;
                _iState = theNewState;
            }

            public void SetState(int theNewState)
            {
                _iState = theNewState;
            }
            public bool AddControlChangeHandler(Action<int, int, int, int> handler)
            {
                if (!ControlChangeHandlers.Contains(handler))
                    ControlChangeHandlers.Add(handler);
                return true;
            }
            void HandleModeChange(int fromMode, int fromState, int toMode, int toState)
            {
                foreach (var handler in ControlChangeHandlers)
                {
                    handler(fromMode, fromState, toMode, toState);
                }
            }

            public bool AddModeInitHandler(Action handler)
            {
                if (!ModeAfterInitHandlers.Contains(handler))
                    ModeAfterInitHandlers.Add(handler);
                return true;
            }
            public void ModeAfterInit(MyIni theIni)
            {
                _iState = theIni.Get("WicoControl", "State").ToInt32(_iState);
                _iMode = theIni.Get("WicoControl", "Mode").ToInt32(_iMode);

                foreach (var handler in ModeAfterInitHandlers)
                {
                    handler();
                }
            }

            void SaveHandler(MyIni theIni)
            {
                theIni.Set("WicoControl", "Mode", _iMode);
                theIni.Set("WicoControl", "State", _iState);
            }

            #endregion

            #region Updates
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
            #endregion

            Program thisProgram;
            readonly TransmissionDistance localConstructs = TransmissionDistance.CurrentConstruct;
            public WicoControl(Program program)
            {
                thisProgram = program;


                WicoControlInit();
            }


            /// <summary>
            /// List of Wico PB blocks on local construct
            /// </summary>
            List<long> _WicoMainSubscribers = new List<long>();
            bool bIAmMain = true; // assume we are main

            // WIco Main/Config stuff
            readonly string WicoMainTag = "WicoTagMain";
            readonly string YouAreSub = "YOUARESUB";
            readonly string UnicastTagTrigger = "TRIGGER";
            readonly string UnicastAnnounce = "IAMWICO";

            public void WicoControlInit()
            {
                // Wico Configuration system
                //TODO: Load defaults from CustomData
                //thisProgram._CustomDataIni;

                //TODO: load last mode/state from Storage
                //thisProgram._SaveIni;

                // send a messge to all local 'Wico' PBs to get configuration.  
                // This will be used to determine the 'master' PB and to know who to send requests to
                thisProgram.IGC.SendBroadcastMessage(WicoMainTag, "Configure", localConstructs);

                _WicoMainSubscribers.Clear();

                thisProgram.wicoIGC.AddPublicHandler(WicoMainTag, WicoControlMessagehandler);
                thisProgram.wicoIGC.AddUnicastHandler(WicoConfigUnicastListener);

                thisProgram.UpdateTriggerHandlers.Add(ProcessTrigger);
                thisProgram.AddSaveHandler(SaveHandler);

            }
            public bool IamMain()
            {
                return bIAmMain;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument,MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if(sArgument=="test")
                {
                    
                }
                /*
                string[] varArgs = sArgument.Trim().Split(';');
                bool bFoundNAVCommands = false;
                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');

                    if (args[0] == "W" || args[0] == "O")
                    { // [W|O] <x>:<y>:<z>  || W <x>,<y>,<z>
                      // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                      // O means orient towards.  W means orient, then move to
                        bFoundNAVCommands = true;
                        thisProgram.Echo("Args:");
                        for (int icoord = 0; icoord < args.Length; icoord++)
                            thisProgram.Echo(args[icoord]);
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        string sArg = args[1].Trim();

                        if (args.Length > 2)
                        {
                            sArg = args[1];
                            for (int kk = 2; kk < args.Length; kk++)
                                sArg += " " + args[kk];
                            sArg = sArg.Trim();
                        }

                        //                    Echo("sArg=\n'" + sArg+"'");
                        string[] coordinates = sArg.Split(',');
                        if (coordinates.Length < 3)
                        {
                            coordinates = sArg.Split(':');
                        }
                        //                    Echo(coordinates.Length + " Coordinates");
                        for (int icoord = 0; icoord < coordinates.Length; icoord++)
                            thisProgram.Echo(coordinates[icoord]);
                        //Echo("coordiantes.Length="+coordinates.Length);  
                        if (coordinates.Length < 3)
                        {
                            //Echo("P:B");  

                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
//                            gyrosOff();// shutdown(gyroList);
                            return;
                        }
                        int iCoordinate = 0;
                        string sWaypointName = "Waypoint";
                        //  -  0   1           2        3          4       5
                        // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                        if (coordinates[0] == "GPS")
                        {
                            if (coordinates.Length > 4)
                            {
                                sWaypointName = coordinates[1];
                                iCoordinate = 2;
                            }
                            else
                            {
                                thisProgram.Echo("Invalid Command");
                                thisProgram.ResetMotion();
//                                gyrosOff();
                                return;
                            }
                        }

                        double x, y, z;
                        bool xOk = double.TryParse(coordinates[iCoordinate++].Trim(), out x);
                        bool yOk = double.TryParse(coordinates[iCoordinate++].Trim(), out y);
                        bool zOk = double.TryParse(coordinates[iCoordinate++].Trim(), out z);
                        if (!xOk || !yOk || !zOk)
                        {
                            //Echo("P:C");  
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            //			shutdown(gyroList);
                            continue;
                        }

                        //                    sStartupError = "CMD Initiated NAV:\n" + sArgument;

                        //                    _vNavTarget = new Vector3D(x, y, z);
                        //                    _bValidNavTarget = true;
                        if (args[0] == "W")
                        {
                            _NavAddTarget(new Vector3D(x, y, z), MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin, sWaypointName, _shipSpeedMax);
                            //                        bGoOption = true;
                        }
                        else
                        {
                            _NavAddTarget(new Vector3D(x, y, z), MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin, sWaypointName, _shipSpeedMax, false);
                            //                        bGoOption = false;
                        }
                        //                    sStartupError += "\nW " + sWaypointName + ":" + wicoNavCommands.Count.ToString();
                        //                   setMode(MODE_GOINGTARGET);

                    }
                    else if (args[0] == "S")
                    { // S <mps>
                      // TODO: Queue the command into NavCommands
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        double x;
                        bool xOk = double.TryParse(args[1].Trim(), out x);
                        if (xOk)
                        {
                            _shipSpeedMax = x;
                            //                        Echo("Set speed to:" + _shipSpeedMax.ToString("0.00"));
                            //             setMode(MODE_ARRIVEDTARGET);
                        }
                        else
                        {
                            //Echo("P:C");  
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                    }
                    else if (args[0] == "D")
                    { // D <meters>
                      // TODO: Queue the command into NavCommands
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        double x;
                        bool xOk = double.TryParse(args[1].Trim(), out x);
                        if (xOk)
                        {
                            ArrivalDistanceMin = x;
                            //                        Echo("Set arrival distance to:" + ArrivalDistanceMin.ToString("0.00"));
                        }

                        else
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                    }
                    else if (args[0] == "C")
                    { // C <anything>
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        else
                        {
                            thisProgram.Echo(varArgs[iArg]);
                        }
                    }
                    else if (args[0] == "L")
                    { // L launch
                        bFoundNAVCommands = true;
                        _NavQueueLaunch();
                    }
                    else if (args[0] == "launch")
                    { // L launch
                        bFoundNAVCommands = true;
                        _NavQueueLaunch();
                    }
                    else if (args[0] == "OL")
                    { // OL Orbital launch
                        bFoundNAVCommands = true;
                        _NavQueueOrbitalLaunch();
                    }
                    else if (args[0] == "orbitallaunch")
                    { // OL Orbital launch
                        bFoundNAVCommands = true;
                        _NavQueueOrbitalLaunch();
                    }
                    else if (args[0] == "dock")
                    { // dock
                        bFoundNAVCommands = true;
                        _NavQueueOrbitalLaunch();
                    }
                    // todo: add launch, dock, land, etc
                    else
                    {
                        int iDMode;
                        if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                        {
                            sArgResults = "mode set to " + iDMode;
                            setMode(iDMode);
                            // return true;
                        }
                        else
                        {
                            sArgResults = "Unknown argument:" + args[0];
                        }
                    }
                }
                if (bFoundNAVCommands)
                {
                    //                sStartupError += "\nFound NAV Commands:" + wicoNavCommands.Count.ToString();
                    _NavStart();
                }
                */

            }

            public void SendToAllSubscribers(string tag, string argument)
            {
                foreach (var submodule in _WicoMainSubscribers)
                {
                    if (submodule == thisProgram.Me.EntityId) continue; // skip ourselves if we are in the list.
                    thisProgram.IGC.SendUnicastMessage(submodule, tag, argument);
                }
            }

            /// <summary>
            /// Broadcast handler for Wico Control Messages
            /// </summary>
            /// <param name="msg"></param>
            public void WicoControlMessagehandler(MyIGCMessage msg)
            {
                var tag = msg.Tag;

                //            Echo("WMMH:"+tag);

                var src = msg.Source;
                if (tag == WicoMainTag)
                {
                    string data = (string)msg.Data;
                    if (data == "Configure")
                    {
                        thisProgram.IGC.SendUnicastMessage(src, UnicastAnnounce, "");
                    }
                }
            }

            /// <summary>
            /// Wico Unicast Handler for Wico Main
            /// </summary>
            /// <param name="msg"></param>
            public void WicoConfigUnicastListener(MyIGCMessage msg)
            {
                var tag = msg.Tag;
                var src = msg.Source;
                if (tag == YouAreSub)
                {
                    bIAmMain = false;
                }
                else if (tag == UnicastAnnounce)
                {
                    // another block announces themselves as one of our collective
                    if (_WicoMainSubscribers.Contains(src))
                    {
                        // already in the list
                    }
                    else
                    {
                        // not in the list
                        thisProgram.Echo("Adding new");
                        _WicoMainSubscribers.Add(src);
                    }
                    bIAmMain = true;
                    foreach (var other in _WicoMainSubscribers)
                    {
                        // if somebody has a lower ID, use them instead.
                        if (other < thisProgram.Me.EntityId)
                        {
                            bIAmMain = false;
                            thisProgram.Echo("Found somebody lower");
                        }
                    }
                }
                else if (tag == UnicastTagTrigger)
                {
                    // we are being informed that we were wanted to run for some reason (misc)
                    thisProgram.Echo("Trigger Received" + msg.Data);
                }
                else if (tag == MODECHANGETAG)
                {
                    string[] aLines = ((string)msg.Data).Split('\n');
                    // 0=old mode 1=old state. 2=new mode 3=new state
                    int theNewMode = Convert.ToInt32(aLines[2]);
                    int theNewState = Convert.ToInt32(aLines[3]);
                    _iMode = theNewMode;
                    _iState = theNewState;

                }
                // TODO: add more messages as needed
            }



        }
        #endregion

    }
}
