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
        #region simplesensors

        List<IMySensorBlock> sensorsList = new List<IMySensorBlock>();

        string sSensorUse = "[WICO]";
        double dSensorSettleWaitMS = 0.125;
        const string sSensorSection = "SENSORS";

        void SensorInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sSensorSection, "SensorUse", ref sSensorUse, true);
            iNIHolder.GetValue(sSensorSection, "SensorSettleWaitMS", ref dSensorSettleWaitMS, true);
        }

        string sensorInit(bool bSleep=true)
        {
            sensorsList.Clear();
            /*
            List<IMyTerminalBlock> ltb = new List<IMyTerminalBlock>();
            sensorsList = new List<IMySensorBlock>();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(ltb, localGridFilter);
            */
            List<IMyTerminalBlock> ltb = GetBlocksContains<IMySensorBlock>(sSensorUse);

            foreach (var sb1 in ltb)
            {
 //               if (sb1.CustomName.Contains("WICO") || sb1.CustomData.Contains("WICO")) ;
                sensorsList.Add(sb1 as IMySensorBlock);
            }
            if(bSleep) sleepAllSensors();
            return "S" + sensorsList.Count.ToString("00");
        }

        List<IMySensorBlock> activeSensors(string sKey = null)
        {
            List<IMySensorBlock> activeSensors = new List<IMySensorBlock>();
            for (int i1 = 0; i1 < sensorsList.Count; i1++)
            {
                IMySensorBlock s = sensorsList[i1] as IMySensorBlock;
                if (s == null) continue;
                if (s.IsActive && s.Enabled && !s.LastDetectedEntity.IsEmpty())
                {
//                    Echo("Adding Active:" + s.CustomName + ":" + s.Enabled);
                    activeSensors.Add(sensorsList[i1]);
                }
            }
            return activeSensors;
        }

        void sleepAllSensors()
        {
            for (int i1 = 0; i1 < sensorsList.Count; i1++)
            {
                IMySensorBlock sb1 = sensorsList[i1] as IMySensorBlock;
                if (sb1 == null) continue;
                sb1.LeftExtend = sb1.RightExtend = sb1.TopExtend = sb1.BottomExtend = sb1.FrontExtend = sb1.BackExtend = 1;
                sb1.Enabled = false;
            }
        }

        void setSensorShip(IMyTerminalBlock tb, float fLeft, float fRight, float fUp, float fDown, float fFront, float fBack)
        {
            // need to use world matrix to get orientation correctly
            IMySensorBlock sb1 = tb as IMySensorBlock;
            if (sb1 == null) return;
            //		Echo(sb.CustomName);
            //	Echo(sb.Position.ToString());
            //x=width, y=height, z=back/forth. (fw=+z) (right=-y)
            float fXOffset = sb1.Position.X * 0.5f; // small grid only?
            float fYOffset = sb1.Position.Y * 0.5f;
            float fZOffset = sb1.Position.Z * 0.5f;
//            Echo("SB.x.y.z=" + fXOffset.ToString("0.0") + ":" + fYOffset.ToString("0.0") + ":" + fZOffset.ToString("0.0"));

//            Echo("MIN=" + Me.CubeGrid.Min.ToString() + "\nMAX:" + Me.CubeGrid.Max.ToString());
            // TODO: need to use grid orientation to main orientation block
            float fSet;
            fSet = (float)(shipDim.WidthInMeters() / 2 - fXOffset + fLeft);
            sb1.LeftExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.WidthInMeters() / 2 + fXOffset + fRight);
            sb1.RightExtend = Math.Max(fSet, 1.0f);

            fSet = (float)(shipDim.HeightInMeters() / 2 - fYOffset + fUp);
            sb1.TopExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.HeightInMeters() / 2 + fYOffset + fDown);
            sb1.BottomExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.LengthInMeters() + fZOffset + fFront);
            sb1.FrontExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.LengthInMeters() - fZOffset + fBack);
            sb1.BackExtend = Math.Max(fSet, 1.0f);

            sb1.Enabled = true;

        }

        bool SensorActive(IMySensorBlock s1, ref bool bAsteroidFound, ref bool bLargeFound, ref bool bSmallFound)
        {
 //           bool bAnyFound = false;
            bAsteroidFound=false;
            bLargeFound=false;
            bSmallFound=false;

            if (s1!=null && s1.IsActive && s1.Enabled && !s1.LastDetectedEntity.IsEmpty())
            {
                List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
                s1.DetectedEntities(lmyDEI);
                for (int j1 = 0; j1 < lmyDEI.Count; j1++)
                {
                    if (lmyDEI[j1].Type == MyDetectedEntityType.Asteroid)
                    {
                        bAsteroidFound = true;
                    }
                    else if (lmyDEI[j1].Type == MyDetectedEntityType.LargeGrid)
                    {
                        bLargeFound = true;
                    }
                    else if (lmyDEI[j1].Type == MyDetectedEntityType.SmallGrid)
                    {
                        bSmallFound = true;
                    }
                }
            }
            return bAsteroidFound || bLargeFound || bSmallFound;// bAnyFound;
        }

        #endregion


    }
}