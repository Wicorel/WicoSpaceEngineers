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
        // 110517 search order for the text panels
        // 2/25: Performance: only check blocks once, re-check on init.
        // use cached blocks 12/xx
        #region logging

        string sLongStatus = "Wico Craft Log";
        string sTextPanelReport = "Craft Report";
        // from Techniker
        IMyTextPanel textRangeReport = null;
        string sRangeReport = "Corner LCD 1";

        IMyTextPanel statustextblock = null;
        IMyTextPanel textLongStatus = null;
        IMyTextPanel textPanelReport = null;
        bool bLoggingInit = false;

        void initLogging()
        {
            statustextblock = getTextStatusBlock(true);
            textLongStatus = getTextBlock(sLongStatus); ;
            textPanelReport = getTextBlock(sTextPanelReport);
	        if(textPanelReport==null)
	        {
            	List<IMyTerminalBlock> lmtb = new List<IMyTerminalBlock>();
		        lmtb = GetBlocksContains<IMyTerminalBlock>("Craft Report");
		        if (lmtb.Count > 0)
			        textPanelReport = lmtb[0] as IMyTextPanel;
	        }

	        textRangeReport = getTextBlock(sRangeReport);
            bLoggingInit = true;
        }

        IMyTextPanel getTextBlock(string stheName)
        {
            IMyTextPanel textblock = null;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blocks = GetBlocksNamed<IMyTerminalBlock>(stheName);
            if (blocks.Count < 1)
            {
                blocks = GetMeBlocksContains<IMyTextPanel>(stheName);
                if (blocks.Count < 1)
                    blocks = GetBlocksContains<IMyTextPanel>(stheName);
            }
            if (blocks.Count > 1)
                throw new OurException("Multiple status blocks found: \"" + stheName + "\"");
            else
                if (blocks.Count > 0)
                textblock = blocks[0] as IMyTextPanel;
            return textblock;
        }

        IMyTextPanel getTextStatusBlock(bool force_update = false)
        {
            if ((statustextblock != null || bLoggingInit) && !force_update) return statustextblock;
            statustextblock = getTextBlock(OurName + " Status");
            return statustextblock;
        }
        void StatusLog(string text, IMyTextPanel block, bool bReverse = false)
        {
            if (block == null) return;
            if (text.Equals("clear"))
            {
                block.WritePublicText("");
            }
            else
            {
                if (bReverse)
                {
                    string oldtext = block.GetPublicText();
                    block.WritePublicText(text + "\n" + oldtext);
                }
                else block.WritePublicText(text + "\n", true);
                // block.WritePublicTitle(DateTime.Now.ToString());
            }
            block.ShowTextureOnScreen();
            block.ShowPublicTextOnScreen();
        }

        void Log(string text)
        {
            StatusLog(text, getTextStatusBlock());
        }
        string progressBar(double percent)
        {
            int barSize = 75;
            if (percent < 0) percent = 0;
            int filledBarSize = (int)(percent * barSize) / 100;
            if (filledBarSize > barSize) filledBarSize = barSize;
            string sResult = "[" + new String('|', filledBarSize) + new String('\'', barSize - filledBarSize) + "]";
            return sResult;
        }

        #endregion
        //////

        string niceDoubleMeters(double thed)
        {
            string nice = "";
            if (thed > 1000)
            {
                nice = thed.ToString("N0") + "km";
            }
            else if (thed > 10)
            {
                nice = thed.ToString("0.0") + "m";
            }
            else
            {
                nice = thed.ToString("0.000") + "m";
            }
            return nice;
        }



    }
}