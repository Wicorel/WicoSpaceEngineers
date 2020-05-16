using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {

        WicoIGC wicoIGC;
//        WicoControl _wicoControl;
        WicoBlockMaster wicoBlockMaster;
        WicoElapsedTime wicoElapsedTime;
        //        TravelMovement wicoTravelMovement;

        // Handlers
        private List<Action<string,MyCommandLine, UpdateType>> UpdateTriggerHandlers = new List<Action<string,MyCommandLine, UpdateType>>();
        private List<Action<UpdateType>> UpdateUpdateHandlers = new List<Action<UpdateType>>();

        // https://github.com/malware-dev/MDK-SE/wiki/Handling-Script-Arguments
        private MyCommandLine myCommandLine = new MyCommandLine();

        private List<Action<MyIni>> SaveHandlers = new List<Action<MyIni>>();
        private List<Action<MyIni>> LoadHandlers = new List<Action<MyIni>>();

        // reset motion handlers
        private List<Action<bool>> ResetMotionHandlers = new List<Action<bool>>();

        // Post init handlers
//        private List<Action> PostInitHandlers = new List<Action>();
        private List<IEnumerator<bool>> PostInitHandlers = new List<IEnumerator<bool>>();

        // https://github.com/malware-dev/MDK-SE/wiki/Handling-configuration-and-storage
        private MyIni _SaveIni = new MyIni();
        private MyIni _CustomDataIni = new MyIni();

        /// <summary>
        /// The combined set of UpdateTypes that count as a 'trigger'
        /// </summary>
        UpdateType utTriggers = UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod | UpdateType.Script;
        /// <summary>
        /// the combined set of UpdateTypes and count as an 'Update'
        /// </summary>
        UpdateType utUpdates = UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once;


        // Surface stuff
        IMyTextSurface mesurface0;
        IMyTextSurface mesurface1;


        double tmGridCheckElapsedMs = 0;
        double tmGridCheckWaitMs = 3.0 * 1000;

        string OurName = "Wico Modular";
        string moduleName = "";
        string moduleList = "";
        string sVersion = " 4.1";


        string sMasterReporting = "";

        public Program()
        {
            if(Me.TerminalRunArgument=="--clear")
            {
                Me.CustomData = "";
                Storage = "";
            }
            MyIniParseResult result;
            if (!_CustomDataIni.TryParse(Me.CustomData, out result))
            {
                Me.CustomData = "";
                _CustomDataIni.Clear();
                Echo(result.ToString());
            }
            if (!_SaveIni.TryParse(Storage, out result))
            {
                Storage = "";
                _SaveIni.Clear();
                Echo(result.ToString());
            }
            long meentityid=0;
            _SaveIni.Get(OurName+sVersion,"MEENITYID").TryGetInt64(out meentityid);
            if (meentityid != Me.EntityId)
            { // what's in storage was not created by this blueprint; clear it out.
                ErrorLog("New instance:Resetting Storage");
                Storage = "";
                _SaveIni.Clear();
            }
            _SaveIni.Set(OurName + sVersion, "MEENITYID", Me.EntityId);

            wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor
            wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            ModuleControlInit();

            wicoElapsedTime = new WicoElapsedTime(this,_wicoControl);

            ModuleProgramInit();

            Runtime.UpdateFrequency |= UpdateFrequency.Once; // cause ourselves to run again to continue initialization

            // Local PB Surface Init
            if (Me.SurfaceCount > 1)
            {
                mesurface0 = Me.GetSurface(0);
                mesurface0.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                mesurface0.WriteText(OurName + sVersion + moduleList);
                mesurface0.FontSize = 2;
                mesurface0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            }

            if (Me.SurfaceCount > 2)
            {
                mesurface1 = Me.GetSurface(1);
                mesurface1.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                mesurface1.WriteText("Version: " + sVersion);
                mesurface1.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                mesurface1.TextPadding = 0.25f;
                mesurface1.FontSize = 3.5f;
            }

            if (!Me.CustomName.Contains(moduleName))
                Me.CustomName = "PB" +moduleName;

            if (!Me.Enabled)
            {
                Echo("I am turned OFF!");
            }
        }

        public void Save()
        {
            foreach (var handler in SaveHandlers)
            {
                handler(_SaveIni);
            }
            Storage = _SaveIni.ToString();
        }

        void AddSaveHandler(Action<MyIni> handler)
        {
            if (!SaveHandlers.Contains(handler))
                SaveHandlers.Add(handler);
        }

        void AddLoadHandler(Action<MyIni> handler)
        {
            if (!LoadHandlers.Contains(handler))
                LoadHandlers.Add(handler);
        }

        bool LoadHandle(MyIni theIni)
        {
            foreach (var handler in LoadHandlers)
            {
                handler(_SaveIni);
            }
            return false; // no need to run again
        }

        void AddUpdateHandler(Action<UpdateType> handler)
        {
            if (!UpdateUpdateHandlers.Contains(handler))
                UpdateUpdateHandlers.Add(handler);
        }

        void AddTriggerHandler(Action<string,MyCommandLine, UpdateType> handler)
        {
            if (!UpdateTriggerHandlers.Contains(handler))
                UpdateTriggerHandlers.Add(handler);
        }

        void AddResetMotionHandler(Action<bool> handler)
        {
            if (!ResetMotionHandlers.Contains(handler))
                ResetMotionHandlers.Add(handler);
        }

        void ResetMotion(bool bNoDrills=false)
        {
            foreach (var handler in ResetMotionHandlers)
            {
                handler(bNoDrills);
            }
        }

        void AddPostInitHandler(IEnumerator<bool> handler)
        {
            if (!PostInitHandlers.Contains(handler))
                PostInitHandlers.Add(handler);
        }

        int postInitIterator = 0;
        bool PostInit()
        {
//            EchoInstructions("PostInit: #Handlers=" + PostInitHandlers.Count);
            for (; postInitIterator < PostInitHandlers.Count;postInitIterator++)
            {
                if(PostInitHandlers[postInitIterator].MoveNext())
                { // more to do on this handler.. 
                    return true;
                }
                else
                {
                    // we are with with this handler
                    PostInitHandlers[postInitIterator].Dispose();
                }
            }
//            EchoInstructions("PostInit:EOR");
            return false;// no need to run again.
        }

        bool bCustomDataNeedsSave = false;
        double LastRunMs = 0;
        double MaxRunMs = 0;
        public void Main(string argument, UpdateType updateSource)
        {
            LastRunMs = Runtime.LastRunTimeMs;
            if (bInitDone)
            { // only count max if done with init.
                if (LastRunMs > MaxRunMs)
                    MaxRunMs = LastRunMs;
            }
            if (moduleList != "") Echo(moduleList.Trim());
            ModulePreMain(argument, updateSource);
            if (tmGridCheckElapsedMs >= 0) tmGridCheckElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (!bInitDone)
            {
                if (!WicoLocalInit())
                {
                    // we did not complete an init.
                    Echo("Init Incomplete.  Trying again");

                    // try again
                    Runtime.UpdateFrequency = UpdateFrequency.Once;
                    return;
                }

            }
            else
            {
//                Echo("Inited");
                _wicoControl.AnnounceState();
                /*
                // only do this on update, not triggers
                if ((updateSource & utUpdates) > 0)
                {
                    //                    Echo("Init and update. Elapsed:" +tmGridCheckElapsedMs.ToString("0.00"));
                    //                    Echo("Local:" + wicoBlockMaster.localBlocksCount.ToString() + " blocks");
                    if (tmGridCheckElapsedMs > tmGridCheckWaitMs || tmGridCheckElapsedMs < 0) // it is time to scan..
                    {
                        //                      Echo("time to check");
                        tmGridCheckElapsedMs = 0;
                        if (wicoBlockMaster.CalcLocalGridChange())
                        {
                            //                            mesurface0.WriteText("GRID check!");
                            //                            Echo("GRID CHANGED!");
                            //                            mesurface0.WriteText("GRID CHANGED!", true);
//                            bInitDone = false;
                            tmGridCheckElapsedMs = 0;
                            Runtime.UpdateFrequency |= UpdateFrequency.Once; // cause ourselves to run again to continue initialization
                            return;
                        }
                        //                        else Echo("No Grid Change");
                    }
                    //else Echo("Not Timeto check");
                }
                //                else Echo("Init and NOTE update");
                */
            }

            if ((updateSource & UpdateType.IGC) > 0)
            {
//                Echo("IGC");
                wicoIGC.ProcessIGCMessages();
            }
            if ((updateSource & (utTriggers)) > 0)
            {
                //                Echo("Triggers:"+argument);
                ErrorLog("Trigger:"+updateSource.ToString()+":" + argument);
                MyCommandLine useCommandLine = null;
                if (myCommandLine.TryParse(argument))
                {
                    useCommandLine = myCommandLine;
                }
                bool bProcessed = false;

//                if(myCommandLine.ArgumentCount>1)
                {
                    if (argument == "save")
                    {
                        Save();
                        ErrorLog("After Save storage=");
                        ErrorLog(Storage);
                        bProcessed = true;
                    }

                }
                if (!bProcessed)
                {
                    foreach (var handler in UpdateTriggerHandlers)
                    {
                        handler(argument, useCommandLine, updateSource);
                    }
                }
            }
            if ((updateSource & (utUpdates)) > 0)
            {
                _wicoControl.ResetUpdates();
                Echo("Update:"+updateSource.ToString());
                foreach (var handler in UpdateUpdateHandlers)
                {
                    handler(updateSource);
                }
            }

            if(sMasterReporting!="") Echo("Reporting:\n"+sMasterReporting);
            if (sMasterReporting.Length > 1024*2)
            {
                sMasterReporting = "";
            }

            ModulePostMain();
            wicoElapsedTime.CheckTimers();
            if (bCustomDataNeedsSave)
            {
                bCustomDataNeedsSave = false;
                Me.CustomData = _CustomDataIni.ToString();
            }

            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();
        }

        public void ErrorLog(string str)
        {
            sMasterReporting += "\n"+str;
        }

        bool bInitDone = false;
        int InitStage = 0;
        bool WicoLocalInit()
        {
            EchoInstructions("WLI:");
            if (bInitDone) return true;
            if (InitStage < 1)
            {
                LoadHandle(_SaveIni);
                InitStage++;
                EchoInstructions("WLI:AfterLH:");
            }
            if (InitStage < 2)
            {
                if (PostInit())
                    return false; // more needed
                InitStage++;
                EchoInstructions("WLI:After PI:");
            }
            bInitDone = true;

            // last thing after init is done so that all modules are loaded
            // This call also has sub-handlers.
            if (InitStage < 3)
            {
                _wicoControl.ModeAfterInit(_SaveIni);
                InitStage++;
                EchoInstructions("WLI:After _wc:MAI:");
            }

            // Save it now so that any defaults are set after an initial run
            Me.CustomData = _CustomDataIni.ToString();

            return bInitDone;
        }

        void WicoInitReset()
        {
            bInitDone = false;
            InitStage = 0;
        }

        /// <summary>
        /// Set flag so customdata will be saved at end of this run
        /// </summary>
        void CustomDataChanged()
        {
            bCustomDataNeedsSave = true;
        }

        // UTILITY ROUTINES
        public string niceDoubleMeters(double thed)
        {
            string nice = "";
            if (thed > 1000)
            {
                nice = thed.ToString("N0") + "km";
            }
            else if (thed > 100)
            {
                nice = thed.ToString("0") + "m";
            }
            else if (thed > 10)
            {
                nice = thed.ToString("0.0") + "m";
            }
            else
            {
                nice = thed.ToString("0.000") + "m";
            }
            return nice;
        }
        public string Vector3DToString(Vector3D v)
        {
            string s;
            s = v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00");
            return s;
        }
        public bool stringToBool(string txt)
        {
            txt = txt.Trim().ToLower();
            return (txt == "on" || txt == "true");
        }
        string toGpsName(string ShipName, string sQual)
        {
            //NOTE: GPS Name can be a MAX of 32 total chars.
            string s;
            int iName = ShipName.Length;
            int iQual = sQual.Length;
            if (iName + iQual > 32)
            {
                if (iQual > 31) return "INVALID";
                iName = 32 - iQual;
            }
            s = ShipName.Substring(0, iName) + sQual;
            s.Replace(":", "_"); // filter out bad characters
            s.Replace(";", "_"); // filter out bad characters
            return s;

        }
        void EchoInstructions(string sBanner = null)
        {
            float fper = 0;
            fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
            if (sBanner == null) sBanner = "Instructions=";
            Echo(sBanner + " " + (fper * 100).ToString("0.00") + "%");
        }

    }
}

