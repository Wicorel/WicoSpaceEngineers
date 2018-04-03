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
         *     Use NAV
         *     Arrive->135
         *     [OBS] Collision ->140
         *     135 We have arrived from far travel. Await slower movement ->120
         *     
         *     [OBS]140 Collision during far travel ->150
         *     [OBS]150 avoid collision during far travel. 
         *      Arrive ->130 
                Collision ->160

            [OBS]160 2nd collision. 
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

//        long miningAsteroidID = -1;

        bool bValidExit = false;

        bool bBoringOnly = false;

//        QuadrantCameraScanner miningfrontScanner;
//        QuadrantCameraScanner miningbackScanner;
//        QuadrantCameraScanner miningleftScanner;
//        QuadrantCameraScanner miningrightScanner;
//        QuadrantCameraScanner miningtopScanner;
//        QuadrantCameraScanner miningbottomScanner;

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
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
//            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;
//            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

//            Echo("Cargo=" + cargopcent.ToString() + "%");

//            Echo("velocity=" + velocityShip.ToString("0.00"));
//            Echo("miningElapsedMs=" + miningElapsedMs.ToString("0.00"));

 //           IMyTextPanel txtPanel = getTextBlock("Sensor Report");
 //           StatusLog("clear", txtPanel);

            if (bValidAsteroid)
                debugGPSOutput("Pre-Valid Ast", vTargetAsteroid);
