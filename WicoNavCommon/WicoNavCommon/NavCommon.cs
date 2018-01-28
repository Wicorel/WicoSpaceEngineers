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

        string sNavSection = "NAV";

        void NavInitCustomData(INIHolder iNIHolder)
        {
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
