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


        #region FINDORE
        /*
         * (like old MODE_MINE)
         * 0 Masterinit
         * 10 check if outside of asteroid or inside
         *   Set sensors
         * 11 Check sensors/cameras
         * 20 if inside and no entry point, try to exit
         * 31 init sensors for search mining
         * 35 inside and actively mining
         * 40 waiting for cargo space (stone) while inside
         * 50 found ore.  Log postion + Vector.  If wanted, continue to mine.
         * 60 if not wanted...
         * 100 outside. find asteroid to move to. Set sensors. Set position=entry point.
         * 101 check sensors/cameras
         * 110 orient toward asteroid
         * 120 Move toward asteroid
         */
        void doModeFindOre()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":Find Ore!", textPanelReport);
            Echo("current_state=" + current_state.ToString());
            double effectiveMass = calculateEffectiveMass();
            double maxThrust = calculateMaxThrust(thrustForwardList);

            Echo("effectiveMass=" + effectiveMass.ToString("N0"));
            Echo("maxThrust=" + maxThrust.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;

            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
            Echo("Cargo=" + cargopcent.ToString() + "%");

            Echo("velocity=" + velocityShip.ToString("0.00"));
            Echo("#Sensors=" + sensorsList.Count);
            Echo("width=" + shipDim.WidthInMeters().ToString("0.0"));
            Echo("height=" + shipDim.HeightInMeters().ToString("0.0"));
            Echo("length=" + shipDim.LengthInMeters().ToString("0.0"));

            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
            StatusLog("clear", txtPanel);

            switch (current_state)
            {
                case 0:
                    current_state = 10;
                    bWantFast = true;
                    if (sensorsList.Count < 1)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: No Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                    }
                    break;
                case 10:
                    sb = sensorsList[0];
                    sleepAllSensors();
                    setSensorShip(sb, 2, 2, 2, 2, 2, 0);
                    current_state = 11;
                    break;
                case 11:
                    {
                        aSensors = activeSensors();
                        bool bFoundAsteroid = false;
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            StatusLog("#DetectedEntities=" + lmyDEI.Count, txtPanel);

                            strb.Clear();
                            strb.Append("Sensor Position:" + s.GetPosition().ToString());
                            strb.AppendLine();


                            for (int j = 0; j < lmyDEI.Count; j++)
                            {

                                strb.Append("Name: " + lmyDEI[j].Name);
                                strb.AppendLine();
                                strb.Append("Type: " + lmyDEI[j].Type);
                                strb.AppendLine();
                                if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                                {
                                    addDetectedEntity(lmyDEI[j]);
                                    StatusLog("Found Asteroid!", txtPanel);
                                    bFoundAsteroid = true;
                                    currentAst.EntityId = lmyDEI[j].EntityId;
                                    currentAst.BoundingBox = lmyDEI[j].BoundingBox;
                                    bValidAsteroid = true;
                                    vTargetAsteroid = lmyDEI[j].Position;
                                }
                            }
                            StatusLog(strb.ToString(), txtPanel);
                        }
                        if (bFoundAsteroid) current_state = 20; // inside
                                                                // should search using cameras..
                                                                // TODO:
                        else current_state = 100;
                    }
                    break;
                case 20:
                    // started find ore while 'inside' asteroid.
                    // point towards exit
                    vExpectedExit = gpsCenter.GetPosition() - currentAst.Position;
                    current_state = 31;
                    break;
                case 31:
                    sb = sensorsList[0];
                    sleepAllSensors();
                    setSensorShip(sb, 0, 0, 0, 0, 25, 4);
                    current_state = 35;
                    break;
                case 35:
                    {
                        aSensors = activeSensors();
                        bValidAsteroid = false;
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            for (int j = 0; j < lmyDEI.Count; j++)
                            {
                                if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                                {
                                    addDetectedEntity(lmyDEI[j]);
                                    Echo("Found Asteroid!");
                                    bValidAsteroid = true;
                                    vTargetAsteroid = lmyDEI[j].Position;
                                    currentAst.EntityId = lmyDEI[j].EntityId;
                                    currentAst.BoundingBox = lmyDEI[j].BoundingBox;
                                }
                                // need to detect other ships and avoid
                            }

                        }
                        if (!bValidAsteroid)
                        {
                            current_state = 10;
                            ResetMotion();
                            bWantFast = true;
                        }
                        else
                        { // we are inside asteroid
                            blockApplyAction(ejectorList, "OnOff_On");
                            if (cargopcent > cargopcthighwater && !bWaitingCargo) //maxDeltaV<fTargetMiningmps ||
                            {
                                // need to cehck how much stone we have.. if zero, then we're full.. go exit.
                                // TODO:
                                ResetMotion();
                                blockApplyAction(drillList, "OnOff_Off");
                                bWaitingCargo = true;
                            }
                            if (bWaitingCargo && cargopcent > cargopctlowwater)
                            { // continue to wait
                              // TODO: Needs time-out
                            }
                            else
                            {
                                bWaitingCargo = false;
                                GyroMain("forward", vExpectedExit, gpsCenter);
                                blockApplyAction(drillList, "OnOff_On");
                                mineMoveForward();
                                bWantFast = true;
                            }
                        }
                    }
                    break;
                case 40:
                    break;
                case 50:
                    break;
                case 60:
                    break;
                case 100:
                    blockApplyAction(drillList, "OnOff_Off");
                    sb = sensorsList[0];
                    setSensorShip(sb, 50, 50, 50, 50, 50, 50);
                    current_state = 101;
                    break;
                case 101:
                    {
                        aSensors = activeSensors();
                        bValidAsteroid = false;
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);

                            StatusLog("#DetectedEntities=" + lmyDEI.Count, txtPanel);
                            strb.Clear();
                            strb.Append("Sensor Position:" + s.GetPosition().ToString());
                            strb.AppendLine();
                            for (int j = 0; j < lmyDEI.Count; j++)
                            {

                                strb.Append("Name: " + lmyDEI[j].Name);
                                strb.AppendLine();
                                strb.Append("Type: " + lmyDEI[j].Type);
                                strb.AppendLine();
                                if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                                {
                                    addDetectedEntity(lmyDEI[j]);
                                    StatusLog("Found Asteroid!", txtPanel);
                                    bValidAsteroid = true;
                                    vTargetAsteroid = lmyDEI[j].Position;
                                    currentAst.EntityId = lmyDEI[j].EntityId;
                                    currentAst.BoundingBox = lmyDEI[j].BoundingBox;
                                }
                            }
                            StatusLog(strb.ToString(), txtPanel);
                            //				else current_state = 100;
                        }
                        if (!bValidAsteroid)
                        {
                            // not in sensor range. start checking cameras
                            if (doCameraScan(cameraForwardList, 5000))
                            { // we scanned
                                if (!lastDetectedInfo.IsEmpty())
                                {  // we hit something

                                    if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                    {
                                        addDetectedEntity(lastDetectedInfo);
                                        vTargetAsteroid = lastDetectedInfo.Position;
                                        bValidAsteroid = true;
                                    }
                                }
                            }

                        }
                        if (bValidAsteroid) current_state = 110; //found asteroid
                    }
                    break;
                case 110:
                    // turn toward asteroid.
                    {
                        if (!bValidAsteroid)
                        {
                            current_state = 0;
                            return;
                        }
                        bWantFast = true;

                        CTRL_COEFF = 0.75;
                        minAngleRad = 0.09f;

                        bool bAimed = false;
                        Vector3D vVec = vTargetAsteroid - gpsCenter.GetPosition();

                        bAimed = GyroMain("forward", vVec, gpsCenter);
                        if (bAimed)
                        {
                            setLastContact();// save current location as 'exit' target.
                            gyrosOff();
                            current_state = 120;
                        }
                    }
                    break;
                case 120:
                    {
                        bWantFast = true;
                        // we should have asteroid in front.
                        bool bAimed = false;
                        Vector3D vVec = vTargetAsteroid - gpsCenter.GetPosition();

                        bAimed = GyroMain("forward", vVec, gpsCenter);
                        if (bAimed)
                        {
                            if (doCameraScan(cameraForwardList, 5000))
                            {
                                if (!lastDetectedInfo.IsEmpty())
                                {
                                    // we have a target
                                    Vector3D vTarget = (Vector3D)lastDetectedInfo.HitPosition;
                                    double distance = (vTarget - gpsCenter.GetPosition()).Length();
                                    Echo("Distance=" + distance.ToString());
                                    mineMoveForward();
                                    if (distance < 5)
                                    {
                                        current_state = 31;
                                        ResetMotion();
                                    }
                                }
                                else
                                {
                                    // we scanned, but didn't hit anything.  it's likely a donut
                                    double distance = (vTargetAsteroid - gpsCenter.GetPosition()).Length();
                                    Echo("Distance=" + distance.ToString());
                                    mineMoveForward();
                                    if (distance < 5)
                                    {
                                        current_state = 31;
                                        ResetMotion();
                                    }

                                }
                            }
                            else Echo("No Camera SCAN!");
                        }
                        else Echo("Aiming");
                        // BUG: Gets stuck in state if nothing hit.. (ie, donut)

                        // ELSE no scan available.. just wait
                    }
                    break;
            }
        }
        #endregion

        void mineMoveForward()
        {
            if (velocityShip > fAbortmps)
            {
                powerDownThrusters(thrustAllList);
            }
            else if (velocityShip < fTargetMiningmps)
                powerUpThrusters(thrustForwardList, 15);
            else powerUpThrusters(thrustForwardList, 1); // coast

        }

    }
}