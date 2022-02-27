﻿using Sandbox.Game.EntityComponents;
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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Miner
        {
            /* BUGS
             * Reload/recompile on bore doesn't seem to restore correctly
             * restart bore doens't restart single bore correctly; it goes to multi-bore start
             * curve/turning during bore mining.  check for it. maybe back up 5/10m and realign and move forward again? (add: maybe it was gyro bug)
             * 
             * TODO
             * size is getting large.. 97k
             *    need to seperate functionality 
             *       or otherwise reduce (Echo and other text check)
             * Add go mine command/functionality
             * 
             * Add 'target' for player reporting where ore is.
             *   -- needs menu system?
             * 
             * Test 'wanting' stone. Ie, having no 'undesireable' ores
             * 
             * Add 'stop bore after getting desireable and then getting nothing but undesireable' mode
             *  Used for 'mine ore at target'.
             *  Need to have specified ore threshhold
             *  
             * Add ability to remotely set/get ore desireability
             *  
             */
            private Program _program;
            private WicoControl _wicoControl;
            private WicoBlockMaster _wicoBlockMaster;
//            private Connectors _connectors;
//            private WicoThrusters _thrusters;
//            private Antennas _antennas;
//            private GasTanks _tanks;
//            private WicoGyros _gyros;
//            private PowerProduction _power;
            private Timers _timers;
            private WicoIGC _wicoIGC;
            private NavCommon _navCommon;
            private Sensors _sensors;
            private Drills _drills;
            //            private NavRemote _navRemote;
//            private CargoCheck _cargoCheck;
            private Cameras _cameras;

            private WicoElapsedTime _elapsedTime;
            private ScanBase _scans;
            private Asteroids _asteroids;

            private OreInfoLocs _oreInfoLocs;
            private OresLocal _ores;

            private DockBase _dock;
            private Displays _displays;

            private SystemsMonitor _systemsMonitor;
            private Antennas _antennas;

            const string CONNECTORAPPROACHTAG = "CONA";
            const string CONNECTORDOCKTAG = "COND";
            const string CONNECTORALIGNDOCKTAG = "ACOND";
            const string CONNECTORREQUESTFAILTAG = "CONF";

            public Miner(Program program, WicoControl wc, WicoBlockMaster wbm, WicoElapsedTime elapsedTime, WicoIGC iGC,
                ScanBase scanBase, Asteroids asteroids
                ,SystemsMonitor systemsMonitor
//                ,WicoThrusters thrusters, Connectors connectors
                , Sensors sensors
                , Cameras cameras, Drills drills
//                , Antennas ant
//                , GasTanks gasTanks, WicoGyros wicoGyros, PowerProduction pp
                , Timers timers, 
                NavCommon navCommon, OreInfoLocs oreInfoLocs, OresLocal ores,
                DockBase dock, Displays displays
                , Antennas antennas
                )
            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
//                _thrusters = thrusters;
//                _connectors = connectors;
                _sensors = sensors;
                _cameras = cameras;
                _drills = drills;
//                _antennas = ant;
//                _tanks = gasTanks;
//                _gyros = wicoGyros;
//                _power = pp;
                _timers = timers;
                _wicoIGC = iGC;
                _navCommon = navCommon;
                _elapsedTime = elapsedTime;
                _scans = scanBase;
                _asteroids = asteroids;
                _oreInfoLocs = oreInfoLocs;
                _ores = ores;
                _dock = dock;
                _displays = displays;
                _systemsMonitor = systemsMonitor;
                _antennas = antennas;

                _program.moduleName += " Space Miner";
                _program.moduleList += "\nSpaceMiner V4.2k";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddResetMotionHandler(ResetMotionHandler);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddPostInitHandler(PostInit());


                MiningCargopcthighwater = _program._CustomDataIni.Get(_program.OurName, "MiningCargopcthighwater").ToInt32(MiningCargopcthighwater);
                _program._CustomDataIni.Set(_program.OurName, "MiningCargopcthighwater", MiningCargopcthighwater);

                MiningCargopctlowwater = _program._CustomDataIni.Get(_program.OurName, "MiningCargopctlowwater").ToInt32(MiningCargopctlowwater);
                _program._CustomDataIni.Set(_program.OurName, "MiningCargopctlowwater", MiningCargopctlowwater);

                EjectorWaitSeconds = _program._CustomDataIni.Get(_program.OurName, EjectorWait).ToDouble(EjectorWaitSeconds);
                _program._CustomDataIni.Set(_program.OurName, EjectorWait, EjectorWaitSeconds);

                MaxEjectorWaitSeconds = _program._CustomDataIni.Get(_program.OurName, MaxEjectorWait).ToDouble(MaxEjectorWaitSeconds);
                _program._CustomDataIni.Set(_program.OurName, MaxEjectorWait, MaxEjectorWaitSeconds);

                _elapsedTime.AddTimer(miningChecksElapsed);
                _elapsedTime.AddTimer(miningElapsed);
                _elapsedTime.AddTimer(EjectorWait, EjectorWaitSeconds, null, false);
                _elapsedTime.AddTimer(MaxEjectorWait, MaxEjectorWaitSeconds, null, false);

                _displays.AddSurfaceHandler("MODE", SurfaceHandler);
            }

            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "MODE")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        int iMode = _wicoControl.IMode;
                        int iState = _wicoControl.IState;

                        if (
                            iMode == WicoControl.MODE_MINE
                            || iMode == WicoControl.MODE_GOTOORE
                            || iMode == WicoControl.MODE_BORESINGLE
                            || iMode == WicoControl.MODE_EXITINGASTEROID
                         )
                        {
                            tsurface.WriteText(sbModeInfo);
                            if (tsurface.SurfaceSize.Y < 512)
                            { // small/corner LCD

                            }
                            else
                            {
                                tsurface.WriteText(sbNotices, true);
                            }
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 512)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 3f;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 2f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
            }
            void LoadHandler(MyIni Ini)
            {
                miningAsteroidID=Ini.Get(_program.OurName, "MiningAsteroidID").ToInt32(0);
                AsteroidCurrentX = Ini.Get(_program.OurName, "AsteroidCurrentX").ToInt32(0);
                AsteroidCurrentY = Ini.Get(_program.OurName, "AsteroidCurrentY").ToInt32(0);

            }

            void SaveHandler(MyIni Ini)
            {
                Ini.Set(_program.OurName, "MiningAsteroidID", miningAsteroidID);
                Ini.Set(_program.OurName, "AsteroidCurrentX", AsteroidCurrentX);
                Ini.Set(_program.OurName, "AsteroidCurrentY", AsteroidCurrentY);
                /*
            iNIHolder.SetValue(sMiningSection, "miningAsteroidID", miningAsteroidID);

            iNIHolder.SetValue(sMiningSection, "AsteroidCurrentX", AsteroidCurrentX);
            iNIHolder.SetValue(sMiningSection, "AsteroidCurrentY", AsteroidCurrentY);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreStart", vAsteroidBoreStart);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreEnd", vAsteroidBoreEnd);
            iNIHolder.SetValue(sMiningSection, "AsteroidMineMode", AsteroidMineMode);
                */
            }
            /// <summary>
            /// Call to initialize information after all load is completed
            /// </summary>
            public IEnumerator<bool> PostInit()
            {
                yield return true;
                float fper = 0;

                fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                if (fper > 0.75f) yield return true;

                if (miningAsteroidID > 0) MinerCalculateAsteroidVector(miningAsteroidID);
            }

            /// <summary>
            /// Modes have changed and we are being called as a handler
            /// </summary>
            /// <param name="fromMode"></param>
            /// <param name="fromState"></param>
            /// <param name="toMode"></param>
            /// <param name="toState"></param>
            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if (
                    fromMode == WicoControl.MODE_MINE 
                    || fromMode == WicoControl.MODE_BORESINGLE
                    )
                {
                    _systemsMonitor.ResetMotion();
                    _drills.turnDrillsOff();
                    _elapsedTime.StopTimer(miningElapsed);
                    _elapsedTime.StopTimer(miningChecksElapsed);
                    _elapsedTime.ResetTimer(EjectorWait);
                    _elapsedTime.ResetTimer(MaxEjectorWait);
                }
                // need to check if this is us
                if (
                    toMode == WicoControl.MODE_MINE
                    || toMode == WicoControl.MODE_GOTOORE
                    || toMode == WicoControl.MODE_BORESINGLE
                    || toMode == WicoControl.MODE_EXITINGASTEROID
                    )
                {
                    _wicoControl.WantOnce();
                    _elapsedTime.StartTimer(miningElapsed);
                    _elapsedTime.StartTimer(miningChecksElapsed);
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode= _wicoControl.IMode;
                int iState=_wicoControl.IState;

                if (
                    iMode == WicoControl.MODE_MINE
                    || iMode == WicoControl.MODE_BORESINGLE
                    || iMode == WicoControl.MODE_EXITINGASTEROID
                    )
                {
                    // reset any state into a 'safe' state to start from.
                    _wicoControl.WantFast();
                }
            }
            void LocalGridChangedHandler()
            {
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                /* 
                 * triggers from V3
                 * nextbore - skip this bore and move to the next
                 * resetasteroid - forget the remembered asteroid
                 * masterreset - clears all saved state (mode, etc)
                 * 
                 * missing triggers from V1
                 * const string BUTTON0="Action Miner Reset";
                 * const string BUTTON1="Action Start Mining";
                 * const string BUTTON2="Action Start Search";
                 * const string BUTTON3="Action Master Reset";
                 * const string BUTTON4="Action Launch";
                 * const string BUTTON5="Action Target Ranged";
                 * const string BUTTON6="Action Go Home";
                 * const string BUTTON7="Action Set Home";
                 * const string BUTTON8="Action Go Mine";
                 * const string BUTTON9="Action Toggle Autopilot";
                 * const string BUTTON10="Action Toggle Relaunch";
                 * const string BUTTON11="M11";
                 * const string BUTTON12="M12";
                 * 
                 */
                /* TODO: Add following triggers
                 * set asteroid <id>
                 * set asteroid <gps> (save/read from CustomData)
                 * (dock) set home dock
                 * (dock) forget home dock
                 * (dock) set fixed approach location (V1 'home')
                 * Go mine the set asteroid (including launch)
                 * 
                 * Add: remember the current asteroid ID.  Use it for "go mine" (even if single bore)
                 */
                _program.Echo("Miner TriggerHandler:" + sArgument);
                string[] varArgs = sArgument.Trim().Split(';');

                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
                    // Commands here:

                }
                if (myCommandLine != null)
                {
                    if (myCommandLine.Argument(0) == "mine")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_MINE);
                    }
                    if (myCommandLine.Argument(0) == "gomine")
                    {
                        // launch and then go mine
                        _navCommon.NavReset();
                        _navCommon.NavQueueMode(WicoControl.MODE_LAUNCH);
                        _navCommon.NavQueueMode(WicoControl.MODE_MINE);
                        _navCommon.NavStartNav();
                    }
                    if (myCommandLine.Argument(0) == "dockmine")
                    {
                        // testing queueing (nav) commands.
                        _navCommon.NavReset();

                        // we should probably give hint to docking as to WHY we want to dock..
                        _dock.SetRelaunch(true);
                        _navCommon.NavQueueMode(WicoControl.MODE_DOCKING);

                        // Or maybe we need to tell docking to change mode when relaunched?
                        _navCommon.NavQueueMode(WicoControl.MODE_MINE);

                        _navCommon.NavStartNav();
                    }
                    if (myCommandLine.Argument(0) == "bore")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_BORESINGLE);
                    }
                    if (myCommandLine.Argument(0) == "sensor")
                    {
                        sb1 = _sensors.GetForwardSensor();
                        _program.Echo("FW Sensor=" + sb1.CustomName);
                        _sensors.SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                    }

                    for (int arg = 0; arg < myCommandLine.ArgumentCount; arg++)
                    {
                        string sArg = myCommandLine.Argument(arg);
                        // commands here:
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                //                _program.Echo("Miner UpdateHandler:" + updateSource.ToString());
                //                _program.Echo("MinerAirWorthy=" + _systemsMonitor.AirWorthy(false, false, MiningCargopcthighwater).ToString());
                if (miningAsteroidID > 0)
                {
                    if (AsteroidMineMode == 0)
                    {
                        _program.Echo("Asteroid  X=" + AsteroidCurrentX + "/" + AsteroidMaxX);
                        _program.Echo("  Y=" + AsteroidCurrentY + "/" + AsteroidMaxY);
                        _program.Echo("  " + (AsteroidCurrentX + AsteroidCurrentY) / (AsteroidMaxX + AsteroidMaxY * 1.0f) + "% completed");

                    }

                }
                if (iMode == WicoControl.MODE_MINE
                    || iMode == WicoControl.MODE_GOTOORE
                    || iMode == WicoControl.MODE_BORESINGLE
                    || iMode == WicoControl.MODE_EXITINGASTEROID
                    )
                {
                    bool bAirWorthy = _systemsMonitor.AirWorthy(false, false);
                    if(_systemsMonitor.HasHydroTanks() && _systemsMonitor.hydroPercent<1)
                    {
                        _antennas.SetAnnouncement("OUT OF GAS!");
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                    }
                }
                if (iMode == WicoControl.MODE_MINE) { doModeMine(); return; }
                if (iMode == WicoControl.MODE_GOTOORE) doModeGotoOre();
                if (iMode == WicoControl.MODE_BORESINGLE) doModeMineSingleBore();
                if (iMode == WicoControl.MODE_EXITINGASTEROID) doModeExitingAsteroid();
            }

            void UnicastHandler(MyIGCMessage msg)
            {
                // NOTE: Called for ALL received unicast messages
                int iMode= _wicoControl.IMode;
                int iState=_wicoControl.IState;
            }

            void ResetMotionHandler(bool bNoDrills = false)
            {
                _systemsMonitor.powerDownThrusters();
                _systemsMonitor.gyrosOff();

                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                if (shipController is IMyRemoteControl) ((IMyRemoteControl)shipController).SetAutoPilotEnabled(false);
                if (shipController is IMyShipController) ((IMyShipController)shipController).DampenersOverride = true;
                if (!bNoDrills) _drills.turnDrillsOff();
            }

            List<IMyTerminalBlock> thrustAllList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();


            int MiningCargopcthighwater = 95;
            int MiningCargopctlowwater = 85;

            float fTargetMiningMps = 0.55f;
            float fMiningAbortMps = 1.25f;
            float fMiningMinThrust = 0.85f;

            float fAsteroidApproachMps = 5.0f;
            float fAsteroidApproachAbortMps = 10.0f;

            float fAsteroidExitMps = 15.0f;

            bool bMiningWaitingCargo = false;


            long miningAsteroidID = -1;

            double MiningBoreHeight = -1;
            double MiningBoreWidth = -1;
            //        BoundingBoxD minigAsteroidBB;

            /*
             *split asteroid into bore holes for testing/destruction
             * 
             * Base size of hole on size of miner
             * 
             * Keep track of bore holes done
             */

            double MineShipLengthScale = 1.5;
            //        double MiningBoreOverlap = 0.05; // percent of overlap

            // regeneratable from asteroid info
            Vector3D AsteroidUpVector; // vector for 'up' (or 'down') in the asteroid to align drill holes
            Vector3D AsteroidOutVector;// vector for 'forward'  negative for going 'other' way.
            Vector3D AsteroidRightVector; // vector for right (-left)  Note: swap for 'other' way

            Vector3D AsteroidPosition;// world coordinates of center of asteroid
            double AsteroidDiameter;
            Vector3D vAsteroidBoreStart;
            Vector3D vAsteroidBoreEnd;
            int AsteroidMineMode = 0;
            // 0 = full destructive.
            // 1 = single bore


            //        bool bAsteroidBoreReverse = false; // go from end to start.

            // current state needs to be saved
            int AsteroidCurrentX = 0;
            int AsteroidCurrentY = 0;
            int AsteroidMaxX = 0;
            int AsteroidMaxY = 0;

            string sMiningSection = "MINING";
            string miningChecksElapsed = "miningChecksElapsedS";
            string miningElapsed = "miningElapsed";

            bool bValidExit = false;

            // need to serialize
            // should reverse meaning to bTunnelTurnable
            bool bBoringOnly = false;

            // are we finding ore by test bores, or full destructive mining?
            // are we going to a specific ore?

            /*
             * 0 Master Init
             * checks for known asteroid. 
             * if none known, check for nearby
             * if found->120
             * If none found ->1
             * 
             * 1: 
             * scan in front for asteroid. 
             * if found->120
             * if none directly in front, Starts Scans->5
             * 
             * 5: a requested scan was just completed.  Check for found asteroids
             * if found->120
             * if none found ->MODE_ATTENTION
             * 
             * 10 Init sensors
             * 11 check sensors to see if 'inside'
             *   in 'inside' ->20
             *   else ->100
             *   
             * 20 [OBS] Start mining while 'inside' asteroid
             *  set exit
             *  ->31
             *  
             *  31 set sensor for mining run  START HERE; NEVER 35
             *  calculate asteroid vectors 
             *  ->32
             *  
             *  32 delay for sensors ->35
             *  
             *  35 Mining/finding
             *  
             *  
             *  100 Init sensor for forward search for asteroid
             *      -> 101
             *   
             *  101 delay for sensors ->102
             *  102 Check sensors for asteroid in front
             *      check camera for asteroid in front
             *      if found ->120
             *      else ->110
             *      
             *  110 asteroid NOT found in front. big sensor search
             *      Set sensors ->111
             *  111 sensors delay ->112
             *  112 check sensors
             *  
             *  120 we have a known asteroid.  go toward our starting location
             *  check cargo, etc.  dock if needed
             *  wait for velocity -> 121
             *  
             *  121 
             *   Far travel ->190 Arrival ->195
             *   if "close enough" ->125
             *   
             *   125 move forward to start loc
             *   ->130
             *  
             *  130 asteroid should be close.  Approach
             *  Init sensors ->131
             *  
             *  131 align to 'up'. ->134
             *  134 align forward ->137
             *  137 align to 'up' ->140
             *  
             *  140 asteroid in front.
             *  check if the bore has any asteroid with raycast
             *  if found->150.  else next bore ->120
             *  
             *  150
             *  (approach) 
             *  Needs vexpectedAsteroidExit set.
             *  Assumes starting from current position
             *  
             *     move forward until close.  then ->31
             *     
             *  190 far travel to asteroid.
             *     ->MODE_NAV  Arrive->195  Error->MODE_ATTENTION
             *     
             *  195 We have arrived from far travel. Await slower movement ->120
             *     
             *     
             *  300 exited asteroid
             *  if above waters, call for pickup (DOCKING)
             *  else, turn around and do it again 
             * 
             * 500 bores all completed.
             * do any final checks and then remove asteroid and go home.
             * 
             * 
             */

            const string EjectorWait = "EjectorWait";
            double EjectorWaitSeconds = 5;
            const string MaxEjectorWait = "MaxEjectorWait";
            double MaxEjectorWaitSeconds = 60;
            const string MaxScansWait = "MaxScansWait";
            double MaxScansWaitSeconds = 120;

                int BoreHoleScanMode = -1;
            Vector3D[] BoreScanFrontPoints = new Vector3D[4];


            List<IMySensorBlock> aSensors = null;
            IMySensorBlock sb1 = null;
            IMySensorBlock sb2 = null;

            void doModeMine()
            {
                int iMode= _wicoControl.IMode;
                int iState=_wicoControl.IState;

                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("MINE");

                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":MINE", textPanelReport);
                _program.Echo("MINE:iState=" + iState.ToString());
                _program.Echo("Mine Mode=" + AsteroidMineMode);
                if (thrustForwardList.Count < 1)
                {
                    _systemsMonitor.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }
                //            echoInstructions("MM-A:" + iState );
                //            _program.Echo(Vector3DToString(vExpectedAsteroidExit));
                //            _program.Echo(Vector3DToString(vLastAsteroid            Vector3D[] corners= new Vector3D[BoundingBoxD.CornerCount];
                //            _program.Echo(Vector3DToString(vLastAsteroidExit));
                //StatusLog("clear", gpsPanel);
                //debugGPSOutput("BoreStart" + AsteroidCurrentX.ToString("00") + AsteroidCurrentY.ToString("00"), vAsteroidBoreStart);
                //debugGPSOutput("BoreEnd" + AsteroidCurrentX.ToString("00") + AsteroidCurrentY.ToString("00"), vAsteroidBoreEnd);

                // TODO: Cache these values.
                double maxThrust = _systemsMonitor.calculateMaxThrust(thrustForwardList);
                //            _program.Echo("maxThrust=" + maxThrust.ToString("N0"));
                if (bBoringOnly)
                {
                    double maxBackThrust = _systemsMonitor.calculateMaxThrust(thrustBackwardList);
                    if (maxBackThrust < maxThrust)
                    {
                        _program.Echo("BACK thrust is less than forward!");
                        maxThrust = maxBackThrust;
                    }
                    // TODO: also check docking 'reverse' thrust iff other than 'back' connector
                }

                double effectiveMass = _wicoBlockMaster.GetPhysicalMass(); 
                //            _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));

