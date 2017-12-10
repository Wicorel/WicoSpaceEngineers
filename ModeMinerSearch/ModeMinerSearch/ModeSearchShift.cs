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
        /*
         * 
         * OLD:
        * 0=init sensors
        * 8 check sensors
        * 1=push down
        * 2=push left ->3
        * 9=push right ->3
        *
        * 3 = check sensors; go to 4 or 5
        * 4 = rotating ->5 or ->7
        * 5. asteroid check on 'bottom' after rotate and nothing in front.
        * 6. rotate after 5 ->7
        * 7. Wait for motion to stop (velocityShip) ->mine
        * 
        * 20. Init search top/left (sensor setting)
        * 30. check sensors
        * 29 move backward until NOT bfrontnasteroid
        * 27. Move forward until bfrontnasteroid
        * 28. delay for motion. reset sensors.

        * * 21. move up until bfront clear
        * 22. move right until roll control clear
        * 23 delay for motion. reset sensors
        * 24 shift left 1/2 ship width - 2m *.35
        * 25 shift down until bfront, bfrontnear. stop if !brollcontrol and do searchverify
        * 26 delay for motion. then start mining
        */
        /*
          * readonly Dictionary < int, string > SearchShiftStates = new Dictionary < int, string > { 
             { 
                 0, "Initializing" 
             }, { 
                 1, "Moving Down" 
             }, { 
                 2, "Moving Left" 
             }, { 
                 3, "Calculate rotate" 
             }, { 
                 4, "Rotating" 
             }, { 
                 5, "Check Bottom" 
             }, { 
                 6, "Rotate" 
             }, { 
                 7, "Delay for motion" 
             }, { 
                 8, "Check Sensors" 
             }, { 
                 9, "Moving Right" 
             }, { 
                 20, "STR:Init search top right" 
             }, { 
                 21, "STR:move up" 
             }, { 
                 22, "STR:move right" 
             }, { 
                 23, "STR:delay for motion" 
             }, { 
                 24, "STR:shift left" 
             }, { 
                 25, "STR:shift down" 
             }, { 
                 26, "STR:delay for motion (2)" 
             }, { 
                 27, "STR:Move in range" 
             }, { 
                 28, "STR:delay for motion (3)" 
             }, { 
                 29, "STR:Move back to clear" 
             }, { 
                 30, "STR:Check Sensors" 
             } 

         }; 
         */

            /* New
             * 0 = master init
             * 10 delay for sensors, then check for asteroid found
             * if found ->20
             * else ->100
             */

        double shiftElapsedMs = 0;

        void doModeSearchShift()
        {
            List<IMySensorBlock> aSensors = null;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":SearchShift", textPanelReport);
            Echo("Search Shift: current_state=" + current_state.ToString());
            double maxThrust = calculateMaxThrust(thrustForwardList);
            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;
            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

            Echo("Cargo=" + cargopcent.ToString() + "%");

            Echo("velocity=" + velocityShip.ToString("0.00"));
            Echo("shiftElapsedMs=" + shiftElapsedMs.ToString("0.00"));

            bool bLocalFoundAsteroid = false;

            IMySensorBlock sb;
 //           IMySensorBlock sb2;
            if (sensorsList.Count < 1)
            {
                StatusLog(OurName + ":" + moduleName + " Search Shift: Not Enough Sensors!", textLongStatus, true);
                setMode(MODE_ATTENTION);
                return;
            }

            sb = sensorsList[0];
//            sb2 = sensorsList[1];
            switch (current_state)
            {
                case 0:
                    ResetMotion();
                    sleepAllSensors();
                    sb.DetectAsteroids = true;
 //                   sensorsList[1].DetectAsteroids = true;
                    setSensorShip(sb, 0, 0, 0, 0, 45, 0);
                    current_state = 10;
                    shiftElapsedMs = 0;
                    break;
                case 10: // delay for sensors
                    shiftElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (shiftElapsedMs < 1) return;
                    if (velocityShip > 0.2f) return;

                    aSensors = activeSensors();
                    bLocalFoundAsteroid = false;
                    for (int i = 0; i < aSensors.Count; i++)
                    {
                        IMySensorBlock s = aSensors[i] as IMySensorBlock;
                        Echo(aSensors[i].CustomName + " ACTIVE!");

                        List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                        s.DetectedEntities(lmyDEI);

                        for (int j = 0; j < lmyDEI.Count; j++)
                        {

                            if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                            {
                                addDetectedEntity(lmyDEI[j]);
                                bLocalFoundAsteroid = true;
                                currentAst.EntityId = lmyDEI[j].EntityId;
                                currentAst.BoundingBox = lmyDEI[j].BoundingBox;
                                if (!bValidAsteroid)
                                {
                                    bValidAsteroid = true;
                                    vTargetAsteroid = lmyDEI[j].Position;
                                }
                            }
                        }
                    }
                    if (bLocalFoundAsteroid) current_state = 20;
                    else current_state = 100;
                    break;
                case 20:
                    setMode(MODE_FINDORE);
                    break;
                case 100:
                    break;
            }
        }
    }
}