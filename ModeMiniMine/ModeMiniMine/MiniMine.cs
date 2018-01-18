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


        float fTargetMiningMps = 0.85f;
        float fMiningAbortMps = 2.0f;
        float fMiningMinThrust = 1.2f;

        float fAsteroidApproachMps = 5.0f;
        float fAsteroidApproachAbortMps = 7.0f;


        bool bWaitingCargo = false;

        string sMiningSection = "MINING";

        void MiningInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sMiningSection, "cargopcthighwater", ref cargopcthighwater, true);
            iNIHolder.GetValue(sMiningSection, "cargopctlowwater", ref cargopctlowwater, true);
            iNIHolder.GetValue(sMiningSection, "TargetMiningMps", ref fTargetMiningMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningAbortMps", ref fMiningAbortMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningMinThrust", ref fMiningMinThrust, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachMps", ref fAsteroidApproachMps, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachAbortMps", ref fAsteroidApproachAbortMps, true);
        }
        /*
         * 0 Master Init
         * 10 Init sensors
         * 11 check sensors to see if 'inside'
         *   in 'inside' ->20
         *   else ->100
         *   
         * 20 [OBS] Start mining while 'inside' asteroid
         *  set exit
         *  ->31
         *  
         *  31 set sensor for mining run ->32
         *  32 delay for sensors ->35
         *  
         *  35 Mining/finding
         *  
         *  40 out, call for pickup (DOCKING)
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
         *  120 Init sensors 
         *  121 found asteroid in front. (approach)
         *     move forward until close.  then ->31
         *     
         *     130 far travel to asteroid.
         *     Arrive->135
         *     Collision ->140
         *     135 We have arrived from far travel. Await slower movement ->120
         *     
         *     140 Collision during far travel ->150
         *     150 avoid collision during far travel. 
         *      Arrive ->130 
                Collision ->160

            160 2nd collision. 
                Stop, ->MODE_ATTENTION
         *     
         *     
         *  300 exited asteroid
         * 
         * 400 start camera scan for near-by asteroids
         * 410 do camera scan.
         * 
         * 
         */
        private StringBuilder strbMining = new StringBuilder();

        double miningElapsedMs = 0;

        long miningAsteroidID = -1;

        bool bValidExit = false;

        QuadrantCameraScanner miningfrontScanner;
        QuadrantCameraScanner miningbackScanner;
        QuadrantCameraScanner miningleftScanner;
        QuadrantCameraScanner miningrightScanner;
        QuadrantCameraScanner miningtopScanner;
        QuadrantCameraScanner miningbottomScanner;

        void doModeFindOre()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;
            IMySensorBlock sb2;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":FindOre", textPanelReport);
            Echo("FIND ORE:current_state=" + current_state.ToString());
            double maxThrust = calculateMaxThrust(thrustForwardList);
//            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
//            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;
//            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

//            Echo("Cargo=" + cargopcent.ToString() + "%");

//            Echo("velocity=" + velocityShip.ToString("0.00"));
//            Echo("miningElapsedMs=" + miningElapsedMs.ToString("0.00"));

            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
            StatusLog("clear", txtPanel);

            if (bValidAsteroid)
                debugGPSOutput("Pre-Valid Ast", vTargetAsteroid);
