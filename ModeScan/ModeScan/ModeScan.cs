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

        QuadrantCameraScanner scanfrontScanner;
        QuadrantCameraScanner scanbackScanner;
        QuadrantCameraScanner scanleftScanner;
        QuadrantCameraScanner scanrightScanner;
        QuadrantCameraScanner scantopScanner;
        QuadrantCameraScanner scanbottomScanner;

        double scanElapsedMs = 0;

        void doModeScans()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":SCAN!", textPanelReport);
            Echo("Scan:current_state=" + current_state.ToString());

            switch (current_state)
            {
                case 0:
                    { // init camera scan for asteroids
                        ResetMotion();
                        scanElapsedMs = 0;

                        // initialize cameras
                        scanfrontScanner = new QuadrantCameraScanner(this, cameraForwardList, 5000);
                        scanbackScanner = new QuadrantCameraScanner(this, cameraBackwardList, 5000);
                        scanleftScanner = new QuadrantCameraScanner(this, cameraLeftList, 5000);
                        scanrightScanner = new QuadrantCameraScanner(this, cameraRightList, 5000);
                        scantopScanner = new QuadrantCameraScanner(this, cameraUpList, 5000);
                        scanbottomScanner = new QuadrantCameraScanner(this, cameraDownList, 5000);

                        current_state = 410;
                        break;
                    }
                case 410:
                    {
                        StatusLog("Long Range Scan", textPanelReport);
                        if (scanfrontScanner == null) // in case we reload/compile in this state..
                            current_state = 0;
                        bWantMedium = true;
                        scanElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        // use for timeout...

                        // do camera scans

                        if (scanfrontScanner.DoScans())
                        {
                            AsteroidProcessLDEI(scanfrontScanner.myLDEI);
                        }
                        if (scanbackScanner.DoScans())
                        {
                            AsteroidProcessLDEI(scanbackScanner.myLDEI);
                        }
                        if (scanleftScanner.DoScans())
                        {
                            AsteroidProcessLDEI(scanleftScanner.myLDEI);
                        }
                        if (scanrightScanner.DoScans())
                        {
                            AsteroidProcessLDEI(scanrightScanner.myLDEI);
                        }
                        if (scantopScanner.DoScans())
                        {
                            AsteroidProcessLDEI(scantopScanner.myLDEI);
                        }
                        if (scanbottomScanner.DoScans())
                        {
                            AsteroidProcessLDEI(scanbottomScanner.myLDEI);
                        }

                        // take the first one found.
                        // TODO: do all search and then choose 'best' (closest?)
                        // TODO: Aim at the hit position and not 'CENTER' for more randomized start on asteroid
                        // TODO: once we find asteroid(s) choose how to find ore intelligently and not just randomly
                        /*
                        if (bValidAsteroid)
                            current_state = 120;
                            */
                        string s = "";
                        s += "Front: ";
                        if (scanfrontScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanfrontScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + scanfrontScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Back: ";
                        if (scanbackScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanbackScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + scanbackScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Left: ";
                        if (scanleftScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanleftScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + scanleftScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Right: ";
                        if (scanrightScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanrightScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + scanrightScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Top: ";
                        if (scantopScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scantopScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + scantopScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Bottom: ";
                        if (scanbottomScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanbottomScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + scanbottomScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        if (AsteroidFindNearest() < 0)
                            s += "No Known Asteroid";
                        else s += "FOUND at least one asteroid!";

                        StatusLog(s, textPanelReport);
                        Echo(s);

                        if (
                            scanfrontScanner.DoneScanning() &&
                            scanbackScanner.DoneScanning() &&
                            scanleftScanner.DoneScanning() &&
                            scanrightScanner.DoneScanning() &&
                            scantopScanner.DoneScanning() &&
                            scanbottomScanner.DoneScanning()
                            )
                        {
                            setMode(MODE_SCANCOMPLETED);
                            /*
                            //                            long asteroidID = -1;
                            if (HasDrills())
                            {
                                scanAsteroidID = AsteroidFindNearest();
                                if (scanAsteroidID < 0)
                                {
                                    // all scans have run and didn't find asteroid..
                                    setMode(MODE_ATTENTION);
                                }
                                else
                                {
                                    bValidAsteroid = true;
                                    vTargetAsteroid = AsteroidGetPosition(scanAsteroidID);
                                    vExpectedAsteroidExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
                                    vExpectedAsteroidExit.Normalize();

                                    current_state = 120;
                                }
                            }
                            else
                            { // if no drills, we are done.
                                setMode(MODE_IDLE);
                            }
                            */
                        }
                        break;
                    }
            }

        }


    }
}