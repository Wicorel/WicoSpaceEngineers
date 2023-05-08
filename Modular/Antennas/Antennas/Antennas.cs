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
        public class Antennas
        {
 //           bool CommunicationsStealth = false;

            bool bGotAntennaName = false;
            public string AntennaName;

            protected List<IMyRadioAntenna> antennaList = new List<IMyRadioAntenna>();
            protected List<IMyLaserAntenna> laserList = new List<IMyLaserAntenna>();

            protected List<IMyBeacon> beaconList = new List<IMyBeacon>();

            protected Program _program;
            protected WicoBlockMaster _wicoBlockMaster;

            public Antennas(Program program, WicoBlockMaster wicoBlockMaster)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyRadioAntenna)
                {
                    if (tb.CustomName.Contains("unused") || tb.CustomData.Contains("unused"))
                        return;
                    antennaList.Add(tb as IMyRadioAntenna);
                    if (!bGotAntennaName)
                    {
                        AntennaName = "Wico " + tb.CustomName.Split('!')[0].Trim();
                        bGotAntennaName = true;
                    }
                }
                if (tb is IMyLaserAntenna)
                {
                    laserList.Add(tb as IMyLaserAntenna);
                }
                if (tb is IMyBeacon)
                {
                    beaconList.Add(tb as IMyBeacon);
                }
            }
            void LocalGridChangedHandler()
            {
                antennaList.Clear();
                laserList.Clear();
                bGotAntennaName = false;
                AntennaName = "";
            }

            /// <summary>
            /// Set All antennas to lower power mode
            /// </summary>
            /// <param name="bAll">Ensures All, or just ones that have script attached are also Enabled</param>
            public void SetLowPower(bool bAll = false)
            {
                bool bFirst = true;
                foreach (var a in antennaList)
                {
                    a.Radius = 200;
                    if (bFirst || bAll)
                    {
                        bFirst = false;
                        a.Enabled = true;
                    }
                }
            }

            /// <summary>
            /// Set antenna radius (power) to the specfied radius.
            /// </summary>
            /// <param name="fRadius">radius in meters.  Default 200</param>
            /// <param name="bAll">Set all antennas (true) or just ones that have script attached (default) (false)</param>
            public void SetRadius(float fRadius = 200, bool bAll = false)
            {
                bool bFirst = true;
                foreach (var a1 in antennaList)
                {
                    //                    if (a1.AttachedProgrammableBlock > 0 || bAll)
                    if (bFirst || bAll)
                    {
                        bFirst = false;
                        a1.Radius = fRadius;
                        a1.Enabled = true;
                    }
                    if (!bAll) return;
                }
            }

            /// <summary>
            /// Returns position of the antenna that we are attached to
            /// </summary>
            /// <returns>position of the antenna block, or empty</returns>
            public Vector3D GetPosition()
            {
                foreach (var a1 in antennaList)
                {
                    // else any one will do
                    return a1.GetPosition();
                }
                Vector3D vNone = new Vector3D();
                return vNone;
            }

            /// <summary>
            /// Internal: desired range of antennas when transmitting.
            /// </summary>
            protected float fAntennaDesiredRange = float.MaxValue;

            /// <summary>
            /// Sets the desired max power of the antenna(s)
            /// </summary>
            /// <param name="bAll">Sets all the antennas.  Default to set only the ones that have script attached</param>
            /// <param name="desiredRange">Range. Default is max</param>
            public void SetMaxPower(bool bAll = false, float desiredRange = float.MaxValue)
            {
                //            if (antennaList==null || antennaList.Count < 1) antennaInit();
                if (desiredRange < 200) desiredRange = 200;
                fAntennaDesiredRange = desiredRange;

                // if silent mode
                // return;
                // else set range now
                SetDesiredPower(bAll);
            }

            public void SetDesiredPower(bool bAll = false)
            {
                bool bFirst = true;
                foreach (var a in antennaList)
                {
                    //                    if (a.AttachedProgrammableBlock > 0 || bAll)
                    if (bFirst || bAll)
                    {
                        bFirst = false;
                        float maxPower = a.GetMaximum<float>("Radius");
                        if (fAntennaDesiredRange < maxPower) maxPower = fAntennaDesiredRange;
                        a.Radius = maxPower;
                        a.Enabled = true;
                    }
                    if (!bAll) return;
                }
            }

            /// <summary>
            /// Returns the number of antennas available
            /// </summary>
            /// <returns></returns>
            public int AntennaCount()
            {
                return (antennaList.Count);
            }

            public void SetAnnouncement(string sMessage)
            {
                if(beaconList.Count>0)
                {
                    IMyBeacon beacon = beaconList[0];
                    beacon.Enabled = true;
                    beacon.HudText = sMessage;
                    return;
                }
                IMyRadioAntenna theAntenna = null;
                foreach(var antenna in antennaList)
                {
                    if(theAntenna==null || (antenna.Enabled && antenna.Radius>theAntenna.Radius))
                    {
                        theAntenna = antenna;
                        continue;
                    }
                }
                if(theAntenna!=null)
                {
                    theAntenna.Enabled = true;
                    theAntenna.HudText = sMessage;
                }
            }

            public void ClearAnnouncement()
            {
                foreach(var beacon in beaconList)
                {
                    if(beacon.Enabled)
                    {
                        beacon.Enabled = false;
                        beacon.HudText = "";

                    }
                }
                foreach(var antenna in antennaList)
                {
                    antenna.HudText = "";
                }
            }

        }
    }
}
