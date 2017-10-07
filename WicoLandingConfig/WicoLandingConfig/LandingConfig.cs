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
        string sLandingID = "[LANDING]"; // put this name in CustomName or CustomData of mechanical items to be controlled

        // landing configuration control
        List<LandingRotor> landingRotorList = new List<LandingRotor>();
        List<LandingPiston> landingPistonList = new List<LandingPiston>();

        public class LandingRotor
        {
            public IMyMotorStator r;
            public float maxVelocity;

//            public LandingRotor subRotor;
 //           public float targetAngle;
        }
        public class LandingPiston
        {
            public IMyPistonBase p;

            public float maxVelocity;

//            public LandingRotor subRotor;
        }

        string landingsInit(IMyTerminalBlock blockOrientation)
        {
            landingRotorList.Clear();
            landingPistonList.Clear();

            List<IMyTerminalBlock> rotorsList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(rotorsList, (x => x.CubeGrid == blockOrientation.CubeGrid));

            for (int i = 0; i < rotorsList.Count; i++)
            {
                if (rotorsList[i].CustomName.Contains(sLandingID) || rotorsList[i].CustomData.Contains(sLandingID))
                {
                    LandingRotor gr = new LandingRotor();
                    landingRotorLoad(rotorsList[i], gr);
                    Echo(gr.r.CustomName);
                    landingRotorList.Add(gr);
                }
            }
            List<IMyTerminalBlock> pistonsList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(pistonsList, (x => x.CubeGrid == blockOrientation.CubeGrid));

            for (int i = 0; i < pistonsList.Count; i++)
            {
                if (pistonsList[i].CustomName.Contains(sLandingID) || pistonsList[i].CustomData.Contains(sLandingID))
                {
                    LandingPiston gr = new LandingPiston();
                    landingPistonLoad(pistonsList[i], gr);
                    Echo(gr.p.CustomName);
                    landingPistonList.Add(gr);
                }
            }
            string s = "";
            s += "LR" + landingRotorList.Count.ToString() + ":";
            s += "LP" + landingPistonList.Count.ToString() + ":";
            return s;
        }

        void landingRotorLoad(IMyTerminalBlock r, LandingRotor gr)
        {
            Func<string, bool> asBool = (txt) =>
            {
                txt = txt.Trim().ToLower();
                return (txt == "True" || txt == "true");
            };

            gr.r = r as IMyMotorStator;
            Echo("Loading Landing Rotor:" + r.CustomName);
            float maxVelocity = r.GetMaximum<float>("Velocity");
            Echo("rotor maxV=" + maxVelocity);
            gr.maxVelocity = maxVelocity;
            string sData = r.CustomData;
            //Echo("data=" + sData);
            string[] lines = sData.Trim().Split('\n');
            //	Echo(lines.Length + " Lines");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] keys = lines[i].Trim().Split('=');
//                if (lines[i].Contains("GimbalZMinus"))

                //		Echo(lines[i]);

            }
//            Echo("Max Velocity=" + gr.maxVelocity.ToString());

        }
        void landingPistonLoad(IMyTerminalBlock r, LandingPiston gr)
        {
            Func<string, bool> asBool = (txt) =>
            {
                txt = txt.Trim().ToLower();
                return (txt == "True" || txt == "true");
            };

            gr.p = r as IMyPistonBase;

            Echo("Loading Landing Piston:" + r.CustomName);
            float maxVelocity = gr.p.MaxVelocity;
            gr.maxVelocity = maxVelocity;
            Echo("piston maxV=" + maxVelocity);
            Echo("Status=" + gr.p.Status.ToString());
            Echo("MaxLimit=" + gr.p.MaxLimit.ToString());
            Echo("MinLimit=" + gr.p.MinLimit.ToString());

            string sData = r.CustomData;
            //Echo("data=" + sData);
            string[] lines = sData.Trim().Split('\n');
            //	Echo(lines.Length + " Lines");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] keys = lines[i].Trim().Split('=');


                //		Echo(lines[i]);

            }
//            Echo("Max Velocity=" + gr.maxVelocity.ToString());

            // sub-rotors & Pistons

        }

        bool landingDoMode(int flightmode=0)
        {
            bool bAllReady = true;
            // 0=flight
            // 1=landing
            if(flightmode==0)
            {
                foreach(var gr in landingPistonList)
                {
                    if (gr.p.Status == PistonStatus.Retracted)
                    {
                        gr.p.SafetyLock = true;
                    }
                    else
                    {
                        gr.p.SafetyLock = false;
                        gr.p.Retract();
                        bAllReady = false;
                    }
                }

            }
            else if(flightmode==1)
            {
                foreach(var gr in landingPistonList)
                {
                    if (gr.p.Status == PistonStatus.Extended)
                        gr.p.SafetyLock = true;
                    else
                    {
                        gr.p.SafetyLock = false;
                        gr.p.Extend();
                        bAllReady = false;
                    }
                }
            }
            return bAllReady;
        }

    }
}
