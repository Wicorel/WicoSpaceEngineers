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

        //TODO: Add loading and saving NAV specific settings to its own section

        // NAV
        //        double arrivalDistanceMax = 100;

        /// <summary>
        /// false means just orient (no motion)
        /// </summary>
        bool bGoOption = true; // false means just orient.

        /// <summary>
        /// We are a sled. Default false
        /// </summary>
        bool bSled = false;

        /// <summary>
        /// We are rotor-control propulsion. Default false
        /// </summary>
        bool bRotor = false;

        /*
        States:
        0 -- Master Init


            150. (spawn) initialize command in gravity. first align to gravity

        160 Main Travel to target



        *** below here are thruster-only routines (for now)

        170 Collision Detected From 160
            Calculate collision avoidance 
            then ->172

        171 dummy state for debugging.
        172 do travel movemenet for collision avoidance. 
        if arrive target, ->160
        if secondary collision ->173

        173 secondary collision
        if a type we can move around, try to move ->174
        else go back to collision detection ->170

        174 initilize escape plan
        ->175

        175 scan for an 'escape' route (pathfind)
        timeout of (default) 5 seconds ->MODE_ATTENTION
        after scans, ->180

        180 travel to avoidance waypoint
        on arrival ->160 (main travel)
        on collision ->173

        200 Arrived at target
        ->MODE_ARRIVEDTARGET

        */
        void doModeGoTarget()
        {
            StatusLog("clear", textPanelReport);

            StatusLog(moduleName + ":Going Target!", textPanelReport);
            StatusLog(moduleName + ":GT: current_state=" + current_state.ToString(), textPanelReport);
//            bWantFast = true;
            Echo("Going Target: state=" + current_state.ToString());

            string sNavDebug = "";
                        sNavDebug+="GT:S=" + current_state;
            //            sNavDebug += " MinE=" + NAVGravityMinElevation;
//            ResetMotion();

            if (current_state == 0)
            {
                ResetTravelMovement();

                if ((craft_operation & CRAFT_MODE_SLED) > 0)
                {
                    bSled = true;
                    if (shipSpeedMax > 45) shipSpeedMax = 45;
                }
                else bSled = false;

                if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
                {
                    bRotor = true;
                    if (shipSpeedMax > 15) shipSpeedMax = 15;
                }
                else bRotor = false;

                GyroControl.SetRefBlock(shipOrientationBlock);

                double elevation = 0;
                ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);

                if(!bSled && !bRotor)
                { // if flying ship
                    // make sure set to default
                    if (NAVGravityMinElevation < 0)
                        NAVGravityMinElevation = 75; // for EFM getting to target 'arrived' radius

//                    NAVGravityMinElevation = (float)shipSpeedMax*2.5f;
                }

                if (bValidNavTarget)
                {
                    if (elevation> shipDim.HeightInMeters())
                    {
                        current_state = 150;
                    }
                    else current_state = 160;
                }
                else setMode(MODE_ATTENTION);
                bWantFast = true;
            }
            else if (current_state == 150)
            {
                bWantFast = true;
                if (dGravity > 0)
                {

                    double elevation = 0;

                    ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                    sNavDebug += " E=" + elevation.ToString("0.0");

                    float fSaveAngle = minAngleRad;
                    minAngleRad = 0.1f;
                    Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();

                    bool bAligned = GyroMain("", grav, shipOrientationBlock);
                    sNavDebug += " Aligned=" + bAligned.ToString();

                    Echo("bAligned=" + bAligned.ToString());
                    minAngleRad = fSaveAngle;
                    if (bAligned || elevation < shipDim.HeightInMeters() * 2)
                    {
                        gyrosOff();
                        if (NAVGravityMinElevation > 0)
                            current_state = 155;
                        else current_state = 160;
                    }
                }
                else current_state = 160;

            }
            else if (current_state == 151)
            {
                bWantFast = true;
                if (dGravity > 0)
                {

                    double elevation = 0;

                    ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                    sNavDebug += " E=" + elevation.ToString("0.0");

                    float fSaveAngle = minAngleRad;
                    minAngleRad = 0.1f;
                    Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();

                    bool bAligned = GyroMain("", grav, shipOrientationBlock);
                    sNavDebug += " Aligned=" + bAligned.ToString();

                    Echo("bAligned=" + bAligned.ToString());
                    minAngleRad = fSaveAngle;
                    if (bAligned || elevation < shipDim.HeightInMeters() * 2)
                    {
                        gyrosOff();
                        if (NAVGravityMinElevation > 0)
                            current_state = 155;
                        else current_state = 160;
                    }
                    else current_state = 150;// try again to be aligned.
                }
                else current_state = 160;

            }
            else if (current_state == 155)
            { // for use in gravity: aim at location using yaw only
                bWantFast = true;
                if (dGravity > 0)
                {
                    Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                    bool bAligned=GyroMain("", grav, shipOrientationBlock);
                    sNavDebug += " Aligned=" + bAligned.ToString();

                    double yawangle = -999;
                    yawangle = CalculateYaw(vNavTarget, shipOrientationBlock);
                    bool bAimed = Math.Abs(yawangle) < 0.1; // NOTE: 2x allowance
                    Echo("yawangle=" + yawangle.ToString());
                    sNavDebug+=" Yaw=" + yawangle.ToString("0.00");

                    if (!bAimed)
                    {
                        if (btmRotor)
                        {
                            Echo("Rotor");
                            DoRotorRotate(yawangle);
                        }
                        else // use for both sled and flight
                        {
                            DoRotate(yawangle, "Yaw");
                        }
                    }
                    if (bAligned && bAimed)
                    {
                        gyrosOff();
                        current_state = 160;
                    }
                    else if (bAligned && Math.Abs(yawangle) < 0.5)
                    {
                        float atmo;
                        float hydro;
                        float ion;

                        calculateHoverThrust(thrustForwardList, out atmo, out hydro, out ion);
                        atmo += 1;
                        hydro += 1;
                        ion += 1;

                        powerUpThrusters(thrustForwardList, atmo, thrustatmo);
                        powerUpThrusters(thrustForwardList, hydro, thrusthydro);
                        powerUpThrusters(thrustForwardList, ion, thrustion);

                    }
                    else
                        powerDownThrusters(thrustForwardList);
                }
                else current_state = 160;
            }
            else if(current_state == 156)
            {
                // realign gravity
                bWantFast = true;
                Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                bool bAimed = GyroMain("", grav, shipOrientationBlock);
                if(bAimed)
                {
                    gyrosOff();
                    current_state = 160;
                }
            }
            else if (current_state == 160)
            { //	160 move to Target
                Echo("Moving to Target");
                Vector3D vTargetLocation = vNavTarget;

                Vector3D vVec = vTargetLocation - shipOrientationBlock.GetPosition();
                double distance = vVec.Length();
                Echo("distance=" + niceDoubleMeters(distance));
                Echo("velocity=" + velocityShip.ToString("0.00"));

                StatusLog("clear",sledReport);
                StatusLog("Moving to Target\nD:" + niceDoubleMeters(distance) + " V:" + velocityShip.ToString(velocityFormat),sledReport);


                if (bGoOption && (distance < arrivalDistanceMin))
                {
                    current_state = 200;

                    Echo("we have arrived");
                    bWantFast = true;
                    /*
                    if (NAVEmulateOld)
                    {
                        var tList = GetBlocksContains<IMyTerminalBlock>("NAV:");
                        for (int i1 = 0; i1 < tList.Count(); i1++)
                        {
                            // don't want to get blocks that have "NAV:" in customdata..
                            if (tList[i1].CustomName.StartsWith("NAV:"))
                            {
                                Echo("Found NAV: command:");
                                tList[i1].CustomName = "NAV: C Arrived Target";
                            }
                        }
                    }
                    ResetMotion();
                    bValidNavTarget = false; // we used this one up.
                    setMode(MODE_ARRIVEDTARGET);
                    */
                    return;
                }

//                debugGPSOutput("TargetLocation", vTargetLocation);
                bool bDoTravel = true;

                if (NAVGravityMinElevation > 0 && dGravity>0)
                {
                    double elevation = 0;

                    MyShipVelocities mysSV = ((IMyShipController)shipOrientationBlock).GetShipVelocities();
                    Vector3D lv = mysSV.LinearVelocity;

                    double vertVel = lv.X;

                    Echo("LV=" + Vector3DToString(lv));
                    //                    sNavDebug += " LV=" + Vector3DToString(lv);
//                    sNavDebug += " vertVel=" + vertVel.ToString("0.0");
//                    sNavDebug += " Hvel=" + lv.Y.ToString("0.0");

                    // NOTE: Elevation is only updated by game every 30? ticks. so it can be WAY out of date based on movement
                    ((IMyShipController)shipOrientationBlock).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                    sNavDebug += " E=" + elevation.ToString("0.0");
                    sNavDebug += " V=" + velocityShip.ToString("0.00");

                    Echo("Elevation=" + elevation.ToString("0.0"));
                    Echo("MinEle=" + NAVGravityMinElevation.ToString("0.0"));

                    //                    double stopD = calculateStoppingDistance(thrustUpList, velocityShip, dGravity);
                    double stopD = 0;
                    if(vertVel < 0)
                    {
                        stopD = calculateStoppingDistance(thrustUpList, Math.Abs(vertVel), dGravity);
                    }
                    double maxStopD= calculateStoppingDistance(thrustUpList, fMaxWorldMps, dGravity);

                    float atmo;
                    float hydro;
                    float ion;
                    calculateHoverThrust(thrustUpList, out atmo, out hydro, out ion);

//                    sNavDebug += " SD=" + stopD.ToString("0");

                    if (
                        //                        !bSled && !bRotor && 
                        NAVGravityMinElevation > 0)
                    {
                        if (
                            vertVel < -0.5  // we are going downwards
                            && (elevation - stopD*2) < NAVGravityMinElevation)
                        { // too low. go higher
                            // Emergency thrust
                            sNavDebug += " EM UP!";

                            Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                            bool bAligned = GyroMain("", grav, shipOrientationBlock);

                            powerUpThrusters(thrustUpList, 100);
                            bDoTravel = false;
                            bWantFast = true;
                        }
                        else if (elevation < NAVGravityMinElevation)
                        {
                            // push upwards
                            atmo += Math.Min(5f, (float)shipSpeedMax);
                            hydro += Math.Min(5f, (float)shipSpeedMax);
                            ion += Math.Min(5f, (float)shipSpeedMax);
                            sNavDebug += " UP! A" + atmo.ToString("0.00");// + " H"+hydro.ToString("0.00") + " I"+ion.ToString("0.00");
                                                                          //powerUpThrusters(thrustUpList, 100);
                            powerUpThrusters(thrustUpList, atmo, thrustatmo);
                            powerUpThrusters(thrustUpList, hydro, thrusthydro);
                            powerUpThrusters(thrustUpList, ion, thrustion);

                        }
                        else if(elevation>(maxStopD+NAVGravityMinElevation*1.25))
                        {
                            // if we are higher than maximum possible stopping distance, go down fast.
                            sNavDebug += " SUPERHIGH";

 //                           Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
//                            bool bAligned = GyroMain("", grav, shipOrientationBlock);

                            powerDownThrusters(thrustUpList, thrustAll, true);
                            Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                            bool bAligned = GyroMain("", grav, shipOrientationBlock);
                            if(!bAligned)
                            {
                                bWantFast = true;
                                bDoTravel = false;
                            }
                            //                            powerUpThrusters(thrustUpList, 1f);
                        }
                        else if (
                            elevation > NAVGravityMinElevation * 2  // too high
//                            && ((elevation-stopD)>NAVGravityMinElevation) // we can stop in time.
//                        && velocityShip < shipSpeedMax * 1.1 // to fast in any direction
//                           && Math.Abs(lv.X) < Math.Min(25, shipSpeedMax) // not too fast 
//                            && Math.Abs(lv.Y) < Math.Min(25, shipSpeedMax) // not too fast downwards (or upwards)
                            )
                        { // too high 
                            sNavDebug += " HIGH";
                            //DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");

                            if (vertVel > 2) // going up
                            { // turn off thrusters.
                                sNavDebug += " ^";
                                powerDownThrusters(thrustUpList, thrustAll, true);
                            }
                            else if (vertVel < -0.5) // going down
                            {
                                sNavDebug += " v";
                                if(vertVel > (-Math.Min(15,shipSpeedMax)))
                                {
                                    // currently descending at less than desired
                                    atmo -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                    hydro -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                    ion -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                    sNavDebug += " DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");
 //                                   bDoTravel = false;
                                }
                                else
                                {
                                    // we are descending too fast.
                                    atmo += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                    hydro += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                    ion += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                    sNavDebug += " 2FAST! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");
                                    Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                                    bool bAligned = GyroMain("", grav, shipOrientationBlock);
                                    if (!bAligned)
                                    {
                                        bWantFast = true;
                                        bDoTravel = false;
                                    }
//                                    bDoTravel = false;
                                }

                            }
                            else
                            {
                                sNavDebug += " -";
                                atmo -= 5;
                                hydro -= 5;
                                ion -= 5;
                            }

                            powerUpThrusters(thrustUpList, atmo, thrustatmo);
                            powerUpThrusters(thrustUpList, hydro, thrusthydro);
                            powerUpThrusters(thrustUpList, ion, thrustion);

                        }
                        else
                        {
                            // normal hover
                            powerDownThrusters(thrustUpList);
                        }
                    }
                }
                if (bDoTravel)
                {
                    Echo("Do Travel");
                    doTravelMovement(vTargetLocation, (float)arrivalDistanceMin, 200, 170);
                }
                else
                {
                    powerDownThrusters(thrustForwardList);
                }
            }

            else if(current_state==170)
            { // collision detection

                bWantFast = true;
                Vector3D vTargetLocation = vNavTarget;
                ResetTravelMovement();
                calcCollisionAvoid(vTargetLocation);

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
                //                 Vector3D vVec = vAvoid - shipOrientationBlock.GetPosition();
                //                double distanceSQ = vVec.LengthSquared();
                Echo("Collision Avoid");
                StatusLog("clear", sledReport);
                StatusLog("Collision Avoid", sledReport);
                doTravelMovement(vAvoid, 5.0f, 160, 173);
            }
            else if (current_state == 173)
            {       // secondary collision
                if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid 
                    || lastDetectedInfo.Type == MyDetectedEntityType.LargeGrid 
                    || lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid 
                    )
                {
                    current_state = 174;
                }
                else current_state = 170;// setMode(MODE_ATTENTION);
                bWantFast = true;
            }
            else if (current_state == 174)
            {
                initEscapeScan();
                ResetTravelMovement();
                dtNavStartShip = DateTime.Now;
                current_state = 175;
                bWantFast = true;
            }
            else if (current_state == 175)
            {
                DateTime dtMaxWait = dtNavStartShip.AddSeconds(5.0f);
                DateTime dtNow = DateTime.Now;
                if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                {
                    setMode(MODE_ATTENTION);
                    doTriggerMain();
                    return;
                }
                if (scanEscape())
                {
                    Echo("ESCAPE!");
                    current_state = 180;
                }
                bWantMedium = true;
//                bWantFast = true;
           }
            else if(current_state==180)
            {
                doTravelMovement(vAvoid,1f, 160, 173);
            }
            else if(current_state==200)
            { // we have arrived at target
                StatusLog("clear", sledReport);
                StatusLog("Arrived at Target", sledReport);
                sNavDebug += " ARRIVED!";

                ResetMotion();
                bValidNavTarget = false; // we used this one up.
//                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false);
                sleepAllSensors();
                setMode(MODE_ARRIVEDTARGET);
                if(NAVEmulateOld)
                {
                    var tList = GetBlocksContains<IMyTerminalBlock>("NAV:");
                    for (int i1 = 0; i1 < tList.Count(); i1++)
                    {
                        // don't want to get blocks that have "NAV:" in customdata..
                        if (tList[i1].CustomName.StartsWith("NAV:"))
                        {
                            Echo("Found NAV: command:");
                            tList[i1].CustomName = "NAV: C Arrived Target";
                        }
                    }
                }
                bWantFast = true;
                doTriggerMain();
            }
            NavDebug(sNavDebug);
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