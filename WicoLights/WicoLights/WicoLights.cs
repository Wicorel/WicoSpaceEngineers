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
        #region lights
        List<IMyTerminalBlock> lightsList = new List<IMyTerminalBlock>();

        string lightsInit()
        {
            lightsList.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(lightsList, localGridFilter);

            return "L" + lightsList.Count.ToString("00");
        }

        void setLightColor(List<IMyTerminalBlock> lightsList, Color c)
        {
            //	Echo("sLC:" + lightsList.Count + ":" + c.ToString());
            for (int i = 0; i < lightsList.Count; i++)
            {
                var light = lightsList[i] as IMyLightingBlock;
                if (light == null) continue;

                if (light.GetValue<Color>("Color").Equals(c) && light.Enabled)
                {
                    continue;
                }

                light.SetValue("Color", c);
                // make sure we switch the color of the texture as well
                light.ApplyAction("OnOff_Off");
                light.ApplyAction("OnOff_On");
            }
        }
        #endregion


    }
}