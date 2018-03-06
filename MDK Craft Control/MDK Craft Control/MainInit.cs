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

        }
        void ModuleInitCustomData(INIHolder iniCustomData)
        {
            ConnectorInitCustomData(iniCustomData);
            ThrustersInitCustomData(iniCustomData);
            GyroInitCustomData(iniCustomData);
            CamerasInitCustomData(iniCustomData);
            GearsInitCustomData(iniCustomData);
            PowerInitCustomData(iniCustomData);
            CargoInitCustomData(iniCustomData);
        }


        string sInitResults = "";
        int currentInit = 0;

        double gridBaseMass = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.
            Echo(gtsAllBlocks.Count.ToString() + " Blocks");

            do
            {
//                Echo("Init:" + currentInit);
                echoInstructions("Init:" + currentInit+" ");
                switch (currentInit)
                {
                    case 0:
                        StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);
                        break;
                    case 1:

                        /*
                         * add commands to set modes
                        if(!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
                        */
                        if (!modeCommands.ContainsKey("doscans")) modeCommands.Add("doscans", MODE_DOSCAN);
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
                        break;
                    case 17:
                        sInitResults += camerasensorsInit(shipOrientationBlock);
                        break;
                    case 18:
                        sInitResults += ejectorsInit();
                        break;
                    case 19:
                        sInitResults += antennaInit();
                        break;
                    case 20:
                        sInitResults += gasgenInit();
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

                        init = true; // we are done
                        break;
                }
                currentInit++;
 //               echoInstructions("EInit:" + currentInit + " | ");
 //               Echo("%=" + (float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount);
            }
            while (!init && (((float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount) < 0.5f));
            if (init) currentInit = 0;

            Log(sInitResults);

            return sInitResults;
        }

        string modeOnInit()
        {
            return ">";
        }

    }
}