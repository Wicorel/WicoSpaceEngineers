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
#region domodes
void doModes()
{
	Echo("mode=" + iMode.ToString());

	if (AnyConnectorIsConnected() && !((craft_operation & CRAFT_MODE_ORBITAL) > 0))
	{
		Echo("DM:docked");
		setMode(MODE_DOCKED);
	}
	if(dGravity>0 && iMode==MODE_INSPACE)
	{
		setMode(MODE_IDLE);
	}
	if (iMode == MODE_IDLE) doModeIdle();
	else if (iMode == MODE_HOVER) doModeHover();
	else if (iMode == MODE_LAUNCHPREP) doModeLaunchprep();
	//else if (iMode == MODE_INSPACE) doModeInSpace();
	else if (iMode == MODE_LANDED) doModeLanded();
	else if (iMode == MODE_ORBITALLAUNCH) doModeOrbitalLaunch();
	// else if (iMode == MODE_DESCENT) doModeDescent();
}
#endregion

#region ORIBITALMODES

float fMaxMps = 100;
//double dStartingGravity=0;
double dAtmoCrossOver = 7000;

void doModeLanded()
{

}


// states
// 0 = init
// 1 = powered hovering. No connections
// 2 = landing gear locked. 
// 

void doModeHover()
{
	StatusLog("clear", textPanelReport);

	StatusLog(OurName + ":" + moduleName + ":Hover", textPanelReport);
	StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
	double elevation = 0;

	((IMyShipController)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
	StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);

	if (thrustStage1UpList.Count < 1)
	{
		if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
		{
			thrustStage1UpList = thrustForwardList;
			thrustStage1DownList = thrustBackwardList;

			cameraStage1LandingList = cameraBackwardList;
		}
		else
		{
			//Echo("Setting thrustStage1UpList");
			thrustStage1UpList = thrustUpList;
			thrustStage1DownList = thrustDownList;
			cameraStage1LandingList = cameraDownList;
		}
	}

	bool bGearsLocked = anyGearIsLocked();
	bool bConnectorsConnected = AnyConnectorIsConnected();
	bool bConnectorIsLocked = AnyConnectorIsLocked();
	bool bGearsReadyToLock = anyGearReadyToLock();

	if(bGearsLocked)
	{
		if(current_state!=2)
		{
			// gears just became locked
Echo("Force thrusters Off!");
			blockApplyAction(thrustAllList,"OnOff_Off");

			if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
			{
				blockApplyAction(tankList, "Stockpile_On");
				blockApplyAction(gasgenList,"OnOff_On");
			}
			current_state = 2;
		}
	}
	else
	{
		if (current_state != 1)
		{
			Echo("Force thrusters ON!");
			blockApplyAction(thrustAllList,"OnOff_On");
			if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
			{
				blockApplyAction(tankList, "Stockpile_Off");
//				blockApplyAction(gasgenList,"OnOff_On");
			}

			current_state = 1;
		}

	}
	if(doCameraScan(cameraStage1LandingList, elevation * 2)) // scan down 2x current alt
	{
		// we are able to do a scan
		if(!lastDetectedInfo.IsEmpty())
		{ // we got something
			double distance = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.HitPosition.Value);
//			if (distance < elevation)
			{ // try to land on found thing below us.
				Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
		if(!bGearsLocked) StatusLog("Hovering above: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below",textPanelReport); 

			}
		}
	}

	if (bGearsLocked)
	{
		StatusLog("Landing Gear(s) LOCKED!", textPanelReport);
		// we can turn off thrusters.. but that's about it..
		// stay in 'hover' iMode

	}
	else if(bGearsReadyToLock)
	{
		StatusLog("Landing Gear(s) Ready to lock.", textPanelReport);
	}
	if (bConnectorsConnected)
	{
		//prepareForSupported();
		StatusLog("Connector connected!\n   auto-prepare for launch", textPanelReport);
		setMode(MODE_LAUNCHPREP);
	}
	else
	{
		if (!bGearsLocked)
		{
//			blockApplyAction(thrustAllList, "OnOff_On");
//			if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_On");
		}
		ConnectAnyConnectors(false, "OnOff_On");
	}

	if (bConnectorIsLocked)
	{
		StatusLog("Connector Locked!", textPanelReport);
	}

	if (bConnectorIsLocked || bGearsLocked)
	{
		Echo("Stable");
		gyrosOff();
	}
	else
	{
		if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
			StatusLog("Wico Gravity Alignment OFF", textPanelReport);
		else
		{
			StatusLog("Gravity Alignment Operational", textPanelReport);

			string sOrientation = "";
			if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
				sOrientation = "rocket";

			GyroMain(sOrientation);
			bWantFast = true;
		}
	}

	//	StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

	//	batteryCheck(0, false);//,textPanelReport);
