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
            if (iMode == MODE_IDLE)
                doModeIdle();
            /*
            else if (iMode == MODE_DUMBNAV)
            {
                doModeNav();
            }
            */
            /*
            else if (iMode == MODE_SLEDMMOVE)
            {
            doModeSledmMove();
            }
            else if (iMode == MODE_SLEDMRAMPD)
            {
            doModeSledmRampD();
            }
            else if (iMode == MODE_SLEDMLEVEL)
            {
            doModeSledmLevel();
            }
            else if (iMode == MODE_SLEDMDRILL)
            {
            doModeSledmDrill();
            }
            else if (iMode == MODE_SLEDMBDRILL)
            {
            doModeSledmBDrill();
            }
            else if (iMode == MODE_DOCKING)
            {
                // doModeDocking();
            }
            else if (iMode == MODE_DOCKED)
            {
                doModeDocked();
            }
            else if (iMode == MODE_LAUNCH)
            {
                // doModeLaunch();
            }
            else if (iMode == MODE_PET)
            {
                doModePet();
            }
                */

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
            if (AnyConnectorIsConnected() && iMode != MODE_LAUNCH && iMode != MODE_RELAUNCH && !((craft_operation & CRAFT_MODE_ORBITAL) > 0) && !((craft_operation & CRAFT_MODE_NAD) > 0))
                setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(OurName + ":" + moduleName + ":Manual Control (idle)", textPanelReport);

            if (AnyConnectorIsConnected() && iMode != MODE_LAUNCH && iMode != MODE_RELAUNCH && !((craft_operation & CRAFT_MODE_ORBITAL) > 0) && !((craft_operation & CRAFT_MODE_NAD) > 0))
                setMode(MODE_DOCKED);
        }
        #endregion



        double calculateStoppingDistance(List<IMyTerminalBlock> thrustUpList, double currentV, double dGrav)
        {
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double hoverthrust = 0;
            hoverthrust = myMass.PhysicalMass * dGrav * 9.810;
            double maxThrust = calculateMaxThrust(thrustUpList);
            double maxDeltaV = (maxThrust - hoverthrust) / myMass.TotalMass;
            double secondstozero = currentV / maxDeltaV;
            Echo("secondstozero=" + secondstozero.ToString("0.00"));
            double stoppingM = currentV / 2 * secondstozero;
            Echo("stoppingM=" + stoppingM.ToString("0.00"));
            return stoppingM;
        }

    }
}