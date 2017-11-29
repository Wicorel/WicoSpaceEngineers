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

        #region travelmovement
        /*
         * TODO:
         * Handle non-rocket modes
         * calculate fastest travel direction and use that.
         * use designated direction and block for movement.
         * 
         * add maxspeed parameter
         * 
         */

        bool dTMDebug = true;

        IMyShipController tmShipController = null;
        double tmMaxSpeed = 85;

        List<IMyTerminalBlock> thrustTmBackwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmForwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmRightList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustTmDownList = new List<IMyTerminalBlock>();

        IMySensorBlock tmSB=null;

        bool btmApproach = false;
        bool btmPrecision = false;
        bool btmClose = false;
        double dtmFar = 100;
        double dtmApproach = 50;
        double dtmPrecision = 15;
        double dtmFarSpeed = 85;
        double dtmApproachSpeed = 85 * .75;
        double dtmPrecisionSpeed = 5;
        
        void InitDoTravelMovement(Vector3D vTargetLocation, double maxSpeed, IMyTerminalBlock myShipController, int iThrustType=thrustAll)
        {

            tmMaxSpeed = maxSpeed;
            tmShipController =  myShipController as IMyShipController;
            Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;
            double distance = vVec.Length();

            thrustersInit(tmShipController, ref thrustTmForwardList, ref  thrustTmBackwardList,
            ref thrustTmDownList, ref thrustTmUpList,
            ref thrustTmLeftList, ref thrustTmRightList,iThrustType);
            sleepAllSensors();
            if (sensorsList.Count > 0)
            {
                tmSB = sensorsList[0];
                tmSB.DetectAsteroids = true;
                tmSB.DetectEnemy = true;
                tmSB.DetectLargeShips = true;
                tmSB.DetectSmallShips = true;
                tmSB.DetectStations = true;
            }
            btmApproach = false; // we have reached approach range
            btmPrecision = false; // we have reached precision range
            btmClose = false; // we have reached close range

            double optimalV = CalculateOptimalSpeed( thrustTmBackwardList, distance);
            if (optimalV < tmMaxSpeed)
                tmMaxSpeed = optimalV;
            sInitResults += "\nDistance="+niceDoubleMeters(distance)+" OptimalV=" + optimalV;

            dtmFarSpeed = tmMaxSpeed;
            dtmApproachSpeed = tmMaxSpeed * 0.75;
            dtmPrecisionSpeed = 5;

            dtmFar = calculateStoppingDistance(thrustTmBackwardList, dtmFarSpeed, 0); // calculate maximum stopping distance at full speed
            dtmApproach = calculateStoppingDistance(thrustTmBackwardList, dtmApproachSpeed, 0);
            dtmPrecision =calculateStoppingDistance(thrustTmBackwardList, dtmPrecisionSpeed, 0);
       }


        void doTravelMovement(Vector3D vTargetLocation, float arrivalDistance, int arrivalState, int colDetectState)
        {
            if(dTMDebug) Echo("dTM:" + arrivalState);
            //		Vector3D vTargetLocation = vHome;// gpsCenter.GetPosition();
            //    gpsCenter.CubeGrid.
            if (tmShipController == null)
            {
                InitDoTravelMovement(vTargetLocation,100, gpsCenter);
            }

            Vector3D vVec = vTargetLocation - tmShipController.CenterOfMass;
            //	Vector3D vVec = vTargetLocation - gpsCenter.GetPosition();
            //		debugGPSOutput("vTargetLocation", vTargetLocation);
            double distance = vVec.Length();

            if(dTMDebug) Echo("dTM:distance=" + niceDoubleMeters(distance));
            if(dTMDebug) Echo("dTM:velocity=" + velocityShip.ToString("0.00"));
            if(dTMDebug) Echo("dTM:tmMaxSpeed=" + tmMaxSpeed.ToString("0.00"));

            if (distance < arrivalDistance)
            {
                ResetMotion();
                current_state = arrivalState;
                tmShipController = null;
                //		GyroControl.SetRefBlock(dockingConnector);
                //		iPushCount = 0;
                //		sleepAllSensors();
                return;
            }
            debugGPSOutput("TargetLocation", vTargetLocation);

            List<IMySensorBlock> aSensors = null;
//            IMySensorBlock sb;

            double stoppingDistance = calculateStoppingDistance(thrustTmBackwardList, velocityShip, 0);
//            Echo("StoppingD=" + niceDoubleMeters(stoppingDistance));

            bool bAimed = false;
            bAimed = GyroMain("forward", vVec, gpsCenter);

            tmShipController.DampenersOverride = true;

            if((distance - stoppingDistance) < arrivalDistance)
            { // we are within stopping distance, so start slowing
                ResetMotion();
                return;
            }

            if (bAimed)
            {
                // we are aimed at location
               if(dTMDebug)  Echo("Aimed");
                gyrosOff();

                if (sensorsList.Count > 0)
                {
                    sleepAllSensors();
//                    sb = sensorsList[0];
                    float fScanDist = Math.Min(50f, (float)stoppingDistance * 1.5f);
                    //			float fScanDist = Math.Min(50f, (float)distance);
                    setSensorShip(tmSB, 0, 0, 0, 0, fScanDist, 0);
                    /*
                     * need to do this ONCE.. so need travel init?
                     * should also sleep other sensors..
                    sb.DetectAsteroids = true;
                    sb.DetectEnemy = true;
                    */
                }
                aSensors = activeSensors();
                if (aSensors.Count > 0)
                {
                   if(dTMDebug)  Echo("Sensor TRIGGER!");
                    if(dTMDebug) Echo("Name: " + aSensors[0].LastDetectedEntity.Name);
                    if(dTMDebug) Echo("Type: " + aSensors[0].LastDetectedEntity.Type);
                    if(dTMDebug) Echo("Relationship: " + aSensors[0].LastDetectedEntity.Relationship);
                    lastDetectedInfo = aSensors[0].LastDetectedEntity;
                    // something in way.
                    current_state = colDetectState;
                    bWantFast = true;
                    ResetMotion();
                    //			sleepAllSensors();
                    return;
                }
                else lastDetectedInfo = new MyDetectedEntityInfo();

                if (doCameraScan(cameraForwardList, Math.Min(1000, stoppingDistance * 2)))
                {
                    if (!lastDetectedInfo.IsEmpty())
                    {
                        // something in way.
                        current_state = colDetectState;
                        bWantFast = true;
                        ResetMotion();
                        return;
                    }
                }
                // else "Close"
                /*
                if (velocityShip > 10)
                    dtmFar = stoppingDistance * 5;
                if (velocityShip > 10)
                    dtmApproach = stoppingDistance * 2;
                if (velocityShip > 10)
                    dtmPrecision = stoppingDistance * 1.1;
                    */

                if(dTMDebug) Echo("dtmFar=" + niceDoubleMeters(dtmFar));
                if(dTMDebug) Echo("dtmApproach=" + niceDoubleMeters(dtmApproach));
                if(dTMDebug) Echo("dtmPrecision=" + niceDoubleMeters(dtmPrecision));

                if (distance > dtmFar &&!btmApproach)
                {
                    if(dTMDebug) Echo("dtmFar");
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 100);
                    else if (velocityShip < dtmFarSpeed * .75)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip <= dtmFarSpeed * .95)
                    {
                        powerUpThrusters(thrustTmForwardList, 1f);
                    }
                    else if (velocityShip >= dtmFarSpeed * 1.01)
                    {
                        powerDownThrusters(thrustAllList);
                    }
                    else // sweet spot
                    {
                        powerDownThrusters(thrustAllList);
                        powerDownThrusters(thrustTmBackwardList, thrustAll, true);
//                        tmShipController.DampenersOverride = false;
                    }
                }
                else if (distance > dtmApproach && !btmPrecision)
                {
                    if(dTMDebug) Echo("Approach");
                    btmApproach = true;

                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 85f);
                    else if (velocityShip < 15)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip <= dtmApproachSpeed)
                        powerUpThrusters(thrustTmForwardList, 1f);
                    else
                        powerDownThrusters(thrustAllList);
                }
                else if (distance > dtmPrecision && !btmClose)
                {
                    if(dTMDebug) Echo("Precision");
 //                   btmPrecision = true;
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 55f);
                    else if (velocityShip < dtmPrecisionSpeed)
                        powerUpThrusters(thrustTmForwardList, 1f);
                    //				else if (velocityShip <= 25)
                    //					powerUpThrusters(thrustForwardList, 1);
                    else
                        powerDownThrusters(thrustAllList);
                }
                else
                {
                    if(dTMDebug) Echo("Close");
                    btmClose = true;
                    if (velocityShip < 1)
                        powerUpThrusters(thrustTmForwardList, 25f);
                    else if (velocityShip < 5)
                        powerUpThrusters(thrustTmForwardList, 5f);
                    //				else if (velocityShip <= 15)
                    //					powerUpThrusters(thrustForwardList, 1);
                    else
                        powerDownThrusters(thrustAllList);
                }
            }
            else
            {
                if(dTMDebug) Echo("Aiming");
                //                tmShipController.DampenersOverride = false;
                tmShipController.DampenersOverride = true;
                if (velocityShip < 5)
                {
                    powerDownThrusters(thrustAllList);
                }
                else
                {
                    powerDownThrusters(thrustTmBackwardList, thrustAll, true);
                }
                //		sleepAllSensors();
            }

        }
        #endregion

        double CalculateOptimalSpeed(List<IMyTerminalBlock> thrustUpList, double distance)
        {
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double maxThrust = calculateMaxThrust(thrustUpList);
            double maxDeltaV = maxThrust / myMass.PhysicalMass;
            // Magic..
            double optimalV = ((distance * .75) / 2) / (maxDeltaV);

            return optimalV;
        }
        double calculateStoppingDistance(List<IMyTerminalBlock> thrustUpList, double currentV, double dGrav=0)
        {
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double hoverthrust = 0;
            hoverthrust = myMass.PhysicalMass * dGrav * 9.810;
            double maxThrust = calculateMaxThrust(thrustUpList);
            double maxDeltaV = (maxThrust - hoverthrust) / myMass.PhysicalMass;
            double secondstozero = currentV / maxDeltaV;
//            Echo("secondstozero=" + secondstozero.ToString("0.00"));
            // velocity will drop as we brake. at half way we should be at half speed
            double stoppingM = currentV / 2 * secondstozero; 
//            Echo("stoppingM=" + stoppingM.ToString("0.00"));
            return stoppingM;
        }

    }
}