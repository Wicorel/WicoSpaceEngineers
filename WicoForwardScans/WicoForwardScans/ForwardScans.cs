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

        double currentScan = 15000;
        double shortRangeMax = 15000;
        double longRangeMax = 150000;
        double maxScan = 15000;

        bool bLongRange = false; // do long range scans

        void doForwardScans()
        {
            // do forward camera scans.
            //	Echo("CamerasForward:" + cameraForwardList.Count);
            //	Echo("Scanrange=" + currentScan.ToString());
            if (doCameraScan(cameraForwardList, currentScan))
            { // we did a scan.
              //		Echo("SCANNED!");
                if (lastDetectedInfo.IsEmpty())
                { // found nothing
                  //			Echo("Found Nothing!");
                    currentScan = maxScan;
                    //			currentScan *= 2;// 500; // increase scan range
                    if (currentScan > maxScan) currentScan = maxScan;
                }
                else
                {
                    if (lastDetectedInfo.HitPosition != null) // even though camera scanner, some objects dn't return hit position (trees).
                    {
                        // next scan try to do just past the detected item..
                        currentScan = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.HitPosition.Value) + 50;
                    }
                }
            }

        }

    }
}