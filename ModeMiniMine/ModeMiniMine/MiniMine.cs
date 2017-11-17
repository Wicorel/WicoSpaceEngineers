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
//        float fWaitCargoMins = 1.5f;
//        float fMaxSearchMins = 1.0f;
//        float fMaxShipClearMins = 5.5f;


        float fTargetMiningmps = 1.5f;
        float fAbortmps = 3.0f;

        float fApproachMps = 5.0f;
        float fApproachAbortMps = 7.0f;


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
         *  31 set sensor for mining run ->32
         *  32 delay for sensors ->35
         *  
         *  35 Mining/finding
         *  
         *  
         *  100 Init sensor for forward search for asteroid
         *      -> 101
         *   
         *  101 delay for sensors ->102
         *  102 Check sensors for asteroid in front
         *      check camera for asteroid in front
         *      if found ->120
         *      else ->110
         *      
         *  110 asteroid NOT found in front. big sensor search
         *      Set sensors ->111
         *  111 sensors delay ->112
         *  112 check sensors
         *      
         *  120 found asteroid in front. (approach)
         *     move forward until close.  then ->31
         *     
         *  300 exited asteroid
         * 
         */
        private StringBuilder strbMining = new StringBuilder();

        double miningElapsedMs = 0;

        bool bValidExit = false;

        void doModeFindOre()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;
            IMySensorBlock sb2;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":FindOre", textPanelReport);
            Echo("current_state=" + current_state.ToString());
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
            Echo("miningElapsedMs=" + miningElapsedMs.ToString("0.00"));

            /*
            Echo("#Sensors=" + sensorsList.Count);
            Echo("width=" + shipDim.WidthInMeters().ToString("0.0"));
            Echo("height=" + shipDim.HeightInMeters().ToString("0.0"));
            Echo("length=" + shipDim.LengthInMeters().ToString("0.0"));
            */
            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
            StatusLog("clear", txtPanel);

            // TODO: Check  maxDeltaV.  If <0.25, then we can't move..
            // so turn around at 0.50 or less.. (and still use cargopcent?)
            if (bValidAsteroid)
                debugGPSOutput("Pre-Valid Ast", vTargetAsteroid);
            //            if(vExpectedExit.AbsMax()>.5)
            {
                Vector3D vT = gpsCenter.GetPosition() + vExpectedExit * 150;
                debugGPSOutput("ExpectedExit", vT);
            }
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
                    sensorsList[0].DetectAsteroids = true;
                    sensorsList[1].DetectAsteroids = true;

                    bValidAsteroid = false;
                    bValidExit = false;

                    ResetMotion();
