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
        #region MOM_ANNOUNCER
        const long lBaseTransmitWait = 15;

        long lBaseLastTransmit = lBaseTransmitWait + 5;

        void doMomAnnounce()
        {
            if (dockingInfo.Count > 0)
            {
                Echo("MOM: Last Transmit=" + lBaseLastTransmit.ToString());
                if (lBaseLastTransmit > lBaseTransmitWait)
                {
                    lBaseLastTransmit = 0;
                    antSend("WICO:MOM:" + gpsName("", gpsCenter.CubeGrid.CustomName) + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(((IMyShipController)gpsCenter).CenterOfMass));
                }
                else
                    lBaseLastTransmit++;
            }
        }
        #endregion

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
                        lBaseLastTransmit = lBaseTransmitWait + 5; // force us to transmit next tick 
                        bWantFast = true;
                    }
                    return false; // we processed it, but still pass it on to other modules
                }
            }
            return false;
        }

    }

}