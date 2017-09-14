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
        #region airvents 

        List<IMyTerminalBlock> airventList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> hangarairventList = new List<IMyTerminalBlock>(); // user controled area 
        List<IMyTerminalBlock> airlockairventList = new List<IMyTerminalBlock>(); // system vent in airlock area; used to pressurize if isolated is empty 
        List<IMyTerminalBlock> isolatedairlockairventList = new List<IMyTerminalBlock>(); // connected isolated air tank used to cycle airlock 

        List<IMyTerminalBlock> bridgeairventList = new List<IMyTerminalBlock>(); // crew/bridge area. should always be pressurized 

        List<IMyTerminalBlock> outsideairventList = new List<IMyTerminalBlock>(); // outside air. detect if planet and depressurize to make Free O2. open doors early/always. detect if space and keep doors closed 

        List<IMyTerminalBlock> cockpitairventList = new List<IMyTerminalBlock>(); // outside air. directly supplying a cockpit. turn off if cockpit not occupied. 

        bool bPressurization = false;

        string airventInit()
        {
            airventList.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyAirVent>(airventList, (x => x.CubeGrid == Me.CubeGrid));

            for (int i = 0; i < airventList.Count; i++)
            {
                if (airventList[i].CustomName.ToLower().Contains("hangar") || airventList[i].CustomData.ToLower().Contains("hangar"))
                    hangarairventList.Add(airventList[i]);
                if (airventList[i].CustomName.ToLower().Contains("outside") || airventList[i].CustomData.ToLower().Contains("outside"))
                    outsideairventList.Add(airventList[i]);
                if (airventList[i].CustomName.ToLower().Contains("bridge") || airventList[i].CustomData.ToLower().Contains("bridge"))
                    bridgeairventList.Add(airventList[i]);
                if (airventList[i].CustomName.ToLower().Contains("crew") || airventList[i].CustomData.ToLower().Contains("crew"))
                    bridgeairventList.Add(airventList[i]);

                if (airventList[i].CustomName.ToLower().Contains("isolated") || airventList[i].CustomData.ToLower().Contains("isolated"))
                    isolatedairlockairventList.Add(airventList[i]);
                else if (airventList[i].CustomName.ToLower().Contains("airlock") || airventList[i].CustomData.ToLower().Contains("airlock"))
                    airlockairventList.Add(airventList[i]);

                if (airventList[i].CustomName.ToLower().Contains("cockpit") || airventList[i].CustomData.ToLower().Contains("cockpit"))
                    cockpitairventList.Add(airventList[i]);

            }
            isPressurizationOn();
            return "A" + airventList.Count.ToString("0");
        }

        void airventOccupied()
        {
            for (int i = 0; i < cockpitairventList.Count; i++)
            {
                IMyAirVent av;
               
                av = airventList[i] as IMyAirVent;
                if (av != null)
                {
                    if (av.IsDepressurizing && dGravity > .75)
                        av.Enabled = true;// ApplyAction("OnOff_On");
                }
            }
        }
        void airventUnoccupied()
        {
            for (int i = 0; i < cockpitairventList.Count; i++)
            {
                IMyAirVent av;
                av = airventList[i] as IMyAirVent;
                if (av != null)
                {
                    if (av.IsDepressurizing)
                        av.Enabled = false;// ApplyAction("OnOff_Off");
                }
            }
        }

        bool isPressurizationOn()
        {
            bPressurization = false;
            if (airventList.Count < 1)
                airventInit();
            if (airventList.Count < 1) // no air vents to check
                return bPressurization;
            if (airventList[0].DetailedInfo.Contains("Oxygen disabled in world settings")) { return bPressurization; }
            bPressurization = true;
            return bPressurization;
        }

        string ventStatus(List<IMyTerminalBlock> airventList, int maxCount = 99)
        {
            string s = "";
            IMyAirVent av;

            int count = Math.Min(maxCount, airventList.Count);
            for (int i = 0; i < count; i++)
            {
                av = airventList[i] as IMyAirVent;
                if (av != null)
                {
                    s += av.CustomName + " ";
                    if (av.IsWorking) s += "On  ";
                    else s += "Off ";
                    //			if(av.IsPressurized()) s+="P"; 
                    //			else s+="-"; 
                    if (av.CanPressurize) s += "C";
                    else s += "l";
                    if (av.IsDepressurizing) s += "D";
                    else s += "P";
                    float airLevel = av.GetOxygenLevel();
                    s += " " + (airLevel * 100).ToString("0.0") + "%";
                    //		StatusLog(s + "\n"+progressBar(airLevel*100),textPanelReport);
                    if (i > 0) s += "\n";
                }
            }
            return s;
        }

        #endregion
    }
}