//	if (bValidExtraInfo)
	{
		if (batteryPercentage >= 0) StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
		if (oxyPercent >= 0)
		{
			StatusLog("O2:" + progressBar(oxyPercent*100), textPanelReport);
			//Echo("O:" + oxyPercent.ToString("000.0%"));
		}
		else Echo("No Oxygen Tanks");

		if (hydroPercent >= 0)
		{
			StatusLog("Hyd:" + progressBar(hydroPercent*100), textPanelReport);
	Echo("H:" + hydroPercent.ToString("0.0")+"%");
		}
		else Echo("No Hydrogen Tanks");

//		if (iOxygenTanks > 0) StatusLog("O2:" + progressBar(tanksFill(iTankOxygen)), textPanelReport);
//		if (iHydroTanks > 0) StatusLog("Hyd:" + progressBar(tanksFill(iTankHydro)), textPanelReport);
	}

	if (dGravity <= 0)
	{
		setMode(MODE_INSPACE);
		gyrosOff();
		StatusLog("clear", textPanelReport);
	}

}

void doModeLaunchprep()
{
	IMyTextPanel textBlock = textPanelReport;

	StatusLog("clear", textBlock);

	StatusLog(OurName + ":" + moduleName + ":Launch Prep", textBlock);
	StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
//	StatusLog("Calculated Simspeed=" + fSimSpeed.ToString(velocityFormat), textBlock);
//	StatusLog("->If Calculated Simspeed does not match \n actual, use SetSimSpeed command to \n set the actual current simspeed.\n", textBlock);

	if (dGravity <= 0)
	{
		if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
		else
		{
			setMode(MODE_INSPACE);
			gyrosOff();
			StatusLog("clear", textPanelReport);
		}
		return;
	}
		

	if (anyGearIsLocked())
	{
		StatusLog("Landing Gear(s) LOCKED!", textBlock);
	}
	if (AnyConnectorIsConnected())
	{
		StatusLog("Connector connected!\n   auto-prepare for launch", textBlock);
	}
	else
	{
		if (!anyGearIsLocked())
		{
			if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_Off");
			setMode(MODE_HOVER);
		}
		ConnectAnyConnectors(false, "OnOff_On");
	}

	if (AnyConnectorIsLocked())
	{
		StatusLog("Connector Locked!", textBlock);
	}

	if (AnyConnectorIsLocked() || anyGearIsLocked())
	{
		Echo("Stable");
	}
	else
	{
		//prepareForSolo();
		setMode(MODE_HOVER);
		return;
	}

	if (AnyConnectorIsConnected())
	{
		if (current_state == 0)
		{
			blockApplyAction(thrustAllList,"OnOff_Off");
			if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_On");

			current_state = 1;
		}
		else if (current_state == 1)
		{
//			if ((craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0)
				current_state = 4; // skip battery checks
//			else
//			if (!batteryCheck(30, true))
//				current_state = 2;
		}
		else if (current_state == 2)
		{
//			if (!batteryCheck(80, true))
				current_state = 3;
		}
		else if (current_state == 3)
		{
//			if (!batteryCheck(100, true))
				current_state = 1;
		}
	}
//	else batteryCheck(0, true); //,textBlock);

//	StatusLog("C:" + progressBar(cargopcent), textBlock);

//	if (bValidExtraInfo)
	{
		StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
		if (oxyPercent >= 0)
		{
			StatusLog("O2:" + progressBar(oxyPercent*100), textPanelReport);
			//Echo("O:" + oxyPercent.ToString("000.0%"));
		}
		else Echo("No Oxygen Tanks");

		if (hydroPercent >= 0)
		{
			StatusLog("Hyd:" + progressBar(hydroPercent*100), textPanelReport);
	Echo("H:" + hydroPercent.ToString("000.0%"));
		}
		else Echo("No Hydrogen Tanks");

//		if (iOxygenTanks > 0) StatusLog("O2:" + progressBar(tanksFill(iTankOxygen)), textPanelReport);
//		if (iHydroTanks > 0) StatusLog("Hyd:" + progressBar(tanksFill(iTankHydro)), textPanelReport);
	}


/*
	if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
	{
		thrustStage1UpList = thrustForwardList;
		thrustStage1DownList = thrustBackwardList;
	}
	else
	{
		thrustStage1UpList = thrustUpList;
		thrustStage1DownList = thrustDownList;
	}
*/
}

