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
            CommunicationsInitCustomData(iniCustomData);
        }


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
//                echoInstructions("Init:" + currentInit + " | ");
                switch (currentInit)
                {
                    case 0:
                        sInitResults += gridsInit();
                        break;
                    case 1:
                        
                        /*
                         * add commands to set modes
                         * For Example:
                        if(!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
                        */
                        modeCommands.Clear();
                        if (!modeCommands.ContainsKey("findore")) modeCommands.Add("findore", MODE_FINDORE);
//                        if (!modeCommands.ContainsKey("doscan")) modeCommands.Add("doscan", MODE_DOSCAN);
                        if (!modeCommands.ContainsKey("mine")) modeCommands.Add("mine", MODE_MINE);
                        if (!modeCommands.ContainsKey("bore")) modeCommands.Add("bore", MODE_BORESINGLE);
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
                        sInitResults += SensorInit(shipOrientationBlock);
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
                    case 19: initOreLocInfo();
                        break;
                    case 20:
                        Deserialize();
                        break;
                    case 21:
                        initPower();
                        break;
                    case 22:
                        tanksInit();
                        break;
                    case 23:
                        sInitResults += modeOnInit();
                        break;
                    case 24:
                        if (sensorsList.Count < 2)
                        {
                            //                            bStartupError = true;
                            sStartupError += "\nNot enough Sensors detected!";
                        }
                        if (!HasDrills())
                        {
                            //                            bStartupError = true;
                            sStartupError += "\nNo Drills found!";
                        }
                        break;
                    case 25:
                        init = true;
                        break;

                }
                currentInit++;
//                echoInstructions("EInit:" + currentInit + " | ");
                Echo("%=" + (float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount);
            }
            while (!init && (((float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount) < 0.5f));

            if (init) currentInit = 0;

//            Log(sInitResults);
            Echo("Init exit");
            return sInitResults;

        }

        string modeOnInit()
        {
            // check current state and perform reload init to correct state
            MinerCalculateBoreSize();
            if (miningAsteroidID > 0)
            {
                MinerCalculateAsteroidVector(miningAsteroidID);
                //                vAsteroidBoreEnd = AsteroidCalculateBoreEnd();
                //                vAsteroidBoreStart = AsteroidCalculateBoreStart();
                MinerCalculateBoreSize();
//                AsteroidCalculateBestStartEnd(); can swap start/end.  Dont want to do that when in the tunnel.
            }
            if (iMode==MODE_FINDORE)
            {
                if (current_state == 35)
                {
                    current_state = 31;
                }
                else if (current_state == 143) // testing
                    current_state = 120; // go back to start of bore
                else if (current_state == 145) // bore scans
                    current_state = 140; // reinit bore scan
            }
            return ">";
        }



    }
}