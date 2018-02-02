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
            CamerasInitCustomData(iniCustomData);

            NavInitCustomData(iniCustomData);

//            GearsInitCustomData(iniCustomData);
        }


        string sInitResults = "";
        string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.

            // set autogyro defaults.
            LIMIT_GYROS = 1;
            minAngleRad = 0.09f;
            CTRL_COEFF = 0.75;

            Echo("Init:"+currentInit);
            if (currentInit == 0)
            {
                //StatusLog("clear",textLongStatus,true);
                StatusLog(DateTime.Now.ToString() + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                //                if (!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
                //	if(!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

                sInitResults += SerializeInit();

                Deserialize();
                sInitResults += gridsInit();
                sInitResults += DefaultOrientationBlockInit();
                initLogging();
            }
            else if (currentInit == 1)
            {
                sInitResults += thrustersInit(shipOrientationBlock);
                sInitResults += rotorsNavInit();
                sInitResults += wheelsInit(shipOrientationBlock);

                sInitResults += sensorInit();
                sInitResults += camerasensorsInit(shipOrientationBlock);
                sInitResults += connectorsInit();
            }
            else if(currentInit==2)
            { 
                sInitResults += gyrosetup(); 
                GyroControl.UpdateGyroList(gyros);
                GyroControl.SetRefBlock(shipOrientationBlock);

                sInitResults += lightsInit();
                sInitResults += camerasensorsInit(shipOrientationBlock);

                calculateGridBBPosition(shipOrientationBlock);
                initShipDim(shipOrientationBlock);
                sInitResults += modeOnInit(); // handle mode initializting from load/recompile..
                init = true;

            }

            currentInit++;
            if (init)
            {
                currentInit = 0;
                bWantFast = false;
            }

            Log(sInitResults);
//            Echo("XXInit:"+currentInit);

            return sInitResults;
        }

        string modeOnInit()
        {
            // check current state and perform reload init to correct state
            return ">";
        }


    }
}