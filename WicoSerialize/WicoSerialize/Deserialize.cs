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
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        long SavedTextPanelID = 0;

        MyIni _SaveInit = new MyIni();
        string sLastLoad = "";
        string sLoad="";

        void Deserialize()
        {
            if (SaveFile == null)
            {
                sLoad = Storage;
            }
            else
            {
                sLoad = SaveFile.GetText();
                //Depracated V1.190                
                //sLoad = SaveFile.GetPublicText();
            }


            if (iniWicoCraftSave == null) return;

            /*
            if (sSave.Length < 1)
            {
                Echo("Saved information not available");
                return;
            }
            */

            // optimize.  if the same, don't bother to re parse.
            if (sLoad == sLastLoad)
            {
                Echo("Load Skip");
                return;
            }
 // DEBUG           Echo("Load Count=" + sLoad.Length);
            sLastLoad = sLoad;

            sLoad=sLoad.Trim();
            MyIniParseResult result;
            if (!_SaveInit.TryParse(sLoad, out result))
            {
                Echo("MyIni:Error parsing INI:" + result.ToString());

                // walk through all of the lines
                string[] aLines = sLoad.Split('\n');

                for (int iLine = 0; iLine < aLines.Count(); iLine++)
                {
                    Echo(iLine + 1 + ":" + aLines[iLine]);
                }

                // TODO: use MyIni here:

//                Echo("str=\n" + sLoad);
//                return;
            }

            iniWicoCraftSave.ParseINI(sLoad);
            iniWicoCraftSave.GetValue(sSerializeSection, "SaveID", ref SavedTextPanelID);

            if (DifferentSaveFile()) // if the cached ID does not match, we are a new ship. Do not load old saved info; re-init
            {
                // clear and reset
//                sStartupError += "\nDIFFERENT ID:RESET SAVE";
                iniWicoCraftSave.ParseINI("");
            }

            ModuleDeserialize(iniWicoCraftSave);

            iniWicoCraftSave.GetValue(sSerializeSection, "Mode", ref iMode, true);
            iniWicoCraftSave.GetValue(sSerializeSection, "current_state", ref current_state, true);
            iniWicoCraftSave.GetValue(sSerializeSection, "PassedArgument", ref sPassedArgument, true);
            iniWicoCraftSave.GetValue(sSerializeSection, "AlertStates", ref iAlertStates, true);
            iniWicoCraftSave.GetValue(sSerializeSection, "craft_operation", ref craft_operation, true);
            iniWicoCraftSave.GetValue(sSerializeSection, "PassedArgument", ref sPassedArgument);
            iniWicoCraftSave.GetValue(sSerializeSection, "ReceivedMessage", ref sReceivedMessage);

            //            Echo("Received Msg='" + sReceivedMessage + "'");
        }

        bool DifferentSaveFile()
        {
            if (SaveFile == null || bIAmSubModule) return false;
            
            if (
                SavedTextPanelID <= 0 // we have loaded one from info
                || SavedTextPanelID == (long)SaveFile.EntityId
                )
                return false;
            else
                return true;
        }

        bool stringToBool(string txt)
        {
            txt = txt.Trim().ToLower();
            return (txt == "True" || txt == "true");
        }
        
    }
}