//                if (miningAsteroidID <= 0)
                    //StatusLog("No Current Asteroid", textPanelReport);

                double maxDeltaV = (maxThrust) / effectiveMass;
                _program.Echo("MaxDV=" + maxDeltaV.ToString("0.00") + " minthrust=" + fMiningMinThrust.ToString("0.00"));
                _program.Echo("cargo%=" + _ores.cargopcent + " max=" + MiningCargopcthighwater);

                if (iState > 0)
                {
//                    if (miningChecksElapsedMs >= 0) miningChecksElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (_elapsedTime.IsInActiveOrExpired(miningChecksElapsed))
                    {
                        _elapsedTime.RestartTimer(miningChecksElapsed);
                        _ores.doCargoCheck();
                        _ores.OreDoCargoCheck();
                        _systemsMonitor.BatteryCheck(0, false);
                        //                    echoInstructions("MM-C:" + iState);
                        // TODO: check hydrogen tanks
                        // TODO: check reactor uranium
                        float fper = 0;
                        fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                        if (fper > 0.35f)
                        {
                            _wicoControl.WantOnce();
//                            _wicoControl.WantFast();
                            return;
                        }
                    }
                }
                aSensors = _sensors.SensorsGetActive();
                if (sb1 == null)
                {
                    sb1 = _sensors.GetForwardSensor();
                    sb1.DetectAsteroids = true;
                }
                if(sb2==null)
                {
                    sb2 = _sensors.GetForwardSensor(1);
                    sb2.DetectAsteroids = true;
                }
                switch (iState)
                {
                    case 0:
                        bValidExit = false;
                        bMiningWaitingCargo = false;
                        _elapsedTime.StartTimer(miningElapsed);
                        _elapsedTime.ResetTimer(miningElapsed);

                        _program.ResetMotion();
                        _systemsMonitor.TurnEjectorsOff();
                        _ores.doCargoCheck();
                        _ores.OreDoCargoCheck(true); // init ores to what's currently in inventory
                        MinerCalculateBoreSize();
                        _systemsMonitor.MoveForwardSlowReset();

                        _wicoControl.WantFast();

                        if (_sensors.GetCount() < 2)
                        {
                            //StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                            return;
                        }

                        // Can we turn in our own tunnel?
                        if (_wicoBlockMaster.LengthInMeters() > _wicoBlockMaster.WidthInMeters() && _wicoBlockMaster.LengthInMeters() > _wicoBlockMaster.HeightInMeters())
                            bBoringOnly = true;
                        else bBoringOnly = false;

                        if (miningAsteroidID <= 0) // no known asteroid
                        {
                            // check if we know one
                            miningAsteroidID = _asteroids.AsteroidFindNearest();
                            Vector3D AsteroidPos = _asteroids.AsteroidGetPosition(miningAsteroidID);
                            double curDistanceSQ = Vector3D.DistanceSquared(AsteroidPos, _wicoBlockMaster.GetMainController().GetPosition());

                            // TODO: allow FAR mining
                            if (curDistanceSQ > 5000 * 5000)
                            {
                                // it's too far away. ignore it.
                                _program.ErrorLog("Ignoreing FAR asteroid");
                                miningAsteroidID = 0;
                            }
                        }
                        if (miningAsteroidID > 0) // return to a known asteroid
                        {
//                            _program.ErrorLog("Using Known asteroid");
                            MinerCalculateBoreSize();
                            if (AsteroidMineMode == 1)
                            {

                            }
                            else
                            {
                                MinerCalculateAsteroidVector(miningAsteroidID);
                                AsteroidCalculateBestStartEnd();
                            }
                            _wicoControl.SetState(120);
                            _wicoControl.WantFast();
                        }
                        else
                        {
                            _wicoControl.SetState(1);
                            _wicoControl.WantFast();
                        }
                        break;
                    case 1:
                        { // no target asteroid.  Raycast in front of us for one.
                            sbModeInfo.AppendLine("Check for Asteroid in front");
                            double scandist = 2000;

                            // TODO: assumes have forward cameras.
                            if (_cameras.CameraForwardScan( scandist))
                            { // we scanned
                              //                            sStartupError += " Scanned";
                                if (!_cameras.lastDetectedInfo.IsEmpty())
                                {  // we hit something
                                   //                                sStartupError += " HIT!";
                                    if (_cameras.lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                    {
                                        MinerProcessScan(_cameras.lastDetectedInfo);
                                        miningAsteroidID = _cameras.lastDetectedInfo.EntityId;
                                    }
                                    if (miningAsteroidID > 0) // go to the asteroid we just found
                                    {
                                        //                                   vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
                                        //                                    bValidAsteroid = true;
                                        //                                        AsteroidMineMode = 1;// drill exactly where we're aimed for.

                                        /* keep the mode..  
                                        if(iState== WicoControl.MODE_BORESINGLE)
                                            AsteroidMineMode = 1; // Just what we're aiming at.
                                        else
                                            AsteroidMineMode = 0; // mine the whole thing
                                        */
                                        MinerCalculateAsteroidVector(miningAsteroidID);

                                        // reset bore hole info to current location
                                        vAsteroidBoreStart = _wicoBlockMaster.GetMainController().GetPosition();

                                        Vector3D vTarget = (Vector3D)_cameras.lastDetectedInfo.HitPosition - _wicoBlockMaster.GetMainController().GetPosition();

                                        vAsteroidBoreEnd = vAsteroidBoreStart;
                                        vAsteroidBoreEnd += _wicoBlockMaster.GetMainController().WorldMatrix.Forward * (AsteroidDiameter + vTarget.Length());

                                        AsteroidUpVector = _wicoBlockMaster.GetMainController().WorldMatrix.Up;

                                        _wicoControl.SetState(120);
                                        _wicoControl.WantFast();
                                    }
                                    else
                                    {
                                        _wicoControl.SetState(6);
                                        _scans.StartScans(iMode, 5); // try again after a scan
                                    }
                                }
                                else
                                {
                                    _wicoControl.SetState(6);
                                    _scans.StartScans(iMode, 5); // try again after a scan
                                }
                            }
                            else
                            {
                                sbNotices.AppendLine("Waiting for available raycast");
                                _program.Echo("Awaiting Available camera");
                                _wicoControl.WantMedium();
                            }
                            break;
                        }
                    case 6:
                        // waiting for scan to start remotely
                        _elapsedTime.AddTimer(MaxScansWait, MaxScansWaitSeconds, null, false);
                        if(!_elapsedTime.IsActive(MaxScansWait))
                        {
                            _elapsedTime.RestartTimer(MaxScansWait);

                        }
                        if(_elapsedTime.IsExpired(MaxScansWait))
                        {
                            // timeout: it's taken too long
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                            sbModeInfo.AppendLine("Scan Timeout");
                            _program.Echo("Scan Timeout");

                        }
                        sbModeInfo.AppendLine("Waiting for scan start");
                        _program.Echo("Waiting for Scans to start");
                        break;
                    case 5:
                        // we have done a LIDAR scan.  check for found asteroids
                        sbModeInfo.AppendLine("Scan Completed");

                        // TODO: pretty much duplicate code from just above.
                        if (miningAsteroidID <= 0) // no known asteroid
                        {
                            // check if we know one
                            AsteroidMineMode = 0; // should use default mode
                            miningAsteroidID = _asteroids.AsteroidFindNearest();
                        }
                        if (miningAsteroidID > 0)
                        {
                            // we have a valid asteroid.
                            //                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
                            //                        bValidAsteroid = true;
                            MinerCalculateAsteroidVector(miningAsteroidID);
                            AsteroidCalculateFirstBore();

                            _wicoControl.SetState(120);
                            _wicoControl.WantFast();
                        }
                        else
                        {
                            _elapsedTime.StopTimer(miningChecksElapsed);
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION); // no asteroid to mine.
                        }
                        break;
                    case 10:
                        //sb = sensorsList[0];
                        _sensors.SensorsSleepAll();
                        sb1 = _sensors.GetForwardSensor();
                        _sensors.SensorSetToShip(sb1, 2, 2, 2, 2, 2, 2);
                        //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                        _elapsedTime.ResetTimer(miningElapsed);// = 0;
                        _wicoControl.SetState(11);
                        _wicoControl.WantMedium();
                        break;

                    case 11:
                        {
                            sbModeInfo.AppendLine("Check Sensor for Inside");
                            //                            miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                            if (!_elapsedTime.IsActive(miningElapsed))
                            {
                                _elapsedTime.ResetTimer(miningElapsed);
                                _elapsedTime.StartTimer(miningElapsed);
                            }
                            if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait)
                            {
                                sbNotices.AppendLine("Waiting for sensor settle");
                                return;
                            }

                            aSensors = _sensors.SensorsGetActive();
                            bool bFoundAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                if (_asteroids.AsteroidProcessLDEI(lmyDEI))
                                    bFoundAsteroid = true;

                                /*
                                                            //StatusLog("#DetectedEntities=" + lmyDEI.Count, txtPanel);

                                                            strbMining.Clear();
                                                            strbMining.Append("Sensor Position:" + s.GetPosition().ToString());
                                                            strbMining.AppendLine();


                                                            for (int j = 0; j < lmyDEI.Count; j++)
                                                            {

                                                                strbMining.Append("Name: " + lmyDEI[j].Name);
                                                                strbMining.AppendLine();
                                                                strbMining.Append("Type: " + lmyDEI[j].Type);
                                                                strbMining.AppendLine();
                                                                if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                                                                {
                                                                    addDetectedEntity(lmyDEI[j]);
                                                                    //StatusLog("Found Asteroid!", txtPanel);
                                                                    bFoundAsteroid = true;
                                                                    currentAst.EntityId = lmyDEI[j].EntityId;
                                                                    currentAst.BoundingBox = lmyDEI[j].BoundingBox;
                                                                    bValidAsteroid = true;
                                                                    vTargetAsteroid = lmyDEI[j].Position;
                                                                }
                                                            }
                                                            //StatusLog(strbMining.ToString(), txtPanel);
                                                            */
                            }
                            if (bFoundAsteroid)
                            {
                                _wicoControl.SetState(100);
                            }
                            else
                            {
                                // no asteroid in sensor range.  Try cameras
                                _wicoControl.SetState(400);
                            }
                            _wicoControl.WantFast();
                        }
                        break;
                    case 20:
                        {
                            // started find ore while 'inside' asteroid.
                            // point towards exit
                            /*
                            vExpectedExit = _wicoBlockMaster.GetMainController().GetPosition() - currentAst.Position;
                            vExpectedExit.Normalize();
                            bValidExit = true;
                            */
                            _wicoControl.SetState(31);
                        }
                        break;
                    case 31:
                        sb1 = _sensors.GetForwardSensor();// sensorsList[0];
                        sb2 = _sensors.GetForwardSensor(1); // sensorsList[1];
                        _sensors.SensorsSleepAll();
                        _sensors.SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                        _sensors.SensorSetToShip(sb2, (float)_wicoBlockMaster.WidthInMeters(), (float)_wicoBlockMaster.WidthInMeters(),
                            (float)_wicoBlockMaster.HeightInMeters(), (float)_wicoBlockMaster.HeightInMeters(),
                            1, (float)_wicoBlockMaster.LengthInMeters());
                        _wicoControl.SetState(32);
                        _elapsedTime.ResetTimer(miningElapsed);
                        _elapsedTime.StartTimer(miningElapsed);
                        _wicoControl.WantFast();
                        _program.ResetMotion();
                        break;
                    case 32:

                        //                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        //                        _elapsedTime.StartTimer(miningElapsed);
                        _program.Echo("IsActive=" + _elapsedTime.IsActive(miningElapsed));
                        if (!_elapsedTime.IsActive(miningElapsed))
                        {
                            _program.Echo("Restarting Timer:" + miningElapsed);
                            _elapsedTime.RestartTimer(miningElapsed);
                        }
                        _program.Echo("Elapsed=" + _elapsedTime.GetElapsed(miningElapsed).ToString("0.00"));
                        if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait)
                        {
                            _program.Echo("Waiting for Sensor to Settle");
                            sbNotices.AppendLine("Waiting for sensor settle");
                            _wicoControl.WantMedium();
                            return; // delay for sensor settling
                        }
                        // else:
                        _elapsedTime.StopTimer(miningElapsed);
                        _wicoControl.WantFast();
                        //                    vAsteroidBoreStart = AsteroidCalculateBoreStart();
                        _wicoControl.SetState(35);
                        break;
                    case 35:
                        { // active mining
                          //
                          //                        int eoicount = 0;
                          //                        echoInstructions("S=" + iState + " " + eoicount++);
                            bool bAimed = false;
                            sbModeInfo.AppendLine("Mine Forward");
                            _program.Echo("Mining forward");
                            //StatusLog("Mining Forward!", textPanelReport);
                            if (bBoringOnly)
                            {
                                sbNotices.AppendLine("Bore Miner only");
                                _program.Echo("Boring Miner");
                            }
                            bool bLocalAsteroid = false;
                            bool bForwardAsteroid = false;
                            bool bSourroundAsteroid = false;
                            bool bLarge = false;
                            bool bSmall = false;
                            //                        _program.Echo("FW=" + sb1.CustomName);
                            //                        _program.Echo("AR=" + sb2.CustomName);
                            // TODO: Make sensors optional (and just always do runs and use distance to know when done with bore.
                            //                        echoInstructions("S=" + iState + "S " + eoicount++);
                            _sensors.SensorIsActive(sb1, ref bForwardAsteroid, ref bLarge, ref bSmall);
//                            if(!bForwardAsteroid) _sensors.SensorIsActive(sb2, ref bForwardAsteroid, ref bLarge, ref bSmall);

                            _sensors.SensorIsActive(sb2, ref bSourroundAsteroid, ref bLarge, ref bSmall);
                            //                        echoInstructions("S=" + iState + "ES " + eoicount++);
                            //                        _program.Echo("FW=" + bForwardAsteroid.ToString() + " AR=" + bSourroundAsteroid.ToString());
                            aSensors = _sensors.SensorsGetActive();
                            //                        _program.Echo(aSensors.Count + " Active Sensors");
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                //                            _program.Echo(aSensors[i].CustomName + " ACTIVE!");
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                var lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                if (_asteroids.AsteroidProcessLDEI(lmyDEI))
                                {
                                    // TODO: if we find ANOTHER asteroid in sensors, figure out what to do
                                    bLocalAsteroid = true;
                                }
                            }
                            //                        echoInstructions("S=" + iState + "EDEI " + eoicount++);

                            double distance = (vAsteroidBoreStart - _wicoBlockMaster.GetMainController().GetPosition()).Length();

                            // *2 because of start and end enhancement
                            double boreLength = AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2;
                            sbNotices.AppendLine("Distance=" + _program.niceDoubleMeters(distance) + " (" + _program.niceDoubleMeters(boreLength) + ")");
                            _program.Echo("Distance=" +_program.niceDoubleMeters(distance) + " (" +_program.niceDoubleMeters(boreLength) + ")");
                            double stoppingDistance = _systemsMonitor.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(),thrustBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);
                            //StatusLog("Bore:" + ((distance + stoppingDistance) / boreLength * 100).ToString("N0") + "%", textPanelReport);
                            //                        if ((distance + stoppingDistance) < (AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2))// even if sensors don't see anything. continue to end of the bore.
                            if ((distance + stoppingDistance) < boreLength * 0.65) // if we are <65% done with bore, continue no matter what sensors say
                            {
                                bLocalAsteroid = true;
                            }
                            if (!bLocalAsteroid)
                            { // no asteroid detected on ANY sensors. ->we have exited the asteroid.
                              //                            _program.Echo("No Local Asteroid found");
//                                _program.ErrorLog("MINE35:No Local Asteroid found");
                                _program.ResetMotion();
                                //                            echoInstructions("S=" + iState + "RM " + eoicount++);
                                if (_ores.cargopcent > MiningCargopctlowwater || maxDeltaV < (fMiningMinThrust))
                                {
                                    // we need to dump our contents
                                    _systemsMonitor.TurnEjectorsOn();
                                    //                                echoInstructions("S=" + iState + "EJ " + eoicount++);
                                }
                                //                            sStartupError += "\nOut:" + aSensors.Count + " : " +bForwardAsteroid.ToString() + ":"+bSourroundAsteroid.ToString();
                                //                            sStartupError += "\nFW=" + bForwardAsteroid.ToString() + " Sur=" + bSourroundAsteroid.ToString();
                                _wicoControl.SetState(300);
                                _wicoControl.WantFast();
                                return;
                            }
                            _systemsMonitor.AirWorthy(false, false, MiningCargopcthighwater);
                            if (!_systemsMonitor.ReactorsGo || !_systemsMonitor.BatteryGo || !_systemsMonitor.TanksGo)
                            {
                                // we are low on power/fuel
                                _program.ResetMotion();
                                _wicoControl.SetMode(WicoControl.MODE_EXITINGASTEROID);
                                return;
                            }
                            _program.Echo("R=" + (_systemsMonitor.ReactorsGo ? "Go" : "NO"));
                            _program.Echo("B=" + (_systemsMonitor.BatteryGo ? "Go" : "NO"));
                            _program.Echo("T=" + (_systemsMonitor.TanksGo ? "Go" : "NO"));
                            if (bForwardAsteroid)
                            { // asteroid in front of us
                                bool bCargoOK = _ores.cargopcent < MiningCargopctlowwater; // how full is our cargo
                                if(!bCargoOK)
                                {
                                    sbNotices.AppendLine("Cargo Full");
                                }
                                if (_ores.bHasDrills && _ores.bCargoFull && !_ores.bDrillFull)
                                {
                                    bCargoOK = true; // if we still have room left drills, keep going 
                                    sbNotices.AppendLine(" using final drill space");
                                }
                                _systemsMonitor.TurnEjectorsOn();
                                if (!bMiningWaitingCargo)
                                {
                                    if (
                                        !_systemsMonitor.AirWorthy(false, false, MiningCargopcthighwater)
                                        || maxDeltaV < fMiningMinThrust
                                        || !bCargoOK
                                        )
                                    {
//                                        _program.Echo("Waiting Cargo Start");
                                        _program.ResetMotion();
                                        bMiningWaitingCargo = true;
                                        _elapsedTime.RestartTimer(EjectorWait);
                                        _elapsedTime.RestartTimer(MaxEjectorWait);
                                    }
                                }
                             
                                if (bMiningWaitingCargo)
                                { // continue to wait
                                    _wicoControl.WantSlow();
//                         _program.Echo("Waiting Cargo Continue");
                                    _program.ResetMotion();
                                    // need to check how much stone we have.. if zero(ish), then we're full.. go exit.

                                    double currUndesireable = _oreInfoLocs.CurrentUndesireableAmount();
                                    _program.Echo("MaxWait active=" + _elapsedTime.IsActive(MaxEjectorWait));

                                   _program.Echo("MaxElapsed=" + (_elapsedTime.GetElapsed(MaxEjectorWait)).ToString("0.00"));
                                    _program.Echo("EjectorElapsed=" + (_elapsedTime.GetElapsed(EjectorWait)).ToString("0.00"));

                                    //                                    _program.ErrorLog("MINE35:undesireable = " + currUndesireable);
                                    _program.Echo("undesireable=" + currUndesireable);
                                    if (
                                        _elapsedTime.IsExpired(MaxEjectorWait)
                                        || (
                                            _elapsedTime.IsExpired(EjectorWait)
                                            && currUndesireable < 15  // we having nothing left to wait for to eject.
                                            &&
                                            ( // we are 'fat'
                                                !_systemsMonitor.AirWorthy(false, false, MiningCargopctlowwater)
                                                || maxDeltaV < fMiningMinThrust
                                            //                                     || _ores.cargopcent >= MiningCargopctlowwater
                                            )
                                        )
                                       )
                                    {
                                        // we are full/getting fat and not much undesireable ore in us...
// already done                                        _program.ResetMotion();
                                        _systemsMonitor.TurnEjectorsOff(); // we don't want this is reset motion
                                        _wicoControl.SetMode(WicoControl.MODE_EXITINGASTEROID);
                                    }
 //                                   _program.Echo("TanksGo=" + _systemsMonitor.TanksGo.ToString());

                                    //StatusLog("Waiting for cargo and thrust to be available", textPanelReport);
                                    if (maxDeltaV > fMiningMinThrust)
                                    {
                                        sbNotices.AppendLine("Waiting for min thrust");
                                    }
                                    if(_ores.cargopcent < MiningCargopctlowwater)
                                    {
                                        sbNotices.AppendLine("Waiting for Cargo space");
                                    }
                                    //                                    sbNotices.AppendLine("Waiting for cargo and thrust to be available");
                                    //                                    _program.Echo("Cargo above low water: Waiting");
                                    if (
                                        maxDeltaV > fMiningMinThrust 
                                        && bCargoOK
                                        && currUndesireable < 1000
                                        )
                                    {
//                                        _program.Echo("MaxDelvaV" + (maxDeltaV > fMiningMinThrust).ToString());
//                                        _program.Echo("Cargo%Good=" + (_ores.cargopcent < MiningCargopctlowwater).ToString());
//                                        _program.Echo("Undesireablegood enough=" + (currUndesireable < 1000));
//                                        _program.Echo("resetting cargo wait");
                                        bMiningWaitingCargo = false; // can now move.
                                        _elapsedTime.ResetTimer(EjectorWait);
                                        _elapsedTime.ResetTimer(MaxEjectorWait);
                                    }
                                }
                                else
                                {
                                    
                                    bAimed = _systemsMonitor.BeamRider(vAsteroidBoreStart, vAsteroidBoreEnd, _wicoBlockMaster.GetMainController());
                                    _program.Echo("Angle off=" + MathHelper.ToDegrees(_systemsMonitor._gyros.VectorAngleBetween(vAsteroidBoreEnd - vAsteroidBoreStart, _wicoBlockMaster.GetMainController().WorldMatrix.Forward)));
                                    if (bAimed)
                                    {
//                                        _program.Echo("Aimed");
                                        _systemsMonitor.MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustForwardList, thrustBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                                        _wicoControl.WantMedium();
                                        _systemsMonitor.gyrosOff();
                                        bAimed = _systemsMonitor.AlignGyros("up", AsteroidUpVector, _wicoBlockMaster.GetMainController());
                                    }
                                    else
                                    {
//                                        _program.Echo("Not Aimed");
                                        _systemsMonitor.powerDownThrusters();
                                        _wicoControl.WantFast();
                                    }
                                    _drills.turnDrillsOn();
                                    //                                _program.Echo("bAimed=" + bAimed.ToString());
                                    /*
                                    if (!bAimed) _wicoControl.WantFast();
                                    else _wicoControl.WantMedium();
                                    */
                                }
                            }
                            else
                            {
 //                               _program.Echo("No asteroid in front range");
                                // we have nothing in front, but are still close
                                //StatusLog("Exiting Asteroid", textPanelReport);
                                _drills.turnDrillsOff();
                                _systemsMonitor.TurnEjectorsOff(); // don't want stuff hitting us in the butt
                                Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);

                                bAimed = _systemsMonitor.AlignGyros("forward", vAim, _wicoBlockMaster.GetMainController());
                                _systemsMonitor.MoveForwardSlow(fAsteroidExitMps, fAsteroidExitMps * 1.25f, thrustForwardList, thrustBackwardList, _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                                if (bAimed)
                                {
                                    _systemsMonitor.gyrosOff();
                                    bAimed = _systemsMonitor.AlignGyros("up", AsteroidUpVector, _wicoBlockMaster.GetMainController());
                                }

                                if (!bAimed) _wicoControl.WantFast();
                                else _wicoControl.WantMedium();
                            }
                            sbNotices.Append(_oreInfoLocs.OreFoundInfo());
                        }
                        break;

                    case 100:
                        _drills.turnDrillsOff();
                        _sensors.SensorsSleepAll();
                        sb1 = _sensors.GetForwardSensor();
                        _sensors.SensorSetToShip(sb1, 0, 0, 0, 0, 50, 0);
                        _wicoControl.SetState(101);
                        _elapsedTime.RestartTimer(miningElapsed);
                        break;
                    case 101:
                        //                        miningElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (!_elapsedTime.IsActive(miningElapsed))
                        {
                            _elapsedTime.ResetTimer(miningElapsed);
                            _elapsedTime.StartTimer(miningElapsed);
                        }
                        if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait)
                        {
                            sbNotices.AppendLine("Delay for sensor settle");
                            return;
                        }
                        _wicoControl.SetState(iState + 1);
