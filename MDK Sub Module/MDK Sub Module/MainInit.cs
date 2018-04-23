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
//            ConnectorInitCustomData(iniCustomData);

//            ThrustersInitCustomData(iniCustomData);
//            SensorInitCustomData(iniCustomData);
//            CamerasInitCustomData(iniCustomData);

//            PowerInitCustomData(iniCustomData);
//            CargoInitCustomData(iniCustomData);
        }

        #region maininit

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.

        	if(currentInit==0) initLogging();

            Log("Init:" + currentInit.ToString());
            double progress = currentInit * 100 / 3;
            string sProgress = progressBar(progress);
            StatusLog(moduleName + sProgress, textPanelReport);

            Echo("Init");
            if (currentInit == 0)
            {
                //StatusLog("clear",textLongStatus,true);
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

//                if (!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
//                if (!modeCommands.ContainsKey("orbitallaunch")) modeCommands.Add("orbitallaunch", MODE_ORBITALLAUNCH);
                // if(!modeCommands.ContainsKey("orbitaldescent")) modeCommands.Add("orbitaldescent", MODE_DESCENT);
                gridsInit();
                sInitResults += SerializeInit();
                Deserialize();
            }
            else if (currentInit == 1)
            {
                sInitResults += DefaultOrientationBlockInit();
                /*
		        sInitResults += connectorsInit();
		        sInitResults += thrustersInit(gpsCenter);
		        sInitResults+=camerasensorsInit(gpsCenter); 

		        sInitResults += gearsInit();
		        sInitResults += tanksInit();
		        sInitResults += gyrosetup();
                */
                Deserialize();
                bWantFast = false;
                sInitResults += modeOnInit();
                init = true;
            }

            currentInit++;
            if (init) currentInit = 0;

            Log(sInitResults);

            return sInitResults;

        }


        #endregion

        string modeOnInit()
        {
            // check current state and perform reload init to correct state
            return ">";
        }


    }
}