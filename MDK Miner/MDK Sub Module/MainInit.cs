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


        #region maininit

        string sInitResults = "";
        string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.

        	if(currentInit==0) initLogging();

            Log("Init:" + currentInit.ToString());
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
                sInitResults += initSerializeCommon();
                Deserialize();
        		initShipDim();
            }
            else if (currentInit == 1)
            {
                sInitResults += BlockInit();
                anchorPosition = gpsCenter;
                currentPosition = anchorPosition.GetPosition();
		        sInitResults += connectorsInit();
		        sInitResults += thrustersInit(gpsCenter);
		        sInitResults+=camerasensorsInit(gpsCenter); 
		        sInitResults+=sensorInit(); 

//		        sInitResults += gearsInit();
		        sInitResults += tanksInit();
		        sInitResults += gyrosetup();

		        sInitResults += drillInit();
		        sInitResults += ejectorsInit();
                initCargoCheck();

                Deserialize();
//                bWantFast = false;
                sInitResults += modeOnInit();
                init = true;
            }

            currentInit++;
            if (init) currentInit = 0;

            Log(sInitResults);
            Echo("Init exit");
            return sInitResults;

        }

        IMyTextPanel gpsPanel = null;

        string BlockInit()
        {
            string sInitResults = "";

            List<IMyTerminalBlock> centerSearch = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(sGPSCenter, centerSearch, (x1 => x1.CubeGrid == Me.CubeGrid));
            if (centerSearch.Count == 0)
            {
                centerSearch = GetBlocksContains<IMyRemoteControl>("[NAV]");
                if (centerSearch.Count == 0)
                {
                    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(centerSearch, (x1 => x1.CubeGrid == Me.CubeGrid));
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
            gpsCenter = centerSearch[0];

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blocks = GetBlocksContains<IMyTextPanel>("[GPS]");
            if (blocks.Count > 0)
                gpsPanel = blocks[0] as IMyTextPanel;

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