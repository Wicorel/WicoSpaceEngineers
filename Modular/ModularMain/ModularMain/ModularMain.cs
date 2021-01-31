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
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        WicoIGC wicoIGC;
        WicoBlockMaster wicoBlockMaster;
        WicoElapsedTime wicoElapsedTime;

//        IFF igciff;

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
        private List<IEnumerator<bool>> PostInitHandlers = new List<IEnumerator<bool>>();

        // Main handlers
        private List<Action<UpdateType>> MainHandlers = new List<Action<UpdateType>>();

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
        bool bUsePBSurfaces = true;
        IMyTextSurface mesurface0;
        IMyTextSurface mesurface1;

        bool bAllowPBRename = true;

        double tmGridCheckElapsedMs = 0;
//        double tmGridCheckWaitMs = 3.0 * 1000;

        string OurName = "Wico Modular";
        /// <summary>
        /// The names of the modules attached to this script. Seperate names with space
        /// </summary>
        string moduleName = "";

        /// <summary>
        /// The list of modules including version number. Seperate with \n at start.
        /// </summary>
        string moduleList = "";
        string sVersion = " 4.2b";


        /// <summary>
        /// Reporting string that lives from run-to-run. Displayed in detailedinfo if !=""
        /// </summary>
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

            // check if our saved data is for this blueprint
            long meentityid=0;
            _SaveIni.Get(OurName+sVersion,"MEENITYID").TryGetInt64(out meentityid);
            if (meentityid != Me.EntityId)
            { // what's in storage was not created by this blueprint; clear it out.
                ErrorLog("New instance:Resetting Storage");
                Storage = "";
                _SaveIni.Clear();
            }
            _SaveIni.Set(OurName + sVersion, "MEENITYID", Me.EntityId);

            bAddDate = _CustomDataIni.Get(OurName, "DebugAddDate").ToBoolean(bAddDate);
            _CustomDataIni.Set(OurName, "DebugAddDate", bAddDate);
            bAddLogCount = _CustomDataIni.Get(OurName, "DebugAddLogCount").ToBoolean(bAddLogCount);
            _CustomDataIni.Set(OurName, "DebugAddLogCount", bAddLogCount);
            bAddRunCount = _CustomDataIni.Get(OurName, "DebugAddRunCount").ToBoolean(bAddRunCount);
            _CustomDataIni.Set(OurName, "DebugAddRunCount", bAddRunCount);

            bAllowPBRename = _CustomDataIni.Get(OurName, "AllowPBRename").ToBoolean(bAllowPBRename);
            _CustomDataIni.Set(OurName, "AllowPBRename", bAllowPBRename);

            bUsePBSurfaces = _CustomDataIni.Get(OurName, "UsePBSurfaces").ToBoolean(bUsePBSurfaces);
            _CustomDataIni.Set(OurName, "UsePBSurfaces", bUsePBSurfaces);

            bEchoOn = _CustomDataIni.Get(OurName, "EchoOn").ToBoolean(bEchoOn);
            _CustomDataIni.Set(OurName, "EchoOn", bEchoOn);

            wicoIGC = new WicoIGC(this); // Must be first as some use it in constructor
            wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
            wicoBlockMaster.LoadLocalGrid();

            // Module specific control init. (note: wicoControl is optional)
            ModuleControlInit();

            wicoElapsedTime = new WicoElapsedTime(this,_wicoControl);