//                        iState++;
                        break;

                    case 102:
                        {
                            aSensors = _sensors.SensorsGetActive();
                            //                        bValidAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                _asteroids.AsteroidProcessLDEI(lmyDEI);
                            }
                            //                        if (!bValidAsteroid)
                            {
                                // not in sensor range. start checking cameras
                                double scandist = 500;
                                /*
                                                            if (bValidAsteroid)
                                                            {
                                                                scandist = (_wicoBlockMaster.GetMainController().GetPosition() - vTargetAsteroid).Length();
                                                            }
                                                            */
                                if (_cameras.CameraForwardScan( scandist))
                                { // we scanned
                                    if (!_cameras.lastDetectedInfo.IsEmpty())
                                    {  // we hit something

                                        if (_cameras.lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                        {
                                            MinerProcessScan(_cameras.lastDetectedInfo);
                                        }
                                    }
                                    else
                                    {
                                        // no asteroid detected.  Check surroundings for one.
                                        _wicoControl.SetState(110);
                                        bValidExit = false;
                                    }
                                }

                            }
                            if (bValidExit) _wicoControl.SetState(120); //found asteroid ahead
                        }
                        break;
                    case 110:
                        // --shouldn't be necessary since we have scans..  but maybe no asteroid in front of aim spot?
                        { // asteroid NOT in front. big sensor search for asteroids in area
                            _program.Echo("set big sensors");

                            _sensors.SensorsSleepAll();
                            _sensors.SensorSetToShip(sb1, 50, 50, 50, 50, 50, 50);
                            _elapsedTime.RestartTimer(miningElapsed);
                            _wicoControl.SetState(111);
                            _program.Echo("iState now=" + iState.ToString());
                        }
                        break;
                    case 111:
                        if (!_elapsedTime.IsActive(miningElapsed))
                        {
                            // should have already been done.
                            _elapsedTime.RestartTimer(miningElapsed);
                        }
                        if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait) return;
                        _wicoControl.SetState(iState + 1);
