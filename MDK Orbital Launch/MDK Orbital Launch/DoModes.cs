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

            if (
                iMode != MODE_ORBITALLAUNCH 
                && AnyConnectorIsConnected() 
                && !((craft_operation & CRAFT_MODE_ORBITAL) > 0)
                )
            {
                Echo("DM:docked");
                setMode(MODE_DOCKED);
            }
            if (dGravity > 0 && iMode == MODE_INSPACE)
            {
                setMode(MODE_IDLE);
            }
            if (iMode == MODE_IDLE) doModeIdle();
            else if (iMode == MODE_HOVER) doModeHover();
            else if (iMode == MODE_LAUNCHPREP) doModeLaunchprep();
            //else if (iMode == MODE_INSPACE) doModeInSpace();
            else if (iMode == MODE_LANDED) doModeLanded();
            else if (iMode == MODE_ORBITALLAUNCH) doModeOrbitalLaunch();
            // else if (iMode == MODE_DESCENT) doModeDescent();
        }
        #endregion

        void doModeLanded()
        {

        }

        #region modeidle
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
//            if (navCommand != null)
 //               if (!(navCommand is IMyTextPanel)) navCommand.CustomName = "NAV: C Wico Craft";
//            if (navStatus != null) navStatus.CustomName = sNavStatus + " Control Reset";
            // bValidPlayerPosition=false;
            setMode(MODE_IDLE);
            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
//            StatusLog("clear",textPanelReport);

//            StatusLog(moduleName + " Manual Control", textPanelReport);
//            if ((craft_operation & CRAFT_MODE_ORBITAL) > 0)
            {
                if (dGravity <= 0)
                {
                    /*
                     *   We only handle planet modes.
                    if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
                    else
                    {
                        setMode(MODE_INSPACE);
                        gyrosOff();
                    }
                    */
                }
                else
                {
                    if (AnyConnectorIsConnected()) setMode(MODE_LAUNCHPREP);
                    else
                        setMode(MODE_HOVER);
                }
            }
        }
        #endregion
        void ResetMotion(bool bNoDrills = false)
        {
            Echo("RESETMOTION!");
	        powerDownThrusters(thrustAllList);
            gyrosOff();
	        if (shipOrientationBlock is IMyRemoteControl) ((IMyRemoteControl)shipOrientationBlock).SetAutoPilotEnabled(false);
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;

        }

    }
}