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

        Dictionary<string, int> modeCommands = new Dictionary<string, int>();
        string sBanner = "";
        public Program()
        {
            sBanner = OurName + ":" + moduleName + " V" + sVersion + " ";
            Echo(sBanner + "Creator");
            initLogging();
            StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
            doSubModuleTimerTriggers("[WCCM]"); // try to trigger MAIN timer in case it stopped.
                                                //	if (!Me.CustomName.Contains(moduleName))
                                                //		Me.CustomName = "PB " + OurName+ " "+moduleName;
        }

        #region MAIN

        bool init = false;
bool bWasInit = false;
bool bWantFast = false;

bool bWorkingProjector = false;

double velocityShip;//, velocityForward, velocityUp, velocityLeft;


void Main(string sArgument)
{
	Echo(sBanner + tick());
	bWantFast = false;
	//ProfilerGraph();

	bWorkingProjector = false;
    var list = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyProjector>(list, localGridFilter);
    for (int i = 0; i < list.Count; i++)
    {
        if (list[i].IsWorking)
        {
			if (list[i].CustomName.Contains("!WCC") || list[i].CustomData.Contains("!WCC")) continue; // ignore
            Echo("Working local Projector found!");
//            init = false;
//            sInitResults = "";
            bWorkingProjector = true;
        }
    }

    string output = "";

	sPassedArgument = "";
	if (sArgument != "" && sArgument != "timer" || sArgument!="wcct")
	{
		Echo("Arg=" + sArgument);
	}

	double newgridBaseMass = 0;
	IMyTextPanel masstextBlock = getTextBlock("MASS");

	if (anchorPosition != null)
	{
		MyShipMass myMass;
		myMass = ((IMyShipController)anchorPosition).CalculateShipMass();

		StatusLog("clear", masstextBlock);
		StatusLog("BaseMass=" + myMass.BaseMass.ToString(), masstextBlock);
		StatusLog("TotalMass=" + myMass.TotalMass.ToString(), masstextBlock);
		StatusLog("Physicalmass=" + myMass.PhysicalMass.ToString(), masstextBlock);
//		Echo("Physicalmass=" + myMass.PhysicalMass.ToString());
		StatusLog("gridBaseMass=" + gridBaseMass.ToString(), masstextBlock);
		newgridBaseMass = myMass.BaseMass;
		if (myMass.BaseMass == 0)
			Echo("No Mass--Station?");
		if (newgridBaseMass != gridBaseMass && gridBaseMass > 0)
		{
			Echo("MASS CHANGE");
			StatusLog(OurName + ":" + moduleName + ":MASS CHANGE", textLongStatus, true);
		}
	}
	else gridBaseMass=newgridBaseMass = 0;
    if (sArgument == "init"  || (Math.Abs(newgridBaseMass-gridBaseMass)>1 && gridBaseMass>0) || (currentInit==0 && calcGridSystemChanged()))
    {
		StatusLog("INIT or GRID/MASS CHANGE!", masstextBlock);

		Echo("Arg init or grid/mass change!");
        sInitResults = "";
		anchorPosition = null;
        init = false;
        currentInit = 0;
		sPassedArgument = "init";
    }
    Log("clear");

    if (!init)
    {
        if (bWorkingProjector)
        {
            Log("Construction in Progress\nTurn off projector to continue");
            StatusLog("Construction in Progress\nTurn off projector to continue", textPanelReport);
        }
		else
			bWantFast = true;
        doInit();
        bWasInit = true;
    }
    else
    {
	    Deserialize();
		sPassedArgument = sArgument; 

		if (bWasInit)
		{
			StatusLog(DateTime.Now.ToString() + " " + sInitResults, textLongStatus, true);
		}
//        Echo(sInitResults);

        Log(craftOperation());
        IMyTerminalBlock anchorOrientation = gpsCenter;
		/*
        if (anchorOrientation != null)
        {
            Matrix mTmp;
            anchorOrientation.Orientation.GetMatrix(out mTmp);
            mTmp *= -1;
            iForward = new Vector3I(mTmp.Forward);
            iUp = new Vector3I(mTmp.Up);
            iLeft = new Vector3I(mTmp.Left);
        }
		*/
		//        Vector3D mLast = vCurrentPos;
		if (gpsCenter != null)
		{
			vCurrentPos = gpsCenter.GetPosition();
			velocityShip = ((IMyShipController)anchorPosition).GetShipSpeed();
		}

		if (gpsCenter is IMyShipController)
//		if (gpsCenter is IMyRemoteControl)
		{
			Vector3D vNG = ((IMyShipController)gpsCenter).GetNaturalGravity();
//			Vector3D vNG = ((IMyRemoteControl)gpsCenter).GetNaturalGravity();
			double dLength = vNG.Length();
			dGravity = dLength / 9.81;

			if (dGravity > 0)
			{
				double elevation = 0;

				((IMyShipController)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
				Echo("Elevation=" + elevation.ToString("0.00"));

				double altitude = 0;
				((IMyShipController)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude);
				Echo("Sea Level=" + altitude.ToString("0.00"));

			}

		}
		else
		{
			dGravity = -1.0;
		}
		
        if (processArguments(sArgument))
            return;


        if(AnyConnectorIsConnected()) output+="Connected";
        else output+="Not Connected";

        if(AnyConnectorIsLocked()) output+="\nLocked";
        else output+=" : Not Locked";
        
        Echo(output);
        Log(output);
		output = "";

        if (bWantFast) Echo("FAST!");

        doCargoCheck();
        Echo("Cargo="+cargopcent.ToString()+"%");
        Echo("Cargo Mult="+cargoMult.ToString());


        batteryCheck(0,false);
		output += "Batteries: #=" + batteryList.Count.ToString();
		if (batteryList.Count > 0 && maxBatteryPower>0)
		{
			output += " : " + (getCurrentBatteryOutput() / maxBatteryPower * 100).ToString("0.00") + "%";
	        output+="\n Storage="+batteryPercentage.ToString()+"%";
		}
		
		Echo(output);
		output = "";

		Echo("Solar: #" + solarList.Count.ToString()+ " "+ currentSolarOutput.ToString("0.00" + "MW"));

		output="Reactors: #" + reactorList.Count.ToString();
		if (reactorList.Count > 0)
		{
			output+=" - " + maxReactorPower.ToString("0.00") + "MW\n";
			float fPer = (float)(getCurrentReactorOutput() / totalMaxPowerOutput * 100);
			output+=" Curr Output=" + getCurrentReactorOutput().ToString("0.00") + "MW"+ " : "+ fPer.ToString("0.00") + "%";
//			Echo("Reactor total usage=" + fPer.ToString("0.00") + "%");
		}
		Echo(output);
		output = "";

		Echo("TotalMaxPower=" + totalMaxPowerOutput.ToString("0.00" + "MW"));

		hydroPercent = tanksFill(iTankHydro);
		oxyPercent = tanksFill(iTankOxygen);
		if (oxyPercent >= 0)
		{
			Echo("O:" + oxyPercent.ToString("000.0%"));
		}
		else Echo("No Oxygen Tanks");

		if (hydroPercent >= 0)
		{
			Echo("H:" + hydroPercent.ToString("000.0%"));
		}
		else Echo("No Hydrogen Tanks");


        if (dGravity >= 0)
        {
            Echo("Grav=" + dGravity.ToString(velocityFormat));
            Log("Planet Gravity " + dGravity.ToString(velocityFormat) + " g");
            Log(progressBar((int)(dGravity / 1.1 * 100)));
        }
        else Log("ERROR: No Remote Control found!");

        doModes();
    }
    Serialize();

	if (anchorPosition == null || SaveFile == null)
	{
		Echo("Cannot use sub-modules; missing controller and/or SaveFile");
	}
	else doSubModuleTimerTriggers();

    if (bWantFast)
        doSubModuleTimerTriggers(sFastTimer);

    bWasInit = false;

    verifyAntenna();

	echoInstructions();

	Echo("Passing:'" + sPassedArgument + "'");

    Echo(craftOperation());

	Echo(sInitResults);
}

#endregion
        void echoInstructions(string sBanner = null)
        {
            float fper = 0;
            fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
            if (sBanner == null) sBanner = "Instructions=";
            Echo(sBanner + (fper * 100).ToString("0.00") + "%");

        }

    }
}