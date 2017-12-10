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

        #region docked
        // 0 = master init
        // 1 = inited.

        void doModeDocked()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":DOCKED!", textPanelReport);

            if (bAutoRelaunch && bValidDock)
            {
                Echo("Docked. Checking Relaunch");
                if (batteryPercentage > batterypcthigh && cargopcent < cargopctmin)
                {
                    setMode(MODE_RELAUNCH);//StartRelaunch();
                    return;
                }
                else
                    Echo(" Awaiting Relaunch Criteria");
            }

            if (!AnyConnectorIsConnected())
            {
                // we magically got disconnected..
                setMode(MODE_IDLE);
                powerDownThrusters(thrustAllList);
                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false,range);
                // Need battery management.
            }
            else
            {

                StatusLog(moduleName + ":Power Saving Mode", textPanelReport);
                if (current_state == 0)
                {
                    powerDownThrusters(thrustAllList, thrustAll, true);
                    antennaLowPower();
                    sleepAllSensors();
                    // turn gyos off?
                    current_state = 1;
                }
 //               Vector3D vVec = calcBlockForwardVector(gpsCenter);
//                IMyTerminalBlock otherConnector = getConnectedConnector();
            }

        }
        #endregion

    }
}