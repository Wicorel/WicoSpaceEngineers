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
 * NEED: 
 * Handle non-rocket modes
 * calculate fastest travel direction and use that.
 * use designated direction and block for movement.
 * 
 */
void doTravelMovement(Vector3D vTargetLocation, float arrivalDistance, int arrivalState, int colDetectState)
{
	Echo("dTM:" + arrivalState);
	//		Vector3D vTargetLocation = vHome;// gpsCenter.GetPosition();
//    gpsCenter.CubeGrid.
	Vector3D vVec = vTargetLocation - ((IMyShipController)gpsCenter).CenterOfMass;
//	Vector3D vVec = vTargetLocation - gpsCenter.GetPosition();
	//		debugGPSOutput("vTargetLocation", vTargetLocation);
	double distance = vVec.Length();
	Echo("distance=" + niceDoubleMeters(distance));
	Echo("velocity=" + velocityShip.ToString("0.00"));

	if (distance < arrivalDistance)
	{
		ResetMotion();
		current_state = arrivalState;
//		GyroControl.SetRefBlock(dockingConnector);
//		iPushCount = 0;
//		sleepAllSensors();
		return;
	}
	debugGPSOutput("TargetLocation", vTargetLocation);

	List<IMySensorBlock> aSensors = null;
	IMySensorBlock sb;

	double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);

	bool bAimed = false;
	bAimed = GyroMain("forward", vVec, gpsCenter);

	if (bAimed)
	{
		// we are aimed at location
		Echo("Aimed");
		gyrosOff();

		if (sensorsList.Count > 0)
		{
			sb = sensorsList[0];
			float fScanDist = Math.Min(50f, (float)stoppingDistance*1.5f);
//			float fScanDist = Math.Min(50f, (float)distance);
			setSensorShip(sb, 0, 0, 0,0, fScanDist, 0);
		}
		aSensors = activeSensors();
		if (aSensors.Count > 0)
		{
			Echo("Sensor TRIGGER!");
			Echo("Name: " + aSensors[0].LastDetectedEntity.Name);
			Echo("Type: " + aSensors[0].LastDetectedEntity.Type);
			Echo("Relationship: " + aSensors[0].LastDetectedEntity.Relationship);
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
		double dFar = 100;
		double dApproach = 50;
		double dPrecision = 15;
//		if (velocityShip > 10)
			dFar = stoppingDistance * 5;
//		if (velocityShip > 10)
			dApproach = stoppingDistance * 2;
//		if (velocityShip > 10)
			dPrecision = stoppingDistance * 1.1;
		Echo("DFar=" + dFar);
		Echo("dApproach=" + dApproach);
		Echo("dPrecision=" + dPrecision);

		if (distance > dFar)
		{
			Echo("DFAR");
			if (velocityShip < 1)
				powerUpThrusters(thrustForwardList, 100);
			else if (velocityShip < 55)
				powerUpThrusters(thrustForwardList, 25);
			else if (velocityShip <= 85)
				powerUpThrusters(thrustForwardList, 1);
			else
				powerDownThrusters(thrustAllList);
		}
		else if (distance > dApproach)
		{
			Echo("Approach");

			if (velocityShip < 1)
				powerUpThrusters(thrustForwardList, 85);
			else if (velocityShip < 15)
				powerUpThrusters(thrustForwardList, 25);
			else if (velocityShip <= 55)
				powerUpThrusters(thrustForwardList, 1);
			else
				powerDownThrusters(thrustAllList);
		}
		else if (distance > dPrecision)
		{
			Echo("Precision");
			if (velocityShip < 1)
				powerUpThrusters(thrustForwardList, 55);
			else if (velocityShip < 5)
				powerUpThrusters(thrustForwardList, 1);
			//				else if (velocityShip <= 25)
			//					powerUpThrusters(thrustForwardList, 1);
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
		Echo("Aiming");
		powerDownThrusters(thrustAllList);
//		sleepAllSensors();
	}

}
        #endregion

double calculateStoppingDistance(List<IMyTerminalBlock> thrustUpList, double currentV, double dGrav)
        {
            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double hoverthrust = 0;
            hoverthrust = myMass.PhysicalMass * dGrav * 9.810;
            double maxThrust = calculateMaxThrust(thrustUpList);
            double maxDeltaV = (maxThrust - hoverthrust) / myMass.PhysicalMass;
            double secondstozero = currentV / maxDeltaV;
            Echo("secondstozero=" + secondstozero.ToString("0.00"));
            double stoppingM = currentV / 2 * secondstozero;
            Echo("stoppingM=" + stoppingM.ToString("0.00"));
            return stoppingM;
        }

    }
}