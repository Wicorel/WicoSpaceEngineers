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

        string sOrbitalSection = "ORBITAL";

        void OrbitalInitCustomData(INIHolder iNIHolder)
        {
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


    }
}
