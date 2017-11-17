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

        void doModeSearchOrient()
        {
            if(current_state==0)
            {
                StatusLog(DateTime.Now.ToString() + " StartSearchOrient", textLongStatus, true);
                dtStartSearch = dtStartNav = DateTime.Now;
                ResetMotion();
                if (cargopcent > 99)
                {
                    setMode(MODE_DOCKING);
                    return;
                }
                double dist = (vCurrentPos - vLastContact).Length();
                if (dist < 14)
                {
                    setMode(MODE_SEARCHVERIFY);
                    return;
                }
                current_state = 10;
            }
            else if (current_state == 10)
            {
                ResetMotion();
                if (velocityShip < 0.2f) 
                {
//                    startNavWaypoint(vLastContact, true);
                    StatusLog(DateTime.Now.ToString() + " Aiming at " + Vector3DToString(vLastContact), textLongStatus, true);
                    current_state = 20;
                }
                else Echo("Waiting for motion");
            }
            else if(current_state==20)
            {
                bWantFast = true;
                if(GyroMain("forward",vLastContact-gpsCenter.GetPosition(),gpsCenter))
                {
                    ResetMotion();
                    setMode(MODE_SEARCHSHIFT);
                }
/*
                if (navStatus == null)
                {
                    ResetToIdle();
                    setAlertState(ALERT_ATTENTION);
                    throw new OurException("No nav Status block found");
                }
                string sStatus = navStatus.CustomName;
                if (sStatus.Contains("Done"))
                {
                    vLastExit = gpsCenter.GetPosition();
                    StartSearchShift();
                }
                */
            }

        }
    }
}