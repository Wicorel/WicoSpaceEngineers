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

        int MiningCargopcthighwater = 95;
        int MiningCargopctlowwater = 85;

        float fTargetMiningMps = 0.55f;
        float fMiningAbortMps = 1.25f;
        float fMiningMinThrust = 0.85f;

        float fAsteroidApproachMps = 5.0f;
        float fAsteroidApproachAbortMps = 10.0f;

        float fAsteroidExitMps = 15.0f;

        bool bMiningWaitingCargo = false;

//        Vector3D vInitialAsteroidContact;
//        Vector3D vInitialAsteroidExit;
//        bool bValidInitialAsteroidContact = false;
//        bool bValidInitialAsteroidExit = false;

//        Vector3D vLastAsteroidContact;
//        Vector3D vLastAsteroidExit;

//        Vector3D vTargetAsteroid;
//        bool bValidAsteroid = false;

//        Vector3D vExpectedAsteroidExit;

        long miningAsteroidID = -1;

        double MiningBoreHeight = -1;
        double MiningBoreWidth = -1;
        //        BoundingBoxD minigAsteroidBB;

        /*
         *split asteroid into bore holes for testing/destruction
         * 
         * Base size of hole on size of miner
         * 
         * Keep track of bore holes done
         */

        double MineShipLengthScale = 1.5;
//        double MiningBoreOverlap = 0.05; // percent of overlap

        // regeneratable from asteroid info
        Vector3D AsteroidUpVector; // vector for 'up' (or 'down') in the asteroid to align drill holes
        Vector3D AsteroidOutVector;// vector for 'forward'  negative for going 'other' way.
        Vector3D AsteroidRightVector; // vector for right (-left)  Note: swap for 'other' way

        Vector3D AsteroidPosition;// world coordinates of center of asteroid
        double AsteroidDiameter;
        Vector3D vAsteroidBoreStart;
        Vector3D vAsteroidBoreEnd;
        int AsteroidMineMode = 0;
        // 0 = full destructive.
        // 1 = single bore


