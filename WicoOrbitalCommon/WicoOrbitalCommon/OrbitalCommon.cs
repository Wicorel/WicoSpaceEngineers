using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        Vector3D vOrbitalLaunch;
        bool bValidOrbitalLaunch = false;
        Vector3D vOrbitalHome;
        bool bValidOrbitalHome = false;

        float orbitalAtmoMult = 5;
        float orbitalIonMult = 2;
        float orbitalHydroMult = 1;

        string sOrbitalSection = "ORBITAL";

        void OrbitalInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.SetValue(sOrbitalSection, "AtmoMult", orbitalAtmoMult);
            iNIHolder.SetValue(sOrbitalSection, "IonMult", orbitalIonMult);
            iNIHolder.SetValue(sOrbitalSection, "HydroMult", orbitalHydroMult);
        }

        void OrbitalSerialize(INIHolder iNIHolder)
        {
            iNIHolder.SetValue(sOrbitalSection, "vOrbitalLaunch", vOrbitalLaunch);
            iNIHolder.SetValue(sOrbitalSection, "ValidOrbitalLaunch", bValidOrbitalLaunch);
            iNIHolder.SetValue(sOrbitalSection, "vOrbitalHome", vOrbitalHome);
            iNIHolder.SetValue(sOrbitalSection, "ValidOrbitalHome", bValidOrbitalHome);
        }

        void OrbitalDeserialize(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sOrbitalSection, "vOrbitalLaunch", ref vOrbitalLaunch, true);
            iNIHolder.GetValue(sOrbitalSection, "ValidOrbitalLaunch", ref bValidOrbitalLaunch, true);
            iNIHolder.GetValue(sOrbitalSection, "vOrbitalHome", ref vOrbitalHome, true);
            iNIHolder.GetValue(sOrbitalSection, "ValidOrbitalHome", ref bValidOrbitalHome, true);

        }

        string sOrbitalUpDirection = "";
        List<IMyTerminalBlock> thrustOrbitalUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustOrbitalDownList = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> cameraOrbitalLandingList = new List<IMyTerminalBlock>();

        /// <summary>
        /// Choose best thrusters and orientation to use in gravity to launch
        /// WANT: base on current power and hydrogen (and ice) availability 
        /// </summary>
        /// <returns>true if the 'best' has changed. Modifies thrustOrbitalUpList, thrustObritalDownList, and sOrbitalDirection</returns>
        ///
        bool calculateBestGravityThrust(bool PerformChangeOver=true)
        {
            double upThrust = calculateTotalEffectiveThrust(thrustUpList, orbitalAtmoMult,orbitalIonMult, orbitalHydroMult);
            double fwThrust = calculateTotalEffectiveThrust(thrustForwardList, orbitalAtmoMult, orbitalIonMult, orbitalHydroMult);
            bool bChanged = false;

            if (fwThrust > upThrust)
            {
                if (sOrbitalUpDirection != "rocket")
                {
                    if (PerformChangeOver)
                    {
                        thrustOrbitalUpList = thrustForwardList;
                        thrustOrbitalDownList = thrustBackwardList;
                        sOrbitalUpDirection = "rocket";
                        cameraOrbitalLandingList = cameraBackwardList;
                    }
                    bChanged = true;
                }
            }
            else
            {
                if (sOrbitalUpDirection != "down")
                {
                    if (PerformChangeOver)
                    {
                        thrustOrbitalUpList = thrustUpList;
                        thrustOrbitalDownList = thrustDownList;
                        sOrbitalUpDirection = "down";
                        cameraOrbitalLandingList = cameraDownList;
                    }
                    bChanged = true;
                }
            }
            return bChanged;

        }

    }
}
