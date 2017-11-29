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
            string sb = "";
            sb += "Wico Craft Controller Saved State Do Not Edit" + "\n";
            sb += savefileversion.ToString("0.00") + "\n";

            sb += iMode.ToString() + "\n";
            sb += current_state.ToString() + "\n";
            sb += currentRun.ToString() + "\n";
            sb += sPassedArgument + "\n";
            sb += iAlertStates.ToString() + "\n";
            sb += dGravity.ToString() + "\n";

            sb += allBlocksCount.ToString() + "\n";

            sb += craft_operation.ToString() + "\n";


            sb += Vector3DToString(vDock) + "\n";
            sb += bValidDock.ToString() + "\n";

            sb += Vector3DToString(vLaunch1) + "\n";
            sb += bValidLaunch1.ToString() + "\n";

            sb += Vector3DToString(vHome) + "\n";
            sb += bValidHome.ToString() + "\n";

            sb += dtStartShip.ToString() + "\n";
            sb += dtStartCargo.ToString() + "\n";
            sb += dtStartSearch.ToString() + "\n";
            sb += dtStartMining.ToString() + "\n";
            sb += dtLastRan.ToString() + "\n";
            sb += dtStartNav.ToString() + "\n";

            //	sb += Vector3DToString(vLastPos) + "\n";
            sb += Vector3DToString(vInitialContact) + "\n";
            sb += bValidInitialContact.ToString() + "\n";

            sb += Vector3DToString(vInitialExit) + "\n";
            sb += bValidInitialExit.ToString() + "\n";

            sb += Vector3DToString(vLastContact) + "\n";
            sb += Vector3DToString(vLastExit) + "\n";
            sb += Vector3DToString(vExpectedExit) + "\n";

            sb += Vector3DToString(vTargetMine) + "\n";
            sb += bValidTarget.ToString() + "\n";

            sb += Vector3DToString(vTargetAsteroid) + "\n";
            sb += bValidAsteroid.ToString() + "\n";

            sb += Vector3DToString(vNextTarget) + "\n";
            sb += bValidNextTarget.ToString() + "\n";

            sb += Vector3DToString(vCurrentNavTarget) + "\n";


            sb += bAutopilotSet.ToString() + "\n";
            sb += bAutoRelaunch.ToString() + "\n";
            sb += iDetects.ToString() + "\n";

            sb += batterypcthigh.ToString() + "\n";
            sb += batterypctlow.ToString() + "\n";
            sb += batteryPercentage.ToString() + "\n";

            sb += cargopctmin.ToString() + "\n";
            sb += cargopcent.ToString() + "\n";
            sb += cargoMult.ToString() + "\n";

            sb += hydroPercent.ToString() + "\n";
            sb += oxyPercent.ToString() + "\n";

            sb += totalMaxPowerOutput.ToString() + "\n";
            sb += maxReactorPower.ToString() + "\n";
            sb += maxSolarPower.ToString() + "\n";
            sb += maxBatteryPower.ToString() + "\n";
            sb += sReceivedMessage + "\n";

            if (SaveFile == null)
            {
                Storage = sb.ToString();
                return;
            }
            if (sLastLoad != sb)
            {
                SaveFile.WritePublicText(sb.ToString(), false);
            }
            else
            {
                if (bVerboseSerialize) Echo("Not saving: Same");
            }

        }
    }
}