//            if (miningAsteroidID > 0)
                Echo("Our Asteroid=" + miningAsteroidID.ToString());

            //            if(vExpectedExit.AbsMax()>.5)
            {
//                Vector3D vT = gpsCenter.GetPosition() + vExpectedExit * 150;
//                debugGPSOutput("ExpectedExit", vT);
            }
            switch (current_state)
            {
                case 0:
                    if (fMiningMinThrust < fTargetMiningMps * 1.1f)
                        fMiningMinThrust = fTargetMiningMps * 1.1f;
                    bValidAsteroid = false; /// really?  shouuldn't we be keepint this?
                    bValidExit = false;
                    bWaitingCargo = false;

                    ResetMotion();
                    //                    turnDrillsOff();
                    turnEjectorsOff();

                    current_state = 10;
                    bWantFast = true;

                    if (!HasDrills())
                    {
                        current_state = 400;
                        return;
                    }

                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                    }
                    sensorsList[0].DetectAsteroids = true;
                    sensorsList[1].DetectAsteroids = true;

                    if (miningAsteroidID > 0) // return to a known asteroid
                    {
                        bValidAsteroid = true;
                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
                        vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();
                        vExpectedExit.Normalize();

                        current_state = 120;

                    }

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
                        if (miningElapsedMs < dSensorSettleWaitMS) return;

                        aSensors = activeSensors();
                        bool bFoundAsteroid = false;
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            if (AsteroidProcessLDEI(lmyDEI))
                                bFoundAsteroid = true;

