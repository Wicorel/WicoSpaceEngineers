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
        /*
        0 init
        1 build in progress.
        10 completed. check for power, etc. turn off projector
        20 Start cut
        25 cut in progress.

        */
        void doModeUnderconstruction()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":Under Construction!", textPanelReport);
            Echo("Under Contruction: "+current_state);

            if (AnyConnectorIsLocked())
                ConnectAnyConnectors();

            if (current_state == 0)
            {
                powerDownThrusters(thrustAllList, thrustAll, true);
                antennaLowPower();
                sleepAllSensors();
                initCutters();
                initProjectors();
                // turn gyos off?
                current_state = 1;
            }
            else if (current_state == 1)
            {
                // recreate getLocalConnectors, but don't ignore connectors that are connected to 'us'
                connectorsInit();

                for (int i = 0; i < localDockConnectors.Count; i++)
                {
                    IMyShipConnector sc = localDockConnectors[i] as IMyShipConnector;
                    if (sc.Status == MyShipConnectorStatus.Connectable)
                    {
                        sc.ApplyAction("SwitchLock");
                    }
                    if (sc.Status == MyShipConnectorStatus.Connected)
                    {
                        Echo("Connected to Print Head!");
                        // try to pull stuff?

                        // or maybe use TIM?
                    }
                }
                if(doProjectorCheck())
                { // we are done projecting.
                    current_state = 10;
                }
            }
            else if (current_state == 10)
            { // turn off projectors.
                turnoffProjectors();
                // check for 'enough' power to continue alone.
                //TODO:
                current_state = 20;
            }
            else if (current_state == 20)
            { // start the cut 
                doCut();
                current_state = 25;
            }
            else if (current_state == 25)
            { // cut-off in progress
                doCut();
                if(calcGridSystemChanged())
                {
                    if (AnyConnectorIsConnected())
                        setMode(MODE_DOCKED);
                }
                // maybe need a time-out?
            }
        }
    }
}