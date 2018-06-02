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

        float fTargetMiningMps = 0.85f;
        float fMiningAbortMps = 1.25f;
        float fMiningMinThrust = 0.85f;

        float fAsteroidApproachMps = 5.0f;
        float fAsteroidApproachAbortMps = 10.0f;

        float fAsteroidExitMps = 15.0f;

        bool bMiningWaitingCargo = false;

        Vector3D vInitialAsteroidContact;
        Vector3D vInitialAsteroidExit;
        bool bValidInitialAsteroidContact = false;
        bool bValidInitialAsteroidExit = false;

        Vector3D vLastAsteroidContact;
        Vector3D vLastAsteroidExit;

        Vector3D vTargetAsteroid;
        bool bValidAsteroid = false;

        Vector3D vExpectedAsteroidExit;

        long miningAsteroidID = -1;
        //        BoundingBoxD minigAsteroidBB;

        /*
         *split asteroid into bore holes for testing/destruction
         * 
         * Base size of hole on size of miner
         * 
         * Keep track of bore holes done
         */

        double MineShipLengthScale = 1.5;
        double MiningBoreOverlap = 0.05; // percent of overlap

        // regeneratable from asteroid info
        Vector3D AsteroidUpVector; // vector for 'up' (or 'down') in the asteroid to align drill holes
        Vector3D AsteroidOutVector;// vector for 'forward'  negative for going 'other' way.
        Vector3D AsteroidRightVector; // vector for right (-left)  Note: swap for 'other' way

        Vector3D AsteroidPosition;// world coordinates of center of asteroid
        double AsteroidDiameter;
        Vector3D vAsteroidBoreStart;
        Vector3D vAsteroidBoreEnd;
        bool bAsteroidBoreReverse = false; // go from end to start.

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

        }

        void MiningSerialize(INIHolder iNIHolder)
        {
            if (iNIHolder == null) return;

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
            //            iNIHolder.SetValue(sMiningSection, "AsteroidUpVector", AsteroidUpVector);
            iNIHolder.SetValue(sMiningSection, "AsteroidCurrentX", AsteroidCurrentX);
            iNIHolder.SetValue(sMiningSection, "AsteroidCurrentY", AsteroidCurrentY);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreStart", vAsteroidBoreStart);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreEnd", vAsteroidBoreEnd);
            iNIHolder.SetValue(sMiningSection, "AsteroidBoreReverse", bAsteroidBoreReverse);
            //            iNIHolder.SetValue(sMiningSection, "AsteroidMaxX", AsteroidMaxX);
            //            iNIHolder.SetValue(sMiningSection, "AsteroidMaxY", AsteroidMaxY);
        }

        void MiningDeserialize(INIHolder iNIHolder)
        {
            if (iNIHolder == null) return;

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
            //            iNIHolder.GetValue(sMiningSection, "AsteroidUpVector", ref AsteroidUpVector, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidCurrentX", ref AsteroidCurrentX, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidCurrentY", ref AsteroidCurrentY, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidBoreStart", ref vAsteroidBoreStart, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidBoreEnd", ref vAsteroidBoreEnd, true);
            iNIHolder.GetValue(sMiningSection, "AsteroidBoreReverse", ref bAsteroidBoreReverse, true);
            //            iNIHolder.GetValue(sMiningSection, "AsteroidMaxX", ref AsteroidMaxX, true);
            //            iNIHolder.GetValue(sMiningSection, "AsteroidMaxY", ref AsteroidMaxY, true);
        }

        void MinerMasterReset()
        {
            bValidInitialAsteroidContact = false;
            bValidInitialAsteroidExit = false;
            bValidAsteroid = false;

            miningAsteroidID = -1;

            AsteroidUpVector = Vector3D.Zero;
            vTargetAsteroid = AsteroidUpVector;
            vLastAsteroidContact = AsteroidUpVector;
            vLastAsteroidExit = AsteroidUpVector;
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
            vAsteroidBoreEnd = AsteroidCalculateBoreEnd();
            vAsteroidBoreStart = AsteroidCalculateBoreStart();

            double dStart = (vAsteroidBoreStart - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
            double dEnd = (vAsteroidBoreEnd - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
            bAsteroidBoreReverse = false;
            if (dEnd < dStart)
            {
                bAsteroidBoreReverse = true;
                Vector3D vTmp = vAsteroidBoreStart;
                vAsteroidBoreStart = vAsteroidBoreEnd;
                vAsteroidBoreEnd = vTmp;
            }
            vExpectedAsteroidExit = vAsteroidBoreStart - vAsteroidBoreEnd;
        }

        bool AsteroidCalculateNextBore()
        {
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
                    if (AsteroidCurrentY == 0)
                        AsteroidCurrentY = 1;
                    else
                    {
                        if (AsteroidCurrentY > 0)
                        {
                            if (AsteroidCurrentY >= AsteroidMaxY)
                            {
                                // We are done with asteroid.
                                AsteroidCurrentX = 0;
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
                }
                else
                { // make it negative.
                    AsteroidCurrentX = -AsteroidCurrentX;
                }
            }
            else// if (AsteroidCurrentX < 0)
            {
                // make it positive
                AsteroidCurrentX = -AsteroidCurrentX;
                AsteroidCurrentX++;
            }
            AsteroidCalculateBestStartEnd();
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
                setMode(MODE_DOCKING);
            }
            else
            {
                // TODO: do a reverse bore instead of going around

                /*
                // TODO: check if THIS was a reverse bore and go 'normal' bore.
                Vector3D vStart = AsteroidCalculateBoreStart();

                // TODO: this is just ASKING for collision..  Instead, calculate vector from 'up' and then go to start.
                NavGoTarget(vStart, iMode, 120);
                */
                NavGoTarget(vAsteroidBoreStart, iMode, 120);

                /*
                int iSign = Math.Sign(AsteroidCurrentY);
                if (iSign == 0) iSign = 1;
                Vector3D vTop = AsteroidPosition + AsteroidUpVector * AsteroidDiameter*iSign*1.25;
                NavGoTarget(vTop, iMode, 310, 10);
                */
            }
        }

        Vector3D AsteroidCalculateBoreStart()
        {
            int iSign = 0;
            // calculate the offset value.
            Vector3D vXOffset = AsteroidRightVector * AsteroidCurrentX * (shipDim.WidthInMeters() * (1 - MiningBoreOverlap));
            // add offset size of 0,0 opening
            iSign = Math.Sign(AsteroidCurrentX);
            if (iSign == 0) iSign = 1;
            if (AsteroidCurrentX != 0) vXOffset += AsteroidRightVector * iSign * shipDim.WidthInMeters() * (0.50-MiningBoreOverlap);

            // calculate the offset value.
            Vector3D vYOffset = AsteroidUpVector * AsteroidCurrentY * (shipDim.HeightInMeters() * (1 - MiningBoreOverlap));
            // offset size of 0,0 opening
            iSign = Math.Sign(AsteroidCurrentY);
            if (iSign == 0) iSign = 1;
            if (AsteroidCurrentY != 0) vYOffset += AsteroidUpVector * iSign * shipDim.HeightInMeters() * (0.50 - MiningBoreOverlap);


            Vector3D vStart = AsteroidPosition + vXOffset + vYOffset;
            // get position to put ship
            // TODO:? doesn't take into account back and forth passes

            vStart += -AsteroidOutVector * (AsteroidDiameter / 2 + shipDim.LengthInMeters() * MineShipLengthScale);
            // increase distance to that small errors in 'arrival' location don't have a large effect on final bore location.
            //                        vStart += -AsteroidOutVector * (AsteroidDiameter/2 + shipDim.LengthInMeters() * 1.5 +100);
            return vStart;
        }
        Vector3D AsteroidCalculateBoreEnd()
        {
            Vector3D vXOffset = AsteroidRightVector * AsteroidCurrentX * (shipDim.WidthInMeters() * (1 - MiningBoreOverlap));
            if (AsteroidCurrentX != 0) vXOffset += AsteroidRightVector * Math.Sign(AsteroidCurrentX) * shipDim.WidthInMeters() * (0.5 - MiningBoreOverlap);
            Vector3D vYOffset = AsteroidUpVector * AsteroidCurrentY * (shipDim.HeightInMeters() * (1 - MiningBoreOverlap));
            if (AsteroidCurrentY != 0) vYOffset += AsteroidUpVector * Math.Sign(AsteroidCurrentY) * shipDim.HeightInMeters() * (0.5 - MiningBoreOverlap);
            Vector3D vEnd = AsteroidPosition + vXOffset + vYOffset;
            // get position to put ship
            // TODO:? doesn't take into account back and forth passes

            vEnd += AsteroidOutVector * (AsteroidDiameter / 2 + shipDim.LengthInMeters() * MineShipLengthScale);
            // increase distance to that small errors in 'arrival' location don't have a large effect on final bore location.
            //                        vStart += -AsteroidOutVector * (AsteroidDiameter/2 + shipDim.LengthInMeters() * 1.5 +100);
            return vEnd;
        }

        /*
        int AsteroidCalculateX(int boreCount)
        {
            int calc = 0;
            switch(boreCount)
            {
                case 0: calc = 0; break;
                case 1: calc = 1; break;
                case 2: calc = 1; break;
                case 3: calc = 0; break;
                case 4: calc = -1; break;
                case 5: calc = -1; break;
                case 6: calc = -1; break;
                case 7: calc = 0; break;
                case 8: calc = 1; break;
                case 9: calc = 2; break;
                case 10: calc = 2; break;
                case 11: calc = 2; break;
                case 12: calc = 2; break;
                case 13: calc = 1; break;
                case 14: calc = 0; break;
                case 15: calc = -1; break;
                case 16: calc = -2; break;
                case 17: calc = -2; break;
                case 18: calc = -2; break;
                case 19: calc = -2; break;
                case 20: calc = -2; break;
                case 21: calc = -1; break;
                case 22: calc = 0; break;
                case 23: calc = 1; break;
                case 24: calc = 2; break;
                case 25: calc = 3; break;
                case 26: calc = 3; break;
                case 27: calc = 3; break;
                case 28: calc = 3; break;
                case 29: calc = 3; break;
                case 30: calc = 3; break;
                case 31: calc = 2; break;
                case 32: calc = 1; break;
                case 33: calc = 0; break;
                case 34: calc = -1; break;
                case 35: calc = -2; break;
                case 36: calc = -3; break;
                case 37: calc = -3; break;
                case 38: calc = -3; break;
                case 39: calc = -3; break;
                case 40: calc = -3; break;
                case 41: calc = -3; break;
                case 42: calc = -3; break;
                case 43: calc = -2; break;
                case 44: calc = -1; break;
                case 45: calc = 0; break;
                case 46: calc = 1; break;
                case 47: calc = 2; break;
                case 48: calc = 3; break;
                case 49: calc = 4; break;
                default: calc = -1; break;
            }

            return calc;
        }

        int AsteroidCalculateY(int boreCount)
        {
            int calc = 0;
            switch (boreCount)
            {
                case 0: calc = 0; break;
                case 1: calc = 0; break;
                case 2: calc = 1; break;
                case 3: calc = 1; break;
                case 4: calc = 1; break;
                case 5: calc = 0; break;
                case 6: calc = -1; break;
                case 7: calc = -1; break;
                case 8: calc = -1; break;
                case 9: calc = -1; break;
                case 10: calc = 0; break;
                case 11: calc = 1; break;
                case 12: calc = 2; break;
                case 13: calc = 2; break;
                case 14: calc = 2; break;
                case 15: calc = 2; break;
                case 16: calc = 2; break;
                case 17: calc = 1; break;
                case 18: calc = 0; break;
                case 19: calc = -1; break;
                case 20: calc = -2; break;
                case 21: calc = -2; break;
                case 22: calc = -2; break;
                case 23: calc = -2; break;
                case 24: calc = -2; break;
                case 25: calc = -2; break;
                case 26: calc = -1; break;
                case 27: calc = 0; break;
                case 28: calc = 1; break;
                case 29: calc = 2; break;
                case 30: calc = 3; break;
                case 31: calc = 3; break;
                case 32: calc = 3; break;
                case 33: calc = 3; break;
                case 34: calc = 3; break;
                case 35: calc = 3; break;
                case 36: calc = 3; break;
                case 37: calc = 2; break;
                case 38: calc = 1; break;
                case 39: calc = 0; break;
                case 40: calc = -1; break;
                case 41: calc = -2; break;
                case 42: calc = -3; break;
                case 43: calc = -3; break;
                case 44: calc = -3; break;
                case 45: calc = -3; break;
                case 46: calc = -3; break;
                case 47: calc = -3; break;
                case 48: calc = -3; break;
                case 49: calc = -3; break;
                default: calc = -1; break;
            }

            return calc;
        }
        */
        void MinerCalculateAsteroidVector(long AsteroidID)
        {
            BoundingBoxD bbd = AsteroidGetBB(AsteroidID);

            Vector3D[] corners= new Vector3D[BoundingBoxD.CornerCount];
            AsteroidPosition = bbd.Center;
            AsteroidDiameter = (bbd.Max - bbd.Min).Length();

            AsteroidMaxX = (int)(AsteroidDiameter / shipDim.WidthInMeters() / 2);
            AsteroidMaxY = (int)(AsteroidDiameter / shipDim.HeightInMeters() / 2);
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
            }
        }
    }
}
