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
        #region maininit

        string sInitResults = "";
        int currentInit = 0;

        double gridBaseMass = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.
            initLogging();

            Log("Init:" + currentInit.ToString());
            double progress = currentInit * 100 / 3; // 3=Number of expected INIT phases.
            string sProgress = progressBar(progress);
            StatusLog(moduleName + sProgress, textPanelReport);

            Echo("Init:" + currentInit);
            if (currentInit == 0)
            {
                //        StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                /*
                if(!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
                */
                sInitResults += gridsInit();
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
                /*
                }
                else if(currentRun==2)
                {
                */
                sInitResults += wheelsInit(gpsCenter);
                sInitResults += rotorsNavInit();

                sInitResults += connectorsInit();
                sInitResults += tanksInit();
                sInitResults += drillInit();
                sInitResults += ejectorsInit();

                sInitResults += antennaInit();
                sInitResults += gasgenInit();

                //        Serialize();

                autoConfig();
                bWantFast = false;

                if (anchorPosition != null)
                {
                    MyShipMass myMass;
                    myMass = ((IMyShipController)anchorPosition).CalculateShipMass();

                    gridBaseMass = myMass.BaseMass;

                    sInitResults += modeOnInit(); // handle mode initializing from load/recompile..

                    init = true; // we are done

                }
                else
                {
                    // we are not complete.  try again..
                    currentInit = 0;
                    bWantFast = false;
                    Echo("Missing Required Item; Please add");
                    return sInitResults;
                }

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


    }
}