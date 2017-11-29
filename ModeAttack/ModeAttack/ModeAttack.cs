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
        #region GATLINGATTACK
        /*
         * 0 Masterinit
         * 10 check for target. If none. try to find one.
         * 11 process found target
         * 50 received attack target command.
         * 51 aim towards target and scan when ready
         * 
         * Attack Plans: 
         * 1 = danger close circle strafe
         * 20 = square Dance
         * 30 = tailpipe
         * 40 = long side fast strafe
         * 100 = track target
         * 
         * Attack Plans: Danger Close Circle Strafe
         * 100 found target. >1000m away
         * 101 found target. <=1000m away
         * 
         * Attack Plan: Square Dance
         * 200 found target
         * 
         * Attack Plan: tailpipe
         * 300 found target
         * 
         * Attack Plan: Long fast strafe
         * 400 found target
         * 
         * Attack Plan: Track Target
         * 1000 Found Target
         */

        bool bHitLastTarget = false;

        // squaredance settings
        long sqStandoffDistance = 850;

        long targetPaintStandoffDistance = 2000;

        long lSqDanceDist = 802; // distance to do square dance from..
        long sqDanceCount = 0;
        long ticksPerDirection = 64; // how many ticks to use a certain direction of movement.
        double dAimOffset = 50; // 0-100.  Default center
        double dAimDelta = 1;

        long panCorner1 = 0; // 
        long panCorner2 = 4;

        bool bBoxVerbose = false;
        bool bScanTargetVerbose = false;

        long defaultScanMax = 10000;

        MyDetectedEntityInfo targetDetectedInfo = new MyDetectedEntityInfo();
        MyDetectedEntityInfo initialTargetDetectedInfo = new MyDetectedEntityInfo();

        StringBuilder strbAttack = new StringBuilder();

        void doModeAttack()
        {

            minAngleRad = 0.01f;

            //	List<IMySensorBlock> aSensors = null;
            //	IMySensorBlock sb;

            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":Attack!", textPanelReport);
            Echo("current_state=" + current_state.ToString());
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double effectiveMass =  myMass.PhysicalMass;;
            double maxThrust = calculateMaxThrust(thrustForwardList);

            //	Echo("effectiveMass=" + effectiveMass.ToString("N0"));
            //	Echo("maxThrust=" + maxThrust.ToString("N0"));

            double maxDeltaV = (maxThrust) / effectiveMass;

            //	Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
            //	Echo("Cargo="+cargopcent.ToString()+"%");

            Echo("velocity=" + velocityShip.ToString("0.00"));
            //	Echo("#Sensors=" + sensorsList.Count);
            //	Echo("width=" + shipDim.WidthInMeters().ToString("0.0"));
            //	Echo("height=" + shipDim.HeightInMeters().ToString("0.0"));
            //	Echo("length=" + shipDim.LengthInMeters().ToString("0.0"));

            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
            StatusLog("clear", txtPanel);

            Vector3D currentpos = gpsCenter.GetPosition();
            CTRL_COEFF = 0.99;
            bWantFast = true;

            switch (current_state)
            {
                case 0:
                    ResetMotion();
                    current_state = 10;
                    break;
                case 10:
                    if (doCameraScan(cameraForwardList, defaultScanMax))
                    {
                        if (lastDetectedInfo.IsEmpty())
                        {
                            ResetMotion();
                            setMode(MODE_INSPACE); // no target found..
                                                   //					setMode(MODE_ATTENTION); // no target found..
                        }
                        else
                        { // we found something.
                            if (IsValidAttackTarget(lastDetectedInfo))
                            {
                                bHitLastTarget = true;
                                targetDetectedInfo = lastDetectedInfo;
                                initialTargetDetectedInfo = lastDetectedInfo;
                                current_state = 11;
                            }
                            else Echo("Not Valid Attack Target");

                        }
                    }
                    break;
                case 11: // process found target
                    {
                        // should dynamically select attack plan based on target
                        if (iAttackPlan == 0) iAttackPlan = 20;
                        //				if (iAttackPlan == 0) iAttackPlan = 20;

                        double distancesq;
                        if (targetDetectedInfo.HitPosition != null)
                            distancesq = Vector3D.DistanceSquared(currentpos, (Vector3D)targetDetectedInfo.HitPosition);
                        else
                            distancesq = Vector3D.DistanceSquared(currentpos, (Vector3D)targetDetectedInfo.Position);

                        if (iAttackPlan == 1)
                        {
                            if (distancesq > 1000000)
                            {
                                current_state = 100;
                            }
                            else
                            {
                                current_state = 101;
                            }
                        }
                        else if (iAttackPlan == 20) current_state = 200;
                        else if (iAttackPlan == 30) current_state = 300;
                        else if (iAttackPlan == 40) current_state = 400;
                        else if (iAttackPlan == 100) current_state = 1000;
                        else
                        {
                            ResetMotion();
                            setMode(MODE_ATTENTION); // unknown attack plan
                        }

                    }
                    break;
                case 50: // process attack command on position
                    {
                        Echo("Attack Position!");
                        ResetMotion();
                        Vector3D vVec;

                        Vector3D targetPos = vTargetMine;

                        vVec = vTargetMine - currentpos;
                        double distance = vVec.Length();
                        Echo("Distance=" + distance);

                        bool bAimed = false;
                        bAimed = GyroMain("forward", vVec, gpsCenter);
                        if (bAimed)
                        {
                            Echo("Aimed");
                            if (doCameraScan(cameraForwardList, vTargetMine))
                            {
                                if (lastDetectedInfo.IsEmpty())
                                { // it moved or is already dead.
                                    setMode(MODE_ATTENTION);
                                }
                                else
                                { // we found something.
                                    if (IsValidAttackTarget(lastDetectedInfo))
                                    {
                                        bHitLastTarget = true;
                                        targetDetectedInfo = lastDetectedInfo;
                                        initialTargetDetectedInfo = lastDetectedInfo;
                                        current_state = 11;
                                    }
                                    else
                                    {
                                        Echo("Not Valid Attack Target");
                                        setMode(MODE_INSPACE);
                                    }

                                }
                            }

                        }
                    }
                    break;
                case 100: // Danger Close Circle Strafe
                    {
                        ResetMotion();
                        Vector3D vVec;

                        Vector3D targetPos = (Vector3D)targetDetectedInfo.HitPosition;

                        vVec = targetDetectedInfo.Position - currentpos;
                        // vVec = targetPos - currentpos;
                        double distance = vVec.Length();
                        Echo("Distance=" + distance);

                        bool bAimed = false;
                        bAimed = GyroMain("forward", vVec, gpsCenter);
                        if (bAimed) powerUpThrusters(thrustForwardList);

                        ((IMyShipController)gpsCenter).DampenersOverride = true;

                        if (doCameraScan(cameraForwardList, distance + 200))
                        {
                            if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                            { // hit nothing???
                            }
                            else
                            { // we found something.
                                targetDetectedInfo = lastDetectedInfo;
                                bHitLastTarget = true;
                                if (distance < 1000)
                                    current_state = 101;
                            }
                        }
                    }
                    break;
                case 101:
                    {
                        // within 1000 range
                        //				Vector3D targetPos = (Vector3D)targetDetectedInfo.HitPosition;
                        //				Vector3D vVec = targetPos - currentpos;

                        Vector3D vOffsetVec, vOffsetTarget, vVec;
                        if (bHitLastTarget)
                        {
                            Echo("Hit last");
                            vOffsetVec = targetDetectedInfo.Position - (Vector3D)targetDetectedInfo.HitPosition;
                            vOffsetTarget = (Vector3D)targetDetectedInfo.HitPosition + vOffsetVec * 2;
                            vVec = vOffsetTarget - currentpos;

                        }
                        else
                        {
                            Echo("MISSED Last scan");
                            // TODO: Add in craft's motion vector to target..

                            //					vOffsetVec = targetDetectedInfo.Position - (Vector3D)targetDetectedInfo.HitPosition;
                            //					vOffsetTarget = (Vector3D)targetDetectedInfo.HitPosition + vOffsetVec * 2;
                            vVec = targetDetectedInfo.Position - currentpos;
                        }

                        double distance = vVec.Length();
                        Echo("Distance=" + distance);

                        ResetMotion();

                        bool bAimed = false;
                        bAimed = GyroMain("forward", vVec, gpsCenter);

                        if (distance < 700)
                        {
                            Echo("In of range");
                            // start circle strafe
                            ((IMyShipController)gpsCenter).DampenersOverride = false;
                            if (bAimed || distance < 300 || velocityShip < 25) powerUpThrusters(thrustRightList, 45); // should be based on bounding box size of target.
                            if (distance > 200) powerUpThrusters(thrustForwardList, 25);
                        }
                        else
                        { // moving out of range.  move closer
                            Echo("out of range");
                            ((IMyShipController)gpsCenter).DampenersOverride = true;
                            powerUpThrusters(thrustForwardList);
                        }
                        if (doCameraScan(cameraForwardList, distance + 100))
                        {
                            if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                            { // hit nothing???
                                bHitLastTarget = false;
                            }
                            else
                            { // we found something.
                                targetDetectedInfo = lastDetectedInfo;
                                bHitLastTarget = true;
                                double distancesq = Vector3D.DistanceSquared(currentpos, (Vector3D)targetDetectedInfo.HitPosition);
                                if (distancesq > 10000000) // sq=3162m
                                {
                                    ResetMotion();
                                    setMode(MODE_IDLE);// current_state = 100;
                                }
                                else
                                {
                                    if (distance < 700)
                                    {
                                        // SHOOT!
                                        if (bWeaponsHot) blockApplyAction(gatlingsList, "ShootOnce");
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 200: // Square Dance
                    {
                        StatusLog("clear", gpsPanel);
                        ResetMotion();
                        Vector3D vVec;

                        Vector3D targetPos = (Vector3D)targetDetectedInfo.HitPosition;
                        //				Vector3d targetVec = targetPos - currentpos;

                        //				vVec = targetDetectedInfo.Position - currentpos;
                        Vector3D vExpectedTargetPos = targetPos; // need to add in velocity
                        vVec = vExpectedTargetPos - currentpos;
                        double distance = vVec.Length();
                        Echo("Distance=" + distance);
                        strbAttack.Clear();
                        strbAttack.Append("Name: " + targetDetectedInfo.Name);
                        strbAttack.AppendLine(); strbAttack.Append("Type: " + targetDetectedInfo.Type);
                        strbAttack.AppendLine(); strbAttack.Append("RelationShip: " + targetDetectedInfo.Relationship);
                        strbAttack.AppendLine(); strbAttack.Append("Size: " + targetDetectedInfo.BoundingBox.Size);
                        strbAttack.AppendLine(); strbAttack.Append("Velocity: " + targetDetectedInfo.Velocity);
                        strbAttack.AppendLine(); strbAttack.Append("Orientation: " + targetDetectedInfo.Orientation);
                        if (bScanTargetVerbose) Echo(strbAttack.ToString());

                        bool bAimed = false;
                        //	holdStandoff(distance, sqStandoffDistance);
                        double stoppingM = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);

                        //				if (distance > lSqDanceDist * 2 )//|| distance >750)
                        if ((distance - stoppingM) > (sqStandoffDistance * 1.5))//|| distance >750)
                        {
                            Echo("Long Distance:  Closing");
                            bAimed = GyroMain("forward", vVec, gpsCenter);
                            if (bAimed)
                            {
                                if (distance > sqStandoffDistance * 3)
                                {
                                    if (velocityShip < 90)
                                        powerUpThrusters(thrustForwardList, 45);
                                    else
                                        powerUpThrusters(thrustForwardList, 1);
                                }
                                else
                                {
                                    if (velocityShip < 20) // seems like a low approach speed...
                                        powerUpThrusters(thrustForwardList, 25);
                                    else if (velocityShip < 35)
                                        powerUpThrusters(thrustForwardList, 1);
                                    // else already did ResetMotion()
                                }
                            }
                            // else already did resetmotion
                        }
                        else
                        {
                            Echo("closer: Aiming");
                            // we want to sweep aim from front to back..
                            Vector3D[] avCorners = targetDetectedInfo.BoundingBox.GetCorners();

                            Vector3D vMin;
                            Vector3D vMax;
                            //					vMin=targetDetectedInfo.BoundingBox.Min;
                            //					vMax=targetDetectedInfo.BoundingBox.Max;
                            if (bBoxVerbose) Echo("panCorner1:" + panCorner1);
                            if (bBoxVerbose) Echo("panCorner2:" + panCorner2);
                            vMin = avCorners[panCorner1];
                            vMax = avCorners[panCorner2];
                            //					if ((panCorner2 - 4) == panCorner1)						vMax = targetDetectedInfo.Position; // go to center.

                            Vector3D vVBox = vMax - vMin;
                            debugGPSOutput("MinBound", vMin);
                            debugGPSOutput("MaxBound", vMax);

                            double boundingDist = vVBox.Length();
                            vVBox.Normalize();

                            targetPos = vMin + vVBox * ((dAimOffset * boundingDist) / 100);
                            debugGPSOutput("targetposPan", targetPos);

                            vVec = targetPos - currentpos;
                            bAimed = GyroMain("forward", vVec, gpsCenter);
                            if (bBoxVerbose) Echo("dAimOffset:" + dAimOffset);
                            // pan back and forth..
                            if (bAimed)
                            {
                                Echo("AIMED");
                                dAimOffset += dAimDelta;
                            }
                            else Echo("Aiming");

                            if (dAimOffset < 0)
                            {
                                dAimOffset = 0;
                                dAimDelta = -dAimDelta;
                                panCorner1++;
                                if (panCorner1 > 3)
                                {
                                    panCorner1 = 0;
                                    panCorner2++;
                                }
                                if (panCorner2 > 7)
                                {
                                    panCorner2 = 4;
                                }

                            }
                            else if (dAimOffset > 100)
                            {
                                dAimOffset = 100;
                                dAimDelta = -dAimDelta;
                            }
                        }

                        if (distance < lSqDanceDist)
                        {
                            Echo("sqDanceCount:" + sqDanceCount);
                            if (sqDanceCount < ticksPerDirection) powerUpThrusters(thrustDownList);
                            else if (sqDanceCount < ticksPerDirection * 2) powerUpThrusters(thrustLeftList);
                            else if (sqDanceCount < ticksPerDirection * 3) powerUpThrusters(thrustUpList);
                            else powerUpThrusters(thrustRightList);

                            sqDanceCount++;
                            if (sqDanceCount > ticksPerDirection * 4) sqDanceCount = 0;
                        }
                        if (doCameraScan(cameraForwardList, distance + 100))
                        {
                            strbAttack.Clear();
                            if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                            { // hit nothing???
                                dAimOffset += dAimDelta; // move faster..
                            }
                            else
                            { // we found something.
                                strbAttack.Append("Name: " + lastDetectedInfo.Name);
                                strbAttack.AppendLine(); strbAttack.Append("Type: " + lastDetectedInfo.Type);
                                strbAttack.AppendLine(); strbAttack.Append("Relationship: " + lastDetectedInfo.Relationship);
                                //		strbAttack.AppendLine();		strbAttack.Append("Orientation: " + lastDetectedInfo.Orientation);
                                if (bScanTargetVerbose) Echo(strbAttack.ToString());
                                double minsize = 3;
                                if (lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid) minsize = 0.75;

                                if (lastDetectedInfo.BoundingBox.Size.X > minsize && IsValidAttackTarget(lastDetectedInfo))
                                {
                                    targetDetectedInfo = lastDetectedInfo;
                                    if (distance < 975)
                                    {
                                        // SHOOT!
                                        if (bWeaponsHot) blockApplyAction(gatlingsList, "ShootOnce");
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 300: // tailpipe (init)
                    {
                        StatusLog("clear", gpsPanel);
                        ResetMotion();
                        Vector3D vVec;

                        Vector3D targetPos = (Vector3D)targetDetectedInfo.HitPosition;
                        vVec = targetPos - currentpos;
                        double distance = vVec.Length();
                        Echo("Distance=" + distance);
                        strbAttack.Clear();
                        strbAttack.Append("Name: " + targetDetectedInfo.Name);
                        strbAttack.AppendLine(); strbAttack.Append("Type: " + targetDetectedInfo.Type);
                        strbAttack.AppendLine(); strbAttack.Append("RelationShip: " + targetDetectedInfo.Relationship);
                        //		strbAttack.AppendLine();		strbAttack.Append("Size: " + targetDetectedInfo.BoundingBox.Size);
                        if (bScanTargetVerbose)
                            Echo(strbAttack.ToString());
 //                       bool bAimed = false;
                        double stoppingM = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                    }
                    break;
                case 400: // Long Fast strafe
                    {
                    }
                    break;
                case 1000: // Track Target
                    {
                        //			bScanTargetVerbose = true;

                        StatusLog("clear", gpsPanel);
                        ResetMotion();
                        Vector3D vVec;

                        Vector3D targetPos = targetDetectedInfo.Position;
                        Vector3D vExpectedTargetPos = targetPos + targetDetectedInfo.Velocity;
                        debugGPSOutput("vExpectedTargetPos", vExpectedTargetPos);
                        Echo("TargetV=" + targetDetectedInfo.Velocity.Length());
                        vVec = vExpectedTargetPos - currentpos;
                        double distance = vVec.Length();
                        Echo("Distance=" + distance);
                        strbAttack.Clear();
                        strbAttack.Append("Name: " + targetDetectedInfo.Name);
                        strbAttack.AppendLine(); strbAttack.Append("Type: " + targetDetectedInfo.Type);
                        strbAttack.AppendLine(); strbAttack.Append("RelationShip: " + targetDetectedInfo.Relationship);
                        strbAttack.AppendLine(); strbAttack.Append("Size: " + targetDetectedInfo.BoundingBox.Size);
                        strbAttack.AppendLine(); strbAttack.Append("Velocity: " + targetDetectedInfo.Velocity);
                        strbAttack.AppendLine(); strbAttack.Append("Orientation: " + targetDetectedInfo.Orientation);
                        if (bScanTargetVerbose) Echo(strbAttack.ToString());

                        //				minAngleRad =0.005f;
                        bool bAimed = false;
                        bAimed = GyroMain("forward", vVec, gpsCenter);
                        if (bAimed)
                        {
                            minAngleRad = 0.005f;
                            GyroMain("forward", vVec, gpsCenter);

                        }

                        if (bAimed || distance < (targetPaintStandoffDistance * .9)) holdStandoff(distance, targetPaintStandoffDistance);

                        if (doCameraScan(cameraForwardList, distance + 100))
                        {
                            Echo("1");
                            if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                            {
                                Echo("2");
                                doCameraScan(cameraForwardList, vExpectedTargetPos);
                                if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                                {
                                    Echo("3");
                                    vExpectedTargetPos = targetPos;
                                    doCameraScan(cameraForwardList, vExpectedTargetPos);

                                    if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                                    {
                                        Echo("4");
                                        vExpectedTargetPos = targetPos + targetDetectedInfo.Velocity * 1.1f;
                                        doCameraScan(cameraForwardList, vExpectedTargetPos);
                                        /*								if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                                            {
                                Echo("5");
                                                vExpectedTargetPos = targetPos+targetDetectedInfo.Velocity*1.2f;
                                                doCameraScan(cameraForwardList, vExpectedTargetPos);
                                                if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                                                {
                                Echo("6");
                                                    vExpectedTargetPos = targetPos+targetDetectedInfo.Velocity*1.3f;
                                                    doCameraScan(cameraForwardList, vExpectedTargetPos);
                                                }
                                            }
                                        */
                                    }
                                }
                            }

                            if (lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(lastDetectedInfo))
                            { // hit nothing???
                              //						dAimOffset+=dAimDelta; // move faster..
                                Echo("MISSED TARGET OR NOT VALID TARGET");
                            }
                            else
                            { // we found something.
                                strbAttack.Clear();
                                strbAttack.Append("Name: " + lastDetectedInfo.Name);
                                strbAttack.AppendLine(); strbAttack.Append("Type: " + lastDetectedInfo.Type);
                                strbAttack.AppendLine(); strbAttack.Append("Relationship: " + lastDetectedInfo.Relationship);
                                strbAttack.AppendLine(); strbAttack.Append("Size: " + lastDetectedInfo.BoundingBox.Size);
                                strbAttack.AppendLine(); strbAttack.Append("Velocity: " + lastDetectedInfo.Velocity);
                                strbAttack.AppendLine(); strbAttack.Append("Orientation: " + lastDetectedInfo.Orientation);
                                if (bScanTargetVerbose) Echo(strbAttack.ToString());
                                double minsize = 3;
                                if (lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid) minsize = 0.75;

                                if (lastDetectedInfo.BoundingBox.Size.X > minsize && IsValidAttackTarget(lastDetectedInfo))
                                {
                                    targetDetectedInfo = lastDetectedInfo;
                                    if (distance < 975)
                                    {
                                        // SHOOT!
                                        //								if (bWeaponsHot) blockApplyAction(gatlingsList, "ShootOnce");
                                    }
                                }
                            }
                        }
                        //				else Echo("NO CAMERA AVAILABLE FOR SCAN");

                    }
                    break;
            }
        }
        #endregion

        void holdStandoff(double distance, long standoffDistance)
        {
            double stoppingM = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
            if ((distance - stoppingM) > (standoffDistance * 1.5))
            {
                Echo("Long Distance:  Closing");
                if (distance > standoffDistance * 3)
                {
                    if (velocityShip < 90)
                        powerUpThrusters(thrustForwardList, 45);
                    else
                        powerUpThrusters(thrustForwardList, 1);
                }
                else
                {
                    if (velocityShip < 20)
                        powerUpThrusters(thrustForwardList, 25);
                    else if (velocityShip < 35)
                        powerUpThrusters(thrustForwardList, 1);
                }
            }
            if (distance > standoffDistance * 1.05)
            {
                Echo("Forward");
                powerUpThrusters(thrustForwardList, 15);
            }
            else if (distance < standoffDistance * 0.7)
            {
                Echo("BACK!");
                powerUpThrusters(thrustBackwardList, 100);
            }
            else if (distance < standoffDistance * 0.99)
            {
                Echo("small back");
                powerUpThrusters(thrustBackwardList, 45);
            }
            else Echo("hold distance");


        }

        void findAttackTarget()
        {
            if (doCameraScan(cameraForwardList, 10000))
            {
                if (lastDetectedInfo.IsEmpty())
                { // hit nothing???
                    Echo("No Target found to attack");
                }
                else
                { // we found something.
                    if (IsValidAttackTarget(lastDetectedInfo))
                    {

                        Echo("setting attack target");

                        targetDetectedInfo = lastDetectedInfo;
                        broadcastAttackCommand();
                        setMode(MODE_ATTACK);
                        current_state = 11;
                    }
                    else Echo("Not Valid Attack Target");
                }
            }
        }

        bool IsValidAttackTarget(MyDetectedEntityInfo thisDetectInfo)
        {
            if (thisDetectInfo.IsEmpty()) return false;

            if (thisDetectInfo.Type == MyDetectedEntityType.Asteroid) return false;
            if (lastDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies) return true;
            if (lastDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Owner) if (bFriendlyFire) return true; else return false;
            if (lastDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.FactionShare) if (bFriendlyFire) return true; else return false;

            return true;

        }

        void broadcastAttackCommand()
        {
            if (!targetDetectedInfo.IsEmpty())
            {
                if (targetDetectedInfo.Velocity.Length() < 1)
                {
                    antSend("WICO:ATTACKP:" + Vector3DToString(targetDetectedInfo.Position));
                }
                else
                {
                    antSend("WICO:ATTACKM:" + deiInfo(targetDetectedInfo));

                }
            }
        }



    }
}