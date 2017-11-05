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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const double dBaseTransmitWait = 15; //seconds between active transmits

        double dBaseLastTransmit = dBaseTransmitWait + 5;

        void doBaseAnnounce(bool bForceAnnounce=false)
        {
            if (dockingInfo.Count > 0)
            {
                Echo("BASE: Last Transmit=" + dBaseLastTransmit.ToString());
                if (dBaseLastTransmit > dBaseTransmitWait || bForceAnnounce)
                {
                    dBaseLastTransmit = 0;
                    bool bJumpCapable = false;

                    antSend("WICO:BASE:" + gpsName("", gpsCenter.CubeGrid.CustomName) + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(gpsCenter.GetPosition()) + ":" + bJumpCapable.ToString());
                    //                   antSend("WICO:MOM:" + gpsName("", gpsCenter.CubeGrid.CustomName) + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(((IMyShipController)gpsCenter).CenterOfMass));
                }
                else
                {
                    dBaseLastTransmit+=Runtime.TimeSinceLastRun.TotalSeconds;
                }
            }
        }

        bool processAnnounceMessage(string sReceivedMessage)
        {
            string[] aMessage = sReceivedMessage.Trim().Split(':');
            if (aMessage.Length > 1)
            {
                if (aMessage[0] != "WICO")
                {
                    Echo("not wico system message");
                    return false;
                }
                if (aMessage.Length > 2)
                {
                    if (aMessage[1] == "HELLO")
                    {
                        Echo("HELLO");
                        dBaseLastTransmit = dBaseTransmitWait + 5; // force us to transmit next tick 
                        bWantFast = true;
                    }
                    return false; // we processed it, but still pass it on to other modules
                }
            }
            return false;
        }

    }

}