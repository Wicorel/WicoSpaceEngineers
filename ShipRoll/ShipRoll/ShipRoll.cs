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
        double CalculateRoll(Vector3D destination, IMyTerminalBlock Origin)
        {
            double rollAngle = 0;
            bool facingTarget = false;

            Vector3D vCenter;
            Vector3D vBack;
            Vector3D vUp;
            Vector3D vRight;

            MatrixD refOrientation = GetBlock2WorldTransform(Origin);

            vCenter = Origin.GetPosition();
            vBack = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Backward);
            vUp = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Up);
            vRight = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Right);


            double centerTargetDistance = calculateDistance(vCenter, destination);
            double upTargetDistance = calculateDistance(vUp, destination);
            double rightLocalDistance = calculateDistance(vRight, vCenter);
            double rightTargetDistance = calculateDistance(vRight, destination);

            facingTarget = centerTargetDistance > upTargetDistance;

            rollAngle = (centerTargetDistance - rightTargetDistance) / rightLocalDistance;
            //Echo("calc Angle=" + Math.Round(rollAngle,5)); 

            if (!facingTarget)
            {
                Echo("ROLL:NOT FACING!");
                rollAngle += (rollAngle < 0) ? -1 : 1;
            }
            return rollAngle;
        }

        bool DoRoll(double rollAngle, string sPlane = "Roll")
        {
            //Echo("rollAngle=" + Math.Round(rollAngle,5)); 
            float targetRoll = 0;
            IMyGyro gyro = gyros[0] as IMyGyro;
            float maxRoll = 60; // gyro.GetMaximum<float>(sPlane);
//            float minRoll = gyro.GetMinimum<float>(sPlane);

            if (Math.Abs(rollAngle) > 1.0)
            {
                targetRoll = (float)maxRoll * (float)(rollAngle);
            }
            else if (Math.Abs(rollAngle) > .7)
            {
                // need to dampen 
                targetRoll = (float)maxRoll * (float)(rollAngle) / 4;
            }
            else if (Math.Abs(rollAngle) > 0.5)
            {
                targetRoll = 0.11f * Math.Sign(rollAngle);
            }
            else if (Math.Abs(rollAngle) > 0.1)
            {
                targetRoll = 0.07f * Math.Sign(rollAngle);
            }
            else if (Math.Abs(rollAngle) > 0.01)
            {
                targetRoll = 0.05f * Math.Sign(rollAngle);
            }
            else if (Math.Abs(rollAngle) > 0.001)
            {
                targetRoll = 0.035f * Math.Sign(rollAngle);
            }
            else targetRoll = 0;

            //				Echo("targetRoll=" + targetRoll); 
            //	rollLevel = (int)(targetRoll * 1000); 

            for (int i = 0; i < gyros.Count; i++)
            {
                gyro = gyros[i] as IMyGyro;
                gyro.SetValueFloat(sPlane, targetRoll);
                gyro.SetValueBool("Override", true);
            }
            return true;
        }

        double calculateDistance(Vector3D a, Vector3D b)
        {
            double x = a.GetDim(0) - b.GetDim(0);
            double y = a.GetDim(1) - b.GetDim(1);
            double z = a.GetDim(2) - b.GetDim(2);
            return Math.Sqrt((x * x) + (y * y) + (z * z));
        }

        #region Grid2World 
        // from http://forums.keenswh.com/threads/library-grid-to-world-coordinates.7284828/ 
        MatrixD GetGrid2WorldTransform(IMyCubeGrid grid)
        { Vector3D origin = grid.GridIntegerToWorld(new Vector3I(0, 0, 0)); Vector3D plusY = grid.GridIntegerToWorld(new Vector3I(0, 1, 0)) - origin; Vector3D plusZ = grid.GridIntegerToWorld(new Vector3I(0, 0, 1)) - origin; return MatrixD.CreateScale(grid.GridSize) * MatrixD.CreateWorld(origin, -plusZ, plusY); }
        MatrixD GetBlock2WorldTransform(IMyCubeBlock blk)
        { Matrix blk2grid; blk.Orientation.GetMatrix(out blk2grid); return blk2grid * MatrixD.CreateTranslation(((Vector3D)new Vector3D(blk.Min + blk.Max)) / 2.0) * GetGrid2WorldTransform(blk.CubeGrid); }
        #endregion

    }
}