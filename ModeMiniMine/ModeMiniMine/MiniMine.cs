using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private StringBuilder strbMining = new StringBuilder();

        double miningElapsedMs = 0;
        double miningChecksElapsedMs = -1;

        bool bValidExit = false;

        // need to serialize
        // should reverse meaning to bTunnelTurnable
        bool bBoringOnly = false;

        // are we finding ore by test bores, or full destructive mining?
        // are we going to a specific ore?

        /*
         * 0 Master Init
         * checks for known asteroid. 
         * if none known, check for nearby
         * if found->120
         * If none found ->1
         * 
         * 1: 
         * scan in front for asteroid. 
         * if found->120
         * if none directly in front, Starts Scans->5
         * 
         * 5: a requested scan was just completed.  Check for found asteroids
         * if found->120
         * if none found ->MODE_ATTENTION
         * 
         * 10 Init sensors
         * 11 check sensors to see if 'inside'
         *   in 'inside' ->20
         *   else ->100
         *   
         * 20 [OBS] Start mining while 'inside' asteroid
         *  set exit
         *  ->31
         *  
         *  31 set sensor for mining run 
         *  calculate asteroid vectors 
         *  ->32
         *  
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
         *  120 we have a known asteroid.  go toward our starting location
         *  check cargo, etc.  dock if needed
         *  wait for velocity -> 121
         *  
         *  121 
         *   Far travel ->190 Arrival ->195
         *   if "close enough" ->125
         *   
         *   125 move forward to start loc
         *   ->130
         *  
         *  130 asteroid should be close.  Approach
         *  Init sensors ->131
         *  
         *  131 align to 'up'. ->134
         *  134 align forward ->137
         *  137 align to 'up' ->140
         *  
         *  140 asteroid in front.
         *  check if the bore has any asteroid with raycast
         *  if found->150.  else next bore ->120
         *  
         *  150
         *  (approach) 
         *  Needs vexpectedAsteroidExit set.
         *  Assumes starting from current position
         *  
         *     move forward until close.  then ->31
         *     
         *  190 far travel to asteroid.
         *     ->MODE_NAV  Arrive->195  Error->MODE_ATTENTION
         *     
         *  195 We have arrived from far travel. Await slower movement ->120
         *     
         *     
         *  300 exited asteroid
         *  if above waters, call for pickup (DOCKING)
         *  else, turn around and do it again 
         * 
         * 500 bores all completed.
         * do any final checks and then remove asteroid and go home.
         * 
         * 
         */

        int BoreHoleScanMode = -1;
        Vector3D[] BoreScanFrontPoints = new Vector3D[4];

        void doModeMine()
        {
            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb1=null;
            IMySensorBlock sb2=null;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":MINE", textPanelReport);
            Echo("MINE:current_state=" + current_state.ToString());
            Echo("Mine Mode=" + AsteroidMineMode);
//            echoInstructions("MM-A:" + current_state );
            //            Echo(Vector3DToString(vExpectedAsteroidExit));
            //            Echo(Vector3DToString(vLastAsteroid            Vector3D[] corners= new Vector3D[BoundingBoxD.CornerCount];
            //            Echo(Vector3DToString(vLastAsteroidExit));
            StatusLog("clear", gpsPanel);
            debugGPSOutput("BoreStart"+AsteroidCurrentX.ToString("00")+AsteroidCurrentY.ToString("00"), vAsteroidBoreStart);
            debugGPSOutput("BoreEnd" + AsteroidCurrentX.ToString("00") + AsteroidCurrentY.ToString("00"), vAsteroidBoreEnd);

                // TODO: Cache these values.
                double maxThrust = calculateMaxThrust(thrustForwardList);
            //            Echo("maxThrust=" + maxThrust.ToString("N0"));
            if (bBoringOnly)
            {
                double maxBackThrust = calculateMaxThrust(thrustBackwardList);
                if (maxBackThrust < maxThrust)
                {
                    Echo("BACK thrust is less than forward!");
                    maxThrust = maxBackThrust;
                }
                // TODO: also check docking 'reverse' thrust iff other than 'back' connector
            }

            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
//            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            if(miningAsteroidID<=0)
                StatusLog("No Current Asteroid", textPanelReport);

            double maxDeltaV = (maxThrust) / effectiveMass;

            StatusLog("DeltaV=" + maxDeltaV.ToString("N1") + " / "  + fMiningMinThrust.ToString("N1") + "min", textPanelReport);


            //            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

            //            Echo("Cargo=" + cargopcent.ToString() + "%");

            //            Echo("velocity=" + velocityShip.ToString("0.00"));
            //            Echo("miningElapsedMs=" + miningElapsedMs.ToString("0.00"));

            //           IMyTextPanel txtPanel = getTextBlock("Sensor Report");
            //           StatusLog("clear", txtPanel);

            //            Echo("BoringCount=" + AsteroidBoreCurrent);
            //            if (bValidAsteroid)
            //                debugGPSOutput("Pre-Valid Ast", vTargetAsteroid);
            //            if (miningAsteroidID > 0)
            //                Echo("Our Asteroid=" + miningAsteroidID.ToString());
//            echoInstructions("MM-B:" + current_state);

            if (current_state > 0)
            {
                if (miningChecksElapsedMs >= 0) miningChecksElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (miningChecksElapsedMs < 0 || miningChecksElapsedMs > 1*1000)
                {
                    miningChecksElapsedMs = 0;

                    // DEBUG: generate a ray at what we're suposed to be pointing at.
                    /*
                    double scanDistance= (shipOrientationBlock.GetPosition()-vAsteroidBoreEnd).Length();
                    if (doCameraScan(cameraForwardList, scanDistance))
                    {

                    }
                    */
                    OreDoCargoCheck();
                    batteryCheck(0,false);
//                    echoInstructions("MM-C:" + current_state);
                    // TODO: check hydrogen tanks
                    // TODO: check reactor uranium
                    float fper = 0;
                    fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
                    if(fper>0.35f)
                    {
                        bWantFast = true;
                        return;
                    }
                }
                StatusLog("Cargo =" + cargopcent + "% / " + MiningCargopcthighwater + "% Max", textPanelReport);
                StatusLog("Battery " + batteryPercentage + "% (Min:" + batterypctlow + "%)", textPanelReport);
                if (TanksHasHydro()) StatusLog("H2 " + hydroPercent + "% (Min:" + batterypctlow + "%)", textPanelReport);
            }