//                    turnDrillsOff();
                    turnEjectorsOff();
                    break;
                case 10:
                    //sb = sensorsList[0];
                    sleepAllSensors();
                    setSensorShip(sensorsList[0], 2, 2, 2, 2, 2, 2);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                    miningElapsedMs = 0;
                    current_state = 11;
                    break;

                case 11:
                    {
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < 1) return;

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

                            strbMining.Clear();
                            strbMining.Append("Sensor Position:" + s.GetPosition().ToString());
                            strbMining.AppendLine();


                            for (int j = 0; j < lmyDEI.Count; j++)
                            {

                                strbMining.Append("Name: " + lmyDEI[j].Name);
                                strbMining.AppendLine();
                                strbMining.Append("Type: " + lmyDEI[j].Type);
                                strbMining.AppendLine();
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
                            StatusLog(strbMining.ToString(), txtPanel);
                        }
                        if (bFoundAsteroid) current_state = 20; // inside
                        else current_state = 100;
                    }
                    break;
                case 20:
                    {
                        // started find ore while 'inside' asteroid.
                        // point towards exit
                        vExpectedExit = gpsCenter.GetPosition() - currentAst.Position;
                        vExpectedExit.Normalize();
                        bValidExit = true;
                        current_state = 31;
                    }
                    break;
                case 31:
                    sb = sensorsList[0];
                    sb2 = sensorsList[1];
                    sleepAllSensors();
                    setSensorShip(sb, 0, 0, 0, 0, 45, 0);
                    setSensorShip(sb2, 5, 5, 5, 5, 0, 15);
                    current_state = 32;
                    miningElapsedMs = 0;
                    break;
                case 32:
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < 1) return; // delay for sensor settling
                    current_state = 35;
                    break;
                case 35:
                    {
                        aSensors = activeSensors();
                        //                        bValidAsteroid = false;
                        bool bLocalAsteroid = false;
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
                                    bLocalAsteroid = true;
                                    if (!bValidAsteroid)
                                    {
                                        Echo("Found New Asteroid!");
                                        bValidAsteroid = true;
                                        vTargetAsteroid = lmyDEI[j].Position;
                                    }
                                    currentAst.EntityId = lmyDEI[j].EntityId;
                                    currentAst.BoundingBox = lmyDEI[j].BoundingBox;
                                }
                                // need to detect other ships and avoid
                            }

                        }
                        if (!bLocalAsteroid)
                        { // we have exited the asteroid.
                            ResetMotion();
//                            turnDrillsOff();
                            if (cargopcent > cargopctlowwater || maxDeltaV < (fTargetMiningmps/2))
                            {
                                // we need to dump our contents
                                turnEjectorsOn();
                            }
                            current_state = 300;
                            bWantFast = true;
                        }
                        else
                        { // we are inside asteroid
                            turnEjectorsOn();
                            GyroMain("forward", vExpectedExit, gpsCenter);
                            //                            blockApplyAction(ejectorList, "OnOff_On");
                            if (maxDeltaV < (fTargetMiningmps/2) || cargopcent > cargopcthighwater && !bWaitingCargo) //
                            {
                                ResetMotion();
//                                turnDrillsOff();
                                turnEjectorsOn();
                                bWaitingCargo = true;
                            }
                            if (bWaitingCargo)
                            { // continue to wait
                                // need to check how much stone we have.. if zero(ish), then we're full.. go exit.
                                doCargoOreCheck();
                                if (currentStoneAmount() < 15)
                                {
                                    // we are full and not much stone ore in us...
                                    ResetMotion();
//                                    turnDrillsOff();
                                    turnEjectorsOff();
                                    setMode(MODE_EXITINGASTEROID);
                                }
                                // TODO: Needs time-out
                                Echo("Cargo above low water: Waiting");
                                if (maxDeltaV > (fTargetMiningmps/2) && cargopcent < cargopctlowwater)
                                    bWaitingCargo = false; // can now move.
                            }
                            else
                            {
//                                GyroMain("forward", vExpectedExit, gpsCenter);
                                turnDrillsOn();
                                mineMoveForward(fTargetMiningmps, fAbortmps);
                                bWantFast = true;
                            }
                        }
                    }
                    break;

                case 100:
                    turnDrillsOff();
                    sleepAllSensors();
                    sb = sensorsList[0];
                    setSensorShip(sb, 0, 0, 0, 0, 50, 0);
                    current_state = 101;
                    miningElapsedMs = 0;
                    break;
                case 101:
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < 1) return; // delay for sensor settling
                    current_state++;
                    break;

                case 102:
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
                            strbMining.Clear();
                            strbMining.Append("Sensor Position:" + s.GetPosition().ToString());
                            strbMining.AppendLine();
                            for (int j = 0; j < lmyDEI.Count; j++)
                            {

                                strbMining.Append("Name: " + lmyDEI[j].Name);
                                strbMining.AppendLine();
                                strbMining.Append("Type: " + lmyDEI[j].Type);
                                strbMining.AppendLine();
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
                            StatusLog(strbMining.ToString(), txtPanel);
                            //				else current_state = 100;
                        }
                        //                        if (!bValidAsteroid)
                        {
                            // not in sensor range. start checking cameras
                            double scandist = 500;
                            /*
                                                        if (bValidAsteroid)
                                                        {
                                                            scandist = (gpsCenter.GetPosition() - vTargetAsteroid).Length();
                                                        }
                                                        */
                            if (doCameraScan(cameraForwardList, scandist))
                            { // we scanned
                                if (!lastDetectedInfo.IsEmpty())
                                {  // we hit something

                                    if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                    {
                                        addDetectedEntity(lastDetectedInfo);
                                        vTargetAsteroid = (Vector3D)lastDetectedInfo.HitPosition;
                                        bValidAsteroid = true;
                                        vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();
                                        vExpectedExit.Normalize();
                                        bValidExit = true;

                                    }
                                }
                                else
                                {
                                    // no asteroid detected.  Check surroundings for one.
                                    current_state = 110;
                                    bValidExit = false;
                                }
                            }

                        }
                        if (bValidExit) current_state = 120; //found asteroid ahead
                    }
                    break;
                case 110:
                    {
                        sleepAllSensors();
                        setSensorShip(sensorsList[0], 50, 50, 50, 50, 50, 50);
                        miningElapsedMs = 0;
                        current_state = 111;
                    }
                    break;
                case 111:
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < 1) return; // delay for sensor settling
                    current_state++;
                    break;
                case 112:
                    {
                        aSensors = activeSensors();
                        //                        bValidAsteroid = false;
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);

                            StatusLog("#DetectedEntities=" + lmyDEI.Count, txtPanel);
                            strbMining.Clear();
                            strbMining.Append("Sensor Position:" + s.GetPosition().ToString());
                            strbMining.AppendLine();
                            for (int j = 0; j < lmyDEI.Count; j++)
                            {

                                strbMining.Append("Name: " + lmyDEI[j].Name);
                                strbMining.AppendLine();
                                strbMining.Append("Type: " + lmyDEI[j].Type);
                                strbMining.AppendLine();
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
                            StatusLog(strbMining.ToString(), txtPanel);
                            //				else current_state = 100;
                        }
                        if (bValidAsteroid)
                        {
                            bWantFast = true;
                            if (GyroMain("forward", vTargetAsteroid - gpsCenter.GetPosition(), gpsCenter))
                            {
                                // we are aimed
                                current_state = 100;
                            }
                        }
                    }
                    break;
                case 120:
                    { // approach
                        if (!bValidAsteroid)
                        {
                            current_state = 10;
                            return;
                        }
                        bWantFast = true;
                        // we should have asteroid in front.
                        bool bAimed = true;

                        Echo(bValidExit.ToString() + " " + Vector3DToString(vExpectedExit));
                        bAimed = GyroMain("forward", vExpectedExit, gpsCenter);
                        if (bAimed)
                        {
                            if (doCameraScan(cameraForwardList, 500))
                            {
                                if (!lastDetectedInfo.IsEmpty())
                                {
                                    // we have a target
 //                                   if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                                    {
                                        Vector3D vTarget = (Vector3D)lastDetectedInfo.HitPosition;
                                        double distance = (vTarget - lastCamera.GetPosition()).Length();
                                        Echo("Distance=" + distance.ToString());
                                        if (distance < 15)
                                        {
                                            vLastContact = gpsCenter.GetPosition();
                                            if(!bValidInitialContact)
                                            {
                                                vInitialContact = vLastContact;
                                                bValidInitialContact = true;
                                            }
                                            current_state = 31;
                                            ResetMotion();
                                        }
                                        else
                                        {
                                            mineMoveForward(fApproachMps,fApproachAbortMps);
                                        }
                                    }
                                    //                                      bValidExit = true;
                                    //                                        vExpectedExit = vTarget - gpsCenter.GetPosition();
                                }
                                else
                                {
                                    /*
                                    // we scanned, but didn't hit anything.  it's likely a donut
                                    double distance = (vTargetAsteroid - gpsCenter.GetPosition()).Length();
                                    Echo("Distance=" + distance.ToString());
                            mineMoveForward(fTargetMiningmps,fAbortmps);
                                    if (distance < 5)
                                    {
                                        vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();
                                        current_state = 31;
                                        ResetMotion();
                                    }
                                    */
                                    setMode(MODE_ATTENTION);
                                }
                            }
                            // ELSE no scan available.. just wait
                            else Echo("No Camera SCAN!");
                        }
                        else Echo("Aiming");
                        // BUG: Gets stuck in state if nothing hit.. (ie, donut)

                    }
                    break;
                case 300:
                    {
                        // we have exitted the asteroid.  Prepare for another run or to go dock
                        Echo("Exitted!");
                        ResetMotion();
                        sleepAllSensors();

                        if (maxDeltaV < (fTargetMiningmps/2) && cargopcent > cargopctlowwater)
                        {
                            setMode(MODE_DOCKING);
                        }
                        else
                        {
                             vLastExit = gpsCenter.GetPosition();
 //                           dist = (vLastExit - vInitialExit).Length();
                            if (!bValidInitialExit)
                            {
                                vInitialExit = vLastExit;
                                bValidInitialExit = true;
                            }
                            setMode(MODE_SEARCHORIENT);
                           // prepare for another run.
                        }
                        break;

                    }
            }
        }

        void doModeGotoOre()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":GotoOre!", textPanelReport);
            Echo("current_state=" + current_state.ToString());
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
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

        }

        void doModeMiningOre()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":MiningOre!", textPanelReport);
            Echo("current_state=" + current_state.ToString());
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
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

        }

        void doModeExitingAsteroid()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":Exiting!", textPanelReport);
            Echo("current_state=" + current_state.ToString());
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
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

        /*
         * 0 - Master Init
         * 10 - Init sensors, turn drills on
         * 11 - await sensor set
         * 20 - turn around until aimed ->30
         * 30 - move forward (out) until exit asteroid
         * 
         * 40 when out, call for pickup
         * 
         */

            switch (current_state)
            {
                case 0: //0 - Master Init
                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                    }
                    ResetMotion();
