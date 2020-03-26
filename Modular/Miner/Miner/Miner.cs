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
            private Program _program;
            private WicoControl _wicoControl;
            private WicoBlockMaster _wicoBlockMaster;
            private Connectors _connectors;
            private WicoThrusters _thrusters;
            private Antennas _antennas;
            private GasTanks _tanks;
            private WicoGyros _gyros;
            private PowerProduction _power;
            private Timers _timers;
            private WicoIGC _wicoIGC;
            private NavCommon _navCommon;
            private Sensors _sensors;
            private Drills _drills;
            //            private NavRemote _navRemote;
            private CargoCheck _cargoCheck;
            private Cameras _cameras;

            private WicoElapsedTime _elapsedTime;

            const string CONNECTORAPPROACHTAG = "CONA";
            const string CONNECTORDOCKTAG = "COND";
            const string CONNECTORALIGNDOCKTAG = "ACOND";
            const string CONNECTORREQUESTFAILTAG = "CONF";

            public Miner(Program program, WicoControl wc, WicoBlockMaster wbm, WicoElapsedTime elapsedTime, WicoIGC iGC,
                WicoThrusters thrusters, Connectors connectors, Sensors sensors, 
                Cameras cameras, Drills drills,
                Antennas ant, GasTanks gasTanks, WicoGyros wicoGyros, PowerProduction pp, Timers timers, 
                NavCommon navCommon, CargoCheck cargoCheck)
            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
                _thrusters = thrusters;
                _connectors = connectors;
                _sensors = sensors;
                _cameras = cameras;
                _drills = drills;
                _antennas = ant;
                _tanks = gasTanks;
                _gyros = wicoGyros;
                _power = pp;
                _timers = timers;
                _wicoIGC = iGC;
                _navCommon = navCommon;
                _cargoCheck = cargoCheck;
                _elapsedTime = elapsedTime;

                //                shipController = myShipController;

                _program.moduleName += " Space Miner";
                _program.moduleList += "\nSpaceMiner V4";

                //                thisProgram._CustomDataIni.Get(sNavSection, "NAVEmulateOld").ToBoolean(NAVEmulateOld);
                //                thisProgram._CustomDataIni.Set(sNavSection, "NAVEmulateOld", NAVEmulateOld);

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddResetMotionHandler(ResetMotionHandler);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                //                _wicoIGC.AddUnicastHandler(UnicastHandler);

                // for backward compatibility
                //                _wicoIGC.AddPublicHandler(CONNECTORAPPROACHTAG, BroadcastHandler);
                //                _wicoIGC.AddPublicHandler(CONNECTORDOCKTAG, BroadcastHandler);
                //                _wicoIGC.AddPublicHandler(CONNECTORALIGNDOCKTAG, BroadcastHandler);

                _elapsedTime.AddTimer(miningChecksElapsed);
                _elapsedTime.AddTimer(miningElapsed);
            }

            void LoadHandler(MyIni Ini)
            {
                /*
            iNIHolder.GetValue(sMiningSection, "TargetMiningMps", ref fTargetMiningMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningAbortMps", ref fMiningAbortMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningMinThrust", ref fMiningMinThrust, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachMps", ref fAsteroidApproachMps, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachAbortMps", ref fAsteroidApproachAbortMps, true);

            iNIHolder.GetValue(sMiningSection, "Cargopcthighwater", ref MiningCargopcthighwater, true);
            iNIHolder.GetValue(sMiningSection, "Cargopctlowater", ref MiningCargopctlowwater, true);

            iNIHolder.GetValue(sMiningSection, "MiningBoreHeight", ref MiningBoreHeight, true);
            iNIHolder.GetValue(sMiningSection, "MiningBoreWidth", ref MiningBoreWidth, true);
                */
            }

            void SaveHandler(MyIni Ini)
            {
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
            /// Modes have changed and we are being called as a handler
            /// </summary>
            /// <param name="fromMode"></param>
            /// <param name="fromState"></param>
            /// <param name="toMode"></param>
            /// <param name="toState"></param>
            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if (fromMode == WicoControl.MODE_MINE)
                {
                    _gyros.gyrosOff();
                    _thrusters.powerDownThrusters();
                    _elapsedTime.StopTimer(miningElapsed);
                    _elapsedTime.StopTimer(miningChecksElapsed);
                }
                // need to check if this is us
                if (toMode == WicoControl.MODE_MINE
                    || toMode == WicoControl.MODE_GOTOORE
                    || toMode == WicoControl.MODE_BORESINGLE
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
                int iState = _wicoControl.IState;

                if (iState== WicoControl.MODE_MINE)
                {
                    _wicoControl.WantFast();
                }
            }
            void LocalGridChangedHandler()
            {
                //               shipController = null;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                string[] varArgs = sArgument.Trim().Split(';');

                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
                    // Commands here:

                }
                if (myCommandLine != null)
                {
                    if (myCommandLine.Argument(0) == "godock")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                    }
                    if (myCommandLine.Argument(0) == "launch")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_LAUNCH);
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
                int iMode= _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iState == WicoControl.MODE_MINE) { doModeMine(); return; }
                if (iState == WicoControl.MODE_GOTOORE) doModeGotoOre();
                if (iState == WicoControl.MODE_BORESINGLE) doModeMineSingleBore();
                if (iState == WicoControl.MODE_EXITINGASTEROID) doModeExitingAsteroid();
            }


            void UnicastHandler(MyIGCMessage msg)
            {
                // NOTE: Called for ALL received unicast messages
                int iMode= _wicoControl.IMode;
                int iState = _wicoControl.IState;

                //                _program.sMasterReporting+="\nMsg Received:"+msg.Tag;

//                if (msg.Tag == CONNECTORAPPROACHTAG && msg.Data is string)
            }

            void ResetMotionHandler(bool bNoDrills = false)
            {
                _thrusters.powerDownThrusters();
                _gyros.gyrosOff();
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
             *  31 set sensor for mining run 
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

                int BoreHoleScanMode = -1;
            Vector3D[] BoreScanFrontPoints = new Vector3D[4];

            void doModeMine()
            {
                int iMode= _wicoControl.IMode;
                int iState = _wicoControl.IState;
                List<IMySensorBlock> aSensors = null;
                IMySensorBlock sb1 = null;
                IMySensorBlock sb2 = null;

                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":MINE", textPanelReport);
                _program.Echo("MINE:iState=" + iState.ToString());
                _program.Echo("Mine Mode=" + AsteroidMineMode);
                //            echoInstructions("MM-A:" + iState );
                //            _program.Echo(Vector3DToString(vExpectedAsteroidExit));
                //            _program.Echo(Vector3DToString(vLastAsteroid            Vector3D[] corners= new Vector3D[BoundingBoxD.CornerCount];
                //            _program.Echo(Vector3DToString(vLastAsteroidExit));
                //StatusLog("clear", gpsPanel);
                //debugGPSOutput("BoreStart" + AsteroidCurrentX.ToString("00") + AsteroidCurrentY.ToString("00"), vAsteroidBoreStart);
                //debugGPSOutput("BoreEnd" + AsteroidCurrentX.ToString("00") + AsteroidCurrentY.ToString("00"), vAsteroidBoreEnd);

                // TODO: Cache these values.
                double maxThrust = _thrusters.calculateMaxThrust(thrustForwardList);
                //            _program.Echo("maxThrust=" + maxThrust.ToString("N0"));
                if (bBoringOnly)
                {
                    double maxBackThrust = _thrusters.calculateMaxThrust(thrustBackwardList);
                    if (maxBackThrust < maxThrust)
                    {
                        _program.Echo("BACK thrust is less than forward!");
                        maxThrust = maxBackThrust;
                    }
                    // TODO: also check docking 'reverse' thrust iff other than 'back' connector
                }

                double effectiveMass = _wicoBlockMaster.GetPhysicalMass(); //((IMyShipController)_wicoBlockMaster.GetMainController()).CalculateShipMass();
                //            _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));

//                if (miningAsteroidID <= 0)
                    //StatusLog("No Current Asteroid", textPanelReport);

                double maxDeltaV = (maxThrust) / effectiveMass;

                //StatusLog("DeltaV=" + maxDeltaV.ToString("N1") + " / " + fMiningMinThrust.ToString("N1") + "min", textPanelReport);


                //            _program.Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

                //            _program.Echo("Cargo=" + cargopcent.ToString() + "%");

                //            _program.Echo("velocity=" + velocityShip.ToString("0.00"));
                //            _program.Echo("miningElapsedMs=" + miningElapsedMs.ToString("0.00"));

                //           IMyTextPanel txtPanel = getTextBlock("Sensor Report");
                //           //StatusLog("clear", txtPanel);

                //            _program.Echo("BoringCount=" + AsteroidBoreCurrent);
                //            if (bValidAsteroid)
                //                //debugGPSOutput("Pre-Valid Ast", vTargetAsteroid);
                //            if (miningAsteroidID > 0)
                //                _program.Echo("Our Asteroid=" + miningAsteroidID.ToString());
                //            echoInstructions("MM-B:" + iState);

                if (iState > 0)
                {
//                    if (miningChecksElapsedMs >= 0) miningChecksElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (_elapsedTime.IsExpired(miningChecksElapsed))
                    {
                        _elapsedTime.ResetTimer(miningChecksElapsed);

                        // DEBUG: generate a ray at what we're suposed to be pointing at.
                        /*
                        double scanDistance= (_wicoBlockMaster.GetMainController().GetPosition()-vAsteroidBoreEnd).Length();
                        if (doCameraScan(cameraForwardList, scanDistance))
                        {

                        }
                        */
                        OreDoCargoCheck();
                        _power.BatteryCheck(0, false);
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
                    //StatusLog("Cargo =" + cargopcent + "% / " + MiningCargopcthighwater + "% Max", textPanelReport);
                    //StatusLog("Battery " + batteryPercentage + "% (Min:" + batterypctlow + "%)", textPanelReport);
//                    if (_tanks.HasHydroTanks()) //StatusLog("H2 " + hydroPercent + "% (Min:" + batterypctlow + "%)", textPanelReport);
                }
                //            echoInstructions("MM-D:" + iState);
                aSensors= _sensors.SensorsGetActive();
                /*
                if (sensorsList.Count >= 2)
                {
                    sb1 = sensorsList[0];
                    sb2 = sensorsList[1];
                }
                */
                switch (iState)
                {
                    case 0:
                        bValidExit = false;
                        bMiningWaitingCargo = false;
                        _elapsedTime.StartTimer(miningElapsed);
                        _elapsedTime.ResetTimer(miningElapsed);

                        _program.ResetMotion();
                        _connectors.TurnEjectorsOff();
                        OreDoCargoCheck(true); // init ores to what's currently in inventory
                        MinerCalculateBoreSize();
                        _thrusters.MoveForwardSlowReset();

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

                        if (miningAsteroidID <= 0) // no known asteroid
                        {
                            // check if we know one
                            miningAsteroidID = AsteroidFindNearest();
                            Vector3D AsteroidPos = AsteroidGetPosition(miningAsteroidID);
                            double curDistanceSQ = Vector3D.DistanceSquared(AsteroidPos, _wicoBlockMaster.GetMainController().GetPosition());
                            if (curDistanceSQ > 5000 * 5000)
                            {
                                // it's too far away. ignore it.
                                miningAsteroidID = 0;
                            }
                        }
                        if (miningAsteroidID > 0) // return to a known asteroid
                        {
                            MinerCalculateBoreSize();
                            if (AsteroidMineMode == 1)
                            {

                            }
                            else
                            {
                                MinerCalculateAsteroidVector(miningAsteroidID);
                                AsteroidCalculateBestStartEnd();
                            }
                            /*
                            vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);

                            bValidAsteroid = true;
                            if (vExpectedAsteroidExit == Vector3D.Zero)
                            {
                                vExpectedAsteroidExit = vTargetAsteroid - _wicoBlockMaster.GetMainController().GetPosition();
                                vExpectedAsteroidExit.Normalize();
                            }
                            */
                            iState = 120;
                            _wicoControl.WantFast();
                        }
                        else
                        {
                            iState = 1;
                            _wicoControl.WantFast();
                        }
                        break;
                    case 1:
                        { // no target asteroid.  Raycast in front of us for one.
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
                                        AsteroidMineMode = 1;// drill exactly where we're aimed for.
                                        MinerCalculateAsteroidVector(miningAsteroidID);

                                        // reset bore hole info to current location
                                        vAsteroidBoreStart = _wicoBlockMaster.GetMainController().GetPosition();

                                        Vector3D vTarget = (Vector3D)_cameras.lastDetectedInfo.HitPosition - _wicoBlockMaster.GetMainController().GetPosition();

                                        vAsteroidBoreEnd = vAsteroidBoreStart;
                                        vAsteroidBoreEnd += _wicoBlockMaster.GetMainController().WorldMatrix.Forward * (AsteroidDiameter + vTarget.Length());

                                        AsteroidUpVector = _wicoBlockMaster.GetMainController().WorldMatrix.Up;

                                        iState = 120;
                                        _wicoControl.WantFast();
                                    }
                                    else
                                    {
                                        StartScans(iMode, 5); // try again after a scan
                                    }
                                }
                                else
                                {
                                    StartScans(iMode, 5); // try again after a scan
                                }
                            }
                            else
                            {
                                _program.Echo("Awaiting Available camera");
                                _wicoControl.WantMedium();
                            }
                            break;
                        }
                    case 5:
                        // we have done a LIDAR scan.  check for found asteroids

                        // TODO: pretty much duplicate code from just above.
                        if (miningAsteroidID <= 0) // no known asteroid
                        {
                            // check if we know one
                            AsteroidMineMode = 0; // should use default mode
                            miningAsteroidID = AsteroidFindNearest();
                        }
                        if (miningAsteroidID > 0)
                        {
                            // we have a valid asteroid.
                            //                        vTargetAsteroid = AsteroidGetPosition(miningAsteroidID);
                            //                        bValidAsteroid = true;
                            MinerCalculateAsteroidVector(miningAsteroidID);
                            AsteroidCalculateFirstBore();

                            iState = 120;
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
                        SensorsSleepAll();
                        SensorSetToShip(sensorsList[0], 2, 2, 2, 2, 2, 2);
                        //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                        _elapsedTime.ResetTimer(miningElapsed);// = 0;
                        iState = 11;
                        _wicoControl.WantMedium();
                        break;

                    case 11:
                        {
//                            miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                            if (miningElapsedMs < dSensorSettleWaitMS) return;

                            aSensors = SensorsGetActive();
                            bool bFoundAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                if (AsteroidProcessLDEI(lmyDEI))
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
                                iState = 100;
                            }
                            else
                            {
                                // no asteroid in sensor range.  Try cameras
                                iState = 400;
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
                            iState = 31;
                        }
                        break;
                    case 31:
                        sb1 = sensorsList[0];
                        sb2 = sensorsList[1];
                        SensorsSleepAll();
                        SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                        SensorSetToShip(sb2, (float)_wicoBlockMaster.WidthInMeters(), (float)_wicoBlockMaster.WidthInMeters(),
                            (float)_wicoBlockMaster.HeightInMeters(), (float)_wicoBlockMaster.HeightInMeters(),
                            1, (float)_wicoBlockMaster.LengthInMeters());
                        iState = 32;
                        _elapsedTime.ResetTimer(miningElapsed);
                        _wicoControl.WantFast();
                        _program.ResetMotion();
                        break;
                    case 32:
                        
//                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (_elapsedTime.GetElapsed(miningElapsed) < dSensorSettleWaitMS*1000)
                        {
                            _wicoControl.WantMedium();
                            return; // delay for sensor settling
                        }
                        // else:
                        _wicoControl.WantFast();
                        //                    vAsteroidBoreStart = AsteroidCalculateBoreStart();
                        iState = 35;
                        break;
                    case 35:
                        { // active mining
                          //
                          //                        int eoicount = 0;
                          //                        echoInstructions("S=" + iState + " " + eoicount++);
                            bool bAimed = false;
                            _program.Echo("Mining forward");
                            //StatusLog("Mining Forward!", textPanelReport);
                            if (bBoringOnly) _program.Echo("Boring Miner");
                            //                        sb1 = sensorsList[0];
                            //                        sb2 = sensorsList[1];
                            bool bLocalAsteroid = false;
                            bool bForwardAsteroid = false;
                            bool bSourroundAsteroid = false;
                            bool bLarge = false;
                            bool bSmall = false;
                            //                        _program.Echo("FW=" + sb1.CustomName);
                            //                        _program.Echo("AR=" + sb2.CustomName);
                            // TODO: Make sensors optional (and just always do runs and use distance to know when done with bore.
                            sb1 = _sensors.GetForwardSensor(0);
                            sb2 = _sensors.GetForwardSensor(1);
                            //                        echoInstructions("S=" + iState + "S " + eoicount++);
                            _sensors.SensorIsActive(sb1, ref bForwardAsteroid, ref bLarge, ref bSmall);
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
                                if (AsteroidProcessLDEI(lmyDEI))
                                {
                                    // TODO: if we find ANOTHER asteroid in sensors, figure out what to do
                                    bLocalAsteroid = true;
                                }
                            }
                            //                        echoInstructions("S=" + iState + "EDEI " + eoicount++);

                            double distance = (vAsteroidBoreStart - _wicoBlockMaster.GetMainController().GetPosition()).Length();

                            // *2 because of start and end enhancement
                            double boreLength = AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2;
                            _program.Echo("Distance=" + niceDoubleMeters(distance) + " (" + niceDoubleMeters(boreLength) + ")");
                            double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                            //StatusLog("Bore:" + ((distance + stoppingDistance) / boreLength * 100).ToString("N0") + "%", textPanelReport);
                            //                        if ((distance + stoppingDistance) < (AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2))// even if sensors don't see anything. continue to end of the bore.
                            if ((distance + stoppingDistance) < boreLength * 0.65) // if we are <65% done with bore, continue no matter what sensors say
                            {
                                bLocalAsteroid = true;
                            }
                            if (!bLocalAsteroid)
                            { // no asteroid detected on ANY sensors. ->we have exited the asteroid.
                              //                            _program.Echo("No Local Asteroid found");
                                _program.ResetMotion();
                                //                            echoInstructions("S=" + iState + "RM " + eoicount++);
                                if (cargopcent > MiningCargopctlowwater || maxDeltaV < (fMiningMinThrust))
                                {
                                    // we need to dump our contents
                                    turnEjectorsOn();
                                    //                                echoInstructions("S=" + iState + "EJ " + eoicount++);
                                }
                                //                            sStartupError += "\nOut:" + aSensors.Count + " : " +bForwardAsteroid.ToString() + ":"+bSourroundAsteroid.ToString();
                                //                            sStartupError += "\nFW=" + bForwardAsteroid.ToString() + " Sur=" + bSourroundAsteroid.ToString();
                                iState = 300;
                                _wicoControl.WantFast();
                                return;
                            }
                            //                        echoInstructions("S=" + iState + "bfacheck " + eoicount++);
                            if (bForwardAsteroid)
                            { // asteroid in front of us
                                turnEjectorsOn();
                                //                            blockApplyAction(ejectorList, "OnOff_On");
                                if (maxDeltaV < fMiningMinThrust || cargopcent > MiningCargopcthighwater && !bMiningWaitingCargo) //
                                {
                                    _program.ResetMotion();
                                    // already done                                turnEjectorsOn();
                                    bMiningWaitingCargo = true;
                                }
                                //                            echoInstructions("S=" + iState + "bmwc check " + eoicount++);
                                if (bMiningWaitingCargo)
                                { // continue to wait
                                    _wicoControl.WantSlow();
                                    _program.ResetMotion();
                                    // need to check how much stone we have.. if zero(ish), then we're full.. go exit.
                                    //                                echoInstructions("S=" + iState + "ODCC " + eoicount++);
                                    //                                OreDoCargoCheck(); redundant
                                    //                                echoInstructions("S=" + iState + "EDOCC " + eoicount++);

                                    double currUndesireable = currentUndesireableAmount();
                                    if (currUndesireable < 15 && (maxDeltaV < fMiningMinThrust || cargopcent > MiningCargopctlowwater))
                                    {
                                        // we are full and not much stone ore in us...
                                        _program.ResetMotion();
                                        _connectors.TurnEjectorsOff();
                                        _elapsedTime.StopTimer(miningChecksElapsed);//miningChecksElapsedMs = -1;
                                        _wicoControl.SetMode(WicoControl.MODE_EXITINGASTEROID);
                                    }
                                    // TODO: Needs time-out
                                    //StatusLog("Waiting for cargo and thrust to be available", textPanelReport);
                                    _program.Echo("Cargo above low water: Waiting");
                                    if (maxDeltaV > fMiningMinThrust && cargopcent < MiningCargopctlowwater && currUndesireable < 1000)
                                        bMiningWaitingCargo = false; // can now move.
                                }
                                else
                                {
                                    /*
                                                                    // 'BeamRider' routine that takes start,end and tries to stay on that beam.
                                                                    // Todo: probably should use center of ship BB, not COM.
                                                                    Vector3D vBoreEnd = (vAsteroidBoreEnd - vAsteroidBoreStart);
                                                                    Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                                                                    Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);
                                    //                                _program.Echo("BoreEnd=" + Vector3DToString(vBoreEnd));
                                    //                                _program.Echo("AimEnd=" + Vector3DToString(vAimEnd));
                                    //                                _program.Echo("RejectEnd=" + Vector3DToString(vRejectEnd));
                                    //                                Vector3D vCorrectedAim = vAsteroidBoreEnd - vRejectEnd;

                                                                    Vector3D vCorrectedAim = (vAsteroidBoreEnd- vRejectEnd*2) - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass;

                                                                    bAimed = GyroMain("forward", vCorrectedAim, _wicoBlockMaster.GetMainController());
                                    */
                                    //                                echoInstructions("S=" + iState + "BR " + eoicount++);
                                    bAimed = BeamRider(vAsteroidBoreStart, vAsteroidBoreEnd, _wicoBlockMaster.GetMainController());

                                    turnDrillsOn();
                                    //                                echoInstructions("S=" + iState + "EOtDO " + eoicount++);
                                    //                                _program.Echo("bAimed=" + bAimed.ToString());
                                    //                                _program.Echo("minAngleRad=" + minAngleRad);
                                    if (bAimed)
                                    {
                                        _program.Echo("Aimed");
                                        MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustForwardList, thrustBackwardList);
                                        _wicoControl.WantMedium();
                                        /*
                                        gyrosOff();
                                        bAimed = GyroMain("up", AsteroidUpVector, _wicoBlockMaster.GetMainController());
                                        */
                                    }
                                    else
                                    {
                                        _program.Echo("Not Aimed");
                                        _thrusters.powerDownThrusters();
                                        _wicoControl.WantFast();
                                    }
                                    //                                _program.Echo("bAimed=" + bAimed.ToString());
                                    /*
                                    if (!bAimed) _wicoControl.WantFast();
                                    else _wicoControl.WantMedium();
                                    */
                                }
                            }
                            else
                            {
                                // we have nothing in front, but are still close
                                //StatusLog("Exiting Asteroid", textPanelReport);
                                turnDrillsOff();
                                _connectors.TurnEjectorsOff(); // don't want stuff hitting us in the butt
                                Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);

                                bAimed = _gyros.AlignGyros("forward", vAim, _wicoBlockMaster.GetMainController());
                                MoveForwardSlow(fAsteroidExitMps, fAsteroidExitMps * 1.25f, thrustForwardList, thrustBackwardList);
                                if (!bAimed) _wicoControl.WantFast();
                                else _wicoControl.WantMedium();
                            }
                        }
                        break;

                    case 100:
                        turnDrillsOff();
                        SensorsSleepAll();
                        sb1 = sensorsList[0];
                        SensorSetToShip(sb1, 0, 0, 0, 0, 50, 0);
                        iState = 101;
                        miningElapsedMs = 0;
                        break;
                    case 101:
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS) return; // delay for sensor settling
                        iState++;
                        break;

                    case 102:
                        {
                            aSensors = SensorsGetActive();
                            //                        bValidAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                AsteroidProcessLDEI(lmyDEI);
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
                                if (doCameraScan(cameraForwardList, scandist))
                                { // we scanned
                                    if (!lastDetectedInfo.IsEmpty())
                                    {  // we hit something

                                        if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                        {
                                            MinerProcessScan(lastDetectedInfo);
                                            /*
                                            addDetectedEntity(lastDetectedInfo);
                                            vTargetAsteroid = (Vector3D)lastDetectedInfo.HitPosition;
                                            bValidAsteroid = true;
                                            vExpectedExit = vTargetAsteroid - _wicoBlockMaster.GetMainController().GetPosition();
                                            vExpectedExit.Normalize();
                                            bValidExit = true;
                                            */

                                        }
                                    }
                                    else
                                    {
                                        // no asteroid detected.  Check surroundings for one.
                                        iState = 110;
                                        bValidExit = false;
                                    }
                                }

                            }
                            if (bValidExit) iState = 120; //found asteroid ahead
                        }
                        break;
                    case 110:
                        // --shouldn't be necessary since we have scans..  but maybe no asteroid in front of aim spot?
                        { // asteroid NOT in front. big sensor search for asteroids in area
                            _program.Echo("set big sensors");

                            _sensors.SensorsSleepAll();
                            _sensors.SensorSetToShip(sensorsList[0], 50, 50, 50, 50, 50, 50);
                            _elapsedTime.ResetTimer(miningElapsed);
                            iState = 111;
                            _program.Echo("iState now=" + iState.ToString());
                        }
                        break;
                    case 111:
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS) return; // delay for sensor settling
                        iState++;
                        break;
                    case 112:
                        { // asteroid not in front. Check sensors
                            aSensors = SensorsGetActive();
                            //                        bValidAsteroid = false;
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                AsteroidProcessLDEI(lmyDEI);
                            }
                            miningAsteroidID = AsteroidFindNearest();
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
                                iState = 120;
                                _wicoControl.WantFast();
                            }
                            else
                            {
                                StartScans(iMode, 5); // try again after a scan
                            }
                        }
                        break;


                    case 120:
                        {
                            // we have a known asteroid.  go toward our starting location
                            // wait for ship to slow
                            _program.ResetMotion();
                            bool bReady = DockAirWorthy(true, false, MiningCargopcthighwater);
                            if (maxDeltaV < fMiningMinThrust || !bReady) //cargopcent > MiningCargopctlowwater || batteryPercentage < batterypctlow)
                            {
                                //TODO: check H2 tanks.
                                //TODO: Check uranium supply
                                // we don't have enough oomph to do the bore.  go dock and refill/empty and then try again.
                                bAutoRelaunch = true;
                                _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                                _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                            }
                            else
                            if (velocityShip < fAsteroidApproachMps)
                            {
                                iState = 121;
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
                            if (distanceSQ > 75 * 75)
                            {
                                // do far travel.
                                iState = 190;
                                return;
                            }
                            iState = 125;
                            break;
                        }
                    case 125:
                        {
                            double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();
                            _program.Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                            double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                            //StatusLog("Move to Bore Start", textPanelReport);

                            if ((distanceSQ - stoppingDistance * 2) > 2)
                            {
                                // set aim to ending location
                                Vector3D vAim = (vAsteroidBoreStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                                bool bAimed = GyroMain("forward", vAim, _wicoBlockMaster.GetMainController());
                                if (bAimed)
                                {
                                    //                                _program.Echo("Aimed");
                                    bWantFast = false;
                                    _wicoControl.WantMedium();
                                    MoveForwardSlow(fAsteroidApproachMps, fAsteroidApproachAbortMps, thrustForwardList, thrustBackwardList);
                                }
                                else _wicoControl.WantFast();
                            }
                            else
                            {
                                // we have arrived
                                _program.ResetMotion();
                                iState = 130;
                                _wicoControl.WantFast();
                            }
                            break;
                        }
                    case 130:
                        _program.Echo("Sensor Set");
                        SensorsSleepAll();
                        SensorSetToShip(sb1, 0, 0, 0, 0, 50, -1);
                        SensorSetToShip(sb2, 1, 1, 1, 1, 15, -1);
                        miningElapsedMs = 0;
                        //first do a rotate to 'up'...
                        iState = 131;
                        _wicoControl.WantFast();
                        break;
                    case 131:
                        {
                            //StatusLog("Borehole Alignment", textPanelReport);
                            if (velocityShip < 0.5)
                            { // wait for ship to slow down
                                double distanceSQ = (vAsteroidBoreStart - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass).LengthSquared();
                                /*
                                if (distanceSQ > 1.5)
                                {
                                    iState = 120; // try again.
                                }
                                else
                                */
                                {
                                    // align with 'up'
                                    _wicoControl.WantFast();
                                    if (GyroMain("up", AsteroidUpVector, _wicoBlockMaster.GetMainController()))
                                    {
                                        iState = 134;
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
                            //StatusLog("Borehole Alignment", textPanelReport);
                            _wicoControl.WantFast();
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            if (GyroMain("forward", vAim, _wicoBlockMaster.GetMainController()))
                            {
                                iState = 137;
                            }
                            break;
                        }
                    case 137:
                        {
                            // align with 'up'
                            //StatusLog("Borehole Alignment", textPanelReport);
                            _wicoControl.WantFast();
                            if (GyroMain("up", AsteroidUpVector, _wicoBlockMaster.GetMainController()))
                            {
                                iState = 140;
                            }
                            break;
                        }

                    case 140:
                        { // bore scan init
                            //StatusLog("Bore Check Init", textPanelReport);
                            _program.Echo("Bore Check Init");
                            BoreHoleScanMode = -1;
                            _wicoControl.WantFast();
                            // we should have asteroid in front.
                            bool bAimed = true;
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            bAimed = GyroMain("forward", vAim, _wicoBlockMaster.GetMainController());

                            miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

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
                                iState = 145;
                                //                            iState = 143; //Beam testing
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
                            _program.Echo("BoreEnd=" + Vector3DToString(vBoreEnd));
                            Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                            //Vector3D vAimEnd = _wicoBlockMaster.GetMainController().WorldMatrix.Forward;
                            //                        vAimEnd.Normalize();
                            _program.Echo("AimEnd=" + Vector3DToString(vAimEnd));
                            //                        Vector3D vCrossEnd = (vBoreEnd - vAimEnd * 2);
                            Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);
                            //                        vCrossEnd.Normalize();

                            _program.Echo("CrossEnd=" + Vector3DToString(vRejectEnd));
                            //                        Vector3D vCrossEnd = (vBoreEnd + vAimEnd) / 2;
                            //debugGPSOutput("CrossStartEnd", vAsteroidBoreStart + vRejectEnd);
                            //                        //debugGPSOutput("CrossEndStart", vAsteroidBoreEnd - vCrossStart);
                            //                        //debugGPSOutput("COM", ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                            //                        _program.Echo("CrossStart=" + Vector3DToString(vCrossStart));


                            //                        _program.Echo("FW=" + Vector3DToString(_wicoBlockMaster.GetMainController().WorldMatrix.Forward));
                            //                          doCameraScan(cameraForwardList, vAsteroidBoreEnd + vCrossEnd);
                            //                        doCameraScan(cameraBackwardList, vAsteroidBoreStart + vCrossStart);
                            Vector3D vCorrectedAim = vAsteroidBoreEnd - vRejectEnd;
                            doCameraScan(cameraForwardList, vCorrectedAim);
                            //                        doCameraScan(cameraBackwardList, vAsteroidBoreStart - vCrossEnd);
                            _wicoControl.WantMedium();
                        }
                        break;
                    case 145:
                        { // bore scan
                            //StatusLog("Bore Check Scan", textPanelReport);
                            _program.Echo("Bore Check Scan");

                            // we should have asteroid in front.
                            bool bAimed = true;
                            bool bAsteroidInFront = false;
                            bool bFoundCloseAsteroid = false;

                            //                        _program.Echo(bValidExit.ToString() + " " + Vector3DToString(vExpectedAsteroidExit));
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            bAimed = GyroMain("forward", vAim, _wicoBlockMaster.GetMainController());

                            miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                            if (miningElapsedMs < dSensorSettleWaitMS)
                            {
                                _wicoControl.WantMedium();
                                return;
                            }

                            if (bAimed)
                            {
                                bool bLarge = false;
                                bool bSmall = false;
                                SensorIsActive(sb1, ref bAsteroidInFront, ref bLarge, ref bSmall);
                                SensorIsActive(sb2, ref bFoundCloseAsteroid, ref bLarge, ref bSmall);

                                /* TEST: Always do the scans
                                if (bFoundCloseAsteroid || bAsteroidInFront)
                                {
                                    // sensor is active.. go forth and mine
                                    iState = 150;
                                    _wicoControl.WantFast();
                                }
                                else 
                                */
                                {
                                    _wicoControl.WantMedium();
                                    // try a raycast to see if we can hit anything
                                    if (cameraForwardList.Count < 1)
                                    {
                                        // no cameras to scan with.. assume
                                        iState = 150;
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
                                        AsteroidDoNextBore();
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
                                            if (doCameraScan(cameraForwardList, vAsteroidBoreEnd))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 1:
                                            vTarget = BoreScanFrontPoints[2] + vScanVector * scanDistance;
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 2:
                                            vTarget = BoreScanFrontPoints[3] + vScanVector * scanDistance;
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 3:
                                            vTarget = BoreScanFrontPoints[0] + vScanVector * scanDistance;
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 4:
                                            vTarget = BoreScanFrontPoints[1] + vScanVector * scanDistance;
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 5:
                                            /*
                                            // check center again.  always full length
                                            if (doCameraScan(cameraForwardList, scanDistance))
                                            {
                                                bDidScan = true;
                                            }
                                            */
                                            bDidScan = true;
                                            break;
                                        case 6:
                                            vTarget = BoreScanFrontPoints[2] + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 7:
                                            vTarget = BoreScanFrontPoints[3] + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 8:
                                            vTarget = BoreScanFrontPoints[0] + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 9:
                                            vTarget = BoreScanFrontPoints[1] + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        // front output order is 0=BL, 1=BR, 2=TL, 3=TR
                                        case 10:
                                            // bottom middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 11:
                                            // top middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 12:
                                            // right middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 13:
                                            // left middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.55);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 14:
                                            vTarget = BoreScanFrontPoints[2] + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 15:
                                            vTarget = BoreScanFrontPoints[3] + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 16:
                                            vTarget = BoreScanFrontPoints[0] + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 17:
                                            vTarget = BoreScanFrontPoints[1] + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        // front output order is 0=BL, 1=BR, 2=TL, 3=TR
                                        case 18:
                                            // bottom middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 19:
                                            // top middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 20:
                                            // right middle
                                            vTarget = (BoreScanFrontPoints[1] + BoreScanFrontPoints[3]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                        case 21:
                                            // left middle
                                            vTarget = (BoreScanFrontPoints[2] + BoreScanFrontPoints[0]) / 2 + vScanVector * (scanDistance * 0.35);
                                            if (doCameraScan(cameraForwardList, vTarget))
                                            {
                                                bDidScan = true;
                                            }
                                            break;
                                    }
                                    if (bDidScan)
                                    {
                                        // the camera scan routine sets lastDetetedInfo itself if scan succeeds
                                        if (!lastDetectedInfo.IsEmpty())
                                        {
                                            if (lastDetectedInfo.Type == MyDetectedEntityType.Asteroid)
                                            {
                                                //                                            sStartupError += "BoreScan hit on " + BoreHoleScanMode + "\n";
                                                // we found an asteroid. (hopefully it's ours..)
                                                bFoundCloseAsteroid = true;
                                            }
                                            else if (lastDetectedInfo.Type == MyDetectedEntityType.FloatingObject)
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
                                                iState = 150;
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
                            //StatusLog("Asteroid Approach", textPanelReport);
                            _program.Echo("Asteroid Approach");

                            _wicoControl.WantSlow();
                            // we should have asteroid in front.
                            bool bAimed = true;
                            bool bAsteroidInFront = false;
                            bool bFoundCloseAsteroid = false;

                            bAimed = BeamRider(vAsteroidBoreStart, vAsteroidBoreEnd, _wicoBlockMaster.GetMainController());
                            /*
                            // 'BeamRider' routine that takes start,end and tries to stay on that beam.
                            Vector3D vBoreEnd = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            Vector3D vAimEnd = (vAsteroidBoreEnd - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass);
                            Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);

                            Vector3D vCorrectedAim = (vAsteroidBoreEnd - vRejectEnd * 2) - ((IMyShipController)_wicoBlockMaster.GetMainController()).CenterOfMass;

                            bAimed = GyroMain("forward", vCorrectedAim, _wicoBlockMaster.GetMainController());
                            */
                            /*                        Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                                                    bAimed = GyroMain("forward", vAim, _wicoBlockMaster.GetMainController());
                            */
                            double distance = (vAsteroidBoreStart - _wicoBlockMaster.GetMainController().GetPosition()).Length();
                            _program.Echo("Distance=" + niceDoubleMeters(distance) + " (" + niceDoubleMeters(AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2) + ")");
                            //StatusLog("Distance=" + niceDoubleMeters(distance) + " (" + niceDoubleMeters(AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2) + ")", textPanelReport);
                            double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                            miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                            if (miningElapsedMs < dSensorSettleWaitMS)
                            {
                                _wicoControl.WantMedium();
                                return;
                            }

                            if (bAimed)
                            {
                                bool bLarge = false;
                                bool bSmall = false;
                                //                            SensorIsActive(sb1, ref bAsteroidInFront, ref bLarge, ref bSmall);
                                SensorIsActive(sb2, ref bFoundCloseAsteroid, ref bLarge, ref bSmall);
                                //
                                bWantFast = false;
                                _wicoControl.WantMedium();

                                // we already verified that there is asteroid in this bore.. go get it.
                                bAsteroidInFront = true;

                                if (bFoundCloseAsteroid)
                                {
                                    _thrusters.powerDownThrusters();
                                    iState = 31;
                                }
                                else if (bAsteroidInFront)
                                {
                                    if ((distance + stoppingDistance) > (AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2))
                                    {
                                        // we have gone too far.  nothing to mine
                                        //                                    sStartupError += "\nTOO FAR! ("+AsteroidCurrentX+"/"+AsteroidCurrentY+")";

                                        iState = 155;
                                        /*
                                           _program.ResetMotion();
                                            if(velocityShip<1)
                                            AsteroidDoNextBore();
                                            */
                                    }
                                    else
                                    {
                                        MoveForwardSlow(fAsteroidApproachMps, fAsteroidApproachAbortMps, thrustForwardList, thrustBackwardList);
                                    }
                                }
                            }
                            else
                            {
                                _wicoControl.WantFast();
                                _program.Echo("Aiming");
                                if ((distance + stoppingDistance) > (AsteroidDiameter + _wicoBlockMaster.LengthInMeters() * MineShipLengthScale * 2))
                                {
                                    // we have gone too far.  nothing to mine
                                    //                                    sStartupError += "\nTOO FAR! ("+AsteroidCurrentX+"/"+AsteroidCurrentY+")";
                                    iState = 155;
                                    /*
                                    _program.ResetMotion();
                                    if (velocityShip < 1)
                                        AsteroidDoNextBore();
                                        */
                                }
                            }
                        }
                        break;
                    case 155:
                        {
                            _program.ResetMotion();
                            //                        if (velocityShip < 1)
                            AsteroidDoNextBore();
                        }
                        break;
                    case 190:
                        {
                            // start NAV travel
                            _navCommon.NavGoTarget(vAsteroidBoreStart, iMode, 195, 11, "MINE-Bore start");
                        }
                        break;
                    case 195:
                        {// we have 'arrived' from far travel
                         // wait for motion to slow
                            _wicoControl.WantMedium();
                            if (velocityShip < fAsteroidApproachMps)
                            {
                                iState = 120;
                                _wicoControl.WantFast();
                            }
                            _program.ResetMotion();
                            break;
                        }
                    case 300:
                        {
                            // we have exitted the asteroid.  Prepare for another run or to go dock
                            _program.Echo("Exitted!");
                            _program.ResetMotion();
                            SensorsSleepAll();
                            turnEjectorsOn();
                            _wicoControl.WantMedium();
                            iState = 305;
                            break;
                        }
                    case 305:
                        {
                            _wicoControl.WantMedium();
                            if (velocityShip > 1) return;

                            if (AsteroidMineMode == 1)
                            {
                                // we did a single bore.
                                // so now we are done.
                                AsteroidMineMode = 0;
                                bAutoRelaunch = false;
                                _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                                miningAsteroidID = -1;
                                break;
                            }
                            // else mine mode !=1
                            bool bReady = DockAirWorthy(true, false, MiningCargopcthighwater);
                            if (maxDeltaV < fMiningMinThrust || !bReady) //cargopcent > MiningCargopctlowwater || batteryPercentage < batterypctlow)
                            {
                                bool bBoresRemaining = AsteroidCalculateNextBore();
                                if (bBoresRemaining)
                                {
                                    bAutoRelaunch = true;
                                    _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                                    _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                                }
                                else
                                {
                                    iState = 500;
                                }
                            }
                            else
                            {
                                // UNLESS: we have another target asteroid..
                                // TODO: 'recall'.. but code probably doesn't go here.
                                AsteroidDoNextBore();
                            }
                        }
                        break;
                    case 310:
                        {
                            AsteroidCalculateBestStartEnd();
                            //                        vAsteroidBoreStart = AsteroidCalculateBoreStart();
                            _navCommon.NavGoTarget(vAsteroidBoreStart, iMode, 120, 11, "MINE-Next Bore start");
                            break;

                        }
                    case 500:
                        {
                            // TODO: do a final search pass for any missed voxels.
                            // TODO: remove asteroid after final pass

                            // Go home.
                            bAutoRelaunch = false;
                            _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                            miningAsteroidID = -1;
                            break;
                        }
                }
            }

            void doModeGotoOre()
            {
                /*
                List<IMySensorBlock> aSensors = null;
                IMySensorBlock sb;
                */
                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":GotoOre!", textPanelReport);
                _program.Echo("GOTO ORE:iState=" + iState.ToString());
                MyShipMass myMass;
                myMass = ((IMyShipController)_wicoBlockMaster.GetMainController()).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass;

                double maxThrust = _thrusters.calculateMaxThrust(thrustForwardList);

                _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));
                _program.Echo("maxThrust=" + maxThrust.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;

                _program.Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
                _program.Echo("Cargo=" + cargopcent.ToString() + "%");

                _program.Echo("velocity=" + velocityShip.ToString("0.00"));
                _program.Echo("#Sensors=" + sensorsList.Count);
                _program.Echo("width=" + _wicoBlockMaster.WidthInMeters().ToString("0.0"));
                _program.Echo("height=" + _wicoBlockMaster.HeightInMeters().ToString("0.0"));
                _program.Echo("length=" + _wicoBlockMaster.LengthInMeters().ToString("0.0"));

                //            IMyTextPanel txtPanel = getTextBlock("Sensor Report");
                //            //StatusLog("clear", txtPanel);

            }

            void doModeMineSingleBore()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":MINE Line", textPanelReport);
                _program.Echo("MINE:iState=" + iState.ToString());
                double maxThrust = _thrusters.calculateMaxThrust(thrustForwardList);
                IMySensorBlock sb1 = null;
                IMySensorBlock sb2 = null;
                //            _program.Echo("maxThrust=" + maxThrust.ToString("N0"));

                MyShipMass myMass;
                myMass = ((IMyShipController)_wicoBlockMaster.GetMainController()).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass;
                //            _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;
                _program.Echo("Our Asteroid=" + miningAsteroidID.ToString());
                if (sensorsList.Count >= 2)
                {
                    sb1 = sensorsList[0];
                    sb2 = sensorsList[1];
                }
                switch (iState)
                {
                    case 0:
                        bValidExit = false;
                        bMiningWaitingCargo = false;

                        _program.ResetMotion();
                        _connectors.TurnEjectorsOff();
                        OreDoCargoCheck(true); // init ores to what's currently in inventory
                        MinerCalculateBoreSize();
                        _thrusters.MoveForwardSlowReset();

                        _wicoControl.WantFast();

                        if (sensorsList.Count < 2)
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
 //                       iState = 1;
                        _wicoControl.SetMode( WicoControl.MODE_MINE,1);
                        AsteroidMineMode = 1;// drill exactly where we're aimed for.
                        _wicoControl.WantFast();
                        break;
                }
            }

            void doModeExitingAsteroid()
            {
                int iMode= _wicoControl.IMode;
                int iState = _wicoControl.IState;
                List<IMySensorBlock> aSensors = null;
                /*
                IMySensorBlock sb;
                */

                //StatusLog("clear", textPanelReport);
                //StatusLog(moduleName + ":Exiting!", textPanelReport);
                _program.Echo("Exiting: iState=" + iState.ToString());
                MyShipMass myMass;
                myMass = ((IMyShipController)_wicoBlockMaster.GetMainController()).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass;

                double maxThrust = _thrusters.calculateMaxThrust(thrustForwardList);

                //            _program.Echo("effectiveMass=" + effectiveMass.ToString("N0"));
                //            _program.Echo("maxThrust=" + maxThrust.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;

                _program.Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
                //StatusLog("Max DeltaV=" + maxDeltaV.ToString("N1") + " / " + fMiningMinThrust.ToString("N1") + "min", textPanelReport);
                if (iState > 0)
                {
//                    if (miningChecksElapsedMs >= 0) miningChecksElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (_elapsedTime.IsExpired(miningChecksElapsed))
                    {
                        _elapsedTime.ResetTimer(miningChecksElapsed);
                        DockAirWorthy(false, false); // does the current value checks.
                                                     /*
                                                     OreDoCargoCheck();
                                                     batteryCheck(0);
                                                     TanksCalculate();
                                                     // TODO: check reactor uranium
                                                     */
                        //StatusLog("Cargo =" + cargopcent + "% / " + MiningCargopcthighwater + "% Max", textPanelReport);
                        //StatusLog("Battery " + batteryPercentage + "% (Min:" + batterypctlow + "%)", textPanelReport);
 //                       if (TanksHasHydro()) //StatusLog("H2 " + hydroPercent + "% (Min:" + batterypctlow + "%)", textPanelReport);
                    }
                }

                _program.Echo("velocity=" + velocityShip.ToString("0.00"));
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
                 * 40 when out, call for pickup
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
                        iState = 10;
                        _program.ResetMotion();
                        //                    turnDrillsOff();
                        turnEjectorsOn();
                        if (sensorsList.Count < 2)
                        {
                            //StatusLog(OurName + ":" + moduleName + " Exit Asteroid: Not Enough Sensors!", textLongStatus, true);
                            sStartupError += "Not enough sensors found!";
                            _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        }
                        break;
                    case 10://10 - Init sensors, 
                        _wicoControl.WantMedium();
                        SensorsSleepAll();
                        if (bBoringOnly)
                        {
                            SensorSetToShip(sensorsList[0], (float)_wicoBlockMaster.WidthInMeters(), (float)_wicoBlockMaster.WidthInMeters(),
                                (float)_wicoBlockMaster.HeightInMeters(), (float)_wicoBlockMaster.HeightInMeters(),
                                (float)_wicoBlockMaster.LengthInMeters(), 1);
                        }
                        else
                        {
                            SensorSetToShip(sensorsList[0], (float)_wicoBlockMaster.WidthInMeters(), (float)_wicoBlockMaster.WidthInMeters(),
                                (float)_wicoBlockMaster.HeightInMeters(), (float)_wicoBlockMaster.HeightInMeters(),
                                1, (float)_wicoBlockMaster.LengthInMeters());

                        }

                        //                    SensorSetToShip(sensorsList[0], 2, 2, 2, 2, 15, 15);
                        //                    setSensorShip(sensorsList[1], 50, 50, 50, 50, 50, 0);
                        miningElapsedMs = 0;
                        iState = 11;
                        break;
                    case 11://11 - await sensor set
                        _wicoControl.WantMedium();
                        miningElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
                        if (miningElapsedMs < dSensorSettleWaitMS) return;
                        if (bBoringOnly)
                            iState = 30;
                        else
                            iState = 20;
                        break;
                    case 20: //20 - turn around until aimed ->30
                        {
                            _wicoControl.WantFast();
                            turnDrillsOn();

                            // we want to turn on our horizontal axis as that should be the 'wide' one.
                            bool bAimed = false;
                            double yawangle = -999;
                            //                        _program.Echo("vTarget=" + Vector3DToString(vLastAsteroidContact));
                            yawangle = CalculateYaw(vAsteroidBoreStart, _wicoBlockMaster.GetMainController());
                            _program.Echo("yawangle=" + yawangle.ToString());
                            double aYawAngle = Math.Abs(yawangle);
                            bAimed = aYawAngle < .05;

                            // turn slower when >180 since we are in tunnel and drills are cutting a path
                            float maxYPR = GyroControl.MaxYPR;
                            _program.Echo("maxYPR=" + maxYPR.ToString("0.00"));
                            if (aYawAngle > 1.0) maxYPR = maxYPR / 3;

                            DoRotate(yawangle, "Yaw", maxYPR, 0.33f);

                            /*
                            //minAngleRad = 0.1f;
                            bAimed=GyroMain("backward", vExpectedExit, _wicoBlockMaster.GetMainController());

                           // minAngleRad = 0.01f;
                            // GyroMain("backward", vExpectedExit, _wicoBlockMaster.GetMainController());
                            */
                            if (bAimed)
                            {
                                iState = 30;
                                //MoveForwardSlow(fTargetMiningmps,fAbortmps, thrustForwardList, thrustBackwardList);
                            }

                        }
                        break;
                    case 30:
                        {
                            bool bAimed = false;
                            string sOrientation = "backward";
                            if (bBoringOnly)
                            {
                                sOrientation = "forward";
                            }
                            Vector3D vAim = (vAsteroidBoreEnd - vAsteroidBoreStart);
                            bAimed = GyroMain(sOrientation, vAim, _wicoBlockMaster.GetMainController());
                            if (bAimed)
                            {
                                _wicoControl.WantMedium();
                                if (bBoringOnly)
                                {
                                    MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustBackwardList, thrustForwardList);
                                }
                                else
                                {
                                    MoveForwardSlow(fTargetMiningMps, fMiningAbortMps, thrustForwardList, thrustBackwardList);
                                }
                            }
                            else _wicoControl.WantFast();

                            bool bLocalAsteroid = false;
                            aSensors = SensorsGetActive();
                            for (int i = 0; i < aSensors.Count; i++)
                            {
                                IMySensorBlock s = aSensors[i] as IMySensorBlock;
                                //                            //StatusLog(aSensors[i].CustomName + " ACTIVE!", txtPanel);
                                //                            _program.Echo(aSensors[i].CustomName + " ACTIVE!");

                                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                                s.DetectedEntities(lmyDEI);
                                if (AsteroidProcessLDEI(lmyDEI))
                                    bLocalAsteroid = true;
                            }
                            if (!bLocalAsteroid)
                            {
                                _program.ResetMotion();
                                SensorsSleepAll();
                                iState = 40;
                            }
                            break;
                        }
                    case 40://40 when out, call for pickup
                        {
                            turnDrillsOff();

                            iState = 50;
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
                            // we should probably give hint to docking as to WHY we want to dock..
                            bAutoRelaunch = true;
                            _elapsedTime.StopTimer(miningChecksElapsed);// miningChecksElapsedMs = -1;
                            _wicoControl.SetMode(WicoControl.MODE_DOCKING);
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

            void AsteroidDoNextBore()
            {
                if (!AsteroidCalculateNextBore())
                {
                    // we are done with asteroid.
                    // TODO: do a final search pass for any missed voxels
                    // TODO: remove asteroid
                    bAutoRelaunch = false;
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
                if (MiningBoreHeight <= 0)
                {
                    MiningBoreHeight = (_wicoBlockMaster.HeightInMeters());
                    MiningBoreWidth = (_wicoBlockMaster.WidthInMeters());
                    //                MiningBoreHeight = (_wicoBlockMaster.HeightInMeters() - _wicoBlockMaster.BlockMultiplier() * 2);
                    //                MiningBoreWidth = (_wicoBlockMaster.WidthInMeters() - _wicoBlockMaster.BlockMultiplier() * 2);

                    // save defaults back to customdata to allow player to change
                    INIHolder iniCustomData = new INIHolder(this, Me.CustomData);
                    iniCustomData.SetValue(sMiningSection, "MiningBoreHeight", MiningBoreHeight.ToString("0.00"));
                    iniCustomData.SetValue(sMiningSection, "MiningBoreWidth", MiningBoreWidth.ToString("0.00"));
                    // informational for the player
                    iniCustomData.SetValue(sMiningSection, "ShipWidth", _wicoBlockMaster.WidthInMeters().ToString("0.00"));
                    iniCustomData.SetValue(sMiningSection, "ShipHeight", _wicoBlockMaster.HeightInMeters().ToString("0.00"));

                    Me.CustomData = iniCustomData.GenerateINI(true);
                }
            }

            void MinerCalculateAsteroidVector(long AsteroidID)
            {
                BoundingBoxD bbd = AsteroidGetBB(AsteroidID);

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
                addDetectedEntity(mydei);
                if (mydei.Type == MyDetectedEntityType.Asteroid)
                {
                    AsteroidAdd(mydei);
                    /*
                    if (!bValidAsteroid)
                    {
                        bValidAsteroid = true;
                        vTargetAsteroid = mydei.Position;
                        //                currentAst.EntityId = mydei.EntityId;
                        //                currentAst.BoundingBox = mydei.BoundingBox;
                        if (mydei.HitPosition != null) vExpectedAsteroidExit = (Vector3D)mydei.HitPosition - _wicoBlockMaster.GetMainController().GetPosition();
                        else vExpectedAsteroidExit = vTargetAsteroid - _wicoBlockMaster.GetMainController().GetPosition();
                        vExpectedAsteroidExit.Normalize();
                        bValidExit = true;
                    }
                    */
                }
            }

        }
    }
}
