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
        UpdateFrequency ufFast = UpdateFrequency.Once; // default value for "Fast" for this module

        /// <summary>
        /// Do we support submodules?
        /// </summary>
        bool bSubModules = true;

        /// <summary>
        /// Display the init text as craft Operation
        /// </summary>
        bool bCraftOperation = true;
        /// <summary>
        /// Dump the UpdateType. Settable in CustomData.  This is default.
        /// </summary>
        bool bDebugUpdate = false;

        /// <summary>
        /// We are a MAIN module, not sub-module
        /// </summary>
        bool bSubModule = false;

        double dSubmoduleTriggerWait = 1; //seconds between triggers
        double dSubmoduleTriggerLast = -1;

        float fMaxWorldMps = 100;
        string sWorldSection = "WORLD";
        void WorldInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sWorldSection, "MaxWorldMps", ref fMaxWorldMps, true);
        }

        string sMainSection = "WICOCRAFT";
        public Program()
        {
            doModuleConstructor();

            INIHolder iniCustomData = new INIHolder(this, Me.CustomData);

            iniCustomData.GetValue(sMainSection, "EchoOn", ref bEchoOn, true);
            iniCustomData.GetValue(sMainSection, "DebugUpdate", ref bDebugUpdate, true);
            iniCustomData.GetValue(sMainSection, "SubModules", ref bSubModules, true);
            iniCustomData.GetValue(sMainSection, "SubmoduleTriggerWait", ref dSubmoduleTriggerWait, true);

            _oldEcho = Echo;
            Echo = MyEcho;

            WorldInitCustomData(iniCustomData);
            GridsInitCustomData(iniCustomData);
            LoggingInitCustomData(iniCustomData);
            TimersInitCustomData(iniCustomData);

            ModuleInitCustomData(iniCustomData);
            if (iniCustomData.IsDirty)
            {
                Me.CustomData = iniCustomData.GenerateINI(true);
            }

            sBanner = OurName + ":" + moduleName + " V" + sVersion + " ";
            _oldEcho(sBanner + "Creator");

            initLogging();
            StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
            if(!doSubModuleTimerTriggers(sMainTimer)) // try to trigger MAIN timer in case it stopped.
            {
                // if no main timer, then use UpdateFrequency
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            }
            //	if (!Me.CustomName.Contains(moduleName))
            //		Me.CustomName = "PB " + OurName+ " "+moduleName;
            if (!Me.Enabled)
            {
                Echo("I am turned OFF!");
            }
        }

        bool bEchoOn = true;

        Action<string> _oldEcho;
        void MyEcho(string output)
        {
            // Do whatever you'd want with the output here
            if (bEchoOn) _oldEcho(output);
        }


        #region MAIN

        bool init = false;
        bool bWasInit = false;
        bool bWantFast = false;
        bool bWantMedium = false;

        bool bWorkingProjector = false;

        double velocityShip;//, velocityForward, velocityUp, velocityLeft;

        double dProjectorCheckWait = 5; //seconds between checks
        double dProjectorCheckLast = -1;

        double dGridCheckWait = 3; //seconds between checks
        double dGridCheckLast = -1;

        double dGravity = -2;

        //        void Main(string sArgument)
        void Main(string sArgument, UpdateType ut)
        {
           Echo(sBanner + tick());
            if(bDebugUpdate)  Echo(ut.ToString());

            bWantFast = false;
            bWantMedium = false;
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

            sPassedArgument = "";
            double newgridBaseMass = 0;

            if (shipOrientationBlock is IMyShipController)
            {
                if (dGridCheckLast > dGridCheckWait || !init)
                {
                   Echo("DO Grid Check");
                    dGridCheckLast = 0;

                    MyShipMass myMass;
                    myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();

                    newgridBaseMass = myMass.BaseMass;
                    Echo("New=" + newgridBaseMass + " CurrentM=" + gridBaseMass);
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

            if (sArgument == "init" || (Math.Abs(newgridBaseMass - gridBaseMass) > 1 && gridBaseMass > 0 && currentInit==0) || (currentInit == 0 && calcGridSystemChanged()))
            {
                Log("INIT or GRID/MASS CHANGE!");

                Echo("Arg init or grid/mass change!");
                sInitResults = "";
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
                {
                }
                bWantFast = true;
                doInit();
                bWasInit = true;
            }
            else
            {
                if(bSubModules) Deserialize();
                sPassedArgument = sArgument;

                if (bWasInit)
                {
                    StatusLog(DateTime.Now.ToString() + " " + sInitResults, textLongStatus, true);
                }

                IMyTerminalBlock anchorOrientation = shipOrientationBlock;
                if (shipOrientationBlock != null)
                {
//                    vCurrentPos = shipOrientationBlock.GetPosition();
                }

                // calculate(get) ship velocity and natural gravity
                if (shipOrientationBlock is IMyShipController)
                //		if (shipOrientationBlock is IMyRemoteControl)
                {
                    velocityShip = ((IMyShipController)shipOrientationBlock).GetShipSpeed();

                    Vector3D vNG = ((IMyShipController)shipOrientationBlock).GetNaturalGravity();
                    double dLength = vNG.Length();
                    dGravity = dLength / 9.81;
                }
                else
                {
                    dGravity = -1.0;
                }
                if (
                    (ut & (UpdateType.Trigger | UpdateType.Terminal)) > 0
                    || (ut & (UpdateType.Mod)) > 0 // script run by a mod
                    || (ut & (UpdateType.Script)) > 0 // this pb run by another script (PB)
                    )
                {
                    // pay attention to argument
                    if (moduleProcessArguments(sArgument))
                    {
                        Serialize();
                        UpdateAllPanels();
                        return;
                    }

                }
                else if ((ut & (UpdateType.Antenna)) > 0)
                {
                    // antenna message
                    if (!moduleProcessAntennaMessage(sArgument))
                    {
                        antReceive(sArgument);
                    }
                    Serialize();
                    doTriggerMain(); // run ourselves again
                    UpdateAllPanels();
                    return;
                }
                else
                {
                    // it should be one of the update types...
                    //            if ((ut & (UpdateType.Once | UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) > 0)
                    sArgument = ""; // else ignore argument
                }

                /*
                if (processArguments(sArgument))
                {
                    UpdateAllPanels();
                    return;
                }
                */
               moduleDoPreModes();

                doModes();
            }
            if(bSubModules) Serialize();

            //            if ((anchorPosition == null || SaveFile == null ))
            if (bSubModules)
            {
                if ((SaveFile == null))
                {
                    Echo("Cannot use sub-modules; missing controller and/or SaveFile");
                }
                else
                {
                    if (
                    (ut & (UpdateType.Trigger | UpdateType.Terminal)) > 0
                    || (ut & (UpdateType.Mod)) > 0 // script run by a mod
                    || (ut & (UpdateType.Script)) > 0 // this pb run by another script (PB)
                      ||  dSubmoduleTriggerLast > dSubmoduleTriggerWait
                        // || init // always run after init done
                        || bWasInit // run first time after init
                        )
                    {
                        Echo("Trigger sub-module!");
                        dSubmoduleTriggerLast = 0;
                        doSubModuleTimerTriggers();
                    }
                    else
                    {
                        Echo("Delay for sub-module trigger");
                        dSubmoduleTriggerLast+= Runtime.TimeSinceLastRun.TotalSeconds;
                    }
                }
            }
            else Echo("Submodules turned off");

            if (bWantFast)
            {
                Echo("FAST!");
                Runtime.UpdateFrequency |= ufFast;
            }
            else
            {
                Runtime.UpdateFrequency &= ~(ufFast);
            }
            if (bWantMedium)
            {
                Echo("MEDIUM");
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            }
            else
            {
                Runtime.UpdateFrequency &= ~(UpdateFrequency.Update10);
            }

            bWasInit = false;

            if(bCraftOperation) Echo(craftOperation());

            modulePostProcessing();
            UpdateAllPanels();
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