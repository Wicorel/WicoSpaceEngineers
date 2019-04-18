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

        void Serialize()
        {
            if (iniWicoCraftSave == null) return;

            ModuleSerialize(iniWicoCraftSave);

            iniWicoCraftSave.SetValue(sSerializeSection, "Mode", iMode.ToString());
            iniWicoCraftSave.SetValue(sSerializeSection, "current_state", current_state.ToString());
            iniWicoCraftSave.SetValue(sSerializeSection, "PassedArgument", sPassedArgument);
            iniWicoCraftSave.SetValue(sSerializeSection, "AlertStates", iAlertStates.ToString());
            iniWicoCraftSave.SetValue(sSerializeSection, "craft_operation", craft_operation.ToString());
//            iniWicoCraftSave.SetValue(sSerializeSection, "PassedArgument", sPassedArgument);
            iniWicoCraftSave.SetValue(sSerializeSection, "ReceivedMessage", sReceivedMessage);
            long SaveID = 0;
            if (SaveFile != null) SaveID = SaveFile.EntityId;
            iniWicoCraftSave.SetValue(sSerializeSection, "SaveID", (long)SaveID);

            //            Echo("Writing ReceivedMessage='" + sReceivedMessage + "'");

            if ( iniWicoCraftSave.IsDirty) // others may have changed it, so check if any section is dirty
            {
                if (iniWicoCraftSave.IsDirty)
                {
                    string sINI = iniWicoCraftSave.GenerateINI();
                    if (SaveFile == null)
                    {
                        //                if (bVerboseSerialize)
                        Echo("WARNING: saving to Storage");
                        Storage = sINI;
                    }
                    else
                    {
                        //                    SaveFile.WritePublicText(sb.ToString(), false);
                        SaveFile.WriteText(sINI, false);
                        // Depracated V1.190       
                        //SaveFile.WritePublicText(sINI, false);
                    }
                }
            }
            else
            {
//                if (bVerboseSerialize)
                    Echo("Not saving: Same");
            }

        }
    }
}