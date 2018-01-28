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
//            CargoInitCustomData(iniCustomData);

            DockingInitCustomData(iniCustomData);
            RelaunchInitCustomData(iniCustomData);
            DockedInitCustomData(iniCustomData);
            LaunchInitCustomData(iniCustomData);
        }


        #region maininit

        string sInitResults = "";
        string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {

            if (currentInit == 0)
            {
                initLogging(); //also does gridsInit()
            }

            // set autogyro defaults.
            LIMIT_GYROS = 1;
            minAngleRad = 0.09f;
            CTRL_COEFF = 0.75;
/*
            Log("Init:" + currentInit.ToString());
            double progress = currentInit * 100 / 3;
            string sProgress = progressBar(progress);
            StatusLog(sProgress, getTextBlock(sTextPanelReport));
            */
            Echo("Init:" + currentInit.ToString());
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
                initShipDim();

                BaseInitInfo();

                sInitResults += modeOnInit();
                init = true;

            }

            currentInit++;
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