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
        UpdateFrequency ufFast = UpdateFrequency.Update1; // default value for "Fast" for this module

        bool bSubModules = true;
        bool bCraftOperation = true;

        public Program()
        {
            sBanner = OurName + ":" + moduleName + " V" + sVersion + " ";
            Echo(sBanner + "Creator");
            doModuleConstructor();

            initLogging();
            StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
            if(!doSubModuleTimerTriggers(sMainTimer)) // try to trigger MAIN timer in case it stopped.
            {
                // if no main timer, then use UpdateFrequency
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            }
            //	if (!Me.CustomName.Contains(moduleName))
            //		Me.CustomName = "PB " + OurName+ " "+moduleName;
        }

        #region MAIN

        bool init = false;
        bool bWasInit = false;
        bool bWantFast = false;

        bool bWorkingProjector = false;

        double velocityShip;//, velocityForward, velocityUp, velocityLeft;

        double dProjectorCheckWait = 5; //seconds between checks
        double dProjectorCheckLast = -1;

        double dGridCheckWait = 3; //seconds between checks
        double dGridCheckLast = -1;

//        void Main(string sArgument)
        void Main(string sArgument, UpdateType ut)
        {
           Echo(sBanner + tick());
            Echo(ut.ToString());
            bWantFast = false;
            //ProfilerGraph();

            if (dProjectorCheckLast > dProjectorCheckWait)
            {
                dProjectorCheckLast = 0;

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
            }
            else
            {
//                Echo("Delay Projector Check");
                if (dProjectorCheckLast < 0)
                {
                    // first-time init
//                    dProjectorCheckLast = Me.EntityId % dProjectorCheckWait; // randomize initial check
                    dProjectorCheckLast = dProjectorCheckWait+5; // force check
                }
                dProjectorCheckLast += Runtime.TimeSinceLastRun.TotalSeconds;
            }
//            Echo("MainInst1:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

            sPassedArgument = "";
/*
            if (sArgument != "" && sArgument != "timer" || sArgument != "wcct")
            {
                Echo("Arg=" + sArgument);
            }
*/
            double newgridBaseMass = 0;

            if (anchorPosition != null)
            {
                if (dGridCheckLast > dGridCheckWait || !init)
                {
                   Echo("DO Grid Check");
                    dGridCheckLast = 0;

//                    IMyTextPanel masstextBlock = getTextBlock("MASS");
                    MyShipMass myMass;
                    myMass = ((IMyShipController)anchorPosition).CalculateShipMass();

//                    StatusLog("clear", masstextBlock);
//                    StatusLog("BaseMass=" + myMass.BaseMass.ToString(), masstextBlock);
//                    StatusLog("TotalMass=" + myMass.TotalMass.ToString(), masstextBlock);
//                    StatusLog("Physicalmass=" + myMass.PhysicalMass.ToString(), masstextBlock);
                    //		Echo("Physicalmass=" + myMass.PhysicalMass.ToString());
//                    StatusLog("gridBaseMass=" + gridBaseMass.ToString(), masstextBlock);
                    newgridBaseMass = myMass.BaseMass;
                    Echo("New=" + newgridBaseMass + " CurrentM=" + gridBaseMass);
             Echo("MASS:" + currentInit + ":"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                    if (myMass.BaseMass == 0)
                        Echo("No Mass--Station?");
                    if (newgridBaseMass != gridBaseMass && gridBaseMass > 0)
                    {
                        Echo("MASS CHANGE");
                        StatusLog(OurName + ":" + moduleName + ":MASS CHANGE", textLongStatus, true);
                    }
                }
                else
                {
//                    Echo("Delay Grid Check");
                    if (dGridCheckLast < 0)
                    {
                        // first-time init
    //                    dGridCheckLast = Me.EntityId % dGridCheckWait; // randomize initial check
                        dGridCheckLast = dGridCheckWait+5; // force check
                    }
                    dGridCheckLast += Runtime.TimeSinceLastRun.TotalSeconds;
                    newgridBaseMass = gridBaseMass; // assume it's the old mass for now
                }
            }
            else
            {
//                Echo("No anchorPosition to check");
                gridBaseMass = newgridBaseMass = 0;
            }
//            Echo("MainInst1A:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

            if (sArgument == "init" || (Math.Abs(newgridBaseMass - gridBaseMass) > 1 && gridBaseMass > 0 && currentInit==0) || (currentInit == 0 && calcGridSystemChanged()))
            {
                Log("INIT or GRID/MASS CHANGE!");

                Echo("Arg init or grid/mass change!");
                sInitResults = "";
                anchorPosition = null;
                init = false;
                currentInit = 0;
                sPassedArgument = "init";
            }
            Log("clear");

//            Echo("MainInst2:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

            if (!init)
            {
                if (bWorkingProjector)
                {
                    Log("Construction in Progress\nTurn off projector to continue");
                    StatusLog("Construction in Progress\nTurn off projector to continue", textPanelReport);
                }
                else
                {
                    bWantFast = true;
                }
                doInit();
                bWasInit = true;
            }
            else
            {
//            Echo("MainInst2a:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                if(bSubModules) Deserialize();
                sPassedArgument = sArgument;

                if (bWasInit)
                {
                    StatusLog(DateTime.Now.ToString() + " " + sInitResults, textLongStatus, true);
                }
                //        Echo(sInitResults);

//            Echo("MainInst2b:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
//                Log(craftOperation());
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
                }

//            Echo("MainInst2c:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                if (gpsCenter is IMyShipController)
                //		if (gpsCenter is IMyRemoteControl)
                {
//            Echo("MainInst2c1:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                    velocityShip = ((IMyShipController)gpsCenter).GetShipSpeed();
//            Echo("MainInst2c2:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

                    Vector3D vNG = ((IMyShipController)gpsCenter).GetNaturalGravity();
                    //			Vector3D vNG = ((IMyRemoteControl)gpsCenter).GetNaturalGravity();
//            Echo("MainInst2c3:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                    double dLength = vNG.Length();
                    dGravity = dLength / 9.81;
//            Echo("MainInst2c4:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

                    if (dGravity > 0)
                    {
                        double elevation = 0;

                        ((IMyShipController)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        Echo("Elevation=" + elevation.ToString("0.00"));

                        double altitude = 0;
                        ((IMyShipController)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude);
                        Echo("Sea Level=" + altitude.ToString("0.00"));

                    }
//            Echo("MainInst2cX:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

                }
                else
                {
                    dGravity = -1.0;
                }

//              Echo("MainInst3:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
               if (processArguments(sArgument))
                    return;

//               Echo("MainInst3A:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
               moduleDoPreModes();

//              Echo("MainInst3B:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);
                doModes();
            }
            if(bSubModules) Serialize();
//             Echo("MainInst4:"+Runtime.CurrentInstructionCount+ "/"+Runtime.MaxInstructionCount);

//            if ((anchorPosition == null || SaveFile == null ))
            if ((SaveFile == null ))
            {
                if(bSubModules) Echo("Cannot use sub-modules; missing controller and/or SaveFile");
            }
            else doSubModuleTimerTriggers();

            if (bWantFast)
            {
                Echo("FAST!");
                Runtime.UpdateFrequency |= ufFast;
            }
            else
            {
                Runtime.UpdateFrequency &= ~(ufFast);
            }

            bWasInit = false;

//verifyAntenna();

//            Echo("Passing:'" + sPassedArgument + "'");

            if(bCraftOperation) Echo(craftOperation());

            modulePostProcessing();
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