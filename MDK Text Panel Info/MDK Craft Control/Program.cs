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
        Program()
        {
            if (!Me.Enabled)
            {
                Echo("I am turned OFF!");
            }

        }

        bool bInit = false;
        int allBlocksCount = 0;

        void Main(string sArg, UpdateType ut)
        {
            if (!bInit || sArg=="init")
            {
                gridsInit();
                bInit = true;
            }

            List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            GetTargetBlocks<IMyTextPanel>(ref textPanels);

            string sReport = "";
            sReport = Me.CubeGrid.CustomName + ":" + Me.CubeGrid.EntityId+"\n";
            sReport += Me.CubeGrid.GetPosition().ToString() + "\n";
            sReport += "-----TEXTPANELS\n";
            foreach(var txp in textPanels)
            {
                if (!txp.ShowText)
                {
                    var strings=new List<string>();
                    txp.GetSelectedImages(strings);
                    sReport += txp.EntityId + ":" +strings.Count+":"+txp.CustomName+"\n";
                    foreach (var str in strings)
                        sReport += "|" + str + "\n";
                }
                else
                {
                    sReport += txp.EntityId + ":TEXT!:+"+txp.CustomName+"\n";
                    sReport += txp.GetPublicText() + "\n";
                }
            }


            List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
            GetTargetBlocks<IMySoundBlock>(ref soundBlocks);

            sReport += "-----SOUND\n";
            foreach(var sound in soundBlocks)
            {
                sReport += sound.EntityId + ":" + sound.CustomName+":"+sound.SelectedSound + "\n";
            }
            Me.CustomData = sReport;
        }
    }
}