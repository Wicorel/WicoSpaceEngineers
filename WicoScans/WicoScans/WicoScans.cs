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
        double scaningElapsedMs = 0;
        QuadrantCameraScanner scanfrontScanner;
        QuadrantCameraScanner scanbackScanner;
        QuadrantCameraScanner scanleftScanner;
        QuadrantCameraScanner scanrightScanner;
        QuadrantCameraScanner scantopScanner;
        QuadrantCameraScanner scanbottomScanner;

        void doModeScans()
        {
            switch (iMode)
            {

                case 0:
                    current_state = 400;
                    break;
                case 400:
                    { // init camera scan for asteroids
                        ResetMotion();
                        turnEjectorsOn();
                        sleepAllSensors();
                        scaningElapsedMs = 0;

                        // initialize cameras
                        scanfrontScanner = new QuadrantCameraScanner(this, cameraForwardList);
                        scanbackScanner = new QuadrantCameraScanner(this, cameraBackwardList);
                        scanleftScanner = new QuadrantCameraScanner(this, cameraLeftList);
                        scanrightScanner = new QuadrantCameraScanner(this, cameraRightList);
                        scantopScanner = new QuadrantCameraScanner(this, cameraUpList);
                        scanbottomScanner = new QuadrantCameraScanner(this, cameraDownList);

                        current_state = 410;
                        break;
                    }
                case 410:
                    {
                        StatusLog("Long Range Scan", textPanelReport);
                        if (scanfrontScanner == null) // in case we reload/compile in this state..
                            current_state = 400;
                        bWantMedium = true;
                        scaningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        // use for timeout...

                        // do camera scans
                        if (scanfrontScanner.DoScans())
                        {
                            if (scanfrontScanner.lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                            {
                                MinerProcessScan(scanfrontScanner.lastDetectedInfo);
                            }
                        }
                        else if (scanbackScanner.DoScans())
                        {
                            MinerProcessScan(scanbackScanner.lastDetectedInfo);
                        }
                        else if (scanleftScanner.DoScans())
                        {
                            MinerProcessScan(scanleftScanner.lastDetectedInfo);
                        }
                        else if (scanrightScanner.DoScans())
                        {
                            MinerProcessScan(scanrightScanner.lastDetectedInfo);
                        }
                        else if (scantopScanner.DoScans())
                        {
                            MinerProcessScan(scantopScanner.lastDetectedInfo);
                        }
                        else if (scanbottomScanner.DoScans())
                        {
                            MinerProcessScan(scanbottomScanner.lastDetectedInfo);
                        }
                        // take the first one found.
                        // TODO: do all search and then choose 'best' (closest?)
                        // TODO: Aim at the hit position and not 'CENTER' for more randomized start on asteroid
                        // TODO: once we find asteroid(s) choose how to find ore intelligently and not just randomly
                        if (bValidAsteroid)
                            current_state = 120;

                        string s = "";
                        s += "Front: ";
                        if (scanfrontScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanfrontScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += "\n";

                        s += "Back: ";
                        if (scanbackScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanbackScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += "\n";

                        s += "Left: ";
                        if (scanleftScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanleftScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += "\n";

                        s += "Right: ";
                        if (scanrightScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanrightScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += "\n";

                        s += "Top: ";
                        if (scantopScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scantopScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += "\n";

                        s += "Bottom: ";
                        if (scanbottomScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += scanbottomScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += "\n";

                        StatusLog(s, textPanelReport);

                        if (
                            scanfrontScanner.DoneScanning() &&
                            scanbackScanner.DoneScanning() &&
                            scanleftScanner.DoneScanning() &&
                            scanrightScanner.DoneScanning() &&
                            scantopScanner.DoneScanning() &&
                            scanbottomScanner.DoneScanning()
                            )
                        {
                            // all scans have run and didn't find asteroid..
                            //
                            setMode(MODE_ATTENTION);
                        }
                        break;
                    }
            }

        }
    }

}
