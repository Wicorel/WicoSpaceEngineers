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
            sGPSCenter = "Remote Control Techniker";

            initCustomData();

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
            if (gpsCenter != null)
            {
                anchorPosition = gpsCenter;
                currentPosition = anchorPosition.GetPosition();
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
                sInitResults += initSerializeCommon();

                Deserialize(); // get info from savefile to avoid blind-rewrite of (our) defaults
            }
            else if (currentInit == 1)
            {
                Deserialize();// get info from savefile to avoid blind-rewrite of (our) defaults

                sInitResults += BlockInit();
                initCargoCheck();
                if (gpsCenter != null)
                {
                    anchorPosition = gpsCenter;
                    currentPosition = anchorPosition.GetPosition();
                }
                initPower();
                sInitResults += thrustersInit(gpsCenter);
                sInitResults += gyrosetup();
                if (gtsAllBlocks.Count < 300) currentInit = 2; // go ahead and do next step.
            }
            if (currentInit == 2)
            {
                sInitResults += wheelsInit(gpsCenter);
                sInitResults += rotorsNavInit();

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
                sInitResults += camerasensorsInit(gpsCenter);
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

                if (anchorPosition != null)
                {
                    MyShipMass myMass;
                    myMass = ((IMyShipController)anchorPosition).CalculateShipMass();

                    gridBaseMass = myMass.BaseMass;
                }////

                sInitResults += modeOnInit(); // handle mode initializing from load/recompile..

                init = true; // we are done
            }

            currentInit++;
            if (init) currentInit = 0;

            Log(sInitResults);

            return sInitResults;
        }

        IMyTextPanel gpsPanel = null;

        string BlockInit()
        {
            string sInitResults = "";

            List<IMyTerminalBlock> centerSearch = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(sGPSCenter, centerSearch, localGridFilter);
            if (centerSearch.Count == 0)
            {
                centerSearch = GetBlocksContains<IMyRemoteControl>("[NAV]");
                if (centerSearch.Count == 0)
                {
                    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(centerSearch, localGridFilter);
                    if (centerSearch.Count == 0)
                    {
                        GridTerminalSystem.GetBlocksOfType<IMyCockpit>(centerSearch, localGridFilter);
                        //                GridTerminalSystem.GetBlocksOfType<IMyShipController>(centerSearch, localGridFilter);
                        int i = 0;
                        for (; i < centerSearch.Count; i++)
                        {
                            Echo("Checking Controller:" + centerSearch[i].CustomName);
                            if (centerSearch[i] is IMyCryoChamber)
                                continue;
                            break;
                        }
                        if (i > centerSearch.Count)
                        {
                            sInitResults += "!!NO valid Controller";
                            Echo("No Controller found");
                        }
                        else
                        {
                            sInitResults += "S";
                            Echo("Using good ship Controller: " + centerSearch[i].CustomName);
                        }
                    }
                    else
                    {
                        sInitResults += "R";
                        Echo("Using First Remote control found: " + centerSearch[0].CustomName);
                    }
                }
            }
            else
            {
                sInitResults += "N";
                Echo("Using Named: " + centerSearch[0].CustomName);
            }
            if (centerSearch.Count > 0)
                gpsCenter = centerSearch[0];

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blocks = GetBlocksContains<IMyTextPanel>("[GPS]");
            if (blocks.Count > 0)
                gpsPanel = blocks[0] as IMyTextPanel;

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

            string value = "";

            value = iniCustomData.GetValue(sTechnikerSection, "DoForwardScans");
            if (value != null)
                bDoForwardScans = value.Trim().ToLower() == "true" ? true : false;

            value = iniCustomData.GetValue(sTechnikerSection, "CheckGasGens");
            if (value != null)
                bCheckGasGens = value.Trim().ToLower() == "true" ? true : false;

            value = iniCustomData.GetValue(sTechnikerSection, "TechnikerCalcs");
            if (value != null)
                bTechnikerCalcs = value.Trim().ToLower() == "true" ? true : false;

            value = iniCustomData.GetValue(sTechnikerSection, "GPSFromEntities");
            if (value != null)
                bGPSFromEntities = value.Trim().ToLower() == "true" ? true : false;

            value = iniCustomData.GetValue(sTechnikerSection, "AirVents");
            if (value != null)
                bAirVents = value.Trim().ToLower() == "true" ? true : false;

            // the old way
            string sData = Me.CustomData;
	        string[] lines = sData.Trim().Split('\n');
//	        Echo(lines.Length + " Lines");
	        for(int i=0;i<lines.Length;i++)
	        {
//        Echo("|" + lines[i].Trim());
		        string[] keys = lines[i].Trim().Split('=');
		        if(lines[i].ToLower().Contains("thrustignore"))
		        {
			        if (keys.Length > 1)
			        {
				        sIgnoreThruster = keys[1];
			        }
		        }
		        if(lines[i].ToLower().Contains("shipname"))
		        {
			        if (keys.Length > 1)
			        {
				        OurName = "Wico " + keys[1];
				        bGotAntennaName = true;
			        }
		        }
		        if(lines[i].ToLower().Contains("shortrangemax"))
		        {
			        if (keys.Length > 1)
			        {
				        double d;
				        if (double.TryParse(keys[1], out d))
					        shortRangeMax = d;
				        else Echo("Error Converting" + keys[1]);
			        }
			        else Echo("Error parsing");
		        }
		        if(lines[i].ToLower().Contains("longrangemax"))
		        {
			        if (keys.Length > 1)
			        {
				        double d;
				        if (double.TryParse(keys[1], out d))
					        longRangeMax = d;
				        else Echo("Error Converting" + keys[1]);
			        }
			        else Echo("Error parsing");
		        }
	        }
	        if (bLongRange)
		        maxScan = longRangeMax;
	        else
		        maxScan = shortRangeMax;

        }

    }
}