// MODE_ORBITAL_LAUNCH states
// 0 init. prepare for solo
// check all connections. hold launch until disconnected
// 
// 1 capture location and init thrust settings.
// 2 initial thrust. trying to move
// 3 initial lift-off achieved. accelerate
// 4 have reached max; maintain
// 5 wait for release..

double dLastVelocityShip = -1;

List<IMyTerminalBlock> thrustStage1UpList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> thrustStage1DownList = new List<IMyTerminalBlock>();

List<IMyTerminalBlock>cameraStage1LandingList=new List<IMyTerminalBlock>(); 


float fAtmoPower = 0;
float fHydroPower = 0;
float fIonPower = 0;

void doModeOrbitalLaunch()
{
	int next_state = current_state;

	IMyTextPanel textBlock = textPanelReport;

	StatusLog("clear", textBlock);

	StatusLog(OurName + ":" + moduleName + ":Oribital Launch", textBlock);
	StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);
	StatusLog(velocityShip.ToString(velocityFormat) + " m/s", textBlock);
	Echo("Orbital Launch. State=" + current_state.ToString());
	if (thrustStage1UpList.Count < 1)
	{
		if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
		{
			thrustStage1UpList = thrustForwardList;
			thrustStage1DownList = thrustBackwardList;

			cameraStage1LandingList = cameraBackwardList;
		}
		else
		{
			//Echo("Setting thrustStage1UpList");
			thrustStage1UpList = thrustUpList;
			thrustStage1DownList = thrustDownList;
			cameraStage1LandingList = cameraDownList;
		}
	}

	if (current_state == 0)
	{
//		dtStartShip = DateTime.Now;
		dLastVelocityShip = 0;
		if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_Off");

		if (AnyConnectorIsConnected() || AnyConnectorIsLocked() || anyGearIsLocked())
		{
			// launch from connected
			vLaunch1 = gpsCenter.GetPosition();
			bValidLaunch1 = true;
			bValidHome = false; // forget any 'home' waypoint.

			ConnectAnyConnectors(false, "OnOff_Off");
//			blockApplyAction(gearList, "OnOff_Off"); // in case autolock is set.
			blockApplyAction(gearList, "Unlock");
			current_state = 5;
			return;
		}
		else
		{
			// launch from hover mode
			bValidLaunch1 = false;
			vHome = gpsCenter.GetPosition();
			bValidHome = true;

			// assume we are hovering; do FULL POWER launch.
			fAtmoPower = 0;
			fHydroPower = 0;
			fIonPower = 0;

			if (ionThrustCount > 0) fIonPower = 75;
			if (hydroThrustCount > 0)
			{
				for (int i = 0; i < thrustStage1UpList.Count; i++)
				{
					if (thrusterType(thrustStage1UpList[i]) == thrusthydro)
						if (thrustStage1UpList[i].IsWorking)
						{
							fHydroPower = 100;
							break;
						}
				}
			}
			if (atmoThrustCount > 0) fAtmoPower = 100;

			powerDownThrusters(thrustStage1DownList, thrustAll, true);

			current_state = 3;
			return;
		}
	}
	if (current_state == 5)
	{
		StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + current_state.ToString(), textLongStatus, true);
		if (AnyConnectorIsConnected() || AnyConnectorIsLocked() || anyGearIsLocked())
		{
			StatusLog("Awaiting release", textBlock);
		}
		else
		{
			StatusLog(DateTime.Now.ToString() + " " + OurName + ":Saved Position", textLongStatus, true);

			// we launched from connected. Save position
			vLaunch1 = gpsCenter.GetPosition();
			bValidLaunch1 = true;
			bValidHome = false; // forget any 'home' waypoint.

			next_state = 1;
		}

		current_state = next_state;
	}
	Vector3D vTarget = new Vector3D(0, 0, 0);
	bool bValidTarget = false;
	if (bValidLaunch1)
	{
		bValidTarget = true;
		vTarget = vLaunch1;
	}
	else if (bValidHome)
	{
		bValidTarget = true;
		vTarget = vHome;
	}

	double alt = 0;
	if (bValidTarget)
	{
		alt = (vCurrentPos - vTarget).Length();
		StatusLog("Distance: " + alt.ToString("N0") + " Meters", textBlock);

		double elevation = 0;

		((IMyShipController)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
		StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textBlock);


	}
	if (current_state == 1)
	{

		calculateHoverThrust(thrustStage1UpList, out fAtmoPower, out fHydroPower, out fIonPower);
		powerDownThrusters(thrustStage1DownList, thrustAll, true);
		current_state = 2;
		return;
	}

	double deltaV = velocityShip - dLastVelocityShip;
	double expectedV = deltaV * 5 + velocityShip;

	if (current_state == 2)
	{ // trying to move
		StatusLog("Attempting Lift-off", textBlock);

		// NOTE: need to NOT turn off atmo if we get all the way into using ions for this state.. and others?
		if (velocityShip < 3f)
		// if(velocityShip<1f)
		{
			increasePower(dGravity, alt);
			increasePower(dGravity, alt);
		}
		else
		{
			next_state = 3; // we have started to lift off.
			dLastVelocityShip = 0;
		}
	}
	else
	if (alt > 100)
	{
		if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
			StatusLog("Wico Gravity Alignment OFF", textBlock);
		else
		{

			StatusLog("Gravity Alignment Operational", textBlock);
			string sOrientation = "";
			if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
				sOrientation = "rocket";

			GyroMain(sOrientation);
		}
	}


	if (current_state == 3)
	{ // accelerate to max speed
		StatusLog("Accelerating to max speed )" + fMaxMps.ToString("0") + ")", textBlock);
		if (dLastVelocityShip < velocityShip)
		{ // we are accelerationg

			if (expectedV < (fMaxMps / 2))
			// if(velocityShip<(fMaxMps/2))
			{
				decreasePower(dGravity, alt); // take away from lowest provider
				increasePower(dGravity, alt);// add it back
			}
			if (expectedV < (fMaxMps / 5)) // add even more.
				increasePower(dGravity, alt);

			if (velocityShip > (fMaxMps - 5))
				next_state = 4;
		}
		else
		{
			increasePower(dGravity, alt);//
			increasePower(dGravity, alt);// and add some more
		}
	}

	if (current_state == 4)
	{ // maintain max speed
		StatusLog("Maintain max speed", textBlock);
		StatusLog("Expectedv=" + expectedV.ToString("0.00") + " max=" + fMaxMps.ToString("0.00"), textBlock);
		Echo("Expectedv=" + expectedV.ToString("0.00") + " max=" + fMaxMps.ToString("0.00"));
		double dMin = (fMaxMps - fMaxMps * .05);
		if (expectedV > dMin)
		// if(velocityShip>(fMaxMps-5))
		{
			calculateHoverThrust(thrustStage1UpList, out fAtmoPower, out fHydroPower, out fIonPower);
			Echo("hover thrust:" + fAtmoPower.ToString() + ":" + fHydroPower.ToString() + ":" + fIonPower.ToString());

			if (fAtmoPower < 1.001)
				fAtmoPower = 0;
			if (fHydroPower < 1.001)
				fHydroPower = 0;
			if (fIonPower < 1.001)
				fIonPower = 0;

		}
		else if (expectedV < (fMaxMps - 10))
		{
			decreasePower(dGravity, alt); // take away from lowest provider
			increasePower(dGravity, alt);// add it back
			increasePower(dGravity, alt);// and add some more
		}
		if (velocityShip < (fMaxMps / 2))
			next_state = 2;

		ConnectAnyConnectors(false, "OnOff_On");
		blockApplyAction(gearList, "OnOff_On");
	}
	dLastVelocityShip = velocityShip;

	StatusLog("", textBlock);

