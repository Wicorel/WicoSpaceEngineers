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

        // Change per module

        void doModuleConstructor()
        {
            // called from main constructor.
            sTextPanelReport = "LCD Bridge R";
            OurName = "Wico Techniker";
            sOrientationBlockNamed = "Remote Control Techniker";

            initCustomData();

        }
        void ModuleInitCustomData(INIHolder iniCustomData)
        {
            ConnectorInitCustomData(iniCustomData);
            ThrustersInitCustomData(iniCustomData);
            GyroInitCustomData(iniCustomData);
            CamerasInitCustomData(iniCustomData);
            GearsInitCustomData(iniCustomData);

        }

        #region maininit

        string sInitResults = "";
        int currentInit = 0;

        double gridBaseMass = 0;

        string doInit()
        {

//             Echo("InitA:" + currentInit + ":"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
           // initialization of each module goes here:

            // when all initialization is done, set init to true.

            Log("Init:" + currentInit.ToString());
            Echo(gtsAllBlocks.Count.ToString() + " Blocks");
            /*
            double progress = currentInit * 100 / 3; // 3=Number of expected INIT phases.
            string sProgress = progressBar(progress);
            StatusLog(moduleName + sProgress, textPanelReport);
            */
            if (shipOrientationBlock != null)
            {
 //               anchorPosition = shipOrientationBlock;
 //               currentPosition = anchorPosition.GetPosition();
            }
//            Echo("InitB:" + currentInit + ":"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
            if (currentInit == 0)
            {
//            Echo("Init0:" + currentInit + ":"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                //        StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                /*
                if(!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
                */
                sInitResults += gridsInit();
                initLogging();
                initTimers();
                sInitResults += SerializeInit();

                Deserialize(); // get info from savefile to avoid blind-rewrite of (our) defaults
            }
            else if (currentInit == 1)
            {
                Deserialize();// get info from savefile to avoid blind-rewrite of (our) defaults

                sInitResults += DefaultOrientationBlockInit();
                initCargoCheck();
                initPower();
                sInitResults += thrustersInit(shipOrientationBlock);
                sInitResults += gyrosetup();
                GyroControl.UpdateGyroList(gyros);
                if (gtsAllBlocks.Count < 300) currentInit = 2; // go ahead and do next step.
            }
            if (currentInit == 2)
            {
                sInitResults += wheelsInit(shipOrientationBlock);
                sInitResults += rotorsNavInit();
                sInitResults += wheelsInit(shipOrientationBlock);
                sInitResults += sensorInit(true);

                if (gtsAllBlocks.Count < 100) currentInit = 3; // go ahead and do next step.
            }
            if (currentInit == 3)
            {
               sInitResults += connectorsInit();
                sInitResults += tanksInit();
                sInitResults += drillInit();
                sInitResults += controllersInit();
                if (gtsAllBlocks.Count < 100) currentInit = 4; // go ahead and do next step.
            }
            if (currentInit == 4)
            {
                sInitResults += ejectorsInit();
                sInitResults += antennaInit();
                sInitResults += gasgenInit();
                sInitResults += camerasensorsInit(shipOrientationBlock);
                sInitResults += airventInit();

                //        Serialize();

                autoConfig();
                bWantFast = false;

                if (bGotAntennaName)
                   sBanner = "*" + OurName + ":" + moduleName + " V" + sVersion + " ";

                if(sBanner.Length>34)
                {
                    sBanner = OurName + ":" + moduleName + "\nV" + sVersion + " ";
                }

                if (shipOrientationBlock is IMyShipController)
                {
                    MyShipMass myMass;
                    myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();

                    gridBaseMass = myMass.BaseMass;
                }////
                initShipDim();

                sInitResults += modeOnInit(); // handle mode initializing from load/recompile..

                init = true; // we are done
            }

            currentInit++;
            if (init) currentInit = 0;

            Log(sInitResults);

            return sInitResults;
        }


        string modeOnInit()
        {
            return ">";
        }


        #endregion

        string sTechnikerSection = "TECHKNIKER";
        void initCustomData()
        {
            INIHolder iniCustomData = new INIHolder(this, Me.CustomData);

            string sValue="";

            iniCustomData.GetValue(sTechnikerSection, "DoForwardScans", ref bDoForwardScans, true);
            iniCustomData.GetValue(sTechnikerSection, "CheckGasGens", ref bCheckGasGens, true);
            iniCustomData.GetValue(sTechnikerSection, "TechnikerCalcs", ref bTechnikerCalcs, true);
            iniCustomData.GetValue(sTechnikerSection, "GPSFromEntities", ref bGPSFromEntities, true);
            iniCustomData.GetValue(sTechnikerSection, "AirVents", ref bAirVents, true);

//            iniCustomData.GetValue(sTechnikerSection, "thrustignore", ref sIgnoreThruster, true);
            if(iniCustomData.GetValue(sTechnikerSection, "shipname", ref sValue))
            {
                OurName = "Wico " + sValue;
                bGotAntennaName = true;
            }

            iniCustomData.GetValue(sTechnikerSection, "shortrangemax", ref shortRangeMax, true);
            iniCustomData.GetValue(sTechnikerSection, "longrangemax", ref longRangeMax,true);

            ThrustersInitCustomData(iniCustomData);
            SensorInitCustomData(iniCustomData);

            if (iniCustomData.IsDirty)
            {
                Me.CustomData = iniCustomData.GenerateINI(true);
            }
            
            if (bLongRange)
		        maxScan = longRangeMax;
	        else
		        maxScan = shortRangeMax;

        }



    }
}