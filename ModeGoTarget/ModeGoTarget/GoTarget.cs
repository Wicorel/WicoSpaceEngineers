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
                    if (speedMax > 45) speedMax = 45;

                //	setAlertState(ALERT_DOCKING);
                //	clearAlertState(ALERT_GOINGHOME | ALERT_LAUNCHING | ALERT_GOINGTARGET | ALERT_DOCKINGASSIST);
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
                if ((craft_operation & CRAFT_MODE_SLED) > 0) bYawOnly = true;
                debugGPSOutput("TargetLocation", vTargetLocation);

                bool bAimed = false;
                double yawangle = -999;
                if (bYawOnly)
                {
                    yawangle = CalculateYaw(vTargetLocation, gpsCenter);
                    bAimed = Math.Abs(yawangle) < .05;
                    DoRotate(yawangle, "Yaw");
                }
                else
                {
                    //vVec = vTargetLocation - gpsCenter.GetPosition();
                    bAimed = GyroMain("forward", vVec, gpsCenter);
                }
                //return;

                if (bAimed)
                //		if (GyroMain("forward", vVec, gpsCenter, bYawOnly))
                {
                    // we are aimed at location
                    Echo("Aimed");
                    gyrosOff();
                    if (!bGoOption)
                    {
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
                    Echo("DFar=" + dFar);
                    Echo("dApproach=" + dApproach);
                    Echo("dPrecision=" + dPrecision);
                    if (distance > dFar)
                    {
                        Echo("DFAR");
                        if (velocityShip < 1)
                            powerUpThrusters(thrustForwardList, 100);
                        else if (velocityShip < speedMax / 2)
                            powerUpThrusters(thrustForwardList, 25);
                        else if (velocityShip < speedMax)
                            powerUpThrusters(thrustForwardList, 1);
                        else
                            powerDownThrusters(thrustAllList);
                    }
                    else if (distance > dApproach)
                    {
                        Echo("Approach");

                        if (velocityShip < 1)
                            powerUpThrusters(thrustForwardList, 100);
                        else if (velocityShip < speedMax / 2)
                            powerUpThrusters(thrustForwardList, 25);
                        else if (velocityShip < speedMax)
                            powerUpThrusters(thrustForwardList, 1);
                        else
                            powerDownThrusters(thrustAllList);
                    }
                    else if (distance > dPrecision)
                    {
                        Echo("Precision");
                        // almost  to target.  should take stoppingdistance into account.
                        if (velocityShip < 1)
                            powerUpThrusters(thrustForwardList, 100);
                        else if (velocityShip < speedMax / 2)
                            powerUpThrusters(thrustForwardList, 25);
                        else if (velocityShip < speedMax)
                            powerUpThrusters(thrustForwardList, 1);
                        else
                            powerDownThrusters(thrustAllList);
                    }
                    else
                    {
                        Echo("Close");
                        if (velocityShip < 1)
                            powerUpThrusters(thrustForwardList, 25);
                        else if (velocityShip < 5)
                            powerUpThrusters(thrustForwardList, 5);
                        //				else if (velocityShip <= 15)
                        //					powerUpThrusters(thrustForwardList, 1);
                        else
                            powerDownThrusters(thrustAllList);
                    }

                }
                else
                {
                    // we are aiming at location
                    Echo("Aiming");
                    //			DoRotate(yawangle, "Yaw");
                    powerDownThrusters(thrustAllList);

                }
            }

        }


    }
}