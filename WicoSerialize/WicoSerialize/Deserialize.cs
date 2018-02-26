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

        long SavedTextPanelID = 0;

        void Deserialize()
        {
            string sSave;
            if (SaveFile == null)
            {
                sSave = Storage;
            }
            else
            {
                sSave = SaveFile.GetPublicText();
            }

            if (iniWicoCraftSave == null) return;
        
            /*
            if (sSave.Length < 1)
            {
                Echo("Saved information not available");
                return;
            }
            */

            iniWicoCraftSave.ParseINI(sSave);
            iniWicoCraftSave.GetValue(sSerializeSection, "SaveID", ref SavedTextPanelID);

            if (DifferentSaveFile()) // if the cached ID does not match, we are a new ship. Do not load old saved info; re-init
                iniWicoCraftSave.ParseINI("");

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
            if (SaveFile == null) return false;

            if (SavedTextPanelID == (long)SaveFile.EntityId)
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