//                        iState++;
                        break;
                    case 112:
                        { // asteroid not in front. Check sensors
                            aSensors = _sensors.SensorsGetActive();
                            //                        bValidAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                _asteroids.AsteroidProcessLDEI(lmyDEI);
                            }
                            miningAsteroidID = _asteroids.AsteroidFindNearest();
                            if (miningAsteroidID > 0) // return to a known asteroid
                            {
                                /*
                                vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);

                                bValidAsteroid = true;
                                if (vExpectedAsteroidExit == Vector3D.Zero)
                                {
                                    vExpectedAsteroidExit = vTargetAsteroid - _wicoBlockMaster.GetMainController().GetPosition();
                                    vExpectedAsteroidExit.Normalize();
                                }
                                */
                                _wicoControl.SetState(120);
                                _wicoControl.WantFast();
                            }
                            else
                            {
                                _scans.StartScans(iMode, 5); // try again after a scan
                            }
                        }
                        break;


                    case 120:
                        {
                            // we have a known asteroid.  go toward our starting location
                            // wait for ship to slow
                            _program.ResetMotion();
                            bool bReady = _systemsMonitor.AirWorthy(true, false, MiningCargopcthighwater);
                            if (maxDeltaV < fMiningMinThrust || !bReady) //cargopcent > MiningCargopctlowwater || batteryPercentage < batterypctlow)
                            {
                                // we don't have enough oomph to do the bore.  go dock and refill/empty and then try again.
                                _dock.SetRelaunch(true);
                                _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                                _program.ErrorLog("Can't mine: Full");
                                _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                            }
                            else
                                if (_wicoBlockMaster.GetShipSpeed() < fAsteroidApproachMps)
                                {
                                    _wicoControl.SetState(121);
                                    _wicoControl.WantFast();
                                }
                                else _wicoControl.WantMedium();
                        }
                        break;

                    case 121:
                        {
                            // we have a known asteroid.  go toward our starting location

                            double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();
                            _program.Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                            // Go to start location.  possible far travel
                            _wicoControl.WantFast();
                            // always do 'far' travel so we get collision avoidance.
                            if (distanceSQ > 75 * 75)
                            {
                                // do far travel.
                                _wicoControl.SetState(190);
                                return;
                            }
                            _wicoControl.SetState(125);
                            break;
                        }
                    case 125:
                        {
                            sbModeInfo.AppendLine("Move to Bore Start");
                            //StatusLog("Move to Bore Start", textPanelReport);
                            double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();
                            sbNotices.AppendLine("DistanceSQ=" + distanceSQ.ToString("0.0"));
                            _program.Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));

                            double stoppingDistance = _systemsMonitor.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(),thrustBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);

                            if ((distanceSQ - stoppingDistance * 2) > 2)
                            {
                                // set aim to ending location
                                Vector3D vAim = (vAsteroidBoreStart - _wicoBlockMaster.CenterOfMass());
                                bool bAimed = _systemsMonitor.AlignGyros("forward", vAim, _wicoBlockMaster.GetMainController());
                                if (bAimed)
                                {
                                    if(_cameras.CameraForwardScan(vAsteroidBoreStart))
                                    {
                                        if(!_cameras.lastDetectedInfo.IsEmpty())
                                        {
                                            // we hit something
                                            // use NAV movement to get collision avoidance.
                                            _wicoControl.SetMode(190);
                                            _wicoControl.WantFast();
                                            return;
                                        }
                                    }
                                    //                                _program.Echo("Aimed");
                                    //bWantFast = false;
                                    _wicoControl.WantMedium();
                                    _systemsMonitor.MoveForwardSlow(fAsteroidApproachMps, fAsteroidApproachAbortMps, thrustForwardList, thrustBackwardList,
                                        _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                                }
                                else _wicoControl.WantFast();
                            }
                            else
                            {
                                // we have arrived
                                _program.ResetMotion();
                                _wicoControl.SetState(130);
                                _wicoControl.WantFast();
                            }
                            break;
                        }
                    case 130:
                        _program.Echo("Sensor Set");
                        _sensors.SensorsSleepAll();
                        _sensors.SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                        _sensors.SensorSetToShip(sb2, 1, 1, 1, 1, 15, -1);
                        _elapsedTime.RestartTimer(miningElapsed);
                        //first do a rotate to 'up'...
                        _wicoControl.SetState(131);
                        _wicoControl.WantFast();
                        break;
                    case 131:
                        {
                            sbModeInfo.AppendLine("Borehole Alignment");
                            //StatusLog("Borehole Alignment", textPanelReport);
                            if (_wicoBlockMaster.GetShipSpeed() < 0.5)
                            { // wait for ship to slow down
//                                double distanceSQ = (vAsteroidBoreStart - _wicoBlockMaster.CenterOfMass()).LengthSquared();
                                {
                                    // align with 'up'
                                    _wicoControl.WantFast();
                                    if (_systemsMonitor.AlignGyros("up", AsteroidUpVector, _wicoBlockMaster.GetMainController()))
                                    {
                                        _wicoControl.SetState(134);
                                    }
                                }
                            }
                            else
                            {
                                _program.ResetMotion();
                                _wicoControl.WantMedium();
                            }
                            break;
                        }
                    case 134:
                        {
                            // align with target
                            sbModeInfo.AppendLine("Borehole Alignment");
                            //StatusLog("Borehole Alignment", textPanelReport);
                            _wicoControl.WantFast();
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            if (_systemsMonitor.AlignGyros("forward", vAim, _wicoBlockMaster.GetMainController()))
                            {
                                _wicoControl.SetState(137);
                            }
                            break;
                        }
                    case 137:
                        {
                            // align with 'up'
                            sbModeInfo.AppendLine("Borehole Alignment");
                            //StatusLog("Borehole Alignment", textPanelReport);
                            _wicoControl.WantFast();
                            if (_systemsMonitor.AlignGyros("up", AsteroidUpVector, _wicoBlockMaster.GetMainController()))
                            {
                                _wicoControl.SetState(140);
                            }
                            break;
                        }

                    case 140:
                        { // bore scan init
                            sbModeInfo.AppendLine("Bore Check Init");
                            //StatusLog("Bore Check Init", textPanelReport);
                            _program.Echo("Bore Check Init");
                            BoreHoleScanMode = -1;
                            _wicoControl.WantFast();
                            // we should have asteroid in front.
                            bool bAimed = true;
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            bAimed = _systemsMonitor.AlignGyros("forward", vAim, _wicoBlockMaster.GetMainController());

//                            miningElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                            if (bAimed)
                            {
                                OrientedBoundingBoxFaces orientedBoundingBox = new OrientedBoundingBoxFaces(_wicoBlockMaster.GetMainController());
                                orientedBoundingBox.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, BoreScanFrontPoints);
                                // front output order is BL, BR, TL, TR

                                // NOTE: This may be wrong order for opposite facing bores..
                                Vector3D vTL = vAsteroidBoreStart + AsteroidUpVector * MiningBoreHeight / 2 - AsteroidRightVector * MiningBoreWidth / 2;
                                Vector3D vTR = vAsteroidBoreStart + AsteroidUpVector * MiningBoreHeight / 2 + AsteroidRightVector * MiningBoreWidth / 2;
                                Vector3D vBL = vAsteroidBoreStart - AsteroidUpVector * MiningBoreHeight / 2 - AsteroidRightVector * MiningBoreWidth / 2;
                                Vector3D vBR = vAsteroidBoreStart - AsteroidUpVector * MiningBoreHeight / 2 + AsteroidRightVector * MiningBoreWidth / 2;

                                BoreScanFrontPoints[0] = vBL;
                                BoreScanFrontPoints[1] = vBR;
                                BoreScanFrontPoints[2] = vTL;
                                BoreScanFrontPoints[3] = vTR;
                                _wicoControl.SetState(145);
                                //                            _wicoControl.SetState(143; //Beam testing
                            }
                            else
                            {
                                _wicoControl.WantFast();
                                _program.Echo("Aiming");
                            }
                        }
                        break;
                    case 143:
                        {
                            // TESTING..  beam-rider testing
                            //                        Vector3D vCrossStart = Vector3D.Cross(vAsteroidBoreStart- ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass, _wicoBlockMaster.GetMainController().WorldMatrix.Backward);
                            //                        Vector3D vCrossEnd = Vector3D.Cross((vAsteroidBoreEnd - vAsteroidBoreStart), _wicoBlockMaster.GetMainController().WorldMatrix.Right);

                            //https://answers.unity.com/questions/437111/calculate-vector-exactly-between-2-vectors.html
                            Vector3D vBoreEnd = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            //                        vBoreEnd.Normalize();
                            _program.Echo("BoreEnd=" + _program.Vector3DToString(vBoreEnd));
                            Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                            //Vector3D vAimEnd = _wicoBlockMaster.GetMainController().WorldMatrix.Forward;
                            //                        vAimEnd.Normalize();
                            _program.Echo("AimEnd=" + _program.Vector3DToString(vAimEnd));
                            //                        Vector3D vCrossEnd = (vBoreEnd - vAimEnd * 2);
                            Vector3D vRejectEnd = _systemsMonitor.VectorRejection(vBoreEnd, vAimEnd);
                            //                        vCrossEnd.Normalize();

                            _program.Echo("CrossEnd=" + _program.Vector3DToString(vRejectEnd));
                            //                        Vector3D vCrossEnd = (vBoreEnd + vAimEnd) / 2;
                            //debugGPSOutput("CrossStartEnd", vAsteroidBoreStart + vRejectEnd);
                            //                        //debugGPSOutput("CrossEndStart", vAsteroidBoreEnd - vCrossStart);
                            //                        //debugGPSOutput("COM", ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                            //                        _program.Echo("CrossStart=" + _program.Vector3DToString(vCrossStart));


                            //                        _program.Echo("FW=" + _program.Vector3DToString(_wicoBlockMaster.GetMainController().WorldMatrix.Forward));
                            //                          _cameras.CameraForwardScan(vAsteroidBoreEnd + vCrossEnd);
                            //                        doCameraScan(cameraBackwardList, vAsteroidBoreStart + vCrossStart);
                            Vector3D vCorrectedAim = vAsteroidBoreEnd - vRejectEnd;
                            _cameras.CameraForwardScan( vCorrectedAim);
                            //                        doCameraScan(cameraBackwardList, vAsteroidBoreStart - vCrossEnd);
                            _wicoControl.WantMedium();
                        }
                        break;
                    case 145:
                        { // bore scan
                            sbModeInfo.AppendLine("Bore Check Scan");
                            //StatusLog("Bore Check Scan", textPanelReport);
                            _program.Echo("Bore Check Scan");
                            if(!_elapsedTime.IsActive(miningElapsed))
                            {
                                _elapsedTime.ResetTimer(miningElapsed);
                                _elapsedTime.StartTimer(miningElapsed);
                            }

                            // we should have asteroid in front.
                            bool bAimed = true;
                            bool bAsteroidInFront = false;
                            bool bFoundCloseAsteroid = false;

                            //                        _program.Echo(bValidExit.ToString() + " " + _program.Vector3DToString(vExpectedAsteroidExit));
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            bAimed = _systemsMonitor.AlignGyros("forward", vAim, _wicoBlockMaster.GetMainController());

                            if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait)
                            {
                                _program.Echo("Waiting for sensor settle:"+ _elapsedTime.GetElapsed(miningElapsed).ToString("0.0")+"/"+ _sensors.SensorSettleWait);
                                _wicoControl.WantMedium();
                                return;
                            }

                            if (bAimed)
                            {
                                bool bLarge = false;
                                bool bSmall = false;
                                _sensors.SensorIsActive(sb1, ref bAsteroidInFront, ref bLarge, ref bSmall);
                                _sensors.SensorIsActive(sb2, ref bFoundCloseAsteroid, ref bLarge, ref bSmall);

                                /* TEST: Always do the scans
                                if (bFoundCloseAsteroid || bAsteroidInFront)
                                {
                                    // sensor is active.. go forth and mine
                                    _wicoControl.SetState(150;
                                    _wicoControl.WantFast();
                                }
                                else 
                                */
                                {
                                    _wicoControl.WantMedium();
                                    // try a raycast to see if we can hit anything
                                    if (_cameras.HasForwardCameras())
                                    {
                                        // no cameras to scan with.. assume
                                        _wicoControl.SetState(150);
                                        _wicoControl.WantFast();
                                        return;
                                    }
                                    //                                _program.Echo("BoreHoleScanMode=" + BoreHoleScanMode);

                                    double scanDistance = (_wicoBlockMaster.GetMainController().GetPosition() - vAsteroidBoreEnd).Length();
                                    bool bDidScan = false;
                                    Vector3D vTarget;
                                    if (BoreHoleScanMode < 0) BoreHoleScanMode = 0;
                                    if (BoreHoleScanMode > 21)
                                    {
                                        // we have scanned all of the areas and not hit anyhing..  skip this borehole
                                        AsteroidDoNextBore(iMode);
                                        BoreHoleScanMode = 0;
                                        return;
                                    }
                                    Vector3D vScanVector = (vAsteroidBoreEnd - vAsteroidBoreStart);
                                    vScanVector.Normalize();
                                    for (int i1 = 0; i1 < 4; i1++)
                                    {
                                        //debugGPSOutput("Points" + i1, BoreScanFrontPoints[i1]);
                                    }
                                    switch (BoreHoleScanMode)
                                    {
                                        case 0:
                                            if (_cameras.CameraForwardScan(vAsteroidBoreEnd))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 1:
                                            vTarget = BoreScanFrontPoints[2] + vScanVector * scanDistance;
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 2:
                                            vTarget = BoreScanFrontPoints[3] + vScanVector * scanDistance;
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 3:
                                            vTarget = BoreScanFrontPoints[0] + vScanVector * scanDistance;
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 4:
                                            vTarget = BoreScanFrontPoints[1] + vScanVector * scanDistance;
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 5:
                                            /*
                                            // check center again.  always full length
                                            if (_cameras.CameraForwardScan(scanDistance))
                                            {
                                                bDidScan = true;
                                            }
                                            */
                                            bDidScan = true;
                                            break;
                                        case 6:
                                            vTarget = BoreScanFrontPoints[2] + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 7:
                                            vTarget = BoreScanFrontPoints[3] + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 8:
                                            vTarget = BoreScanFrontPoints[0] + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 9:
                                            vTarget = BoreScanFrontPoints[1] + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        // front output order is 0=BL, 1=BR, 2=TL, 3=TR
                                        case 10:
                                            // bottom middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 11:
                                            // top middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 12:
                                            // right middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 13:
                                            // left middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 14:
                                            vTarget = BoreScanFrontPoints[2] + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 15:
                                            vTarget = BoreScanFrontPoints[3] + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 16:
                                            vTarget = BoreScanFrontPoints[0] + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 17:
                                            vTarget = BoreScanFrontPoints[1] + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        // front output order is 0=BL, 1=BR, 2=TL, 3=TR
                                        case 18:
                                            // bottom middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 19:
                                            // top middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 20:
                                            // right middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 21:
                                            // left middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (_cameras.CameraForwardScan(vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                    }
                                    if (bDidScan)
                                    {
                                        // the camera scan routine sets lastDetetedInfo itself if scan succeeds
                                        if (!_cameras.lastDetectedInfo.IsEmpty())
                                        {
                                            if (_cameras.lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                            {
                                                //                                            sStartupError += "BoreScan hit on " + BoreHoleScanMode + "\n";
                                                // we found an asteroid. (hopefully it's ours..)
                                                bFoundCloseAsteroid = true;
                                            }
                                            else if (_cameras.lastDetectedInfo.Type == MyDetectedEntityType.FloatingObject)
                                            {
                                                //                                           _program.Echo("Found NON-Asteroid in SCAN");
                                                // don't count raycast if we hit debris.
                                                bDidScan = false;
                                            }
                                            else
                                            {
                                                // something else.. maybe ourselves or another ship?

                                            }

                                            if (bFoundCloseAsteroid)
                                            {
                                                // we found an asteroid (fragment).  go get it.
                                                _wicoControl.SetState(150);
                                                _wicoControl.WantFast();
                                            }
                                        }
                                        else
                                        {
                                        }
                                        if (bDidScan) BoreHoleScanMode++;
                                    }
                                    else
                                    {
                                        _program.Echo("Awaiting Available raycast");
                                    }

                                }
                            }
                            else
                            {
                                _wicoControl.WantFast();
                                _program.Echo("Aiming");
                            }
                        }
                        break;
                    case 150:
                        { // approach
                            sbModeInfo.AppendLine("Asteroid Approach");
                            //StatusLog("Asteroid Approach", textPanelReport);
                            _program.Echo("Asteroid Approach");
                            if (!_elapsedTime.IsActive(miningElapsed))
                            {
                                _elapsedTime.ResetTimer(miningElapsed);
                                _elapsedTime.StartTimer(miningElapsed);
                            }

                            _wicoControl.WantSlow();
                            // we should have asteroid in front.
                            bool bAimed = true;
                            bool bAsteroidInFront = false;
                            bool bFoundCloseAsteroid = false;

                            bAimed = _systemsMonitor.BeamRider(vAsteroidBoreStart, vAsteroidBoreEnd, _wicoBlockMaster.GetMainController());
                            if (bAimed)
                            {
//                                _program.Echo("Riding beam: adjust up");
                                _systemsMonitor.gyrosOff();
                                //                                _program.Echo("up="+_program.Vector3DToString(AsteroidUpVector));
                                _systemsMonitor.SetMinAngle(0.05f);
                                bAimed = _systemsMonitor.AlignGyros("up", AsteroidUpVector, _wicoBlockMaster.GetMainController());
                                if (bAimed) _program.Echo("Am Up");
                            }

                            double distance = (vAsteroidBoreStart - _wicoBlockMaster.GetMainController().GetPosition()).Length();
                            _program.Echo("Distance=" + _program.niceDoubleMeters(distance) + " (" + _program.niceDoubleMeters(AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2) + ")");
                            //StatusLog("Distance=" +_program.niceDoubleMeters(distance) + " (" +_program.niceDoubleMeters(AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2) + ")", textPanelReport);
                            double stoppingDistance = _systemsMonitor.calculateStoppingDistance(_wicoBlockMaster.GetPhysicalMass(), thrustBackwardList, _wicoBlockMaster.GetShipSpeed(), 0);

                            if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait)
                            {
                                _program.Echo("Sensor Settle");
                                _wicoControl.WantMedium();
                                return;
                            }

                            if (bAimed)
                            {
                                _program.Echo("Aimed2");
                                bool bLarge = false;
                                bool bSmall = false;
                                //                            SensorIsActive(sb1, ref bAsteroidInFront, ref bLarge, ref bSmall);
                                _sensors.SensorIsActive(sb2, ref bFoundCloseAsteroid, ref bLarge, ref bSmall);
                                //
//                                bWantFast = false;
                                _wicoControl.WantMedium();

                                // we already verified that there is asteroid in this bore.. go get it.
                                bAsteroidInFront = true;

                                if (bFoundCloseAsteroid)
                                {
                                    _systemsMonitor.powerDownThrusters();
                                    _wicoControl.SetState(31);
                                }
                                else if (bAsteroidInFront)
                                {
                                    if ((distance + stoppingDistance) > (AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2))
                                    {
                                        // we have gone too far.  nothing to mine
                                        _wicoControl.SetState(155);
                                    }
                                    else
                                    {
                                        _systemsMonitor.MoveForwardSlow(fAsteroidApproachMps, fAsteroidApproachAbortMps, thrustForwardList, thrustBackwardList,
                                            _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                                    }
                                }
                            }
                            else
                            {
                                _wicoControl.WantFast();
                                _program.Echo("Aiming");

                                // TODO: Needs timeout (we already have miningElapsed)
                                if ((distance + stoppingDistance) > (AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2))
                                {
                                    // we have gone too far.  nothing to mine
                                    //                                    sStartupError += "\nTOO FAR! ("+AsteroidCurrentX+"/"+AsteroidCurrentY+")";
                                    _wicoControl.SetState(155);
                                }
                            }
                        }
                        break;
                    case 155:
                        {
                            _program.ErrorLog("155:Move Next Bore");
                            _program.ResetMotion();
                            //                        if (_wicoBlockMaster.GetShipSpeed() < 1)
                            AsteroidDoNextBore(iMode);
                        }
                        break;
                    case 190:
                        {
                            sbModeInfo.AppendLine("Start Nav Travel");
                            // start NAV travel
                            _navCommon.NavGoTarget(vAsteroidBoreStart, iMode, 195, 11, "MINE-Bore start");
                        }
                        break;
                    case 195:
                        {// we have 'arrived' from far travel
                            sbModeInfo.AppendLine("NAV Completed: Awaiting Slow");
                            // wait for motion to slow
                            _wicoControl.WantMedium();
                            if (_wicoBlockMaster.GetShipSpeed() < fAsteroidApproachMps)
                            {
                                _wicoControl.SetState(120);
                                _wicoControl.WantFast();
                            }
                            _program.ResetMotion();
                            break;
                        }
                    case 300:
                        {
                            // we have exitted the asteroid.  Prepare for another run or to go dock
                            sbModeInfo.AppendLine("Exitted Asteroid");
                            _program.Echo("Exitted!");
                            _program.ResetMotion();
                            _sensors.SensorsSleepAll();
                            _systemsMonitor.TurnEjectorsOn();
                            _wicoControl.WantMedium();
                            _wicoControl.SetState(305);
                            break;
                        }
                    case 305:
                        {
                            _wicoControl.WantMedium();
                            if (_wicoBlockMaster.GetShipSpeed() > 1) return;

                            if (AsteroidMineMode == 1)
                            {
                                // we did a single bore.
                                // so now we are done.
                                AsteroidMineMode = 0; // reset to default
                                _dock.SetRelaunch(false);
                                _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                                miningAsteroidID = -1;
                                break;
                            }
                            // else mine mode !=1
                            bool bReady = _systemsMonitor.AirWorthy(true, false, MiningCargopcthighwater);
                            if (maxDeltaV < fMiningMinThrust || !bReady) //cargopcent > MiningCargopctlowwater || batteryPercentage < batterypctlow)
                            {
                                bool bBoresRemaining = AsteroidCalculateNextBore();
                                if (bBoresRemaining)
                                {
                                    _dock.SetRelaunch(true);
                                    _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                                    _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                                }
                                else
                                {
                                    _program.ErrorLog("No Bores Remaining; Return to dock");
                                    _wicoControl.SetState(500); // search for any remaining voxels..
                                }
                            }
                            else
                            {
                                // UNLESS: we have another target asteroid..
                                // TODO: 'recall'.. but code probably doesn't go here.
                                AsteroidDoNextBore(iMode);
                            }
                        }
                        break;
                    case 310:
                        {
                            AsteroidCalculateBestStartEnd();
                            //                        vAsteroidBoreStart = AsteroidCalculateBoreStart();
                            sbModeInfo.AppendLine("Start NAV to Next Bore start");
                            _navCommon.NavGoTarget(vAsteroidBoreStart, iMode, 120, 11, "MINE-Next Bore start");
                            break;

                        }
                    case 500:
                        {
                            // TODO: do a final search pass for any missed voxels.
                            // TODO: remove asteroid from announced lists after final pass

                            // Go home.
                            _dock.SetRelaunch(false);
                            _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                            miningAsteroidID = -1;
                            break;
                        }
                }
            }

            void doModeGotoOre()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("MINE: Go To Ore");

                /*
                 List<IMySensorBlock> aSensors = null;
                 IMySensorBlock sb;
                 */
                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":GotoOre!", textPanelReport);
                _program.Echo("GOTO ORE:iState=" + iState.ToString());
                if (thrustForwardList.Count < 1)
                {
                    _systemsMonitor.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }
                double effectiveMass = _wicoBlockMaster.GetPhysicalMass();

                double maxThrust = _systemsMonitor.calculateMaxThrust(thrustForwardList);

