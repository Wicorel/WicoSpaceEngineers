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
        void doModuleConstructor()
        {
            // called from main constructor.

        }
        void ModuleInitCustomData(INIHolder iniCustomData)
        {
            ConnectorInitCustomData(iniCustomData);
            ThrustersInitCustomData(iniCustomData);
            GyroInitCustomData(iniCustomData);
 //           CamerasInitCustomData(iniCustomData);
            //            GearsInitCustomData(iniCustomData);
        }


        #region maininit

        string sInitResults = "";
        string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.

            if (currentInit == 0) initLogging();
            Log("Init:" + currentInit.ToString());
            double progress = currentInit * 100 / 3;
            string sProgress = progressBar(progress);
            StatusLog(moduleName + sProgress, textPanelReport);

            Echo("Init:" + currentInit);
            if (currentInit == 0)
            {
                //StatusLog("clear",textLongStatus,true);
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                if (!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
                if (!modeCommands.ContainsKey("orbitallaunch")) modeCommands.Add("orbitallaunch", MODE_ORBITALLAUNCH);
                // if(!modeCommands.ContainsKey("orbitaldescent")) modeCommands.Add("orbitaldescent", MODE_DESCENT);
                gridsInit();
                //               initLogging();
                sInitResults += SerializeInit();
                Deserialize();
            }
            else if (currentInit == 1)
            {
                sInitResults += DefaultOrientationBlockInit();

                sInitResults += thrustersInit(shipOrientationBlock);
                sInitResults += connectorsInit();
                sInitResults += gyrosetup();
                sInitResults += lightsInit();
                initShipDim(shipOrientationBlock);

                Deserialize();
                //                bWantFast = false;
                sInitResults += modeOnInit();
                init = true;
            }

            currentInit++;
            if (init)
            {
                currentInit = 0;
                bWantFast = false;
            }

            Log(sInitResults);
            return sInitResults;

        }

 //       IMyTextPanel gpsPanel = null;


        #endregion

        string modeOnInit()
        {
            if (iMode == MODE_UNDERCONSTRUCTION)
            {
                if (current_state == 25)
                {
                    // we have inited while in cut-off mode
                    doCut(false);
                    if (!AnyConnectorIsConnected() && AnyConnectorIsLocked())
                    {
                        ConnectAnyConnectors();
                        ResetMotion();
                        setMode(MODE_DOCKED);
                    }
                }
                else
                    current_state = 0;
            }
            // check current state and perform reload init to correct state
            return ">";
        }


    }
}