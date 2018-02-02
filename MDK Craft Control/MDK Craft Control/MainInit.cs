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

            Echo("Init:" + currentInit);
            if (currentInit == 0)
            {
                //        StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
                StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                /*
                 * add commands to set modes
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

//                sInitResults += BlockInit();
                sInitResults += DefaultOrientationBlockInit();
                initCargoCheck();
                initPower();
                sInitResults += thrustersInit(shipOrientationBlock);
                sInitResults += gyrosetup();
//                if (gtsAllBlocks.Count < 300) currentInit = 2; // go ahead and do next step.
                if (shipOrientationBlock is IMyRemoteControl)
                {
                    Vector3D playerPosition;
                    bool bGotPlayer = ((IMyRemoteControl)shipOrientationBlock).GetNearestPlayer(out playerPosition);
                    IMyRemoteControl myR= (IMyRemoteControl)shipOrientationBlock;
                    myR.SetCollisionAvoidance(false);
                    myR.SetDockingMode(false);
                    myR.Direction = Base6Directions.Direction.Forward;
                    myR.FlightMode = FlightMode.OneWay;
                    myR.ClearWaypoints();
                    myR.AddWaypoint(playerPosition, "Name");
                }
            }
            if (currentInit == 2)
            {
                sInitResults += wheelsInit(shipOrientationBlock);
                sInitResults += rotorsNavInit();

                sInitResults += connectorsInit();
                sInitResults += tanksInit();
                sInitResults += drillInit();
//                if (gtsAllBlocks.Count < 100) currentInit = 3; // go ahead and do next step.
            }
            if (currentInit == 3)
            {
                sInitResults += camerasensorsInit(shipOrientationBlock);
                sInitResults += ejectorsInit();

                sInitResults += antennaInit();
                sInitResults += gasgenInit();

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
                }

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

    }
}