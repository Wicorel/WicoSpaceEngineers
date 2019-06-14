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
        double dBaseTransmitWait = 55; //seconds between active transmits

        double dBaseLastTransmit = -1;

        string sBaseSection = "BASE";
        void BaseInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sBaseSection, "BaseTransmitWait", ref dBaseTransmitWait, true);

        }

        void doBaseAnnounce(bool bForceAnnounce=false)
        {
            if (dockingInfo.Count > 0)
            {
                if (dBaseLastTransmit > dBaseTransmitWait || bForceAnnounce)
                {
                    dBaseLastTransmit = 0;
                    bool bJumpCapable = false;
                    string sname = Me.CubeGrid.CustomName;
                    Vector3D vPosition = antennaPosition();

                    if (shipOrientationBlock != null)
                    {
                        sname = shipOrientationBlock.CubeGrid.CustomName;
//                        vPosition = shipOrientationBlock.GetPosition();
                    }
//                    antSend("WICO:BASE:" + gpsName("", sname) + ":" + SaveFile.EntityId.ToString() +
                    antSend("BASE", gpsName("", sname) + ":" + SaveFile.EntityId.ToString() +
                        ":" + Vector3DToString(vPosition) + ":" + bJumpCapable.ToString());
                    //                   antSend("WICO:MOM:" + gpsName("", shipOrientationBlock.CubeGrid.CustomName) + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(((IMyShipController)shipOrientationBlock).CenterOfMass));
                }
                else
                {
                    if(dBaseLastTransmit<0)
                    {
                        // first-time init
                        dBaseLastTransmit = Me.EntityId % dBaseTransmitWait; // randomize initial send

                    }
                    dBaseLastTransmit+=Runtime.TimeSinceLastRun.TotalSeconds;
                    Echo("BASE: Last Transmit=" + dBaseLastTransmit.ToString());
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