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
        // we have entered gravity well 
        // 0=initialize 
        // 10=dampeners on. aim towards target 
        // 11=aligned check 
        // 20=dampeners on. minor thrust fowards to align motion to target 
        // 21 hold alignment 
        // 22 hold alignment 
        // 23 hold alignment 
        // 30=dampeners off 
        // 40=free-falll. continue alignment. when in range for 180. start 180  
        // 60= check for 180 completed 
        // 61= perform align to gravity vector. ->70 if complete
        // 70=check for in retro-burn range of target in range; Dampeners on 
        // 90=wait for zero velocity 
        // 100 ... user control.. 
        // 200 orient top toward location 
        // 201 thrust toward location 
        // 202 'over' location 
        // 203 >1k 'over' location 
        // 204 >500 'over' location 
        // 205 >100 'over' location 
        // 206 >25 'over' location 
        // 207 final descent 


        //bool bOverTarget=false; 
        void doModeDescent()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(OurName + ":" + moduleName + ":Descent", textPanelReport);
            StatusLog("Gravity=" + dGravity.ToString(velocityFormat), textPanelReport);
            Echo("Gravity=" + dGravity.ToString(velocityFormat));
            double alt = 0;
            double halt = 0;

            Vector3D vTarget = new Vector3D(0, 0, 0);
            bool bValidTarget = false;

            MyShipMass myMass;
            myMass = ((IMyShipController)anchorPosition).CalculateShipMass();

            if (bValidOrbitalLaunch)
            {
                // source position was connector
                bValidTarget = true;
                vTarget = vOrbitalLaunch;
            }
            else if (bValidOrbitalHome)
            {
                // source position was hover
                bValidTarget = true;
                vTarget = vOrbitalHome;
            }
            else
            {
                bValidTarget = false;
                //	if(dGravity>0) 
                // assume we are pointed at planet... push forward until in gravity.


                //	StatusLog(OurName+":"+moduleName+":Cannot Descend: No Waypoint present.",textPanelReport); 
                //		setMode(MODE_IDLE); 
                //		return; 
            }

            if (bValidTarget)
            {

                alt = (shipOrientationBlock.GetPosition() - vTarget).Length();

                StatusLog("Distance: " + alt.ToString("N0") + " Meters", textPanelReport);

                if (dGravity > 0)
                {
                    if (shipOrientationBlock is IMyRemoteControl)
                    {
                        Vector3D vNG = ((IMyRemoteControl)shipOrientationBlock).GetNaturalGravity();

                        //double Pitch,Yaw; 
                        Vector3D groundPosition;
                        groundPosition = shipOrientationBlock.GetPosition();
                        vNG.Normalize();
                        groundPosition += vNG * alt;

                        halt = (groundPosition - vTarget).Length();
                        StatusLog("Hor distance: " + halt.ToString("N0") + " Meters", textPanelReport);

                    }

                }
            }
            else
            {
                // doing a blind landing
                Echo("Blind Landing");
                StatusLog("Blind Landing", textPanelReport);

                ((IMyRemoteControl)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out alt);
                halt = 0;
                minAltRotate = 39000;
            }
            if (dGravity > 0)
            {
                double elevation = 0;

                ((IMyRemoteControl)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
                Echo("Elevation: " + elevation.ToString("N0") + " Meters");
            }

            Echo("Descent Mode:" + current_state.ToString());

            if (anyGearReadyToLock())
            {
                gearsLock();
            }
            double progress = 0;
            if (velocityShip <= 0) progress = 0;
            else if (velocityShip > fMaxMps) progress = 100;
            else progress = ((velocityShip - 0) / (fMaxMps - 0) * 100.0f);

            string sProgress = progressBar(progress);
            StatusLog("V:" + sProgress, textPanelReport);

            if (batteryPercentage >= 0) StatusLog("B:" + progressBar(batteryPercentage), textPanelReport);
            if (oxyPercent >= 0) StatusLog("O:" + progressBar(oxyPercent * 100), textPanelReport);
            if (hydroPercent >= 0) StatusLog("H:" + progressBar(hydroPercent * 100), textPanelReport);

            string sOrientation = "";
            if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                sOrientation = "rocket";

            IMyShipController imsc = shipOrientationBlock as IMyShipController;
            if (imsc != null && imsc.DampenersOverride)
            {
                StatusLog("DampenersOverride ON", textPanelReport);

                Echo("DampenersOverride ON");
            }
            else
            {
                StatusLog("DampenersOverride OFF", textPanelReport);
                Echo("DampenersOverride OFF");
            }

            if (AnyConnectorIsConnected())
            {
                setMode(MODE_IDLE);
                return;
            }
            if (AnyConnectorIsLocked())
            {
                ConnectAnyConnectors(true);
                gearsLock(true);
//                blockApplyAction(gearList, "Lock");
            }

            if (thrustStage1UpList.Count < 1)
            {  // one-time init.
                if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                {
                    thrustStage1UpList = thrustForwardList;
                    thrustStage1DownList = thrustBackwardList;

                    cameraStage1LandingList = cameraBackwardList;
                }
                else
                {
                    thrustStage1UpList = thrustUpList;
                    thrustStage1DownList = thrustDownList;

                    cameraStage1LandingList = cameraDownList;
                }
            }
            ////

            if (dGravity > 0)
            {
                float fMPS = fMaxMps;
                if (velocityShip > fMPS) fMPS = (float)velocityShip;

                retroStartAlt = (int)calculateStoppingDistance(thrustStage1UpList, (double)fMPS, dGravity);
                Echo("dGravity: " + dGravity.ToString("0.00"));

                startReverseAlt = Math.Max(retroStartAlt * 5, minAltRotate);
                Echo("calc retroStartAlt=" + retroStartAlt.ToString());

                retroStartAlt += (int)((shipDim.HeightInMeters() + 1)); // add calc point of height for altitude.. NOTE 'height' is not necessarily correct..
                                                                        //		retroStartAlt += (int)((shipDim.HeightInMeters()+1)/2) ; // add calc point of height for altitude.. NOTE 'height' is not necessarily correct..
                                                                        //		retroStartAlt += (int)fMaxMps; // one second of speed (1s timer delay)
                Echo("adj retroStartAlt=" + retroStartAlt.ToString());
            }

            if (current_state == 0)
            {
                Echo("Init State");
                //powerDownThrusters(thrustAllList,thrustAll,true);
                if (dGravity > 0)
                { // we are starting in gravity
                    if (alt < (startReverseAlt * 1.5))
                    { // just do a landing
                        current_state = 40;
                    }
                    else
                    {
                        current_state = 10;
                    }
                }
                else
                {
                    if (imsc != null && imsc.DampenersOverride)
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = false;
//                        blockApplyAction(shipOrientationBlock, "DampenersOverride"); //DampenersOverride 
//                    ConnectAnyConnectors(false, "OnOff_On");
                    ConnectAnyConnectors(false, true);
                    if (!bValidTarget)
                    {
                        StatusLog("No target landing waypoint set.", textPanelReport);
                        current_state = 10;
                        //			setMode(MODE_IDLE); 
                    }
                    else
                        current_state = 10;
                }
            }
 //           Echo("After init check=" + current_state.ToString());
            if (current_state == 10)
            {
                Echo("Dampeners to on. Aim toward target");
                //		bOverTarget=false; 
                powerDownThrusters(thrustStage1DownList, thrustAll, false);
                bWantFast = true;
                if (bValidTarget)
                {
                    GyroMain(sOrientation, vTarget, shipOrientationBlock);
                        //			startNavWaypoint(vTarget, true);
                    current_state = 11;
                }
                else current_state = 20;
            }
            if (current_state == 11)
            {
                bWantFast = true;
                if (GyroMain(sOrientation, vTarget, shipOrientationBlock))
                    current_state = 20;
                /*
                string sStatus = "Shutdown";// navStatus.CustomName; 
                StatusLog("Waiting for alignment with launch location " + dGravity.ToString(velocityFormat), textPanelReport);

                if (sStatus.Contains("Shutdown"))
                { // somebody hit nav override 
                    Echo("NAV OVERRIDE.  Stopping");
                    current_state = 0;
                }
                if (sStatus.Contains("Done"))
                {
                    current_state = 20;
                }
                */
            }
            if (current_state == 20)
            {

                if (bValidTarget)
                    StatusLog("Move towards recorded landing location", textPanelReport);
                else
                    StatusLog("Move towards surface for landing", textPanelReport);

                if (imsc != null && !imsc.DampenersOverride)
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;
//                    blockApplyAction(shipOrientationBlock, "DampenersOverride");
                //		current_state=30; 
                if (dGravity <= 0 || velocityShip < (fMaxMps * .8))
                    powerUpThrusters(thrustForwardList, 5);
                else powerDownThrusters(thrustForwardList);
                powerDownThrusters(thrustBackwardList, thrustAll, true);
                if (velocityShip > 50 && dGravity > 0)
                    current_state = 30;
                return;
            }
            if (current_state == 21)
            {
                StatusLog("Alignment", textPanelReport);
                current_state = 22;
                return; // give at least one tick of dampeners 
            }
            if (current_state == 22)
            {
                StatusLog("Alignment", textPanelReport);
                current_state = 23;
                return; // give at least one tick of dampeners 
            }
            if (current_state == 23)
            {
                StatusLog("Alignment", textPanelReport);
                current_state = 30;
                return; // give at least one tick of dampeners 
            }
            if (current_state == 30)
            {
                powerDownThrusters(thrustBackwardList, thrustAll, true);

                if (imsc != null && imsc.DampenersOverride)
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = false;
//                    blockApplyAction(shipOrientationBlock, "DampenersOverride");
                current_state = 40;
            }
            if (current_state == 40)
            {
                StatusLog("Free Fall", textPanelReport);
                Echo("Free Fall");
                if (imsc != null && imsc.DampenersOverride)
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = false;
//                    blockApplyAction(shipOrientationBlock, "DampenersOverride");

                if (alt < startReverseAlt)
                {
                    //			startNavCommand("!;V"); 
                    current_state = 60;
                }
                else
                {
                 powerDownThrusters(thrustStage1UpList);
                   StatusLog("Waiting for reverse altitude: " + startReverseAlt.ToString("N0") + " meters", textPanelReport);

                    if (alt > 44000 && alt < 45000)
                        current_state = 10; // re-align 
                    else if (alt > 34000 && alt < 35000)
                        current_state = 10; // re-align 
                    else if (alt > 24000 && alt < 25000)
                        current_state = 10; // re-align 
                    else if (alt > 14000 && alt < 15000)
                        current_state = 10; // re-align 
                }
            }
            if (current_state == 60)
            {
                if (dGravity <= 0)
                {
                    setMode(MODE_IDLE);
                    return;
                }
                //		string sStatus=navStatus.CustomName; 
                StatusLog("Waiting for alignment with gravity", textPanelReport);

                if (imsc != null && imsc.DampenersOverride)
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = false;
//                    blockApplyAction(shipOrientationBlock, "DampenersOverride");

                GyroMain(sOrientation);
                bWantFast = true;
/*
                for (int i = 0; i < gyros.Count; ++i)
                { //kick the ship over since it is 180 to alignment and gyromain will not start rotation when 180
                    IMyGyro g = gyros[i] as IMyGyro;

                    float maxPitch = g.GetMaximum<float>("Pitch");
                    g.SetValueFloat("Pitch", maxPitch);
                    g.SetValueFloat("Yaw", 0);
                    g.SetValueFloat("Roll", 0);
                    g.SetValueBool("Override", true);
                }
*/
                current_state = 61;
                return;
            }

            if (current_state == 61)
            {  // we are rotating ship to gravity..
                if (GyroMain(sOrientation) || alt < retroStartAlt)
                {
                    current_state = 70;
                }
                bWantFast = true;

            }
            if (current_state == 70)
            {
                StatusLog("Waiting for range for retro-thrust:" + retroStartAlt.ToString("N0") + " meters", textPanelReport);
                if (doCameraScan(cameraStage1LandingList, alt * 2)) // scan down 2x current alt
                {
                    // we are able to do a scan
                    if (!lastDetectedInfo.IsEmpty())
                    { // we got something
                        double distance = Vector3D.Distance(shipOrientationBlock.GetPosition(), lastDetectedInfo.HitPosition.Value);
                        if (distance < alt)
                        { // try to land on found thing below us.
                            Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                            StatusLog("Landing on: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                            alt = distance;
                        }
                    }
                }

                GyroMain(sOrientation);
                if (dGravity > .9)
                {
                    powerDownThrusters(thrustAllList, thrustatmo, false);
                    powerDownThrusters(thrustAllList, thrusthydro, false);
                    powerDownThrusters(thrustAllList, thrustion, false);
                    //			powerDownThrusters(thrustAllList,thrustion,true); 
                }
                else if (dGravity < .5)
                {
                    powerDownThrusters(thrustAllList, thrustatmo, true);
                    powerDownThrusters(thrustAllList, thrusthydro, false);
                    powerDownThrusters(thrustAllList, thrustion, false);
                }

                if (alt < (retroStartAlt + fMaxMps * 2)) bWantFast = true;

                if ((alt) < retroStartAlt)
                {
                    if (imsc != null && !imsc.DampenersOverride)
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;
//                        blockApplyAction(shipOrientationBlock, "DampenersOverride");
                    current_state = 90;
                }
            }
            double roll = 0;
            string s;
            if (bValidTarget)
            {
                roll = CalculateRoll(vTarget, shipOrientationBlock);
                s = "Roll=" + roll.ToString("0.00");
                Echo(s);
                StatusLog(s, textPanelReport);
            }

            if (current_state == 90)
            {
                StatusLog("RETRO! Waiting for ship to slow", textPanelReport);
                if (velocityShip < 1)
                {
                    current_state = 200;
                }
                //		if (velocityShip < 50)
                {
                    if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                        StatusLog("Wico Gyro Alignment OFF", textPanelReport);
                    else
                    {
                        GyroMain(sOrientation);

                        if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                            DoRoll(roll);
                        else
                            DoRoll(roll, "Yaw");

                        bWantFast = true;
                    }
                }
            }

            if (current_state == 100)
            {
                StatusLog("Player control for final docking", textPanelReport);
                if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    StatusLog("Wico Gyro Alignment OFF", textPanelReport);
                else
                {
                    GyroMain(sOrientation);
                }
            }
            else if (current_state == 200)
            {
                StatusLog("Orient toward landing location", textPanelReport);

                if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    StatusLog("Wico Gyro Alignment OFF", textPanelReport);
                else
                {
                    GyroMain(sOrientation);
                }
                if (bValidTarget)
                {
                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                        DoRoll(roll);
                    else
                        DoRoll(roll, "Yaw");
                    if (roll < .01 && roll >= -.01)
                    {
                        current_state = 201;
                    }
                    else bWantFast = true;
                }
                else current_state = 202;

            }
            else if (current_state == 201)
            {
                if (bValidTarget)
                    StatusLog("Move towards recorded landing location", textPanelReport);
                else
                    StatusLog("Move towards surface for landing", textPanelReport);

                if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    StatusLog("Wico Gyro Alignment OFF", textPanelReport);
                else
                {

                    GyroMain(sOrientation);

                }
                s = "velocity=" + velocityShip.ToString("0.00");
                Echo(s);
                StatusLog(s, textPanelReport);

                if (roll < .01 && roll >= -0.01)
                {
                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                        DoRoll(roll);
                    else
                        DoRoll(roll, "Yaw");
                    if (halt > 50)
                        if (velocityShip > 75)
                            powerUpThrusters(thrustStage1UpList, 1);
                        else
                            powerUpThrusters(thrustStage1UpList, 100);
                    else if (halt > 25)
                        if (velocityShip > 10)
                            powerUpThrusters(thrustStage1UpList, 1);
                        else
                            powerUpThrusters(thrustStage1UpList, 75);
                    else if (halt > 9)
                        if (velocityShip > 5)
                            powerUpThrusters(thrustStage1UpList, 1);
                        else
                            powerUpThrusters(thrustStage1UpList, 35);
                    else
                    {
                        s = "Stop for roll only";
                        Echo(s);
                        StatusLog(s, textPanelReport);

                        powerDownThrusters(thrustStage1UpList);
                        gyrosOff();
                        if (velocityShip < 0.5)
                            current_state = 202;
                    }
                }
                else
                {
                    if (Math.Abs(roll) >= 1.0)
                    {
                        if (velocityShip < 0.01)
                            current_state = 202;
                    }
                    powerDownThrusters(thrustStage1UpList);
                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                        DoRoll(roll);
                    else
                        DoRoll(roll, "Yaw");
                    bWantFast = true;
                }


            }
            else if (current_state == 202)
            { // we are 'over' location.

                bWantFast = true;
                float hoveratmoPercent = 0;
                float hoverhydroPercent = 0;
                float hoverionPercent = 0;

                calculateHoverThrust(thrustStage1UpList, out hoveratmoPercent, out hoverhydroPercent, out hoverionPercent);
                bool bLandingReady=landingDoMode(1);

                if (doCameraScan(cameraStage1LandingList, alt * 2)) // scan down 2x current alt
                {
                    // we are able to try a scan
                    if (!lastDetectedInfo.IsEmpty())
                    { // we got something
                        double distance = Vector3D.Distance(shipOrientationBlock.GetPosition(), lastDetectedInfo.HitPosition.Value);
                        if (distance < alt)
                        { // try to land on found thing below us.
                            Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                            StatusLog("Landing on: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                            alt = distance;
                        }
                    }
                }

                //			calculateHoverThrust(thrustStage1UpList, out hoveratmoPercent, out hoverhydroPercent, out hoverionPercent);

                Echo("down#=" + thrustStage1DownList.Count.ToString());
                Echo("alt=" + alt.ToString());
                StatusLog("Descending toward landing location", textPanelReport);

                if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    StatusLog("Wico Gyro Alignment OFF", textPanelReport);
                else
                {
                    //			if(alt>100) 
                    GyroMain(sOrientation);
                }
                s = "velocity=" + velocityShip.ToString("0.00");
                Echo(s);
                StatusLog(s, textPanelReport);


                if (bValidTarget)
                {
                    StatusLog("Have a Target", textPanelReport);

                    if (roll < .01 && roll >= 0)
                    {
                        Echo("aiming target");
                        bWantFast = true;

                        if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                            DoRoll(roll);
                        else
                            DoRoll(roll, "Yaw");
                        if (halt > 50)
                            if (velocityShip > 25)
                                powerUpThrusters(thrustStage1UpList, 1);
                            else
                                powerUpThrusters(thrustStage1UpList, 100);
                        else if (halt > 25)
                            if (velocityShip > 10)
                                powerUpThrusters(thrustStage1UpList, 1);
                            else
                                powerUpThrusters(thrustStage1UpList, 75);
                        else if (halt > 1)
                            if (velocityShip > 2)
                                powerUpThrusters(thrustStage1UpList, 1);
                            else
                                powerUpThrusters(thrustStage1UpList, 25);
                        else
                        {
                            powerDownThrusters(thrustStage1UpList);
                            gyrosOff();
                            if (velocityShip < .01)
                                current_state = 202;
                        }
                    }
                    else
                    {
                        powerDownThrusters(thrustStage1UpList);
                        if (bValidTarget)
                        {
                            if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                                DoRoll(roll);
                            else
                                DoRoll(roll, "Yaw");
                            bWantFast = true;
                        }
                    }
                }
                else Echo("No Target");
                Echo("halt=" + halt.ToString());
                if (halt < 5)
                {
                    StatusLog("'Above' Target or blind target", textPanelReport);

                    powerDownThrusters(thrustAllList);
                    if (alt > 500)
                    {
                        StatusLog(">500 Alt", textPanelReport);

                        if (velocityShip > 55)
                        {
                            powerDownThrusters(thrustAllList); // slow down
                        }
                        else
                        {
                            powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.50), thrustatmo);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.50), thrusthydro);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.50), thrustion);
                        }
                    }
                    else if (alt > 100) //100 to 500
                    {
                        StatusLog(">100 Alt", textPanelReport);

                        gyrosOff();
                        if (bValidTarget)
                        {
                            GyroMain(sOrientation, vTarget - shipOrientationBlock.GetPosition(), shipOrientationBlock);
                        }
                        else
                            GyroMain(sOrientation);

                        powerDownThrusters(thrustAllList);

                        if (velocityShip > 20 || !bLandingReady)
                        {
                            powerDownThrusters(thrustStage1DownList);
                            powerDownThrusters(thrustStage1UpList);
                        }
                        else
                        {

                            //powerUpThrusters(thrustStage1UpList, (float)(hoverthrust * 0.97));

                            powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.60), thrustatmo);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.60), thrusthydro);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.60), thrustion);

                        }
                        //					powerUpThrusters(thrustStage1DownList,65); 
                    }
                    else if (alt > 20) // 20 to 100
                    {
                        StatusLog(">20 Alt", textPanelReport);

                        gyrosOff();
                        if (bValidTarget)
                        {
                            GyroMain(sOrientation, vTarget - shipOrientationBlock.GetPosition(), shipOrientationBlock);
                        }
                        else
                            GyroMain(sOrientation);

                        if (velocityShip > 15 || !bLandingReady)
                        {
                            // too fast or wait for landing mode
                            powerDownThrusters(thrustStage1UpList);
                        }
                        else if (velocityShip > 5)
                        {
                            powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.99), thrustatmo);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.99), thrusthydro);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.99), thrustion);

                            //						powerDownThrusters(thrustStage1UpList);
                        }
                        else
                        {
                            //powerUpThrusters(thrustStage1UpList, (float)(hoverthrust * 0.99));
                            powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.75), thrustatmo);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.75), thrusthydro);
                            powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.75), thrustion);
                        }
                    }
                    else if (alt > 1)
                    {
                        StatusLog(">1 Alt", textPanelReport);

                        gyrosOff();
                        if (bValidTarget)
                        {
                            GyroMain(sOrientation, vTarget - shipOrientationBlock.GetPosition(), shipOrientationBlock);
                        }
                        else
                            GyroMain(sOrientation);

                        if (bValidOrbitalLaunch)
                        {
                            if (velocityShip > 3 || !bLandingReady)
                            {
                                powerDownThrusters(thrustStage1UpList);
                            }
                            if (velocityShip > 1)
                            {
                                powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.85), thrustatmo);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.85), thrusthydro);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.85), thrustion);

                                //						powerDownThrusters(thrustStage1UpList);
                            }
                            else
                            {
                                powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.81), thrustatmo);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.81), thrusthydro);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.81), thrustion);
                            }
                        }
                        else if (bValidOrbitalHome)
                        {
                            // we had started from hover.. 
                            setMode(MODE_HOVER);
                            powerDownThrusters(thrustAllList);
                            gyrosOff();
                        }
                        else
                        {
                            // turn on autolock on landing gears..

                            // we are doing blind landing; keep going.
                            if (velocityShip > 3)
                            {
                                powerDownThrusters(thrustStage1UpList);
                            }
                            else if (velocityShip > 1)
                            {
                                powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.85), thrustatmo);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.85), thrusthydro);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.85), thrustion);

                                //						powerDownThrusters(thrustStage1UpList);
                            }
                            else
                            {
                                powerUpThrusters(thrustStage1UpList, (float)(hoveratmoPercent * 0.81), thrustatmo);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverhydroPercent * 0.81), thrusthydro);
                                powerUpThrusters(thrustStage1UpList, (float)(hoverionPercent * 0.81), thrustion);
                            }
                            if (anyGearIsLocked())
                            {
                                powerDownThrusters(thrustAllList, thrustAll, true);// turn off all thrusters
                                setMode(MODE_IDLE); // we have done our job.  pass it on to somebody else..
                            }
                        }
                    }
                    else
                    {
                        powerDownThrusters(thrustAllList);
                        gyrosOff();
                    }
                }
                else
                {
                    powerDownThrusters(thrustAllList);
                }
            }
            else if (current_state == 203)
            {
            }
            else if (current_state == 204)
            {
            }
            else if (current_state == 205)
            {
            }
            else if (current_state == 206)
            {
            }
            else if (current_state == 207)
            {
            }
            Echo("End state=" + current_state);

        }

        List<IMyTerminalBlock> thrustStage1UpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustStage1DownList = new List<IMyTerminalBlock>();
        //List<IMyTerminalBlock>thrustStage2UpList=new List<IMyTerminalBlock>(); 

        List<IMyTerminalBlock> cameraStage1LandingList = new List<IMyTerminalBlock>();



    }
}