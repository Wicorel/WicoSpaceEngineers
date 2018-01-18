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
        bool bVerboseSerialize=false;

        // 122317 INI processing
        // 1105 allow save file to use contains()
        // V3.0 - redo all variables & cleanup
        #region serializecommon
        string SAVE_FILE_NAME = "Wico Craft Save";
        string sSerializeSection = "SERIALIZE";
        void SerializeInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sSerializeSection, "SAVE_FILE_NAME", ref SAVE_FILE_NAME, true);
        }

        float savefileversion = 3.00f;
        IMyTextPanel SaveFile = null;

        INIHolder iniWicoCraftSave;

        // Saved info:
        int current_state = 0;

        long allBlocksCount = 0;

        Vector3D vCurrentPos;
        Vector3D vDock;
        Vector3D vLaunch1;
        Vector3D vHome;
        bool bValidDock = false;
        bool bValidLaunch1 = false;
        bool bValidHome = false;
        double dGravity = -2;
        int craft_operation = CRAFT_MODE_AUTO;
        int currentRun = 0;
        string sPassedArgument = "";

        // valid vectors
        bool bValidInitialContact = false;
        bool bValidInitialExit = false;
        bool bValidTarget = false;
        bool bValidAsteroid = false;
        bool bValidNextTarget = false;

        // operation flags
        bool bAutopilotSet = true;
        bool bAutoRelaunch = false;

        // 
        int iAlertStates = 0;

        // time outs
        DateTime dtStartShip;
        DateTime dtStartCargo;
        DateTime dtStartSearch;
        DateTime dtStartMining;
        DateTime dtLastRan;
        DateTime dtStartNav;

        // positions
        //Vector3D vLastPos;
        Vector3D vInitialContact;
        Vector3D vInitialExit;
        Vector3D vLastContact;
        Vector3D vLastExit;
        Vector3D vTargetMine;
        Vector3D vTargetAsteroid;
        Vector3D vCurrentNavTarget;
        Vector3D vNextTarget;
        Vector3D vExpectedExit;

        // detection
        int iDetects = 0;
        int batterypcthigh = 80;
        int batterypctlow = 20;
        int batteryPercentage = -1;

        int cargopctmin = 5;
        int cargopcent = -1;
        double cargoMult = -1;

        // tanks
        double hydroPercent = -1;
        double oxyPercent = -1;

        double totalMaxPowerOutput = 0;
        double maxReactorPower = -1;
        double maxSolarPower = -1;
        double maxBatteryPower = -1;

        string sReceivedMessage = "";

        string initSerializeCommon()
        {

            string sInitResults = "S";

            SaveFile = null;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blocks = GetBlocksNamed<IMyTextPanel>(SAVE_FILE_NAME);

            if (blocks.Count > 1) Echo("Multiple blocks found: \"" + SAVE_FILE_NAME + "\"");
            else if (blocks.Count == 0)
            { 
                blocks = GetBlocksContains<IMyTextPanel>(SAVE_FILE_NAME);
                if (blocks.Count == 1)
                    SaveFile = blocks[0] as IMyTextPanel;
                else
                {
                    blocks = GetMeBlocksContains<IMyTextPanel>(SAVE_FILE_NAME);
                    if (blocks.Count == 1)
                        SaveFile = blocks[0] as IMyTextPanel;
                }
            }
            else SaveFile = blocks[0] as IMyTextPanel;
            iniWicoCraftSave = new INIHolder(this, "");

            if (SaveFile == null)
            {
                sInitResults = "-";
                Echo(SAVE_FILE_NAME + " (TextPanel) is missing or Named incorrectly. ");
            }
            return sInitResults;
        }

        bool validSavefile()
        {
            return SaveFile != null;
        }
        string Vector3DToString(Vector3D v)
        {
            string s;
            s = v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00");
            //    s = v.GetDim(0) + ":" + v.GetDim(1) + ":" + v.GetDim(2);
            return s;
        }
        bool ParseVector3d(string sVector, out double x, out double y, out double z)
        {
            string[] coordinates = sVector.Trim().Split(',');
            if (coordinates.Length < 3)
            {
                coordinates = sVector.Trim().Split(':');
            }
            x = 0;
            y = 0;
            z = 0;
            if (coordinates.Length < 3) return false;

            bool xOk = double.TryParse(coordinates[0].Trim(), out x);
            bool yOk = double.TryParse(coordinates[1].Trim(), out y);
            bool zOk = double.TryParse(coordinates[2].Trim(), out z);
            if (!xOk || !yOk || !zOk)
            {
                return false;
            }
            return true;
        }

        #endregion

        // state variables
        string sLastLoad = "";

    }
}