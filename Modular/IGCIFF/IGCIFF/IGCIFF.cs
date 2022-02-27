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
using VRage;

namespace IngameScript
{
    partial class Program
    {
        public class IFF
        {
            // https://www.reddit.com/r/spaceengineers/comments/k0sl87/automatic_rotor_turret_friendly_fire_checking/
            const string IGCIFFMessage = "IGC_IFF_MSG";

            const string IGCIFFTimer = "IGCIFFTIMER";

            readonly WicoIGC _wicoIGC;
            WicoElapsedTime _wicoElapsedTime;
            long _EntityId;
            Program _program;

            double AnnounceSeconds = 1;

            public IFF(Program program, WicoIGC wicoIGC, WicoElapsedTime wicoElapsedTime)
            {

                _wicoIGC = wicoIGC;
                _wicoElapsedTime = wicoElapsedTime;
                _EntityId = program.Me.CubeGrid.EntityId;
                _program = program;

                AnnounceSeconds = _program.CustomDataIni.Get(_program.OurName, "IFFAnnounceSeconds").ToDouble(AnnounceSeconds);
                _program.CustomDataIni.Set(_program.OurName, "IFFAnnounceSeconds", AnnounceSeconds);

                if (AnnounceSeconds > 0)
                {
                    wicoElapsedTime.AddTimer(IGCIFFTimer, AnnounceSeconds, ElapsedTimehandler);
                    wicoElapsedTime.StartTimer(IGCIFFTimer);
                }
            }
            public void ElapsedTimehandler(string s)
            {
                // our timer has expired
                if (s == IGCIFFTimer)
                {
                    Announce();
                }
            }
            public void AnnounceEnemy(long EntityID, Vector3D position, Double radius)
            {
                MyTuple<byte, long, Vector3D, double> msg;
                msg.Item1 = 1; // The relationship between you and the receiver. 0 is neutral, 1 is hostile, 2 is friendly, 4 is missile locked (for LAMP designation). You will want to set this to 2 for the turret slaver to listen to your message
                msg.Item2 = EntityID; // our grid entity ID
                msg.Item3 = position; // our position
                msg.Item4 = radius; // our radius
                _program.IGC.SendBroadcastMessage(IGCIFFMessage, msg);
            }
            public void Announce()
            { 
                MyTuple<byte, long, Vector3D, double> msg;
                msg.Item1 = 2; // The relationship between you and the receiver. 0 is neutral, 1 is hostile, 2 is friendly, 4 is missile locked (for LAMP designation). You will want to set this to 2 for the turret slaver to listen to your message
                msg.Item2 = _EntityId; // our grid entity ID
                msg.Item3 = _program.Me.CubeGrid.GetPosition(); // our position
                msg.Item4 = _program.Me.CubeGrid.WorldVolume.Radius; // our radius
                _program.IGC.SendBroadcastMessage(IGCIFFMessage, msg);
            }
        }
    }
}
