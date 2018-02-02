using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

//        Vector3D vNavHome;
//        bool bValidNavHome = false;

        Vector3D vNavTarget;// vTargetMine;
        bool bValidNavTarget = false;
        DateTime dtNavStartShip;

        //        Vector3D vNavLaunch;
        //        bool bValidNavLaunch = false;
        //        Vector3D vNavHome;
        //        bool bValidNavHome = false;
        bool dTMDebug = false;
        bool dTMUseCameraCollision = true;
        bool dTMUseSensorCollision = true;
        bool NAVEmulateOld = true;
        float NAVGravityMinElevation = -1;

        string sNavSection = "NAV";

        void NavInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sNavSection, "DTMDebug", ref dTMDebug, true);
            iNIHolder.GetValue(sNavSection, "CameraCollision", ref dTMUseCameraCollision, true);
            iNIHolder.GetValue(sNavSection, "SensorCollision", ref dTMUseSensorCollision, true);
            iNIHolder.GetValue(sNavSection, "NAVEmulateOld", ref NAVEmulateOld, true);
            iNIHolder.GetValue(sNavSection, "NAVGravityMinElevation", ref NAVGravityMinElevation, true);
        }

        void NavSerialize(INIHolder iNIHolder)
        {
            //            iNIHolder.SetValue(sNavSection, "vNavHome", vNavHome);
            //            iNIHolder.SetValue(sNavSection, "ValidNavHome", bValidNavHome);
            iNIHolder.SetValue(sNavSection, "vTarget", vNavTarget);
            iNIHolder.SetValue(sNavSection, "ValidNavTarget", bValidNavTarget);

            iNIHolder.SetValue(sNavSection, "dStartShip", dtNavStartShip);
        }

        void NavDeserialize(INIHolder iNIHolder)
        {
//            iNIHolder.GetValue(sNavSection, "vNavLaunch", ref vNavLaunch, true);
//            iNIHolder.GetValue(sNavSection, "ValidNavLaunch", ref bValidNavLaunch, true);
            iNIHolder.GetValue(sNavSection, "vTarget", ref vNavTarget, true);
            iNIHolder.GetValue(sNavSection, "ValidNavTarget", ref bValidNavTarget, true);

            iNIHolder.GetValue(sNavSection, "dStartShip", ref dtNavStartShip,true);

        }


    }
}