//            igciff = new IFF(this, wicoIGC, wicoElapsedTime);

            // initialise module specific classes
            ModuleProgramInit();

            Runtime.UpdateFrequency |= UpdateFrequency.Once; // cause ourselves to run again to continue initialization

            _oldEcho = Echo;
            Echo = MyEcho;

            // Local PB Surface Init
            if (bUsePBSurfaces)
            {
                if (Me.SurfaceCount > 0)
                {
                    mesurface0 = Me.GetSurface(0);
                    mesurface0.ContentType = ContentType.TEXT_AND_IMAGE;
                    mesurface0.WriteText(OurName + sVersion + "\n" + moduleList);
                    mesurface0.FontSize = 1.3f;
                    mesurface0.Alignment = TextAlignment.CENTER;
                }

                if (Me.SurfaceCount > 1)
                {
                    mesurface1 = Me.GetSurface(1);
                    mesurface1.ContentType = ContentType.TEXT_AND_IMAGE;
                    mesurface1.WriteText("Version: " + sVersion);
                    mesurface0.Alignment = TextAlignment.CENTER;
                    mesurface1.TextPadding = 0.25f;
                    mesurface1.FontSize = 3.5f;
                }
            }

            if (bAllowPBRename && !Me.CustomName.Contains(moduleName))
                Me.CustomName = "PB" +moduleName;

            if (!Me.Enabled)
            {
                _oldEcho("I am turned OFF!");
            }
        }

        bool bEchoOn = true;

        Action<string> _oldEcho;
        void MyEcho(string output)
        {
            if (bEchoOn) _oldEcho(output);
        }

        public void Save()
        {
            foreach (var handler in SaveHandlers)
            {
                handler(_SaveIni);
            }
            Storage = _SaveIni.ToString();
        }

        /// <summary>
        /// Save to Storage in INI format
        /// </summary>
        /// <param name="handler"></param>
        void AddSaveHandler(Action<MyIni> handler)
        {
            if (!SaveHandlers.Contains(handler))
                SaveHandlers.Add(handler);
        }

        /// <summary>
        /// Load from Storage in INI format.
        /// </summary>
        /// <param name="handler"></param>
        void AddLoadHandler(Action<MyIni> handler)
        {
            if (!LoadHandlers.Contains(handler))
                LoadHandlers.Add(handler);
        }

        bool HandleLoad(MyIni theIni)
        {
            foreach (var handler in LoadHandlers)
            {
                handler(_SaveIni);
            }
            return false; // no need to run again
        }

        void HandleMain(UpdateType updateSource)
        {
            foreach(var handler in MainHandlers)
            {
                handler(updateSource);
            }
        }

        void AddUpdateHandler(Action<UpdateType> handler)
        {
            if (!UpdateUpdateHandlers.Contains(handler))
                UpdateUpdateHandlers.Add(handler);
        }

        /// <summary>
        /// Add a handler that's called with the script is triggered.
        /// </summary>
        /// <param name="handler"></param>
        void AddTriggerHandler(Action<string,MyCommandLine, UpdateType> handler)
        {
            if (!UpdateTriggerHandlers.Contains(handler))
                UpdateTriggerHandlers.Add(handler);
        }

        /// <summary>
        /// Add a handler for ResetMotion
        /// </summary>
        /// <param name="handler"></param>
        void AddResetMotionHandler(Action<bool> handler)
        {
            if (!ResetMotionHandlers.Contains(handler))
                ResetMotionHandlers.Add(handler);
        }

        /// <summary>
        /// Perform ResetMotion. Call all of the handlers for Resetmotion
        /// </summary>
        /// <param name="bNoDrills"></param>
        void ResetMotion(bool bNoDrills=false)
        {
            foreach (var handler in ResetMotionHandlers)
            {
                handler(bNoDrills);
            }
        }

        /// <summary>
        /// Add a handler that's called after class initialization has completed.
        /// </summary>
        /// <param name="handler"></param>
        void AddPostInitHandler(IEnumerator<bool> handler)
        {
            if (!PostInitHandlers.Contains(handler))
                PostInitHandlers.Add(handler);
        }
        /// <summary>
        /// Add a handler that's called on every invocation of Main() after init is completed.
        /// </summary>
        /// <param name="handler"></param>
        void AddMainHandler(Action<UpdateType> handler)
        {
            if (!MainHandlers.Contains(handler))
                MainHandlers.Add(handler);
        }

        int postInitIterator = 0;
        /// <summary>
        /// Perform post init handling.  
        /// </summary>
        /// <returns>Return true of more work to be done (for yield/return)</returns>
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
        long runCount = 0;
        public void Main(string argument, UpdateType updateSource)
        {
            runCount++;
            LastRunMs = Runtime.LastRunTimeMs;
            if (bInitDone)
            { // only count max if done with init.
                if (LastRunMs > MaxRunMs)
                    MaxRunMs = LastRunMs;
            }
            if (moduleList != "")
            {
                Echo(OurName + sVersion + moduleList);
//                Echo(moduleList.Trim());
            }
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
                if (_wicoControl != null)
                    _wicoControl.AnnounceState();
            }

//            if ((updateSource & UpdateType.IGC) > 0)
            {
                wicoIGC.ProcessIGCMessages();
            }
            if ((updateSource & (utTriggers)) > 0)
            {
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
//                Echo("Update:"+updateSource.ToString());
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

            HandleMain(updateSource);

//            wicoElapsedTime.CheckTimers();

            if (bCustomDataNeedsSave)
            {
                bCustomDataNeedsSave = false;
                Me.CustomData = _CustomDataIni.ToString();
            }

            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();
        }

        long logcount = 0;
        bool bAddDate = false;
        bool bAddLogCount = false;
        bool bAddRunCount = false;
        public void ErrorLog(string str)
        {
            if (bAddDate) str = System.DateTime.Now.ToLongTimeString() + ":" + str; ;
            if (bAddLogCount) str = logcount++.ToString() + ":" + str;
            if (bAddRunCount) str = runCount.ToString() + ":" + str;
            sMasterReporting += "\n"+str;
        }

        bool bInitDone = false;
        int InitStage = 0;
        bool WicoLocalInit()
        {
//            EchoInstructions("WLI:");
            if (bInitDone) return true;
            if (InitStage < 1)
            {
                HandleLoad(_SaveIni);
                InitStage++;
//                EchoInstructions("WLI:AfterLH:");
            }
            if (InitStage < 2)
            {
                if (PostInit())
                    return false; // more needed
                InitStage++;
//                EchoInstructions("WLI:After PI:");
            }
            bInitDone = true;

            // last thing after init is done so that all modules are loaded
            // This call also has sub-handlers.
            if (InitStage < 3)
            {
                if (_wicoControl != null)
                    _wicoControl.ModeAfterInit(_SaveIni);
                InitStage++;
//                EchoInstructions("WLI:After _wc:MAI:");
            }

            // Save it now so that any defaults are set after an initial run
            Me.CustomData = _CustomDataIni.ToString();

            return bInitDone;
        }

        /// <summary>
        /// Request that init be done again
        /// </summary>
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

        public Vector3D StringToVector3D(string s)
        {
            string[] coordinates = s.Split(',');
            if (coordinates.Length < 3)
            {
                coordinates = s.Split(':');
            }

            double x, y, z;
            int iCoordinate = 0;
            bool xOk = double.TryParse(coordinates[iCoordinate++].Trim(), out x);
            bool yOk = double.TryParse(coordinates[iCoordinate++].Trim(), out y);
            bool zOk = double.TryParse(coordinates[iCoordinate++].Trim(), out z);
            return new Vector3D(x, y, z);
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
            //TODO: add new color option

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

