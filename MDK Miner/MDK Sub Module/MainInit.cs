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
//            bGotAntennaName = false;
        }
        void ModuleInitCustomData(INIHolder iniCustomData)
        {
            ConnectorInitCustomData(iniCustomData);
            MiningInitCustomData(iniCustomData);
            ThrustersInitCustomData(iniCustomData);
            SensorInitCustomData(iniCustomData);
            CamerasInitCustomData(iniCustomData);

            PowerInitCustomData(iniCustomData);
            CargoInitCustomData(iniCustomData);
        }

        string sInitResults = "";
        string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.

        	if(currentInit==0) initLogging();

//            Log("Init:" + currentInit.ToString());
            double progress = currentInit * 100 / 3;
            string sProgress = progressBar(progress);
            StatusLog(moduleName + sProgress, textPanelReport);

            Echo("Init: " + currentInit.ToString());
            if (currentInit == 0)
            {
                //StatusLog("clear",textLongStatus,true);
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                if (!modeCommands.ContainsKey("findore")) modeCommands.Add("findore", MODE_FINDORE);

                gridsInit();
                //                sInitResults += initSerializeCommon();
                sInitResults += SerializeInit();
                Deserialize();
        		initShipDim();
            }
            else if (currentInit == 1)
            {
                sInitResults += DefaultOrientationBlockInit();
		        sInitResults += connectorsInit();
		        sInitResults += thrustersInit(shipOrientationBlock);
		        sInitResults+=camerasensorsInit(shipOrientationBlock); 
		        sInitResults+=sensorInit(); 

//		        sInitResults += gearsInit();
		        sInitResults += tanksInit();
		        sInitResults += gyrosetup();
                GyroControl.UpdateGyroList(gyros);

		        sInitResults += drillInit();
		        sInitResults += ejectorsInit();
                initCargoCheck();
                initAsteroidsInfo();

                Deserialize();
//                bWantFast = false;
                sInitResults += modeOnInit();
                init = true;
            }

            currentInit++;
            if (init) currentInit = 0;

//            Log(sInitResults);
            Echo("Init exit");
            return sInitResults;

        }


        string modeOnInit()
        {
            // check current state and perform reload init to correct state
            if(iMode==MODE_FINDORE)
            {
                if (current_state == 410)
                    current_state = 400;// reinit
            }
            return ">";
        }



    }
}