//            echoInstructions("MM-D:" + current_state);
            if (sensorsList.Count >= 2)
            {
                sb1 = sensorsList[0];
                sb2 = sensorsList[1];
            }
            switch (current_state)
            {
                case 0:
                    bValidExit = false;
                    bMiningWaitingCargo = false;

                    ResetMotion();
                    turnEjectorsOff();
                    OreDoCargoCheck(true); // init ores to what's currently in inventory
                    MinerCalculateBoreSize();
                    MoveForwardSlowReset();

                    bWantFast = true;

                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                        return;
                    }
                    sb1.DetectAsteroids = true;
                    sb2.DetectAsteroids = true;

                    // Can we turn in our own tunnel?
                    if (shipDim.LengthInMeters() > shipDim.WidthInMeters() && shipDim.LengthInMeters() > shipDim.HeightInMeters())
                        bBoringOnly = true;
                    else bBoringOnly = false;

                    if (miningAsteroidID <= 0) // no known asteroid
                    {
                        // check if we know one
                        miningAsteroidID = AsteroidFindNearest();
                        Vector3D AsteroidPos = AsteroidGetPosition(miningAsteroidID);
                        double curDistanceSQ = Vector3D.DistanceSquared(AsteroidPos, shipOrientationBlock.GetPosition());
                        if(curDistanceSQ>5000*5000)
                        {
                            // it's too far away. ignore it.
                            miningAsteroidID = 0;
                        }
                    }
                    if (miningAsteroidID > 0) // return to a known asteroid
                    {
                        MinerCalculateBoreSize();
                        if (AsteroidMineMode == 1)
                        {

                        }
                        else
                        {
                            MinerCalculateAsteroidVector(miningAsteroidID);
                            AsteroidCalculateBestStartEnd();
                        }
                        /*
                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);

                        bValidAsteroid = true;
                        if (vExpectedAsteroidExit == Vector3D.Zero)
                        {
                            vExpectedAsteroidExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
                            vExpectedAsteroidExit.Normalize();
                        }
                        */
                        current_state = 120;
                        bWantFast = true;
                    }
                    else
                    {
                        current_state = 1;
                        bWantFast = true;
                    }
                    break;
                case 1:
                    { // no target asteroid.  Raycast in front of us for one.
                        double scandist = 2000;
                        // TODO: assumes have forward cameras.
                        if (doCameraScan(cameraForwardList, scandist))
                        { // we scanned
//                            sStartupError += " Scanned";
                            if (!lastDetectedInfo.IsEmpty())
                            {  // we hit something
//                                sStartupError += " HIT!";
                                if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                {
                                    MinerProcessScan(lastDetectedInfo);
                                    miningAsteroidID = lastDetectedInfo.EntityId;
                                }
                                if (miningAsteroidID > 0) // go to the asteroid we just found
                                {
 //                                   vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
//                                    bValidAsteroid = true;
                                    AsteroidMineMode = 1;// drill exactly where we're aimed for.
                                    MinerCalculateAsteroidVector(miningAsteroidID);

                                    // reset bore hole info to current location
                                    vAsteroidBoreStart = shipOrientationBlock.GetPosition();

                                    Vector3D vTarget =  (Vector3D)lastDetectedInfo.HitPosition - shipOrientationBlock.GetPosition() ;

                                    vAsteroidBoreEnd = vAsteroidBoreStart;
                                    vAsteroidBoreEnd+= shipOrientationBlock.WorldMatrix.Forward *(AsteroidDiameter+vTarget.Length());

                                    AsteroidUpVector = shipOrientationBlock.WorldMatrix.Up;

                                    current_state = 120;
                                    bWantFast = true;
                                }
                                else
                                {
                                    StartScans(iMode, 5); // try again after a scan
                                }
                            }
                            else
                            {
                                StartScans(iMode, 5); // try again after a scan
                            }
                        }
                        else
                        {
                            Echo("Awaiting Available camera");
                            bWantMedium = true;
                        }
                        break;
                    }
                case 5:
                    // we have done a LIDAR scan.  check for found asteroids

                    // TODO: pretty much duplicate code from just above.
                    if (miningAsteroidID <= 0) // no known asteroid
                    {
                        // check if we know one
                        AsteroidMineMode = 0; // should use default mode
                        miningAsteroidID = AsteroidFindNearest();
                    }
                    if(miningAsteroidID > 0)
                    {
                        // we have a valid asteroid.
//                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
//                        bValidAsteroid = true;
                        MinerCalculateAsteroidVector(miningAsteroidID);
                        AsteroidCalculateFirstBore();

                        current_state = 120;
                        bWantFast = true;
                    }
                    else
                    {
                        miningChecksElapsedMs = -1;
                        setMode(MODE_ATTENTION); // no asteroid to mine.
                    }
                    break;
                case 10:
                    //sb = sensorsList[0];
                    SensorsSleepAll();
                    SensorSetToShip(sensorsList[0], 2, 2, 2, 2, 2, 2);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                    miningElapsedMs = 0;
                    current_state = 11;
                    bWantMedium = true;
                    break;

                case 11:
                    {
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS) return;

                        aSensors = SensorsGetActive();
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
                        bWantFast = true;
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
                    sb1 = sensorsList[0];
                    sb2 = sensorsList[1];
                    SensorsSleepAll();
                    SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                    SensorSetToShip(sb2, (float)shipDim.WidthInMeters(), (float)shipDim.WidthInMeters(),
                        (float)shipDim.HeightInMeters(), (float)shipDim.HeightInMeters(),
                        1, (float)shipDim.LengthInMeters());
                    current_state = 32;
                    miningElapsedMs = 0;
                    bWantFast = true;
                    ResetMotion();
                    break;
                case 32:
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < dSensorSettleWaitMS)
                    {
                        bWantMedium = true;
                        return; // delay for sensor settling
                    }
                    // else:
                    bWantFast = true;
