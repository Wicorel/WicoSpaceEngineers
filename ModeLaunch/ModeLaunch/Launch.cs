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
        List<IMyTerminalBlock> thrustLaunchBackwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustLaunchForwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustLaunchLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustLaunchRightList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustLaunchUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustLaunchDownList = new List<IMyTerminalBlock>();

        void doModeLaunch()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":LAUNCH!", textPanelReport);
            if (current_state == 0)
            {
                StatusLog(DateTime.Now.ToString() + " ACTION: StartLaunch", textLongStatus, true);
                StatusLog(moduleName + ":Start Launch", textPanelReport);
/*
                Echo("#LocalDock=" + localDockConnectors.Count);
                for (int i = 0; i < localDockConnectors.Count; i++)
                {
                    Echo(i + ":" + localDockConnectors[i].CustomName);
                }
                */
                if (!AnyConnectorIsConnected())
                {
                    StatusLog("Can't perform action unless docked", textLongStatus, true);
                    ResetMotion();
                    setMode(MODE_IDLE);
                    return;
                }
                else
                {
                    IMyTerminalBlock dockingConnector = getConnectedConnector(true);
//                    Echo("Using Connector=" + dockingConnector.CustomName);

                    thrustersInit(dockingConnector, ref thrustLaunchForwardList, ref  thrustLaunchBackwardList,
                        ref thrustLaunchDownList, ref thrustLaunchUpList,
                        ref thrustLaunchLeftList, ref thrustLaunchRightList);
                }
                vDock = ((IMyShipController)gpsCenter).CenterOfMass;
//                vDock = gpsCenter.GetPosition();
                powerDownThrusters(thrustAllList); // turns ON all thrusters.
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
                powerUpThrusters(thrustLaunchBackwardList);

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

                if (velocityShip > 2) powerUpThrusters(thrustLaunchBackwardList, 25);
                else powerUpThrusters(thrustLaunchBackwardList);
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