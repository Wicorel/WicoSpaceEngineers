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
        int cargopcthighwater = 95;
        int cargopctlowwater = 60;
        float fWaitCargoMins = 1.5f;
        float fMaxSearchMins = 1.0f;
        float fMaxShipClearMins = 5.5f;


        float fTargetMiningmps = 1.5f;
        float fAbortmps = 3.0f;



        bool bWaitingCargo = false;


        /*
         * 0 Master Init
         * 10 Init sensors
         * 11 check sensors to see if 'inside'
         *   in 'inside' ->20
         *   else ->100
         *   
         * 20 Start mining while 'inside' asteroid
         *  set exit
         *  ->31
         *  
         *  31 set sensor for mining run
         *  ->35
         *  
         *  35 Mining
         *  
         *  
         *  100 Init sensor for forward search for asteroid
         *      -> 101
         *      
         *  101 Check sensors for asteroid in front
         *      check camera for asteroid in front
         *      if found ->120
         *      
         *  120 found asteroid in front. (approach)
         *     move forward until close.  then ->31
         *  
         *   
         * 
         * old->new
         * 0->100 Approaching
         * 1->200 Proximity
         * 3->300 Exiting
         * 
         * 10 Init sensors
         * 
         */
        private StringBuilder strb = new StringBuilder();

        void doModeMine()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":Mine!", textPanelReport);
            Echo("FindOre:current_state=" + current_state.ToString());
        	MyShipMass myMass;
            myMass=((IMyShipController)gpsCenter).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;

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

            // TODO: Check  maxDeltaV.  If <0.25, then we can't move..
            // so turn around at 0.50 or less.. (and still use cargopcent?)

            switch (current_state)
            {
                case 0:
                    current_state = 10;
                    bWantFast = true;
                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                    }
                    ResetMotion();
                    break;
                case 10:
                    //sb = sensorsList[0];
                    sleepAllSensors();
                    setSensorShip(sensorsList[0], 2, 2, 2, 2, 2, 0);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
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
                        { // we have exited the asteroid.
                            ResetMotion();
                            if(cargopcent>cargopctlowwater || maxDeltaV<fTargetMiningmps)
                            {
                                // we need to dump our contents
                            }

                            current_state = 10;
                            bWantFast = true;
                        }
                        else
                        { // we are inside asteroid
                            turnEjectorsOn();
//                            blockApplyAction(ejectorList, "OnOff_On");
                            if (cargopcent > cargopcthighwater && !bWaitingCargo) //maxDeltaV<fTargetMiningmps ||
                            {
                                // need to check how much stone we have.. if zero, then we're full.. go exit.
                                // TODO:
                                ResetMotion();
                                turnDrillsOff();
//                                blockApplyAction(drillList, "OnOff_Off");
                                bWaitingCargo = true;
                            }
                            if (bWaitingCargo && cargopcent > cargopctlowwater)
                            { // continue to wait
                              // TODO: Needs time-out
                                Echo("Cargo above highwater: Waiting");
                                // should check for stone amount...
                            }
                            else
                            {
                                bWaitingCargo = false;
                                GyroMain("forward", vExpectedExit, gpsCenter);
                                turnDrillsOn();
//                                blockApplyAction(drillList, "OnOff_On");
                                mineMoveForward();
                                bWantFast = true;
                            }
                        }
                    }
                    break;

                case 100:
                    turnDrillsOff();
//                    blockApplyAction(drillList, "OnOff_Off");
                    sb = sensorsList[0];
                    setSensorShip(sb, 0, 0, 0, 0, 50, 0);
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
                        //                        if (!bValidAsteroid)
                        {
                            // not in sensor range. start checking cameras
                            if (doCameraScan(cameraForwardList, 5000))
                            { // we scanned
                                if (!lastDetectedInfo.IsEmpty())
                                {  // we hit something

                                    if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                    {
                                        addDetectedEntity(lastDetectedInfo);
                                        vTargetAsteroid = (Vector3D)lastDetectedInfo.HitPosition;
                                        bValidAsteroid = true;
                                        vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();

                                    }
                                }
                            }

                        }
                        if (bValidAsteroid) current_state = 120; //found asteroid ahead
                    }
                    break;
                case 120:
                    {
                        bWantFast = true;
                        // we should have asteroid in front.
                        bool bAimed = false;
                        Vector3D vVec =  vTargetAsteroid - gpsCenter.GetPosition();

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

        int iMMFWiggle = 0;
        void mineMoveForward()
        {
            Echo("mMF");

            if (velocityShip > fAbortmps)
            {
                Echo("ABORT");
                powerDownThrusters(thrustAllList);
                iMMFWiggle = 0;
            }
            else if (velocityShip < fTargetMiningmps)
            {
                if (velocityShip < 0.5f)
                    iMMFWiggle++;
                Echo("Push");
                powerUpThrusters(thrustForwardList, 15f+iMMFWiggle);
            }
            else
            {
                Echo("Coast");
                powerUpThrusters(thrustForwardList, 1f); // coast
            }

        }

    }
}