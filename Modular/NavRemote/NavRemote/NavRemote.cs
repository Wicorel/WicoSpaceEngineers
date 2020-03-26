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
        public class NavRemote
        {


            Program thisProgram;

            public NavRemote(Program program)
            {
                thisProgram = program;

                // TODO: talk to NAV module at startup to see if it exists.
                // If it DOES NOT, then maybe use Keen autopilot?

//                wbm.AddLocalBlockHandler(BlockParseHandler);
//                wbm.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            public void NavReset()
            {
                thisProgram.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVRESET, "", TransmissionDistance.CurrentConstruct);
            }

            public void NavAddTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_NAVNEXTTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
                string data = NavCommon.NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                thisProgram.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVADDTARGET, data, TransmissionDistance.CurrentConstruct);
            }

            public void NavGoTarget(Vector3D vTarget, int modeArrival = WicoControl.MODE_ARRIVEDTARGET, int stateArrival = 0, double DistanceMin = 50, string TargetName = "", double maxSpeed = 9999, bool bGo = true)
            {
                string data = NavCommon.NAVSerializeCommand(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                thisProgram.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVADDTARGET, data, TransmissionDistance.CurrentConstruct);
                thisProgram.IGC.SendBroadcastMessage(NavCommon.WICOB_NAVSTART, "", TransmissionDistance.CurrentConstruct);
            }

        }
    }
}
