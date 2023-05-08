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
            readonly Program _program;
            readonly WicoControl _wicoControl;
            readonly WicoIGC _wicoIGC;

            protected bool _bLocalNavAvailable = false;
            long NavLocalID = 0;

            public NavCommon(Program program, WicoControl wicoControl, WicoIGC wicoIGC, bool bAnnounce=true)
            {
                _program = program;
                _wicoControl = wicoControl;
                _wicoIGC = wicoIGC;

                // TODO: talk to NAV module at startup to see if it exists.
                // If it DOES NOT, then maybe use Keen autopilot?

                //                wbm.AddLocalBlockHandler(BlockParseHandler);
                //                wbm.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVPRESENT, IGCHandler);
                _wicoIGC.AddUnicastHandler(IGCHandler);

                // Request any local NAV to tell us it's here..
                if(bAnnounce) _program.IGC.SendBroadcastMessage(WICOB_NAVHEARTBEAT, "", TransmissionDistance.CurrentConstruct);
            }
            void IGCHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
                if (msg.Tag == NavCommon.WICOB_NAVPRESENT)
                {
                    if (msg.Data is string)
                    {
//                        if (_Debug) _program.ErrorLog(msg.Tag);
                        _bLocalNavAvailable = true;
                        NavLocalID = msg.Source;
                    }
                }
            }

            public const string WICOB_NAVADDTARGET = "WICOB_NAVADDTARGET";
            public const string WICOB_NAVIMMEDIATETARGET = "WICOB_NAVTARGET";
            public const string WICOB_NAVSTART = "WICOB_NAVSTART";
            public const string WICOB_NAVRESET = "WICOB_NAVRESET";
            public const string WICOB_NAVSETMODE = "WICOB_NAVSETMODE";

            public const string WICOB_NAVHEARTBEAT = "WICOB_NAVHEARTBEAT"; // is there a nav available?
            public const string WICOB_NAVPRESENT = "WICOB_NAVPRESENT"; // a nav is present

            readonly static StringBuilder sbNav = new StringBuilder(100);
            public static string NAVSerializeCommand(Vector3D vTarget, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
                sbNav.Clear();
                sbNav.AppendLine(Vector3DToString(vTarget));
                sbNav.AppendLine(modeArrival.ToString());
                sbNav.AppendLine(stateArrival.ToString());
                sbNav.AppendLine(DistanceMin.ToString());
                sbNav.AppendLine(TargetName);
                sbNav.AppendLine(maxSpeed.ToString());
                sbNav.AppendLine(bGo.ToString());
                return sbNav.ToString();
            }

            public static void NAVDeserializeCommand(string command, out Vector3D vTarget, out int modeArrival, out int stateArrival, out double DistanceMin, out string TargetName, out double maxSpeed, out bool bGo)
            {
                sbNav.Clear();
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
            // new format GPS: 1.196  
            // GPS: Wicorel #1:9984.6:60391.79:4806.48:#FF75C9F1:
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
                // TODO: support no local NAV and use remote control instead.
 //               _program.ErrorLog("NavCommon NavGoTarget");
                string data = NavCommon.NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVIMMEDIATETARGET, data, TransmissionDistance.CurrentConstruct);
            }
            public virtual void NavQueueMode(int theMode)
            {
                //                _program.ErrorLog("NavCommon NavQueueMode");
                _program.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVSETMODE, theMode.ToString(), TransmissionDistance.CurrentConstruct);
            }
            public virtual void NavStartNav()
            {
                _wicoControl.SetMode(WicoControl.MODE_STARTNAV);
            }

        }
    }
}