/*
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
                            */
                        }
                        if (bFoundAsteroid) 
                        {
                            current_state = 100;
                        }
                        else
                        {
                            // no asteroid in sensor range.  Try cameras
                            current_state = 400;
                        }
                    }
                    break;
                case 20:
                    {
                        // started find ore while 'inside' asteroid.
                        // point towards exit
                        /*
                        vExpectedExit = gpsCenter.GetPosition() - currentAst.Position;
                        vExpectedExit.Normalize();
                        bValidExit = true;
                        */
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
                    if (miningElapsedMs < dSensorSettleWaitMS) return; // delay for sensor settling
                    current_state = 35;
                    break;
                case 35:
                    { // active mining
                      // TODO: check just the front sensor and we are 'exiting' if no asteroid active.
                      //
                        sb = sensorsList[0];
                        sb2 = sensorsList[1];
                        bool bLocalAsteroid = false;
                        bool bForwardAsteroid = false;
                        bool bSourroundAsteroid = false;
                        bool bLarge = false;
                        bool bSmall = false;
                        SensorActive(sb, ref bForwardAsteroid,ref bLarge, ref bSmall);
                        SensorActive(sb2, ref bSourroundAsteroid, ref bLarge, ref bSmall);

                        aSensors = activeSensors();
                        Echo(aSensors.Count + " Active Sensors");
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            if (AsteroidProcessLDEI(lmyDEI))
                                bLocalAsteroid = true;
                        }
                        if (!bLocalAsteroid)
                        { // we have exited the asteroid.
                            Echo("No Local Asteroid found");
                            ResetMotion();
                            if (cargopcent > cargopctlowwater || maxDeltaV < (fMiningMinThrust))
                            {
                                // we need to dump our contents
                                turnEjectorsOn();
                            }
                            current_state = 300;
                            bWantFast = true;
                        }
                        else if(bSourroundAsteroid)
                        { // we are inside asteroid
                            turnEjectorsOn();
                            //                            blockApplyAction(ejectorList, "OnOff_On");
                            if (maxDeltaV < fMiningMinThrust || cargopcent > cargopcthighwater && !bWaitingCargo) //
                            {
                                ResetMotion();
                                turnEjectorsOn();
                                bWaitingCargo = true;
                            }
                            if (bWaitingCargo)
                            { // continue to wait
                                ResetMotion();
                                // need to check how much stone we have.. if zero(ish), then we're full.. go exit.
                                OreDoCargoCheck();
                                if (currentStoneAmount() < 15)
                                {
                                    // we are full and not much stone ore in us...
                                    ResetMotion();
                                    turnEjectorsOff();
                                    setMode(MODE_EXITINGASTEROID);
                                }
                                // TODO: Needs time-out
                                Echo("Cargo above low water: Waiting");
                                if (maxDeltaV > fMiningMinThrust && cargopcent < cargopctlowwater)
                                    bWaitingCargo = false; // can now move.
                            }
                            else
                            {
                                GyroMain("forward", vExpectedExit, gpsCenter);
                                //                                GyroMain("forward", vExpectedExit, gpsCenter);
                                turnDrillsOn();
                                mineMoveForward(fTargetMiningMps, fMiningAbortMps);
                                //                                bWantFast = true;
                                bWantMedium = true;
                            }
                        }
                        else
                        {
                            // we have nothing in front, but are still close
                            turnDrillsOff();
                            mineMoveForward(fAsteroidApproachMps, fAsteroidApproachAbortMps);
                            bWantMedium = true;
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
                    if (miningElapsedMs < dSensorSettleWaitMS) return; // delay for sensor settling
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
                            AsteroidProcessLDEI(lmyDEI);
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
                                        MinerProcessScan(lastDetectedInfo);
                                        /*
                                        addDetectedEntity(lastDetectedInfo);
                                        vTargetAsteroid = (Vector3D)lastDetectedInfo.HitPosition;
                                        bValidAsteroid = true;
                                        vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();
                                        vExpectedExit.Normalize();
                                        bValidExit = true;
                                        */

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
                    { // asteroid NOT in front. big sensor search for asteroids in area
                        sleepAllSensors();
                        setSensorShip(sensorsList[0], 50, 50, 50, 50, 50, 50);
                        miningElapsedMs = 0;
                        current_state = 111;
                    }
                    break;
                case 111:
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < dSensorSettleWaitMS) return; // delay for sensor settling
                    current_state++;
                    break;
                case 112:
                    { // asteroid not in front. Check sensors
                        aSensors = activeSensors();
                        //                        bValidAsteroid = false;
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            AsteroidProcessLDEI(lmyDEI);
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
                    sleepAllSensors();
                    setSensorShip(sensorsList[0], 5, 5, 5, 5, 25, 0);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                    miningElapsedMs = 0;
                    current_state = 121;

                    break;
                case 121:
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

                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS) return;

                        if (bAimed)
                        {
                            double scandist;
                            double distanceSQ = 0;
                            if (bValidAsteroid)
                            {
                                distanceSQ = (vTargetAsteroid - gpsCenter.GetPosition()).LengthSquared();
                                scandist = distanceSQ;
                                Echo("distanceSQ=" + distanceSQ.ToString("0.00"));

                                // TODO: we need to keep the radius/BB of the target asteroid..
                                //                                if (lastDetectedInfo.BoundingBox.Contains(vTargetLocation) != ContainmentType.Disjoint)
                                if (distanceSQ > 512 * 512)
                                {
                                    current_state = 130;
                                    bWantFast = true;
                                    return;
                                }
                            }
                            else scandist = 500; // unknown scan distance

                            bWantFast = false;
                            bWantMedium = true;
                            if (doCameraScan(cameraForwardList, scandist))
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
                                            if (!bValidInitialContact)
                                            {
                                                vInitialContact = vLastContact;
                                                bValidInitialContact = true;
                                            }
                                            current_state = 31;
                                            ResetMotion();
                                        }
                                        else if (distance > 100)
                                        {
                                            vInitialContact = (Vector3D)lastDetectedInfo.HitPosition;
                                            current_state = 130;// do faster travel
                                        }
                                        else
                                        {
                                            mineMoveForward(fAsteroidApproachMps, fAsteroidApproachAbortMps);
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
                                    //                                    setMode(MODE_ATTENTION);
                                    // or we made a whole there..

                                }

                            }
                            // ELSE no scan available.. just wait
                            else Echo("No Camera SCAN!");

                            // check sensors for one in front of us
                            aSensors = activeSensors();
                            bool bFoundAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