//        bool bAsteroidBoreReverse = false; // go from end to start.

        // current state needs to be saved
        int AsteroidCurrentX = 0;
        int AsteroidCurrentY = 0;
        int AsteroidMaxX = 0;
        int AsteroidMaxY = 0;

        // TODO:? Take into account gravity and use that as alignment?  Natural and Planet

        string sMiningSection = "MINING";
        void MiningInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sMiningSection, "TargetMiningMps", ref fTargetMiningMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningAbortMps", ref fMiningAbortMps, true);
            iNIHolder.GetValue(sMiningSection, "MiningMinThrust", ref fMiningMinThrust, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachMps", ref fAsteroidApproachMps, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidApproachAbortMps", ref fAsteroidApproachAbortMps, true);

            iNIHolder.GetValue(sMiningSection, "Cargopcthighwater", ref MiningCargopcthighwater, true);
            iNIHolder.GetValue(sMiningSection, "Cargopctlowater", ref MiningCargopctlowwater, true);

            iNIHolder.GetValue(sMiningSection, "MiningBoreHeight", ref MiningBoreHeight, true);
            iNIHolder.GetValue(sMiningSection, "MiningBoreWidth", ref MiningBoreWidth, true);

        }

        void MiningSerialize(INIHolder iNIHolder)
        {
            if (iNIHolder == null) return;
/*
            iNIHolder.SetValue(sMiningSection, "vLastContact", vLastAsteroidContact);
            iNIHolder.SetValue(sMiningSection, "vTargetAsteroid", vTargetAsteroid);
            iNIHolder.SetValue(sMiningSection, "vLastExit", vLastAsteroidExit);
            iNIHolder.SetValue(sMiningSection, "vExpectedExit", vExpectedAsteroidExit);
            iNIHolder.SetValue(sMiningSection, "vInitialContact", vInitialAsteroidContact);
            iNIHolder.SetValue(sMiningSection, "vInitialExit", vInitialAsteroidContact);

            iNIHolder.SetValue(sMiningSection, "ValidAsteroid", bValidAsteroid);
            iNIHolder.SetValue(sMiningSection, "ValidInitialContact", bValidInitialAsteroidContact);
            iNIHolder.SetValue(sMiningSection, "ValidInitialExit", bValidInitialAsteroidExit);
            */
            iNIHolder.SetValue(sMiningSection, "miningAsteroidID", miningAsteroidID);

            iNIHolder.SetValue(sMiningSection, "AsteroidCurrentX", AsteroidCurrentX);
            iNIHolder.SetValue(sMiningSection, "AsteroidCurrentY", AsteroidCurrentY);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreStart", vAsteroidBoreStart);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreEnd", vAsteroidBoreEnd);
            iNIHolder.SetValue(sMiningSection, "AsteroidMineMode", AsteroidMineMode);
        }

        void MiningDeserialize(INIHolder iNIHolder)
        {
            if (iNIHolder == null) return;

            /*
            iNIHolder.GetValue(sMiningSection, "vLastContact", ref vLastAsteroidContact, true);
            iNIHolder.GetValue(sMiningSection, "vTargetAsteroid", ref vTargetAsteroid, true);
            iNIHolder.GetValue(sMiningSection, "vLastExit", ref vLastAsteroidExit, true);
            iNIHolder.GetValue(sMiningSection, "vExpectedExit", ref vExpectedAsteroidExit, true);
            iNIHolder.GetValue(sMiningSection, "vInitialContact", ref vInitialAsteroidContact, true);
            iNIHolder.GetValue(sMiningSection, "vInitialExit", ref vInitialAsteroidContact, true);

            iNIHolder.GetValue(sMiningSection, "ValidAsteroid", ref bValidAsteroid, true);
            iNIHolder.GetValue(sMiningSection, "ValidInitialContact", ref bValidInitialAsteroidContact, true);
            iNIHolder.GetValue(sMiningSection, "ValidInitialExit", ref bValidInitialAsteroidExit, true);
            */
            iNIHolder.GetValue(sMiningSection, "miningAsteroidID", ref miningAsteroidID, true);

            iNIHolder.GetValue(sMiningSection, "AsteroidCurrentX", ref AsteroidCurrentX, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidCurrentY", ref AsteroidCurrentY, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidBoreStart", ref vAsteroidBoreStart, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidBoreEnd", ref vAsteroidBoreEnd, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidMineMode", ref AsteroidMineMode, true);
        }

        void MinerMasterReset()
        {
//            bValidInitialAsteroidContact = false;
//            bValidInitialAsteroidExit = false;
//            bValidAsteroid = false;

            miningAsteroidID = -1;
            AsteroidMineMode = 0;

            AsteroidUpVector = Vector3D.Zero;
//            vTargetAsteroid = AsteroidUpVector;
//            vLastAsteroidContact = AsteroidUpVector;
//            vLastAsteroidExit = AsteroidUpVector;
        }

        void AsteroidCalculateFirstBore()
        {
            AsteroidCurrentX = 0;
            AsteroidCurrentY = 0;
            MinerCalculateAsteroidVector(miningAsteroidID);
            AsteroidCalculateBestStartEnd();
        }

        void AsteroidCalculateBestStartEnd()
        {
            // calculate bore start and end.
            MinerCalculateBoreSize();

            int iSign = 0;
            // calculate the offset value.

            Vector3D vXOffset = AsteroidRightVector * AsteroidCurrentX * MiningBoreWidth;
            // add offset size of 0,0 opening
            iSign = Math.Sign(AsteroidCurrentX);
            if (iSign == 0) iSign = 1;
//            if (AsteroidCurrentX != 0) vXOffset += AsteroidRightVector * iSign * MiningBoreWidth/2;

            // calculate the offset value.
            Vector3D vYOffset = AsteroidUpVector * AsteroidCurrentY * MiningBoreHeight;
            // offset size of 0,0 opening
            iSign = Math.Sign(AsteroidCurrentY);
            if (iSign == 0) iSign = 1;
//            if (AsteroidCurrentY != 0) vYOffset += AsteroidUpVector * iSign * MiningBoreHeight/2;


            Vector3D vStart = AsteroidPosition + vXOffset + vYOffset;
            vStart += -AsteroidOutVector * (AsteroidDiameter / 2 + shipDim.LengthInMeters() * MineShipLengthScale);

            Vector3D vEnd = AsteroidPosition + vXOffset + vYOffset;
            vEnd += AsteroidOutVector * (AsteroidDiameter / 2 + shipDim.LengthInMeters() * MineShipLengthScale);

            vAsteroidBoreEnd = vEnd;
            vAsteroidBoreStart = vStart;
//            vAsteroidBoreEnd = AsteroidCalculateBoreEnd();
//            vAsteroidBoreStart = AsteroidCalculateBoreStart();

            // calculate order
            double dStart = (vAsteroidBoreStart - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
            double dEnd = (vAsteroidBoreEnd - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
//            bAsteroidBoreReverse = false;

            // reverse so start is the closest.
            if (dEnd < dStart)
            {
//                bAsteroidBoreReverse = true;
                Vector3D vTmp = vAsteroidBoreStart;
                vAsteroidBoreStart = vAsteroidBoreEnd;
                vAsteroidBoreEnd = vTmp;
            }
//            vExpectedAsteroidExit = vAsteroidBoreStart - vAsteroidBoreEnd;
        }

        bool AsteroidCalculateNextBore()
        {
            // TODO: handle other mine modes like spread out
            if (AsteroidMineMode == 1) return false; // there are no next bores :)

            bool bAsteroidDone = false;

            if(AsteroidCurrentX==0 && AsteroidCurrentY==0)
            {
                AsteroidCurrentX = 1;
                AsteroidCalculateBestStartEnd();
                return true;
            }

            if (AsteroidCurrentX > 0)
            {
                if (AsteroidCurrentX >= AsteroidMaxX)
                {
                    AsteroidCurrentX = 0;
                    bAsteroidDone = AsteroidCalculateNextRow();
                }
                else
                { // make it negative.
                    AsteroidCurrentX = -AsteroidCurrentX;
                }
            }
            else// if (AsteroidCurrentX < 0)
            {
                // make it positive
                AsteroidCurrentX = Math.Abs(AsteroidCurrentX);
                AsteroidCurrentX++;
            }
            if (Math.Abs(AsteroidCurrentX) >= AsteroidMaxX)
            {
                bAsteroidDone = AsteroidCalculateNextRow();
            }
            AsteroidCalculateBestStartEnd();
            return true;
        }

        bool AsteroidCalculateNextRow()
        {
            AsteroidCurrentX = 0;
            if (AsteroidCurrentY == 0)
                AsteroidCurrentY = 1;
            else
            {
                if (AsteroidCurrentY > 0)
                {
                    if (AsteroidCurrentY >= AsteroidMaxY)
                    {
                        // We are done with asteroid.
                        AsteroidCurrentY = 0;
                        return false;
                    }
                    AsteroidCurrentY = -AsteroidCurrentY;

                }
                else
                { // back to positive
                    AsteroidCurrentY = -AsteroidCurrentY;
                    // and increment
                    AsteroidCurrentY++;
                }
            }
            return true;

        }

        void AsteroidDoNextBore()
        {
            if (!AsteroidCalculateNextBore())
            {
                // we are done with asteroid.
                // TODO: do a final search pass for any missed voxels
                // TODO: remove asteroid
                bAutoRelaunch = false;
                miningAsteroidID = -1;
                AsteroidMineMode = 0;
                setMode(MODE_DOCKING);
            }
            else
            {
                NavGoTarget(vAsteroidBoreStart, iMode, 120);
            }
        }
        void MinerCalculateBoreSize()
        {
            if (MiningBoreHeight <= 0)
            {
                MiningBoreHeight = (shipDim.HeightInMeters() );
                MiningBoreWidth = (shipDim.WidthInMeters());
//                MiningBoreHeight = (shipDim.HeightInMeters() - shipDim.BlockMultiplier() * 2);
//                MiningBoreWidth = (shipDim.WidthInMeters() - shipDim.BlockMultiplier() * 2);

                // save defaults back to customdata to allow player to change
                INIHolder iniCustomData = new INIHolder(this, Me.CustomData);
                iniCustomData.SetValue(sMiningSection, "MiningBoreHeight", MiningBoreHeight.ToString("0.00"));
                iniCustomData.SetValue(sMiningSection, "MiningBoreWidth", MiningBoreWidth.ToString("0.00"));
                // informational for the player
                iniCustomData.SetValue(sMiningSection, "ShipWidth", shipDim.WidthInMeters().ToString("0.00"));
                iniCustomData.SetValue(sMiningSection, "ShipHeight", shipDim.HeightInMeters().ToString("0.00"));

                Me.CustomData = iniCustomData.GenerateINI(true);
            }
        }

        void MinerCalculateAsteroidVector(long AsteroidID)
        {
            BoundingBoxD bbd = AsteroidGetBB(AsteroidID);

            Vector3D[] corners= new Vector3D[BoundingBoxD.CornerCount];
            AsteroidPosition = bbd.Center;
            AsteroidDiameter = (bbd.Max - bbd.Min).Length();

            MinerCalculateBoreSize();

            AsteroidMaxX = (int)(AsteroidDiameter / MiningBoreWidth / 2);
            AsteroidMaxY = (int)(AsteroidDiameter / MiningBoreHeight / 2);
            /*
             * Near Side  Far Side
             * 3---0    7---4
             * |   |    |   |
             * 2---1    6---5
             * 
             * so 
             * Right side is 0,1,5,4
             * Top is 0,3,7,4
             * Bottom is 1,2,6,5
             */

            bbd.GetCorners(corners);
            AsteroidUpVector = PlanarNormal(corners[3], corners[4], corners[7]);
            AsteroidOutVector = PlanarNormal(corners[0], corners[1], corners[2]);
            AsteroidRightVector = PlanarNormal(corners[0], corners[1], corners[4]);
            /*

            // Everything after is debug stuff
            Vector3D v1 = corners[3];
            Vector3D v2 = corners[4];
            Vector3D v3 = corners[7];
            debugGPSOutput("Up1", v1);
            debugGPSOutput("Up2", v2);
            debugGPSOutput("Up3", v3);
            v1 = bbd.Center + AsteroidUpVector * 100;
            debugGPSOutput("UPV", v1);
            v1 = corners[0];
            v2 = corners[1];
            v3 = corners[2];
            debugGPSOutput("Out1", v1);
            debugGPSOutput("Out2", v2);
            debugGPSOutput("Out3", v3);
            v1 = bbd.Center + AsteroidOutVector * 100;
            debugGPSOutput("OutV", v1);
            */
        }

        void MinerProcessScan(MyDetectedEntityInfo mydei)
        {
            if (mydei.IsEmpty())
            {
                return;
            }
            addDetectedEntity(mydei);
            if (mydei.Type == MyDetectedEntityType.Asteroid)
            {
                AsteroidAdd(mydei);
                /*
                if (!bValidAsteroid)
                {
                    bValidAsteroid = true;
                    vTargetAsteroid = mydei.Position;
                    //                currentAst.EntityId = mydei.EntityId;
                    //                currentAst.BoundingBox = mydei.BoundingBox;
                    if (mydei.HitPosition != null) vExpectedAsteroidExit = (Vector3D)mydei.HitPosition - shipOrientationBlock.GetPosition();
                    else vExpectedAsteroidExit = vTargetAsteroid - shipOrientationBlock.GetPosition();
                    vExpectedAsteroidExit.Normalize();
                    bValidExit = true;
                }
                */
            }
        }
    }
}
