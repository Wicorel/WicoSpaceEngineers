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
        string sDockedSection = "DOCK";

        bool bAllowStaticDock = true;
        double LaunchMaxVelocity = 20;
        double LaunchDistance = 45;

        bool bAutoRelaunch = false;

        bool bStaticValid = false; // do we have a fixed valid docking location
        Vector3D vStaticDock; // the docking location
        Vector3D vStaticLaunch;
        Vector3D vStaticHome;

        DateTime dtRelaunchActionStart;

        void DockedInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sDockedSection, "AllowStaticDocking", ref bAllowStaticDock, true);
            iNIHolder.GetValue(sDockedSection, "LaunchMaxVelocity", ref LaunchMaxVelocity, true);
            iNIHolder.GetValue(sDockedSection, "LaunchDistance", ref LaunchDistance, true);
        }

        void DockedSerialize(INIHolder iNIHolder)
        {
            //TODO: remember docked connector
            iNIHolder.SetValue(sDockedSection, "AutoRelaunch", bAutoRelaunch);
            iNIHolder.SetValue(sDockedSection, "ActionStart", dtRelaunchActionStart);
            iNIHolder.SetValue(sDockedSection, "StaticValid", bStaticValid);
            iNIHolder.SetValue(sDockedSection, "StaticDock", vStaticDock);
            iNIHolder.SetValue(sDockedSection, "StaticLaunch", vStaticLaunch);
            iNIHolder.SetValue(sDockedSection, "StaticHome", vStaticHome);
        }

        void DockedDeserialize(INIHolder iNIHolder)
        {
            //TODO: remember docked connector
            iNIHolder.GetValue(sDockedSection, "AutoRelaunch", ref bAutoRelaunch, true);
            iNIHolder.GetValue(sDockedSection, "ActionStart", ref dtRelaunchActionStart);
            iNIHolder.GetValue(sDockedSection, "StaticValid", ref bStaticValid);
            iNIHolder.GetValue(sDockedSection, "StaticDock", ref vStaticDock);
            iNIHolder.GetValue(sDockedSection, "StaticLaunch", ref vStaticLaunch);
            iNIHolder.GetValue(sDockedSection, "StaticHome", ref vStaticHome);
        }


        // DOCKING Section

        Vector3D vDockAlign;
        bool bDoDockAlign = false;
        Vector3D vDock;
        Vector3D vLaunch1;
        Vector3D vHome;
        bool bValidDock = false;
        bool bValidLaunch1 = false;
        bool bValidHome = false;

        long lTargetBase = 0;
        DateTime dtDockingActionStart;

        string sDockingSection = "DOCKING";

        void DockingInitCustomData(INIHolder iNIHolder)
        {
        }

        void DockingSerialize(INIHolder iNIHolder)
        {
            iNIHolder.SetValue(sDockingSection, "vDock", vDock);
            iNIHolder.SetValue(sDockingSection, "ValidDock", bValidDock);
            iNIHolder.SetValue(sDockingSection, "vLaunch1", vLaunch1);
            iNIHolder.SetValue(sDockingSection, "bValidLaunch1", bValidLaunch1);
            iNIHolder.SetValue(sDockingSection, "vHome", vHome);
            iNIHolder.SetValue(sDockingSection, "bValidHome", bValidHome);

            iNIHolder.SetValue(sDockingSection, "TargetBase", lTargetBase);
            iNIHolder.SetValue(sDockingSection, "ActionStart", dtDockingActionStart);

        }

        void DockingDeserialize(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sDockingSection, "vDock", ref vDock, true);
            iNIHolder.GetValue(sDockingSection, "ValidDock", ref bValidDock, true);
            iNIHolder.GetValue(sDockingSection, "vLaunch1", ref vLaunch1, true);
            iNIHolder.GetValue(sDockingSection, "bValidLaunch1", ref bValidLaunch1, true);
            iNIHolder.GetValue(sDockingSection, "vHome", ref vHome, true);
            iNIHolder.GetValue(sDockingSection, "bValidHome", ref bValidHome, true);

            iNIHolder.GetValue(sDockingSection, "TargetBase", ref lTargetBase, true);
            iNIHolder.GetValue(sDockingSection, "ActionStart", ref dtDockingActionStart);

        }
        double airworthyChecksElapsedMs = -1;

        bool DockAirWorthy(bool bForceCheck = false, bool bLaunchCheck = true, int cargohighwater=80)
        {
            bool BatteryGo = true;
            bool TanksGo = true;
            bool ReactorsGo = true;
            bool CargoGo = true;

            if (airworthyChecksElapsedMs >= 0)
                airworthyChecksElapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;
            bool bDoChecks = bForceCheck;
            if(airworthyChecksElapsedMs>0.5*1000)
            {
                airworthyChecksElapsedMs = 0;
                bDoChecks = true;
            }

            // Check battery charge
            if(bDoChecks) batteryCheck(0, false);
            if(bLaunchCheck)
            {
                if (batteryPercentage >= 0 && batteryPercentage < batterypcthigh)
                {
                    BatteryGo = false;
                }

            }
            else
            { 
                // check if we need to go back and refill
                if (batteryPercentage >= 0 && batteryPercentage < batterypctlow)
                {
                    BatteryGo = false;
                }
            }

            // check cargo emptied
            if (bDoChecks) doCargoCheck();
            if (bLaunchCheck)
            {
                if (cargopcent > cargopctmin)
                {
                    CargoGo = false;
                }
            }
            else
            {
                if (cargopcent > cargohighwater)
                {
                    CargoGo = false;
                }
            }
            // TODO: Check H2 tanks
            if (bDoChecks) TanksCalculate();
            if (bLaunchCheck)
            {
                if (TanksHasHydro() && hydroPercent*100 < 70)
                    TanksGo = false;
            }
            else
            {
                if (TanksHasHydro() && hydroPercent*100 < 30)
                    TanksGo = false;
            }
            // TODO: check reactor fuel

            if (BatteryGo && TanksGo && ReactorsGo && CargoGo)
            {
                return true;
            }
            else return false;

        }

    }
}