//                                StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
//                                Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                if (AsteroidProcessLDEI(lmyDEI))
                                    bFoundAsteroid = true;

                            }
                            if (bFoundAsteroid)
                            {
                                current_state = 31;
                            }
                        }
                        else
                        {
                            Echo("Aiming");
                        }
                        // BUG: Gets stuck in state if nothing hit.. (ie, donut)
                    }
                    break;
                case 130:
                    { // far travel to asteroid. use doTravelMovement
                        Echo("Far travel to asteroid");

                        doTravelMovement(vTargetAsteroid, 45, 135, 140, true);
                        break;
                    }
                case 135:
                    {// we have 'arrived' from far travel
                        // wait for motion to slow
                        if (velocityShip < fAsteroidApproachMps)
                            current_state = 120;
                        ResetMotion();
                        break;
                    }
                case 140:
                    {
                        ResetTravelMovement();
                        ResetMotion();
                        calcCollisionAvoid(vInitialContact);
                        current_state = 150;
                        break;
                    }
                case 150:
                    {
                        doTravelMovement(vAvoid, 5.0f, 130, 160);
                        break;
                    }
                case 160:
                    {
                        ResetTravelMovement();
                        ResetMotion();
                        setMode(MODE_ATTENTION);
                        break;
                    }
                case 300:
                    {
                        // we have exitted the asteroid.  Prepare for another run or to go dock
                        Echo("Exitted!");
                        ResetMotion();
                        sleepAllSensors();

                        if (maxDeltaV < fMiningMinThrust || cargopcent > cargopctlowwater)
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
                case 400:
                    { // init camera scan for asteroids
                        ResetMotion();
                        turnEjectorsOn();
                        sleepAllSensors();
                        miningElapsedMs = 0;

                        // initialize cameras
                        miningfrontScanner = new QuadrantCameraScanner(this, cameraForwardList, 5000);
                        miningbackScanner = new QuadrantCameraScanner(this, cameraBackwardList, 5000);
                        miningleftScanner = new QuadrantCameraScanner(this, cameraLeftList, 5000);
                        miningrightScanner = new QuadrantCameraScanner(this, cameraRightList, 5000);
                        miningtopScanner = new QuadrantCameraScanner(this, cameraUpList, 5000);
                        miningbottomScanner = new QuadrantCameraScanner(this, cameraDownList, 5000);

                        current_state = 410;
                        break;
                    }
                case 410:
                    {
                        StatusLog("Long Range Scan", textPanelReport);
                        if (miningfrontScanner == null) // in case we reload/compile in this state..
                            current_state = 400;
                        bWantMedium = true;
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        // use for timeout...

                        // do camera scans

                        if (miningfrontScanner.DoScans())
                        {
                            AsteroidProcessLDEI(miningfrontScanner.myLDEI);
                        }
                        if (miningbackScanner.DoScans())
                        {
                            AsteroidProcessLDEI(miningbackScanner.myLDEI);
                        }
                        if (miningleftScanner.DoScans())
                        {
                            AsteroidProcessLDEI(miningleftScanner.myLDEI);
                        }
                        if (miningrightScanner.DoScans())
                        {
                            AsteroidProcessLDEI(miningrightScanner.myLDEI);
                        }
                        if (miningtopScanner.DoScans())
                        {
                            AsteroidProcessLDEI(miningtopScanner.myLDEI);
                        }
                        if(miningbottomScanner.DoScans())
                        {
                            AsteroidProcessLDEI(miningbottomScanner.myLDEI);
                        }

                        // take the first one found.
                        // TODO: do all search and then choose 'best' (closest?)
                        // TODO: Aim at the hit position and not 'CENTER' for more randomized start on asteroid
                        // TODO: once we find asteroid(s) choose how to find ore intelligently and not just randomly
                        /*
                        if (bValidAsteroid)
                            current_state = 120;
                            */
                        string s = "";
                        s += "Front: ";
                        if (miningfrontScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += miningfrontScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s+=" " +miningfrontScanner.myLDEI.Count +" asteroids";
                        s += "\n";

                        s += "Back: ";
                        if (miningbackScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += miningbackScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + miningbackScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Left: ";
                        if (miningleftScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += miningleftScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + miningleftScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Right: ";
                        if (miningrightScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += miningrightScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + miningrightScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Top: ";
                        if (miningtopScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += miningtopScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + miningtopScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        s += "Bottom: ";
                        if (miningbottomScanner.DoneScanning())
                            s += "DONE!";
                        else
                        {
                            s += miningbottomScanner.SCAN_DISTANCE.ToString("0") + " meters";
                        }
                        s += " " + miningbottomScanner.myLDEI.Count + " asteroids";
                        s += "\n";

                        if (AsteroidFindNearest() < 0)
                            s += "No Known Asteroid";
                        else s += "FOUND at least one asteroid!";

                        StatusLog(s, textPanelReport);
                        Echo(s);


                        if (
                            miningfrontScanner.DoneScanning() &&
                            miningbackScanner.DoneScanning() &&
                            miningleftScanner.DoneScanning() &&
                            miningrightScanner.DoneScanning() &&
                            miningtopScanner.DoneScanning() &&
                            miningbottomScanner.DoneScanning()
                            )
                        {
                            //                            long asteroidID = -1;
                            if (HasDrills())
                            {
                                miningAsteroidID = AsteroidFindNearest();
                                if (miningAsteroidID < 0)
                                {
                                    // all scans have run and didn't find asteroid..
                                    setMode(MODE_ATTENTION);
                                }
                                else
                                {
                                    bValidAsteroid = true;
                                    vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
                                    vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();
                                    vExpectedExit.Normalize();

                                    current_state = 120;
                                }
                            }
                            else
                            { // if no drills, we are done.
                                setMode(MODE_IDLE);
                            }
                        }
                        break;
                    }
                case 425:
                    {
                        // aim at asteroid location
                        if(!bValidExit)
                        {
                            setMode(MODE_ATTENTION);
                        }
                        {
                            bWantFast = true;
                            if (GyroMain("forward", vExpectedExit - gpsCenter.GetPosition(), gpsCenter))
                            {
                                // we are aimed
                                current_state = 120;
                            }
                        }
                        break;
                    }
            }
        }

        void doModeGotoOre()
        {
            /*
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;
            */
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":GotoOre!", textPanelReport);
            Echo("GOTO ORE:current_state=" + current_state.ToString());
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
            /*
            IMySensorBlock sb;
            */

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":Exiting!", textPanelReport);
            Echo("Exiting: current_state=" + current_state.ToString());
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
         * Turn by quarters.  Left, then Left until about 180   
         * 20 - turn around until aimed 
         * and them move forward until exittedasteroid ->40
         * 
         * 40 when out, call for pickup
         * 
         */

            switch (current_state)
            {
                case 0: //0 - Master Init
                    current_state = 10;
                    ResetMotion();
                    //                    turnDrillsOff();
                    turnEjectorsOn();
                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                    }
                    break;
                case 10://10 - Init sensors, 
                    sleepAllSensors();
                    setSensorShip(sensorsList[0], 2, 2, 2, 2, 15, 15);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                    miningElapsedMs = 0;
                    current_state = 11;
                    break;
                case 11://11 - await sensor set
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < dSensorSettleWaitMS) return;
                    current_state = 20;
                    break;
                case 20: //20 - turn around until aimed ->30
                    {
                        bWantFast = true;
                        turnDrillsOn();

                        // we want to turn on our horizontal axis as that should be the 'wide' one.
                        bool bAimed = false;
                        double yawangle = -999;
                        Echo("vTarget=" + Vector3DToString(vLastContact));
                        yawangle = CalculateYaw(vLastContact, gpsCenter);
            Echo("yawangle=" + yawangle.ToString());
                        double aYawAngle = Math.Abs(yawangle);
                        bAimed = aYawAngle < .05;


                        // turn slower when >180 since we are in tunnel and drills are cutting a path
                        float maxYPR = GyroControl.MaxYPR;
                Echo("maxYPR=" + maxYPR.ToString("0.00"));
                        if (aYawAngle > 1.0) maxYPR = maxYPR / 3; 

                        DoRotate(yawangle, "Yaw", maxYPR, 0.33f);

                        /*
                        //minAngleRad = 0.1f;
                        bAimed=GyroMain("backward", vExpectedExit, gpsCenter);

                       // minAngleRad = 0.01f;
                        // GyroMain("backward", vExpectedExit, gpsCenter);
                        */
                        if (bAimed)
                        {
                            current_state = 30;
                            //mineMoveForward(fTargetMiningmps,fAbortmps);
                        }
                        
                    }
                    break;
                case 30:
                    {
                        bool bAimed = false;
                        bAimed = GyroMain("backward", vExpectedExit, gpsCenter);
                        if (bAimed)
                        {
                            bWantMedium = true;
                            mineMoveForward(fTargetMiningMps, fMiningAbortMps);
                        }
                        else bWantFast = true;

                        bool bLocalAsteroid = false;
                        aSensors = activeSensors();
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            if (AsteroidProcessLDEI(lmyDEI))
                                bLocalAsteroid = true;
                        }
                        if (!bLocalAsteroid)
                        {
                            ResetMotion();
                            //                            turnDrillsOff();
                            sleepAllSensors();
                            current_state = 40;
                        }
                        break;
                    }
                case 40://40 when out, call for pickup
                    {
                        turnDrillsOff();
                        // we should probably give hint to docking as to WHY we want to dock..
                        setMode(MODE_DOCKING);
                    }
                    break;
                default:
                    {
                        Echo("UNKNOWN STATE!");
                        break;
                    }
            }
        }

        int iMMFWiggle = 0;
        void mineMoveForward(float fTarget, float fAbort)
        {
            Echo("mMF " + iMMFWiggle.ToString());
            double maxThrust = calculateMaxThrust(thrustForwardList);
            //            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
            //            Echo("effectiveMass=" + effectiveMass.ToString("N0"));
            double maxDeltaV = (maxThrust) / effectiveMass;
            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

            float thrustPercent = (float)(fTarget / maxDeltaV);
            Echo("thrustPercent=" + thrustPercent.ToString("0.00"));

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
                Echo("Push ");
                powerUpThrusters(thrustForwardList, thrustPercent + iMMFWiggle);
//                powerUpThrusters(thrustForwardList, 15f + iMMFWiggle);
            }
            else if(velocityShip<(fTarget*1.2))
            {
                // we are around target. 90%<-current->120%
                 Echo("Coast");
                iMMFWiggle /= 2;
               // turn off reverse thrusters and 'coast'.
                powerDownThrusters(thrustBackwardList, thrustAll, true);
                powerDownThrusters(thrustForwardList);
            }
            else
            { // above 120% target, but below abort
                Echo("Coast2");
                iMMFWiggle /= 2;
                powerUpThrusters(thrustForwardList, 1f); // coast
            }

        }

        void MinerProcessScan(MyDetectedEntityInfo mydei)
        {
            if (mydei.IsEmpty())
            {
                return;
            }
            addDetectedEntity(mydei);
            if (mydei.Type == MyDetectedEntityType.Asteroid)
            {
                addAsteroid(mydei);
                if (!bValidAsteroid)
                {
                    bValidAsteroid = true;
                    vTargetAsteroid = mydei.Position;
                    //                currentAst.EntityId = mydei.EntityId;
                    //                currentAst.BoundingBox = mydei.BoundingBox;
                    if (mydei.HitPosition != null) vExpectedExit = (Vector3D)mydei.HitPosition - gpsCenter.GetPosition();
                    else vExpectedExit = vTargetAsteroid - gpsCenter.GetPosition();
                    vExpectedExit.Normalize();
                    bValidExit = true;
                }
            }
        }

    }
}