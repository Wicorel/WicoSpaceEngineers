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
        bool bSubModule = true;

        float fMaxWorldMps = 100;
        string sWorldSection = "WORLD";

        void WorldInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sWorldSection, "MaxWorldMps", ref fMaxWorldMps, true);
        }

        void ProcessInitCustomData()
        {
            INIHolder iniCustomData = new INIHolder(this, Me.CustomData);

            iniCustomData.GetValue(OurName, "EchoOn", ref bEchoOn, true);

            WorldInitCustomData(iniCustomData);
            GridsInitCustomData(iniCustomData);
            LoggingInitCustomData(iniCustomData);

            ModuleInitCustomData(iniCustomData);
            if (iniCustomData.IsDirty)
            {
                Me.CustomData = iniCustomData.GenerateINI(true);
            }
        }

        bool bEchoOn = true;

        Action<string> _oldEcho;
        void MyEcho(string output)
        {
            // Do whatever you'd want with the output here
            if(bEchoOn) _oldEcho(output);
        }


        public Program()
        {
            doModuleConstructor();
            ProcessInitCustomData();

            //           _oldEcho = Echo;
            //           Echo = MyEcho;

            sBanner = OurName + ":" + moduleName + " V" + sVersion + " ";
            Echo(sBanner + "Creator");
            //            gridsInit(); //GridTerminalSystem cannot be relied on at initial compile
            //            initLogging();
            // Only needed for 'main' module           Runtime.UpdateFrequency |= UpdateFrequency.Once;
            if (!Me.CustomName.Contains(moduleName))
                Me.CustomName = "PB " + OurName + " " + moduleName;
            if (!Me.Enabled)
            {
                Echo("I am turned OFF!");
            }
        }

        // added UpdateType and UpdateFrequency
        // sub-module common main

        bool init = false;
        bool bWasInit = false;
        bool bWantFast = false;
        bool bWantMedium = false;

        bool bWorkingProjector = false;

        double velocityShip = -1;
        double dGravity = -2;

        //       void Main(string sArgument)
        void Main(string sArgument, UpdateType ut)
        {
            Echo(sBanner + tick());
//            Echo(ut.ToString());
            bWantFast = false;
            bWantMedium = false;

            bWorkingProjector = false;
            var list = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(list, localGridFilter);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsWorking)
                {
                    Echo("Projector:" + list[i].CustomName);
                    bWorkingProjector = true;
                }
            }
            if (bWorkingProjector)
                Echo("Working local Projector found!");

            if (sArgument != "" && sArgument != "timer" && sArgument != "wccs") Echo("Arg=" + sArgument);

            if (sArgument == "init")
            {
                sInitResults = "";
                init = false;
            }

            if (!init)
            {
                if (bWorkingProjector)
                {
                    StatusLog("clear", getTextBlock(sTextPanelReport));

                    StatusLog(moduleName + ":Construction in Progress\nTurn off projector to continue", textPanelReport);
                }
                bWantFast = true;
                doInit();
                bWasInit = true;
            }
            else
            {
                if (bWasInit) StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + sInitResults, textLongStatus, true);

                Deserialize();

                if (shipOrientationBlock is IMyShipController)
                {
                    velocityShip = ((IMyShipController)shipOrientationBlock).GetShipSpeed();
                    Vector3D vNG = ((IMyShipController)shipOrientationBlock).GetNaturalGravity();
                    //			Vector3D vNG = ((IMyRemoteControl)shipOrientationBlock).GetNaturalGravity();
                    double dLength = vNG.Length();
                    dGravity = dLength / 9.81;
                }
                if ((ut & (UpdateType.Trigger | UpdateType.Terminal)) > 0)
                {
                    // pay attention to argument
                    if (moduleProcessArguments(sArgument))
                    {
                        Serialize();
                        UpdateAllPanels();
                        return;
                    }

                }
                else if ((ut & (UpdateType.Mod)) > 0)
                {
                    // script run by a mod
                    if (moduleProcessArguments(sArgument))
                    {
                        Serialize();
                        UpdateAllPanels();
                        return;
                    }

                }
                else if ((ut & (UpdateType.Script)) > 0)
                {
                    // script run by another PB
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
                    doTriggerMain();
                    UpdateAllPanels();
                    return;
                }
                else
                {
                    //            if ((ut & (UpdateType.Once | UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) > 0)
                    sArgument = ""; // else ignore argument
                }

                processPendingReceives();
                processPendingSends();


                moduleDoPreModes();

                doModes();
            }
            Serialize();

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

            modulePostProcessing();

            bWasInit = false;
            UpdateAllPanels();

        }

        void echoInstructions(string sBanner = null)
        {
            float fper = 0;
            fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
            if (sBanner == null) sBanner = "Instructions=";
            Echo(sBanner + " " + (fper * 100).ToString("0.00") + "%");

        }



    }
}