//	if (bValidExtraInfo)
		StatusLog("Car:" + progressBar(cargopcent), textPanelReport);

//	batteryCheck(0, false);//,textPanelReport);
//	if (bValidExtraInfo)
		StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
	if (iOxygenTanks > 0) StatusLog("O2:" + progressBar(tanksFill(iTankOxygen)), textPanelReport);
	if (iHydroTanks > 0) StatusLog("Hyd:" + progressBar(tanksFill(iTankHydro)), textPanelReport);

	StatusLog("", textBlock);
	if (dGravity < 0.01)
	{
		powerDownThrusters(thrustAllList);
		gyrosOff();
		startNavCommand("!;V");
		setMode(MODE_INSPACE);
		StatusLog("clear", textPanelReport);

		return;
	}

	int iPowered = 0;

	if (fIonPower > 0)
	{
		powerDownThrusters(thrustAllList, thrustatmo, true);
		powerDownThrusters(thrustAllList, thrustion);
		iPowered = powerUpThrusters(thrustStage1UpList, fIonPower, thrustion);
		//Echo("Powered "+ iPowered.ToString()+ " Ion Thrusters");
	}
	else
	{
		powerDownThrusters(thrustAllList, thrustion, true);
		powerDownThrusters(thrustStage1UpList, thrustion);
	}

	if (fHydroPower > 0)
	{
		powerUpThrusters(thrustStage1UpList, fHydroPower, thrusthydro);
	}
	else
	{ // important not to let them provide dampener power..
		powerDownThrusters(thrustStage1DownList, thrusthydro, true);
		powerDownThrusters(thrustStage1UpList, thrusthydro, true);
	}
	if (fAtmoPower > 0)
	{
		powerUpThrusters(thrustStage1UpList, fAtmoPower, thrustatmo);
	}
	else
	{

		closeDoors(outterairlockDoorList);

		// iPowered=powerDownThrusters(thrustStage1UpList,thrustatmo,true);
		iPowered = powerDownThrusters(thrustAllList, thrustatmo, true);
		//Echo("Powered DOWN "+ iPowered.ToString()+ " Atmo Thrusters");

	}

	{
		powerDownThrusters(thrustStage1DownList, thrustAll, true);
	}

	if (ionThrustCount > 0) StatusLog("ION:" + progressBar(fIonPower), textBlock);
	if (hydroThrustCount > 0) StatusLog("HYD:" + progressBar(fHydroPower), textBlock);
	if (atmoThrustCount > 0) StatusLog("ATM:" + progressBar(fAtmoPower), textBlock);

	current_state = next_state;

}

