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
        bool bSupportSubModules = true;

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
        bool bIAmSubModule = false;

        double dSubmoduleTriggerWait = 5; //seconds between submodule triggers
        double dSubmoduleTriggerLast = -1;

        double dErrorGridReInitWait = 5; //seconds between trying to re-init between errors
        double dErrorGridReInitLast = -1;

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
            iniCustomData.GetValue(sMainSection, "SubModules", ref bSupportSubModules, true);
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



        bool init = false;
        bool bWasInit = false;
        string sInitResults = "";
        int currentInit = 0;

        string sStartupError = "";
        bool bStartupError = false;

        bool bWantFast = false;
        bool bWantMedium = false;

        bool bWorkingProjector = false;
        double dProjectorCheckWait = 5; //seconds between checks
        double dProjectorCheckLast = -1;

        double dGridCheckWait = 3; //seconds between checks
        double dGridCheckLast = -1;

        double velocityShip = -1;
        double dGravity = -2;

        //        void Main(string sArgument)
        void Main(string sArgument, UpdateType ut)
        {
           Echo(sBanner + tick());
            if (bDebugUpdate)
            {
                Echo(ut.ToString() + " : " + (int)ut);
            }
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
 //                  Echo("DO Grid Check");
                    dGridCheckLast = 0;

                    MyShipMass myMass;
                    myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();

                    newgridBaseMass = myMass.BaseMass;
//                    Echo("New=" + newgridBaseMass + " CurrentM=" + gridBaseMass);
//                    if (myMass.BaseMass == 0)  Echo("No Mass--Station?");
                    if (newgridBaseMass != gridBaseMass && gridBaseMass > 0)
                    {
                        Echo("MASS CHANGE");
                        StatusLog(OurName + ":" + moduleName + ":MASS CHANGE", textLongStatus, true);
                        // check for an error and retry
//                        if (bStartupError && !bWasInit)
                        {
                            // keep trying
                            init = false;
                            sInitResults = "";
                           dErrorGridReInitLast = 0;
                        }

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
                gridBaseMass = newgridBaseMass = -1;
                // check for an error and retry
                if (bStartupError && !bWasInit)
                {
                    // keep trying
                    init = false;
                    sInitResults = "";
                    dErrorGridReInitLast = 0;
                }
            }
            if (dErrorGridReInitLast > dErrorGridReInitWait)
            {
                dErrorGridReInitLast = 0;
                if (bStartupError)
                {
                    sArgument = "init";
                    Echo("RESCAN!");
                    dErrorGridReInitLast = 0;
                }
            }
            else
            {
                if (bStartupError)
                {
                    Echo("Waiting for Rescan:" + dErrorGridReInitLast.ToString("0.0") + "(" + dErrorGridReInitWait.ToString("0.0") + ")");
                    dErrorGridReInitLast += Runtime.TimeSinceLastRun.TotalSeconds;
                }
            }

            if (
                (sArgument == "init" && currentInit==0)
                || (Math.Abs(newgridBaseMass - gridBaseMass) > 1 && gridBaseMass > 0 && currentInit==0) 
   //             || (currentInit == 0 && calcGridSystemChanged())
                )
            {
                Log("INIT or GRID/MASS CHANGE!");

                Echo("Arg init or grid/mass change!");
                sInitResults = "";
                dErrorGridReInitLast = dErrorGridReInitWait + 5;
                init = false;
                currentInit = 0;
                sStartupError = "";
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
                if(currentInit==0)
                {
                    bStartupError = false;
                    sStartupError = "";
                }
                doInit();
                if (bStartupError) bWantFast = false;
                bWasInit = true;
                if (init)
                {
                    sArgument = "";
                    dErrorGridReInitLast = 0;
                }
            }
            else
            {
                if(bSupportSubModules) Deserialize();
                sPassedArgument = sArgument;

                if (bWasInit)
                {
                    StatusLog(DateTime.Now.ToString() + " " + sInitResults, textLongStatus, true);
                }

 //               IMyTerminalBlock anchorOrientation = shipOrientationBlock;
                if (shipOrientationBlock != null)
                {
//                    vCurrentPos = shipOrientationBlock.GetPosition();
                }

                // calculate(get) ship velocity and natural gravity
                if (shipOrientationBlock is IMyShipController)
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
                    || (ut & (UpdateType.Trigger)) > 0 // script run by a mod
                    || (ut & (UpdateType.Terminal)) > 0 // script run by a mod
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
                    /*
                    // check for an error and retry
                    if(bStartupError && !bWasInit)
                    {
                        // keep trying
                        init = false;
                        sInitResults = "";
                    }
                    */
                }

                /*
                if (processArguments(sArgument))
                {
                    UpdateAllPanels();
                    return;
                }
                */
                processPendingReceives();
                processPendingSends();
                moduleDoPreModes();

                doModes();
            }
            if(bSupportSubModules) Serialize();

            //            if ((anchorPosition == null || SaveFile == null ))
            if (bSupportSubModules)
            {
                if ((SaveFile == null))
                {
//                    Echo("Cannot use sub-modules; missing controller and/or SaveFile");
                }
                else
                {
                    if (
                    (ut & (UpdateType.Trigger | UpdateType.Terminal)) > 0 // Timer or toolbar or 'run'
                    || (ut & (UpdateType.Mod)) > 0 // script run by a mod
                    || (ut & (UpdateType.Script)) > 0 // this pb run by another script (PB)
                      ||  dSubmoduleTriggerLast > dSubmoduleTriggerWait
                        // || init // always run after init done
                        || bWasInit // run first time after init
                        )
                    {
//                        Echo("Trigger sub-module!");
                        dSubmoduleTriggerLast = 0;
                        doSubModuleTimerTriggers(sSubModuleTimer);
                    }
                    else
                    {
//                        Echo("Delay for sub-module trigger");
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


            if(bCraftOperation) Echo(craftOperation());

            modulePostProcessing();
            UpdateAllPanels();
            bWasInit = false;
        }

        void echoInstructions(string sBanner = null)
        {
            float fper = 0;
            fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
            if (sBanner == null) sBanner = "Instructions=";
            Echo(sBanner + (fper * 100).ToString("0.00") + "%");

        }

    }
}