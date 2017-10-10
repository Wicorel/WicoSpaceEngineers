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

        string sensorInit(bool bSleep=true)
        {
            sensorsList.Clear();
            List<IMyTerminalBlock> ltb = new List<IMyTerminalBlock>();
            sensorsList = new List<IMySensorBlock>();
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(ltb, localGridFilter);
            foreach (var sb in ltb)
            {
                sensorsList.Add(sb as IMySensorBlock);
            }
            if(bSleep) sleepAllSensors();
            return "S" + sensorsList.Count.ToString("00");
        }

        List<IMySensorBlock> activeSensors(string sKey = null)
        {
            List<IMySensorBlock> activeSensors = new List<IMySensorBlock>(); ;
            for (int i = 0; i < sensorsList.Count; i++)
            {
                IMySensorBlock s = sensorsList[i] as IMySensorBlock;
                if (s == null) continue;
                if (s.IsActive && s.Enabled && !s.LastDetectedEntity.IsEmpty())
                {
                    Echo("Adding Active:" + s.CustomName + ":" + s.Enabled);
                    activeSensors.Add(sensorsList[i]);
                }
            }
            return activeSensors;
        }

        void sleepAllSensors()
        {
            for (int i = 0; i < sensorsList.Count; i++)
            {
                IMySensorBlock sb = sensorsList[i] as IMySensorBlock;
                if (sb == null) continue;
                sb.LeftExtend = sb.RightExtend = sb.TopExtend = sb.BottomExtend = sb.FrontExtend = sb.BackExtend = 1;
                sb.Enabled = false;
            }
        }

        void setSensorShip(IMyTerminalBlock tb, float fLeft, float fRight, float fUp, float fDown, float fFront, float fBack)
        {
            // need to use world matrix to get orientation correctly
            IMySensorBlock sb = tb as IMySensorBlock;
            if (sb == null) return;
            //		Echo(sb.CustomName);
            //	Echo(sb.Position.ToString());
            //x=width, y=height, z=back/forth. (fw=+z) (right=-y)
            float fXOffset = sb.Position.X * 0.5f; // small grid only?
            float fYOffset = sb.Position.Y * 0.5f;
            float fZOffset = sb.Position.Z * 0.5f;

            float fSet;
            fSet = (float)(shipDim.WidthInMeters() / 2 - fXOffset + fLeft);
            sb.LeftExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.WidthInMeters() / 2 + fXOffset + fRight);
            sb.RightExtend = Math.Max(fSet, 1.0f);

            fSet = (float)(shipDim.HeightInMeters() / 2 - fYOffset + fUp);
            sb.TopExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.HeightInMeters() / 2 + fYOffset + fDown);
            sb.BottomExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.LengthInMeters() + fZOffset + fFront);
            sb.FrontExtend = Math.Max(fSet, 1.0f);
            fSet = (float)(shipDim.LengthInMeters() - fZOffset + fBack);
            sb.BackExtend = Math.Max(fSet, 1.0f);

            sb.Enabled = true;

        }
        #endregion


    }
}