void increasePower(double dGravity, double alt)
{
	if (dGravity > .5 && alt < dAtmoCrossOver)
	{
		if (fAtmoPower < 100 && atmoThrustCount > 0)
			fAtmoPower += 5;
		else if (fHydroPower == 0 && fIonPower > 0)
		{ // we are using ion already...
			if (fIonPower < 100 && ionThrustCount > 0)
				fIonPower += 5;
			else
				fHydroPower += 5;
		}
		else if (fIonPower < 100 && ionThrustCount > 0)
			fIonPower += 5;
		else if (fHydroPower < 100 && hydroThrustCount > 0)
		{
			// fAtmoPower=100;
			fHydroPower += 5;
		}
		else // no power left to give, captain!
		{
			StatusLog("Not Enough Thrust!", textPanelReport);
			Echo("Not Enough Thrust!");
		}
	}
	else if (dGravity > .5 || alt > dAtmoCrossOver)
	{
		if (fIonPower < fAtmoPower && atmoThrustCount > 0 && ionThrustCount > 0)
		{
			float f = fIonPower;
			fIonPower = fAtmoPower;
			fAtmoPower = f;
		}
		if (fIonPower < 100 && ionThrustCount > 0)
			fIonPower += 10;
		else if (fHydroPower < 100 && hydroThrustCount > 0)
		{
			fHydroPower += 5;
		}
		else if (alt < dAtmoCrossOver && fAtmoPower < 100 && atmoThrustCount > 0)
			fAtmoPower += 10;
		else if (alt > dAtmoCrossOver && atmoThrustCount > 0)
			fAtmoPower -= 5; // we may be sucking power from ion
		else // no power left to give, captain!
		{
			StatusLog("Not Enough Thrust!", textPanelReport);
			Echo("Not Enough Thrust!");
		}
	}
	else if (dGravity > .01)
	{
		if (fIonPower < 100 && ionThrustCount > 0)
			fIonPower += 15;
		else if (fHydroPower < 100 && hydroThrustCount > 0)
		{
			fHydroPower += 5;
		}
		else if (alt < dAtmoCrossOver && fAtmoPower < 100 && atmoThrustCount > 0)
			fAtmoPower += 10;
		else // no power left to give, captain!
		{
			StatusLog("Not Enough Thrust!", textPanelReport);
			Echo("Not Enough Thrust!");
		}

	}

	if (fIonPower > 100) fIonPower = 100;
	if (fAtmoPower > 100) fAtmoPower = 100;
	if (fAtmoPower < 0) fAtmoPower = 0;
	if (fHydroPower > 100) fHydroPower = 100;

}

