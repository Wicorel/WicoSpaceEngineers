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
        Vector3D vInitialAsteroidContact;
        Vector3D vInitialAsteroidExit;
        Vector3D vLastAsteroidContact;
        Vector3D vLastAsteroidExit;
        //        Vector3D vTargetMine;
        Vector3D vTargetAsteroid;
        //        Vector3D vCurrentNavTarget;
        //        Vector3D vNextTarget;
        Vector3D vExpectedAsteroidExit;
        bool bValidInitialAsteroidContact = false;
        bool bValidInitialAsteroidExit = false;
        //        bool bValidTarget = false;
        bool bValidAsteroid = false;

        long miningAsteroidID = -1;

        string sMiningSection = "MINING";
        void MiningInitCustomData(INIHolder iNIHolder)
        {
            //           iNIHolder.GetValue(sMiningSection, "cargopcthighwater", ref cargopcthighwater, true);
            //            iNIHolder.GetValue(sMiningSection, "cargopctlowwater", ref cargopctlowwater, true);
            iNIHolder.GetValue(sMiningSection, "TargetMiningMps", ref fTargetMiningMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningAbortMps", ref fMiningAbortMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningMinThrust", ref fMiningMinThrust, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachMps", ref fAsteroidApproachMps, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachAbortMps", ref fAsteroidApproachAbortMps, true);



        }

        void MiningSerialize(INIHolder iNIHolder)
        {
            iNIHolder.SetValue(sMiningSection, "vLastContact", vLastAsteroidContact);
            iNIHolder.SetValue(sMiningSection, "vTargetAsteroid", vTargetAsteroid);
            iNIHolder.SetValue(sMiningSection, "vLastExit", vLastAsteroidExit);
            iNIHolder.SetValue(sMiningSection, "vExpectedExit", vExpectedAsteroidExit);
            iNIHolder.SetValue(sMiningSection, "vInitialContact", vInitialAsteroidContact);
            iNIHolder.SetValue(sMiningSection, "vInitialExit", vInitialAsteroidContact);

            iNIHolder.SetValue(sMiningSection, "ValidAsteroid", bValidAsteroid);
            iNIHolder.SetValue(sMiningSection, "ValidInitialContact", bValidInitialAsteroidContact);
            iNIHolder.SetValue(sMiningSection, "ValidInitialExit", bValidInitialAsteroidExit);

            iNIHolder.SetValue(sMiningSection, "miningAsteroidID", miningAsteroidID);
        }

        void MiningDeserialize(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sMiningSection, "vLastContact", ref vLastAsteroidContact, true);
            iNIHolder.GetValue(sMiningSection, "vTargetAsteroid", ref vTargetAsteroid, true);
            iNIHolder.GetValue(sMiningSection, "vLastExit", ref vLastAsteroidExit, true);
            iNIHolder.GetValue(sMiningSection, "vExpectedExit", ref vExpectedAsteroidExit, true);
            iNIHolder.GetValue(sMiningSection, "vInitialContact", ref vInitialAsteroidContact, true);
            iNIHolder.GetValue(sMiningSection, "vInitialExit", ref vInitialAsteroidContact, true);

            iNIHolder.GetValue(sMiningSection, "ValidAsteroid", ref bValidAsteroid, true);
            iNIHolder.GetValue(sMiningSection, "ValidInitialContact", ref bValidInitialAsteroidContact, true);
            iNIHolder.GetValue(sMiningSection, "ValidInitialExit", ref bValidInitialAsteroidExit, true);

            iNIHolder.GetValue(sMiningSection, "miningAsteroidID", ref miningAsteroidID, true);
        }

    }
}
