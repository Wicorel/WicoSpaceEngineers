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
        public class Navigation : NavCommon
        {



            Program _program;
            WicoControl _wicoControl;
            WicoBlockMaster _wicoBlockMaster;
            WicoIGC _wicoIGC;
            TravelMovement _travelMovement;
            WicoElapsedTime _wicoElapsedTime;
            WicoGyros _gyros;
            Wheels _wheels;
            NavRotors _navRotors;
            WicoThrusters _wicoThrusters;
            Displays _displays;

            bool _Debug = false;

            public Navigation(Program program, WicoControl wc, WicoBlockMaster wbm, WicoIGC wicoIGC, TravelMovement travelMovement,
                WicoElapsedTime wicoElapsedTime, WicoGyros wicoGyros,
                Wheels wicoWheels, NavRotors navRotors, WicoThrusters wicoThrusters
                ,Displays displays
                ): base(program)
            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
                _wicoIGC = wicoIGC;
                _travelMovement = travelMovement;
                _wicoElapsedTime = wicoElapsedTime;
                _gyros = wicoGyros;
                _wheels = wicoWheels;
                _navRotors = navRotors;
                _wicoThrusters = wicoThrusters;
                _displays = displays;


                _program.moduleName += " Navigation";
                _program.moduleList += "\nNavigation V4.2";

                NAVEmulateOld=_program._CustomDataIni.Get(sNavSection, "NAVEmulateOld").ToBoolean(NAVEmulateOld);
                _program._CustomDataIni.Set(sNavSection, "NAVEmulateOld", NAVEmulateOld);

                bAutoPatrol = _program._CustomDataIni.Get(sNavSection, "AutoPatrol").ToBoolean(bAutoPatrol);
                _program._CustomDataIni.Set(sNavSection, "AutoPatrol", bAutoPatrol);

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVADDTARGET, BroadcastHandler);
                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVRESET,     BroadcastHandler);
                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVSTART,     BroadcastHandler);

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

                        if (iMode == WicoControl.MODE_GOINGTARGET
                            || iMode == WicoControl.MODE_STARTNAV
                            || iMode == WicoControl.MODE_NAVNEXTTARGET
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
                            tsurface.FontSize = 2;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.5f;
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
                Vector3D v3D;

                Vector3D.TryParse(Ini.Get(sNavSection, "vTarget").ToString(), out v3D);
                VNavTarget = v3D;
//                _program.ErrorLog("On Load, target=" + VNavTarget.ToString());

                Ini.Set(sNavSection, "vTarget", VNavTarget.ToString());

                Vector3D.TryParse(Ini.Get(sNavSection, "vAvoid").ToString(), out v3D);
                vAvoid = v3D;
                Ini.Set(sNavSection, "vAvoid", vAvoid.ToString());

                Vector3D.TryParse(Ini.Get(sNavSection, "vBestThrustOrientation").ToString(), out v3D);
                vBestThrustOrientation = v3D;
                Ini.Set(sNavSection, "vBestThrustOrientation", vBestThrustOrientation.ToString());

                BValidNavTarget = Ini.Get(sNavSection, "ValidNavTarget").ToBoolean();
                NAVTargetName= Ini.Get(sNavSection, "ValidNavTarget").ToString();

                dtNavStartShip=DateTime.FromBinary(Ini.Get(sNavSection, "dStartShip").ToInt64());
                ShipSpeedMax=Ini.Get(sNavSection, "shipSpeedMax").ToDouble(_wicoControl.fMaxWorldMps);
                ArrivalDistanceMin = Ini.Get(sNavSection, "arrivalDistanceMin").ToDouble();
                NAVArrivalMode = Ini.Get(sNavSection, "NAVArrivalMode").ToInt32();
                NAVArrivalState = Ini.Get(sNavSection, "NAVArrivalState").ToInt32();

                _NavCommandsLoad(Ini);
            }

            void SaveHandler(MyIni Ini)
            {
                Ini.Set(sNavSection, "vTarget", VNavTarget.ToString());
                Ini.Set(sNavSection, "vAvoid", vAvoid.ToString());
                Ini.Set(sNavSection, "vBestThrustOrientation", vBestThrustOrientation.ToString());
                Ini.Set(sNavSection, "ValidNavTarget", BValidNavTarget);
                Ini.Set(sNavSection, "TargetName", NAVTargetName);

                Ini.Set(sNavSection, "dStartShip", dtNavStartShip.ToBinary());
                Ini.Set(sNavSection, "shipSpeedMax", ShipSpeedMax);
                Ini.Set(sNavSection, "arrivalDistanceMin", ArrivalDistanceMin);
                Ini.Set(sNavSection, "NAVArrivalMode", NAVArrivalMode);
                Ini.Set(sNavSection, "NAVArrivalState", NAVArrivalState);

                _NavCommandsSave(Ini);
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
                if (fromMode == WicoControl.MODE_GOINGTARGET)
                {
                    _gyros.gyrosOff();
                    _wicoThrusters.powerDownThrusters();
                    _travelMovement.ResetTravelMovement(_wicoElapsedTime);
                }
                if (fromMode == WicoControl.MODE_GOINGTARGET
                   || fromMode == WicoControl.MODE_STARTNAV
                   || fromMode == WicoControl.MODE_NAVNEXTTARGET
                   )
                {
                    _displays.ClearDisplays("MODE");
                }
                // need to check if this is us
                if (toMode == WicoControl.MODE_GOINGTARGET
                    || toMode == WicoControl.MODE_STARTNAV
                    || toMode == WicoControl.MODE_NAVNEXTTARGET
                    )
                {
                    _wicoControl.WantOnce();
                }
                if(toMode<=0 || toMode== WicoControl.MODE_ATTENTION)
                {
                    NavReset();
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_GOINGTARGET)
                {
                    // TODO: Check state and re-init as needed
                    _wicoControl.WantFast();
                }
                if (iMode == WicoControl.MODE_STARTNAV)
                {
                    // TODO: Check state and re-init as needed
                    _wicoControl.WantFast();
                }
                if (iMode == WicoControl.MODE_NAVNEXTTARGET)
                {
                    // TODO: Check state and re-init as needed
                    _wicoControl.WantFast();
                }
                if (iMode == WicoControl.MODE_ARRIVEDTARGET)
                {
                    // TODO: Check state and re-init as needed
                    _wicoControl.WantOnce();
                }
                if(iMode == 0)
                {
                    NavReset();
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

                bool bFoundNAVCommands = false;
//                _program.ErrorLog("#Args=" + varArgs.Length);
                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
//                    _program.ErrorLog("Arg[" + iArg + "]=" + varArgs[iArg]);

                    if (args[0] == "W" || args[0] == "O")
                    { // [W|O] <x>:<y>:<z>  || W <x>,<y>,<z>
                      // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                      // O means orient towards.  W means orient, then move to
                        bFoundNAVCommands = true;
                        _program.Echo("Args:");
                        for (int icoord = 0; icoord < args.Length; icoord++)
                            _program.Echo(args[icoord]);
                        if (args.Length < 1)
                        {
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        string sArg = args[1].Trim();

                        if (args.Length > 2)
                        {
                            sArg = args[1];
                            for (int kk = 2; kk < args.Length; kk++)
                                sArg += " " + args[kk];
                            sArg = sArg.Trim();
                        }

                        //                    Echo("sArg=\n'" + sArg+"'");
                        string[] coordinates = sArg.Split(',');
                        if (coordinates.Length < 3)
                        {
                            coordinates = sArg.Split(':');
                        }
                        //                    Echo(coordinates.Length + " Coordinates");
                        for (int icoord = 0; icoord < coordinates.Length; icoord++)
                            _program.Echo(coordinates[icoord]);
                        //Echo("coordiantes.Length="+coordinates.Length);  
                        if (coordinates.Length < 3)
                        {
                            //Echo("P:B");  

                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            _gyros.gyrosOff();// shutdown(gyroList);
                            return;
                        }
                        int iCoordinate = 0;
                        string sWaypointName = "Waypoint";
                        // -  0   1           2        3          4       5
                        // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                        if (coordinates[0] == "GPS")
                        {
                            if (coordinates.Length > 4)
                            {
                                sWaypointName = coordinates[1];
                                iCoordinate = 2;
                            }
                            else
                            {
                                _program.Echo("Invalid Command");
                                _gyros.gyrosOff();
                                return;
                            }
                        }

                        double x, y, z;
                        bool xOk = double.TryParse(coordinates[iCoordinate++].Trim(), out x);
                        bool yOk = double.TryParse(coordinates[iCoordinate++].Trim(), out y);
                        bool zOk = double.TryParse(coordinates[iCoordinate++].Trim(), out z);
                        if (!xOk || !yOk || !zOk)
                        {
                            //Echo("P:C");  
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            //			shutdown(gyroList);
                            continue;
                        }

                        //                    _program.sMasterReporting = "CMD Initiated NAV:\n" + sArgument;

                        //                    _vNavTarget = new Vector3D(x, y, z);
                        //                    BValidNavTarget = true;
                        if (args[0] == "W")
                        {
                            _NavAddTarget(new Vector3D(x, y, z), sWaypointName, true, WicoControl.MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin, ShipSpeedMax);
                            //                        bGoOption = true;
                        }
                        else
                        {
                            _NavAddTarget(new Vector3D(x, y, z), sWaypointName, false, WicoControl.MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin, ShipSpeedMax);
                            //                        bGoOption = false;
                        }
                    }
                    else if (args[0] == "S")
                    { // S <mps>
                      // TODO: Queue the command into NavCommands
                        if (args.Length < 1)
                        {
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        double x;
                        bool xOk = double.TryParse(args[1].Trim(), out x);
                        if (xOk)
                        {
                            ShipSpeedMax = x;
                        }
                        else
                        {
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                    }
                    else if (args[0] == "D")
                    { // D <meters>
                      // TODO: Queue the command into NavCommands
                        if (args.Length < 1)
                        {
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        double x;
                        bool xOk = double.TryParse(args[1].Trim(), out x);
                        if (xOk)
                        {
                            ArrivalDistanceMin = x;
                            //                        Echo("Set arrival distance to:" + ArrivalDistanceMin.ToString("0.00"));
                        }

                        else
                        {
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                    }
                    else if (args[0] == "C")
                    { // C <anything>
                        if (args.Length < 1)
                        {
                            _program.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        else
                        {
                            _program.Echo(varArgs[iArg]);
                        }
                    }
                    else if (args[0] == "L")
                    { // L launch
                        bFoundNAVCommands = true;
                        NavQueueMode(WicoControl.MODE_LAUNCH);
                    }
                    else if (args[0] == "launch")
                    { // L launch
                        bFoundNAVCommands = true;
                        NavQueueMode(WicoControl.MODE_LAUNCH);
                    }
                    else if (args[0] == "OL")
                    { // OL Orbital launch
                        bFoundNAVCommands = true;
                        NavQueueMode(WicoControl.MODE_ORBITALLAUNCH);
                    }
                    else if (args[0] == "orbitallaunch")
                    { // OL Orbital launch
                        bFoundNAVCommands = true;
                        NavQueueMode(WicoControl.MODE_ORBITALLAUNCH);
                    }
                    else if (args[0] == "dock")
                    { // dock
                        bFoundNAVCommands = true;
                        NavQueueMode(WicoControl.MODE_DOCKING);
                    }
                    else if (args[0] == "patrol")
                    {
                        bFoundNAVCommands = StartPatrol();
                    }
                    else if(args[0]=="autopatrol")
                    {
                        bAutoPatrol = !bAutoPatrol;
                        string s="AutoPatrol="+bAutoPatrol.ToString();
                        _program.Echo(s);
                        if (_Debug) _program.ErrorLog(s);
                    }
                }
                if (bFoundNAVCommands)
                {
//                     _program.sMasterReporting += "\nFound NAV Commands:" + wicoNavCommands.Count.ToString();
                    _NavStart();
                }
                if (myCommandLine != null)
                {
                    for (int arg = 0; arg < myCommandLine.ArgumentCount; arg++)
                    {
                        string sArg = myCommandLine.Argument(arg);
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

//                _program.Echo("NAV: UpdateHandler");

                // need to check if this is us
                if (iMode == WicoControl.MODE_GOINGTARGET)
                {
                    doModeGoTarget();
                }
                if (iMode == WicoControl.MODE_STARTNAV) { doModeStartNav(); return; }
                if (iMode == WicoControl.MODE_NAVNEXTTARGET) { doModeNavNext(); return; }
                if (iMode == WicoControl.MODE_ARRIVEDTARGET) 
                {
                    sbNotices.Clear();
                    sbModeInfo.Clear();
                    sbModeInfo.AppendLine("Arrived Target");
                    if (bAutoPatrol)
                    {
                        sbNotices.AppendLine(" restart AutoPatrol!");
                        if (_Debug) _program.ErrorLog("Arrived: AutoPatrol!");
                        bool bFoundNav = StartPatrol();
                        if (bFoundNav) _NavStart();
                    }
                }
            }

            void BroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
                if(msg.Tag==NavCommon.WICOB_NAVADDTARGET)
                {
                    if (msg.Data is string)
                    {
                        Vector3D vTarget;
                        int modeArrival;
                        int stateArrival;
                        double DistanceMin;
                        string TargetName;
                        double maxSpeed;
                        bool bGo;
                        NavCommon.NAVDeserializeCommand(msg.Data.ToString(), out vTarget, out modeArrival, out stateArrival, out DistanceMin, out TargetName, out maxSpeed, out bGo);
                        _NavGoTarget(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                    }

                }
                    else if(msg.Tag==NavCommon.WICOB_NAVRESET)
                {
//                    _program.ErrorLog("Received: " + msg.Tag);
                    if (msg.Data is string)
                    {
                        NavReset();
                    }

                }
                else if(msg.Tag==NavCommon.WICOB_NAVSTART)
                {
                    if (msg.Data is string)
                    {
//                        _program.ErrorLog("Received: "+msg.Tag);
                        _NavStart();
                    }
                }
            }

            Vector3D vAvoid;

            /// <summary>
            ///  GRID orientation to aim ship
            /// </summary>
            Vector3D vBestThrustOrientation;

            bool bAutoPatrol = false;

            /// <summary>
            /// We are a sled. Default false
            /// </summary>
            bool bSled = false;

            /// <summary>
            /// We are rotor-control propulsion. Default false
            /// </summary>
            bool bRotor = false;
            /// <summary>
            /// We are wheel propulsion.
            /// </summary>
            bool bWheels = false;

            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();
            /*
            States:
            0 -- Master Init


                150. (spawn) initialize command in gravity. first align to gravity (created for EFM)
                151
                155 aim at location using YAW only.
                145 realign with gravity

            160 Main Travel to target

                161 debug hold state (doesn't advance to anything)


            *** below here are thruster-only routines (for now)

            300 Collision Detected From 160
                Calculate collision avoidance 
                then ->320

            301 dummy state for debugging.
            320 do travel movement for collision avoidance. 
            if arrive target, ->160
            if secondary collision ->340

            340 secondary collision
            Ship grid->345
            if a type we can move around, try to move ->350
            else go back to collision detection ->300

                345 calculate avoidance from grid
                348 calculate best thrust
                349 dupe?

            350 initilize escape plan

            ->360

            360 scan for an 'escape' route (pathfind)
            timeout of (default) 5 seconds ->MODE_ATTENTION
            after scans, ->380

            380 travel to avoidance waypoint
            on arrival ->160 (main travel)
            on collision ->340

            500 Arrived at target
            ->MODE_ARRIVEDTARGET

            */

            Vector3D GridUpVector;
            Vector3D GridRightVector;
            DateTime dtNavStartShip;

            public Vector3D VNavTarget { get; set; }

            public bool BValidNavTarget { get; set; } = false;
            public bool BGoOption { get; set; } = false;

            /// <summary>
            /// Set maximum travel speed of ship. 
            /// Set this using S command for NAV
            /// </summary>
            public double ShipSpeedMax { get; set; } = 9999;

            /// <summary>
            /// the minimum distance to be from the target to be considered 'arrived'
            /// </summary>
            public double ArrivalDistanceMin { get; set; } = 50;

            public int NAVArrivalMode { get; set; } = WicoControl.MODE_NAVNEXTTARGET;
            public int NAVArrivalState { get; set; } = 0;

            public string NAVTargetName { get; set; } = "";

            bool NAVEmulateOld = false;

// TODO: Move to TravelMovement            bool AllowBlindNav = false;

//            bool bNavBeaconDebug = false;

            string sNavSection = "NAV";

            void doModeGoTarget()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbModeInfo.Clear();

                sbModeInfo.AppendLine("Going to Target");
                _program.Echo("Going Target: state=" + iState.ToString());
                if (NAVTargetName != "")
                {
                    sbModeInfo.AppendLine(" " + NAVTargetName);
                    _program.Echo(NAVTargetName);
                }

                string sNavDebug = "";
                sNavDebug += "GT:S=" + iState;
                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dGravity = vNG.Length();

                if(thrustForwardList.Count<1)
                {
                    _wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }

                if (iState == 0)
                {
                    Matrix or1;
                    thrustForwardList[0].Orientation.GetMatrix(out or1);
                    vBestThrustOrientation = or1.Forward; // start out aiming at whatever the FW thrusters are aiming at..

                    _travelMovement.ResetTravelMovement(_wicoElapsedTime);

                    if (_wheels.HasSledWheels())
                    {
                        bSled = true;
                        if (ShipSpeedMax > 45) ShipSpeedMax = 45;
                    }
                    else bSled = false;

                    if (_navRotors.NavRotorCount() > 0)
                    {
                        bRotor = true;
                        if (ShipSpeedMax > 15) ShipSpeedMax = 15;
                    }
                    else bRotor = false;
                    if (_wheels.HasWheels())
                    {
                        bWheels = true;
                        //                   if (_shipSpeedMax > 15) _shipSpeedMax = 15;
                    }
                    else bWheels = false;

                    // TODO: Put a timer on this so it's not done Update1
                    double elevation = 0;
                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);

                    if (!bSled && !bRotor)
                    { // if flying ship
                      // make sure set to default
                        if (_wicoBlockMaster.DesiredMinTravelElevation < 0)
                            _wicoBlockMaster.DesiredMinTravelElevation = 75; // for EFM getting to target 'arrived' radius
                    }

                    if (dGravity > 0)
                        _wicoControl.SetState(160);
                    else
                    if (BValidNavTarget)
                    {
                        if (elevation > _wicoBlockMaster.HeightInMeters())
                        {
                            _wicoControl.SetState(150);
                        }
                        else _wicoControl.SetState(160);
                    }
                    else _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                    _wicoControl.WantFast();
                }
                else if (iState == 150)
                {
                    _wicoControl.WantFast();
                    Vector3D vTargetLocation = VNavTarget;
                    if (dGravity > 0)
                    {

                        double elevation = 0;

                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");

                        float fSaveAngle = _gyros.GetMinAngle();
                        _gyros.SetMinAngle(0.1f);

                        bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        _program.Echo("bAligned=" + bAligned.ToString());
                        _gyros.SetMinAngle(fSaveAngle);
                        if (bAligned || elevation < _wicoBlockMaster.HeightInMeters() * 2)
                        {
                            _gyros.gyrosOff();
                            if (_wicoBlockMaster.DesiredMinTravelElevation > 0)
                                _wicoControl.SetState(155);
                            else _wicoControl.SetState(160);
                        }
                    }
                    else _wicoControl.SetState(160);

                }
                else if (iState == 151)
                {
                    _wicoControl.WantFast();
                    if (dGravity > 0 || bWheels)
                    {
                        double elevation = 0;

                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");

                        float fSaveAngle = _gyros.GetMinAngle();
                        _gyros.SetMinAngle(0.1f);

                        bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        _program.Echo("bAligned=" + bAligned.ToString());
                        _gyros.SetMinAngle(fSaveAngle);
                        if (bAligned || elevation < _wicoBlockMaster.HeightInMeters() * 2)
                        {
                            _gyros.gyrosOff();
                            if (_wicoBlockMaster.DesiredMinTravelElevation > 0)
                                _wicoControl.SetState(155);
                            else _wicoControl.SetState(160);
                        }
                        else _wicoControl.SetState(150);// try again to be aligned.
                    }
                    else _wicoControl.SetState(160);

                }
                else if (iState == 155)
                { // for use in gravity: aim at location using yaw only
                    _wicoControl.WantFast();// bWantFast = true;
                    if (bWheels)
                    {
                        _wicoControl.SetState(160);
                        return;
                    }

                    if (dGravity > 0)
                    {
                        bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        double yawangle = -999;
                        yawangle = _program.CalculateYaw(VNavTarget, shipController);
                        bool bAimed = Math.Abs(yawangle) < 0.1; // NOTE: 2x allowance
                        _program.Echo("yawangle=" + yawangle.ToString());
                        sNavDebug += " Yaw=" + yawangle.ToString("0.00");

                        if (!bAimed)
                        {
                            if (bRotor)
                            {
                                _program.Echo("Rotor");
                                _navRotors.DoRotorRotate(yawangle);
                            }
                            else // use for both sled and flight
                            {
                                _gyros.DoRotate(yawangle, "Yaw");
                            }
                        }
                        if (bAligned && bAimed)
                        {
                            _gyros.gyrosOff();
                            _wicoControl.SetState(160);
                        }
                        else if (bAligned && Math.Abs(yawangle) < 0.5)
                        {
                            float atmo;
                            float hydro;
                            float ion;

                            _wicoThrusters.CalculateHoverThrust(shipController, thrustForwardList, out atmo, out hydro, out ion);
                            atmo += 1;
                            hydro += 1;
                            ion += 1;

                            _wicoThrusters.powerUpThrusters(thrustForwardList, atmo, WicoThrusters.thrustatmo);
                            _wicoThrusters.powerUpThrusters(thrustForwardList, hydro, WicoThrusters.thrusthydro);
                            _wicoThrusters.powerUpThrusters(thrustForwardList, ion, WicoThrusters.thrustion);

                        }
                        else
                            _wicoThrusters.powerDownThrusters(thrustForwardList);
                    }
                    else _wicoControl.SetState(160);
                }
                else if (iState == 156)
                {
                    // realign gravity
                    _wicoControl.WantFast();
                    bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                    if (bAimed)
                    {
                        _gyros.gyrosOff();
                        _wicoControl.SetState(160);
                    }
                }
                else if (iState == 160)
                { //	160 move to Target
                    _program.EchoInstructions("NAV:160");
                    sbNotices.AppendLine("Moving to Target");
                    _program.Echo("Moving to Target");
                    _program.Echo("Target="+VNavTarget.ToString());
                    Vector3D vTargetLocation = VNavTarget;
                    double velocityShip = shipController.GetShipSpeed();

                    Vector3D vVec = vTargetLocation - shipController.GetPosition();
                    double distance = vVec.Length();
                    sbModeInfo.AppendLine("distance=" + _program.niceDoubleMeters(distance));
                    _program.Echo("distance=" + _program.niceDoubleMeters(distance));
//                    sbNotices.AppendLine("ArrivalDistanceMin=" + _program.niceDoubleMeters(ArrivalDistanceMin));
                    _program.Echo("ArrivalDistanceMin=" + _program.niceDoubleMeters(ArrivalDistanceMin));
                    sbNotices.AppendLine("velocity=" + velocityShip.ToString("0.00"));
                    _program.Echo("velocity=" + velocityShip.ToString("0.00"));

                    string sTarget = "Moving to Target";
                    if (NAVTargetName != "") sTarget = "Moving to " + NAVTargetName;


                    if (
                        BGoOption && 
                        (distance < ArrivalDistanceMin))
                    {
                        _wicoControl.SetState(500);

                        sbNotices.AppendLine("We have arrived");
                        _program.Echo("we have arrived");
                        _wicoControl.WantFast();
                        return;
                    }

                    bool bDoTravel = false;
                    if (BGoOption)
                        bDoTravel = true;

                    if (_wicoBlockMaster.DesiredMinTravelElevation > 0 && dGravity > 0)
                    {
                        double elevation = 0;

                        MyShipVelocities mysSV = shipController.GetShipVelocities();
                        Vector3D lv = mysSV.LinearVelocity;

                        // ASSUMES: -up = gravity down  Assuming ship orientation
                        var upVec = shipController.WorldMatrix.Up;
                        var vertVel = Vector3D.Dot(lv, upVec);

                        // NOTE: Elevation is only updated by game every 30? ticks. so it can be WAY out of date based on movement
                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");
                        sNavDebug += " V=" + velocityShip.ToString("0.00");

                        sbNotices.AppendLine("Elevation=" + elevation.ToString("0.0"));
                        _program.Echo("Elevation=" + elevation.ToString("0.0"));
                        _program.Echo("MinEle=" + _wicoBlockMaster.DesiredMinTravelElevation.ToString("0.0"));

                        double stopD = 0;
                        if (vertVel < 0)
                        {
                            stopD = _wicoThrusters.calculateStoppingDistance(shipController, thrustUpList, Math.Abs(vertVel), dGravity);
                        }
                        double maxStopD = _wicoThrusters.calculateStoppingDistance(shipController, thrustUpList, _wicoControl.fMaxWorldMps, dGravity);

                        float atmo;
                        float hydro;
                        float ion;
                        _wicoThrusters.CalculateHoverThrust(shipController, thrustUpList, out atmo, out hydro, out ion);

                        if (
                            //                        !bSled && !bRotor && 
                            _wicoBlockMaster.DesiredMinTravelElevation > 0)
                        {
                            if (
                                vertVel < -0.5  // we are going downwards
                                && (elevation - stopD * 2) < _wicoBlockMaster.DesiredMinTravelElevation)
                            { // too low. go higher
                              // Emergency thrust
                                sNavDebug += " EM UP!";

                                bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);

                                _wicoThrusters.powerUpThrusters(thrustUpList, 100);
                                bDoTravel = false;
                                _wicoControl.WantFast();
                            }
                            else if (elevation < _wicoBlockMaster.DesiredMinTravelElevation)
                            {
                                // push upwards
                                atmo += Math.Min(5f, (float)ShipSpeedMax);
                                hydro += Math.Min(5f, (float)ShipSpeedMax);
                                ion += Math.Min(5f, (float)ShipSpeedMax);
                                sNavDebug += " UP! A" + atmo.ToString("0.00");// + " H"+hydro.ToString("0.00") + " I"+ion.ToString("0.00");
                                _wicoThrusters.powerUpThrusters(thrustUpList, atmo, WicoThrusters.thrustatmo);
                                _wicoThrusters.powerUpThrusters(thrustUpList, hydro, WicoThrusters.thrusthydro);
                                _wicoThrusters.powerUpThrusters(thrustUpList, ion, WicoThrusters.thrustion);

                            }
                            else if (elevation > (maxStopD + _wicoBlockMaster.DesiredMinTravelElevation * 1.25))
                            {
                                // if we are higher than maximum possible stopping distance, go down fast.
                                sNavDebug += " SUPERHIGH";

                                //                           Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                                //                            bool bAligned = GyroMain("", grav, shipOrientationBlock);

                                _wicoThrusters.powerDownThrusters(thrustUpList, WicoThrusters.thrustAll, true);
                                bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                                if (!bAligned)
                                {
                                    _wicoControl.WantFast();
                                    bDoTravel = false;
                                }
                            }
                            else if (
                                elevation > _wicoBlockMaster.DesiredMinTravelElevation * 2  
                                )
                            { // too high 
                                sNavDebug += " HIGH";
                                //DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");

                                if (vertVel > 2) // going up
                                { // turn off thrusters.
                                    sNavDebug += " ^";
                                    _wicoThrusters.powerDownThrusters(thrustUpList, WicoThrusters.thrustAll, true);
                                }
                                else if (vertVel < -0.5) // going down
                                {
                                    sNavDebug += " v";
                                    if (vertVel > (-Math.Min(15, ShipSpeedMax)))
                                    {
                                        // currently descending at less than desired
                                        atmo -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                        hydro -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                        ion -= Math.Max(25f, Math.Min(5f, (float)velocityShip / 2));
                                        sNavDebug += " DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");
                                                                                        //                                   bDoTravel = false;
                                    }
                                    else
                                    {
                                        // we are descending too fast.
                                        atmo += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                        hydro += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                        ion += Math.Max(100f, Math.Min(5f, (float)velocityShip / 2));
                                        sNavDebug += " 2FAST! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");

                                        bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                                        if (!bAligned)
                                        {
                                            _wicoControl.WantFast();
                                            bDoTravel = false;
                                        }
                                    }

                                }
                                else
                                {
                                    sNavDebug += " -";
                                    atmo -= 5;
                                    hydro -= 5;
                                    ion -= 5;
                                }

                                _wicoThrusters.powerUpThrusters(thrustUpList, atmo, WicoThrusters.thrustatmo);
                                _wicoThrusters.powerUpThrusters(thrustUpList, hydro, WicoThrusters.thrusthydro);
                                _wicoThrusters.powerUpThrusters(thrustUpList, ion, WicoThrusters.thrustion);

                            }
                            else
                            {
                                // normal hover
                                _wicoThrusters.powerDownThrusters(thrustUpList);
                            }
                        }
                    }
                    if (bDoTravel)
                    {
                        _program.Echo("Do Travel");
//                        _travelMovement.InitDoTravelMovement(vTargetLocation, ShipSpeedMax, (float)ArrivalDistanceMin, _wicoBlockMaster.GetMainController());
//                        _wicoControl.SetState(161);
                        _travelMovement.doTravelMovement(_wicoElapsedTime, vTargetLocation, (float)ArrivalDistanceMin, 500, 300, ShipSpeedMax);
                    }
                    else
                    {
                        _wicoThrusters.powerDownThrusters(thrustForwardList);
                        sbNotices.AppendLine("Aim Only");
                        _program.Echo("Aim Only");
                        Matrix or1;
                        shipController.Orientation.GetMatrix(out or1);
                        Vector3D vDirection = or1.Forward;
                        _wicoControl.WantFast();
                        if (_gyros.AlignGyros(vDirection, vVec))
                        {
                            _wicoControl.SetState(500);// iState = 500;
                            _program.Echo("We are now Aimed");
                        }
                    }
                }
                else if(iState==161)
                    {
                    // Holding
                    _program.Echo("Hodling for DEBUG");
                }
                else if (iState == 300)
                { // collision detection

                    _wicoControl.WantFast();
                    Vector3D vTargetLocation = VNavTarget;
                    _travelMovement.ResetTravelMovement(_wicoElapsedTime);
                    _travelMovement.calcCollisionAvoid(vTargetLocation, _travelMovement.LastDetectedInfo, out vAvoid);

                    //                iState = 301; // testing
                    _wicoControl.SetState(320);
                }
                else if (iState == 301)
                {
                    // just hold this state
                    _wicoControl.WantFast();
                }

                else if (iState == 320)
                {
                    sbNotices.AppendLine("Primary Collision Avoid");
                    _program.Echo("Primary Collision Avoid");
                    //                    StatusLog("clear", sledReport);
                    //                    StatusLog("Collision Avoid", sledReport);
                    //                    StatusLog("Collision Avoid", textPanelReport);
                    Vector3D vVec = vAvoid - shipController.GetPosition();
                    double distance = vVec.Length();
                    double velocityShip = shipController.GetShipSpeed();
                    sbModeInfo.AppendLine("distance=" + _program.niceDoubleMeters(distance));
                    sbNotices.AppendLine("velocity=" + velocityShip.ToString("0.00"));
                    _travelMovement.doTravelMovement(_wicoElapsedTime, vAvoid, 5.0f, 160, 340, ShipSpeedMax);
                }
                else if (iState == 340)
                {       // secondary collision
                    if (
                        _travelMovement.LastDetectedInfo.Type == MyDetectedEntityType.LargeGrid
                        || _travelMovement.LastDetectedInfo.Type == MyDetectedEntityType.SmallGrid
                        )
                    {
                        _wicoControl.SetState(345);
                    }
                    else if (_travelMovement.LastDetectedInfo.Type == MyDetectedEntityType.Asteroid
                        )
                    {
                        _wicoControl.SetState(350);
                    }
                    else _wicoControl.SetState(300);
                    _wicoControl.WantFast();
                }
                else if (iState == 345)
                {
                    // we hit a grid.  align to it
                    Vector3D[] corners = new Vector3D[BoundingBoxD.CornerCount];

                    BoundingBoxD bbd = _travelMovement.LastDetectedInfo.BoundingBox;
                    bbd.GetCorners(corners);

                    GridUpVector = _program.wicoSensors.PlanarNormal(corners[3], corners[4], corners[7]);
                    GridRightVector = _program.wicoSensors.PlanarNormal(corners[0], corners[1], corners[4]);
                    _wicoControl.WantFast();
                    _wicoControl.SetState(348);
                }
                else if (iState == 348)
                {
                    _wicoControl.WantFast();
                    Matrix or1;
                    Vector3 vOrientation;
                    thrustUpList[0].Orientation.GetMatrix(out or1);
                    vOrientation = or1.Forward;
                    if(_gyros.AlignGyros(vBestThrustOrientation, vNG))
                    {
                        _wicoControl.SetState(349);
                    }
                }
                else if (iState == 349)
                {
                    _wicoControl.WantFast();
                    Matrix or1;
                    Vector3 vOrientation;
                    thrustRightList[0].Orientation.GetMatrix(out or1);
                    vOrientation = or1.Forward;
                    if (_gyros.AlignGyros(vBestThrustOrientation, vNG))
                    {
                        _wicoControl.SetState(350);
                    }
                }
                else if (iState == 350)
                {
                    _travelMovement.ResetTravelMovement(_wicoElapsedTime);
                    _travelMovement.initEscapeScan(shipController,_travelMovement.bCollisionWasSensor);
                    dtNavStartShip = DateTime.Now;
                    _wicoControl.SetState(360);
                    _wicoControl.WantFast();
                }
                else if (iState == 360)
                {
                    sbNotices.AppendLine("Collision Avoid");
                    sbNotices.AppendLine(" Scan for escape route");
                    _program.Echo("Collision Avoid");
                    //                    StatusLog("Collision Avoid\nScan for escape route", textPanelReport);
                    DateTime dtMaxWait = dtNavStartShip.AddSeconds(5.0f);
                    DateTime dtNow = DateTime.Now;
                    if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                    {
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                        return;
                    }
                    _program.Echo("ScanEscape()");
                    if (_travelMovement.scanEscape())
                    {
                        _program.Echo("ESCAPE!");
                        _wicoControl.SetState(380);
                    }
                    _program.Echo("Post ScanEscape()");
                    _wicoControl.WantMedium(); 
                }
                else if (iState == 380)
                {
                    //                    StatusLog("Collision Avoid Travel", textPanelReport);
                    sbNotices.AppendLine("Escape Collision Avoid");
                    _program.Echo("Escape Collision Avoid");
                    Vector3D vVec = vAvoid - shipController.GetPosition();
                    double distance = vVec.Length();
                    double velocityShip = shipController.GetShipSpeed();
                    sbModeInfo.AppendLine("distance=" + _program.niceDoubleMeters(distance));
                    sbNotices.AppendLine("velocity=" + velocityShip.ToString("0.00"));
                    _travelMovement.doTravelMovement(_wicoElapsedTime, vAvoid, 1f, 160, 340, ShipSpeedMax);
                }
                else if (iState == 500)
                { // we have arrived at target

                    {

                        //                        StatusLog("clear", sledReport);
                        //                        StatusLog("Arrived at Target", sledReport);
                        //                        StatusLog("Arrived at Target", textPanelReport);
                        sNavDebug += " ARRIVED!";
                        sbNotices.AppendLine("Arrived at Target");
                        _program.ResetMotion();
                        BValidNavTarget = false; // we used this one up.
                                                 //                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                        _program.wicoAntennas.SetMaxPower(false);
                        _program.wicoSensors.SensorsSleepAll();
                        //                    _program.sMasterReporting += "Finish WP:" + wicoNavCommands.Count.ToString()+":"+NAVArrivalMode.ToString();
                        // set to desired mode and state
                        _wicoControl.SetMode(NAVArrivalMode, NAVArrivalState);

                        // set up defaults for next run (in case they had been changed)
                        NAVArrivalMode = WicoControl.MODE_NAVNEXTTARGET;
                        NAVArrivalState = 0;
                        NAVTargetName = "";
                        BGoOption = true;

                        if (NAVEmulateOld)
                        {
                            var tList = _wicoBlockMaster.GetBlocksContains<IMyTerminalBlock>("NAV:");
                            for (int i1 = 0; i1 < tList.Count(); i1++)
                            {
                                // don't want to get blocks that have "NAV:" in customdata..
                                if (tList[i1].CustomName.StartsWith("NAV:"))
                                {
                                    _program.Echo("Found NAV: command:");
                                    tList[i1].CustomName = "NAV: C Arrived Target";
                                }
                            }
                        }
                    }
                    _wicoControl.WantFast();
                }
                //                NavDebug(sNavDebug);
            }

            public void SetNavigation(Vector3D vNavTarget, string NavTargetName, bool bGo = true, int NavArrivalMode= WicoControl.MODE_NAVNEXTTARGET, int NavArrivalState=0, double _ArrivalDistanceMin=50, double _ShipSpeedMax=9999 )
            {
                VNavTarget = vNavTarget;
                BValidNavTarget = true;
                NAVTargetName = NavTargetName;
                BGoOption = bGo;
                NAVArrivalMode = NavArrivalMode;
                NAVArrivalState = NavArrivalState;
                ArrivalDistanceMin = _ArrivalDistanceMin;
                ShipSpeedMax = _ShipSpeedMax;
            }
            List<WicoNavCommand> wicoNavCommands = new List<WicoNavCommand>();

            string NAVCOMMANDS = "NAVCOMMANDS";
            void _NavCommandsSave(MyIni ini)
            {
                ini.Set(NAVCOMMANDS, "count", wicoNavCommands.Count);
                int current = 0;
                foreach(var nc in wicoNavCommands)
                {
                    ini.Set(NAVCOMMANDS, "command" + current, (int)nc.Command);
                    ini.Set(NAVCOMMANDS, "vNavTarget" + current, nc.vNavTarget.ToString());
                    ini.Set(NAVCOMMANDS, "NAVArrivalMode" + current, nc.NAVArrivalMode);
                    ini.Set(NAVCOMMANDS, "NAVArrivalState" + current, nc.NAVArrivalState);
                    ini.Set(NAVCOMMANDS, "shipSpeedMax" + current, nc.shipSpeedMax);
                    ini.Set(NAVCOMMANDS, "arrivalDistanceMin" + current, nc.arrivalDistanceMin);
                    ini.Set(NAVCOMMANDS, "NAVTargetName" + current, nc.NAVTargetName);
                }
            }

            void _NavCommandsLoad(MyIni ini)
            {
                int count = ini.Get(NAVCOMMANDS, "count").ToInt32(0);

                for(int current=0;current<count;current++)
                {
                    WicoNavCommand wnc = new WicoNavCommand();

                    int command= ini.Get(sNavSection, "command"+current).ToInt32();
                    Vector3D.TryParse(ini.Get(NAVCOMMANDS, "vNavTarget" + current).ToString(), out wnc.vNavTarget);
                    wnc.NAVArrivalMode = ini.Get(NAVCOMMANDS, "NAVArrivalMode" + current).ToInt32();
                    wnc.NAVArrivalState = ini.Get(NAVCOMMANDS, "NAVArrivalState" + current).ToInt32();
                    wnc.shipSpeedMax = ini.Get(NAVCOMMANDS, "shipSpeedMax" + current).ToDouble();
                    wnc.arrivalDistanceMin = ini.Get(NAVCOMMANDS, "arrivalDistanceMin" + current).ToDouble();
                    wnc.NAVTargetName = ini.Get(NAVCOMMANDS, "NAVTargetName" + current).ToString();

                    wnc.pg = _program;
                    wnc._wicoControl = _wicoControl;
                    wnc._wicoNavigation = this;

                    wicoNavCommands.Add(wnc);
                }
            }

            void _NavCommandsReset()
            {
                if(_Debug) _program.ErrorLog("NavReset()");
                wicoNavCommands.Clear();
            }

            public override void NavAddTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
//                _program.ErrorLog("NavAddTarget");
                _NavAddTarget(vTarget, TargetName, bGo, modeArrival, stateArrival, DistanceMin,  maxSpeed);
            }

            public override void NavGoTarget(Vector3D vTarget, int modeArrival = 699, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
//                _program.ErrorLog("Navigation NavGoTarget");
                _NavGoTarget(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
            }
            public override void NavQueueMode(int theMode)
            {
                _NavQueueMode(theMode);
            }

            public override void NavReset()
            {
                _NavCommandsReset();
            }

            void _NavAddTarget(Vector3D vTarget, string TargetName = "", bool bGo = true, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, double maxSpeed = 9999)
            {
//                _program.ErrorLog("_NavAddTarget()"+TargetName);

                if (maxSpeed > _wicoControl.fMaxWorldMps)
                    maxSpeed = _wicoControl.fMaxWorldMps;
                WicoNavCommand wicoNavCommand = new WicoNavCommand
                {
                    pg = _program,
                    vNavTarget = vTarget,
                    bValidNavTarget = true,
                    NAVArrivalMode = modeArrival,
                    NAVArrivalState = stateArrival,
                    arrivalDistanceMin = DistanceMin,
                    shipSpeedMax = maxSpeed,
                    NAVTargetName = TargetName,
                    _wicoControl = _wicoControl,
                    _wicoNavigation = this
                
                };
                if (bGo) wicoNavCommand.Command = CommandTypes.Waypoint;
                else wicoNavCommand.Command = CommandTypes.Orientation;

                //            _program.sMasterReporting += "Adding NAV Commnd:";
                //            _program.sMasterReporting += " Name=:" + wicoNavCommand.NAVTargetName;
                //            _program.sMasterReporting += " Loc=" + Vector3DToString(wicoNavCommand.vNavTarget);
                wicoNavCommands.Add(wicoNavCommand);
            }
            void _NavQueueMode(int theMode)
            {
                WicoNavCommand wicoNavCommand = new WicoNavCommand
                {
                    pg = _program,
                    Command = CommandTypes.SetMode,
                    SetMode = theMode
                };

                wicoNavCommands.Add(wicoNavCommand);

            }
            void _NavGoTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_ARRIVEDTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
                _NavAddTarget(vTarget, TargetName, bGo, modeArrival, stateArrival, DistanceMin, maxSpeed);
                _NavStart();
            }
            void _NavStart()
            {
                if (_Debug) _program.ErrorLog("NavStart():" + wicoNavCommands.Count + " Commands");
                if (_Debug) _program.Echo("NavStart():" + wicoNavCommands.Count + " Commands");
                if (wicoNavCommands.Count > 0)
                {
                    _wicoControl.SetMode(WicoControl.MODE_STARTNAV);
                }
                else
                {
                    _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                    _program.Echo("No Nav to start");
                    _program.sMasterReporting+="\nError:No Nav to start";
                }
            }
            void NavCommandProcessNext()
            {
                if (_Debug) _program.Echo(wicoNavCommands.Count + " Commands");
                if (_Debug) _program.ErrorLog("NavNext():" + wicoNavCommands.Count + " Commands");
                if (wicoNavCommands.Count < 1)
                {
                    _wicoControl.SetMode(NAVArrivalMode);
                    _wicoControl.SetState(NAVArrivalState);
                    // reset to defaults.
//                    _program.ErrorLog("Arrived at Target");
                    NAVArrivalMode = WicoControl.MODE_ARRIVEDTARGET;
                    NAVArrivalState = 0;
                    return;
                }
                wicoNavCommands[0].ProcessCommand();
                wicoNavCommands.RemoveAt(0);
                if (_Debug) _program.ErrorLog("EONavNext():" + wicoNavCommands.Count + " Commands");
                // should serialize these so they can resume on reload
            }

            void doModeStartNav()
            {
                if (_Debug) _program.ErrorLog("doModeStartNav()");
                NavCommandProcessNext();
                _wicoControl.WantFast();
            }
            void doModeNavNext()
            {
                if (_Debug) _program.ErrorLog("ModeNavNext:" + wicoNavCommands.Count + " Commands");
                NavCommandProcessNext();
                _wicoControl.WantFast();
            }
            bool StartPatrol()
            {
                bool bAddedTargets = false;
                var rc = _wicoBlockMaster.GetRemoteControl();
                // we need to have a remote control to get waypoints from...
                if (rc == null) return false;

                List<MyWaypointInfo> waypoints = new List<MyWaypointInfo>();
                rc.GetWaypointInfo(waypoints);

                // TODO: put nearest first and then cycle through the list
                foreach (var wp in waypoints)
                {
                    _NavAddTarget(wp.Coords, wp.Name, true, WicoControl.MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin, rc.SpeedLimit);
                    bAddedTargets = true;
                }
                if (!bAddedTargets)
                {
                    _program.Echo("No waypoints set in remote");
                    _program.ErrorLog("No waypoints set in remote");

                    if (bAutoPatrol) bAutoPatrol = false;
                    _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                }
                if (_Debug) _program.ErrorLog("StartPatrol:():" + wicoNavCommands.Count + " Commands");
                return bAddedTargets;

            }

            void SetDebug(bool debug)
            {
                _Debug = debug;
            }

        }

        public enum CommandTypes { unknown, Waypoint, Orientation, ArrivalDistance, MaxSpeed,SetMode, Launch, Land, Dock, OrbitalLaunch };

        public class WicoNavCommand
        {
            public Program pg;

            public CommandTypes Command = CommandTypes.unknown;
            public int SetMode;
            public Vector3D vNavTarget;
            public bool bValidNavTarget = false;

            public int NAVArrivalMode = WicoControl.MODE_ARRIVEDTARGET;
            public int NAVArrivalState = 0;

            /// <summary>
            /// Set maximum speed of ship. 
            /// Set this using S command for NAV
            /// </summary>
            public double shipSpeedMax = 9999;
            /// <summary>
            /// the minimum distance to be from the target to be considered 'arrived'
            /// </summary>
            public double arrivalDistanceMin = 50;
            public string NAVTargetName = "";

            public WicoControl _wicoControl;
            public Navigation _wicoNavigation;

            public bool ProcessCommand()
            {
                switch (Command)
                {
                    case CommandTypes.Waypoint:
                        {
                            _wicoNavigation.VNavTarget = vNavTarget;
                            _wicoNavigation.BValidNavTarget = true;
                            _wicoNavigation.NAVArrivalMode = NAVArrivalMode;
                            _wicoNavigation.NAVArrivalState = NAVArrivalState;
                            _wicoNavigation.ArrivalDistanceMin = arrivalDistanceMin;
                            _wicoNavigation.NAVTargetName = NAVTargetName;

                            _wicoNavigation.ShipSpeedMax = shipSpeedMax;

                            _wicoNavigation.BGoOption = true;

                            _wicoControl.SetMode(WicoControl.MODE_GOINGTARGET);
                            _wicoControl.WantOnce();
                        }
                        break;
                    case CommandTypes.Orientation:
                        {
                            _wicoNavigation.VNavTarget = vNavTarget;
                            _wicoNavigation.BValidNavTarget = true;
                            _wicoNavigation.NAVArrivalMode = NAVArrivalMode;
                            _wicoNavigation.NAVArrivalState = NAVArrivalState;
                            _wicoNavigation.NAVTargetName = NAVTargetName;

                            _wicoNavigation.BGoOption = false;
                            _wicoControl.SetMode(WicoControl.MODE_GOINGTARGET);
                        }
                        break;
                    case CommandTypes.MaxSpeed:
                        {
                            _wicoNavigation.ShipSpeedMax = shipSpeedMax;
                            _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                        }
                        break;
                    case CommandTypes.ArrivalDistance:
                        {
                            _wicoNavigation.ArrivalDistanceMin = arrivalDistanceMin;
                            _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                        }
                        break;
                    case CommandTypes.SetMode:
                        {
                            _wicoControl.SetMode(SetMode);
                        }
                        break;
                    case CommandTypes.Launch:
                        {
                            _wicoControl.SetMode(WicoControl.MODE_LAUNCH);
                        }
                        break;
                    case CommandTypes.OrbitalLaunch:
                        {
                            _wicoControl.SetMode(WicoControl.MODE_ORBITALLAUNCH);
                        }
                        break;
                    case CommandTypes.Dock:
                        {
                            _wicoControl.SetMode(WicoControl.MODE_DOCKING);
                        }
                        break;
                    case CommandTypes.Land:
                        {
                            _wicoControl.SetMode(WicoControl.MODE_DESCENT);
                        }
                        break;
                    case CommandTypes.unknown:
                        {
                            pg.Echo("Unknown Command");
                            _wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                            return true;
                        }
                        //                        break;
                }
                return false;
            }

        }
    }

}


