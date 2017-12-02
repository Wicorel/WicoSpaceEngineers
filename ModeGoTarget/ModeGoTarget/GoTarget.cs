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
        // NAV
        double arrivalDistanceMin = 50;
        //        double arrivalDistanceMax = 100;
        double speedMax = 100;
        bool bGoOption = true; // false means just orient.
        bool bSled = false;
        bool bRotor = false;

        void doModeGoTarget()
        {
            StatusLog("clear", textPanelReport);

            StatusLog(moduleName + ":Going Target!", textPanelReport);
            StatusLog(moduleName + ":GT: current_state=" + current_state.ToString(), textPanelReport);
            bWantFast = true;
            Echo("Going Target: state=" + current_state.ToString());
            if (current_state == 0)
            {
                if ((craft_operation & CRAFT_MODE_SLED) > 0)
                {
                    bSled = true;
                    if (speedMax > 45) speedMax = 45;
                }
                else bSled = false;

                if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
                {
                    bRotor = true;
                    if (speedMax > 15) speedMax = 15;
                }
                else bRotor = false;

                GyroControl.SetRefBlock(gpsCenter);
                if (bValidHome || bValidTarget)
                {
                    current_state = 160;
                }
                else setMode(MODE_ATTENTION);
            }
            else if (current_state == 160)
            { //	160 move to Target
                Echo("Moving to Target");
                Vector3D vTargetLocation = vHome;
                if (bValidTarget)
                    vTargetLocation = vTargetMine;


                Vector3D vVec = vTargetLocation - gpsCenter.GetPosition();
                double distance = vVec.Length();
                Echo("distance=" + niceDoubleMeters(distance));
                Echo("velocity=" + velocityShip.ToString("0.00"));
                //      Echo("TL:" + vTargetLocation.X.ToString("0.00") + ":" + vTargetLocation.Y.ToString("0.00") + ":" + vTargetLocation.Z.ToString("0.00"));
                //		if(distance<17)
                if (bGoOption && (distance < arrivalDistanceMin))
                {
                    Echo("we have arrived");
                    //				bValidTargetLocation = false;
                    gyrosOff();
                    ResetMotion();
                    bValidHome = false; // we used this one up.
                    setMode(MODE_ARRIVEDTARGET);
                    return;
                }
                bool bYawOnly = false;
                if (bSled || bRotor) bYawOnly = true;

                debugGPSOutput("TargetLocation", vTargetLocation);

                bool bAimed = false;
                double yawangle = -999;
                if (bYawOnly)
                {
                    yawangle = CalculateYaw(vTargetLocation, gpsCenter);
                    Echo("yawangle=" + yawangle.ToString());
                    bAimed = Math.Abs(yawangle) < .05;
                    if (bSled)
                        DoRotate(yawangle, "Yaw");
                    else if (bRotor)
                        DoRotorRotate(yawangle);
                    // else:  WE DON"T KNOW WHAT WE ARE

                }
                else if (bRotor)
                {
                    bAimed = GyroMain("forward", vVec, gpsCenter);
                    if (bAimed)
                    {
                        // we are aimed at location
                        Echo("Aimed");
                        gyrosOff();
                        if (!bGoOption)
                        {
                            powerDownRotors(rotorNavLeftList);
                            powerDownRotors(rotorNavRightList);

                            powerDownThrusters(thrustAllList);
                            setMode(MODE_ARRIVEDTARGET);
                            return;
                        }

                        double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                        double dFar = 100;
                        double dApproach = 50;
                        double dPrecision = 15;
                        if (velocityShip > 5) dFar = stoppingDistance * 5;
                        if (velocityShip > 5) dApproach = stoppingDistance * 2;
                        Echo("distance=" + niceDoubleMeters(distance));
                        //                   Echo("DFar=" + dFar);
                        //                   Echo("dApproach=" + dApproach);
                        //                   Echo("dPrecision=" + dPrecision);
                        Echo("speedMax=" + speedMax);
                        Echo("velocityShip=" + velocityShip);
                        if (distance > dFar)
                        {
                            //                       Echo("DFAR");
                            if (velocityShip < 1)
                            {
                                Echo("DFAR*1");
                                powerForward(100);
                            }
                            else if (velocityShip < (speedMax * 0.85))
                            //                        else if (velocityShip < speedMax / 2)
                            {
                                Echo("DFAR**2");
                                powerForward(55);
                            }
                            else if (velocityShip < (speedMax * 1.05))
                            {
                                Echo("DFAR***3");
                                powerForward(1);
                            }
                            else
                            {
                                Echo("DFAR****4");
                                powerDown();
                            }
                        }
                        else if (distance > dApproach)
                        {
                            Echo("Approach");

                            if (velocityShip < 1)
                                powerForward(100);
                            else if (velocityShip < speedMax / 2)
                                powerForward(25);
                            else if (velocityShip < speedMax)
                                powerForward(1);
                            else
                                powerDown();
                        }
                        else if (distance > dPrecision)
                        {
                            Echo("Precision");
                            // almost  to target.  should take stoppingdistance into account.
                            if (velocityShip < 1)
                                powerForward(100);
                            else if (velocityShip < speedMax / 2)
                                powerForward(25);
                            else if (velocityShip < speedMax)
                                powerForward(1);
                            else
                                powerDown();
                        }
                        else
                        {
                            Echo("Close");
                            if (velocityShip < 1)
                                powerForward(25);
                            else if (velocityShip < 5)
                                powerForward(5);
                            else
                                powerDown();
                        }

                    }
                    else
                    {
                        // we are aiming at location
                        Echo("Aiming");
                        //			DoRotate(yawangle, "Yaw");

                        // DO NOT turn off rotors..
                        powerDownThrusters(thrustAllList);

                    }
                }
                else
                {
                    doTravelMovement(vTargetLocation, 3.0f, 200, 170);
                }
            }

            else if(current_state==170)
            { // collision detection
//                IMyTextPanel tx = gpsPanel;
//                gpsPanel = textLongStatus;
 //           StatusLog("clear", gpsPanel);

                Vector3D vTargetLocation = vHome;
                if (bValidTarget)
                    vTargetLocation = vTargetMine;
                ResetTravelMovement();
                calcCollisionAvoid(vTargetLocation);

//                gpsPanel = tx;
//                current_state = 171; // testing
                current_state = 172;
            }
            else if (current_state == 171)
            { 
                // just hold this state
                bWantFast = false;
            }

            else if (current_state == 172)
            {
                doTravelMovement(vAvoid, 5.0f, 160, 173);
            }
            else if (current_state == 173)
            {       // secondary collision
                if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                {
                    current_state = 174;// setMode(MODE_ATTENTION);
                }
                else current_state = 170;
            }
            else if (current_state == 174)
            {
                initEscapeScan();
                dtStartShip = DateTime.Now;
                current_state = 175;
            }
            else if (current_state == 175)
            {
                DateTime dtMaxWait = dtStartShip.AddSeconds(5.0f);
                DateTime dtNow = DateTime.Now;
                if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                {
                    setMode(MODE_ATTENTION);
                    return;
                }
                if (scanEscape())
                {
                    current_state = 172;
                }
            }
            else if(current_state==200)
            { // we have arrived at target
                ResetMotion();
                bValidHome = false; // we used this one up.
                setMode(MODE_ARRIVEDTARGET);
            }
        }

        void powerForward(float fPower)
        {
            if (bRotor)
            {
                /*
                // need to ramp up/down rotor power or they will flip small vehicles and spin a lot
                float maxVelocity = rotorNavLeftList[0].GetMaximum<float>("Velocity");
                float currentVelocity = rotorNavLeftList[0].GetValueFloat("Velocity");
                float cPower = (currentVelocity / maxVelocity * 100);
                cPower = Math.Abs(cPower);
                if (fPower > (cPower + 5f))
                    fPower = cPower + 5;
                if (fPower < (cPower - 5))
                    fPower = cPower - 5;

                if (fPower < 0f) fPower = 0f;
                if (fPower > 100f) fPower = 100f;
                */
                powerUpRotors(fPower);
            }
            else
                powerUpThrusters(thrustForwardList, fPower);
        }

        void powerDown()
        {
            powerDownThrusters(thrustAllList);
            powerDownRotors();
        }
    }
}