//                    vAsteroidBoreStart = AsteroidCalculateBoreStart();
                    current_state = 35;
                    break;
                case 35:
                    { // active mining
                      //
                        int eoicount = 0;
//                        echoInstructions("S=" + current_state + " " + eoicount++);
                        bool bAimed = false;
                        Echo("Mining forward");
                        StatusLog("Mining Forward!", textPanelReport);
                        if (bBoringOnly) Echo("Boring Miner");
                        //                        sb1 = sensorsList[0];
                        //                        sb2 = sensorsList[1];
                        bool bLocalAsteroid = false;
                        bool bForwardAsteroid = false;
                        bool bSourroundAsteroid = false;
                        bool bLarge = false;
                        bool bSmall = false;
                        //                        Echo("FW=" + sb1.CustomName);
                        //                        Echo("AR=" + sb2.CustomName);
                        // TODO: Make sensors optional (and just always do runs and use distance to know when done with bore.
                        sb1 = sensorsList[0];
                        sb2 = sensorsList[1];
//                        echoInstructions("S=" + current_state + "S " + eoicount++);
                        SensorIsActive(sb1, ref bForwardAsteroid, ref bLarge, ref bSmall);
                        SensorIsActive(sb2, ref bSourroundAsteroid, ref bLarge, ref bSmall);
//                        echoInstructions("S=" + current_state + "ES " + eoicount++);
                        //                        Echo("FW=" + bForwardAsteroid.ToString() + " AR=" + bSourroundAsteroid.ToString());
                        aSensors = SensorsGetActive();
                        //                        Echo(aSensors.Count + " Active Sensors");
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            //                            Echo(aSensors[i].CustomName + " ACTIVE!");
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
                            var lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            if (AsteroidProcessLDEI(lmyDEI))
                            {
                                // TODO: if we find ANOTHER asteroid in sensors, figure out what to do
                                bLocalAsteroid = true;
                            }
                        }
//                        echoInstructions("S=" + current_state + "EDEI " + eoicount++);

                        double distance = (vAsteroidBoreStart - shipOrientationBlock.GetPosition()).Length();

                        // *2 because of start and end enhancement
                        double boreLength = AsteroidDiameter + shipDim.LengthInMeters() * MineShipLengthScale * 2;
                        Echo("Distance=" + niceDoubleMeters(distance) + " (" + niceDoubleMeters(boreLength) + ")");
                        double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                        StatusLog("Bore:" + ((distance + stoppingDistance) / boreLength * 100).ToString("N0") + "%", textPanelReport);
    //                        if ((distance + stoppingDistance) < (AsteroidDiameter + shipDim.LengthInMeters() * MineShipLengthScale * 2))// even if sensors don't see anything. continue to end of the bore.
                        if ((distance + stoppingDistance) < boreLength*0.65) // if we are <65% done with bore, continue no matter what sensors say
                        {
                            bLocalAsteroid = true;
                        }
                        if (!bLocalAsteroid)
                        { // no asteroid detected on ANY sensors. ->we have exited the asteroid.
//                            Echo("No Local Asteroid found");
                            ResetMotion();
//                            echoInstructions("S=" + current_state + "RM " + eoicount++);
                            if (cargopcent > MiningCargopctlowwater || maxDeltaV < (fMiningMinThrust))
                            {
                                // we need to dump our contents
                                turnEjectorsOn();
//                                echoInstructions("S=" + current_state + "EJ " + eoicount++);
                            }
                            //                            sStartupError += "\nOut:" + aSensors.Count + " : " +bForwardAsteroid.ToString() + ":"+bSourroundAsteroid.ToString();
                            //                            sStartupError += "\nFW=" + bForwardAsteroid.ToString() + " Sur=" + bSourroundAsteroid.ToString();
                            current_state = 300;
                            bWantFast = true;
                            return;
                        }
