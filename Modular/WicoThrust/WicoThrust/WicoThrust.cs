﻿using Sandbox.Game.EntityComponents;
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

        public class WicoThrusters: WicoBasicThrusters
        {

            public WicoThrusters(Program program, WicoBlockMaster wicoBlockMaster): base(program,wicoBlockMaster)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;

                ThrustersInit();
            }

            readonly Matrix thrustIdentityMatrix = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            public void ThrustersCalculateOrientation(IMyTerminalBlock orientationBlock, ref List<IMyTerminalBlock> thrustForwardList,
                ref List<IMyTerminalBlock> thrustBackwardList, ref List<IMyTerminalBlock> thrustDownList, ref List<IMyTerminalBlock> thrustUpList,
                ref List<IMyTerminalBlock> thrustLeftList, ref List<IMyTerminalBlock> thrustRightList)
            {
                thrustForwardList.Clear();
                thrustBackwardList.Clear();
                thrustDownList.Clear();
                thrustUpList.Clear();
                thrustLeftList.Clear();
                thrustRightList.Clear();
                if (orientationBlock == null) return;

                Matrix fromGridToReference;
                orientationBlock.Orientation.GetMatrix(out fromGridToReference);
                Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

                Matrix fromThrusterToGrid;

                for (int i = 0; i < thrustAllList.Count; ++i)
                {
                    var thruster = thrustAllList[i] as IMyThrust;
                    thruster.Orientation.GetMatrix(out fromThrusterToGrid);
                    Vector3 accelerationDirection = Vector3.Transform(fromThrusterToGrid.Backward, fromGridToReference);
                    if (accelerationDirection == thrustIdentityMatrix.Left)
                    {
                        thrustLeftList.Add(thrustAllList[i]);
                    }
                    else if (accelerationDirection == thrustIdentityMatrix.Right)
                    {
                        thrustRightList.Add(thrustAllList[i]);
                    }
                    else if (accelerationDirection == thrustIdentityMatrix.Backward)
                    {
                        thrustBackwardList.Add(thrustAllList[i]);
                    }
                    else if (accelerationDirection == thrustIdentityMatrix.Forward)
                    {
                        thrustForwardList.Add(thrustAllList[i]);
                    }
                    else if (accelerationDirection == thrustIdentityMatrix.Up)
                    {
                        thrustUpList.Add(thrustAllList[i]);
                    }
                    else if (accelerationDirection == thrustIdentityMatrix.Down)
                    {
                        thrustDownList.Add(thrustAllList[i]);
                    }
                }
            }

            public double ThrustersCalculateMax(List<IMyTerminalBlock> thrusters, int iTypes = thrustAll)
            {
                double thrust = 0;
                for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                {
                    int iThrusterType = ThrusterType(thrusters[thrusterIndex]);
                    if ((iThrusterType & iTypes) > 0)
                    {
                        IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
                        thrust += thruster.MaxEffectiveThrust;
                    }
                }

                return thrust;
            }

            public double AtmoEffectiveness()
            {
                //            if (atmoThrustCount < 1) return 0;

                var myThrust = ThrustFindFirst(thrustAllList, thrustatmo);

                if (myThrust == null) return 0;

                return myThrust.MaxEffectiveThrust / myThrust.MaxThrust;
            }
            public IMyThrust ThrustFindFirst(List<IMyTerminalBlock> list, int iType = thrustAll)
            {
                foreach (var thrust in thrustAllList)
                {
                    if (thrust is IMyThrust && (ThrusterType(thrust) & iType) > 0)
                        return thrust as IMyThrust;
                }
                return null;
            }

            /// <summary>
            /// Turns on thrusters and sets the override.
            /// </summary>
            /// <param name="thrusters">list of thrusters to use</param>
            /// <param name="fPower">power setting 0->100</param>
            /// <param name="iTypes">Type of thrusters to control. Default is all</param>
            /// <returns>number of thrusters changed</returns>
            public int powerUpThrusters(List<IMyTerminalBlock> thrusters, float fPower=100f, int iTypes = thrustAll)
            {
                int iCount = 0;
                if (fPower > 100) fPower = 100;
                if (fPower < 0) fPower = 0;
                for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                {
                    int iThrusterType = ThrusterType(thrusters[thrusterIndex]);
 //                   _program.Echo(thrusterIndex + ":" + iThrusterType);
                    if ((iThrusterType & iTypes) > 0)
                    {
                        IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
                        if (!thruster.IsWorking)
                        {
                            if (!thruster.Enabled) // yes, this is worth the cost to check.
                                thruster.Enabled = true;
                        }
                        iCount += 1;
                        thruster.ThrustOverridePercentage = fPower / 100f;
                    }
                }
                return iCount;
            }
            public double calculateMaxThrust(List<IMyTerminalBlock> thrusters, int iTypes = thrustAll)
            {
                double thrust = 0;
                //	Echo("cMT:" + iTypes.ToString() + ":"+ thrusters.Count);
                for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                {
                    int iThrusterType = ThrusterType(thrusters[thrusterIndex]);
                    //		Echo(thrusterIndex.ToString() + ":" + thrusters[thrusterIndex].CustomName + ":" + iThrusterType.ToString());
                    if ((iThrusterType & iTypes) > 0)
                    {
                        //			Echo("My Type");
                        IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
                        double dThrust = thruster.MaxEffectiveThrust; // maxThrust(thruster);
                        thrust += dThrust;
                        //			Echo("thisthrust=" + dThrust.ToString("N0"));
                    }
                    //		else Echo("NOT My Type");
                }

                return thrust;
            }
            double cos45 = MathHelper.Sqrt2 * 0.5;

            public void GetBestThrusters(Vector3D v1,
                List<IMyTerminalBlock> thrustForwardList, List<IMyTerminalBlock> thrustBackwardList,
                List<IMyTerminalBlock> thrustDownList, List<IMyTerminalBlock> thrustUpList,
                List<IMyTerminalBlock> thrustLeftList, List<IMyTerminalBlock> thrustRightList,
                out List<IMyTerminalBlock> thrustTowards, out List<IMyTerminalBlock> thrustAway
                )
            {
//                Matrix or1;
                double angle;
                Vector3D vThrustAim;
                Vector3D vNGN = v1;
                vNGN.Normalize();
//                _program.sMasterReporting += "GBT: Checking cos45=" + cos45.ToString("0.00")+"\n";

                // default selection to assign out parameters in main-line code
                thrustTowards = thrustForwardList;
                thrustAway = thrustBackwardList;

                if (Vector3D.IsZero(vNGN)) return;

                if (thrustForwardList.Count > 0)
                {
                    vThrustAim = thrustForwardList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
//                    _program.sMasterReporting += "GBT: F:Angle="+angle.ToString("0.00") + "\n";
                    if (angle > cos45)
                    {
//                        _program.Echo("GBT: Thrust fowrard");
//                        _program.sMasterReporting += "GBT: Thrust fowrard\n";
                        return;
                    }
                }

                if (thrustUpList.Count > 0)
                {
//                  thrustUpList[0].Orientation.GetMatrix(out or1);
//                    vThrustAim = or1.Forward;
                    vThrustAim = thrustUpList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
                    if (angle > cos45)
                    {
//                        _program.sMasterReporting += "GBT: Thrust UP\n";
//                        _program.Echo("GBT: Thrust UP");
                        thrustTowards = thrustUpList;
                        thrustAway = thrustDownList;
                        return;
                    }
                }

                if (thrustBackwardList.Count > 0)
                {
//                    thrustBackwardList[0].Orientation.GetMatrix(out or1);
//                    vThrustAim = or1.Forward;
                    vThrustAim = thrustBackwardList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
                    if (angle > cos45)
                    {
//                      _program.Echo("GBT: Thrust BACKWARD");
//                        _program.sMasterReporting += "GBT: Thrust BACKWARD\n";
                        thrustTowards = thrustBackwardList;
                        thrustAway = thrustForwardList;
                        return;
                    }
                }

                if (thrustDownList.Count > 0)
                {
//                    thrustDownList[0].Orientation.GetMatrix(out or1);
//                    vThrustAim = or1.Forward;
                    vThrustAim = thrustDownList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
                    if (angle > cos45)
                    {
//                        _program.Echo("GBT: Thrust DOWN");
//                        _program.sMasterReporting += "GBT: Thrust DOWN\n";
                        thrustTowards = thrustDownList;
                        thrustAway = thrustUpList;
                        return;
                    }
                }

                if (thrustRightList.Count > 0)
                {
//                    thrustRightList[0].Orientation.GetMatrix(out or1);
//                    vThrustAim = or1.Forward;
                    vThrustAim = thrustRightList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
                    if (angle > cos45)
                    {
//                        _program.Echo("GBT: Thrust RIGHT");
//                        _program.sMasterReporting += "GBT: Thrust RIGHT\n";
                        thrustTowards = thrustRightList;
                        thrustAway = thrustLeftList;
                        return;
                    }
                }

                if (thrustLeftList.Count > 0)
                {
//                    thrustLeftList[0].Orientation.GetMatrix(out or1);
//                    vThrustAim = or1.Forward;
                    vThrustAim = thrustLeftList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
                    if (angle > cos45)
                    {
//                        _program.Echo("GBT: Thrust LEFT");
//                        _program.sMasterReporting += "GBT: Thrust LEFT\n";
                        thrustTowards = thrustLeftList;
                        thrustAway = thrustRightList;
                        return;
                    }
                }
//                _program.Echo("GBT: Thrust DEFAULT");
//                _program.sMasterReporting += "GBT: Thrust DEFAULT\n";

            }

            public void GetMaxScaledThrusters(
                List<IMyTerminalBlock> thrustForwardList, List<IMyTerminalBlock> thrustBackwardList,
                List<IMyTerminalBlock> thrustDownList, List<IMyTerminalBlock> thrustUpList,
                List<IMyTerminalBlock> thrustLeftList, List<IMyTerminalBlock> thrustRightList,
                out List<IMyTerminalBlock> bestThrust, out List<IMyTerminalBlock> reverseBestThrust,
                float atmoMult = 5f, float ionMult = 2f, float hydroMult = 1f
                )
            {
                double currentMaxThrust = 0;
                double maxThrust = 0;
                maxThrust = CalculateTotalEffectiveThrust(thrustForwardList, atmoMult, ionMult, hydroMult);
                currentMaxThrust = maxThrust;
                bestThrust = thrustForwardList;
                reverseBestThrust = thrustBackwardList;
                /*
                if (maxThrust > currentMaxThrust)
                {
                    currentMaxThrust = maxThrust;
                    tb = thrustForwardList[0];
                }
                */
                maxThrust = CalculateTotalEffectiveThrust(thrustBackwardList, atmoMult, ionMult, hydroMult);
                if (maxThrust > currentMaxThrust)
                {
                    currentMaxThrust = maxThrust;
                    bestThrust = thrustBackwardList;
                    reverseBestThrust = thrustForwardList;
                }
                maxThrust = CalculateTotalEffectiveThrust(thrustDownList, atmoMult, ionMult, hydroMult);
                if (maxThrust > currentMaxThrust)
                {
                    currentMaxThrust = maxThrust;
                    bestThrust = thrustDownList;
                    reverseBestThrust = thrustUpList;
                }
                maxThrust = CalculateTotalEffectiveThrust(thrustUpList, atmoMult, ionMult, hydroMult);
                if (maxThrust > currentMaxThrust)
                {
                    currentMaxThrust = maxThrust;
                    bestThrust = thrustUpList;
                    reverseBestThrust = thrustDownList;
                }
                maxThrust = CalculateTotalEffectiveThrust(thrustLeftList, atmoMult, ionMult, hydroMult);
                if (maxThrust > currentMaxThrust)
                {
                    currentMaxThrust = maxThrust;
                    bestThrust = thrustLeftList;
                    reverseBestThrust = thrustRightList;
                }
                maxThrust = CalculateTotalEffectiveThrust(thrustRightList, atmoMult, ionMult, hydroMult);
                if (maxThrust > currentMaxThrust)
                {
                    currentMaxThrust = maxThrust;
                    bestThrust = thrustRightList;
                    reverseBestThrust = thrustLeftList;
                }

                return;
            }

            public bool CalculateHoverThrust( List<IMyTerminalBlock> thrusters, out float atmoPercent, out float hydroPercent, out float ionPercent)
            {
                atmoPercent = 0;
                hydroPercent = 0;
                ionPercent = 0;
                double ionThrust = calculateMaxThrust(thrusters, thrustion);
                double atmoThrust = calculateMaxThrust(thrusters, thrustatmo);
                double hydroThrust = calculateMaxThrust(thrusters, thrusthydro);

                double physicalMass = _wicoBlockMaster.GetAllPhysicalMass();
                Vector3D vGrav = _wicoBlockMaster.GetNaturalGravity();
                double dGravity = vGrav.Length() / 9.81;
                double hoverthrust = physicalMass * dGravity * 9.810;

                if (atmoThrust > 0)
                {
                    if (atmoThrust < hoverthrust)
                    {
                        atmoPercent = 100;
                        hoverthrust -= atmoThrust;
                    }
                    else
                    {
                        atmoPercent = (float)(hoverthrust / atmoThrust * 100);
                        if (atmoPercent > 0)
                            hoverthrust -= (atmoThrust * atmoPercent / 100);
                    }
                }
                if (hoverthrust < 0.01) hoverthrust = 0;
                //	Echo("ALeft over thrust=" + hoverthrust.ToString("N0"));

                if (ionThrust > 0 && hoverthrust > 0)
                {
                    if (ionThrust < hoverthrust)
                    {
                        ionPercent = 100;
                        hoverthrust -= ionThrust;
                    }
                    else
                    {
                        ionPercent = (float)(hoverthrust / ionThrust * 100);
                        if (ionPercent > 0)
                            hoverthrust -= ((ionThrust * ionPercent) / 100);
                    }
                }
                if (hoverthrust < 0.01) hoverthrust = 0;
                //	Echo("ILeft over thrust=" + hoverthrust.ToString("N0"));

                if (hydroThrust > 0 && hoverthrust > 0)
                {
                    if (hydroThrust < hoverthrust)
                    {
                        hydroPercent = 100;
                        hoverthrust -= hydroThrust;
                    }
                    else
                    {
                        hydroPercent = (float)(hoverthrust / hydroThrust * 100);
                        if (hydroPercent > 0)
                            hoverthrust -= ((hydroThrust * hydroPercent) / 100); ;
                    }
                }
                if (atmoPercent < 0.01) atmoPercent = 0;
                if (hydroPercent < 0.01) hydroPercent = 0;
                if (ionPercent < 0.01) ionPercent = 0;

                if (hoverthrust > 0) return false;
                return true;
            }

            /// <summary>
            /// Stopping distance based on thrust available, mass, current velocity and an optional gravity factor
            /// </summary>
            /// <param name="thrustStopList">list of thrusters to use</param>
            /// <param name="currentV">velocity to calculage</param>
            /// <param name="dGrav">optional gravity factor</param>
            /// <returns>stopping distance in meters</returns>
            public double calculateStoppingDistance(double physicalMass, List<IMyTerminalBlock> thrustStopList, double currentV, double dGrav)
            {
                double hoverthrust = physicalMass * dGrav * 9.810;
                double maxThrust = calculateMaxThrust(thrustStopList);
                double maxDeltaV = (maxThrust - hoverthrust) / physicalMass;
                double secondstozero = currentV / maxDeltaV;
                //            Echo("secondstozero=" + secondstozero.ToString("0.00"));
                double stoppingM = currentV / 2 * secondstozero;
                //            Echo("stoppingM=" + stoppingM.ToString("0.00"));
                return stoppingM;
            }

            public double CalculateTotalEffectiveThrust(List<IMyTerminalBlock> thrusters, float atmoMult = 5f, float ionMult = 2f, float hydroMult = 1f)
            {
                double totalThrust = 0;

                foreach (var block in thrusters)
                {
                    var thruster = block as IMyThrust;
                    if (thruster == null) continue;
                    int thrusterType = ThrusterType(thruster);
                    if (thrusterType == thrustatmo)
                        totalThrust += thruster.MaxEffectiveThrust * atmoMult;
                    else if (thrusterType == thrustion)
                        totalThrust += thruster.MaxEffectiveThrust * ionMult;
                    else if (thrusterType == thrusthydro)
                        totalThrust += thruster.MaxEffectiveThrust * hydroMult;
                    else
                        totalThrust += thruster.MaxEffectiveThrust;
                }
                return totalThrust;
            }

            int _iMFSWiggle = 0;
            /// <summary>
            /// Move using specified thrusters at slow speed.
            /// </summary>
            /// <param name="fTarget">Target speed in mps</param>
            /// <param name="fAbort">Abort speed in mps.  Emergency stop if above this speed</param>
            /// <param name="mfsForwardThrust">thrusters for 'forward'</param>
            /// <param name="mfsBackwardThrust">reverse thrusters to slow down. 'Back'</param>
            /// <param name="effectiveMass">ship's effective mass</param>
            /// <param name="shipSpeed">the ship's current speed</param>
            public void MoveForwardSlow(float fTarget, float fAbort, List<IMyTerminalBlock> mfsForwardThrust,
                List<IMyTerminalBlock> mfsBackwardThrust, double effectiveMass, double shipSpeed)
            {
                if (_iMFSWiggle < 0) _iMFSWiggle = 0;

                // todo: only do every so often
                // TODO: 1. Cache for the list 2. Don't take list and cache (assume forward?)
                // 3. make other directions(used in docking in direction of connector)
                double maxThrust = calculateMaxThrust(mfsForwardThrust);
                //            Echo("maxThrust=" + maxThrust.ToString("N0"));

                float thrustPercent = 100f;
                if (effectiveMass > 0)
                {
                    double maxDeltaV = (maxThrust) / effectiveMass;
                    //           Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));
                    if (maxDeltaV > 0) thrustPercent = (float)(fTarget / maxDeltaV);
//                    _program.Echo("thrustPercent=" + thrustPercent.ToString("0.00"));
                }
                //            Echo("effectiveMass=" + effectiveMass.ToString("N0"));
                if (shipSpeed > fAbort)
                {
//                    _program.Echo("ABORT!");
                    powerDownThrusters(thrustAllList);
                }
                else if (shipSpeed < (fTarget * 0.90))
                {
                    if (shipSpeed < 0.09)
                    {
                        // we have not started moving yet
                        _iMFSWiggle++;
                        thrustPercent *= 1.25f;
                    }
                    if (shipSpeed < fTarget * 0.25)
                    {   
                        // we are not yet moving fast enough
                        _iMFSWiggle++;
                    }
                    //                Echo("Push ");
                    //                Echo("thrustPercent=" + thrustPercent.ToString("0.00"));
//                    _program.Echo("Wiggle=" + _iMFSWiggle);
                    if (_iMFSWiggle < 500) // 100*5
                    {
                        powerUpThrusters(mfsForwardThrust, thrustPercent + _iMFSWiggle / 5);
                    }
                    else
                    {
//                        _program.Echo("WIGGLE DELAY");
                        powerDownThrusters();
                    }
                    if (_iMFSWiggle > 700) // number is higher to have a delay
                    {
//                        _program.Echo("WIGGLE RESET");
                        _iMFSWiggle = 0; // reset
                    }
                }
                else if (shipSpeed < (fTarget * 1.1))
                {
                    // we are around target. 90%<-current->120%
                    //                                 Echo("Coast");
                    _iMFSWiggle--;
                    // turn off reverse thrusters and 'coast'.
                    powerDownThrusters(mfsBackwardThrust, thrustAll, true);
                    powerDownThrusters(mfsForwardThrust);
                }
                else
                { // above 110% target, but below abort
                  //                Echo("Coast2");
                    _iMFSWiggle--;
                    _iMFSWiggle--;
                    powerUpThrusters(mfsForwardThrust, 1f); // coast
                }

            }

            public void MoveForwardSlowReset()
            {
                _iMFSWiggle = 0;
            }
        }

    }
}
