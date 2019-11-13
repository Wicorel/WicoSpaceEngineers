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
        void doModeRelaunch()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":RELAUNCH!", textPanelReport);
            Echo("Relaunch countdown in progress");
            if (current_state == 0)
            {
                StatusLog(DateTime.Now.ToString() + " ACTION: ReLaunch", textLongStatus, true);
                if (!AnyConnectorIsConnected())
                {
                    StatusLog("Can't perform action unless docked", textLongStatus, true);
                    ResetMotion();
                    setMode(MODE_IDLE);// ResetToIdle();
                    return;
                }
                doSubModuleTimerTriggers("[RELAUNCH]");
                /*
                if (!bValidTarget && !bValidInitialContact && !bValidAsteroid)
                {
                    return;
                }*/
                //                setMode(MODE_RELAUNCH);
                dtRelaunchActionStart = DateTime.Now;
                current_state = 1;
                Serialize();
                return;
            }
            // delay launch
            DateTime dtMaxWait = dtRelaunchActionStart.AddSeconds(5.0f);
            DateTime dtNow = DateTime.Now;
            if (DateTime.Compare(dtNow, dtMaxWait) > 0)
            {
                setMode(MODE_LAUNCH);
            }
        }
    }
}