//                        echoInstructions("S=" + current_state + "bfacheck " + eoicount++);
                        if (bForwardAsteroid)
                        { // asteroid in front of us
                            turnEjectorsOn();
                            //                            blockApplyAction(ejectorList, "OnOff_On");
                            if (maxDeltaV < fMiningMinThrust || cargopcent > MiningCargopcthighwater && !bMiningWaitingCargo) //
                            {
                                ResetMotion();
// already done                                turnEjectorsOn();
                                bMiningWaitingCargo = true;
                            }
//                            echoInstructions("S=" + current_state + "bmwc check " + eoicount++);
                            if (bMiningWaitingCargo)
                            { // continue to wait
                                bWantSlow = true;
                                ResetMotion();
                                // need to check how much stone we have.. if zero(ish), then we're full.. go exit.
//                                echoInstructions("S=" + current_state + "ODCC " + eoicount++);
//                                OreDoCargoCheck(); redundant
//                                echoInstructions("S=" + current_state + "EDOCC " + eoicount++);
                                double currStone = currentStoneAmount();
                                if (currStone < 15 && (maxDeltaV < fMiningMinThrust || cargopcent > MiningCargopctlowwater))
                                {
                                    // we are full and not much stone ore in us...
                                    ResetMotion();
                                    turnEjectorsOff();
                                    miningChecksElapsedMs = -1;
                                    setMode(MODE_EXITINGASTEROID);
                                }
                                // TODO: Needs time-out
                                StatusLog("Waiting for cargo and thrust to be available", textPanelReport);
                                Echo("Cargo above low water: Waiting");
                                if (maxDeltaV > fMiningMinThrust && cargopcent < MiningCargopctlowwater && currStone<1000)
                                    bMiningWaitingCargo = false; // can now move.
                            }
                            else
                            {
                                /*
                                                                // 'BeamRider' routine that takes start,end and tries to stay on that beam.
                                                                // Todo: probably should use center of ship BB, not COM.
                                                                Vector3D vBoreEnd = (vAsteroidBoreEnd - vAsteroidBoreStart);
                                                                Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)shipOrientationBlock).CenterOfMass);
                                                                Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);
                                //                                Echo("BoreEnd=" + Vector3DToString(vBoreEnd));
                                //                                Echo("AimEnd=" + Vector3DToString(vAimEnd));
                                //                                Echo("RejectEnd=" + Vector3DToString(vRejectEnd));
                                //                                Vector3D vCorrectedAim = vAsteroidBoreEnd - vRejectEnd;

                                                                Vector3D vCorrectedAim = (vAsteroidBoreEnd- vRejectEnd*2) - ((IMyShipController)shipOrientationBlock).CenterOfMass;

                                                                bAimed = GyroMain("forward", vCorrectedAim, shipOrientationBlock);
                                */
//                                echoInstructions("S=" + current_state + "BR " + eoicount++);
                                bAimed = BeamRider(vAsteroidBoreStart, vAsteroidBoreEnd, shipOrientationBlock);

                                turnDrillsOn();
//                                echoInstructions("S=" + current_state + "EOtDO " + eoicount++);
                                //                                Echo("bAimed=" + bAimed.ToString());
                                //                                Echo("minAngleRad=" + minAngleRad);
                                if (bAimed)
                                {
                                    Echo("Aimed");
                                    MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustForwardList, thrustBackwardList);
                                    bWantMedium = true;
                                    /*
                                    gyrosOff();
                                    bAimed = GyroMain("up", AsteroidUpVector, shipOrientationBlock);
                                    */
                                }
                                else
                                {
                                    Echo("Not Aimed");
                                    powerDownThrusters(thrustAllList);
                                    bWantFast = true;
                                }
//                                Echo("bAimed=" + bAimed.ToString());
                                /*
                                if (!bAimed) bWantFast = true;
                                else bWantMedium = true;
                                */
                            }
                        }
                        else
                        {
                            // we have nothing in front, but are still close
                            StatusLog("Exiting Asteroid", textPanelReport);
                            turnDrillsOff();
                            turnEjectorsOff(); // don't want stuff hitting us in the butt
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);

                            bAimed = GyroMain("forward", vAim, shipOrientationBlock);
                            MoveForwardSlow(fAsteroidExitMps, fAsteroidExitMps*1.25f, thrustForwardList, thrustBackwardList);
                            if (!bAimed) bWantFast = true;
                            else bWantMedium = true;
                        }
                    }
                    break;

                case 100:
                    turnDrillsOff();
                    SensorsSleepAll();
                    sb1 = sensorsList[0];
                    SensorSetToShip(sb1, 0, 0, 0, 0, 50, 0);
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
                        aSensors = SensorsGetActive();
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
                    // --shouldn't be necessary since we have scans..  but maybe no asteroid in front of aim spot?
                    { // asteroid NOT in front. big sensor search for asteroids in area
                        Echo("set big sensors");

                        SensorsSleepAll();
                        SensorSetToShip(sensorsList[0], 50, 50, 50, 50, 50, 50);
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
                        aSensors = SensorsGetActive();
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
                        miningAsteroidID = AsteroidFindNearest();
                        if (miningAsteroidID > 0) // return to a known asteroid
                        {
                            /*
                            vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);

                            bValidAsteroid = true;
                            if (vExpectedAsteroidExit == Vector3D.Zero)
                            {
                                vExpectedAsteroidExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
                                vExpectedAsteroidExit.Normalize();
                            }
                            */
                            current_state = 120;
                            bWantFast = true;
                        }
                        else
                        {
                            StartScans(iMode, 5); // try again after a scan
                        }
                    }
                    break;


                case 120:
                    {
                        // we have a known asteroid.  go toward our starting location
                        // wait for ship to slow
                        ResetMotion();
                        bool bReady = DockAirWorthy(true,false,MiningCargopcthighwater);
                        if (maxDeltaV < fMiningMinThrust || !bReady) //cargopcent > MiningCargopctlowwater || batteryPercentage < batterypctlow)
                        {
                            //TODO: check H2 tanks.
                            //TODO: Check uranium supply
                            // we don't have enough oomph to do the bore.  go dock and refill/empty and then try again.
                            bAutoRelaunch = true;
                            miningChecksElapsedMs = -1;
                            setMode(MODE_DOCKING);
                        }
                        else
                        if (velocityShip < fAsteroidApproachMps)
                        {
                            current_state = 121;
                            bWantFast = true;
                        }
                        else bWantMedium = true;
                    }
                    break;

                case 121:
                    {
                        // we have a known asteroid.  go toward our starting location

                        double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
                        Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                        // Go to start location.  possible far travel
                        bWantFast = true;
                        if (distanceSQ > 75 * 75)
                        {
                            // do far travel.
                            current_state = 190;
                            return;
                        }
                        current_state = 125;
                        break;
                    }
                case 125:
                    {
                        double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
                        Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                        double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                        StatusLog("Move to Bore Start", textPanelReport);

                        if ((distanceSQ - stoppingDistance * 2) > 2)
                        {
                            // set aim to ending location
                            Vector3D vAim = (vAsteroidBoreStart - ((IMyShipController)shipOrientationBlock).CenterOfMass);
                            bool bAimed = GyroMain("forward", vAim, shipOrientationBlock);
                            if (bAimed)
                            {
                                //                                Echo("Aimed");
                                bWantFast = false;
                                bWantMedium = true;
                                MoveForwardSlow(fAsteroidApproachMps, fAsteroidApproachAbortMps, thrustForwardList, thrustBackwardList);
                            }
                            else bWantFast = true;
                        }
                        else
                        {
                            // we have arrived
                            ResetMotion();
                            current_state = 130;
                            bWantFast = true;
                        }
                        break;
                    }
                case 130:
                    Echo("Sensor Set");
                    SensorsSleepAll();
                    SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                    SensorSetToShip(sb2, 1, 1, 1, 1, 15, -1);
                    miningElapsedMs = 0;
                    //first do a rotate to 'up'...
                    current_state = 131;
                    bWantFast = true;
                    break;
                case 131:
                    {
                        StatusLog("Borehole Alignment", textPanelReport);
                        if (velocityShip < 0.5)
                        { // wait for ship to slow down
                            double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
                            /*
                            if (distanceSQ > 1.5)
                            {
                                current_state = 120; // try again.
                            }
                            else
                            */
                            {
                                // align with 'up'
                                bWantFast = true;
                                if (GyroMain("up", AsteroidUpVector, shipOrientationBlock))
                                {
                                    current_state = 134;
                                }
                            }
                        }
                        else
                        {
                            ResetMotion();
                            bWantMedium = true;
                        }
                        break;
                    }
                case 134:
                    {
                        // align with target
                        StatusLog("Borehole Alignment", textPanelReport);
                        bWantFast = true;
                        Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                        if (GyroMain("forward", vAim, shipOrientationBlock))
                        {
                            current_state = 137;
                        }
                        break;
                    }
                case 137:
                    {
                        // align with 'up'
                        StatusLog("Borehole Alignment", textPanelReport);
                        bWantFast = true;
                        if (GyroMain("up", AsteroidUpVector, shipOrientationBlock))
                        {
                            current_state = 140;
                        }
                        break;
                    }

                case 140:
                    { // bore scan init
                        StatusLog("Bore Check Init", textPanelReport);
                        Echo("Bore Check Init");
                        BoreHoleScanMode = -1;
                        bWantFast = true;
                        // we should have asteroid in front.
                        bool bAimed = true;
                        Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                        bAimed = GyroMain("forward", vAim, shipOrientationBlock);

                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

                        if (bAimed)
                        {
                            OrientedBoundingBoxFaces orientedBoundingBox = new OrientedBoundingBoxFaces(shipOrientationBlock);
                            orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, BoreScanFrontPoints);
                            // front output order is BL, BR, TL, TR

                            // NOTE: This may be wrong order for opposite facing bores..
                            Vector3D vTL = vAsteroidBoreStart + AsteroidUpVector * MiningBoreHeight / 2 - AsteroidRightVector * MiningBoreWidth / 2;
                            Vector3D vTR = vAsteroidBoreStart + AsteroidUpVector * MiningBoreHeight / 2 + AsteroidRightVector * MiningBoreWidth / 2;
                            Vector3D vBL = vAsteroidBoreStart - AsteroidUpVector * MiningBoreHeight / 2 - AsteroidRightVector * MiningBoreWidth / 2;
                            Vector3D vBR = vAsteroidBoreStart - AsteroidUpVector * MiningBoreHeight / 2 + AsteroidRightVector * MiningBoreWidth / 2;

                            BoreScanFrontPoints[0] = vBL;
                            BoreScanFrontPoints[1] = vBR;
                            BoreScanFrontPoints[2] = vTL;
                            BoreScanFrontPoints[3] = vTR;
                            current_state = 145;
//                            current_state = 143; //Beam testing
                        }
                        else
                        {
                            bWantFast = true;
                            Echo("Aiming");
                        }
                    }
                    break;
                case 143:
                    {
                        // TESTING..  beam-rider testing
                        //                        Vector3D vCrossStart = Vector3D.Cross(vAsteroidBoreStart- ((IMyShipController)shipOrientationBlock).CenterOfMass, shipOrientationBlock.WorldMatrix.Backward);
                        //                        Vector3D vCrossEnd = Vector3D.Cross((vAsteroidBoreEnd - vAsteroidBoreStart), shipOrientationBlock.WorldMatrix.Right);

                        //https://answers.unity.com/questions/437111/calculate-vector-exactly-between-2-vectors.html
                        Vector3D vBoreEnd = (vAsteroidBoreEnd - vAsteroidBoreStart);
//                        vBoreEnd.Normalize();
                        Echo("BoreEnd=" + Vector3DToString(vBoreEnd));
                        Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)shipOrientationBlock).CenterOfMass);
                        //Vector3D vAimEnd = shipOrientationBlock.WorldMatrix.Forward;
