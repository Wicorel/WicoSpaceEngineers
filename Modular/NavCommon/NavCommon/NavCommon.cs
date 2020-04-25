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
        public class NavCommon
        {
            Program _program;

            public NavCommon(Program program)
            {
                _program = program;

                // TODO: talk to NAV module at startup to see if it exists.
                // If it DOES NOT, then maybe use Keen autopilot?

                //                wbm.AddLocalBlockHandler(BlockParseHandler);
                //                wbm.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            public const string WICOB_NAVADDTARGET = "WICOB_NAVADDTARGET";
            public const string WICOB_NAVSTART = "WICOB_NAVSTART";
            public const string WICOB_NAVRESET = "WICOB_NAVRESET";
            public const string WICOB_NAVSETMODE = "WICOB_NAVSETMODE";

            public static string NAVSerializeCommand(Vector3D vTarget, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
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

            public static void NAVDeserializeCommand(string command, out Vector3D vTarget, out int modeArrival, out int stateArrival, out double DistanceMin, out string TargetName, out double maxSpeed, out bool bGo)
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
  //                  Echo("Invalid Command:(" + strlines[0] + ")");
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
            public static string Vector3DToString(Vector3D v)
            {
                string s;
                s = v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00");
                return s;
            }

            public virtual void NavReset()
            {
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVRESET, "", TransmissionDistance.CurrentConstruct);
            }

            public virtual void NavAddTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
                string data = NavCommon.NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVADDTARGET, data, TransmissionDistance.CurrentConstruct);
            }

            public virtual void NavGoTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_ARRIVEDTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
//                _program.ErrorLog("NavCommon NavGoTarget");
                string data = NavCommon.NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVADDTARGET, data, TransmissionDistance.CurrentConstruct);
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVSTART, "", TransmissionDistance.CurrentConstruct);
            }
            public virtual void NavQueueMode(int theMode)
            {
//                _program.ErrorLog("NavCommon NavQueueMode");
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVSETMODE, theMode.ToString(), TransmissionDistance.CurrentConstruct);
            }



        }
    }
}
