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

        /// <summary>
        /// Set maximum speed of ship. 
        /// Set this using S command for NAV
        /// </summary>
        double shipSpeedMax = 100;

        /// <summary>
        /// the minimum distance to be from the target to be considered 'arrived'
        /// </summary>
        double arrivalDistanceMin = 50;

        int NAVArrivalMode = MODE_ARRIVEDTARGET;
        int NAVArrivalState = 0;

        //        Vector3D vNavLaunch;
        //        bool bValidNavLaunch = false;
        //        Vector3D vNavHome;
        //        bool bValidNavHome = false;
        bool dTMDebug = false;
        bool dTMUseCameraCollision = true;
        bool dTMUseSensorCollision = true;
        bool NAVEmulateOld = false;
        bool AllowBlindNav = false;
        float NAVGravityMinElevation = -1;

        bool bNavBeaconDebug = false;

        string sNavSection = "NAV";

        void NavInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sNavSection, "DTMDebug", ref dTMDebug, true);
            iNIHolder.GetValue(sNavSection, "CameraCollision", ref dTMUseCameraCollision, true);
            iNIHolder.GetValue(sNavSection, "SensorCollision", ref dTMUseSensorCollision, true);
            iNIHolder.GetValue(sNavSection, "NAVEmulateOld", ref NAVEmulateOld, true);
            iNIHolder.GetValue(sNavSection, "NAVGravityMinElevation", ref NAVGravityMinElevation, true);
            iNIHolder.GetValue(sNavSection, "NavBeaconDebug", ref bNavBeaconDebug, true);
            iNIHolder.GetValue(sNavSection, "AllowBlindNav", ref AllowBlindNav, true);
        }

        void NavSerialize(INIHolder iNIHolder)
        {
            //            iNIHolder.SetValue(sNavSection, "vNavHome", vNavHome);
            //            iNIHolder.SetValue(sNavSection, "ValidNavHome", bValidNavHome);
            iNIHolder.SetValue(sNavSection, "vTarget", vNavTarget);
            iNIHolder.SetValue(sNavSection, "ValidNavTarget", bValidNavTarget);

            iNIHolder.SetValue(sNavSection, "dStartShip", dtNavStartShip);
            iNIHolder.SetValue(sNavSection, "shipSpeedMax", shipSpeedMax);
            iNIHolder.SetValue(sNavSection, "arrivalDistanceMin", arrivalDistanceMin);
            iNIHolder.SetValue(sNavSection, "NAVArrivalMode", NAVArrivalMode);
            iNIHolder.SetValue(sNavSection, "NAVArrivalState", NAVArrivalState);
        }

        void NavDeserialize(INIHolder iNIHolder)
        {
//            iNIHolder.GetValue(sNavSection, "vNavLaunch", ref vNavLaunch, true);
//            iNIHolder.GetValue(sNavSection, "ValidNavLaunch", ref bValidNavLaunch, true);
            iNIHolder.GetValue(sNavSection, "vTarget", ref vNavTarget, true);
            iNIHolder.GetValue(sNavSection, "ValidNavTarget", ref bValidNavTarget, true);

            iNIHolder.GetValue(sNavSection, "dStartShip", ref dtNavStartShip, true);
            iNIHolder.GetValue(sNavSection, "shipSpeedMax", ref shipSpeedMax, true);
            iNIHolder.GetValue(sNavSection, "arrivalDistanceMin", ref arrivalDistanceMin, true);
            iNIHolder.GetValue(sNavSection, "NAVArrivalMode", ref NAVArrivalMode, true);
            iNIHolder.GetValue(sNavSection, "NAVArrivalState", ref NAVArrivalState, true);
        }

        List<IMyBeacon> navDebugBeacons = new List<IMyBeacon>();
        void NavDebug(string str)
        {
            if(bNavBeaconDebug)
            {
                if (navDebugBeacons.Count < 1)
                    GridTerminalSystem.GetBlocksOfType(navDebugBeacons);
                foreach(var beacon in navDebugBeacons)
                {
                    beacon.CustomName = str;
                }
            }
        }

        //TODO: Add istarget asteroid?
        void NavGoTarget(Vector3D vTarget, int modeArrival=MODE_ARRIVEDTARGET, int stateArrival=0, double DistanceMin=50)
        {
            vNavTarget = vTarget;
            bValidNavTarget = true;
            NAVArrivalMode = modeArrival;
            NAVArrivalState = stateArrival;
            arrivalDistanceMin = DistanceMin;
            current_state = 0;
            setMode(MODE_GOINGTARGET);
        }

    }
}
