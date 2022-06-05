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
        public class ScansMode : ScanBase
        {

            Program _program;
            WicoControl _wicoControl;
            WicoIGC _wicoIGC;
            WicoBlockMaster _wicoBlockMaster;
            Cameras _cameras;
            Asteroids _asteroids;
            Displays _displays;

            /// <summary>
            /// The max range at which we scan. Default.  Value loaded from CustomData
            /// </summary>
            long _scanRange = 5000;
            string sScansSection = "SCANS";

            string _CameraSection = "CAMERA";
            string _NameScanRange = "ScanRange";

            int ScansDoneMode = WicoControl.MODE_NAVNEXTTARGET;
            int ScansDoneState = 0;

            public ScansMode(Program program, WicoControl wicoControl, WicoBlockMaster wicoBlockMaster,
                WicoIGC igc, Cameras cameras, Asteroids asteroids
                , Displays displays
                ) : base(program, wicoControl)
            {
                _program = program;
                _wicoControl = wicoControl;
                _wicoBlockMaster = wicoBlockMaster;
                _wicoIGC = igc;
                _cameras = cameras;
                _asteroids = asteroids;
                _displays = displays;

                _program.moduleName += " Scans";
                _program.moduleList += "\nScans V4.2a";

                _scanRange = _program.CustomDataIni.Get(_CameraSection, _NameScanRange).ToInt64(_scanRange);
                _program.CustomDataIni.Set(_CameraSection, _NameScanRange, _scanRange);

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);


                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _wicoIGC.AddPublicHandler(sScansTag, BroadcastHandler);
                _wicoIGC.AddUnicastHandler(BroadcastHandler);

                _displays.AddSurfaceHandler("MODE", SurfaceHandler);

            }

            void LoadHandler(MyIni Ini)
            {
                // TODO: Save current scan state and resume

                ScansDoneMode = Ini.Get(sScansSection, "ScansDoneMode").ToInt32(ScansDoneMode);
                ScansDoneState = Ini.Get(sScansSection, "ScansDoneState").ToInt32(ScansDoneState);

            }

            void SaveHandler(MyIni Ini)
            {
                // TODO: Save current scan state and resume.
                Ini.Set(sScansSection, "ScansDoneMode", ScansDoneMode);
                Ini.Set(sScansSection, "ScansDoneState", ScansDoneState);
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
                // need to check if this is us
                if (toMode == WicoControl.MODE_DOSCANS
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

                if (iMode == WicoControl.MODE_DOSCANS)
                {
                    // TODO: Check state and re-init as needed
                    _wicoControl.WantFast();
                }
            }
            void LocalGridChangedHandler()
            {
                //               shipController = null;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if (sArgument == "doscans")
                {
                    _wicoControl.SetMode(WicoControl.MODE_DOSCANS);
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                // need to check if this is us
                if (iMode == WicoControl.MODE_DOSCANS) { doModeScans(); }
            }

            void BroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
                if (msg.Tag == sScansTag)
                {
                    if (msg.Data is string)
                    {
                        string sCommand = msg.Data as string;
                        sCommand = sCommand.Trim();
                        string[] saArguments = sCommand.Split(':');

                        if (saArguments[0]== sStartCommand)
                        {
                            int iArgument = 1;
                            int doneMode = WicoControl.MODE_NAVNEXTTARGET;
                            int doneState = 0;

                            // optional arguments for mode and state
                            if (int.TryParse(saArguments[iArgument++], out doneMode))
                                ScansDoneMode = doneMode;

                            if (int.TryParse(saArguments[iArgument++], out doneState))
                                ScansDoneState = doneState;

                            _wicoControl.SetMode(WicoControl.MODE_DOSCANS);
                        }
                    }
                }
            }
            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "MODE")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        int iMode = _wicoControl.IMode;
                        int iState = _wicoControl.IState;

                        if (iMode == WicoControl.MODE_DOSCANS
                            )
                        {
                            tsurface.WriteText(sbModeInfo);
                            if (tsurface.SurfaceSize.Y < 256)
                            { // small/corner LCD

                            }
                            else
                            {
                                tsurface.WriteText(sbNotices, true);
                            }
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 256)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 2;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.5f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
            }

            // TODO: Flags for other options (TBD)
            // TODO: scan range
            // TODO: stop on first hit (by type?)
            // TODO: all sides or specific sides?
            public override void StartScans(int doneMode = WicoControl.MODE_NAVNEXTTARGET, int doneState = 0)
            {
                ScansDoneMode = doneMode;
                ScansDoneState = doneState;
                //                current_state = 0;
                _wicoControl.SetMode(WicoControl.MODE_DOSCANS, 0);
            }


            QuadrantCameraScanner scanfrontScanner;
            QuadrantCameraScanner scanbackScanner;
            QuadrantCameraScanner scanleftScanner;
            QuadrantCameraScanner scanrightScanner;
            QuadrantCameraScanner scantopScanner;
            QuadrantCameraScanner scanbottomScanner;

            double scanElapsedMs = 0;

            void doModeScans()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                //                StatusLog("clear", textPanelReport);
                //                StatusLog(moduleName + ":SCAN!", textPanelReport);
                _program.Echo("Scan:iState=" + iState.ToString());
                sbModeInfo.Clear();
                sbNotices.Clear();
                sbModeInfo.AppendLine("Raycast Scanning");

                switch (iState)
                {
                    case 0:
                        { // init camera scan for asteroids/objects
                            _program.ResetMotion();
                            // TODO: Use WicoET
                            scanElapsedMs = 0;

                            _scanRange = _program.CustomDataIni.Get(_CameraSection, _NameScanRange).ToInt64(_scanRange);

                            // initialize cameras
                            if (scanfrontScanner == null)
                                scanfrontScanner = new QuadrantCameraScanner(_program, _cameras.GetForwardCameras(), _scanRange);
                            else scanfrontScanner.Reset();

                            if (scanbackScanner == null)
                                scanbackScanner = new QuadrantCameraScanner(_program, _cameras.GetBackwardCameras(), _scanRange);
                            else scanbackScanner.Reset();

                            if (scanleftScanner == null)
                                scanleftScanner = new QuadrantCameraScanner(_program, _cameras.GetLeftCameras(), _scanRange);
                            else scanleftScanner.Reset();

                            if (scanrightScanner == null)
                                scanrightScanner = new QuadrantCameraScanner(_program, _cameras.GetRightCameras(), _scanRange);
                            else scanrightScanner.Reset();

                            if (scantopScanner == null)
                                scantopScanner = new QuadrantCameraScanner(_program, _cameras.GetUpCameras(), _scanRange);
                            else scantopScanner.Reset();

                            if (scanbottomScanner == null)
                                scanbottomScanner = new QuadrantCameraScanner(_program, _cameras.GetDownwardCameras(), _scanRange);
                            else scanbottomScanner.Reset();

                            _wicoControl.SetState(410);// iState = 410;
                            _wicoControl.WantFast();
                            break;
                        }
                    case 410:
                        {
                            //                            StatusLog("Long Range Scan", textPanelReport);
                            if (scanfrontScanner == null) // in case we reload/compile in this state..
                            {
                                _wicoControl.WantFast();
                                iState = 0;
                                return;
                            }
                            _wicoControl.WantMedium();

                            // TODO: use ET
                            scanElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                            // use for timeout...

                            // do camera scans

                            if (scanfrontScanner.DoScans())
                            {
                                _asteroids.AsteroidProcessLDEI(scanfrontScanner.myLDEI);
                            }
                            if (scanbackScanner.DoScans())
                            {
                                _asteroids.AsteroidProcessLDEI(scanbackScanner.myLDEI);
                            }
                            if (scanleftScanner.DoScans())
                            {
                                _asteroids.AsteroidProcessLDEI(scanleftScanner.myLDEI);
                            }
                            if (scanrightScanner.DoScans())
                            {
                                _asteroids.AsteroidProcessLDEI(scanrightScanner.myLDEI);
                            }
                            if (scantopScanner.DoScans())
                            {
                                _asteroids.AsteroidProcessLDEI(scantopScanner.myLDEI);
                            }
                            if (scanbottomScanner.DoScans())
                            {
                                _asteroids.AsteroidProcessLDEI(scanbottomScanner.myLDEI);
                            }

                            // TODO: if missing cameras on a side(s), rotate ship to point cameras at that side
                            // TODO: Flags for other options (TBD)
                            // TODO: scan range
                            // TODO: stop on first hit (by type?)
                            // TODO: all sides or specific sides?

                            string s = "";
                            s += "Front: ";
                            if (scanfrontScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
//                                s += scanfrontScanner.SCAN_DISTANCE.ToString("0") + "M";
                            }
                            s += " " + scanfrontScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Back: ";
                            if (scanbackScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
//                                s += scanbackScanner.SCAN_DISTANCE.ToString("0") + "M";
                            }
                            s += " " + scanbackScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Left: ";
                            if (scanleftScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
//                                s += scanleftScanner.SCAN_DISTANCE.ToString("0") + "M";
                            }
                            s += " " + scanleftScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Right: ";
                            if (scanrightScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
//                                s += scanrightScanner.SCAN_DISTANCE.ToString("0") + "M";
                            }
                            s += " " + scanrightScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Top: ";
                            if (scantopScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
//                                s += scantopScanner.SCAN_DISTANCE.ToString("0") + "M";
                            }
                            s += " " + scantopScanner.myLDEI.Count + " objects";
                            s += "\n";

                            s += "Bottom: ";
                            if (scanbottomScanner.DoneScanning())
                                s += "DONE!";
                            else
                            {
//                                s += scanbottomScanner.SCAN_DISTANCE.ToString("0") + "M";
                            }
                            s += " " + scanbottomScanner.myLDEI.Count + " objects";
                            s += "\n";

                            if (_asteroids.AsteroidFindNearest() < 0)
                                s += "No Known Asteroid";
                            else s += "FOUND at least one asteroid!";

                            //                            StatusLog(s, textPanelReport);
                            sbNotices.AppendLine(s);

                            _program.Echo(s);

                            if (
                                scanfrontScanner.DoneScanning() &&
                                scanbackScanner.DoneScanning() &&
                                scanleftScanner.DoneScanning() &&
                                scanrightScanner.DoneScanning() &&
                                scantopScanner.DoneScanning() &&
                                scanbottomScanner.DoneScanning()
                                )
                            {
                                _wicoControl.SetMode(ScansDoneMode, ScansDoneState);

                                // reset to default for possible next run
                                ScansDoneMode = WicoControl.MODE_NAVNEXTTARGET;
                                ScansDoneState = 0;
                            }
                            break;
                        }
                }

            }


        }
    }
}
