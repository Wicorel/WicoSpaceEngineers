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
        #region thrusters
        List<IMyTerminalBlock> thrustAllList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();

        double thrustForward = 0;
        double thrustBackward = 0;
        double thrustDown = 0;
        double thrustUp = 0;
        double thrustLeft = 0;
        double thrustRight = 0;

        int ionThrustCount = 0;
        int hydroThrustCount = 0;
        int atmoThrustCount = 0;
        const int thrustatmo = 1;
        const int thrusthydro = 2;
        const int thrustion = 4;
        const int thrustAll = 0xff;
        readonly Matrix thrustIdentityMatrix = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        string sIgnoreThruster = "IGNORE";
        string sCutterThruster = "cutter";

        void thrustersInit(IMyTerminalBlock orientationBlock, ref List<IMyTerminalBlock> thrustForwardList, 
            ref List<IMyTerminalBlock> thrustBackwardList, ref List<IMyTerminalBlock> thrustDownList,ref List<IMyTerminalBlock> thrustUpList,
            ref List<IMyTerminalBlock> thrustLeftList, ref List<IMyTerminalBlock> thrustRightList, int iThrustCheckType=thrustAll)
        {
            thrustForwardList.Clear();
            thrustBackwardList.Clear();
            thrustDownList.Clear();
            thrustUpList.Clear();
            thrustLeftList.Clear();
            thrustRightList.Clear();
            // TODO: this modifies thrust all list..  it probably shouldn't for a sub-get..
            thrustAllList.Clear();

            if (orientationBlock == null) return;
//            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrustAllList, localGridFilter);
	        List<IMyTerminalBlock> thrustLocal = new List<IMyTerminalBlock>();

            // Add 'cutter' exclusion from thrusters.
	        GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrustLocal, localGridFilter);
	        for(int i=0;i<thrustLocal.Count;i++)
	        {
		        if (thrustLocal[i].CustomName.ToLower().Contains(sCutterThruster) || thrustLocal[i].CustomData.ToLower().Contains(sCutterThruster))
			        continue;
		        if (thrustLocal[i].CustomName.ToLower().Contains(sIgnoreThruster) || thrustLocal[i].CustomData.ToLower().Contains(sIgnoreThruster))
			        continue;
		        thrustAllList.Add(thrustLocal[i]);
	        }

            Matrix fromGridToReference;
            orientationBlock.Orientation.GetMatrix(out fromGridToReference);
            Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

            thrustForward = 0;
            thrustBackward = 0;
            thrustDown = 0;
            thrustUp = 0;
            thrustLeft = 0;
            thrustRight = 0;

            for (int i = 0; i < thrustAllList.Count; ++i)
            {
                IMyThrust thruster = thrustAllList[i] as IMyThrust;
                Matrix fromThrusterToGrid;
                thruster.Orientation.GetMatrix(out fromThrusterToGrid);
                Vector3 accelerationDirection = Vector3.Transform(fromThrusterToGrid.Backward, fromGridToReference);
                int iThrustType = thrusterType(thrustAllList[i]);
                if (iThrustType == thrustatmo)
                    atmoThrustCount++;
                else if (iThrustType == thrusthydro)
                    hydroThrustCount++;
                else if (iThrustType == thrustion)
                    ionThrustCount++;
                if (accelerationDirection == thrustIdentityMatrix.Left)
                {
                    thrustLeft += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustLeftList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Right)
                {
                    thrustRight += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustRightList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Backward)
                {
                    thrustBackward += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustBackwardList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Forward)
                {
                    thrustForward += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustForwardList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Up)
                {
                    thrustUp += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustUpList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Down)
                {
                    thrustDown += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustDownList.Add(thrustAllList[i]);
                }
            }

           }

        string thrustersInit(IMyTerminalBlock orientationBlock)
        {
            thrustForwardList.Clear();
            thrustBackwardList.Clear();
            thrustDownList.Clear();
            thrustUpList.Clear();
            thrustLeftList.Clear();
            thrustRightList.Clear();
            thrustAllList.Clear();

            if (orientationBlock == null) return "No Orientation Block";
//            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrustAllList, localGridFilter);
	        List<IMyTerminalBlock> thrustLocal = new List<IMyTerminalBlock>();

            // Add 'cutter' exclusion from thrusters.
	        GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrustLocal, localGridFilter);
	        for(int i=0;i<thrustLocal.Count;i++)
	        {
		        if (thrustLocal[i].CustomName.ToLower().Contains(sCutterThruster) || thrustLocal[i].CustomData.ToLower().Contains(sCutterThruster))
			        continue;
		        if (thrustLocal[i].CustomName.ToLower().Contains(sIgnoreThruster) || thrustLocal[i].CustomData.ToLower().Contains(sIgnoreThruster))
			        continue;
		        thrustAllList.Add(thrustLocal[i]);
	        }

            Matrix fromGridToReference;
            orientationBlock.Orientation.GetMatrix(out fromGridToReference);
            Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

            thrustForward = 0;
            thrustBackward = 0;
            thrustDown = 0;
            thrustUp = 0;
            thrustLeft = 0;
            thrustRight = 0;

            for (int i = 0; i < thrustAllList.Count; ++i)
            {
                IMyThrust thruster = thrustAllList[i] as IMyThrust;
                Matrix fromThrusterToGrid;
                thruster.Orientation.GetMatrix(out fromThrusterToGrid);
                Vector3 accelerationDirection = Vector3.Transform(fromThrusterToGrid.Backward, fromGridToReference);
                int iThrustType = thrusterType(thrustAllList[i]);
                if (iThrustType == thrustatmo)
                    atmoThrustCount++;
                else if (iThrustType == thrusthydro)
                    hydroThrustCount++;
                else if (iThrustType == thrustion)
                    ionThrustCount++;
                if (accelerationDirection == thrustIdentityMatrix.Left)
                {
                    thrustLeft += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustLeftList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Right)
                {
                    thrustRight += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustRightList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Backward)
                {
                    thrustBackward += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustBackwardList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Forward)
                {
                    thrustForward += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustForwardList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Up)
                {
                    thrustUp += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustUpList.Add(thrustAllList[i]);
                }
                else if (accelerationDirection == thrustIdentityMatrix.Down)
                {
                    thrustDown += maxThrust((IMyThrust)thrustAllList[i]);
                    thrustDownList.Add(thrustAllList[i]);
                }
            }

            string s;
            s = ">";
            s += "F" + thrustForwardList.Count.ToString("00");
            s += "B" + thrustBackwardList.Count.ToString("00");
            s += "D" + thrustDownList.Count.ToString("00");
            s += "U" + thrustUpList.Count.ToString("00");
            s += "L" + thrustLeftList.Count.ToString("00");
            s += "R" + thrustRightList.Count.ToString("00");
            s += "<";
            return s;
        }
        int thrusterType(IMyTerminalBlock theBlock)
        {
            if (theBlock is IMyThrust)
            {
                if (theBlock.BlockDefinition.SubtypeId.Contains("Atmo"))
                    return thrustatmo;
                else if (theBlock.BlockDefinition.SubtypeId.Contains("Hydro"))
                    return thrusthydro;
                else return thrustion;
            }
            // else
            return 0;
        }

        double maxThrust(IMyThrust thruster)
        {
            return thruster.MaxEffectiveThrust;
        }

        double calculateMaxThrust(List<IMyTerminalBlock> thrusters, int iTypes = thrustAll)
        {
            double thrust = 0;
            //	Echo("cMT:" + iTypes.ToString() + ":"+ thrusters.Count);
            for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
            {
                int iThrusterType = thrusterType(thrusters[thrusterIndex]);
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

        bool calculateHoverThrust(List<IMyTerminalBlock> thrusters, out float atmoPercent, out float hydroPercent, out float ionPercent)
        {
            atmoPercent = 0;
            hydroPercent = 0;
            ionPercent = 0;
            double ionThrust = calculateMaxThrust(thrusters, thrustion);
            double atmoThrust = calculateMaxThrust(thrusters, thrustatmo);
            double hydroThrust = calculateMaxThrust(thrusters, thrusthydro);

            MyShipMass myMass;
            myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
            double hoverthrust = 0;
            hoverthrust = myMass.PhysicalMass * dGravity * 9.810;

//            Echo("hoverthrust=" + hoverthrust.ToString("N0"));

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

        List<IMyTerminalBlock> findThrusters(string sGroup)
        {
            List<IMyTerminalBlock> lthrusters = new List<IMyTerminalBlock>();
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                if (groups[groupIndex].Name == sGroup)
                {
                    List<IMyTerminalBlock> thrusters = null;
                    groups[groupIndex].GetBlocks(thrusters, localGridFilter);
                    for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
                    {
                        lthrusters.Add(thrusters[thrusterIndex]);
                    }
                    break;
                }
            }
            return lthrusters;
        }
        int powerUpThrusters(List<IMyTerminalBlock> thrusters, float fPower, int iTypes = thrustAll)
        {
            int iCount = 0;
            if (fPower > 100) fPower = 100;
            if (fPower < 0) fPower = 0;
            for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
            {
                int iThrusterType = thrusterType(thrusters[thrusterIndex]);
                if ((iThrusterType & iTypes) > 0)
                {
                    IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
//                    float maxThrust = thruster.GetMaximum<float>("Override");
                    if (!thruster.IsWorking)
                    {
                        thruster.Enabled = true;// ApplyAction("OnOff_On");
                    }
                    iCount += 1;
                    thruster.ThrustOverridePercentage = fPower;
//                    thruster.SetValueFloat("Override", maxThrust * (fPower / 100.0f));
                }
            }
            return iCount;
        }
        int powerUpThrusters(List<IMyTerminalBlock> thrusters, int iPower = 100, int iTypes = thrustAll)
        {
            return powerUpThrusters(thrusters, (float)iPower, iTypes);

        }
        bool powerUpThrusters(string sFThrust, int iPower = 100, int iTypes = thrustAll)
        {
            if (iPower > 100) iPower = 100;
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                if (groups[groupIndex].Name == sFThrust)
                {
                    List<IMyTerminalBlock> thrusters = null;
                    groups[groupIndex].GetBlocks(thrusters, localGridFilter);
                    return (powerUpThrusters(thrusters, iPower, iTypes) > 0);
                }
            }
            return false;
        }
        int powerDownThrusters(List<IMyTerminalBlock> thrusters, int iTypes = thrustAll, bool bForceOff = false)
        {
            int iCount = 0;
            for (int thrusterIndex = 0; thrusterIndex < thrusters.Count; thrusterIndex++)
            {
                int iThrusterType = thrusterType(thrusters[thrusterIndex]);
                if ((iThrusterType & iTypes) > 0)
                {
                    iCount++;
                    IMyThrust thruster = thrusters[thrusterIndex] as IMyThrust;
                    thruster.ThrustOverride = 0;
//                    thruster.SetValueFloat("Override", 0);
                    if (thruster.IsWorking && bForceOff)
                        thruster.Enabled = false;// ApplyAction("OnOff_Off");
                    else if (!thruster.IsWorking && !bForceOff)
                        thruster.Enabled = true;// ApplyAction("OnOff_On");
                }
            }
            return iCount;
        }
        bool powerDownThrusters(string sFThrust)
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>(); GridTerminalSystem.GetBlockGroups(groups); for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                if (groups[groupIndex].Name == sFThrust)
                {
                    List<IMyTerminalBlock> thrusters = null;
                    groups[groupIndex].GetBlocks(thrusters, localGridFilter);
                    return (powerDownThrusters(thrusters) > 0);
                }
            }
            return false;
        }
        bool powerUpThrusters()
        {
            return (powerUpThrusters(thrustForwardList) > 0);
        }
        bool powerDownThrusters()
        {
            return (powerDownThrusters(thrustForwardList) > 0);
        }
        double currentOverrideThrusters(List<IMyTerminalBlock> theBlocks, int iTypes = thrustAll)
        {
            for (int i = 0; i < theBlocks.Count; i++)
            {
                int iThrusterType = thrusterType(theBlocks[i]);
                if ((iThrusterType & iTypes) > 0 && theBlocks[i].IsWorking)
                {
                    IMyThrust thruster = theBlocks[i] as IMyThrust;
                    return thruster.ThrustOverride;
                    /*
                    float maxThrust = thruster.GetMaximum<float>("Override");
                    if (maxThrust > 0)
                        return (double)thruster.ThrustOverride / maxThrust * 100;
                        */
                }
            }
            return 0;
        }
        bool areThrustersOn(List<IMyTerminalBlock> theBlocks, int iTypes = thrustAll)
        {
            for (int i = 0; i < theBlocks.Count; i++)
            {
                int iThrusterType = thrusterType(theBlocks[i]);
                if ((iThrusterType & iTypes) > 0 && theBlocks[i].IsWorking)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        int countThrusters(List<IMyTerminalBlock> theBlocks, int iTypes = thrustAll)
        {

	        int iCount = 0;
	        for (int i = 0; i < theBlocks.Count; i++)
	        {
		        int iThrusterType = thrusterType(theBlocks[i]);
		        if ((iThrusterType & iTypes) > 0 && theBlocks[i].IsWorking)
		        {
			        iCount++;
		        }
	        }
	        return iCount;

        }
        

    }
}