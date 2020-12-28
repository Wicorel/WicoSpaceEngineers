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

        class OrbitalModes
        {
            Program _program;
            WicoControl _wicoControl;
            WicoBlockMaster _wicoBlockMaster;
            WicoThrusters _wicoThrusters;
            WicoGyros _gyros;
            Connectors _connectors;
            LandingGears _landingGears;
            GasTanks _gasTanks;
            GasGens _gasGens;
            Timers _timers;
            Displays _displays;
            Cameras _wicoCameras;

            public double MaxLaunchMps = 100f;
            public double MaxDescentMps = 107f;
            bool bAlignGravityHover = true;
            bool bOrbitalLaunchDebug = false;
            string sOrbitalSection = "ORBITAL";

            public OrbitalModes(Program program, WicoControl wicoControl, WicoBlockMaster wicoBlockMaster
                , WicoThrusters wicoThrusters, WicoGyros gyros
                , Connectors connectors, LandingGears landingGears
                , GasTanks gasTanks, GasGens gasGens
                , Timers timers, Displays displays
                , Cameras wicoCameras
                )
            {
                _program = program;
                _wicoControl = wicoControl;
                _wicoBlockMaster = wicoBlockMaster;
                _wicoThrusters = wicoThrusters;
                _gyros = gyros;
                _connectors = connectors;
                _landingGears = landingGears;
                _gasTanks = gasTanks;
                _gasGens = gasGens;
                _timers = timers;
                _displays = displays;
                _wicoCameras = wicoCameras;

                _program.moduleName += " Orbital";
                _program.moduleList += "\nOrbital V4.2h";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                bOrbitalLaunchDebug = _program._CustomDataIni.Get(sOrbitalSection, "OrbitalLaunchDebug").ToBoolean(bOrbitalLaunchDebug);
                _program._CustomDataIni.Set(sOrbitalSection, "OrbitalLaunchDebug", MaxLaunchMps);

                bAlignGravityHover = _program._CustomDataIni.Get(sOrbitalSection, "AlignGravityHover").ToBoolean(bAlignGravityHover);
                _program._CustomDataIni.Set(sOrbitalSection, "AlignGravityHover", MaxLaunchMps);

                MaxLaunchMps = _program._CustomDataIni.Get(sOrbitalSection, "LaunchSpeedMax").ToDouble(_wicoControl.fMaxWorldMps);
                _program._CustomDataIni.Set(sOrbitalSection, "LaunchSpeedMax", MaxLaunchMps);

                MaxDescentMps = _program._CustomDataIni.Get(sOrbitalSection, "DescentSpeedMax").ToDouble(MaxDescentMps);
                _program._CustomDataIni.Set(sOrbitalSection, "DescentSpeedMax", MaxDescentMps);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _displays.AddSurfaceHandler("MODE",SurfaceHandler);
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
                            iMode == WicoControl.MODE_ORBITALLAUNCH
                            || iMode == WicoControl.MODE_DESCENT
                            || iMode == WicoControl.MODE_ORBITALLAND
                            || iMode == WicoControl.MODE_HOVER
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
                        if(bValidOrbitalTransfer) tsurface.WriteText("GPS:" + _program.toGpsName("", "OrbitalTransferPoint") + ":" + _program.Vector3DToString(OrbitalTransferPoint) + ":", false);
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

            /// <summary>
            /// Do we have a vlid TargetLanding location?
            /// </summary>
            bool bValidTargetLanding = false;
            /// <summary>
            /// The location on the planet we want to land.
            /// </summary>
            Vector3D TargetLanding;
            Vector3D TargetPlanetLocation;
            Vector3D OrbitalTransferPoint;
            bool bValidOrbitalTransfer = false;

            void LoadHandler(MyIni Ini)
            {
                string s;
                s = Ini.Get(sOrbitalSection, "TargetLanding").ToString();
                Vector3D.TryParse(s, out TargetLanding);

                s = Ini.Get(sOrbitalSection, "TargetPlanetLocation").ToString();
                Vector3D.TryParse(s, out TargetPlanetLocation);

                s = Ini.Get(sOrbitalSection, "OrbitalTransferPoint").ToString();
                Vector3D.TryParse(s, out OrbitalTransferPoint);

                bValidTargetLanding = Ini.Get(sOrbitalSection, "ValidTargetLanding").ToBoolean(bValidTargetLanding);
                bValidOrbitalTransfer = Ini.Get(sOrbitalSection, "ValidOrbitalTransfer").ToBoolean(bValidOrbitalTransfer);
            }

            void SaveHandler(MyIni Ini)
            {
                Ini.Set(sOrbitalSection, "TargetPlanetLocation", TargetPlanetLocation.ToString());
                Ini.Set(sOrbitalSection, "TargetLanding", TargetLanding.ToString());
                Ini.Set(sOrbitalSection, "OrbitalTransferPoint", OrbitalTransferPoint.ToString());
                Ini.Set(sOrbitalSection, "ValidTargetLanding", bValidTargetLanding);
                Ini.Set(sOrbitalSection, "ValidOrbitalTransfer", bValidOrbitalTransfer);
            }

            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
                if (
                    fromMode == WicoControl.MODE_ORBITALLAUNCH
                    || fromMode == WicoControl.MODE_DESCENT
                    || fromMode == WicoControl.MODE_ORBITALLAND
                    || fromMode == WicoControl.MODE_HOVER
                 )
                {
                    _displays.ClearDisplays("MODE");
                    _gyros.gyrosOff();
                    _wicoThrusters.powerDownThrusters();
                    IMyShipController shipController = _wicoBlockMaster.GetMainController();
                    if (shipController != null)
                    {
                        shipController.DampenersOverride = true; // true means dampeners ON.
                    }
                }
                if (fromMode == WicoControl.MODE_LAUNCHPREP)
                {
                    _wicoThrusters.powerDownThrusters();
                    _gasTanks.TanksStockpile(false);
                    _gasGens.GasGensEnable(true);
                }


                if (toMode == WicoControl.MODE_ORBITALLAUNCH)
                {
                    _wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_HOVER)
                {
                    _wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_LAUNCHPREP)
                {
                    _wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_DESCENT)
                {
                    _wicoControl.WantOnce();
                }
                if (toMode == WicoControl.MODE_ORBITALLAND)
                {
                    _wicoControl.WantOnce();
                }
            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;


                if (iMode < 0)
                {
                    _wicoControl.SetMode(WicoControl.MODE_HOVER);
                }
                if (iMode == WicoControl.MODE_ORBITALLAUNCH)
                {
                    _wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_ORBITALLAND)
                {
                    _wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_DESCENT)
                {
                    _wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_HOVER)
                {
                    _wicoControl.SetState(0);
                    _wicoControl.WantFast();

                }
                else if (iMode == WicoControl.MODE_LAUNCHPREP)
                {
                    _wicoControl.SetState(0);
                    _wicoControl.WantFast();
                }

            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                if (myCommandLine != null && myCommandLine.ArgumentCount>0)
                {
                    if (myCommandLine.Argument(0) == "hover")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_HOVER);
                    }
                    if (myCommandLine.Argument(0) == "orbitallaunch")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_ORBITALLAUNCH);
                    }
                    if (myCommandLine.Argument(0) == "orbitalland")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_ORBITALLAND);
                    }
                    if (myCommandLine.Argument(0) == "descend")
                    {
                        _wicoControl.SetMode(WicoControl.MODE_DESCENT);
                        descentTargetAlt = 100;
                        if (myCommandLine.Argument(1) != null)
                        {
                            descentTargetAlt = Convert.ToInt32(myCommandLine.Argument(1));
                        }
                    }
                    if(myCommandLine.Argument(0) == "autohover")
                    {
                        bAlignGravityHover = !bAlignGravityHover;
                        _program.Echo("alignGravity=" + bAlignGravityHover.ToString());
                    }
                    if(myCommandLine.Argument(0) == "thrustcheck")
                    {

                        if (thrustForwardList.Count < 1)
                        {
                            _wicoThrusters.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                                ref thrustForwardList, ref thrustBackwardList,
                                ref thrustDownList, ref thrustUpList,
                                ref thrustLeftList, ref thrustRightList
                                );
                        }
                        double physicalMass = _wicoBlockMaster.GetPhysicalMass();

                        Vector3D vNGN = _wicoBlockMaster.GetMainController().GetNaturalGravity();
                        double dGravity = vNGN.Length() / 9.81;

                        double hoverthrust = physicalMass * dGravity * 9.810;

                        double forwardThrust =_wicoThrusters.calculateMaxThrust(thrustForwardList, WicoThrusters.thrusthydro);
                        double upwardThrust = _wicoThrusters.calculateMaxThrust(thrustUpList, WicoThrusters.thrusthydro);
                        _program.Echo("fwThrust=" + forwardThrust);
                        _program.Echo("upThrust=" + upwardThrust);

                    }
                }
            }


            void UpdateHandler(UpdateType updateSource)
            {
                _wicoControl.WantSlow();
                /*
                if (bValidOrbitalTransfer)
                {
                    Vector3D vBoreEnd = ( OrbitalTransferPoint - _program.wicoBlockMaster.GetMainController().CenterOfMass);
                    Vector3D vPosition = _program.wicoBlockMaster.GetMainController().CenterOfMass;
                    Vector3D vForward = vPosition+_program.wicoBlockMaster.GetMainController().WorldMatrix.Forward * vBoreEnd.Length();
                    Vector3D vAimEnd = vForward - vPosition;
                    Vector3D vRejectEnd = _gyros.VectorRejection(vBoreEnd, vAimEnd);
                    Vector3D vCorrectAimPoint = OrbitalTransferPoint + vRejectEnd * 2;
                    if(_wicoCameras.CameraForwardScan(vCorrectAimPoint))
                    {
                        _program.Echo("Scan worked");
                        _wicoCameras.CameraForwardScan(vForward);
                    }
                    else
                    {
                        _program.Echo("Scan failed");
                    }
//                    _wicoCameras.CameraForwardScan(vRejectEnd);
                }
                */
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
//                if ((updateSource & UpdateType.Update100) > 0)
                {
                    if (thrustOrbitalUpList.Count < 1)
                    {
                        _wicoThrusters.ThrustersCalculateOrientation(_wicoBlockMaster.GetMainController(),
                            ref thrustForwardList, ref thrustBackwardList,
                            ref thrustDownList, ref thrustUpList,
                            ref thrustLeftList, ref thrustRightList
                            );
                        Vector3D vNGN = _wicoBlockMaster.GetMainController().GetNaturalGravity();
                        vNGN.Normalize();
                        _wicoThrusters.GetBestThrusters(vNGN,
                            thrustForwardList, thrustBackwardList,
                            thrustDownList, thrustUpList,
                            thrustLeftList, thrustRightList,
                            out thrustOrbitalUpList, out thrustOrbitalDownList
                            );
                    }
                }
                if (thrustOrbitalUpList.Count >0)
                {
                    double physicalMass = _wicoBlockMaster.GetPhysicalMass();
                    Vector3D vNGN = _wicoBlockMaster.GetMainController().GetNaturalGravity();

                    double dGravity = vNGN.Length() / 9.81;

                    if (dGravity > 0)
                    {
                        double hoverthrust = physicalMass * dGravity * 9.810;
                        double thrustAvailable = _wicoThrusters.calculateMaxThrust(thrustOrbitalUpList);
                        double atmoThrustAvailable= _wicoThrusters.calculateMaxThrust(thrustOrbitalUpList,WicoThrusters.thrustatmo);
                        double ionThrustAvailable = _wicoThrusters.calculateMaxThrust(thrustOrbitalUpList, WicoThrusters.thrustion);
                        double hydroThrustAvailable = _wicoThrusters.calculateMaxThrust(thrustOrbitalUpList, WicoThrusters.thrusthydro);

                        _program.Echo("Needed=" + hoverthrust.ToString("0") + "\nAvail=" + thrustAvailable.ToString("0"));
                        if (atmoThrustAvailable > 0) _program.Echo("Atmo=" + atmoThrustAvailable.ToString("0"));
                        if (ionThrustAvailable > 0) _program.Echo("Ion=" + ionThrustAvailable.ToString("0"));
                        if (hydroThrustAvailable > 0) _program.Echo("Hyd=" + hydroThrustAvailable.ToString("0"));

                        if (hoverthrust == 0) _program.Echo("(We are connected to a station)");
                        if (thrustAvailable > 0) _program.Echo((hoverthrust * 100 / thrustAvailable).ToString("0.00") + "% Thrust Needed");

                        if (hoverthrust > thrustAvailable)
                        {
                            sbModeInfo.AppendLine("OVERWEIGHT");
                            _program.Echo("OVERWEIGHT");
                            if(iState==35 || iState==45)
                            {
                                // we are already attempting change
                            }
                            else if ( CheckAttitudeChange())
                            {
                                _wicoControl.SetState(35);
                                _wicoControl.WantFast();
                                return;
                            }
                        }
                    }
                    
                }
                if (bValidOrbitalTransfer)
                    _program.Echo("Valid Orbital Transfer");
                if (bValidTargetLanding)
                    _program.Echo("Valid Landing Target");

                // need to check if this is us
                if (iMode == WicoControl.MODE_ORBITALLAUNCH)
                {
                    ModeOrbitalLaunch(updateSource);
                }
                else if (iMode == WicoControl.MODE_LAUNCHPREP)
                {
                    doModeLaunchprep(updateSource);
                }
                else if (iMode == WicoControl.MODE_HOVER)
                {
                    ModeHover(updateSource);
                }
                else if (iMode == WicoControl.MODE_DESCENT)
                {
                    doModeDescent(updateSource);
                }
                else if (iMode == WicoControl.MODE_ORBITALLAND)
                {
                    doModeDescent(updateSource);
                }
            }


            int retroStartDistance = 1300;

            int descentTargetAlt = 100;

            float orbitalAtmoMult = 5;
            float orbitalIonMult = 2;
            float orbitalHydroMult = 1;

            double dLastVelocityShip = -1;

            float fOrbitalAtmoPower = 0;
            float fOrbitalHydroPower = 0;
            float fOrbitalIonPower = 0;

            bool bHasAtmo = false;
            bool bHasHydro = false;
            bool bHasIon = false;


            float PhysicalMass;


            //            string sOrbitalUpDirection = "";
            Vector3D vBestThrustOrientation;

            List<IMyTerminalBlock> thrustOrbitalUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustOrbitalDownList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> cameraOrbitalLandingList = new List<IMyTerminalBlock>();

            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();
            // MODE_ORBITAL_LAUNCH states
            // 0 init. prepare for solo
            // check all connections. hold launch until disconnected
            // 
            // 10 capture location and init thrust settings.
            // 20 initial thrust. trying to move
            // 30 initial lift-off achieved.  start landing config retraction
            // 31 continue to accelerate
            // 35 optimal alignment change.  wait  for new alignment

            // 40 have reached max; maintain

                //45  (re-orient) UNUSED
            // 100 we have reached space.  Aim best thrust in direction of travel


            // 150 wait for release..

            // 200 Not enough thrust to launch.

            public void ModeOrbitalLaunch(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                // need to check if this is us
                if (iMode != WicoControl.MODE_ORBITALLAUNCH)
                {
                    return;
                }
                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("Orbital Launch");
                _program.Echo("MODE: Orbital Launch");
                //                _program.Echo(updateSource.ToString());

                //                _wicoControl.WantMedium();
                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();

                bool bAligned = false;
                if (iState == 0 || thrustOrbitalUpList.Count < 1)
                {
                    MyShipMass myMass;
                    myMass = shipController.CalculateShipMass();

                    PhysicalMass = myMass.PhysicalMass;

                    _wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    Vector3D vNGN = vNG;
                    vNGN.Normalize();
                    _wicoThrusters.GetBestThrusters(vNGN,
                        thrustForwardList, thrustBackwardList,
                        thrustDownList, thrustUpList,
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList
                        );
                    Matrix or1;
                    if (thrustOrbitalUpList.Count > 0)
                    {

                        thrustOrbitalUpList[0].Orientation.GetMatrix(out or1);
                        vBestThrustOrientation = or1.Forward; // start out aiming at whatever the up thrusters are aiming at..
//                        vBestThrustOrientation = thrustOrbitalUpList[0].WorldMatrix.Forward;
                    }
                    else
                    {
                        shipController.Orientation.GetMatrix(out or1);
                        vBestThrustOrientation = or1.Down; // assumes forward facing cockpit
//                        vBestThrustOrientation = shipController.WorldMatrix.Down;
                    }


                    /*
                    _wicoThrusters.GetMaxScaledThrusters(
                        thrustForwardList, thrustBackwardList, 
                        thrustDownList, thrustUpList, 
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList,
                        orbitalAtmoMult, orbitalIonMult, orbitalHydroMult);

                    */


                    //                    calculateBestGravityThrust();
                    
                    bHasAtmo = false;
                    bHasHydro = false;
                    bHasIon = false;
                    if (_wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustion) != null)
                        bHasIon = true;
                    if (_wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrusthydro) != null)
                        bHasHydro = true;
                    if (_wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustatmo) != null)
                        bHasAtmo = true;

                }
                if (iState == 0)
                {
                    //		dtStartShip = DateTime.Now;
                    MaxLaunchMps = _program._CustomDataIni.Get(sOrbitalSection, "LaunchSpeedMax").ToDouble(_wicoControl.fMaxWorldMps);

                    _wicoControl.WantOnce();
                    //                    dLastVelocityShip = 0;
                    //                    if ((craft_operation & CRAFT_MODE_NOTANK) == 0) 
                    _gasTanks.TanksStockpile(false);
                    _gasGens.GasGensEnable(true);

                    if (_connectors.AnyConnectorIsConnected()
                        || _connectors.AnyConnectorIsLocked()
                        || _landingGears.AnyGearIsLocked())
                    {
                        // launch from connected
                        //                       vOrbitalLaunch = shipOrientationBlock.GetPosition();
                        //                        bValidOrbitalLaunch = true;
                        //                        bValidOrbitalHome = false; // forget any 'home' waypoint.

                        _connectors.ConnectAnyConnectors(false, false);
                        bValidTargetLanding = true;

                        TargetLanding = shipController.GetPosition();
                        shipController.TryGetPlanetPosition(out TargetPlanetLocation);

                        _landingGears.GearsLock(false);
                        _wicoControl.SetState(150);// = 150;
                        return;
                    }
                    else
                    {
                        // launch from hover mode
                        //                        bValidOrbitalLaunch = false;
                        //                        vOrbitalHome = shipOrientationBlock.GetPosition();
                        //                        bValidOrbitalHome = true;
                        bValidTargetLanding = false;

                        // assume we are hovering; do FULL POWER launch.
                        fOrbitalAtmoPower = 0;
                        fOrbitalHydroPower = 0;
                        fOrbitalIonPower = 0;

                        if (bHasIon)
                            fOrbitalIonPower = 75;
                        if (bHasHydro)
                        { // only use Hydro power if they are already turned on
                            for (int i = 0; i < thrustOrbitalUpList.Count; i++)
                            {
                                if (_wicoThrusters.ThrusterType(thrustOrbitalUpList[i]) == WicoThrusters.thrusthydro)
                                    if (thrustOrbitalUpList[i].IsWorking)
                                    {
                                        fOrbitalHydroPower = 100;
                                        break;
                                    }
                            }
                        }
                        //                        if (bHasAtmo)
                        if (_wicoThrusters.ThrustFindFirst(thrustOrbitalUpList, WicoThrusters.thrustatmo) != null)
                            fOrbitalAtmoPower = 100;

                        _wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

                        _wicoControl.SetState(30);
                        //                        current_state = 30;
                        return;
                    }
                }
                if (iState == 150)
                {
                    //                    StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + current_state.ToString(), textLongStatus, true);
                    if (_connectors.AnyConnectorIsConnected() || _connectors.AnyConnectorIsLocked() || _landingGears.AnyGearIsLocked())
                    {
                        //                        StatusLog("Awaiting release", textPanelReport);
                        sbModeInfo.AppendLine("Awaiting Release");
                        //                        Log("Awaiting release");
                    }
                    else
                    {
                        //                        StatusLog(DateTime.Now.ToString() + " " + OurName + ":Saved Position", textLongStatus, true);

                        // we launched from connected. Save position
                        //                        vOrbitalLaunch = shipOrientationBlock.GetPosition();
                        //                        bValidOrbitalLaunch = true;
                        //                        bValidOrbitalHome = false; // forget any 'home' waypoint.

                        _wicoControl.SetState(10);

                        //                        next_state = 10;
                    }

                }
                double elevation = 0;

                shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                sbNotices.AppendLine("Elevation="+ elevation.ToString("N0") + " Meters");
                //               StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
                double alt = elevation; // note: if we use camera raycast, we can more accuratly determine altitude.

                double velocityShip = shipController.GetShipSpeed();
                double deltaV = velocityShip - dLastVelocityShip;
                double expectedV = deltaV * 5 + velocityShip;

                double dLength = vNG.Length();
                double dGravity = dLength / 9.81;
                sbNotices.AppendLine("Gravity: "+ dGravity.ToString("0.00"));

                if (iState == 10)
                {
                    _wicoControl.WantOnce();
                    _wicoThrusters.CalculateHoverThrust( thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                    _wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);


                    double physicalMass = _wicoBlockMaster.GetPhysicalMass();

                    //                    Vector3D vNGN = _wicoBlockMaster.GetMainController().GetNaturalGravity();
                    //                    double dGravity = vNGN.Length() / 9.81;

                    double hoverthrust = physicalMass * dGravity * 9.810;
                    double thrustAvailable = _wicoThrusters.calculateMaxThrust(thrustOrbitalUpList);

                    if (hoverthrust > thrustAvailable)
                    {
                        // Not enough thrust in desired direction
                        sbModeInfo.AppendLine("OVERWEIGHT");
//                        sbNotices.AppendLine("OVERWEIGHT");
                        _program.Echo("OVERWEIGHT");
                        _connectors.ConnectAnyConnectors(true, true);

                        _landingGears.GearsLock(true);
                    }
                    else _wicoControl.SetState(20);
                    return;
                }
                if (iState == 20)
                { // trying to move
                    sbModeInfo.AppendLine("Attemtping Lift-off");
                  //                    StatusLog("Attempting Lift-off", textPanelReport);
                  //                    Log("Attempting Lift-off");
                  // NOTE: need to NOT turn off atmo if we get all the way into using ions for this state.. and others?
                    if (velocityShip < 3f)
                    // if(velocityShip<1f)
                    {
                        increasePower(dGravity, alt);
                        increasePower(dGravity, alt);
                    }
                    else
                    {
                        _wicoControl.SetState(30);// we have started to lift off.
                                                             //                        next_state = 30; // we have started to lift off.
                        dLastVelocityShip = 0;
                    }

                    bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                    if (!bAligned)
                        _wicoControl.WantFast();
                    else _wicoControl.WantMedium();
                }
                else
                {
                    _wicoControl.WantMedium();
                    if (alt > 5)
                    {
                        _gyros.SetMinAngle();
                        bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                        if (!bAligned)
                            _wicoControl.WantFast();
                    }
                }

                if (iState == 30)
                { // Retract landing config
                  //                    StatusLog("Movement started. Retracting Landing config ", textPanelReport);
                  //                    Log("Movement started. Retracting Landing config ");
                    sbModeInfo.AppendLine("Movement started. Retracting Landing config ");
                    _timers.TimerTriggers("[Lift-Off]");
                    _wicoControl.SetState(31);// next_state = 31;
                    _wicoControl.WantFast();// bWantFast = true;
                }
                if (iState == 31)
                { // accelerate to max speed

                    //                    StatusLog("Accelerating to max speed (" + _wicoControl.fMaxWorldMps.ToString("0") + ")", textPanelReport);
                    sbModeInfo.AppendLine("Accelerating to max speed");
                    //                    Log("Accelerating to max speed");
                    _program.Echo("Accelerating to max speed");

                    // TODO: only check every so often.
                    if (CheckAttitudeChange())
                    {
                        _wicoControl.SetState(35);// current_state = 35;
                        _wicoControl.WantFast();// bWantFast = true;
                        return;

                    }
                    _wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

                    if (dLastVelocityShip < velocityShip)
                    { // we are Accelerating
                        if (bOrbitalLaunchDebug) _program.Echo("Accelerating");
                        if (expectedV < (MaxLaunchMps / 2))
                        // if(velocityShip<(fMaxMps/2))
                        {
                            decreasePower(dGravity, alt); // take away from lowest provider
                            increasePower(dGravity, alt);// add it back
                        }
                        if (expectedV < (_wicoControl.fMaxWorldMps / 5)) // add even more.
                            increasePower(dGravity, alt);

                        if (velocityShip > (MaxLaunchMps - 5))
                            _wicoControl.SetState(40);// next_state = 40;
                    }
                    else
                    {
                        increasePower(dGravity, alt);//
                        increasePower(dGravity, alt);// and add some more
                    }
                }
                if (iState == 35)
                {
                    sbModeInfo.AppendLine("[Launch-Reorient]");

                    _timers.TimerTriggers("[Launch-Reorient]");
                    // re-align and then resume
                    _wicoThrusters.powerDownThrusters();
                    //                bAligned = GyroMain(sOrbitalUpDirection);
                    //                    if (bAligned)
//                    _wicoControl.SetState(31); // next_state = 31;
                    _wicoControl.SetState(45);
                    _wicoControl.WantFast();// bWantFast = true;
                }
                if (iState == 40)
                { // maintain max speed
                    sbModeInfo.AppendLine("Maintain max speed");
                    sbModeInfo.AppendLine(_program.niceDoubleMeters(expectedV) + " m/s");
                    sbModeInfo.AppendLine("Altitude=" + alt.ToString("N0"));
//                    sbNotices.AppendLine("Maintain max speed");
                    _program.Echo("Maintain max speed");
                    //                    Log("Maintain max speed");

                    // TODO: only check every so often.
                    if (CheckAttitudeChange())
                    {
                        _wicoControl.SetState(35);// current_state = 35;
                        _wicoControl.WantFast();// bWantFast = true;
                        return;

                    }
                    //                  if (bOrbitalLaunchDebug) StatusLog("Expectedv=" + expectedV.ToString("0.00") + " max=" + _wicoControl.fMaxWorldMps.ToString("0.00"), textPanelReport);
                    //                    if (bOrbitalLaunchDebug)
                    //                        _program.Echo("Expectedv=" + expectedV.ToString("0.00") + " max=" + _wicoControl.fMaxWorldMps.ToString("0.00"));
                    double dMin = (MaxLaunchMps - MaxLaunchMps * .02); // within n% of max mps
                                                                                                                       //                    _program.Echo("dMin=" + dMin.ToString("0.00"));
                    if (expectedV > dMin)
                    // if(velocityShip>(fMaxMps-5))
                    {
                        bool bThrustOK = _wicoThrusters.CalculateHoverThrust(thrustOrbitalUpList, out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower);
                        //                        if (bOrbitalLaunchDebug) 
                        //                        _program.Echo("hover thrust:" + fOrbitalAtmoPower.ToString("0.00") + ":" + fOrbitalHydroPower.ToString("0.00") + ":" + fOrbitalIonPower.ToString("0.00"));
                        //                        if (bOrbitalLaunchDebug) StatusLog("hover thrust:" + fOrbitalAtmoPower.ToString("0.00") + ":" + fOrbitalHydroPower.ToString("0.00") + ":" + fOrbitalIonPower.ToString("0.00"), textPanelReport);

                    }
                    else if (expectedV < (MaxLaunchMps - 10))
                    {
                        //                      _program.Echo("Increase power");
                        decreasePower(dGravity, alt); // take away from lowest provider
                        increasePower(dGravity, alt);// add it back
                        increasePower(dGravity, alt);// and add some more
                    }
                    if (velocityShip < (MaxLaunchMps / 2))
                    {
                        _wicoControl.SetState(20);// next_state = 20;
                        _wicoControl.WantFast();// bWantFast = true;
                    }

                    _connectors.ConnectAnyConnectors(false, true);// "OnOff_On");
                    _landingGears.BlocksOnOff(true);// blocksOnOff(gearList, true);
                    //                blockApplyAction(gearList, "OnOff_On");
                }
                if (iState == 45)
                {
                    // re-align and then resume
                    _wicoThrusters.powerDownThrusters();
                    bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);

                    if (bAligned)
                    {
                        _wicoControl.SetState(31);
                        _wicoControl.WantFast();
                        return; 
                    }
                }
                dLastVelocityShip = velocityShip;

                if (iState == 100)
                {
                    // we have just reached space
                    sbModeInfo.AppendLine("[Reached-Orbit]");
                    _timers.TimerTriggers("[Reached-Orbit]");

                    OrbitalTransferPoint = shipController.GetPosition(); // save our exit location
                    bValidOrbitalTransfer = true;

                    CheckAttitudeChange(true);

                    _program.ResetMotion();
                    _gyros.gyrosOff();
                    _wicoThrusters.powerDownThrusters(); // turn on all thrusters
                    shipController.DampenersOverride = true;

                    _wicoControl.SetState(110);
                    _wicoControl.WantFast();
                    return;
                }
                if (iState == 110)
                {
                    sbModeInfo.AppendLine("Turning to stop");
//                    sbNotices.AppendLine("Turning to stop");
                    MyShipVelocities myShipVelocities = shipController.GetShipVelocities();
//                    bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                    bAligned = _gyros.AlignGyros(vBestThrustOrientation, myShipVelocities.LinearVelocity);
                    if (bAligned)
                    {
                        _gyros.gyrosOff();
                        _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                    }
                    else _wicoControl.WantFast();
                    return;
                }

                if (dGravity < 0.01)
                {
                    _wicoThrusters.powerDownThrusters();
                    _gyros.gyrosOff();
                    _wicoControl.SetState(100);
                    _wicoControl.WantFast();// bWantFast = true;
                    return;
                }

                int iPowered = 0;

                //                _program.Echo("IonPower=" + fOrbitalIonPower.ToString("0.00"));
                if (fOrbitalIonPower > 0.01)
                {
                    _wicoThrusters.powerDownThrusters(WicoThrusters.thrustatmo, true);
                    _wicoThrusters.powerDownThrusters(WicoThrusters.thrustion);
                    iPowered = _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, fOrbitalIonPower, WicoThrusters.thrustion);
                    //Echo("Powered "+ iPowered.ToString()+ " Ion Thrusters");
                }
                else
                {
                    _wicoThrusters.powerDownThrusters(WicoThrusters.thrustion, true);
                    _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion);
                }

                //                _program.Echo("HydroPower=" + fOrbitalHydroPower.ToString("0.00"));
                if (fOrbitalHydroPower > 0.01)
                {
                    //                Echo("Powering Hydro to " + fHydroPower.ToString());
                    _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, fOrbitalHydroPower, WicoThrusters.thrusthydro);
                }
                else
                { // important not to let them provide dampener power..
                    _wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrusthydro, true);
                    _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro, true);
                }
                //                _program.Echo("AtmoPower=" + fOrbitalAtmoPower.ToString("0.00"));
                if (fOrbitalAtmoPower > 0.01)
                {
                    _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, fOrbitalAtmoPower, WicoThrusters.thrustatmo);
                }
                else
                {
                    //                    closeDoors(outterairlockDoorList);

                    // iPowered=powerDownThrusters(thrustStage1UpList,thrustatmo,true);
                    iPowered = _wicoThrusters.powerDownThrusters(WicoThrusters.thrustatmo, true);
                    //Echo("Powered DOWN "+ iPowered.ToString()+ " Atmo Thrusters");
                }

                {
                    _wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);
                }

            }

            double tmWarningElapsedMs = -1;
            double tmWarningWaitMs = 750;

            // states
            // 0 = init
            // 10 = powered hovering. No connections
            // 20 = landing gear locked. 
            // 
            double elevation = 0;
            double dGravity =-1;

            void ModeHover(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                sbNotices.Clear();
                sbModeInfo.Clear();
                sbModeInfo.AppendLine("HOVER!");
                //                StatusLog("clear", textPanelReport);
                _program.Echo("Hover Mode:" + iState);
                //                StatusLog(OurName + ":" + moduleName + ":Hover", textPanelReport);
                //                StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);

                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dLength = vNG.Length();
                dGravity = dLength / 9.81;
                shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                sbNotices.AppendLine("Elevation=" + elevation.ToString("0.0"));

                //                StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
                if (iState == 0)
                {
//                    _program.sMasterReporting += "H0:Controller=" + shipController.CustomName+"\n";
                    _wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );


                    Vector3D vNGN = vNG;
                    vNGN.Normalize();
                    _wicoThrusters.GetBestThrusters(vNGN,
                        thrustForwardList, thrustBackwardList,
                        thrustDownList, thrustUpList,
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList
                        );

                    //                    _program.sMasterReporting += "FW thrust0=" + thrustForwardList[0].CustomName + "\n";
                    //                    _program.sMasterReporting += "UP thrust0=" + thrustUpList[0].CustomName + "\n";
                    //                    _program.sMasterReporting += "OUP thrust0=" + thrustOrbitalUpList[0].CustomName + "\n";
                    //                    _program.sMasterReporting += "OBACK thrust0=" + thrustOrbitalDownList[0].CustomName + "\n";

                    Matrix or1;
                    if (thrustOrbitalUpList.Count > 0)
                    {
                        thrustOrbitalUpList[0].Orientation.GetMatrix(out or1);
                        vBestThrustOrientation = or1.Forward; // start out aiming at whatever the up thrusters are aiming at..
//                        vBestThrustOrientation = thrustOrbitalUpList[0].WorldMatrix.Forward; // start out aiming at whatever the up thrusters are aiming at..
//                        _program.sMasterReporting += " H0:Force Uplist forward";
//                        _program.sMasterReporting += " H0:Up=" + thrustOrbitalUpList[0].CustomName;
                    }
                    else
                    {
                        shipController.Orientation.GetMatrix(out or1);
                        vBestThrustOrientation = or1.Down; // assumes forward facing cockpit
                    }
                    _gyros.SetMinAngle(0.005f);
                    _wicoControl.SetState(10);
                }

                float fAtmoPower, fHydroPower, fIonPower;
                _wicoThrusters.CalculateHoverThrust(thrustOrbitalUpList, out fAtmoPower, out fHydroPower, out fIonPower);

                if (fAtmoPower > 75)
                {
                    _program.Echo(">75% atmo thrust needed!");
                }
                else tmWarningElapsedMs = -1;

                    float fAverage = 0;
                    int typesCount = 0;
                    if (fAtmoPower > 0)
                    {
                        fAverage += fAtmoPower;
                        typesCount++;
                    }
                    if (fHydroPower > 0)
                    {
                        fAverage += fHydroPower;
                        typesCount++;
                    }
                    if (fIonPower > 0)
                    {
                        fAverage += fIonPower;
                        typesCount++;
                    }
                    if (typesCount > 0)
                        fAverage /= typesCount;
                    if (fAverage > 75)
                    {
//                                _program.Echo("tmwarningelapsed=" + tmWarningElapsedMs.ToString("0.0"));
                        if (tmWarningElapsedMs < 0)
                        {
                            tmWarningElapsedMs = 0;
//                                  ts.BackgroundColor = Color.Red;
//                                    ts.FontColor = Color.Black;
                        }
                        else
                        {
                            tmWarningElapsedMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                            if(tmWarningElapsedMs>tmWarningWaitMs)
                            {
                                tmWarningElapsedMs = 0;
                                /*
                                if(ts.BackgroundColor!=Color.Black)
                                {
                                    ts.BackgroundColor = Color.Black;
                                    ts.FontColor = Color.Red;
                                }
                                else
                                {
                                    ts.BackgroundColor = Color.Red;
                                    ts.FontColor = Color.Black;
                                }
                                */
                            }
                        }
                        sbNotices.AppendLine("LIFT!");

//                                ts.WriteText("LIFT!\n");
                    }
                    else
                    {
//                                ts.WriteText("");
//                                ts.BackgroundColor = Color.Black;
//                                ts.FontColor = Color.White;
                    }
                    sbNotices.AppendLine("A=" + fAtmoPower.ToString("0.0"));
                    sbNotices.AppendLine("H=" + fHydroPower.ToString("0.0"));
                    sbNotices.AppendLine("I=" + fIonPower.ToString("0.0"));

                //                else _program.Echo("shipcontroller NOT under controll\n"+shipController.CustomName);

                // if they are needed, ensure that they are on
                _program.Echo("A=" + fAtmoPower.ToString("0") + " H=" + fHydroPower.ToString() + " I=" + fIonPower.ToString());
                if (fAtmoPower > 0)
                {
//                    _program.Echo("Forcing Atmo On");
                    _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustatmo);
                }
                if (fHydroPower > 0)
                {
//                    _program.Echo("Forcing Hydro On");
                    _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro);
                }
                if (fIonPower > 0)
                {
//                    _program.Echo("Forcing Ion On");
                    _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion);
                }


                bool bGearsLocked = _landingGears.AnyGearIsLocked();
                bool bConnectorsConnected = _connectors.AnyConnectorIsConnected();
                bool bConnectorIsLocked = _connectors.AnyConnectorIsLocked();
                bool bGearsReadyToLock = _landingGears.anyGearReadyToLock();

                _wicoControl.WantMedium();

                if (bGearsLocked)
                {
                    if (iState != 20)
                    {
                        // gears just became locked
                        _program.Echo("Force thrusters Off!");
                        _wicoThrusters.powerDownThrusters(WicoThrusters.thrustAll, true);

                        //                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        {
                            _gasTanks.TanksStockpile(true);
                            _gasGens.GasGensEnable();
                        }
                        _wicoControl.SetState(20);// iState = 20;
                    }
                    //                    landingDoMode(1);
                }
                else
                {
                    if (iState == 10)
                    {
                        // we where locked with landing gear
                        //                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        {
                            _wicoThrusters.powerDownThrusters(); // turns ON all thusters
                            _gasTanks.TanksStockpile(false);
                            //? gas gens off?
                            // check power usage to see if atmo/ion can run what we lift
                        }

                        _wicoControl.SetState(10);// iState = 10;
                    }
                    //                    landingDoMode(0);
                }
                /*
                // add to delay time
                if (HoverCameraElapsedMs >= 0) HoverCameraElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

                // check for delay
                if (HoverCameraElapsedMs > HoverCameraWaitMs || HoverCameraElapsedMs < 0) // it is time to scan..
                {
                    if (doCameraScan(cameraOrbitalLandingList, elevation * 2)) // scan down 2x current alt
                    {
                        HoverCameraElapsedMs = 0;
                        // we are able to do a scan
                        if (!lastDetectedInfo.IsEmpty())
                        { // we got something
                            double distance = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.HitPosition.Value);
                            //			if (distance < elevation)
                            { // try to land on found thing below us.
                                _program.Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                                if (!bGearsLocked) StatusLog("Hovering above: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                            }
                        }
                    }
                }
                else _program.Echo("Camera Scan delay");
                */

                if (bGearsLocked)
                {
                    sbNotices.AppendLine("Landing Gear Locked");
                    //                    StatusLog("Landing Gear(s) LOCKED!", textPanelReport);
                    // we can turn off thrusters.. but that's about it..
                    // stay in 'hover' iMode
                }
                else if (bGearsReadyToLock)
                {
                    sbNotices.AppendLine("Landing Gear(s) Ready to lock.");
                    //                    StatusLog("Landing Gear(s) Ready to lock.", textPanelReport);
                }
                if (bConnectorsConnected)
                {
                    //prepareForSupported();
                    //                    StatusLog("Connector connected!\n   auto-prepare for launch", textPanelReport);
                    _wicoControl.SetMode(WicoControl.MODE_LAUNCHPREP);
                    _program.ErrorLog("Hover: Forcing Launch Prep due to connector connected");
                }
                else
                {
                    if (!bGearsLocked)
                    {
                        //			blockApplyAction(thrustAllList, "OnOff_On");
                        //			if ((craft_operation & CRAFT_MODE_NOTANK) == 0) blockApplyAction(tankList,"Stockpile_On");
                    }
                    _connectors.ConnectAnyConnectors(false, true);// "OnOff_On");
                }

                if (bConnectorIsLocked)
                {
                    //                   StatusLog("Connector Locked!", textPanelReport);
                }

                if (bConnectorIsLocked || bGearsLocked)
                {
                    _program.Echo("Stable");
                    //                    landingDoMode(1); // landing mode
                    _gyros.gyrosOff();
                }
                else
                {
                    /*
                    if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                    {
                        gyrosOff();
                        StatusLog("Wico Gravity Alignment OFF", textPanelReport);
                    }
                    else
                    */
                    if (bAlignGravityHover)
                    {
                        //                       StatusLog("Gravity Alignment Operational", textPanelReport);

                        /*
                        string sOrientation = "";
                        if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                            sOrientation = "rocket";
                        */
                        //                        bool bAimed = GyroMain(sOrbitalUpDirection);
                        sbNotices.AppendLine("Aligning to Gravity");
                        _program.Echo("Aligning to gravity");
                        //                        vBestThrustOrientation = thrustOrbitalUpList[0].WorldMatrix.Forward;
                        //                        _program.Echo("bestThrust:" + vBestThrustOrientation.ToString()+"\nvNG="+vNG.ToString());
                        bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                        //                        bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vNG, shipController);
                        if (bAimed)
                        {
                            _wicoControl.WantMedium();
                        }
                        else
                        {
                            _program.Echo("Not Aligned: Request Fast!");
                            _wicoControl.WantFast();
                        }
                    }
                    else
                    {
                        sbNotices.AppendLine("Not aligning to Gravity");
                        _program.Echo("Not aligning to gravity");
                    }
                }

              if (dGravity <= 0)
                {
                    _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                    _gyros.gyrosOff();
                    //                    StatusLog("clear", textPanelReport);
                }

            }

            void doModeLaunchprep(UpdateType updateSource)
            {
                //           IMyTextPanel textPanelReport = this.textPanelReport;

                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbNotices.AppendLine("Launch Prep");
                _wicoControl.WantMedium();

                //                StatusLog("clear", textPanelReport);

                //                StatusLog(OurName + ":" + moduleName + ":Launch Prep", textPanelReport);
                //                StatusLog("Planet Gravity: " + dGravity.ToString(velocityFormat) + " g", textPanelReport);

                _program.Echo(":LaunchPrep:" + iState);
                //           Echo("BatteryPercentage=" + batteryPercentage);
                //            Echo("batterypctlow=" + batterypctlow);
                //                double elevation = 0;

                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dLength = vNG.Length();
                double dGravity = dLength / 9.81;

                if (dGravity <= 0)
                {
                    if (_connectors.AnyConnectorIsConnected()) _wicoControl.SetMode(WicoControl.MODE_DOCKED);
                    else
                    {
                        _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                        _gyros.gyrosOff();
                        _wicoControl.WantFast();

                        //                        StatusLog("clear", textPanelReport);
                    }
                    return;
                }

                bool bGearLocked = _landingGears.AnyGearIsLocked();
                bool bAnyConnectorLocked = _connectors.AnyConnectorIsLocked();
                bool bAnyConnectorConnected = _connectors.AnyConnectorIsConnected();

                _program.Echo(" Gear=" + bGearLocked.ToString() + "\n ConLock=" + bAnyConnectorLocked.ToString() + "\n ConConn=" + bAnyConnectorConnected.ToString());

                if (bAnyConnectorConnected)
                {
                    sbNotices.AppendLine("Connector connected!");
                    sbNotices.AppendLine("  auto-prepare for launch");
                    //                    StatusLog("Connector connected!\n   auto-prepare for launch", textPanelReport);
                }
                else
                {
                    // no connectors connected
                    if (!bGearLocked)
                    {
                        //                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        _gasTanks.TanksStockpile(false); // blockApplyAction(tankList, "Stockpile_Off");
                        _wicoControl.SetMode(WicoControl.MODE_HOVER);
                    }
                    _connectors.ConnectAnyConnectors(false, true);// "OnOff_On");
                }

                if (bAnyConnectorConnected || bGearLocked)
                {
                    sbNotices.AppendLine("Stable");
                    _program.Echo("Stable");
                }
                else
                {
                    //prepareForSolo();
                    _wicoControl.SetMode(WicoControl.MODE_HOVER);
                    return;
                }

                if (bAnyConnectorConnected)
                {
                    if (iState == 0)
                    {
                        _wicoThrusters.powerDownThrusters(WicoThrusters.thrustAll, true);// blockApplyAction(thrustAllList, "OnOff_Off");
                                                                                                    //                        if ((craft_operation & CRAFT_MODE_NOTANK) == 0)
                        _gasTanks.TanksStockpile(true);// blockApplyAction(tankList, "Stockpile_On");

                        _wicoControl.SetState(1); // current_state = 1;
                    }
                    else if (iState == 1)
                    {
                        //			if ((craft_operation & CRAFT_MODE_NOPOWERMGMT) == 0)
                        _wicoControl.SetState(4);// current_state = 4; // skip battery checks
                                                            //			else
                                                            //			if (!batteryCheck(30, true))
                                                            //				current_state = 2;
                    }
                    else if (iState == 2)
                    {
                        //			if (!batteryCheck(80, true))
                        _wicoControl.SetState(3);// current_state = 3;
                    }
                    else if (iState == 3)
                    {
                        //			if (!batteryCheck(100, true))
                        _wicoControl.SetState(1);// current_state = 1;
                    }
                    else if(iState==4)
                    {

                    }
                }
                //	else             batteryCheck(0, true); //,textBlock);
                // TODO: same thing we do when docked in space.....
                //TODO: Check reactors and pull uranium
                //TODO: Check gas gens and pull ice

                //	StatusLog("C:" + progressBar(cargopcent), textBlock);

                /*
                if (batteryList.Count > 0)
                {
                    StatusLog("Bat:" + progressBar(batteryPercentage), textPanelReport);
                    Echo("BatteryPercentage=" + batteryPercentage);
                }
                else StatusLog("Bat: <NONE>", textPanelReport);

                if (oxyPercent >= 0)
                {
                    StatusLog("O2:" + progressBar(oxyPercent * 100), textPanelReport);
                    //Echo("O:" + oxyPercent.ToString("000.0%"));
                }
                else Echo("No Oxygen Tanks");

                if (hydroPercent >= 0)
                {
                    StatusLog("Hyd:" + progressBar(hydroPercent * 100), textPanelReport);
                    if (hydroPercent < 0.20f)
                        StatusLog(" WARNING: Low Hydrogen Supplies", textPanelReport);

                    Echo("H:" + hydroPercent.ToString("000.0%"));
                }
                else Echo("No Hydrogen Tanks");
                if (batteryList.Count > 0 && batteryPercentage < batterypctlow)
                    StatusLog(" WARNING: Low Battery Power", textPanelReport);

                */
            }


            // we have entered gravity well 
            // 0=initialize 
            // 10=dampeners on. aim towards target 
            // 11=aligned check 
            // 20=dampeners on. minor thrust fowards to align motion to target 
            // 21 hold alignment 
            // 22 hold alignment 
            // 23 hold alignment 
            // 30=dampeners off 
            // 40=free-falll. continue alignment. when in range for 180. start 180 

            // 60= check for 180 completed 
            // 61= perform align to gravity vector. ->70 if complete
            // 70=check for in retro-burn range of target in range; Dampeners on 
            // 80 descent going too fast.  slow down (dampeners)
            // 90=wait for zero velocity 
            // 100 descent/landing completed 
            // 200 final descent 
            // 210 alt>300
            // 220 alt >100
            // 230 alt>20
            // 240 else close

            // 300 get parachute orientation
            // 310 wait for parachute height
            // then ->200


            // 500 Go to orbitalTransfer point ->10 if close
            // 510 go/arrive orbital transfer. ->10 when close



            //            https://spaceengineerswiki.com/Parachute_Hatch#Terminal_Velocity

            //bool bOverTarget=false; 
            void doModeDescent(UpdateType updateSource)
            {
                // todo: handle parachutes.  
                // TODO: Check parachute orientation
                // TODO: handle 'stop XX meters above surface' (then hover, next nav command)
                // TODO: Arrived Target in gravity -> hover mode (should be in Nav, I guess)
                // TODO: Calculate best orientation through descent.

                // TODO: search for nearby planets to land on
                // to allow: nav to 'approach' waypoint, find planet. aim at center, start descent/land)
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbModeInfo.Clear();

                //               StatusLog("clear", textPanelReport);
                //               StatusLog(OurName + ":" + moduleName + ":Descent", textPanelReport);
                //               StatusLog("Gravity=" + dGravity.ToString(velocityFormat), textPanelReport);

                if (iMode == WicoControl.MODE_ORBITALLAND)
                {
                    sbModeInfo.AppendLine("Orbital Land");
                    _program.Echo("Orbital Land");
                }
                if (iMode == WicoControl.MODE_DESCENT)
                {
                    sbModeInfo.AppendLine("Descent to " + descentTargetAlt.ToString() + " meters");
                    _program.Echo("Orbital Descent to " + descentTargetAlt.ToString() + " meters");
                }

                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                double velocityShip = shipController.GetShipSpeed();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dLength = vNG.Length();
                double dGravity = dLength / 9.81; // What the UI shows

                double alt = 0;
                double distanceToTravel = 0;


                if (dGravity > 0)
                {
                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out alt);

                    double elevation = 0;

                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
                    //                    StatusLog("Elevation: " + elevation.ToString("N0") + " Meters", textPanelReport);
                    sbNotices.AppendLine("Elevation: " + elevation.ToString("N0") + " Meters");
                    _program.Echo("Elevation: " + elevation.ToString("N0") + " Meters");
                    distanceToTravel = alt;
                }
                if(bValidTargetLanding)
                {
                    distanceToTravel = (shipController.CenterOfMass - TargetLanding).Length();
                }
                sbNotices.AppendLine("Velocity=" + velocityShip.ToString("0.00"));
                _program.Echo("Descent Mode:" + iState.ToString());

                if (_landingGears.anyGearReadyToLock())
                {
                    _landingGears.GearsLock();
                }
                double progress = 0;
                if (velocityShip <= 0) progress = 0;
                else if (velocityShip > MaxDescentMps) progress = 100;
                else progress = ((velocityShip - 0) / (MaxDescentMps - 0));
                /*
                string sProgress = progressBar(progress);
                StatusLog("V:" + sProgress, textPanelReport);

                if (batteryPercentage >= 0) StatusLog("B:" + progressBar(batteryPercentage), textPanelReport);
                if (oxyPercent >= 0) StatusLog("O:" + progressBar(oxyPercent * 100), textPanelReport);
                if (hydroPercent >= 0) StatusLog("H:" + progressBar(hydroPercent * 100), textPanelReport);
                */
                /*
                            string sOrbitalUpDirection = "";
                            if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                                sOrbitalUpDirection = "rocket";
                                */
                IMyShipController imsc = shipController as IMyShipController;
                if (imsc != null && imsc.DampenersOverride)
                {
                    //                   StatusLog("DampenersOverride ON", textPanelReport);

                    sbNotices.AppendLine("DampenersOverride ON");
                    _program.Echo("DampenersOverride ON");
                }
                else
                {
                    //                   StatusLog("DampenersOverride OFF", textPanelReport);
                    sbNotices.AppendLine("DampenersOverride OFF");
                    _program.Echo("DampenersOverride OFF");
                }

                if (_connectors.AnyConnectorIsConnected())
                {
                    _wicoControl.SetMode(WicoControl.MODE_LAUNCHPREP);// setMode(MODE_IDLE);
                    _program.ErrorLog("Descent: Forcing Launch Prep due to connector connected");
                    return;
                }
                if (_connectors.AnyConnectorIsLocked())
                {
                    _connectors.ConnectAnyConnectors(true);
                    _landingGears.GearsLock(true);
                    //                blockApplyAction(gearList, "Lock");
                }
                if (iState == 0 || thrustOrbitalUpList.Count < 1)
                {
                    MyShipMass myMass;
                    myMass = shipController.CalculateShipMass();

                    PhysicalMass = myMass.PhysicalMass;

                    _wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                    Vector3D vNGN = vNG;
                    Matrix or1;
                    if (vNG == Vector3D.Zero)
                    {
                        vNGN=shipController.WorldMatrix.Forward; 
                    }

                    vNGN.Normalize();
                    _wicoThrusters.GetBestThrusters(vNGN,
                        thrustForwardList, thrustBackwardList,
                        thrustDownList, thrustUpList,
                        thrustLeftList, thrustRightList,
                        out thrustOrbitalUpList, out thrustOrbitalDownList
                        );
                    shipController.Orientation.GetMatrix(out or1);
                    vBestThrustOrientation = or1.Forward; // start out aiming at whatever the ship is aiming at..
//                    vBestThrustOrientation = shipController.WorldMatrix.Forward;
                }
                //               calculateBestGravityThrust();
                /*
                if (thrustStage1UpList.Count < 1)
                {  // one-time init.
                    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                    {
                        thrustStage1UpList = thrustForwardList;
                        thrustStage1DownList = thrustBackwardList;

                        cameraStage1LandingList = cameraBackwardList;
                    }
                    else
                    {
                        thrustStage1UpList = thrustUpList;
                        thrustStage1DownList = thrustDownList;

                        cameraStage1LandingList = cameraDownList;
                    }
                }
                */
                ////
                if (dGravity > 0)
                {
                    float fMPS = _wicoControl.fMaxWorldMps;
                    if (velocityShip > fMPS) fMPS = (float)velocityShip;

                    retroStartDistance = (int)_wicoThrusters.calculateStoppingDistance(shipController, thrustOrbitalUpList, fMPS, dGravity);
                    _program.Echo("dGravity: " + dGravity.ToString("0.00"));
                    sbNotices.AppendLine("Stopping Distance=" + retroStartDistance.ToString());
                    _program.Echo("Stopping Distance=" + retroStartDistance.ToString());

                    if (retroStartDistance < 0) _program.Echo("WARNING: CRASH!!!");

                    //                    startReverseAlt = Math.Max(retroStartAlt * 5, minAltRotate);
                    //                    _program.Echo("calc retroStartAlt=" + retroStartAlt.ToString());

                    retroStartDistance += (int)((_wicoBlockMaster.HeightInMeters() + 1)); // add calc point of height for altitude.. NOTE 'height' is not necessarily correct..
                                                                                                //		retroStartAlt += (int)fMaxMps; // one second of speed (1s timer delay)

                    if (iMode == WicoControl.MODE_DESCENT)
                        retroStartDistance += descentTargetAlt;

                    _program.Echo("adj retroStartAlt=" + retroStartDistance.ToString());
                }
                //               double finalMaxStop = _wicoThrusters.calculateStoppingDistance(myMass.PhysicalMass, thrustOrbitalUpList, _wicoControl.fMaxWorldMps, 1.0);
                //                _program.Echo("Final StoppingD=" + finalMaxStop.ToString("0"));
                _wicoControl.WantMedium();

                if (iState == 0)
                {
                    // if finalMaxStop< 0, then we do not have enough thrust to land safely.
                    // check for parachutes. if finalParachuteTerminal<10 we are good.  10-20 =dangerous.  >20=nogo

                    _program.Echo("Init State");
                    MaxDescentMps = _program._CustomDataIni.Get(sOrbitalSection, "DescentSpeedMax").ToDouble(MaxDescentMps);
                    //powerDownThrusters(thrustAllList,thrustAll,true);
                    if (dGravity > 0)
                    { // we are starting in gravity
                      //                       if (alt < (startReverseAlt * 1.5))
                        { // just do a landing
                            _wicoControl.SetState(40);// current_state = 40;
                        }
                        /*
                        else
                        {
                            current_state = 10;
                        }
                        */
                    }
                    else
                    {
                        if (iState!=80 && imsc != null && imsc.DampenersOverride)
                            imsc.DampenersOverride = false;

                        _connectors.ConnectAnyConnectors(false, true);
                        if (!bValidOrbitalTransfer)
                        {
                            _wicoControl.SetState(10);
                        }
                        else
                        {
                            _wicoControl.SetState(500);
                        }
                    }
                    _wicoControl.WantFast();
                }
                if (iState == 10)
                {
                    sbNotices.AppendLine("Dampeners to on. Aim toward target");
                    _program.Echo("Dampeners to on. Aim toward target");
                    //		bOverTarget=false; 
                    //_wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, false);
                    _wicoControl.WantFast();
                    if (bValidTargetLanding)
                    {
                        _wicoControl.SetState(11);
                    }
                    else _wicoControl.SetState(20);
                }
                if (iState == 11)
                {
                    _wicoControl.WantFast();
                    if(bValidTargetLanding)
                    {
                        Vector3D vAlign = TargetLanding - shipController.GetPosition();
                        bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vAlign);
                        if(bAimed)
                            _wicoControl.SetState(20);

                    }
                    else
                        _wicoControl.SetState(20);
                }
                if (iState == 20)
                {
                    if (imsc != null && !imsc.DampenersOverride)
                        imsc.DampenersOverride = true;

                    // push forward (towards) planet
                    if (dGravity <= 0 || velocityShip < (MaxDescentMps * .8))
                        _wicoThrusters.powerUpThrusters(thrustForwardList, 5);
                    else _wicoThrusters.powerDownThrusters(thrustForwardList);
                    _wicoThrusters.powerDownThrusters(thrustBackwardList, WicoThrusters.thrustAll, true);
                    if (dGravity > 0)
                        _wicoControl.SetState(30);// current_state = 30;
                    return;
                }
                if (iState == 21)
                {
                    //                    StatusLog("Alignment", textPanelReport);
                    _wicoControl.SetState(22);//current_state = 22;
                    return; // give at least one tick of dampeners 
                }
                if (iState == 22)
                {
                    //                    StatusLog("Alignment", textPanelReport);
                    _wicoControl.SetState(23);//current_state = 23;
                    return; // give at least one tick of dampeners 
                }
                if (iState == 23)
                {
                    //                    StatusLog("Alignment", textPanelReport);
                    _wicoControl.SetState(30);
                    return; // give at least one tick of dampeners 
                }
                if (iState == 30)
                {
                    CheckAttitudeChange(true);
                    _wicoThrusters.powerDownThrusters();
                    _wicoThrusters.powerDownThrusters(thrustBackwardList, WicoThrusters.thrustAll, true);

                    if (imsc != null && imsc.DampenersOverride)
                        imsc.DampenersOverride = false;
                    _wicoControl.SetState(40);
                }
                if (iState == 40)
                {
                    sbModeInfo.AppendLine("Free Fall");
                    _program.Echo("Free Fall");
                    if (dGravity > 0)
                    {
                        CheckAttitudeChange(true);
                        double dG = Math.Max(1.0,dGravity); // TODO: assumes earthlike
                       
                        double finalMaxStop = _wicoThrusters.calculateStoppingDistance(PhysicalMass, thrustOrbitalUpList, MaxDescentMps, dG);

                        if (finalMaxStop < 0) // thrusters cannot save us... (but if atmo, then this could change when we get lower)
                        {
                            double finalParachuteTerminal = _program.wicoParachutes.CalculateTerminalVelocity(PhysicalMass, _wicoBlockMaster.gridsize, 9.81, 0.85f);
                            _program.Echo("FParachute V: " + finalParachuteTerminal.ToString("0.00"));
                            if (finalParachuteTerminal < 10f)
                            {
                                // parachute landing
                                _wicoControl.SetState(300);
                            }
                            else _wicoControl.SetState(60); // try thrusters anyway
                        }
                        else _wicoControl.SetState(60);

                        if(velocityShip>MaxDescentMps)
                        {
                            _wicoControl.SetState(80);
                            _wicoControl.WantFast();
                            return;
                        }
                    }
                    if (imsc != null && imsc.DampenersOverride)
                        imsc.DampenersOverride = false;
                    //                    blockApplyAction(shipOrientationBlock, "DampenersOverride");

                    //                    if (alt < startReverseAlt)
                    {
                        //                        _wicoControl.SetState(60);//current_state = 60;
                    }
                    /*
                    else
                    {
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList);
 //                       StatusLog("Waiting for reverse altitude: " + startReverseAlt.ToString("N0") + " meters", textPanelReport);

                        if (alt > 44000 && alt < 45000)
                            _wicoControl.SetState(10);//current_state = 10; // re-align 
                        else if (alt > 34000 && alt < 35000)
                            _wicoControl.SetState(10);//current_state = 10; // re-align 
                        else if (alt > 24000 && alt < 25000)
                            _wicoControl.SetState(10);//current_state = 10; // re-align 
                        else if (alt > 14000 && alt < 15000)
                            _wicoControl.SetState(10);//current_state = 10; // re-align 
                    }
                    */
                }

                if (iState == 60)
                {
                    if (dGravity <= 0)
                    {
                        _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);//setMode(MODE_IDLE);
                        return;
                    }
                    CheckAttitudeChange(true);

                    if (imsc != null && imsc.DampenersOverride)
                        imsc.DampenersOverride = false;

                    // todo: Use target location instead of gravity
                    Vector3D vAlign = vNG;
                    if (bValidTargetLanding) vAlign = TargetLanding - shipController.GetPosition();

                    _gyros.AlignGyros(vBestThrustOrientation, vAlign);
                    _wicoControl.WantFast();//bWantFast = true;
                    _wicoControl.SetState(61);//current_state = 61;
                    return;
                }

                if (iState == 61)
                {  // we are rotating ship to gravity..

                   // TODO: Use target location instead of gravity
                   //TODO: if way out of alignment; use side thrusters
                   // TODO: use beamrider ?when closer..

                    Vector3D vAlign = vNG;
                    if(bValidTargetLanding) vAlign= TargetLanding - shipController.GetPosition();
                    if (bValidTargetLanding && bValidOrbitalTransfer)
                    {
                        Vector3D vBoreEnd = (TargetLanding - OrbitalTransferPoint);
                        Vector3D vPosition = shipController.CenterOfMass;

                        Vector3D vAimEnd = (TargetLanding - vPosition);
                        Vector3D vRejectEnd = _gyros.VectorRejection(vBoreEnd, vAimEnd);

                        Vector3D vCorrectedAim = (TargetLanding - vRejectEnd * 2) - vPosition;
                        vAlign = vCorrectedAim;
                    }
                    bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vAlign);

                    if ( bAimed
                    || distanceToTravel < retroStartDistance)
                    {
                        _wicoControl.SetState(70);
                    }
                    _wicoControl.WantFast();
                }
                if (iState == 70)
                {
                    sbNotices.AppendLine("Waiting for range for retro-thrust:" + retroStartDistance.ToString("N0") + " meters");
                    _program.Echo("Waiting for range for retro-thrust:" + retroStartDistance.ToString("N0") + " meters");

                    if (CheckAttitudeChange())
                    {
                        _wicoControl.SetState(61);
                        _program.Echo("attitude change");
                        _wicoControl.WantFast();
                    }

                    if (velocityShip > MaxDescentMps)
                    {
                        // TODO: use hoverthrust instead of just turning on thrusters full blast.
                        _wicoThrusters.powerDownThrusters();
                        _wicoControl.SetState(80);
                        _wicoControl.WantFast();
                        return;
                    }
                    // TODO: Align to target if known.
                    // TODO: if out of alignment and target, then use side thrusters to correct angle.
                    Vector3D vAlign = vNG;
                    if (bValidTargetLanding) vAlign = TargetLanding - shipController.CenterOfMass;
                    /*
                    if (bValidTargetLanding && bValidOrbitalTransfer)
                    {
                        _program.Echo("Target Align");
                        Vector3D vPosition = shipController.CenterOfMass;
                        Vector3D vBoreEnd = (TargetLanding - vPosition);
                        //                       Vector3D vBoreEnd = (TargetLanding - OrbitalTransferPoint);
//                        Vector3D vForward = vPosition + vBestThrustOrientation * vBoreEnd.Length();
                        Vector3D vForward = vPosition + vNG * vBoreEnd.Length();

//                        Vector3D vAimEnd = (vForward - vPosition);
                        Vector3D vMovement = shipController.GetShipVelocities().LinearVelocity;

                        // if we are going pretty slow, use gravity instead.
                        if (velocityShip < MaxDescentMps * .5)
                            vMovement = vNG;

//                        Vector3D vRejectEnd = _gyros.VectorRejection(vBoreEnd, vAimEnd);
                        Vector3D vRejectEnd = _gyros.VectorRejection(vBoreEnd, vMovement);
                        _program.Echo("Reject=" + vRejectEnd.ToString());

                        Vector3D vCorrectAimPoint = TargetLanding - vRejectEnd * 2;
                        _wicoCameras.CameraBackScan(vCorrectAimPoint);
                        Vector3D vCorrectedAim = vCorrectAimPoint - vPosition;
                        vAlign = vCorrectedAim;
                    }
                    */
                    bool bAligned=_gyros.AlignGyros(vBestThrustOrientation, vAlign); 
                    if (bAligned)
                    {
                        _program.Echo("Aligned");
                        /*
                        if (imsc != null && imsc.DampenersOverride)
                            imsc.DampenersOverride = false;
                        */
                        if (imsc != null && !imsc.DampenersOverride)
                        {
                            imsc.DampenersOverride = true;
                        }


                        _wicoControl.WantMedium();// bWantMedium = true;
                        double scandistance = alt;
                        if (scandistance > retroStartDistance)
                            scandistance = retroStartDistance;
                        /*
                                                if (doCameraScan(cameraOrbitalLandingList, scandistance * 2)) // scan down 2x current alt
                                                {
                                                    // we are able to do a scan
                                                    if (!lastDetectedInfo.IsEmpty())
                                                    { // we got something
                                                        double distance = Vector3D.Distance(shipOrientationBlock.GetPosition(), lastDetectedInfo.HitPosition.Value);
                                                        if (distance < alt)
                                                        { // try to land on found thing below us.
                                                            _program.Echo("Scan found:" + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below");
                                                            StatusLog("Landing on: " + lastDetectedInfo.Name + " " + distance.ToString("N0") + "m below", textPanelReport);

                                                            alt = distance;
                                                        }
                                                    }
                                                }
                                                */
                        _wicoThrusters.powerDownThrusters();
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustAll, true);
                    }
                    else
                    {
                        _program.Echo("NOT aligned");
                        // TODO: if way off, use side thrusters
                        if (imsc != null && !imsc.DampenersOverride)
                            imsc.DampenersOverride = true;
                        _wicoControl.WantFast();
                        _wicoThrusters.powerDownThrusters();
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustAll, true);
                    }

                    if (distanceToTravel < (retroStartDistance + MaxDescentMps * 2)) _wicoControl.WantFast();// bWantFast = true;

                    if ((distanceToTravel) < retroStartDistance)
                    {
                        // we need to stop NOW!
                        if (imsc != null && !imsc.DampenersOverride)
                        {
                            imsc.DampenersOverride = true;
                        }
                        _wicoThrusters.powerDownThrusters();
                        _wicoControl.SetState(90);
                        _timers.TimerTriggers("[Landing]");
                    }
                }
                if (iState == 80)
                {
                    // TODO: use hoverthrust instead of just turning on thrusters full blast.
                    imsc.DampenersOverride = true; // allow dampeners
                    sbModeInfo.AppendLine("Decrease descent speed");
                    Vector3D vAlign = vNG;
                    if (bValidTargetLanding) vAlign = TargetLanding - shipController.GetPosition();

                    bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vAlign);

                    if (velocityShip < (MaxDescentMps * .95))
                    {
                        CheckAttitudeChange(true);
                        _wicoThrusters.powerDownThrusters();
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustAll, true);
                        imsc.DampenersOverride = false;
                        _wicoControl.SetState(70);
                        _wicoControl.WantFast();
                    }
                    return;
                }
                /*
                double roll = 0;
                string s;
                if (bValidTarget)
                {
                    roll = CalculateRoll(vTarget, shipOrientationBlock);
                    s = "Roll=" + roll.ToString("0.00");
                    _program.Echo(s);
                    StatusLog(s, textPanelReport);
                }
                */
                if (iState == 90)
                {
                    sbModeInfo.AppendLine("RETRO! Waiting for ship to slow");
                    _program.Echo("RETRO!  Hope we make it!");
                    if (velocityShip < 1)
                    {
                        if (iMode == WicoControl.MODE_DESCENT)
                            _wicoControl.SetState(100);
                        else
                            _wicoControl.SetState(200);
                    }
                    Vector3D vAlign = vNG;
//                    if (bValidTargetLanding) vAlign = TargetLanding - shipController.GetPosition();
                    bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vAlign);
                    if (bAimed)
                    {
                        _wicoControl.WantMedium();
                    }
                    else _wicoControl.WantFast();
                }

                if (iState == 100)
                {
                    _wicoThrusters.powerDownThrusters();
                    _gyros.gyrosOff();

                    _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                    _timers.TimerTriggers("[Landed]");
                }
                if (iState == 200)
                {
                    // final descent to land
                    _wicoControl.WantFast();

                    _wicoThrusters.CalculateHoverThrust(thrustOrbitalUpList,
                        out fOrbitalAtmoPower, out fOrbitalHydroPower, out fOrbitalIonPower
                        );
                    float fTotalPower = fOrbitalAtmoPower + fOrbitalHydroPower + fOrbitalIonPower;
                    if (distanceToTravel > 300 && fTotalPower < 50)
                    {
                        _wicoControl.SetState(210);
                    }
                    else if (distanceToTravel > 100 && fTotalPower < 70)
                    {
                        _wicoControl.SetState(220);

                    }
                    else if (distanceToTravel > 20 && fTotalPower < 80)
                    {
                        _wicoControl.SetState(230);
                    }
                    else _wicoControl.SetState(240);
                }
                if (iState == 210)
                {
                    if (!_gyros.AlignGyros(vBestThrustOrientation, vNG))
                        _wicoControl.WantFast();
                    if (velocityShip > 55)
                    {
                        _wicoThrusters.powerDownThrusters(); // slow down
                    }
                    else
                    {
                        _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalAtmoPower * 0.50), WicoThrusters.thrustatmo);
                        _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalHydroPower * 0.50), WicoThrusters.thrusthydro);
                        _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalIonPower * 0.50), WicoThrusters.thrustion);
                    }

                }
                if (iState == 220)
                {
                    if (!_gyros.AlignGyros(vBestThrustOrientation, vNG))
                    _wicoControl.WantFast();

                    if (velocityShip > 20) 
                    {
                        _wicoThrusters.powerDownThrusters(thrustOrbitalDownList);
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList);
                    }
                    else
                    {
                        _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalAtmoPower * 0.60), WicoThrusters.thrustatmo);
                        _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalHydroPower * 0.60), WicoThrusters.thrusthydro);
                        _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalIonPower * 0.60), WicoThrusters.thrustion);
                    }
                }
                if (iState == 230)
                {
                    if (!_gyros.AlignGyros(vBestThrustOrientation, vNG))
                        _wicoControl.WantFast();

                    if (velocityShip > 15)
                    {
                        //                       Echo("a20:1");
                        // too fast or wait for landing mode
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList);
                    }
                    else if (velocityShip > 5)
                    {
                        if (fOrbitalAtmoPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustatmo, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalAtmoPower * 0.99), WicoThrusters.thrustatmo);
                        if (fOrbitalHydroPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalHydroPower * 0.99), WicoThrusters.thrusthydro);

                        if (fOrbitalIonPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalIonPower * 0.99), WicoThrusters.thrustion);
                    }
                    else
                    {
                        if (fOrbitalAtmoPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustatmo, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalAtmoPower * 0.85), WicoThrusters.thrustatmo);
                        if (fOrbitalHydroPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalHydroPower * 0.85), WicoThrusters.thrusthydro);

                        if (fOrbitalIonPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalIonPower * 0.85), WicoThrusters.thrustion);
                    }
                }
                if (iState == 240)
                {
                    if (! _gyros.AlignGyros(vBestThrustOrientation, vNG))
                        _wicoControl.WantFast();

                    // we are doing blind landing; keep going.
                    if (velocityShip > 3)
                    {
                        _wicoThrusters.powerDownThrusters(thrustOrbitalUpList);
                    }
                    else if (velocityShip > 2)
                    {
                        if (fOrbitalAtmoPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustatmo, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalAtmoPower * 0.99), WicoThrusters.thrustatmo);
                        if (fOrbitalHydroPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalHydroPower * 0.99), WicoThrusters.thrusthydro);

                        if (fOrbitalIonPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalIonPower * 0.99), WicoThrusters.thrustion);

                        //						_wicoThrusters.powerDownThrusters(thrustStage1UpList);
                    }
                    else
                    {
                        if (fOrbitalAtmoPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustatmo, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalAtmoPower * 0.85), WicoThrusters.thrustatmo);
                        if (fOrbitalHydroPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrusthydro, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalHydroPower * 0.85), WicoThrusters.thrusthydro);

                        if (fOrbitalIonPower <= 0)
                            _wicoThrusters.powerDownThrusters(thrustOrbitalUpList, WicoThrusters.thrustion, true);
                        else
                            _wicoThrusters.powerUpThrusters(thrustOrbitalUpList, (float)(fOrbitalIonPower * 0.85), WicoThrusters.thrustion);
                    }
                    if (_landingGears.AnyGearIsLocked())
                    {
                        _wicoThrusters.powerDownThrusters(WicoThrusters.thrustAll, true);// turn off all thrusters
                        _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);// setMode(MODE_IDLE); // we have done our job.  pass it on to somebody else..
                    }
                }
                if (iState == 300)
                {
                    vBestThrustOrientation = -_program.wicoParachutes.ChuteOrientation();
                    _wicoControl.SetState(310);

                    _wicoControl.WantFast();
                }
                if (iState == 310)
                {
                    sbNotices.AppendLine("Waiting for parachute height");
                    _program.Echo("Waiting for parachute height");
                    bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG);
                    //                    bool bAligned = _gyros.AlignGyros(vBestThrustOrientation, vNG, shipController);
                    if (!bAligned)
                        _wicoControl.WantFast();
                    // parachute landing
                    // TODO: calculate parachute deploy height
                    double parachuteTerminal = _program.wicoParachutes.CalculateTerminalVelocity(PhysicalMass, _wicoBlockMaster.gridsize, dLength);
                    _program.Echo("CParachute V: " + parachuteTerminal.ToString("0.00"));
                    if (parachuteTerminal < 10 || distanceToTravel < 200)
                    {
                        _program.wicoParachutes.OpenChutes();
                        _wicoControl.SetState(200); // attempt powered landing
                    }

                }
                if(iState == 500)
                {
                    // check going to orbital transfer point
                    // TODO: Use NAV if installed
                    // TODO: calculate path to take us outside of gravity of planet.
                    if (bValidOrbitalTransfer)
                    {
                        double distancesq = Vector3D.DistanceSquared(OrbitalTransferPoint, shipController.GetPosition());
                        if (distancesq < 10 * 10)
                            _wicoControl.SetState(10);
                        else
                        {
                            _wicoControl.SetState(510);
                        }
                    }
                    else _wicoControl.SetState(10);

                }
                if (iState == 510)
                {
                    // go to orbital transfer location
                    if (!bValidOrbitalTransfer)
                        _wicoControl.SetState(10); // we shouldn't be here

                    double distancesq = Vector3D.DistanceSquared(OrbitalTransferPoint, shipController.GetPosition());
                    if (distancesq < 10 * 10 || dGravity>0.01)
                    {
                        _program.ResetMotion();
                        _wicoThrusters.powerDownThrusters();
                        _gyros.gyrosOff();
                        shipController.DampenersOverride = true;

                        _wicoControl.SetState(10);
                        return;
                    }
                    Vector3D vAlign = OrbitalTransferPoint - shipController.GetPosition();
                    bool bAimed = _gyros.AlignGyros(vBestThrustOrientation, vAlign);
                    if(bAimed)
                    {
                        // TODO: target MPS based on distance.
                        if (velocityShip< (MaxDescentMps*0.8))
                        {

                            // push
                            shipController.DampenersOverride = true;
                            _wicoThrusters.powerUpThrusters(thrustForwardList);
                        }
                        else
                        {
                            // coast
                            shipController.DampenersOverride = false;
                            _wicoThrusters.powerDownThrusters();
                            _wicoThrusters.powerDownThrusters(thrustBackwardList, WicoThrusters.thrustAll, true);
                        }
                    }
                    else
                    {
                        shipController.DampenersOverride = true;
                    }
                }

            }

            void increasePower(double dGravity, double alt)
            {
                double dAtmoEff = _wicoThrusters.AtmoEffectiveness();
                /*
                Echo("atmoeff=" + dAtmoEff.ToString());
                Echo("hydroThrustCount=" + hydroThrustCount.ToString());
                Echo("fHydroPower=" + fHydroPower.ToString());
                Echo("fAtmoPower=" + fAtmoPower.ToString());
                Echo("fIonPower=" + fIonPower.ToString());
                */
                if (dGravity > .5 && (!bHasAtmo || dAtmoEff > 0.10))
                //                if (dGravity > .5 && alt < dAtmoCrossOver)
                {
                    if (fOrbitalAtmoPower < 100 && bHasAtmo)
                        fOrbitalAtmoPower += 5;
                    else if (fOrbitalHydroPower == 0 && fOrbitalIonPower > 0)
                    { // we are using ion already...
                        if (fOrbitalIonPower < 100 && bHasIon)
                            fOrbitalIonPower += 5;
                        else
                            fOrbitalHydroPower += 5;
                    }
                    else if (fOrbitalIonPower < 100 && bHasIon)
                        fOrbitalIonPower += 5;
                    else if (fOrbitalHydroPower < 100 && bHasHydro)
                    {
                        // fAtmoPower=100;
                        fOrbitalHydroPower += 5;
                    }
                    else // no power left to give, captain!
                    {
                        //                        StatusLog("Not Enough Thrust!", textPanelReport);
                        //                        Echo("Not Enough Thrust!");
                    }
                }
                else if (dGravity > .5 || dAtmoEff < 0.10)
                {
                    if (fOrbitalIonPower < fOrbitalAtmoPower && bHasAtmo && bHasIon)
                    {
                        float f = fOrbitalIonPower;
                        fOrbitalIonPower = fOrbitalAtmoPower;
                        fOrbitalAtmoPower = f;
                    }
                    if (fOrbitalIonPower < 100 && bHasIon)
                        fOrbitalIonPower += 10;
                    else if (fOrbitalHydroPower < 100 && bHasHydro)
                    {
                        fOrbitalHydroPower += 5;
                    }
                    else if (dAtmoEff > 0.10 && fOrbitalAtmoPower < 100 && bHasAtmo)
                        fOrbitalAtmoPower += 10;
                    else if (dAtmoEff > 0.10 && bHasAtmo)
                        fOrbitalAtmoPower -= 5; // we may be sucking power from ion
                    else // no power left to give, captain!
                    {
                        //                       StatusLog("Not Enough Thrust!", textPanelReport);
                        //                        Echo("Not Enough Thrust!");
                    }
                }
                else if (dGravity > .01)
                {
                    if (fOrbitalIonPower < 100 && bHasIon)
                        fOrbitalIonPower += 15;
                    else if (fOrbitalHydroPower < 100 && bHasHydro)
                    {
                        fOrbitalHydroPower += 5;
                    }
                    else if (dAtmoEff > 0.10 && fOrbitalAtmoPower < 100 && bHasAtmo)
                        fOrbitalAtmoPower += 10;
                    else // no power left to give, captain!
                    {
                        //                        StatusLog("Not Enough Thrust!", textPanelReport);
                        //                        Echo("Not Enough Thrust!");
                    }

                }

                if (fOrbitalIonPower > 100) fOrbitalIonPower = 100;
                if (fOrbitalAtmoPower > 100) fOrbitalAtmoPower = 100;
                if (fOrbitalAtmoPower < 0) fOrbitalAtmoPower = 0;
                if (fOrbitalHydroPower > 100) fOrbitalHydroPower = 100;

            }

            void decreasePower(double dGravity, double alt)
            {
                if (dGravity > .85 && _wicoThrusters.AtmoEffectiveness() > 0.10)
                {
                    if (fOrbitalHydroPower > 0)
                    {
                        fOrbitalHydroPower -= 5;
                    }
                    else if (fOrbitalIonPower > 0)
                        fOrbitalIonPower -= 5;
                    else if (fOrbitalAtmoPower > 10)
                        fOrbitalAtmoPower -= 5;
                }
                else if (dGravity > .3)
                {
                    if (fOrbitalAtmoPower > 0)
                        fOrbitalAtmoPower -= 10;
                    else if (fOrbitalHydroPower > 0)
                    {
                        fOrbitalHydroPower -= 5;
                    }
                    else if (fOrbitalIonPower > 10)
                        fOrbitalIonPower -= 5;

                }
                else if (dGravity > .01)
                {
                    if (fOrbitalAtmoPower > 0)
                        fOrbitalAtmoPower -= 5;
                    else if (fOrbitalHydroPower > 0)
                    {
                        fOrbitalHydroPower -= 5;
                    }
                    else if (fOrbitalIonPower > 10)
                        fOrbitalIonPower -= 5;
                }

                if (fOrbitalIonPower < 0) fOrbitalIonPower = 0;
                if (fOrbitalAtmoPower < 0) fOrbitalAtmoPower = 0;
                if (fOrbitalHydroPower < 0) fOrbitalHydroPower = 0;

            }

            bool CheckAttitudeChange(bool bForceCalc = false)
            {
                _program.Echo("CAC");
                List<IMyTerminalBlock> oldup = thrustOrbitalUpList;
                List<IMyTerminalBlock> olddown = thrustOrbitalDownList;

                _wicoThrusters.GetMaxScaledThrusters(
                    thrustForwardList, thrustBackwardList,
                    thrustDownList, thrustUpList,
                    thrustLeftList, thrustRightList,
                    out thrustOrbitalUpList, out thrustOrbitalDownList,
                    orbitalAtmoMult, orbitalIonMult, orbitalHydroMult
                    );

                if (thrustOrbitalUpList != oldup || bForceCalc) // something changed
                {
                    _program.Echo("CAC:Change in attitude needed");
                    _wicoThrusters.powerDownThrusters(olddown);
                    _wicoThrusters.powerDownThrusters(oldup);
                    _wicoThrusters.powerDownThrusters(thrustOrbitalDownList, WicoThrusters.thrustAll, true);

                    Matrix or1;
                    if (thrustOrbitalUpList.Count > 0)
                    {
                        //                        _program.Echo("Using up thrust[0]");
                        thrustOrbitalUpList[0].Orientation.GetMatrix(out or1);
                        vBestThrustOrientation = or1.Forward;
                        //                        vBestThrustOrientation = thrustOrbitalUpList[0].WorldMatrix.Forward;
                    }
                    else
                    {
                        //                        _program.Echo("No Up Thrust Found!");
                        IMyShipController shipcontroller = _wicoBlockMaster.GetMainController();
                        shipcontroller.Orientation.GetMatrix(out or1);
                        vBestThrustOrientation = or1.Forward;
                        //vBestThrustOrientation=shipcontroller.WorldMatrix.Forward;
                    }

                    return true;
                }
                //                _program.Echo("CheckAttitude:No Change");
                return false;


            }

        }
    }
}
