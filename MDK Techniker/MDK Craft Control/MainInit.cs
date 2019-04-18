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

        double gridBaseMass = 0;

        string doInit()
        {

            //             Echo("InitA:" + currentInit + ":"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
            // initialization of each module goes here:

            // when all initialization is done, set init to true.

            if (bStartupError)
            {
                Echo("(RE)INIT:" + sStartupError);
            }
            Log("Init:" + currentInit.ToString());
//            Echo(gtsAllBlocks.Count.ToString() + " Blocks");
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

            do
            {
                //                Echo("Init:" + currentInit);
                echoInstructions("Init:" + currentInit + " ");
                switch (currentInit)
                {
                    case 0:
                        sStartupError = "";
                        if (bStartupError) gridsInit(); // check the entire grid again
                        bStartupError = false;
                        StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);
                        break;
                    case 2:
                        sInitResults += gridsInit();
                        break;
                    case 3:
                        initLogging();
                        break;
                    case 4:
                        initTimers();
                        break;
                    case 5:
                        sInitResults += SerializeInit();
                        Deserialize(); // get info from savefile to avoid blind-rewrite of (our) defaults
                        break;
                    case 6:
                        sInitResults += DefaultOrientationBlockInit();
                        break;
                    case 7:
                        initCargoCheck();
                        break;
                    case 8:
                        initPower();
                        break;
                    case 9:
                        sInitResults += thrustersInit(shipOrientationBlock);
                        break;
                    case 10:
                        sInitResults += gyrosetup();
                        GyroControl.UpdateGyroList(gyros);
                        break;
                    case 11:
                        if (shipOrientationBlock is IMyRemoteControl)
                        {
                            Vector3D playerPosition;
                            bool bGotPlayer = ((IMyRemoteControl)shipOrientationBlock).GetNearestPlayer(out playerPosition);
                            IMyRemoteControl myR = (IMyRemoteControl)shipOrientationBlock;
                            myR.SetCollisionAvoidance(false);
                            myR.SetDockingMode(false);
                            myR.Direction = Base6Directions.Direction.Forward;
                            myR.FlightMode = FlightMode.OneWay;
                            myR.ClearWaypoints();
                            /*
                            if (bGotPlayer)
                            {
                                // we are a pirate faction.  chase the player.
                                myR.AddWaypoint(playerPosition, "Name");
                                myR.SetAutoPilotEnabled(true);
                                setMode(MODE_ATTACK);
                            }
                            */
                        }
                        break;
                    case 12:
                        sInitResults += wheelsInit(shipOrientationBlock);
                        break;
                    case 13:
                        sInitResults += rotorsNavInit();
                        break;
                    case 14:
                        sInitResults += connectorsInit();
                        break;
                    case 15:
                        sInitResults += tanksInit();
                        break;
                    case 16:
                        sInitResults += drillInit();
                        sInitResults += controllersInit();
                        break;
                    case 17:
                        sInitResults += SensorInit(shipOrientationBlock);
                        break;
                    case 18:
                        sInitResults += ejectorsInit();
                        break;
                    case 19:
                        sInitResults += antennaInit();
                        break;
                    case 20:
                        sInitResults += gasgenInit();
                        sInitResults += camerasensorsInit(shipOrientationBlock);
                        sInitResults += airventInit();
                        break;
                    case 21:
                        autoConfig();
                        break;
                    case 22:
                        //                        bWantFast = false;

                        if (bGotAntennaName)
                            sBanner = "*" + OurName + ":" + moduleName + " V" + sVersion + " ";

                        if (sBanner.Length > 34)
                        {
                            sBanner = OurName + ":" + moduleName + "\nV" + sVersion + " ";
                        }
                        if (shipOrientationBlock is IMyShipController)
                        {
                            MyShipMass myMass;
                            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();

                            gridBaseMass = myMass.BaseMass;
                        }

                        sInitResults += modeOnInit(); // handle mode initializing from load/recompile..
                        break;
                    case 23:
                        // do startup error check
                        init = true; // we are done
                        sStartupError = "";
                        bStartupError = false;
                        if (shipOrientationBlock == null)
                        {
                            bStartupError = true;
                            sStartupError += "\nNo Ship Controller";
                        }
                        if (ionThrustCount < 1 && hydroThrustCount < 1 && atmoThrustCount < 1)
                        {
                            // no thrusters
                            if (wheelSledList.Count < 1)
                            {
                                // no sled wheels && no thrusters
                                if (rotorNavRightList.Count < 1)
                                {
                                    bStartupError = true;
                                    sStartupError += "\nNo Propulsion Method Found";
                                    sStartupError += "\nNo Thrusters.\nNo NAV Rotors\nNo Sled Wheels";

                                }
                            }
                            else
                            {
                                // sled wheels, but not thrusters...
                                bStartupError = true;
                                sStartupError += "\nNo Valid Propulsion Method Found";
                                sStartupError += "\nSled wheels, but No Thrusters.\nNo NAV Rotors";
                            }
                        }
                        else
                        {
                            // we DO have thrusters
                            if (gyros.Count < 1)
                            {
                                // thrusters, but no gyros
                                bStartupError = true;
                                sStartupError += "\nNo Gyros Found";
                            }
                            // check for sled wheels?
                            if (shipOrientationBlock is IMyShipController)
                            {
                                // can check gravity..
                            }
                        }
                        // check for [WCCS] timer, but no Wico Craft Save.. and vice-versa
                        if (TimerTriggerFind(sSubModuleTimer))
                        {
                            // there is a submodule timer trigger
                            if (SaveFile == null)
                            { // no save text panel

                                bStartupError = true;
                                sStartupError += "\nSubmodule timer, but no text\n panel named:" + SAVE_FILE_NAME;

                            }
                        }
                        else
                        {
                            if (bSupportSubModules)
                            {
                                bStartupError = true;
                                sStartupError += "\nSubmodules Enabled, but no\n timer containing:" + sSubModuleTimer;
                                if (SaveFile == null)
                                { // no save text panel

                                    bStartupError = true;
                                    sStartupError += "\n No text\n panel containing:" + SAVE_FILE_NAME;

                                }

                            }
                        }
                        if (!bStartupError)
                        {
                            init = true;
                        }
                        else
                        {
                            currentInit = -1; // start init all over again
                        }
                        break;
                }
                currentInit++;
                //               echoInstructions("EInit:" + currentInit + " | ");
                //               Echo("%=" + (float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount);
            }
            while (!init && (((float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount) < 0.2f));
/*
            if (currentInit == 0)
            {
//            Echo("Init0:" + currentInit + ":"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                //        StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

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
                initShipDim(shipOrientationBlock);

                sInitResults += modeOnInit(); // handle mode initializing from load/recompile..

                if(currentInit==5)
                {
                    sStartupError = "";
                    bStartupError = false;
                    if (shipOrientationBlock == null)
                    {
                        bStartupError = true;
                        sStartupError += "\nNo Ship Controller";
                    }
                    if (ionThrustCount < 1 && hydroThrustCount < 1 && atmoThrustCount < 1)
                    {
                        // no thrusters
                        if (wheelSledList.Count < 1)
                        {
                            // no sled wheels && no thrusters
                            if (rotorNavRightList.Count < 1)
                            {
                                bStartupError = true;
                                sStartupError += "\nNo Propulsion Method Found";
                                sStartupError += "\nNo Thrusters.\nNo NAV Rotors\nNo Sled Wheels";

                            }
                        }
                        else
                        {
                            // sled wheels, but not thrusters...
                            bStartupError = true;
                            sStartupError += "\nNo Valid Propulsion Method Found";
                            sStartupError += "\nSled wheels, but No Thrusters.\nNo NAV Rotors";
                        }
                    }
                    else
                    {
                        // we DO have thrusters
                        if (gyros.Count < 1)
                        {
                            // thrusters, but no gyros
                            bStartupError = true;
                            sStartupError += "\nNo Gyros Found";
                        }
                        // check for sled wheels?
                        if (shipOrientationBlock is IMyShipController)
                        {
                            // can check gravity..
                        }
                    }
                    // check for [WCCS] timer, but no Wico Craft Save.. and vice-versa
                    if (TimerTriggerFind(sSubModuleTimer))
                    {
                        // there is a submodule timer trigger
                        if (SaveFile == null)
                        { // no save text panel

                            bStartupError = true;
                            sStartupError += "\nSubmodule timer, but no text\n panel named:" + SAVE_FILE_NAME;

                        }
                    }
                    else
                    {
                        if (bSubModules)
                        {
                            bStartupError = true;
                            sStartupError += "\nSubmodules Enabled, but no\n timer containing:" + sSubModuleTimer;
                            if (SaveFile == null)
                            { // no save text panel

                                bStartupError = true;
                                sStartupError += "\n No text\n panel containing:" + SAVE_FILE_NAME;

                            }

                        }
                    }

                }
                init = true; // we are done
            }

            currentInit++;
            */
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