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

            Program myGridProgram;

            public int ThrusterCount()
            {
                return thrustAllList.Count;
            }

            public WicoThrusters(Program program)
            {
                myGridProgram = program;
                ThrustersInit();
            }
            public void ThrustersInit()
            {
                //TODO: Load defaults from CustomData

                // Minimal init; just add handlers
                thrustAllList.Clear();
                myGridProgram.wicoBlockMaster.AddLocalBlockHandler(ThrusterParseHandler);
            }

            public void ThrusterParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyThrust)
                {
                    // TODO: Ignore cutters, etc
                    thrustAllList.Add(tb);
                }
            }

            const int thrustatmo = 1;
            const int thrusthydro = 2;
            const int thrustion = 4;
            const int thrusthover = 8;
            const int thrustAll = 0xff;

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

                for (int i = 0; i < thrustAllList.Count; ++i)
                {
                    var thruster = thrustAllList[i] as IMyThrust;
                    Matrix fromThrusterToGrid;
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


            public bool calculateHoverThrust(IMyTerminalBlock ShipController, List<IMyTerminalBlock> thrusters, out float atmoPercent, out float hydroPercent, out float ionPercent)
            {
                atmoPercent = 0;
                hydroPercent = 0;
                ionPercent = 0;
                double ionThrust = calculateMaxThrust(thrusters, thrustion);
                double atmoThrust = calculateMaxThrust(thrusters, thrustatmo);
                double hydroThrust = calculateMaxThrust(thrusters, thrusthydro);

                MyShipMass myMass;
                myMass = ((IMyShipController)ShipController).CalculateShipMass();
                Vector3D vGrav = ((IMyShipController)ShipController).GetNaturalGravity();
                double dGravity = vGrav.Length();
                double hoverthrust = hoverthrust = myMass.PhysicalMass * dGravity * 9.810;

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
            public double calculateStoppingDistance(IMyTerminalBlock ShipController, List<IMyTerminalBlock> thrustStopList, double currentV, double dGrav)
            {
                var myMass = ((IMyShipController)ShipController).CalculateShipMass();
                double hoverthrust = myMass.PhysicalMass * dGrav * 9.810;
                double maxThrust = calculateMaxThrust(thrustStopList);
                double maxDeltaV = (maxThrust - hoverthrust) / myMass.PhysicalMass;
                double secondstozero = currentV / maxDeltaV;
                //            Echo("secondstozero=" + secondstozero.ToString("0.00"));
                double stoppingM = currentV / 2 * secondstozero;
                //            Echo("stoppingM=" + stoppingM.ToString("0.00"));
                return stoppingM;
            }
        }
        #endregion

    }
}
