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
//            bDisableLogging = true;

        }
        void ModuleInitCustomData(INIHolder iniCustomData)
        {
            ConnectorInitCustomData(iniCustomData);
            ThrustersInitCustomData(iniCustomData);
            GyroInitCustomData(iniCustomData);
            CamerasInitCustomData(iniCustomData);
            SensorInitCustomData(iniCustomData);
            PowerInitCustomData(iniCustomData);
            CargoInitCustomData(iniCustomData);
            CommunicationsInitCustomData(iniCustomData);

            NavInitCustomData(iniCustomData);

            DockingInitCustomData(iniCustomData);

            DockedInitCustomData(iniCustomData);
            LaunchInitCustomData(iniCustomData);
        }


        #region maininit

        string doInit()
        {
            do
            {
                Echo("Init:" + currentInit.ToString());
                switch (currentInit)
                {
                    case 0:
                        sInitResults += gridsInit();
                        break;
                    case 1:
                        if (!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
                        if (!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

                        break;
                    case 2:
                        initLogging();
                        StatusLog(DateTime.Now.ToString() + OurName + ":" + moduleName + ":INIT", textLongStatus, true);
                        break;
                    case 3:
                        sInitResults += SerializeInit();
                        Deserialize();
                        break;
                    case 4:
                        sInitResults += DefaultOrientationBlockInit();
                        break;
                    case 5:
                        sInitResults += thrustersInit(shipOrientationBlock);
                        break;
                    case 6:
                        sInitResults += rotorsNavInit();
                        break;
                    case 7:
                        sInitResults += SensorInit(shipOrientationBlock);
                        break;
                    case 8:
                        sInitResults += camerasensorsInit(shipOrientationBlock);
                        break;
                    case 9:
                        sInitResults += connectorsInit();
                        break;
                    case 10:
                        sInitResults += gyrosetup();
                        // set autogyro defaults.
                        LIMIT_GYROS = 1;
                        minAngleRad = 0.09f;
                        CTRL_COEFF = 0.75;
                        break;
                    case 11:
                        sInitResults += lightsInit();
                        break;
                    case 12:
                        initShipDim(shipOrientationBlock);
                        break;
                    case 13:
                        BaseInitInfo();
                        break;
                    case 14:
                        initCargoCheck();
                        break;
                    case 15:
                        sInitResults += modeOnInit();
                        init = true;
                        break;
                    case 16:
                        break;
                    case 17:
                        break;
                    case 18:
                        break;
                    case 19:
                        break;
                }
                currentInit++;
            }
            while (!init && (((float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount) < 0.5f)) ;
/*
            if (currentInit == 0)
            {
                //StatusLog("clear",textLongStatus,true);
                StatusLog(DateTime.Now.ToString() + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                if (!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
                if (!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

//                sInitResults += gridsInit();
                initTimers();

                //                sInitResults += initSerializeCommon();
                sInitResults += SerializeInit();

                Deserialize();
                sInitResults += gridsInit();
                sInitResults += DefaultOrientationBlockInit();

                sInitResults += thrustersInit(shipOrientationBlock);
                sInitResults += rotorsNavInit();
                sInitResults += sensorInit();
                sInitResults += camerasensorsInit(shipOrientationBlock);

                sInitResults += connectorsInit();
                sInitResults += gyrosetup();

                sInitResults += lightsInit();
                initShipDim(shipOrientationBlock);

                BaseInitInfo();

                sInitResults += modeOnInit();
                init = true;

            }

            currentInit++;
            */
            if (init) currentInit = 0;

            Log(sInitResults);
            Echo(sInitResults);

            return sInitResults;
        }


        #endregion

        string modeOnInit()
        {
            if (iMode == MODE_DOCKING)
            {
                ResetMotion();
                current_state = 0;
            }
            return ">";
        }

    }
}