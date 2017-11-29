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

        void Deserialize()
        {
            double x, y, z;

            string sSave;
            if (SaveFile == null)
                sSave = Storage;
            else
                sSave = SaveFile.GetPublicText();

            if (sSave.Length < 1)
            {
                Echo("Saved information not available");
                return;
            }
            sLastLoad = sSave;

            int i = 1;
            float fVersion = 0;

            string[] atheStorage = sSave.Split('\n');

            // Trick using a "local method", to get the next line from the array `atheStorage`.
            Func<string> getLine = () =>
            {
                return (i >= 0 && atheStorage.Length > i ? atheStorage[i++] : null);
            };

            if (atheStorage.Length < 3)
            {
                // invalid storage
                Storage = "";
                Echo("Invalid Storage");
                return;
            }

            // Simple "local method" which returns false/true, depending on if the
            // given `txt` argument contains the text "True" or "true".
            Func<string, bool> asBool = (txt) =>
            {
                txt = txt.Trim().ToLower();
                return (txt == "True" || txt == "true");
            };

            fVersion = (float)Convert.ToDouble(getLine());

            if (fVersion > savefileversion)
            {
                Echo("Save file version mismatch; it is newer. Check programming blocks.");
                return; // it is something NEWER than us..
            }
            if (fVersion < 2.99)
            {
                Echo("Obsolete save. ignoring:" + fVersion.ToString());
                return;
            }
            iMode = Convert.ToInt32(getLine());
            current_state = Convert.ToInt32(getLine());
            currentRun = Convert.ToInt32(getLine());
            sPassedArgument = getLine();

            iAlertStates = Convert.ToInt32(getLine());

            bool pOK;
            pOK = double.TryParse(getLine(), out dGravity);
            long lJunk;
            pOK = long.TryParse(getLine(), out lJunk);
            //    pOK = long.TryParse(getLine(), out allBlocksCount);

            craft_operation = Convert.ToInt32(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vDock = new Vector3D(x, y, z);
            bValidDock = asBool(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vLaunch1 = new Vector3D(x, y, z);
            bValidLaunch1 = asBool(getLine().ToLower());

            ParseVector3d(getLine(), out x, out y, out z);
            vHome = new Vector3D(x, y, z);
            bValidHome = asBool(getLine());

            dtStartShip = DateTime.Parse(getLine());
            dtStartCargo = DateTime.Parse(getLine());
            dtStartSearch = DateTime.Parse(getLine());
            dtStartMining = DateTime.Parse(getLine());
            dtLastRan = DateTime.Parse(getLine());
            dtStartNav = DateTime.Parse(getLine());
            /*
            ParseVector3d(getLine(), out x, out y, out z);
            vLastPos = new Vector3D(x, y, z);
            */
            ParseVector3d(getLine(), out x, out y, out z);
            vInitialContact = new Vector3D(x, y, z);
            bValidInitialContact = asBool(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vInitialExit = new Vector3D(x, y, z);
            bValidInitialExit = asBool(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vLastContact = new Vector3D(x, y, z);

            ParseVector3d(getLine(), out x, out y, out z);
            vLastExit = new Vector3D(x, y, z);

            ParseVector3d(getLine(), out x, out y, out z);
            vExpectedExit = new Vector3D(x, y, z);

            ParseVector3d(getLine(), out x, out y, out z);
            vTargetMine = new Vector3D(x, y, z);
            bValidTarget = asBool(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vTargetAsteroid = new Vector3D(x, y, z);
            bValidAsteroid = asBool(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vNextTarget = new Vector3D(x, y, z);
            bValidNextTarget = asBool(getLine());

            ParseVector3d(getLine(), out x, out y, out z);
            vCurrentNavTarget = new Vector3D(x, y, z);

            bAutopilotSet = asBool(getLine());
            bAutoRelaunch = asBool(getLine());

            iDetects = Convert.ToInt32(getLine());

            batterypcthigh = Convert.ToInt32(getLine());
            batterypctlow = Convert.ToInt32(getLine());
            batteryPercentage = Convert.ToInt32(getLine());

            cargopctmin = Convert.ToInt32(getLine());
            cargopcent = Convert.ToInt32(getLine());
            cargoMult = Convert.ToDouble(getLine());

            hydroPercent = Convert.ToDouble(getLine());
            oxyPercent = Convert.ToDouble(getLine());

            totalMaxPowerOutput = Convert.ToDouble(getLine());
            maxReactorPower = Convert.ToDouble(getLine());
            maxSolarPower = Convert.ToDouble(getLine());
            maxBatteryPower = Convert.ToDouble(getLine());

            sReceivedMessage = getLine();
        }

        bool stringToBool(string txt)
        {
            txt = txt.Trim().ToLower();
            return (txt == "True" || txt == "true");
        }
    }
}