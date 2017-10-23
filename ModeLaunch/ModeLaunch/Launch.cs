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
        #region launch

        void doModeLaunch()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":LAUNCH!", textPanelReport);
            if (current_state == 0)
            {
                StatusLog(DateTime.Now.ToString() + " ACTION: StartLaunch", textLongStatus, true);
                StatusLog(moduleName + ":Start Launch", textPanelReport);

                if (!AnyConnectorIsConnected())
                {
                    StatusLog("Can't perform action unless docked", textLongStatus, true);
                    ResetMotion();
                    setMode(MODE_IDLE);
                    return;
                }
                vDock = ((IMyShipController)gpsCenter).CenterOfMass;
//                vDock = gpsCenter.GetPosition();
                powerDownThrusters(thrustAllList);
                antennaMaxPower();
                current_state = 100;
                return;
            }
            if (AnyConnectorIsLocked() || AnyConnectorIsConnected())
            {
                StatusLog(moduleName + ":Awaiting Disconnect", textPanelReport);
                Echo("Awaiting Disconnect");

                ConnectAnyConnectors(false, "OnOff_Off");

                return;
            }
            if (current_state == 100)
            {
                powerUpThrusters(thrustForwardList);

                current_state = 1;
            }

//            Vector3D vPos = gpsCenter.GetPosition();
            Vector3D vPos = ((IMyShipController)gpsCenter).CenterOfMass;

            Echo("vDock=" + Vector3DToString(vDock));
            Echo("vPos=" + Vector3DToString(vPos));

            double dist = (vPos - vDock).LengthSquared();
            StatusLog(moduleName + ":Distance Launched=" + dist.ToString("0.00") + "m", textPanelReport);
            Echo(moduleName + ":Distance Launched=" + dist.ToString("0.00") + "m");

            if (dist > 10)
            {
                ConnectAnyConnectors(true, "OnOff_On");
            }
            {

                if (velocityShip > 2) powerUpThrusters(thrustForwardList, 25);
                else powerUpThrusters(thrustForwardList);

            }
            if (dist > 45)
            {
                ResetMotion();
                if (bValidTarget || bValidAsteroid) setMode(MODE_GOINGTARGET);//ActionGoMine();
                else
                {
                    setMode(MODE_INSPACE);
                }
            }
        }
        #endregion

    }
}