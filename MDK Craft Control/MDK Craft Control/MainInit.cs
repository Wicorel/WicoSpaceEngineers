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
//            CamerasInitCustomData(iniCustomData);
            GearsInitCustomData(iniCustomData);
            PowerInitCustomData(iniCustomData);
            CargoInitCustomData(iniCustomData);
            CommunicationsInitCustomData(iniCustomData);
        }




        double gridBaseMass =-1;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.
//            Echo(gtsAllBlocks.Count.ToString() + " Blocks");
            if(bStartupError)
            {
                Echo("(RE)INIT:"+sStartupError);
            }

            do
            {
//                Echo("Init:" + currentInit);
                echoInstructions("Init:" + currentInit+" ");
                if (bStartupError)
                {
                    Echo("ERROR: Need (RE)INIT:" + sStartupError);
                    Echo(sStartupError);
                }

                switch (currentInit)
                {
                    case 0:
                        sStartupError = "";
                        if (bStartupError) gridsInit(); // check the entire grid again
                        bStartupError = false;
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
//                        sInitResults += camerasensorsInit(shipOrientationBlock);
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
                        break;
                    case 23:
                        {

                            // do startup error check
                            init = true; // we are done
                                         //                       sStartupError = "";
                                         //                        bStartupError = false;
                            if (shipOrientationBlock == null)
                            {
                                shipOrientationBlock = Me;
                                sStartupError += "\nUsing " + Me.CustomName + " as orientation";

                                //bStartupError = true;
                                sStartupError += "\nNo Ship Controller";
                                dGravity = -1.0;

                            }
                            else
                            {
                                if (shipOrientationBlock is IMyShipController)
                                {
                                    velocityShip = ((IMyShipController)shipOrientationBlock).GetShipSpeed();

                                    Vector3D vNG = ((IMyShipController)shipOrientationBlock).GetNaturalGravity();
                                    double dLength = vNG.Length();
                                    dGravity = dLength / 9.81;
                                }
                                else
                                {
                                    dGravity = -1.0;
                                }

                            }
                            Echo("Grid Mass=" + gridBaseMass.ToString());
                            if (gridBaseMass > 0)
                            { // Only require propulsion if not a station
                                if (dGravity==0)
                                {
                                    if (ionThrustCount < 1 && hydroThrustCount < 1)
                                        sStartupError += "\nIn Space, but no valid thrusters";
                                }
                                if (ionThrustCount < 1 && hydroThrustCount < 1 && atmoThrustCount < 1)
                                {
                                    // no thrusters
                                    if (wheelSledList.Count < 1)
                                    {
                                        // no sled wheels && no thrusters
                                        if (rotorNavRightList.Count < 1)
                                        {
                                            if (wheelRightList.Count < 1)
                                            {
                                                //TODO: Detect station and it's OK to not go anywhere..

                                                bStartupError = true;
                                                sStartupError += "\nNo Propulsion Method Found";
                                                sStartupError += "\nNo Thrusters.\nNo NAV Rotors\nNo Sled Wheels\nNo Wheels";
                                            }

                                        }
                                    }
                                    else
                                    {
                                        // sled wheels, but not thrusters...
                                        bStartupError = true;
                                        sStartupError += "\nNo Valid Propulsion Method Found";
                                        sStartupError += "\nSled wheels, but No Thrusters.\nNo NAV Rotors";
                                        if (gyros.Count < 1)
                                        {
                                            bStartupError = true;
                                            sStartupError = "\nSled wheels, but no Gyros";
                                        }
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
                }
                currentInit++;
 //               echoInstructions("EInit:" + currentInit + " | ");
 //               Echo("%=" + (float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount);
            }
            while (!init && (((float)Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount) < 0.2f));
            if (init) currentInit = 0;
            if (bStartupError)
            {
                Echo("ERROR: Need (RE)INIT:" + sStartupError);
            }

            Echo(sStartupError);

            Log(sInitResults);

            return sInitResults;
        }

        string modeOnInit()
        {
            return ">";
        }

    }
}