//                        vAimEnd.Normalize();
                        Echo("AimEnd=" + Vector3DToString(vAimEnd));
//                        Vector3D vCrossEnd = (vBoreEnd - vAimEnd * 2);
                        Vector3D vRejectEnd = VectorRejection(vBoreEnd , vAimEnd);
                        //                        vCrossEnd.Normalize();

                        Echo("CrossEnd=" + Vector3DToString(vRejectEnd));
                        //                        Vector3D vCrossEnd = (vBoreEnd + vAimEnd) / 2;
                        debugGPSOutput("CrossStartEnd", vAsteroidBoreStart + vRejectEnd);
                        //                        debugGPSOutput("CrossEndStart", vAsteroidBoreEnd - vCrossStart);
                        //                        debugGPSOutput("COM", ((IMyShipController)shipOrientationBlock).CenterOfMass);
                        //                        Echo("CrossStart=" + Vector3DToString(vCrossStart));


                        //                        Echo("FW=" + Vector3DToString(shipOrientationBlock.WorldMatrix.Forward));
                        //                          doCameraScan(cameraForwardList, vAsteroidBoreEnd + vCrossEnd);
                        //                        doCameraScan(cameraBackwardList, vAsteroidBoreStart + vCrossStart);
                        Vector3D vCorrectedAim = vAsteroidBoreEnd - vRejectEnd;
                        doCameraScan(cameraForwardList, vCorrectedAim);
