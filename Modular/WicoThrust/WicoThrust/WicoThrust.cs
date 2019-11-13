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
        #region THRUSTERS

        class WicoThrusters
        {
            List<IMyTerminalBlock> thrustAllList = new List<IMyTerminalBlock>();

            Program thisProgram;

            string sThrusterSection = "THRUSTERS";

            string sCutterThruster = "cutter";

            public int ThrusterCount()
            {
                return thrustAllList.Count;
            }

            public WicoThrusters(Program program)
            {
                thisProgram = program;
                ThrustersInit();
            }
            public void ThrustersInit()
            {
                thisProgram._CustomDataIni.Get(sThrusterSection, "CutterThruster").ToString(sCutterThruster);
                thisProgram._CustomDataIni.Set(sThrusterSection, "CutterThruster", sCutterThruster);

                // Minimal init; just add handlers
                thrustAllList.Clear();
                thisProgram.wicoBlockMaster.AddLocalBlockHandler(ThrusterParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                thisProgram.AddResetMotionHandler(ResetMotionHandler);
            }

            void ResetMotionHandler(bool bNoDrills = false)
            {
                powerDownThrusters();
            }

        public void ThrusterParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyThrust)
                {
                    if (tb.CustomName.ToLower().Contains(sCutterThruster))
                        return; // don't add it.
                    thrustAllList.Add(tb);
                }
            }

            void LocalGridChangedHandler()
            {
                thrustAllList.Clear();
            }

            public const int thrustatmo = 1;
            public const int thrusthydro = 2;
            public const int thrustion = 4;
            public const int thrusthover = 8;
            public const int thrustAll = 0xff;

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

            public int ThrusterType(IMyTerminalBlock theBlock)
            {
                if (theBlock is IMyThrust)
                {
                    // HoverEngines  http://steamcommunity.com/sharedfiles/filedetails/?id=1225107070
                    if (theBlock.BlockDefinition.SubtypeId.Contains("AtmosphericHover"))
                        return thrusthover;
                    else if (theBlock.BlockDefinition.SubtypeId.Contains("Atmo"))
                        return thrustatmo;
                    else if (theBlock.BlockDefinition.SubtypeId.Contains("Hydro"))
                        return thrusthydro;
                    // Hover Engines. SmallBlock_HoverEngine http://steamcommunity.com/sharedfiles/filedetails/?id=560731791 (last updated Dec 29, 2015)
                    else if (theBlock.BlockDefinition.SubtypeId.Contains("SmallBlock_HoverEngine"))
                        return thrusthover;
                    // assume ion since its name is generic
                    else return thrustion;
                }
                // else
                return 0;
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

            public int powerDownThrusters(int iTypes = thrustAll, bool bForceOff = false)
            {
                return powerDownThrusters(thrustAllList, iTypes, bForceOff);
            }
            public int powerDownThrusters(List<IMyTerminalBlock> thrusters, int iTypes = thrustAll, bool bForceOff = false)
            {
                int iCount = 0;
                for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                {
                    int iThrusterType = ThrusterType(thrusters[thrusterIndex]);
                    if ((iThrusterType & iTypes) > 0)
                    {
                        iCount++;
                        IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
                        thruster.ThrustOverride = 0;
                        //                    thruster.SetValueFloat("Override", 0);
                        if (thruster.IsWorking && bForceOff && thruster.Enabled == true)  // Yes, the check is worth it
                            thruster.Enabled = false;// ApplyAction("OnOff_Off");
                        else if (!thruster.IsWorking && !bForceOff && thruster.Enabled == false)
                            thruster.Enabled = true;// ApplyAction("OnOff_On");
                    }
                }
                return iCount;
            }
            /// <summary>
            /// Turns on thrusters and sets the override.
            /// </summary>
            /// <param name="thrusters">list of thrusters to use</param>
            /// <param name="fPower">power setting 0->100</param>
            /// <param name="iTypes">Type of thrusters to control. Default is all</param>
            /// <returns>number of thrusters changed</returns>
            public int powerUpThrusters(List<IMyTerminalBlock> thrusters, float fPower, int iTypes = thrustAll)
            {
                int iCount = 0;
                if (fPower > 100) fPower = 100;
                if (fPower < 0) fPower = 0;
                for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                {
                    int iThrusterType = ThrusterType(thrusters[thrusterIndex]);
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
                double cos45 = MathHelper.Sqrt2 * 0.5;
//                thisProgram.sMasterReporting += "GBT: Checking cos45=" + cos45.ToString("0.00")+"\n";

                // default selection to assign out parameters in main-line code
                thrustTowards = thrustForwardList;
                thrustAway = thrustBackwardList;

//                thrustForwardList[0].Orientation.GetMatrix(out or1);
                
                vThrustAim = thrustForwardList[0].WorldMatrix.Forward;
                angle = vNGN.Dot(vThrustAim);
//                thisProgram.sMasterReporting += "GBT:T F:Angle=" + angle.ToString("0.00") + "\n";

//                thrustUpList[0].Orientation.GetMatrix(out or1);
//                vThrustAim = or1.Forward;
                vThrustAim = thrustUpList[0].WorldMatrix.Forward;
                angle = vNGN.Dot(vThrustAim);
//                thisProgram.sMasterReporting += "GBT:T U:Angle=" + angle.ToString("0.00") + "\n";

//                thrustBackwardList[0].Orientation.GetMatrix(out or1);
//                vThrustAim = or1.Forward;
                vThrustAim = thrustBackwardList[0].WorldMatrix.Forward;
                angle = vNGN.Dot(vThrustAim);
//                thisProgram.sMasterReporting += "GBT:T B:Angle=" + angle.ToString("0.00") + "\n";

//                thrustDownList[0].Orientation.GetMatrix(out or1);
//                vThrustAim = or1.Forward;
                vThrustAim = thrustDownList[0].WorldMatrix.Forward;
                angle = vNGN.Dot(vThrustAim);
//                thisProgram.sMasterReporting += "GBT:T D:Angle=" + angle.ToString("0.00") + "\n";

//                thrustRightList[0].Orientation.GetMatrix(out or1);
//                vThrustAim = or1.Forward;
                vThrustAim = thrustRightList[0].WorldMatrix.Forward;
                angle = vNGN.Dot(vThrustAim);
//                thisProgram.sMasterReporting += "GBT:T R:Angle=" + angle.ToString("0.00") + "\n";

//                thrustLeftList[0].Orientation.GetMatrix(out or1);
//                vThrustAim = or1.Forward;
                vThrustAim = thrustLeftList[0].WorldMatrix.Forward;
                angle = vNGN.Dot(vThrustAim);
//                thisProgram.sMasterReporting += "GBT:T L:Angle=" + angle.ToString("0.00") + "\n";

                /*
                thrustDownList[0].CustomName = "thrust DN";
                thrustDownList[0].ShowOnHUD = true;
                thrustDownList[0].ShowInTerminal= true;
                thrustForwardList[0].CustomName = "thrust FW";
                thrustForwardList[0].ShowOnHUD = true;
                thrustForwardList[0].ShowInTerminal = true;
                thrustUpList[0].CustomName = "thrust Up";
                thrustUpList[0].ShowOnHUD = true;
                thrustUpList[0].ShowInTerminal = true;
                thrustBackwardList[0].CustomName = "thrust BK";
                thrustBackwardList[0].ShowOnHUD = true;
                thrustBackwardList[0].ShowInTerminal = true;
                thrustRightList[0].CustomName = "thrust RT";
                thrustRightList[0].ShowOnHUD = true;
                thrustRightList[0].ShowInTerminal = true;
                thrustLeftList[0].CustomName = "thrust LF";
                thrustLeftList[0].ShowOnHUD = true;
                thrustLeftList[0].ShowInTerminal = true;
                */

                if (thrustForwardList.Count > 0)
                {
  //                  thrustForwardList[0].Orientation.GetMatrix(out or1);
//                    vThrustAim = or1.Forward;
                    vThrustAim = thrustForwardList[0].WorldMatrix.Forward;
                    angle = Math.Abs(vNGN.Dot(vThrustAim));
//                    thisProgram.sMasterReporting += "GBT: F:Angle="+angle.ToString("0.00") + "\n";
                    if (angle > cos45)
                    {
//                        thisProgram.Echo("GBT: Thrust fowrard");
                        thisProgram.sMasterReporting += "GBT: Thrust fowrard\n";
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
                        thisProgram.sMasterReporting += "GBT: Thrust UP\n";
//                        thisProgram.Echo("GBT: Thrust UP");
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
                        //                      thisProgram.Echo("GBT: Thrust BACKWARD");
//                        thisProgram.sMasterReporting += "GBT: Thrust BACKWARD\n";
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
                        //                        thisProgram.Echo("GBT: Thrust DOWN");
//                        thisProgram.sMasterReporting += "GBT: Thrust DOWN\n";
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
                        //                        thisProgram.Echo("GBT: Thrust RIGHT");
//                        thisProgram.sMasterReporting += "GBT: Thrust RIGHT\n";
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
                        //                        thisProgram.Echo("GBT: Thrust LEFT");
//                        thisProgram.sMasterReporting += "GBT: Thrust LEFT\n";
                        thrustTowards = thrustLeftList;
                        thrustAway = thrustRightList;
                        return;
                    }
                }
//                thisProgram.Echo("GBT: Thrust DEFAULT");
                thisProgram.sMasterReporting += "GBT: Thrust DEFAULT\n";

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
                /*
                Matrix or1;
                tb.Orientation.GetMatrix(out or1);
                return or1.Down;
                */
            }

            public bool CalculateHoverThrust(IMyShipController ShipController, List<IMyTerminalBlock> thrusters, out float atmoPercent, out float hydroPercent, out float ionPercent)
            {
                atmoPercent = 0;
                hydroPercent = 0;
                ionPercent = 0;
                double ionThrust = calculateMaxThrust(thrusters, thrustion);
                double atmoThrust = calculateMaxThrust(thrusters, thrustatmo);
                double hydroThrust = calculateMaxThrust(thrusters, thrusthydro);

                MyShipMass myMass;
                myMass = ShipController.CalculateShipMass();
                Vector3D vGrav = ShipController.GetNaturalGravity();
                double dGravity = vGrav.Length() / 9.81;
                double hoverthrust = myMass.PhysicalMass * dGravity * 9.810;

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
                //	Echo("Atmo=" + ((atmoThrust * atmoPercent) / 100).ToString("N0"));
                //	Echo("ion=" + ((ionThrust * ionPercent) / 100).ToString("N0"));
                //	Echo("hydro=" + ((hydroThrust * hydroPercent) / 100).ToString("N0"));
                //	Echo("Left over thrust=" + hoverthrust.ToString("N0"));
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
            public double calculateStoppingDistance(float physicalMass, List<IMyTerminalBlock> thrustStopList, double currentV, double dGrav)
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

            //            [Obsolete]
            public double calculateStoppingDistance(IMyTerminalBlock ShipController, List<IMyTerminalBlock> thrustStopList, double currentV, double dGrav)
            {
                var myMass = ((IMyShipController)ShipController).CalculateShipMass();
                return calculateStoppingDistance(myMass.PhysicalMass, thrustStopList, currentV, dGrav);
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

        }
        #endregion

    }
}
