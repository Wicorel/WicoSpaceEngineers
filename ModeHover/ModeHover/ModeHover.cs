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

    }
}