//                    turnDrillsOff();
                    turnEjectorsOn();
                    current_state = 10;
                    break;
                case 10://10 - Init sensors, turn drills on
                    sleepAllSensors();
                    setSensorShip(sensorsList[0], 2, 2, 2, 2, 15, 15);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                    miningElapsedMs = 0;
                    current_state = 11;
                    break;
                case 11://11 - await sensor set
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < 1) return;
                    current_state = 20;
                    break;
                case 20: //20 - turn around until aimed ->30
                    {
                        bWantFast = true;
                        turnDrillsOn();
                        //minAngleRad = 0.1f;
                        bool bAimed = GyroMain("backward", vExpectedExit, gpsCenter);

                       // minAngleRad = 0.01f;
                        // GyroMain("backward", vExpectedExit, gpsCenter);
                        if (bAimed)
                        {
                            mineMoveForward(fTargetMiningmps,fAbortmps);

                        }
                        bool bLocalAsteroid = false;
                        aSensors = activeSensors();
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            StatusLog("#DetectedEntities=" + lmyDEI.Count, txtPanel);

                            strbMining.Clear();
                            strbMining.Append("Sensor Position:" + s.GetPosition().ToString());
                            strbMining.AppendLine();

                            for (int j = 0; j < lmyDEI.Count; j++)
                            {

                                strbMining.Append("Name: " + lmyDEI[j].Name);
                                strbMining.AppendLine();
                                strbMining.Append("Type: " + lmyDEI[j].Type);
                                strbMining.AppendLine();
                                if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                                {
                                    addDetectedEntity(lmyDEI[j]);
                                    StatusLog("Found Asteroid!", txtPanel);
                                    bLocalAsteroid = true;
                                }
                            }
                            StatusLog(strbMining.ToString(), txtPanel);
                        }
                        if (!bLocalAsteroid)
                        {
                            ResetMotion();
//                            turnDrillsOff();
                            sleepAllSensors();
                            current_state = 40;
                        }
                    }
                    break;
                case 30://30 - move forward (out) until exit asteroid

                    break;
                case 40://40 when out, call for pickup
                    {
                        turnDrillsOff();
                        // we should probably give hint to docking as to WHY we want to dock..
                        setMode(MODE_DOCKING);
                    }
                    break;
            }
        }

        int iMMFWiggle = 0;
        void mineMoveForward(float fTarget, float fAbort)
        {
            Echo("mMF");

            if (velocityShip > fAbort)
            {
                Echo("ABORT");
                powerDownThrusters(thrustAllList);
                iMMFWiggle = 0;
            }
            else if (velocityShip < (fTarget*0.90))
            {
                if (velocityShip < 0.5f)
                    iMMFWiggle++;
                Echo("Push");
                powerUpThrusters(thrustForwardList, 15f + iMMFWiggle);
            }
            else if(velocityShip<(fTarget*1.2))
            {
                // we are around target. 90%<-current->120%
                 Echo("Coast");
               // turn off reverse thrusters and 'coast'.
                powerDownThrusters(thrustBackwardList, thrustAll, true);
                powerDownThrusters(thrustForwardList);
            }
            else
            { // above 120% target, but below abort
                Echo("Coast2");
                powerUpThrusters(thrustForwardList, 1f); // coast
            }

        }

    }
}