//                _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));
//                sbNotices.AppendLine("effectiveMass=" + effectiveMass.ToString("N0"));
//                _program.Echo("maxThrust=" + maxThrust.ToString("N0"));

//                double maxDeltaV = (maxThrust) / effectiveMass;

//                _program.Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
//                _program.Echo("Cargo=" + _ores.cargopcent.ToString() + "%");
//                sbNotices.AppendLine("Cargo=" + _ores.cargopcent.ToString() + "%");

//                _program.Echo("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
//                sbNotices.AppendLine("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
//                _program.Echo("#Sensors=" + _sensors.sensorsList.Count);
//                _program.Echo("width=" + _wicoBlockMaster.WidthInMeters().ToString("0.0"));
//                _program.Echo("height=" + _wicoBlockMaster.HeightInMeters().ToString("0.0"));
//                _program.Echo("length=" + _wicoBlockMaster.LengthInMeters().ToString("0.0"));

                //            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
                //            //StatusLog("clear", txtPanel);

            }

            void doModeMineSingleBore()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("MINE: Single Bore");

                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":MINE Line", textPanelReport);
                if (thrustForwardList.Count < 1)
                {
                    _systemsMonitor.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }
                _program.Echo("MINE:iState=" + iState.ToString());
                double maxThrust = _systemsMonitor.calculateMaxThrust(thrustForwardList);
                IMySensorBlock sb1 = null;
                IMySensorBlock sb2 = null;
                //            _program.Echo("maxThrust=" + maxThrust.ToString("N0"));

                double effectiveMass = _wicoBlockMaster.GetPhysicalMass();
                //            _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;
                _program.Echo("Our Asteroid=" + miningAsteroidID.ToString());
                if (_sensors.GetCount() >= 2)
                {
                    sb1 = _sensors.GetForwardSensor(0);// sensorsList[0];
                    sb2 = _sensors.GetForwardSensor(1);//sensorsList[1];
                }
                switch (iState)
                {
                    case 0:
                        bValidExit = false;
                        bMiningWaitingCargo = false;

                        _program.ResetMotion();
                        _systemsMonitor.TurnEjectorsOff();
                        _ores.doCargoCheck();
                        _ores.OreDoCargoCheck(true); // init ores to what's currently in inventory
                        MinerCalculateBoreSize();
                        _systemsMonitor.MoveForwardSlowReset();

                        _wicoControl.WantFast();

                        if (_sensors.GetCount() < 2)
                        {
                            //StatusLog(OurName + ":" + moduleName + " Find Ore: Not Enough Sensors!", textLongStatus, true);
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                            return;
                        }
                        sb1.DetectAsteroids = true;
                        sb2.DetectAsteroids = true;

                        // Can we turn in our own tunnel?
                        if (_wicoBlockMaster.LengthInMeters() > _wicoBlockMaster.WidthInMeters() && _wicoBlockMaster.LengthInMeters() > _wicoBlockMaster.HeightInMeters())
                            bBoringOnly = true;
                        else bBoringOnly = false;

                        miningAsteroidID = 0;
 //                       _wicoControl.SetState(1;
                        _wicoControl.SetMode( WicoControl.MODE_MINE,1);
                        AsteroidMineMode = 1;// drill exactly where we're aimed for.
                        _wicoControl.WantFast();
                        break;
                }
            }

            void doModeExitingAsteroid()
            {
                int iMode= _wicoControl.IMode;
                int iState=_wicoControl.IState;
                List<IMySensorBlock> aSensors = null;
                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("MINE: Exit Asteroid");

                /*
                IMySensorBlock sb;
                */

                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":Exiting!", textPanelReport);
                _program.Echo("Exiting: iState=" + iState.ToString());
                if (thrustForwardList.Count < 1)
                {
                    _systemsMonitor.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }
                double effectiveMass = _wicoBlockMaster.GetPhysicalMass();

                double maxThrust = _systemsMonitor.calculateMaxThrust(thrustForwardList);

                //            _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));
                //            _program.Echo("maxThrust=" + maxThrust.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;

                _program.Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
                //StatusLog("Max DeltaV=" + maxDeltaV.ToString("N1") + " / " + fMiningMinThrust.ToString("N1") + "min", textPanelReport);
                if (iState > 0)
                {
                    // Does it's own internal checking for only running every so often.
                        _systemsMonitor.AirWorthy(false, false,MiningCargopcthighwater); // does the current value checks.
                }

                _program.Echo("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
                sbNotices.AppendLine("velocity=" + _wicoBlockMaster.GetShipSpeed().ToString("0.00"));
                _program.Echo("Boring=" + bBoringOnly.ToString());
                //            _program.Echo("#Sensors=" + sensorsList.Count);
                //            _program.Echo("width=" + _wicoBlockMaster.WidthInMeters().ToString("0.0"));
                //            _program.Echo("height=" + _wicoBlockMaster.HeightInMeters().ToString("0.0"));
                //            _program.Echo("length=" + _wicoBlockMaster.LengthInMeters().ToString("0.0"));

                //            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
                //            //StatusLog("clear", txtPanel);

                /*
                 * 0 - Master Init
                 * 10 - Init sensors, turn drills on
                 * 11 - await sensor set
                 * Turn by quarters.  Left, then Left until about 180   
                 * 20 - turn around until aimed 
                 * and them move forward until exittedasteroid ->40
                 * 
                 * 40 when out, call for pickup ->50
                 * 50 start docking procedure.
                 * 
                 */

                switch (iState)
                {
                    case 0: //0 - Master Init
                        if (_wicoBlockMaster.LengthInMeters() > _wicoBlockMaster.WidthInMeters() && _wicoBlockMaster.LengthInMeters() > _wicoBlockMaster.HeightInMeters())
                            bBoringOnly = true;
                        else bBoringOnly = true;
                        //                    else bBoringOnly = false;
                        _wicoControl.WantMedium();
                        _wicoControl.SetState(10);
                        _program.ResetMotion();
                        //                    turnDrillsOff();
                        _systemsMonitor.TurnEjectorsOn();
                        if (_sensors.GetCount() < 2)
                        {
                            //StatusLog(OurName + ":" + moduleName + " Exit Asteroid: Not Enough Sensors!", textLongStatus, true);
                            _program.ErrorLog("Not enough sensors found!");
                            _elapsedTime.ResetTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        }
                        break;
                    case 10://10 - Init sensors, 
                        _wicoControl.WantMedium();
                        _sensors.SensorsSleepAll();
                        if (bBoringOnly)
                        {
                            _sensors.SensorSetToShip(sb1, (float)_wicoBlockMaster.WidthInMeters(), (float)_wicoBlockMaster.WidthInMeters(),
                                (float)_wicoBlockMaster.HeightInMeters(), (float)_wicoBlockMaster.HeightInMeters(),
                                (float)_wicoBlockMaster.LengthInMeters(), 1);
                            _sensors.SensorSetToShip(sb2, 0, 0, 0, 0, -1, 50);
                        }
                        else
                        {
                            _sensors.SensorSetToShip(sb1, (float)_wicoBlockMaster.WidthInMeters(), (float)_wicoBlockMaster.WidthInMeters(),
                                (float)_wicoBlockMaster.HeightInMeters(), (float)_wicoBlockMaster.HeightInMeters(),
                                1, (float)_wicoBlockMaster.LengthInMeters());

                        }

                        //                    SensorSetToShip(sensorsList[0], 2, 2, 2, 2, 15, 15);
                        //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                        _elapsedTime.RestartTimer(miningElapsed);
                        _wicoControl.SetState(11);
                        break;
                    case 11://11 - await sensor set
                        _wicoControl.WantMedium();
                        sbModeInfo.AppendLine("Awaiting Sensor Set");
                        if (!_elapsedTime.IsActive(miningElapsed))
                        {
                            _elapsedTime.ResetTimer(miningElapsed);
                            _elapsedTime.StartTimer(miningElapsed);
                        }
                        if (_elapsedTime.GetElapsed(miningElapsed) < _sensors.SensorSettleWait) return;
                        if (bBoringOnly)
                            _wicoControl.SetState(30);
                        else
                            _wicoControl.SetState(20);
                        break;
                    case 20: //20 - turn around until aimed ->30
                        {
                            sbModeInfo.AppendLine("Turning in hole");
                            _wicoControl.WantFast();
                            _drills.turnDrillsOn();

                            // we want to turn on our horizontal axis as that should be the 'wide' one.
                            bool bAimed = false;
                            double yawangle = -999;
                            //                        _program.Echo("vTarget=" + _program.Vector3DToString(vLastAsteroidContact));
                            yawangle = _systemsMonitor.CalculateYaw(vAsteroidBoreStart, _wicoBlockMaster.GetMainController());
//                            _program.Echo("yawangle=" + yawangle.ToString());
                            double aYawAngle = Math.Abs(yawangle);
                            bAimed = aYawAngle < .05;

                            // turn slower when >180 since we are in tunnel and drills are cutting a path
                            float maxYPR = _systemsMonitor.MaxYPR;
//                            _program.Echo("maxYPR=" + maxYPR.ToString("0.00"));
                            if (aYawAngle > 1.0) maxYPR = maxYPR / 3;

                            _systemsMonitor.DoRotate(yawangle, "Yaw", maxYPR, 0.33f);

                            if (bAimed)
                            {
                                _wicoControl.SetState(30);
                            }
                        }
                        break;
                    case 30:
                        {
                            sbModeInfo.AppendLine("Moving out");
                            bool bAimed = false;
                            string sOrientation = "backward";
                            if (bBoringOnly)
                            {
                                sOrientation = "forward";
                            }
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            bAimed = _systemsMonitor.AlignGyros(sOrientation, vAim, _wicoBlockMaster.GetMainController());

                            bool bLocalAsteroid = false;
                            aSensors = _sensors.SensorsGetActive();
                            List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                //                            _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                s.DetectedEntities(lmyDEI);
                                if (_asteroids.AsteroidProcessLDEI(lmyDEI))
                                    bLocalAsteroid = true;
                            }
                            if (bAimed)
                            {
                                float fTargetMps = fTargetMiningMps;
                                float fAbortMps = fMiningAbortMps;

                                if (bBoringOnly && sb2 != null)
                                {
                                    sb2.DetectedEntities(lmyDEI);
                                    if (!_asteroids.AsteroidProcessLDEI(lmyDEI))
                                    {
                                        // we are backing up and it is clear behind us.
                                        fTargetMps = fAsteroidExitMps;
                                        fAbortMps = fAsteroidExitMps * 1.25f;

                                    }
                                }
                                _wicoControl.WantMedium();
                                if (bBoringOnly)
                                {
                                    _systemsMonitor.MoveForwardSlow(fTargetMps, fAbortMps, thrustBackwardList, thrustForwardList,
                                        _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                                }
                                else
                                {
                                    _systemsMonitor.MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustForwardList, thrustBackwardList,
                                        _wicoBlockMaster.GetPhysicalMass(), _wicoBlockMaster.GetShipSpeed());
                                }
                            }
                            else _wicoControl.WantFast();
                            if (!bLocalAsteroid)
                            {
                                _program.ResetMotion();
                                _sensors.SensorsSleepAll();
                                _wicoControl.SetState(40);
                            }
                            break;
                        }
                    case 40://40 when out, call for pickup
                        {
                            _drills.turnDrillsOff();

                            _wicoControl.SetState(50);
                            _wicoControl.WantFast();
                            /*

                            // todo: if on near side, just go docking.f
                            int iSign = Math.Sign(AsteroidCurrentY);
                            if (iSign == 0) iSign = 1;
                            Vector3D vTop = AsteroidPosition + AsteroidUpVector * AsteroidDiameter * iSign * 1.25;
                            // TODO: if on 'near' side', just go dock... but we don't know where dock is...

    //                        double distanceSQ = (vStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();

                            NavGoTarget(vTop, iMode, 50, 10);
                            */
                        }
                        break;
                    case 50:
                        {
                            sbModeInfo.AppendLine("Starting DOCK request");
                            _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;

                            _navCommon.NavReset();

                            // we should probably give hint to docking as to WHY we want to dock..
                            _dock.SetRelaunch(true);
                            _navCommon.NavQueueMode(WicoControl.MODE_DOCKING);

                            // Or maybe we need to tell docking to change mode when relaunched?
                            _navCommon.NavQueueMode(WicoControl.MODE_MINE);


                            _navCommon.NavStartNav();

                            //                          _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                            break;
                        }
                    default:
                        {
                            _program.Echo("UNKNOWN STATE!");
                            break;
                        }
                }
            }

            void AsteroidCalculateFirstBore()
            {
                AsteroidCurrentX = 0;
                AsteroidCurrentY = 0;
                MinerCalculateAsteroidVector(miningAsteroidID);
                AsteroidCalculateBestStartEnd();
            }

            void AsteroidCalculateBestStartEnd()
            {
                // calculate bore start and end.
                MinerCalculateBoreSize();

                int iSign = 0;
                // calculate the offset value.

                Vector3D vXOffset = AsteroidRightVector * AsteroidCurrentX * MiningBoreWidth;
                // add offset size of 0,0 opening
                iSign = Math.Sign(AsteroidCurrentX);
                if (iSign == 0) iSign = 1;
                //            if (AsteroidCurrentX != 0) vXOffset += AsteroidRightVector * iSign * MiningBoreWidth/2;

                // calculate the offset value.
                Vector3D vYOffset = AsteroidUpVector * AsteroidCurrentY * MiningBoreHeight;
                // offset size of 0,0 opening
                iSign = Math.Sign(AsteroidCurrentY);
                if (iSign == 0) iSign = 1;
                //            if (AsteroidCurrentY != 0) vYOffset += AsteroidUpVector * iSign * MiningBoreHeight/2;


                Vector3D vStart = AsteroidPosition + vXOffset + vYOffset;
                vStart += -AsteroidOutVector * (AsteroidDiameter / 2 + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale);

                Vector3D vEnd = AsteroidPosition + vXOffset + vYOffset;
                vEnd += AsteroidOutVector * (AsteroidDiameter / 2 + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale);

                vAsteroidBoreEnd = vEnd;
                vAsteroidBoreStart = vStart;
                //            vAsteroidBoreEnd = AsteroidCalculateBoreEnd();
                //            vAsteroidBoreStart = AsteroidCalculateBoreStart();

                // calculate order
                double dStart = (vAsteroidBoreStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();
                double dEnd = (vAsteroidBoreEnd - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();
                //            bAsteroidBoreReverse = false;

                // reverse so start is the closest.
                if (dEnd < dStart)
                {
                    //                bAsteroidBoreReverse = true;
                    Vector3D vTmp = vAsteroidBoreStart;
                    vAsteroidBoreStart = vAsteroidBoreEnd;
                    vAsteroidBoreEnd = vTmp;
                }
                //            vExpectedAsteroidExit = vAsteroidBoreStart - vAsteroidBoreEnd;
            }

            bool AsteroidCalculateNextBore()
            {
                // TODO: handle other mine modes like spread out
                if (AsteroidMineMode == 1) return false; // there are no next bores :)

                bool bAsteroidDone = false;

                if (AsteroidCurrentX == 0 && AsteroidCurrentY == 0)
                {
                    AsteroidCurrentX = 1;
                    AsteroidCalculateBestStartEnd();
                    return true;
                }

                if (AsteroidCurrentX > 0)
                {
                    if (AsteroidCurrentX >= AsteroidMaxX)
                    {
                        AsteroidCurrentX = 0;
                        bAsteroidDone = AsteroidCalculateNextRow();
                    }
                    else
                    { // make it negative.
                        AsteroidCurrentX = -AsteroidCurrentX;
                    }
                }
                else// if (AsteroidCurrentX < 0)
                {
                    // make it positive
                    AsteroidCurrentX = Math.Abs(AsteroidCurrentX);
                    AsteroidCurrentX++;
                }
                if (Math.Abs(AsteroidCurrentX) >= AsteroidMaxX)
                {
                    bAsteroidDone = AsteroidCalculateNextRow();
                }
                AsteroidCalculateBestStartEnd();
                return true;
            }

            bool AsteroidCalculateNextRow()
            {
                AsteroidCurrentX = 0;
                if (AsteroidCurrentY == 0)
                    AsteroidCurrentY = 1;
                else
                {
                    if (AsteroidCurrentY > 0)
                    {
                        if (AsteroidCurrentY >= AsteroidMaxY)
                        {
                            // We are done with asteroid.
                            AsteroidCurrentY = 0;
                            return false;
                        }
                        AsteroidCurrentY = -AsteroidCurrentY;

                    }
                    else
                    { // back to positive
                        AsteroidCurrentY = -AsteroidCurrentY;
                        // and increment
                        AsteroidCurrentY++;
                    }
                }
                return true;

            }

            void AsteroidDoNextBore(int iMode)
            {
                if (!AsteroidCalculateNextBore())
                {
                    // we are done with asteroid.
                    // TODO: do a final search pass for any missed voxels
                    // TODO: remove asteroid
                    _dock.SetRelaunch(false);
                    miningAsteroidID = -1;
                    AsteroidMineMode = 0;
                    _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                }
                else
                {
                    _navCommon.NavGoTarget(vAsteroidBoreStart, iMode, 120, 12, "Asteroid-Next Bore");
                }
            }
            void MinerCalculateBoreSize()
            {
                MiningBoreHeight = _program._CustomDataIni.Get(sMiningSection, "MiningBoreHeight").ToDouble(MiningBoreHeight);
                MiningBoreWidth = _program._CustomDataIni.Get(sMiningSection, "MiningBoreWidth").ToDouble(MiningBoreWidth);

                if (MiningBoreHeight <= 0)
                {
                    MiningBoreHeight = (_wicoBlockMaster.HeightInMeters());
                    MiningBoreWidth = (_wicoBlockMaster.WidthInMeters());
                    //                MiningBoreHeight = (_wicoBlockMaster.HeightInMeters() - _wicoBlockMaster.BlockMultiplier() * 2);
                    //                MiningBoreWidth = (_wicoBlockMaster.WidthInMeters() - _wicoBlockMaster.BlockMultiplier() * 2);

                    // save defaults back to customdata to allow player to change

                    _program._CustomDataIni.Set(sMiningSection, "MiningBoreHeight", MiningBoreHeight.ToString("0.00"));
                    _program._CustomDataIni.Set(sMiningSection, "MiningBoreWidth", MiningBoreWidth.ToString("0.00"));
                    // informational for the player
                    _program._CustomDataIni.Set(sMiningSection, "ShipWidth", _wicoBlockMaster.WidthInMeters().ToString("0.00"));
                    _program._CustomDataIni.Set(sMiningSection, "ShipHeight", _wicoBlockMaster.HeightInMeters().ToString("0.00"));
                    _program.CustomDataChanged();
                }
            }

            void MinerCalculateAsteroidVector(long AsteroidID)
            {
                BoundingBoxD bbd = _asteroids.AsteroidGetBB(AsteroidID);

                Vector3D[] corners = new Vector3D[BoundingBoxD.CornerCount];
                AsteroidPosition = bbd.Center;
                AsteroidDiameter = (bbd.Max - bbd.Min).Length();

                MinerCalculateBoreSize();

                AsteroidMaxX = (int)(AsteroidDiameter / MiningBoreWidth / 2);
                AsteroidMaxY = (int)(AsteroidDiameter / MiningBoreHeight / 2);
                /*
                 * Near Side  Far Side
                 * 3---0    7---4
                 * |   |    |   |
                 * 2---1    6---5
                 * 
                 * so 
                 * Right side is 0,1,5,4
                 * Top is 0,3,7,4
                 * Bottom is 1,2,6,5
                 */

                bbd.GetCorners(corners);
                AsteroidUpVector = _sensors.PlanarNormal(corners[3], corners[4], corners[7]);
                AsteroidOutVector = _sensors.PlanarNormal(corners[0], corners[1], corners[2]);
                AsteroidRightVector = _sensors.PlanarNormal(corners[0], corners[1], corners[4]);
                /*

                // Everything after is debug stuff
                Vector3D v1 = corners[3];
                Vector3D v2 = corners[4];
                Vector3D v3 = corners[7];
                debugGPSOutput("Up1", v1);
                debugGPSOutput("Up2", v2);
                debugGPSOutput("Up3", v3);
                v1 = bbd.Center + AsteroidUpVector * 100;
                debugGPSOutput("UPV", v1);
                v1 = corners[0];
                v2 = corners[1];
                v3 = corners[2];
                debugGPSOutput("Out1", v1);
                debugGPSOutput("Out2", v2);
                debugGPSOutput("Out3", v3);
                v1 = bbd.Center + AsteroidOutVector * 100;
                debugGPSOutput("OutV", v1);
                */
            }

            void MinerProcessScan(MyDetectedEntityInfo mydei)
            {
                if (mydei.IsEmpty())
                {
                    return;
                }
//                addDetectedEntity(mydei);
                if (mydei.Type == MyDetectedEntityType.Asteroid)
                {
                    _asteroids.AsteroidAdd(mydei);
                }
            }

        }
    }
}