//                        doCameraScan(cameraBackwardList, vAsteroidBoreStart - vCrossEnd);
                        bWantMedium = true;
                    }
                    break;
                case 145:
                    { // bore scan
                        StatusLog("Bore Check Scan", textPanelReport);
                        Echo("Bore Check Scan");
                         
                        // we should have asteroid in front.
                        bool bAimed = true;
                        bool bAsteroidInFront = false;
                        bool bFoundCloseAsteroid = false;

                        //                        Echo(bValidExit.ToString() + " " + Vector3DToString(vExpectedAsteroidExit));
                        Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                        bAimed = GyroMain("forward", vAim, shipOrientationBlock);

                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS)
                        {
                            bWantMedium = true;
                            return;
                        }

                        if (bAimed)
                        {
                            bool bLarge = false;
                            bool bSmall = false;
                            SensorIsActive(sb1, ref bAsteroidInFront, ref bLarge, ref bSmall);
                            SensorIsActive(sb2, ref bFoundCloseAsteroid, ref bLarge, ref bSmall);

                            /* TEST: Always do the scans
                            if (bFoundCloseAsteroid || bAsteroidInFront)
                            {
                                // sensor is active.. go forth and mine
                                current_state = 150;
                                bWantFast = true;
                            }
                            else 
                            */
                            {
                                bWantMedium = true;
                                // try a raycast to see if we can hit anything
                                if (cameraForwardList.Count < 1)
                                {
                                    // no cameras to scan with.. assume
                                    current_state = 150;
                                    bWantFast = true;
                                    return;
                                }
                                //                                Echo("BoreHoleScanMode=" + BoreHoleScanMode);

                                double scanDistance = (shipOrientationBlock.GetPosition() - vAsteroidBoreEnd).Length();
                                bool bDidScan = false;
                                Vector3D vTarget;
                                if (BoreHoleScanMode < 0) BoreHoleScanMode = 0;
                                if (BoreHoleScanMode > 21)
                                {
                                    // we have scanned all of the areas and not hit anyhing..  skip this borehole
                                    AsteroidDoNextBore();
                                    return;
                                    BoreHoleScanMode = 0;
                                }
                                Vector3D vScanVector = (vAsteroidBoreEnd - vAsteroidBoreStart);
                                vScanVector.Normalize();
                                for (int i1 = 0; i1 < 4; i1++)
                                {
                                    debugGPSOutput("Points" + i1, BoreScanFrontPoints[i1]);
                                }
                                switch (BoreHoleScanMode)
                                {
                                    case 0:
                                        if (doCameraScan(cameraForwardList, vAsteroidBoreEnd))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 1:
                                        vTarget = BoreScanFrontPoints[2] + vScanVector * scanDistance;
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 2:
                                        vTarget = BoreScanFrontPoints[3] + vScanVector * scanDistance;
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 3:
                                        vTarget = BoreScanFrontPoints[0] + vScanVector * scanDistance;
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 4:
                                        vTarget = BoreScanFrontPoints[1] + vScanVector * scanDistance;
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 5:
                                        /*
                                        // check center again.  always full length
                                        if (doCameraScan(cameraForwardList, scanDistance))
                                        {
                                            bDidScan = true;
                                        }
                                        */
                                        bDidScan = true;
                                        break;
                                    case 6:
                                        vTarget = BoreScanFrontPoints[2] + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 7:
                                        vTarget = BoreScanFrontPoints[3] + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 8:
                                        vTarget = BoreScanFrontPoints[0] + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 9:
                                        vTarget = BoreScanFrontPoints[1] + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    // front output order is 0=BL, 1=BR, 2=TL, 3=TR
                                    case 10:
                                        // bottom middle
                                        vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 11:
                                        // top middle
                                        vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 12:
                                        // right middle
                                        vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 13:
                                        // left middle
                                        vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.55);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 14:
                                        vTarget = BoreScanFrontPoints[2] + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 15:
                                        vTarget = BoreScanFrontPoints[3] + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 16:
                                        vTarget = BoreScanFrontPoints[0] + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 17:
                                        vTarget = BoreScanFrontPoints[1] + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    // front output order is 0=BL, 1=BR, 2=TL, 3=TR
                                    case 18:
                                        // bottom middle
                                        vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 19:
                                        // top middle
                                        vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 20:
                                        // right middle
                                        vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                    case 21:
                                        // left middle
                                        vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.35);
                                        if (doCameraScan(cameraForwardList, vTarget))
                                        {
                                            bDidScan = true;
                                        }
                                        break;
                                }
                                if (bDidScan)
                                {
                                    // the camera scan routine sets lastDetetedInfo itself if scan succeeds
                                    if (!lastDetectedInfo.IsEmpty())
                                    {
                                        if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                        {
//                                            sStartupError += "BoreScan hit on " + BoreHoleScanMode + "\n";
                                            // we found an asteroid. (hopefully it's ours..)
                                            bFoundCloseAsteroid = true;
                                        }
                                        else if (lastDetectedInfo.Type == MyDetectedEntityType.FloatingObject)
                                        {
                                            //                                           Echo("Found NON-Asteroid in SCAN");
                                            // don't count raycast if we hit debris.
                                            bDidScan = false;
                                        }
                                        else
                                        { 
                                            // something else.. maybe ourselves or another ship?

                                        }

                                        if (bFoundCloseAsteroid)
                                        {
                                            // we found an asteroid (fragment).  go get it.
                                            current_state = 150;
                                            bWantFast = true;
                                        }
                                    }
                                    else
                                    {
                                    }
                                    if(bDidScan) BoreHoleScanMode++;
                                }
                                else
                                {
                                    Echo("Awaiting Available raycast");
                                }

                            }
                        }
                        else
                        {
                            bWantFast = true;
                            Echo("Aiming");
                        }
                    }
                    break;
                case 150:
                    { // approach
                        StatusLog("Asteroid Approach", textPanelReport);
                        Echo("Asteroid Approach");

                        bWantSlow = true;
                        // we should have asteroid in front.
                        bool bAimed = true;
                        bool bAsteroidInFront = false;
                        bool bFoundCloseAsteroid = false;

                        bAimed = BeamRider(vAsteroidBoreStart, vAsteroidBoreEnd, shipOrientationBlock);
                        /*
                        // 'BeamRider' routine that takes start,end and tries to stay on that beam.
                        Vector3D vBoreEnd = (vAsteroidBoreEnd - vAsteroidBoreStart);
                        Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)shipOrientationBlock).CenterOfMass);
                        Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);

                        Vector3D vCorrectedAim = (vAsteroidBoreEnd - vRejectEnd * 2) - ((IMyShipController)shipOrientationBlock).CenterOfMass;

                        bAimed = GyroMain("forward", vCorrectedAim, shipOrientationBlock);
                        */
                        /*                        Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                                                bAimed = GyroMain("forward", vAim, shipOrientationBlock);
                        */
                        double distance = (vAsteroidBoreStart - shipOrientationBlock.GetPosition()).Length();
                        Echo("Distance=" + niceDoubleMeters(distance) + " (" + niceDoubleMeters(AsteroidDiameter + shipDim.LengthInMeters() * MineShipLengthScale * 2) + ")");
                        StatusLog("Distance=" + niceDoubleMeters(distance) + " (" + niceDoubleMeters(AsteroidDiameter + shipDim.LengthInMeters() * MineShipLengthScale * 2) + ")", textPanelReport);
                        double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS)
                        {
                            bWantMedium = true;
                            return;
                        }

                        if (bAimed)
                        {
                            bool bLarge = false;
                            bool bSmall = false;
//                            SensorIsActive(sb1, ref bAsteroidInFront, ref bLarge, ref bSmall);
                            SensorIsActive(sb2, ref bFoundCloseAsteroid, ref bLarge, ref bSmall);
                            //
                            bWantFast = false;
                            bWantMedium = true;

                            // we already verified that there is asteroid in this bore.. go get it.
                            bAsteroidInFront = true;

                            if (bFoundCloseAsteroid)
                            {
                                powerDownThrusters(thrustAllList);
                                current_state = 31;
                            }
                            else if (bAsteroidInFront)
                            {
                                if ((distance+stoppingDistance) > (AsteroidDiameter + shipDim.LengthInMeters() * MineShipLengthScale * 2))
                                {
                                    // we have gone too far.  nothing to mine
                                    //                                    sStartupError += "\nTOO FAR! ("+AsteroidCurrentX+"/"+AsteroidCurrentY+")";

                                    current_state = 155;
                                    /*
                                       ResetMotion();
                                        if(velocityShip<1)
                                        AsteroidDoNextBore();
                                        */
                                    }
                                    else
                                    {
                                        MoveForwardSlow(fAsteroidApproachMps, fAsteroidApproachAbortMps, thrustForwardList, thrustBackwardList);
                                    }
                                }
                            }
                            else
                            {
                                bWantFast = true;
                                Echo("Aiming");
                                if ((distance + stoppingDistance) > (AsteroidDiameter + shipDim.LengthInMeters() * MineShipLengthScale * 2))
                                {
                                    // we have gone too far.  nothing to mine
                                    //                                    sStartupError += "\nTOO FAR! ("+AsteroidCurrentX+"/"+AsteroidCurrentY+")";
                                    current_state = 155;
                                    /*
                                    ResetMotion();
                                    if (velocityShip < 1)
                                        AsteroidDoNextBore();
                                        */
                                }
                            }
                    }
                    break;
                case 155:
                    {
                        ResetMotion();
//                        if (velocityShip < 1)
                            AsteroidDoNextBore();
                    }
                    break;
                case 190:
                    {
                        // start NAV travel
                        NavGoTarget(vAsteroidBoreStart, iMode, 195, 11, "MINE-Bore start");
                    }
                    break;
                case 195:
                    {// we have 'arrived' from far travel
                        // wait for motion to slow
                        bWantMedium = true;
                        if (velocityShip < fAsteroidApproachMps)
                        {
                            current_state = 120;
                            bWantFast = true;
                        }
                        ResetMotion();
                        break;
                    }
                case 300:
                    {
                        // we have exitted the asteroid.  Prepare for another run or to go dock
                        Echo("Exitted!");
                        ResetMotion();
                        SensorsSleepAll();
                        turnEjectorsOn();
                        bWantMedium = true;
                        current_state = 305;
                        break;
                    }
                case 305:
                    {
                        bWantMedium = true;
                        if (velocityShip > 1) return;

                        if (AsteroidMineMode == 1)
                        {
                            // we did a single bore.
                            // so now we are done.
                            AsteroidMineMode = 0;
                            bAutoRelaunch = false;
                            setMode(MODE_DOCKING);
                            miningAsteroidID = -1;
                            break;
                        }
                        // else mine mode !=1
                        bool bReady = DockAirWorthy(true, false, MiningCargopcthighwater);
                        if (maxDeltaV < fMiningMinThrust || !bReady) //cargopcent > MiningCargopctlowwater || batteryPercentage < batterypctlow)
                        {
                            bool bBoresRemaining = AsteroidCalculateNextBore();
                            if (bBoresRemaining)
                            {
                                bAutoRelaunch = true;
                                miningChecksElapsedMs = -1;
                                setMode(MODE_DOCKING);
                            }
                            else
                            {
                                current_state = 500;
                            }
                        }
                        else
                        {
                            // UNLESS: we have another target asteroid..
                            // TODO: 'recall'.. but code probably doesn't go here.
                            AsteroidDoNextBore();
                        }
                    }
                    break;
                    case 310:
                    {
                        AsteroidCalculateBestStartEnd();
//                        vAsteroidBoreStart = AsteroidCalculateBoreStart();
                        NavGoTarget(vAsteroidBoreStart, iMode, 120,11,"MINE-Next Bore start");
                        break;

                    }
                case 500:
                    {
                        // TODO: do a final search pass for any missed voxels.
                        // TODO: remove asteroid after final pass

                        // Go home.
                        bAutoRelaunch = false;
                        setMode(MODE_DOCKING);
                        miningAsteroidID = -1;
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

        void doModeMineSingleBore()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":MINE Line", textPanelReport);
            Echo("MINE:current_state=" + current_state.ToString());
            double maxThrust = calculateMaxThrust(thrustForwardList);
            IMySensorBlock sb1 = null;
            IMySensorBlock sb2 = null;
            //            Echo("maxThrust=" + maxThrust.ToString("N0"));

            MyShipMass myMass;
            myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
            double effectiveMass = myMass.PhysicalMass;
            //            Echo("effectiveMass=" + effectiveMass.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;
            Echo("Our Asteroid=" + miningAsteroidID.ToString());
            if (sensorsList.Count >= 2)
            {
                sb1 = sensorsList[0];
                sb2 = sensorsList[1];
            }
            switch (current_state)
            {
                case 0:
                    bValidExit = false;
                    bMiningWaitingCargo = false;

                    ResetMotion();
                    turnEjectorsOff();
                    OreDoCargoCheck(true); // init ores to what's currently in inventory
                    MinerCalculateBoreSize();
                    MoveForwardSlowReset();

                    bWantFast = true;

                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                        setMode(MODE_ATTENTION);
                        return;
                    }
                    sb1.DetectAsteroids = true;
                    sb2.DetectAsteroids = true;

                    // Can we turn in our own tunnel?
                    if (shipDim.LengthInMeters() > shipDim.WidthInMeters() && shipDim.LengthInMeters() > shipDim.HeightInMeters())
                        bBoringOnly = true;
                    else bBoringOnly = false;

                    miningAsteroidID = 0;
                    current_state = 1;
                    iMode = MODE_MINE;
                    AsteroidMineMode = 1;// drill exactly where we're aimed for.
                    bWantFast = true;
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

