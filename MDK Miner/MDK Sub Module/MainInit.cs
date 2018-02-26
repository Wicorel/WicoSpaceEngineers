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

            /*
        	if(currentInit==0) initLogging();

//            Log("Init:" + currentInit.ToString());
            double progress = currentInit * 100 / 3;
            string sProgress = progressBar(progress);
            StatusLog(moduleName + sProgress, textPanelReport);
            */
            do
            {
                echoInstructions("Init:" + currentInit + " | ");
                switch (currentInit)
                {
                    case 0:
                        sInitResults += gridsInit();
                        break;
                    case 1:

                        /*
                         * add commands to set modes
                        if(!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
                        */
                        modeCommands.Clear();
                        if (!modeCommands.ContainsKey("findore")) modeCommands.Add("findore", MODE_FINDORE);
                        if (!modeCommands.ContainsKey("doscan")) modeCommands.Add("doscan", MODE_DOSCAN);
                        if (!modeCommands.ContainsKey("mine")) modeCommands.Add("mine", MODE_MINE);
                        break;
                    case 2:
                        initLogging();
                        break;
                    case 3:
                        StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);
                        break;
                    case 4:
                        sInitResults += SerializeInit();
                        Deserialize();
                        break;
                    case 5:
                        break;
                    case 6:
                        sInitResults += DefaultOrientationBlockInit();
                        break;
                    case 7:
                        initShipDim(shipOrientationBlock);
                        break;
                    case 8:
                        sInitResults += connectorsInit();
                        break;
                    case 9:
                        sInitResults += thrustersInit(shipOrientationBlock);
                        break;
                    case 10:
                        sInitResults += camerasensorsInit(shipOrientationBlock);
                        break;
                    case 11:
                        sInitResults += sensorInit();
                        break;
                    case 12:
                        sInitResults += tanksInit();
                        break;
                    case 13:
                        sInitResults += gyrosetup();
                        break;
                    case 14:
                        GyroControl.UpdateGyroList(gyros);
                        break;
                    case 15:
                        sInitResults += drillInit();
                        break;
                    case 16:
                        sInitResults += ejectorsInit();
                        break;
                    case 17:
                        initCargoCheck();
                        break;
                    case 18:
                        initAsteroidsInfo();
                        break;
                    case 19:
                        Deserialize();
                        break;
                    case 20:
                        sInitResults += modeOnInit();
                        break;
                    case 21:
                        init = true;
                        break;
                    case 22:
                        break;

                }
                currentInit++;
                echoInstructions("EInit:" + currentInit + " | ");
                Echo("%=" + (float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount);
            }
            while (!init && (((float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount) < 0.5f));

            /*
            Echo("Init: " + currentInit.ToString());
            if (currentInit == 0)
            {
                //StatusLog("clear",textLongStatus,true);
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                modeCommands.Clear();
                if (!modeCommands.ContainsKey("findore")) modeCommands.Add("findore", MODE_FINDORE);
                if (!modeCommands.ContainsKey("doscan")) modeCommands.Add("doscan", MODE_DOSCAN);
                if (!modeCommands.ContainsKey("mine")) modeCommands.Add("mine", MODE_MINE);

                gridsInit();
                //                sInitResults += initSerializeCommon();
                sInitResults += SerializeInit();
                Deserialize();
                sInitResults += DefaultOrientationBlockInit();
                initShipDim(shipOrientationBlock);
            }
            else if (currentInit == 1)
            {
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
            */
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