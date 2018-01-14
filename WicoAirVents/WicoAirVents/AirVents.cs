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
        /* 
         * On a planet with o2, a air vent will show oxygen levels, even when OFF. 5.4%=earth 
         * alien=0.06% 
         *  
         *  
         * In space, can vent isolated tank by filling airlock, then opening doors and dumping o2 
         *  
         * Can create no o2 in (sealed) rooms on planets by depresurrizing 
         */

        #region airvents 

        List<IMyTerminalBlock> airventList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> hangarairventList = new List<IMyTerminalBlock>(); // user controled area 
        List<IMyTerminalBlock> airlockairventList = new List<IMyTerminalBlock>(); // system vent in airlock area; used to pressurize if isolated is empty 
        List<IMyTerminalBlock> isolatedairlockairventList = new List<IMyTerminalBlock>(); // connected isolated air tank used to cycle airlock 

        List<IMyTerminalBlock> bridgeairventList = new List<IMyTerminalBlock>(); // crew/bridge area. should always be pressurized 

        List<IMyTerminalBlock> outsideairventList = new List<IMyTerminalBlock>(); // outside air. detect if planet and depressurize to make Free O2. open doors early/always. detect if space and keep doors closed 

        List<IMyTerminalBlock> cockpitairventList = new List<IMyTerminalBlock>(); // outside air. directly supplying a cockpit. turn off if cockpit not occupied. 

        bool bPressurization = false; // pressurization enabled in world settings
        bool bAVInit = false;


        string airventInit()
        {
            airventList.Clear();
            bAVInit = false;
            GetTargetBlocks<IMyAirVent>(ref airventList);

            //            GridTerminalSystem.GetBlocksOfType<IMyAirVent>(airventList, localGridFilter);
 //           Echo("AV#=" + airventList.Count);

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
            bAVInit = true;
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

                    //                    if (av.IsDepressurizing && dGravity > .75)
                    if (av.Status == VentStatus.Depressurizing && dGravity > .75)
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
                    //                   if (av.IsDepressurizing)
                    if (av.Status == VentStatus.Depressurizing)
                        av.Enabled = false;// ApplyAction("OnOff_Off");
                }
            }
        }

        bool isPressurizationOn()
        {
            if (!bAVInit)
                airventInit();
            if (airventList.Count < 1) // no air vents to check
                return false;
            IMyAirVent av = airventList[0] as IMyAirVent;
            if (av == null) return false;
            return av.PressurizationEnabled;
            /*
             * pre 1.185
            if (airventList[0].DetailedInfo.Contains("Oxygen disabled in world settings"))
            {
                return false;
            }
            return true;
            */
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
                    if (av.Status == VentStatus.Depressurizing) s += "D";
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

        #region textpanels

        List<IMyTerminalBlock> textpanelList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> textpanelAirlockPressurizingList = new List<IMyTerminalBlock>(); //airlock pressurizing warning
        List<IMyTerminalBlock> textpanelAirlockWarningList = new List<IMyTerminalBlock>(); //LCD Panel airlock warning

        // Sabrina 2.0
        List<IMyTerminalBlock> textpanelAirlockOutter = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> textpanelAirlockInner = new List<IMyTerminalBlock>();

        string textpaneltInit()
        {
            textpanelList.Clear();
            textpanelAirlockPressurizingList.Clear();
            textpanelAirlockWarningList.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(textpanelList, localGridFilter);

            for (int i = 0; i < textpanelList.Count; i++)
            {

                if (textpanelList[i].CustomName.ToLower().Contains("pressurizing"))
                    textpanelAirlockPressurizingList.Add(textpanelList[i]);
                if (textpanelList[i].CustomName.ToLower().Contains("airlock warning"))
                    textpanelAirlockWarningList.Add(textpanelList[i]);
                if (textpanelList[i].CustomName.ToLower().Contains("airlock 2")) //           AIRLOCK
                    textpanelAirlockOutter.Add(textpanelList[i]);
                if (textpanelList[i].CustomName.ToLower().Contains("airlock 1")) //           HANGAR
                    textpanelAirlockInner.Add(textpanelList[i]);

                /*
            if (textpanelList[i].CustomData.ToLower().Contains("pressurizing"))
                textpanelAirlockPressurizingList.Add(textpanelList[i]);
            if (textpanelList[i].CustomData.ToLower().Contains("airlock warning"))
                textpanelAirlockWarningList.Add(textpanelList[i]);
            if (textpanelList[i].CustomData.ToLower().Contains("airlock outter")) //           AIRLOCK
                textpanelAirlockOutter.Add(textpanelList[i]);
            if (textpanelList[i].CustomData.ToLower().Contains("airlock inner")) //           HANGAR
                textpanelAirlockInner.Add(textpanelList[i]);
                */
            }
            return "T" + "P" + textpanelAirlockWarningList.Count.ToString("0") + "W" + textpanelAirlockWarningList.Count.ToString("0");
        }

        // RGB: 255:150:0

        #endregion
        void doCheckAirVents()
        {
	        // handle turning off air vents if pressurization is off to save power.
	        if (bPressurization)
	        {
                foreach (var a1 in airventList)
                    if(a1 is IMyFunctionalBlock)
                    {
                        var f1 = a1 as IMyFunctionalBlock;
                        f1.Enabled = true;
                    }
	        }
	        else // no pressurization or no vents.
	        {
		        if (airventList.Count > 0)
		        {
			        StatusLog("Pressurization turned OFF\n in World Settings\n", textPanelReport);
                    foreach (var a1 in airventList)
                        if (a1 is IMyFunctionalBlock)
                        {
                            var f1 = a1 as IMyFunctionalBlock;
                            f1.Enabled = true;
                        }
                }
            }

        }

    }
}