//            Echo("effectiveMass=" + effectiveMass.ToString("N0"));
//            Echo("maxThrust=" + maxThrust.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;

            Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
            StatusLog("Max DeltaV=" + maxDeltaV.ToString("N1") + " / " + fMiningMinThrust.ToString("N1") + "min", textPanelReport);
            if (current_state > 0)
            {
                if (miningChecksElapsedMs >= 0) miningChecksElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (miningChecksElapsedMs < 0 || miningChecksElapsedMs > 1)
                {
                    miningChecksElapsedMs = 0;
                    DockAirWorthy(false, false); // does the current value checks.
                    /*
                    OreDoCargoCheck();
                    batteryCheck(0);
                    TanksCalculate();
                    // TODO: check reactor uranium
                    */
                    StatusLog("Cargo =" + cargopcent + "% / " + MiningCargopcthighwater + "% Max", textPanelReport);
                    StatusLog("Battery " + batteryPercentage + "% (Min:" + batterypctlow + "%)", textPanelReport);
                    if(TanksHasHydro()) StatusLog("H2 " + hydroPercent + "% (Min:" + batterypctlow + "%)", textPanelReport);
                }
            }

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
                    else bBoringOnly = true;
                    //                    else bBoringOnly = false;
                    bWantMedium = true;
                    current_state = 10;
                    ResetMotion();
                    //                    turnDrillsOff();
                    turnEjectorsOn();
                    if (sensorsList.Count < 2)
                    {
                        StatusLog(OurName + ":" + moduleName + " Exit Asteroid: Not Enough Sensors!", textLongStatus, true);
                        sStartupError += "Not enough sensors found!";
                        miningChecksElapsedMs = -1;
                        setMode(MODE_ATTENTION);
                    }
                    break;
                case 10://10 - Init sensors, 
                    bWantMedium = true;
                    SensorsSleepAll();
                    if (bBoringOnly)
                    {
                        SensorSetToShip(sensorsList[0], (float)shipDim.WidthInMeters(), (float)shipDim.WidthInMeters(),
                            (float)shipDim.HeightInMeters(), (float)shipDim.HeightInMeters(),
                            (float)shipDim.LengthInMeters(), 1);
                    }
                    else
                    {
                        SensorSetToShip(sensorsList[0], (float)shipDim.WidthInMeters(), (float)shipDim.WidthInMeters(),
                            (float)shipDim.HeightInMeters(), (float)shipDim.HeightInMeters(),
                            1, (float)shipDim.LengthInMeters());

                    }

                    //                    SensorSetToShip(sensorsList[0], 2, 2, 2, 2, 15, 15);
                    //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                    miningElapsedMs = 0;
                    current_state = 11;
                    break;
                case 11://11 - await sensor set
                    bWantMedium = true;
                    miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (miningElapsedMs < dSensorSettleWaitMS) return;
                    if (bBoringOnly)
                        current_state = 30;
                    else
                        current_state = 20;
                    break;
                case 20: //20 - turn around until aimed ->30
                    {
                        bWantFast = true;
                        turnDrillsOn();

                        // we want to turn on our horizontal axis as that should be the 'wide' one.
                        bool bAimed = false;
                        double yawangle = -999;
//                        Echo("vTarget=" + Vector3DToString(vLastAsteroidContact));
                        yawangle = CalculateYaw(vAsteroidBoreStart, shipOrientationBlock);
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
                            //MoveForwardSlow(fTargetMiningmps,fAbortmps, thrustForwardList, thrustBackwardList);
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
                        Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                        bAimed = GyroMain(sOrientation, vAim, shipOrientationBlock);
                        if (bAimed)
                        {
                            bWantMedium = true;
                            if (bBoringOnly)
                            {
                                MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustBackwardList, thrustForwardList);
                            }
                            else
                            {
                                MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustForwardList, thrustBackwardList);
                            }
                        }
                        else bWantFast = true;

                        bool bLocalAsteroid = false;
                        aSensors = SensorsGetActive();
                        for (int i = 0; i < aSensors.Count; i++)
                        {
                            IMySensorBlock s = aSensors[i] as IMySensorBlock;
//                            StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
//                            Echo(aSensors[i].CustomName + " ACTIVE!");

                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            s.DetectedEntities(lmyDEI);
                            if (AsteroidProcessLDEI(lmyDEI))
                                bLocalAsteroid = true;
                        }
                        if (!bLocalAsteroid)
                        {
                            ResetMotion();
                            SensorsSleepAll();
                            current_state = 40;
                        }
                        break;
                    }
                case 40://40 when out, call for pickup
                    {
                        turnDrillsOff();

                        current_state = 50;
                        bWantFast = true;
                        /*

                        // todo: if on near side, just go docking.f
                        int iSign = Math.Sign(AsteroidCurrentY);
                        if (iSign == 0) iSign = 1;
                        Vector3D vTop = AsteroidPosition + AsteroidUpVector * AsteroidDiameter * iSign * 1.25;
                        // TODO: if on 'near' side', just go dock... but we don't know where dock is...

//                        double distanceSQ = (vStart - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();

                        NavGoTarget(vTop, iMode, 50, 10);
                        */
                    }
                    break;
                case 50:
                    {
                        // we should probably give hint to docking as to WHY we want to dock..
                        bAutoRelaunch = true;
                        miningChecksElapsedMs = -1;
                        setMode(MODE_DOCKING);
                        break;
                    }
                default:
                    {
                        Echo("UNKNOWN STATE!");
                        break;
                    }
            }
        }

    }

}

