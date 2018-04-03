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
        #region domodes
        void doModes()
        {
            Echo("mode=" + iMode.ToString());
            /*
                if ((craft_operation & CRAFT_MODE_PET) > 0 && iMode != MODE_PET)
                    setLightColor(lightsList, Color.Chocolate);

                if (AnyConnectorIsConnected() && iMode != MODE_LAUNCH && iMode != MODE_RELAUNCH && !((craft_operation & CRAFT_MODE_ORBITAL) > 0) && !((craft_operation & CRAFT_MODE_NAD) > 0))
                {
                    setMode(MODE_DOCKED);
                }
                */
            //           Echo("Grid Mass=" + gridBaseMass);

            if (iMode == MODE_IDLE) doModeIdle();
//            if (iMode == MODE_DOSCAN) doModeScans();
            else if (iMode == MODE_ATTENTION)
            {
                StatusLog("clear", textPanelReport);
                StatusLog(moduleName + ":ATTENTION!", textPanelReport);
                StatusLog(moduleName + ": current_state=" + current_state.ToString(), textPanelReport);
                StatusLog("\nCraft Needs attention", textPanelReport);

            }
        }
        #endregion


        #region modeidle
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            //    if (navCommand != null)
            //        if (!(navCommand is IMyTextPanel)) navCommand.CustomName ="NAV: C Wico Craft";
            //    if (navStatus != null) navStatus.CustomName=sNavStatus + " Control Reset";
            //bValidPlayerPosition = false;
            setMode(MODE_IDLE);
            if (gridBaseMass>0 && AnyConnectorIsConnected() && iMode != MODE_LAUNCH && iMode != MODE_RELAUNCH && !((craft_operation & CRAFT_MODE_ORBITAL) > 0) && !((craft_operation & CRAFT_MODE_NAD) > 0))
                setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(OurName + ":" + moduleName + ":Manual Control (idle)", textPanelReport);

            if (gridBaseMass > 0 && AnyConnectorIsConnected() && iMode != MODE_LAUNCH && iMode != MODE_RELAUNCH && !((craft_operation & CRAFT_MODE_ORBITAL) > 0) && !((craft_operation & CRAFT_MODE_NAD) > 0))
                setMode(MODE_DOCKED);
        }
        #endregion

    }
}