//            if (miningAsteroidID > 0)
//                Echo("Our Asteroid=" + miningAsteroidID.ToString());

            //            if(vExpectedExit.AbsMax()>.5)
            {
//                Vector3D vT = shipOrientationBlock.GetPosition() + vExpectedExit * 150;
//                debugGPSOutput("ExpectedExit", vT);
            }
            if (current_state > 0)
            {
                // only need to do these like once per second. or if something major changes.
                OreDoCargoCheck();
            }
            switch (current_state)
            {
                case 0:
                    if (fMiningMinThrust < fTargetMiningMps * 1.1f)
                        fMiningMinThrust = fTargetMiningMps * 1.1f;
                    bValidAsteroid = false; /// really?  shouuldn't we be keepint this?
                    bValidExit = false;
                    bMiningWaitingCargo = false;

                    ResetMotion();
                    //                    turnDrillsOff();
                    turnEjectorsOff();
                    OreDoCargoCheck(true); // init ores to what's currently in inventory

 //                   current_state = 10;
                    bWantFast = true;

/*                    if (!HasDrills())
                    {
                        current_state = 400;
                        return;
                    }
                    */
                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                        return;
                    }
                    sensorsList[0].DetectAsteroids = true;
                    sensorsList[1].DetectAsteroids = true;

                    // Can we turn in our own tunnel?
                    if (shipDim.LengthInMeters() > shipDim.WidthInMeters() && shipDim.LengthInMeters() > shipDim.HeightInMeters())
                        bBoringOnly = true;
                    else bBoringOnly = false;

                    if (miningAsteroidID <= 0) // no known asteroid
                    {
                        // check if we know one
                        miningAsteroidID = AsteroidFindNearest();
                    }
                    if (miningAsteroidID > 0) // return to a known asteroid
                    {
                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);

                        bValidAsteroid = true;
                        vExpectedAsteroidExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
                        vExpectedAsteroidExit.Normalize();

                        current_state = 120;
                    }
                    else
                    {
                        StartScans(iMode,5); // try again after a scan
                    }

                    break;
                case 5:
                    // we have done a scan.  check for found asteroids

                    // TODO: pretty much duplicate code from just above.
                    if (miningAsteroidID <= 0) // no known asteroid
                    {
                        // check if we know one
                        miningAsteroidID = AsteroidFindNearest();
                    }
                    if(miningAsteroidID > 0)
                    {
                        // we have a valid asteroid.
                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);

                        bValidAsteroid = true;
                        vExpectedAsteroidExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
                        vExpectedAsteroidExit.Normalize();

                        current_state = 120;
                    }
                    else
                    {
                        setMode(MODE_ATTENTION); // no asteroid to mine.
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
//                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
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
                        vExpectedExit = shipOrientationBlock.GetPosition() - currentAst.Position;
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
                        Echo("Actively Mining forward");
                        if (bBoringOnly) Echo("Boring Miner");
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
 //                       Echo(aSensors.Count + " Active Sensors");
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
                            if (cargopcent > MiningCargopctlowwater || maxDeltaV < (fMiningMinThrust))
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
                            if (maxDeltaV < fMiningMinThrust || cargopcent > MiningCargopcthighwater && !bMiningWaitingCargo) //
                            {
                                ResetMotion();
                                turnEjectorsOn();
                                bMiningWaitingCargo = true;
                            }
                            if (bMiningWaitingCargo)
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
                                if (maxDeltaV > fMiningMinThrust && cargopcent < MiningCargopctlowwater)
                                    bMiningWaitingCargo = false; // can now move.
                            }
                            else
                            {
                                GyroMain("forward", vExpectedAsteroidExit, shipOrientationBlock);
                                //                                GyroMain("forward", vExpectedExit, shipOrientationBlock);
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
//                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
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
                                                            scandist = (shipOrientationBlock.GetPosition() - vTargetAsteroid).Length();
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
                                        vExpectedExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
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
                        Echo("set big sensors");

                        sleepAllSensors();
                        setSensorShip(sensorsList[0], 50, 50, 50, 50, 50, 50);
                        miningElapsedMs = 0;
                        current_state = 111;
                        Echo("current_State now=" + current_state.ToString());
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
//                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            AsteroidProcessLDEI(lmyDEI);
                        }
                        if (bValidAsteroid)
                        {
                            bWantFast = true;
                            if (GyroMain("forward", vTargetAsteroid - shipOrientationBlock.GetPosition(), shipOrientationBlock))
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

                        Echo(bValidExit.ToString() + " " + Vector3DToString(vExpectedAsteroidExit));
                        bAimed = GyroMain("forward", vExpectedAsteroidExit, shipOrientationBlock);

                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS) return;

                        if (bAimed)
                        {
                            double scandist;
                            double distanceSQ = 0;
                            if (bValidAsteroid)
                            {
                                distanceSQ = (vTargetAsteroid - shipOrientationBlock.GetPosition()).LengthSquared();
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
                                            vLastAsteroidContact = shipOrientationBlock.GetPosition();
                                            if (!bValidInitialAsteroidContact)
                                            {
                                                vInitialAsteroidContact = vLastAsteroidContact;
                                                bValidInitialAsteroidContact = true;
                                            }
                                            current_state = 31;
                                            ResetMotion();
                                        }
                                        else if (distance > 100)
                                        {
                                            vInitialAsteroidContact = (Vector3D)lastDetectedInfo.HitPosition;
                                            current_state = 130;// do faster travel
                                        }
                                        else
                                        {
                                            mineMoveForward(fAsteroidApproachMps, fAsteroidApproachAbortMps);
                                        }
                                    }
                                    //                                      bValidExit = true;
                                    //                                        vExpectedExit = vTarget - shipOrientationBlock.GetPosition();
                                }
                                else
                                {
                                    /*
                                    // we scanned, but didn't hit anything.  it's likely a donut
                                    double distance = (vTargetAsteroid - shipOrientationBlock.GetPosition()).Length();
                                    Echo("Distance=" + distance.ToString());
                            mineMoveForward(fTargetMiningmps,fAbortmps);
                                    if (distance < 5)
                                    {
                                        vExpectedExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
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
                    {
                        // start NAV travel
                        NavGoTarget(vTargetAsteroid, iMode, 135);
                    }
                    break;
                    /*
                case 130:
                    { // far travel to asteroid. use doTravelMovement

                        // to reduce code size, we should require NAV module and then resume on MODE_ARRIVEDTARGET
                        // need some kind of contract for that system...

                        Echo("Far travel to asteroid");

                        doTravelMovement(vTargetAsteroid, 45, 135, 140, true);
                        break;
                    }
                    */
                case 135:
                    {// we have 'arrived' from far travel
                        // wait for motion to slow
                        if (velocityShip < fAsteroidApproachMps)
                            current_state = 120;
                        ResetMotion();
                        break;
                    }
                    /*
                case 140:
                    {
                        ResetTravelMovement();
                        ResetMotion();
                        calcCollisionAvoid(vInitialAsteroidContact);
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
                    */
                case 300:
                    {
                        // we have exitted the asteroid.  Prepare for another run or to go dock
                        Echo("Exitted!");
                        ResetMotion();
                        sleepAllSensors();

                        if (maxDeltaV < fMiningMinThrust || cargopcent > MiningCargopctlowwater)
                        {
                            setMode(MODE_DOCKING);
                        }
                        else
                        {
                             vLastAsteroidExit = shipOrientationBlock.GetPosition();
 //                           dist = (vLastExit - vInitialExit).Length();
                            if (!bValidInitialAsteroidExit)
                            {
                                vInitialAsteroidExit = vLastAsteroidExit;
                                bValidInitialAsteroidExit = true;
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
            /*
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb;
            */
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":GotoOre!", textPanelReport);
            Echo("GOTO ORE:current_state=" + current_state.ToString());
            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
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

//            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
//            StatusLog("clear", txtPanel);

        }

        void doModeMine()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":MINE", textPanelReport);
            Echo("MINE:current_state=" + current_state.ToString());
            double maxThrust = calculateMaxThrust(thrustForwardList);
            //            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
            //            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;
            Echo("Our Asteroid=" + miningAsteroidID.ToString());
            switch (current_state)
            {
                case 0:
                    if (fMiningMinThrust < fTargetMiningMps * 1.1f)
                        fMiningMinThrust = fTargetMiningMps * 1.1f;
                    bValidAsteroid = false; /// really?  shouuldn't we be keeping this?
                    bValidExit = false;
                    bMiningWaitingCargo = false;

                    ResetMotion();
                    //                    turnDrillsOff();
                    turnEjectorsOff();

                    current_state = 10;
                    bWantFast = true;

                    if (!HasDrills())
                    {
                        setMode(MODE_ATTENTION);
                        return;
                    }
                    break;
                case 10: // check for asteroid in front of us
                    double scandist = 500;
                    if (doCameraScan(cameraForwardList, scandist))
                    { // we scanned
                        if (!lastDetectedInfo.IsEmpty())
                        {  // we hit something

                            if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                            {
                                MinerProcessScan(lastDetectedInfo);
                                setMode(MODE_FINDORE);
                                current_state = 120;
                            }
                        }
                        else
                        {
                            // no asteroid detected.  Check surroundings for one.
                            current_state = 110;
                            bValidExit = false;
                        }
                    }
                    break;

            }
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
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;

            double maxThrust = calculateMaxThrust(thrustForwardList);

            Echo("effectiveMass=" + effectiveMass.ToString("N0"));
//            Echo("maxThrust=" + maxThrust.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;

            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
            Echo("Cargo=" + cargopcent.ToString() + "%");

            Echo("velocity=" + velocityShip.ToString("0.00"));
            Echo("Boring=" + bBoringOnly.ToString());
//            Echo("#Sensors=" + sensorsList.Count);
//            Echo("width=" + shipDim.WidthInMeters().ToString("0.0"));
//            Echo("height=" + shipDim.HeightInMeters().ToString("0.0"));
//            Echo("length=" + shipDim.LengthInMeters().ToString("0.0"));

//            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
//            StatusLog("clear", txtPanel);

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
                    if (shipDim.LengthInMeters() > shipDim.WidthInMeters() && shipDim.LengthInMeters() > shipDim.HeightInMeters())
                        bBoringOnly = true;
                    else bBoringOnly = false;

                    current_state = 10;
                    ResetMotion();
                    //                    turnDrillsOff();
                    turnEjectorsOn();
                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Exit Asteroid: Not Enough Sensors!", textLongStatus, true);
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
                        Echo("vTarget=" + Vector3DToString(vLastAsteroidContact));
                        yawangle = CalculateYaw(vLastAsteroidContact, shipOrientationBlock);
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
                        bAimed=GyroMain("backward", vExpectedExit, shipOrientationBlock);

                       // minAngleRad = 0.01f;
                        // GyroMain("backward", vExpectedExit, shipOrientationBlock);
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
                        string sOrientation = "backward";
                        if (bBoringOnly)
                        {
                            sOrientation = "forward";
                        }
                        bAimed = GyroMain(sOrientation, vExpectedAsteroidExit, shipOrientationBlock);
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
//                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
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
//            Echo("mMF " + iMMFWiggle.ToString());
            double maxThrust = calculateMaxThrust(thrustForwardList);
            //            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
            //            Echo("effectiveMass=" + effectiveMass.ToString("N0"));
            double maxDeltaV = (maxThrust) / effectiveMass;
 //           Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

            float thrustPercent = (float)(fTarget / maxDeltaV);
//            Echo("thrustPercent=" + thrustPercent.ToString("0.00"));

            if (velocityShip > fAbort)
            {
 //               Echo("ABORT");
                powerDownThrusters(thrustAllList);
                iMMFWiggle = 0;
            }
            else if (velocityShip < (fTarget*0.90))
            {
                if (velocityShip < 0.5f)
                    iMMFWiggle++;
//                Echo("Push ");
                powerUpThrusters(thrustForwardList, thrustPercent + iMMFWiggle);
//                powerUpThrusters(thrustForwardList, 15f + iMMFWiggle);
            }
            else if(velocityShip<(fTarget*1.2))
            {
                // we are around target. 90%<-current->120%
//                 Echo("Coast");
                iMMFWiggle /= 2;
               // turn off reverse thrusters and 'coast'.
                powerDownThrusters(thrustBackwardList, thrustAll, true);
                powerDownThrusters(thrustForwardList);
            }
            else
            { // above 120% target, but below abort
//                Echo("Coast2");
                iMMFWiggle /= 2;
                powerUpThrusters(thrustForwardList, 1f); // coast
            }

        }


    }
}