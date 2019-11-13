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
        class Navigation
        {
            Program thisProgram;
            IMyShipController shipController;

            Vector3D vAvoid;

            /// <summary>
            ///  GRID orientation to aim ship
            /// </summary>
            Vector3D vBestThrustOrientation;


            public Navigation(Program program, IMyShipController myShipController)
            {
                thisProgram = program;
                shipController = myShipController;

                thisProgram.moduleName += " Navigation";
                thisProgram.moduleList += "\nNavigation V4";

                thisProgram._CustomDataIni.Get(sNavSection, "NAVEmulateOld").ToBoolean(NAVEmulateOld);
                thisProgram._CustomDataIni.Set(sNavSection, "NAVEmulateOld", NAVEmulateOld);

                thisProgram.AddUpdateHandler(UpdateHandler);
                thisProgram.AddTriggerHandler(ProcessTrigger);

                thisProgram.AddLoadHandler(LoadHandler);
                thisProgram.AddSaveHandler(SaveHandler);

                thisProgram.wicoControl.AddModeInitHandler(ModeInitHandler);
                thisProgram.wicoControl.AddControlChangeHandler(ModeChangeHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            void LoadHandler(MyIni Ini)
            {
                Vector3D v3D;

                Vector3D.TryParse(Ini.Get(sNavSection, "vTarget").ToString(), out v3D);
                VNavTarget = v3D;

                Ini.Set(sNavSection, "vTarget", VNavTarget.ToString());

                BValidNavTarget=Ini.Get(sNavSection, "ValidNavTarget").ToBoolean();
                NAVTargetName= Ini.Get(sNavSection, "ValidNavTarget").ToString();

                dtNavStartShip=DateTime.FromBinary(Ini.Get(sNavSection, "dStartShip").ToInt64());
                ShipSpeedMax=Ini.Get(sNavSection, "dStartShip").ToDouble();
                ArrivalDistanceMin = Ini.Get(sNavSection, "dStartShip").ToDouble();
                NAVArrivalMode = Ini.Get(sNavSection, "dStartShip").ToInt32();
                NAVArrivalState = Ini.Get(sNavSection, "dStartShip").ToInt32();
            }

            void SaveHandler(MyIni Ini)
            {
                Ini.Set(sNavSection, "vTarget", VNavTarget.ToString());
                Ini.Set(sNavSection, "ValidNavTarget", BValidNavTarget);
                Ini.Set(sNavSection, "TargetName", NAVTargetName);

                Ini.Set(sNavSection, "dStartShip", dtNavStartShip.ToBinary());
                Ini.Set(sNavSection, "shipSpeedMax", ShipSpeedMax);
                Ini.Set(sNavSection, "arrivalDistanceMin", ArrivalDistanceMin);
                Ini.Set(sNavSection, "NAVArrivalMode", NAVArrivalMode);
                Ini.Set(sNavSection, "NAVArrivalState", NAVArrivalState);
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
                    thisProgram.wicoGyros.gyrosOff();
                    thisProgram.wicoThrusters.powerDownThrusters();
                }
                // need to check if this is us
                if (toMode == WicoControl.MODE_GOINGTARGET
                    || toMode == WicoControl.MODE_STARTNAV
                    || toMode == WicoControl.MODE_NAVNEXTTARGET
                    )
                {
                    thisProgram.wicoControl.WantOnce();
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                if (iMode == WicoControl.MODE_GOINGTARGET)
                {
                    // TODO: Check state and re-init as needed
                    thisProgram.wicoControl.WantFast();
                }
                if (iMode == WicoControl.MODE_STARTNAV)
                {
                    // TODO: Check state and re-init as needed
                    thisProgram.wicoControl.WantFast();
                }
                if (iMode == WicoControl.MODE_NAVNEXTTARGET)
                {
                    // TODO: Check state and re-init as needed
                    thisProgram.wicoControl.WantFast();
                }
            }
            void LocalGridChangedHandler()
            {
                shipController = null;
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

                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');

                    if (args[0] == "W" || args[0] == "O")
                    { // [W|O] <x>:<y>:<z>  || W <x>,<y>,<z>
                      // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                      // O means orient towards.  W means orient, then move to
                        bFoundNAVCommands = true;
                        thisProgram.Echo("Args:");
                        for (int icoord = 0; icoord < args.Length; icoord++)
                            thisProgram.Echo(args[icoord]);
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
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
                            thisProgram.Echo(coordinates[icoord]);
                        //Echo("coordiantes.Length="+coordinates.Length);  
                        if (coordinates.Length < 3)
                        {
                            //Echo("P:B");  

                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            thisProgram.wicoGyros.gyrosOff();// shutdown(gyroList);
                            return;
                        }
                        int iCoordinate = 0;
                        string sWaypointName = "Waypoint";
                        //  -  0   1           2        3          4       5
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
                                thisProgram.Echo("Invalid Command");
                                thisProgram.wicoGyros.gyrosOff();
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
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            //			shutdown(gyroList);
                            continue;
                        }

                        //                    thisProgram.sMasterReporting = "CMD Initiated NAV:\n" + sArgument;

                        //                    _vNavTarget = new Vector3D(x, y, z);
                        //                    BValidNavTarget = true;
                        if (args[0] == "W")
                        {
                            _NavAddTarget(new Vector3D(x, y, z), sWaypointName, true, WicoControl.MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin,  ShipSpeedMax);
                            //                        bGoOption = true;
                        }
                        else
                        {
                            _NavAddTarget(new Vector3D(x, y, z), sWaypointName, false, WicoControl.MODE_NAVNEXTTARGET, 0, ArrivalDistanceMin, ShipSpeedMax);
                            //                        bGoOption = false;
                        }
                        //                    thisProgram.sMasterReporting += "\nW " + sWaypointName + ":" + wicoNavCommands.Count.ToString();
                        //                   setMode(MODE_GOINGTARGET);

                    }
                    else if (args[0] == "S")
                    { // S <mps>
                      // TODO: Queue the command into NavCommands
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        double x;
                        bool xOk = double.TryParse(args[1].Trim(), out x);
                        if (xOk)
                        {
                            ShipSpeedMax = x;
                            //                        Echo("Set speed to:" + _shipSpeedMax.ToString("0.00"));
                            //             setMode(MODE_ARRIVEDTARGET);
                        }
                        else
                        {
                            //Echo("P:C");  
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                    }
                    else if (args[0] == "D")
                    { // D <meters>
                      // TODO: Queue the command into NavCommands
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
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
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                    }
                    else if (args[0] == "C")
                    { // C <anything>
                        if (args.Length < 1)
                        {
                            thisProgram.Echo("Invalid Command:(" + varArgs[iArg] + ")");
                            continue;
                        }
                        else
                        {
                            thisProgram.Echo(varArgs[iArg]);
                        }
                    }
                    else if (args[0] == "L")
                    { // L launch
                        bFoundNAVCommands = true;
                        _NavQueueLaunch();
                    }
                    else if (args[0] == "launch")
                    { // L launch
                        bFoundNAVCommands = true;
                        _NavQueueLaunch();
                    }
                    else if (args[0] == "OL")
                    { // OL Orbital launch
                        bFoundNAVCommands = true;
                        _NavQueueOrbitalLaunch();
                    }
                    else if (args[0] == "orbitallaunch")
                    { // OL Orbital launch
                        bFoundNAVCommands = true;
                        _NavQueueOrbitalLaunch();
                    }
                    else if (args[0] == "dock")
                    { // dock
                        bFoundNAVCommands = true;
                        _NavQueueOrbitalLaunch();
                    }
                }
                if (bFoundNAVCommands)
                {
                     thisProgram.sMasterReporting += "\nFound NAV Commands:" + wicoNavCommands.Count.ToString();
                    _NavStart();
                }
                if (myCommandLine != null)
                {
                    for (int arg = 0; arg < myCommandLine.ArgumentCount; arg++)
                    {
                        string sArg = myCommandLine.Argument(arg);
                        if (sArg == "test")
                        {
                            NAVTargetName = "Test Target";
                            //GPS:Wicorel #1:46.41:-153.94:-101.56:
                            VNavTarget = new Vector3D(46.41, -153.94, -101.56); //-57:43.81:-110.51:
                            BValidNavTarget = true;
                            thisProgram.wicoControl.SetMode(WicoControl.MODE_GOINGTARGET);
                        }
                        if (sArg == "aim")
                        {
                            NAVTargetName = "Test Target";
                            //GPS:Wicorel #1:46.41:-153.94:-101.56:
                            VNavTarget = new Vector3D(46.41, -153.94, -101.56); //-57:43.81:-110.51:
                            BValidNavTarget = true;
                            BGoOption = false;
                            thisProgram.wicoControl.SetMode(WicoControl.MODE_GOINGTARGET);
                        }
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                // need to check if this is us
                if (iMode == WicoControl.MODE_GOINGTARGET)
                {
                    doModeGoTarget();
                }
                if (iMode == WicoControl.MODE_STARTNAV) { doModeStartNav(); return; }
                if (iMode == WicoControl.MODE_NAVNEXTTARGET) { doModeNavNext(); return; }
            }

            /// <summary>
            /// We are a sled. Default false
            /// </summary>
            bool bSled = false;

            /// <summary>
            /// We are rotor-control propulsion. Default false
            /// </summary>
            bool bRotor = false;

            bool bWheels = false;

            // propulsion mode
            bool btmRotor = false;
            bool btmSled = false;
            bool btmWheels = false;
            bool btmHasGyros = false;
            // else it's gyros and thrusters
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

            160 Main Travel to target



            *** below here are thruster-only routines (for now)

            300 Collision Detected From 160
                Calculate collision avoidance 
                then ->320

            301 dummy state for debugging.
            320 do travel movement for collision avoidance. 
            if arrive target, ->160
            if secondary collision ->340

            340 secondary collision
            if a type we can move around, try to move ->350
            else go back to collision detection ->300

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

            public int NAVArrivalMode { get; set; } = WicoControl.MODE_ARRIVEDTARGET;
            public int NAVArrivalState { get; set; } = 0;

            public string NAVTargetName { get; set; } = "";

            //        Vector3D vNavLaunch;
            //        bool bValidNavLaunch = false;
            //        Vector3D vNavHome;
            //        bool bValidNavHome = false;
            bool NAVEmulateOld = false;
// TODO: Move to TravelMovement            bool AllowBlindNav = false;
            //            float NAVGravityMinElevation = -1;

            bool bNavBeaconDebug = false;


            string sNavSection = "NAV";

            void doModeGoTarget()
            {
                int iMode = thisProgram.wicoControl.IMode;
                int iState = thisProgram.wicoControl.IState;

                //                StatusLog("clear", textPanelReport);

                //                StatusLog(moduleName + ":Going Target!", textPanelReport);
                //            StatusLog(moduleName + ":GT: iState=" + iState.ToString(), textPanelReport);
                //            bWantFast = true;
                thisProgram.Echo("Going Target: state=" + iState.ToString());
                if (NAVTargetName != "") thisProgram.Echo(NAVTargetName);

                string sNavDebug = "";
                sNavDebug += "GT:S=" + iState;
                //            sNavDebug += " MinE=" + NAVGravityMinElevation;
                //            ResetMotion();
                IMyShipController shipController = thisProgram.wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dGravity = vNG.Length();

                if(thrustForwardList.Count<1)
                {
                    thisProgram.wicoThrusters.ThrustersCalculateOrientation(shipController,
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

                    thisProgram.wicoTravelMovement.ResetTravelMovement();
                    //                thisProgram.sMasterReporting+="\nStart movemenet: ArrivalMode="+NAVArrivalMode+" State="+NAVArrivalState;
                    //                    if ((craft_operation & CRAFT_MODE_SLED) > 0)
                    if (thisProgram.wicoWheels.HasSledWheels())
                    {
                        bSled = true;
                        if (ShipSpeedMax > 45) ShipSpeedMax = 45;
                    }
                    else bSled = false;

                    //                    if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
                    if (thisProgram.wicoNavRotors.NavRotorCount() > 0)
                    {
                        bRotor = true;
                        if (ShipSpeedMax > 15) ShipSpeedMax = 15;
                    }
                    else bRotor = false;
                    //                    if ((craft_operation & CRAFT_MODE_WHEEL) > 0)
                    if (thisProgram.wicoWheels.HasWheels())
                    {
                        bWheels = true;
                        //                   if (_shipSpeedMax > 15) _shipSpeedMax = 15;
                    }
                    else bWheels = false;

                    //                    GyroControl.SetRefBlock(shipOrientationBlock);

                    // TODO: Put a timer on this so it's not done Update1
                    double elevation = 0;
                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);

                    if (!bSled && !bRotor)
                    { // if flying ship
                      // make sure set to default
                        if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation < 0)
                            thisProgram.wicoBlockMaster.DesiredMinTravelElevation = 75; // for EFM getting to target 'arrived' radius
                    }

                    if (BValidNavTarget)
                    {
                        if (elevation > thisProgram.wicoBlockMaster.HeightInMeters())
                        {
                            thisProgram.wicoControl.SetState(150);
                        }
                        else thisProgram.wicoControl.SetState(160);// iState = 160;
                    }
                    else thisProgram.wicoControl.SetMode(WicoControl.MODE_ATTENTION);//else setMode(MODE_ATTENTION);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                else if (iState == 150)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (dGravity > 0)
                    {

                        double elevation = 0;

                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");

                        float fSaveAngle = thisProgram.wicoGyros.GetMinAngle();// minAngleRad;
                        thisProgram.wicoGyros.SetMinAngle(0.1f);// minAngleRad = 0.1f;

                        //                        bool bAligned = GyroMain("", vNG, shipController);
                        bool bAligned = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        thisProgram.Echo("bAligned=" + bAligned.ToString());
                        thisProgram.wicoGyros.SetMinAngle(fSaveAngle); //minAngleRad = fSaveAngle;
                        if (bAligned || elevation < thisProgram.wicoBlockMaster.HeightInMeters() * 2)
                        {
                            thisProgram.wicoGyros.gyrosOff();
                            if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0)
                                thisProgram.wicoControl.SetState(155);
                            else thisProgram.wicoControl.SetState(160); // iState = 160;
                        }
                    }
                    else thisProgram.wicoControl.SetState(160); // iState = 160;

                }
                else if (iState == 151)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (dGravity > 0 || btmWheels)
                    {

                        double elevation = 0;

                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");

                        float fSaveAngle = thisProgram.wicoGyros.GetMinAngle();// minAngleRad;
                        thisProgram.wicoGyros.SetMinAngle(0.1f);// minAngleRad = 0.1f;

                        //                        bool bAligned = GyroMain("", vNG, shipController);
                        bool bAligned = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        thisProgram.Echo("bAligned=" + bAligned.ToString());
                        thisProgram.wicoGyros.SetMinAngle(fSaveAngle); //minAngleRad = fSaveAngle;
                        if (bAligned || elevation < thisProgram.wicoBlockMaster.HeightInMeters() * 2)
                        {
                            thisProgram.wicoGyros.gyrosOff();
                            if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0)
                                thisProgram.wicoControl.SetState(155);
                            else thisProgram.wicoControl.SetState(160);// iState = 160;
                        }
                        else thisProgram.wicoControl.SetState(150); //iState = 150;// try again to be aligned.
                    }
                    else thisProgram.wicoControl.SetState(160); //iState = 160;

                }
                else if (iState == 155)
                { // for use in gravity: aim at location using yaw only
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    if (bWheels)
                    {
                        thisProgram.wicoControl.SetState(160);
//                        iState = 160;
                        return;
                    }

                    if (dGravity > 0)
                    {
                        //                        bool bAligned = GyroMain("", vNG, shipController);
                        bool bAligned = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);
                        sNavDebug += " Aligned=" + bAligned.ToString();

                        double yawangle = -999;
                        yawangle = thisProgram.CalculateYaw(VNavTarget, shipController);
                        bool bAimed = Math.Abs(yawangle) < 0.1; // NOTE: 2x allowance
                        thisProgram.Echo("yawangle=" + yawangle.ToString());
                        sNavDebug += " Yaw=" + yawangle.ToString("0.00");

                        if (!bAimed)
                        {
                            if (btmRotor)
                            {
                                thisProgram.Echo("Rotor");
                                thisProgram.wicoNavRotors.DoRotorRotate(yawangle);
                            }
                            else // use for both sled and flight
                            {
                                thisProgram.wicoGyros.DoRotate(yawangle, "Yaw");
                            }
                        }
                        if (bAligned && bAimed)
                        {
                            thisProgram.wicoGyros.gyrosOff();
                            thisProgram.wicoControl.SetState(160);// iState = 160;
                        }
                        else if (bAligned && Math.Abs(yawangle) < 0.5)
                        {
                            float atmo;
                            float hydro;
                            float ion;

                            thisProgram.wicoThrusters.CalculateHoverThrust(shipController, thrustForwardList, out atmo, out hydro, out ion);
                            atmo += 1;
                            hydro += 1;
                            ion += 1;

                            thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, atmo, WicoThrusters.thrustatmo);
                            thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, hydro, WicoThrusters.thrusthydro);
                            thisProgram.wicoThrusters.powerUpThrusters(thrustForwardList, ion, WicoThrusters.thrustion);

                        }
                        else
                            thisProgram.wicoThrusters.powerDownThrusters(thrustForwardList);
                    }
                    else thisProgram.wicoControl.SetState(160); //iState = 160;
                }
                else if (iState == 156)
                {
                    // realign gravity
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                                                       //                    bool bAimed = GyroMain("", grav, shipController);
                    bool bAimed = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);
                    if (bAimed)
                    {
                        thisProgram.wicoGyros.gyrosOff();
                        thisProgram.wicoControl.SetState(160); //iState = 160;
                    }
                }
                else if (iState == 160)
                { //	160 move to Target
                    thisProgram.Echo("Moving to Target");
                    thisProgram.Echo("Target="+VNavTarget.ToString());
                    Vector3D vTargetLocation = VNavTarget;
                    double velocityShip = shipController.GetShipSpeed();

                    Vector3D vVec = vTargetLocation - shipController.GetPosition();
                    double distance = vVec.Length();
                    thisProgram.Echo("distance=" + thisProgram.niceDoubleMeters(distance));
                    thisProgram.Echo("velocity=" + velocityShip.ToString("0.00"));

                    //                    StatusLog("clear", sledReport);
                    string sTarget = "Moving to Target";
                    if (NAVTargetName != "") sTarget = "Moving to " + NAVTargetName;
                    //                    StatusLog(sTarget + "\nD:" + niceDoubleMeters(distance) + " V:" + velocityShip.ToString(velocityFormat), sledReport);
                    //                    StatusLog(sTarget + "\nDistance: " + niceDoubleMeters(distance) + "\nVelocity: " + niceDoubleMeters(velocityShip) + "/s", textPanelReport);


                    if (
                        //!bGoOption || 
                        BGoOption && 
                        (distance < ArrivalDistanceMin))
                    {
                        thisProgram.wicoControl.SetState(500);// iState = 500;

                        thisProgram.Echo("we have arrived");
                        thisProgram.wicoControl.WantFast();// bWantFast = true;
                        return;
                    }

                    //                debugGPSOutput("TargetLocation", vTargetLocation);
                    bool bDoTravel = false;
                    if (BGoOption)
                        bDoTravel = true;

                    if (thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0 && dGravity > 0)
                    {
                        double elevation = 0;

                        MyShipVelocities mysSV = shipController.GetShipVelocities();
                        Vector3D lv = mysSV.LinearVelocity;

                        // ASSUMES: -up = gravity down  Assuming ship orientation
                        var upVec = shipController.WorldMatrix.Up;
                        var vertVel = Vector3D.Dot(lv, upVec);

                        //                    thisProgram.Echo("LV=" + Vector3DToString(lv));
                        //                    sNavDebug += " LV=" + Vector3DToString(lv);
                        //                    sNavDebug += " vertVel=" + vertVel.ToString("0.0");
                        //                    sNavDebug += " Hvel=" + lv.Y.ToString("0.0");

                        // NOTE: Elevation is only updated by game every 30? ticks. so it can be WAY out of date based on movement
                        shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                        sNavDebug += " E=" + elevation.ToString("0.0");
                        sNavDebug += " V=" + velocityShip.ToString("0.00");

                        thisProgram.Echo("Elevation=" + elevation.ToString("0.0"));
                        thisProgram.Echo("MinEle=" + thisProgram.wicoBlockMaster.DesiredMinTravelElevation.ToString("0.0"));

                        //                    double stopD = calculateStoppingDistance(thrustUpList, velocityShip, dGravity);
                        double stopD = 0;
                        if (vertVel < 0)
                        {
                            stopD = thisProgram.wicoThrusters.calculateStoppingDistance(shipController, thrustUpList, Math.Abs(vertVel), dGravity);
                        }
                        double maxStopD = thisProgram.wicoThrusters.calculateStoppingDistance(shipController, thrustUpList, thisProgram.wicoControl.fMaxWorldMps, dGravity);

                        float atmo;
                        float hydro;
                        float ion;
                        thisProgram.wicoThrusters.CalculateHoverThrust(shipController, thrustUpList, out atmo, out hydro, out ion);

                        //                    sNavDebug += " SD=" + stopD.ToString("0");

                        if (
                            //                        !bSled && !bRotor && 
                            thisProgram.wicoBlockMaster.DesiredMinTravelElevation > 0)
                        {
                            if (
                                vertVel < -0.5  // we are going downwards
                                && (elevation - stopD * 2) < thisProgram.wicoBlockMaster.DesiredMinTravelElevation)
                            { // too low. go higher
                              // Emergency thrust
                                sNavDebug += " EM UP!";

                                //                                bool bAligned = GyroMain("", grav, shipController);
                                bool bAligned = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);

                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, 100);
                                bDoTravel = false;
                                thisProgram.wicoControl.WantFast();// bWantFast = true;
                            }
                            else if (elevation < thisProgram.wicoBlockMaster.DesiredMinTravelElevation)
                            {
                                // push upwards
                                atmo += Math.Min(5f, (float)ShipSpeedMax);
                                hydro += Math.Min(5f, (float)ShipSpeedMax);
                                ion += Math.Min(5f, (float)ShipSpeedMax);
                                sNavDebug += " UP! A" + atmo.ToString("0.00");// + " H"+hydro.ToString("0.00") + " I"+ion.ToString("0.00");
                                                                              //powerUpThrusters(thrustUpList, 100);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, atmo, WicoThrusters.thrustatmo);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, hydro, WicoThrusters.thrusthydro);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, ion, WicoThrusters.thrustion);

                            }
                            else if (elevation > (maxStopD + thisProgram.wicoBlockMaster.DesiredMinTravelElevation * 1.25))
                            {
                                // if we are higher than maximum possible stopping distance, go down fast.
                                sNavDebug += " SUPERHIGH";

                                //                           Vector3D grav = (shipOrientationBlock as IMyShipController).GetNaturalGravity();
                                //                            bool bAligned = GyroMain("", grav, shipOrientationBlock);

                                thisProgram.wicoThrusters.powerDownThrusters(thrustUpList, WicoThrusters.thrustAll, true);
                                //                                bool bAligned = GyroMain("", grav, shipController);
                                bool bAligned = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);
                                if (!bAligned)
                                {
                                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                                    bDoTravel = false;
                                }
                                //                            powerUpThrusters(thrustUpList, 1f);
                            }
                            else if (
                                elevation > thisProgram.wicoBlockMaster.DesiredMinTravelElevation * 2  // too high
                                                                                                       //                            && ((elevation-stopD)>NAVGravityMinElevation) // we can stop in time.
                                                                                                       //                        && velocityShip < _shipSpeedMax * 1.1 // to fast in any direction
                                                                                                       //                           && Math.Abs(lv.X) < Math.Min(25, _shipSpeedMax) // not too fast 
                                                                                                       //                            && Math.Abs(lv.Y) < Math.Min(25, _shipSpeedMax) // not too fast downwards (or upwards)
                                )
                            { // too high 
                                sNavDebug += " HIGH";
                                //DOWN! A" + atmo.ToString("0.00");// + " H" + hydro.ToString("0.00") + " I" + ion.ToString("0.00");

                                if (vertVel > 2) // going up
                                { // turn off thrusters.
                                    sNavDebug += " ^";
                                    thisProgram.wicoThrusters.powerDownThrusters(thrustUpList, WicoThrusters.thrustAll, true);
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

//                                        bool bAligned = GyroMain("", grav, shipController);
                                        bool bAligned = thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG);
                                        if (!bAligned)
                                        {
                                            thisProgram.wicoControl.WantFast();// bWantFast = true;
                                            bDoTravel = false;
                                        }
                                        //                                    bDoTravel = false;
                                    }

                                }
                                else
                                {
                                    sNavDebug += " -";
                                    atmo -= 5;
                                    hydro -= 5;
                                    ion -= 5;
                                }

                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, atmo, WicoThrusters.thrustatmo);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, hydro, WicoThrusters.thrusthydro);
                                thisProgram.wicoThrusters.powerUpThrusters(thrustUpList, ion, WicoThrusters.thrustion);

                            }
                            else
                            {
                                // normal hover
                                thisProgram.wicoThrusters.powerDownThrusters(thrustUpList);
                            }
                        }
                    }
                    if (bDoTravel)
                    {
                        thisProgram.Echo("Do Travel");
                        thisProgram.wicoTravelMovement.doTravelMovement(vTargetLocation, (float)ArrivalDistanceMin, 500, 300);
                    }
                    else
                    {
                        thisProgram.wicoThrusters.powerDownThrusters(thrustForwardList);
                        thisProgram.Echo("Aim");
                        Matrix or1;
                        shipController.Orientation.GetMatrix(out or1);
                        Vector3D vDirection = or1.Forward;
                        thisProgram.wicoControl.WantFast();
                        if (thisProgram.wicoGyros.AlignGyros(vDirection, vVec))
                        {
                            thisProgram.wicoControl.SetState(500);// iState = 500;
                            thisProgram.Echo("We are now Aimed");
                        }
                    }
                }

                else if (iState == 300)
                { // collision detection

                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    Vector3D vTargetLocation = VNavTarget;
                    thisProgram.wicoTravelMovement.ResetTravelMovement();
                    thisProgram.wicoTravelMovement.calcCollisionAvoid(vTargetLocation, thisProgram.wicoTravelMovement.LastDetectedInfo, out vAvoid);

                    //                iState = 301; // testing
                    thisProgram.wicoControl.SetState(320); //iState = 320;
                }
                else if (iState == 301)
                {
                    // just hold this state
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }

                else if (iState == 320)
                {
                    //                 Vector3D vVec = vAvoid - shipOrientationBlock.GetPosition();
                    //                double distanceSQ = vVec.LengthSquared();
                    thisProgram.Echo("Primary Collision Avoid");
                    //                    StatusLog("clear", sledReport);
                    //                    StatusLog("Collision Avoid", sledReport);
                    //                    StatusLog("Collision Avoid", textPanelReport);
                    thisProgram.wicoTravelMovement.doTravelMovement(vAvoid, 5.0f, 160, 340);
                }
                else if (iState == 340)
                {       // secondary collision
                    if (
                        thisProgram.wicoTravelMovement.LastDetectedInfo.Type == MyDetectedEntityType.LargeGrid
                        || thisProgram.wicoTravelMovement.LastDetectedInfo.Type == MyDetectedEntityType.SmallGrid
                        )
                    {
                        thisProgram.wicoControl.SetState(345); //iState = 345;
                    }
                    else if (thisProgram.wicoTravelMovement.LastDetectedInfo.Type == MyDetectedEntityType.Asteroid
                        )
                    {
                        thisProgram.wicoControl.SetState(350);// iState = 350;
                    }
                    else thisProgram.wicoControl.SetState(300); //iState = 300;// setMode(MODE_ATTENTION);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                else if (iState == 345)
                {
                    // we hit a grid.  align to it
                    Vector3D[] corners = new Vector3D[BoundingBoxD.CornerCount];

                    BoundingBoxD bbd = thisProgram.wicoTravelMovement.LastDetectedInfo.BoundingBox;
                    bbd.GetCorners(corners);

                    GridUpVector = thisProgram.wicoSensors.PlanarNormal(corners[3], corners[4], corners[7]);
                    GridRightVector = thisProgram.wicoSensors.PlanarNormal(corners[0], corners[1], corners[4]);
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    thisProgram.wicoControl.SetState(348); //iState = 348;
                }
                else if (iState == 348)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    Matrix or1;
                    Vector3 vOrientation;
                    thrustUpList[0].Orientation.GetMatrix(out or1);
                    vOrientation = or1.Forward;
                    if(thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG))
                    //                    if (GyroMain("up", GridUpVector, shipController))
                    {
                        thisProgram.wicoControl.SetState(349); //iState = 349;
                    }
                }
                else if (iState == 349)
                {
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                    Matrix or1;
                    Vector3 vOrientation;
                    thrustRightList[0].Orientation.GetMatrix(out or1);
                    vOrientation = or1.Forward;
                    if (thisProgram.wicoGyros.AlignGyros(vBestThrustOrientation, vNG))
                    //                    if (GyroMain("right", GridRightVector, shipController))
                    {
                        thisProgram.wicoControl.SetState(350); //iState = 350;
                    }
                }
                else if (iState == 350)
                {
                    //                initEscapeScan(bCollisionWasSensor, !bCollisionWasSensor);
                    thisProgram.wicoTravelMovement.initEscapeScan(thisProgram.wicoTravelMovement.bCollisionWasSensor);
                    thisProgram.wicoTravelMovement.ResetTravelMovement();
                    dtNavStartShip = DateTime.Now;
                    thisProgram.wicoControl.SetState(360);// iState = 360;
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                }
                else if (iState == 360)
                {
                    //                    StatusLog("Collision Avoid\nScan for escape route", textPanelReport);
                    DateTime dtMaxWait = dtNavStartShip.AddSeconds(5.0f);
                    DateTime dtNow = DateTime.Now;
                    if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                    {
                        thisProgram.wicoControl.SetMode(WicoControl.MODE_ATTENTION);// setMode(MODE_ATTENTION);
                        //                        doTriggerMain();
                        return;
                    }
                    if (thisProgram.wicoTravelMovement.scanEscape())
                    {
                        thisProgram.Echo("ESCAPE!");
                        thisProgram.wicoControl.SetState(380);// iState = 380;
                    }
                    thisProgram.wicoControl.WantMedium(); // bWantMedium = true;
                    //                bWantFast = true;
                }
                else if (iState == 380)
                {
                    //                    StatusLog("Collision Avoid Travel", textPanelReport);
                    thisProgram.Echo("Escape Collision Avoid");
                    thisProgram.wicoTravelMovement.doTravelMovement(vAvoid, 1f, 160, 340);
                }
                else if (iState == 500)
                { // we have arrived at target

                    /*
                    // check for more nav commands
                    if(wicoNavCommands.Count>0)
                    {
                        wicoNavCommands.RemoveAt(0);
                    }
                    if(wicoNavCommands.Count>0)
                    {
                        // another command
                        wicoNavCommandProcessNext();
                    }
                    else
                    */
                    {

                        //                        StatusLog("clear", sledReport);
                        //                        StatusLog("Arrived at Target", sledReport);
                        //                        StatusLog("Arrived at Target", textPanelReport);
                        sNavDebug += " ARRIVED!";

                        thisProgram.ResetMotion();
                        BValidNavTarget = false; // we used this one up.
                                                 //                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                        thisProgram.wicoAntennas.SetMaxPower(false);
                        thisProgram.wicoSensors.SensorsSleepAll();
                        //                    thisProgram.sMasterReporting += "Finish WP:" + wicoNavCommands.Count.ToString()+":"+NAVArrivalMode.ToString();
                        // set to desired mode and state
                        thisProgram.wicoControl.SetMode(NAVArrivalMode);// setMode(NAVArrivalMode);
                        thisProgram.wicoControl.SetState(NAVArrivalState); //iState = NAVArrivalState;

                        // set up defaults for next run (in case they had been changed)
                        NAVArrivalMode = WicoControl.MODE_ARRIVEDTARGET;
                        NAVArrivalState = 0;
                        NAVTargetName = "";
                        BGoOption = true;

                        //                setMode(MODE_ARRIVEDTARGET);
                        if (NAVEmulateOld)
                        {
                            var tList = thisProgram.wicoBlockMaster.GetBlocksContains<IMyTerminalBlock>("NAV:");
                            for (int i1 = 0; i1 < tList.Count(); i1++)
                            {
                                // don't want to get blocks that have "NAV:" in customdata..
                                if (tList[i1].CustomName.StartsWith("NAV:"))
                                {
                                    thisProgram.Echo("Found NAV: command:");
                                    tList[i1].CustomName = "NAV: C Arrived Target";
                                }
                            }
                        }
                    }
                    thisProgram.wicoControl.WantFast();// bWantFast = true;
                                                       //                    doTriggerMain();
                }
                //                NavDebug(sNavDebug);
            }

            public void SetNavigation(Vector3D vNavTarget, string NavTargetName, bool bGo = true, int NavArrivalMode= WicoControl.MODE_NAVNEXTTARGET, int NavArrivalState=0, double _ArrivalDistanceMin=50, double _ShipSpeexMax=9999 )
            {
                VNavTarget = vNavTarget;
                BValidNavTarget = true;
                NAVTargetName = NavTargetName;
                BGoOption = bGo;
                NAVArrivalMode = NavArrivalMode;
                NAVArrivalState = NavArrivalState;
                ArrivalDistanceMin = _ArrivalDistanceMin;
                ShipSpeedMax = _ShipSpeexMax;
            }
            List<WicoNavCommand> wicoNavCommands = new List<WicoNavCommand>();

            void _NavAddTarget(Vector3D vTarget, string TargetName = "", bool bGo = true, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, double maxSpeed = 9999)
            {

                if (maxSpeed > thisProgram.wicoControl.fMaxWorldMps)
                    maxSpeed = thisProgram.wicoControl.fMaxWorldMps;
                WicoNavCommand wicoNavCommand = new WicoNavCommand
                {
                    pg = thisProgram,
                    vNavTarget = vTarget,
                    bValidNavTarget = true,
                    NAVArrivalMode = modeArrival,
                    NAVArrivalState = stateArrival,
                    arrivalDistanceMin = DistanceMin,
                    shipSpeedMax = maxSpeed,
                    NAVTargetName = TargetName
                };
                if (bGo) wicoNavCommand.Command = CommandTypes.Waypoint;
                else wicoNavCommand.Command = CommandTypes.Orientation;

                //            thisProgram.sMasterReporting += "Adding NAV Commnd:";
                //            thisProgram.sMasterReporting += " Name=:" + wicoNavCommand._NAVTargetName;
                //            thisProgram.sMasterReporting += " Loc=" + Vector3DToString(wicoNavCommand._vNavTarget);
                wicoNavCommands.Add(wicoNavCommand);
            }
            void _NavQueueLaunch()
            {
                WicoNavCommand wicoNavCommand = new WicoNavCommand
                {
                    pg = thisProgram,
                    Command = CommandTypes.Launch
                };

                wicoNavCommands.Add(wicoNavCommand);
            }
            void _NavQueueOrbitalLaunch()
            {
                WicoNavCommand wicoNavCommand = new WicoNavCommand
                {
                    pg = thisProgram,
                    Command = CommandTypes.OrbitalLaunch
                };

                wicoNavCommands.Add(wicoNavCommand);
            }
            void _NavGoTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_ARRIVEDTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
                _NavAddTarget(vTarget, TargetName, bGo, modeArrival, stateArrival, DistanceMin,  maxSpeed);
                _NavStart();
            }
            void _NavQueueDock()
            {
                WicoNavCommand wicoNavCommand = new WicoNavCommand
                {
                    pg = thisProgram,
                    Command = CommandTypes.Dock
                };

                wicoNavCommands.Add(wicoNavCommand);

            }

            void _NavStart()
            {
                if (wicoNavCommands.Count > 0)
                {
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_STARTNAV);
                }
                else
                {
                    thisProgram.wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                    thisProgram.Echo("No Nav to start");
                    thisProgram.sMasterReporting+="\nError:No Nav to start";
                }
            }
            void NavCommandProcessNext()
            {
                //            sStartupError+="\nCommand Process:"+wicoNavCommands.Count.ToString();

                if (wicoNavCommands.Count < 1)
                {
                    thisProgram.wicoControl.SetMode(NAVArrivalMode);
                    thisProgram.wicoControl.SetState(NAVArrivalState);
                    // reset to defaults.
                    NAVArrivalMode = WicoControl.MODE_ARRIVEDTARGET;
                    NAVArrivalState = 0;
                    return;
                }
                wicoNavCommands[0].ProcessCommand();
                wicoNavCommands.RemoveAt(0);
                // should serialize these so they can resume on reload
            }
            IMyBroadcastListener _AddNavListener;
            IMyBroadcastListener _StartNavListener;
            IMyBroadcastListener _ResetNavListener;
            IMyBroadcastListener _LaunchNavListener;
            IMyBroadcastListener _OrbitalNavListener;

            // need to be shared...
            const string WICOB_NAVADDTARGET = "WICOB_NAVADDTARGET";
            const string WICOB_NAVSTART = "WICOB_NAVSTART";
            const string WICOB_NAVRESET = "WICOB_NAVRESET";
            const string WICOB_NAVLAUNCH = "WICO_NAVLAUNCH";
            const string WICOB_NAVDOCK = "WICO_NAVDOCK";
            const string WICOB_NAVORBITALLAUNCH = "WICO_NAVORBITALLAUNCH";
            const string WICOB_NAVLAND = "WICO_NAVLAND";

            void NavInitIGC()
            {
                _AddNavListener = thisProgram.IGC.RegisterBroadcastListener(WICOB_NAVADDTARGET); // What it listens for
                _AddNavListener.SetMessageCallback(WICOB_NAVADDTARGET); // What it will run the PB with once it has a message

                _StartNavListener = thisProgram.IGC.RegisterBroadcastListener(WICOB_NAVSTART); // What it listens for
                _StartNavListener.SetMessageCallback(WICOB_NAVSTART); // What it will run the PB with once it has a message

                _ResetNavListener = thisProgram.IGC.RegisterBroadcastListener(WICOB_NAVRESET); // What it listens for
                _ResetNavListener.SetMessageCallback(WICOB_NAVRESET); // What it will run the PB with once it has a message

                _LaunchNavListener = thisProgram.IGC.RegisterBroadcastListener(WICOB_NAVLAUNCH); // What it listens for
                _LaunchNavListener.SetMessageCallback(WICOB_NAVLAUNCH); // What it will run the PB with once it has a message

                _OrbitalNavListener = thisProgram.IGC.RegisterBroadcastListener(WICOB_NAVORBITALLAUNCH); // What it listens for
                _OrbitalNavListener.SetMessageCallback(WICOB_NAVORBITALLAUNCH); // What it will run the PB with once it has a message

            }

            void doModeStartNav()
            {
                thisProgram.Echo("Start Nav: state=" + thisProgram.wicoControl.IState.ToString());
                //            sStartupError += "Start Nav.";
                NavCommandProcessNext();
                thisProgram.wicoControl.WantFast();
            }
            void doModeNavNext()
            {
                //            sStartupError += "\nNAV NEXT" + wicoNavCommands.Count.ToString();
                thisProgram.Echo("Next Nav: state=" + thisProgram.wicoControl.IState.ToString());
                NavCommandProcessNext();
                thisProgram.wicoControl.WantFast();
                //            sStartupError += "\nENAV NEXT" + wicoNavCommands.Count.ToString() + " iMode="+iMode.ToString()+ ":"+NAVTargetName;
            }

        }

        public enum CommandTypes { unknown, Waypoint, Orientation, ArrivalDistance, MaxSpeed, Launch, Land, Dock, OrbitalLaunch };

        public class WicoNavCommand
        {
            public Program pg;

            public CommandTypes Command = CommandTypes.unknown;
            public Vector3D vNavTarget;
            public bool bValidNavTarget = false;
            //            public DateTime dtNavStartShip;

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


            public bool ProcessCommand()
            {
                //                pg.thisProgram.sMasterReporting += "\nProcessCommand";
                switch (Command)
                {
                    case CommandTypes.Waypoint:
                        {
                            pg.wicoNavigation.VNavTarget = vNavTarget;
                            pg.wicoNavigation.BValidNavTarget = true;
                            pg.wicoNavigation.NAVArrivalMode = NAVArrivalMode;
                            pg.wicoNavigation.NAVArrivalState = NAVArrivalState;
                            pg.wicoNavigation.ArrivalDistanceMin = arrivalDistanceMin;
                            pg.wicoNavigation.NAVTargetName = NAVTargetName;

                            pg.wicoNavigation.ShipSpeedMax = shipSpeedMax;

                            pg.wicoNavigation.BGoOption = true;

                            //                            pg.thisProgram.sMasterReporting += "Going to:" + pg._NAVTargetName;
                            pg.wicoControl.SetMode(WicoControl.MODE_GOINGTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Orientation:
                        {
                            pg.wicoNavigation.VNavTarget = vNavTarget;
                            pg.wicoNavigation.BValidNavTarget = true;
                            pg.wicoNavigation.NAVArrivalMode = NAVArrivalMode;
                            pg.wicoNavigation.NAVArrivalState = NAVArrivalState;
                            //                            pg.ArrivalDistanceMin = ArrivalDistanceMin;
                            pg.wicoNavigation.NAVTargetName = NAVTargetName;

                            //                            pg._shipSpeedMax = _shipSpeedMax;

                            pg.wicoNavigation.BGoOption = false;
                            pg.wicoControl.SetMode(WicoControl.MODE_GOINGTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.MaxSpeed:
                        {
                            pg.wicoNavigation.ShipSpeedMax = shipSpeedMax;
                            pg.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.ArrivalDistance:
                        {
                            pg.wicoNavigation.ArrivalDistanceMin = arrivalDistanceMin;
                            pg.wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Launch:
                        {
                            pg.wicoControl.SetMode(WicoControl.MODE_LAUNCH);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.OrbitalLaunch:
                        {
                            pg.wicoControl.SetMode(WicoControl.MODE_ORBITALLAUNCH);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Dock:
                        {
                            pg.wicoControl.SetMode(WicoControl.MODE_DOCKING);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Land:
                        {
                            pg.wicoControl.SetMode(WicoControl.MODE_DESCENT);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.unknown:
                        {
                            pg.Echo("Unknown Command");
                            pg.wicoControl.SetMode(WicoControl.MODE_ATTENTION);
                            return true;
                        }
                        //                        break;
                }
                return false;
            }
        }
    }

}


