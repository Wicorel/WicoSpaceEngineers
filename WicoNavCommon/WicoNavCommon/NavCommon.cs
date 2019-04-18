using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        const string WICOB_NAVADDTARGET = "WICOB_NAVADDTARGET";
        const string WICOB_NAVSTART = "WICOB_NAVSTART";
        const string WICOB_NAVRESET = "WICOB_NAVRESET";
        const string WICOB_NAVLAUNCH = "WICO_NAVLAUNCH";
        const string WICOB_NAVDOCK = "WICO_NAVDOCK";
        const string WICOB_NAVORBITALLAUNCH = "WICO_NAVORBITALLAUNCH";
        const string WICOB_NAVLAND = "WICO_NAVLAND";

        //        Vector3D vNavHome;
        //        bool bValidNavHome = false;

        // TODO: move into per-command
        bool bGoOption = false; // back compat for now.
        public enum CommandTypes { unknown, Waypoint, Orientation, ArrivalDistance, MaxSpeed, Launch, Land, Dock, OrbitalLaunch };

        public class WicoNavCommand
        {
            public Program pg;

            public CommandTypes Command = CommandTypes.unknown;
            public Vector3D vNavTarget;
            public bool bValidNavTarget = false;
//            public DateTime dtNavStartShip;

            public int NAVArrivalMode = MODE_ARRIVEDTARGET;
            public int NAVArrivalState = 0;

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
//                pg.sStartupError += "\nProcessCommand";
                switch(Command)
                {
                    case CommandTypes.Waypoint:
                        {
                            pg.vNavTarget = vNavTarget;
                            pg.bValidNavTarget = true;
                            pg.NAVArrivalMode = NAVArrivalMode;
                            pg.NAVArrivalState = NAVArrivalState;
                            pg.arrivalDistanceMin = arrivalDistanceMin;
                            pg.NAVTargetName = NAVTargetName;

                            pg.shipSpeedMax = shipSpeedMax;

                            pg.bGoOption = true;

//                            pg.sStartupError += "Going to:" + pg.NAVTargetName;
                            pg.setMode(MODE_GOINGTARGET);
//                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Orientation:
                        {
                            pg.vNavTarget = vNavTarget;
                            pg.bValidNavTarget = true;
                            pg.NAVArrivalMode = NAVArrivalMode;
                            pg.NAVArrivalState = NAVArrivalState;
//                            pg.arrivalDistanceMin = arrivalDistanceMin;
                            pg.NAVTargetName = NAVTargetName;

//                            pg.shipSpeedMax = shipSpeedMax;

                            pg.bGoOption = false;
                            pg.setMode(MODE_GOINGTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.MaxSpeed:
                        {
                            pg.shipSpeedMax = shipSpeedMax;
                            pg.setMode(MODE_NAVNEXTTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.ArrivalDistance:
                        {
                            pg.arrivalDistanceMin = arrivalDistanceMin;
                            pg.setMode(MODE_NAVNEXTTARGET);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Launch:
                        {
                            pg.setMode(MODE_LAUNCH);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.OrbitalLaunch:
                        {
                            pg.setMode(MODE_ORBITALLAUNCH);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Dock:
                        {
                            pg.setMode(MODE_DOCKING);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.Land:
                        {
                            pg.setMode(MODE_DESCENT);
                            //                            pg.current_state = 0; redundant with setmode
                        }
                        break;
                    case CommandTypes.unknown:
                        {
                            pg.Echo("Unknown Command");
                            pg.setMode(MODE_ATTENTION);
                            return true;
                        }
//                        break;
                }
                return false;
            }
        }

        List<WicoNavCommand> wicoNavCommands = new List<WicoNavCommand>();

        // Current entity
        Vector3D vNavTarget;// vTargetMine;
        bool bValidNavTarget = false;
        DateTime dtNavStartShip;

        /// <summary>
        /// Set maximum speed of ship. 
        /// Set this using S command for NAV
        /// </summary>
        double shipSpeedMax = 9999;

        /// <summary>
        /// the minimum distance to be from the target to be considered 'arrived'
        /// </summary>
        double arrivalDistanceMin = 50;

        int NAVArrivalMode = MODE_ARRIVEDTARGET;
        int NAVArrivalState = 0;

        string NAVTargetName = "";

        //        Vector3D vNavLaunch;
        //        bool bValidNavLaunch = false;
        //        Vector3D vNavHome;
        //        bool bValidNavHome = false;
        bool dTMDebug = false;
        bool dTMUseCameraCollision = true;
        bool dTMUseSensorCollision = true;
        bool NAVEmulateOld = false;
        bool AllowBlindNav = false;
        float NAVGravityMinElevation = -1;

        bool bNavBeaconDebug = false;


        string sNavSection = "NAV";

        void NavInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sNavSection, "DTMDebug", ref dTMDebug, true);
            iNIHolder.GetValue(sNavSection, "CameraCollision", ref dTMUseCameraCollision, true);
            iNIHolder.GetValue(sNavSection, "SensorCollision", ref dTMUseSensorCollision, true);
            iNIHolder.GetValue(sNavSection, "NAVEmulateOld", ref NAVEmulateOld, true);
            iNIHolder.GetValue(sNavSection, "NAVGravityMinElevation", ref NAVGravityMinElevation, true);
            iNIHolder.GetValue(sNavSection, "NavBeaconDebug", ref bNavBeaconDebug, true);
            iNIHolder.GetValue(sNavSection, "AllowBlindNav", ref AllowBlindNav, true);
            if (shipSpeedMax > fMaxWorldMps)
                shipSpeedMax = fMaxWorldMps;

        }

        void NavSerialize(INIHolder iNIHolder)
        {
            //            iNIHolder.SetValue(sNavSection, "vNavHome", vNavHome);
            //            iNIHolder.SetValue(sNavSection, "ValidNavHome", bValidNavHome);
            iNIHolder.SetValue(sNavSection, "vTarget", vNavTarget);
            iNIHolder.SetValue(sNavSection, "ValidNavTarget", bValidNavTarget);
            iNIHolder.SetValue(sNavSection, "TargetName", NAVTargetName);

            iNIHolder.SetValue(sNavSection, "dStartShip", dtNavStartShip);
            iNIHolder.SetValue(sNavSection, "shipSpeedMax", shipSpeedMax);
            iNIHolder.SetValue(sNavSection, "arrivalDistanceMin", arrivalDistanceMin);
            iNIHolder.SetValue(sNavSection, "NAVArrivalMode", NAVArrivalMode);
            iNIHolder.SetValue(sNavSection, "NAVArrivalState", NAVArrivalState);
        }

        void NavDeserialize(INIHolder iNIHolder)
        {
//            iNIHolder.GetValue(sNavSection, "vNavLaunch", ref vNavLaunch, true);
//            iNIHolder.GetValue(sNavSection, "ValidNavLaunch", ref bValidNavLaunch, true);
            iNIHolder.GetValue(sNavSection, "vTarget", ref vNavTarget, true);
            iNIHolder.GetValue(sNavSection, "ValidNavTarget", ref bValidNavTarget, true);
            iNIHolder.GetValue(sNavSection, "TargetName", ref NAVTargetName, true);

            iNIHolder.GetValue(sNavSection, "dStartShip", ref dtNavStartShip, true);
            iNIHolder.GetValue(sNavSection, "shipSpeedMax", ref shipSpeedMax, true);
            iNIHolder.GetValue(sNavSection, "arrivalDistanceMin", ref arrivalDistanceMin, true);
            iNIHolder.GetValue(sNavSection, "NAVArrivalMode", ref NAVArrivalMode, true);
            iNIHolder.GetValue(sNavSection, "NAVArrivalState", ref NAVArrivalState, true);
        }

        List<IMyBeacon> navDebugBeacons = new List<IMyBeacon>();
        void NavDebug(string str)
        {
            if(bNavBeaconDebug)
            {
                if (navDebugBeacons.Count < 1)
                    GridTerminalSystem.GetBlocksOfType(navDebugBeacons);
                foreach(var beacon in navDebugBeacons)
                {
                    beacon.CustomName = str;
                }
            }
        }

        void NavReset()
        {
            IGC.SendBroadcastMessage(WICOB_NAVRESET, "", TransmissionDistance.CurrentConstruct);
        }

        void NavAddTarget(Vector3D vTarget, int modeArrival = MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
        {
            string data = NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
            IGC.SendBroadcastMessage(WICOB_NAVADDTARGET, data, TransmissionDistance.CurrentConstruct);

        }

        void NavGoTarget(Vector3D vTarget, int modeArrival=MODE_ARRIVEDTARGET, int stateArrival=0, double DistanceMin=50, string TargetName="", double maxSpeed=9999, bool bGo=true)
        {
            string data = NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
            IGC.SendBroadcastMessage(WICOB_NAVSTART, data, TransmissionDistance.CurrentConstruct);
//            sStartupError += "Sent message=\n" + data;
        }

        void NavQueueLaunch()
        {
            IGC.SendBroadcastMessage(WICOB_NAVSTART, "", TransmissionDistance.CurrentConstruct);
        }

        string NAVSerializeCommand(Vector3D vTarget, int modeArrival = MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo=true)
        {
            string command = "";
            command += Vector3DToString(vTarget);
            command += "\n";
            command += modeArrival.ToString();
            command += "\n";
            command += stateArrival.ToString();
            command += "\n";
            command += DistanceMin.ToString();
            command += "\n";
            command += TargetName;
            command += "\n";
            command += maxSpeed.ToString();
            command += "\n";
            command += bGo.ToString();
            command += "\n";

            return command;
        }

        void NAVDeserializeCommand(string command, out Vector3D vTarget, out int modeArrival, out int stateArrival, out double DistanceMin, out string TargetName, out double maxSpeed, out bool bGo)
        {
            command = command.Trim();
            string[] strlines = command.Split('\n');
            string[] coordinates = strlines[0].Split(',');
            if (coordinates.Length < 3)
            {
                coordinates = strlines[0].Split(':');
            }
            double x, y, z;
            int iCoordinate = 0;
            bool xOk = double.TryParse(coordinates[iCoordinate++].Trim(), out x);
            bool yOk = double.TryParse(coordinates[iCoordinate++].Trim(), out y);
            bool zOk = double.TryParse(coordinates[iCoordinate++].Trim(), out z);
            if (!xOk || !yOk || !zOk)
            {
                //Echo("P:C");  
                Echo("Invalid Command:(" + strlines[0] + ")");
                //			shutdown(gyroList);

            }
            vTarget = new Vector3D(x, y, z);
            int.TryParse(strlines[1], out modeArrival);
            int.TryParse(strlines[2], out stateArrival);
            double.TryParse(strlines[3], out DistanceMin);
            TargetName = strlines[4];
            double.TryParse(strlines[5], out maxSpeed);
            bGo = true;
            if (strlines.Length > 5)
                bool.TryParse(strlines[6], out bGo);
        }


    }
}