void decreasePower(double dGravity, double alt)
{
	if (dGravity > .85 && alt < dAtmoCrossOver)
	{
		if (fHydroPower > 0)
		{
			fHydroPower -= 5;
		}
		else if (fIonPower > 0)
			fIonPower -= 5;
		else if (fAtmoPower > 10)
			fAtmoPower -= 5;
	}
	else if (dGravity > .3)
	{
		if (fAtmoPower > 0)
			fAtmoPower -= 10;
		else if (fHydroPower > 0)
		{
			fHydroPower -= 5;
		}
		else if (fIonPower > 10)
			fIonPower -= 5;

	}
	else if (dGravity > .01)
	{
		if (fAtmoPower > 0)
			fAtmoPower -= 5;
		else if (fHydroPower > 0)
		{
			fHydroPower -= 5;
		}
		else if (fIonPower > 10)
			fIonPower -= 5;
	}

	if (fIonPower < 0) fIonPower = 0;
	if (fAtmoPower < 0) fAtmoPower = 0;
	if (fHydroPower < 0) fHydroPower = 0;

}

#endregion


        #region modeidle
void ResetToIdle()
{
	StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
	ResetMotion();
	if (navCommand != null)
		if (!(navCommand is IMyTextPanel)) navCommand.CustomName="NAV: C Wico Craft";
	if (navStatus != null) navStatus.CustomName=sNavStatus + " Control Reset";
	// bValidPlayerPosition=false;
	setMode(MODE_IDLE);
	if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
}
void doModeIdle()
{
	//StatusLog("clear",textPanelReport);

	StatusLog(moduleName + " Manual Control", textPanelReport);
	if ((craft_operation & CRAFT_MODE_ORBITAL) > 0)
	{
		if (dGravity <= 0)
		{
			if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
			else
			{
				setMode(MODE_INSPACE);
				gyrosOff();
			}
		}
		else setMode(MODE_HOVER);
		// else setMode(MODE_LAUNCHPREP);
	}
	/*
	* else
	*/
	/*
	{

	if (bWantAutoGyro)
	GyroMain("");
	}
	*/
}
 #endregion
        void ResetMotion(bool bNoDrills = false)
        {
            Echo("RESETMOTION!");
            if (navEnable != null) blockApplyAction(navEnable, "OnOff_Off"); //navEnable.ApplyAction("OnOff_Off");
            powerDownThrusters(thrustAllList);
            blockApplyAction(sRemoteControl, "AutoPilot_Off");
        }

    }
}