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
        #region avionicsgyrocontrol  

        // FROM: http://steamcommunity.com/sharedfiles/filedetails/?id=498780349  

        // IMyGyroControl class v1.0  
        // Developed by Lynnux  
        // The Avionics:IMyGyroControl is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.  
        // To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/4.0/.   

        IMyGyroControl GyroControl = new IMyGyroControl();

        class IMyGyroControl
        {
            public const Base6Directions.Direction Forward = Base6Directions.Direction.Forward;
            public const Base6Directions.Direction Backward = Base6Directions.Direction.Backward;
            public const Base6Directions.Direction Left = Base6Directions.Direction.Left;
            public const Base6Directions.Direction Right = Base6Directions.Direction.Right;
            public const Base6Directions.Direction Up = Base6Directions.Direction.Up;
            public const Base6Directions.Direction Down = Base6Directions.Direction.Down;

            public float MaxYPR = 30.0f;
            public List<IMyTerminalBlock> Gyros = new List<IMyTerminalBlock>();

            Base6Directions.Direction YawAxisDir = Up;
            Base6Directions.Direction PitchAxisDir = Left;
            Base6Directions.Direction RollAxisDir = Forward;
            Base6Directions.Direction RefUp = Up;
            Base6Directions.Direction RefLeft = Left;
            Base6Directions.Direction RefForward = Forward;


            //One of the next two methods is necessary, 1st to call  
            //to pass your list of gyros to control...  
            public void UpdateGyroList(List<IMyTerminalBlock> GyroList)
            {
                Gyros = GyroList;
                if (Gyros.Count > 0) MaxYPR = Gyros[0].GetMaximum<float>("Yaw");
            }
            public void UpdateGyroList(List<IMyGyro> GyroList)
            {
                Gyros.Clear();
                for (int i = 0; i < GyroList.Count; i++)
                {
                    Gyros.Add((GyroList[i] as IMyTerminalBlock));//  ;
                }
                if (Gyros.Count > 0) MaxYPR = Gyros[0].GetMaximum<float>("Yaw");
            }

            //...or IMyGyroControl looks for all gyros on the grid of the programmable block  
            public void UpdateGyroList(IMyProgrammableBlock PB, IMyGridTerminalSystem GTS)
            {
                Gyros.Clear();
                if ((GTS != null) && (PB != null)) GTS.GetBlocksOfType<IMyGyro>(Gyros, x => ((x.CubeGrid == PB.CubeGrid) && x.IsFunctional));
                if (Gyros.Count > 0) MaxYPR = Gyros[0].GetMaximum<float>("Yaw");
            }

            //recommended but not necessary, third to call if used (gyro system is set up then)  
            //passing OrientBlock=null will use the grid as reference   
            public void SetRefBlock(IMyTerminalBlock OrientBlock,
                        Base6Directions.Direction DirForward = Forward,
                        Base6Directions.Direction DirUp = Up)
            {
                if (Base6Directions.GetAxis(DirForward) == Base6Directions.GetAxis(DirUp))
                    DirUp = Base6Directions.GetPerpendicular(DirForward);
                if (OrientBlock == null)
                {
                }
                else
                {
                    Vector3 RotatedVector = Base6Directions.GetVector(DirForward);
                    Vector3.TransformNormal(ref RotatedVector, OrientBlock.Orientation, out RotatedVector);
                    DirForward = Base6Directions.GetDirection(ref RotatedVector);
                    RotatedVector = Base6Directions.GetVector(DirUp);
                    Vector3.TransformNormal(ref RotatedVector, OrientBlock.Orientation, out RotatedVector);
                    DirUp = Base6Directions.GetDirection(ref RotatedVector);
                }
                RefUp = DirUp;
                RefForward = DirForward;
                RefLeft = Base6Directions.GetLeft(RefUp, RefForward);
            }

            public void SetOverride(bool value)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    Gyros[i].SetValueBool("Override", value);
                }
            }

            public void SetOverride(int gyro, bool value)
            {
                if (gyro < Gyros.Count)
                {
                    Gyros[gyro].SetValueBool("Override", value);
//                    Gyros[gyro].SetValueBool("Override", value);
                }
            }

            public void SetPower(float power)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    Gyros[i].SetValueFloat("Power", power);
                }
            }

            public void SetPower(int gyro, float power)
            {
                if (gyro < Gyros.Count)
                {
                    Gyros[gyro].SetValueFloat("Power", power);
                }
            }

            public void RequestEnable(bool enable)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    (Gyros[i] as IMyGyro).Enabled = enable;
                }
            }

            public void RequestEnable(int gyro, bool enable)
            {
                if (gyro < Gyros.Count)
                {
                    (Gyros[gyro] as IMyGyro).Enabled = enable;
                }
            }

            public void ShowOnHUD(bool show)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    Gyros[i].SetValueBool("ShowOnHUD", show);
                }
            }

            public void ShowOnHUD(int gyro, bool show)
            {
                if (gyro < Gyros.Count)
                {
                    Gyros[gyro].SetValueBool("ShowOnHUD", show);
                }
            }

            void GetAxisAndDir(Base6Directions.Direction RefDir, out string Axis, out float sign)
            {
                Axis = "Yaw";
                sign = -1.0f;
                if (Base6Directions.GetAxis(YawAxisDir) == Base6Directions.GetAxis(RefDir))
                {
                    if (YawAxisDir == RefDir) sign = 1.0f;
                }
                if (Base6Directions.GetAxis(PitchAxisDir) == Base6Directions.GetAxis(RefDir))
                {
                    Axis = "Pitch";
                    if (PitchAxisDir == RefDir) sign = 1.0f;
                }
                if (Base6Directions.GetAxis(RollAxisDir) == Base6Directions.GetAxis(RefDir))
                {
                    Axis = "Roll";
                    if (RollAxisDir == RefDir) { } else sign = 1.0f;
                }
            }

            public void SetYaw(float yaw)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    string Axis;
                    float sign;
                    Vector3 RotatedVector = Base6Directions.GetVector(Up);
                    Vector3.TransformNormal(ref RotatedVector, Gyros[i].Orientation, out RotatedVector);
                    YawAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    GetAxisAndDir(RefUp, out Axis, out sign);
//                    Echo("Set axis=" + Azis + " yaw="+yaw.ToString("0.00")+" sign=" + sign);
                    Gyros[i].SetValueFloat(Axis, sign * yaw);
                }
            }

            public void SetPitch(float pitch)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    string Axis;
                    float sign;
                    Vector3 RotatedVector = Base6Directions.GetVector(Left);
                    Vector3.TransformNormal(ref RotatedVector, Gyros[i].Orientation, out RotatedVector);
                    PitchAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    GetAxisAndDir(RefLeft, out Axis, out sign);
                    Gyros[i].SetValueFloat(Axis, sign * pitch);
                }
            }

            public void SetRoll(float roll)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    string Axis;
                    float sign;
                    Vector3 RotatedVector = Base6Directions.GetVector(Forward);
                    Vector3.TransformNormal(ref RotatedVector, Gyros[i].Orientation, out RotatedVector);
                    RollAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    GetAxisAndDir(RefForward, out Axis, out sign);
                    Gyros[i].SetValueFloat(Axis, sign * roll);
                }
            }

            public void SetYawPitchRoll(float yaw, float pitch, float roll)
            {
                for (int i = 0; i < Gyros.Count; i++)
                {
                    string Axis;
                    float sign;
                    Vector3 RotatedVector = Base6Directions.GetVector(Forward);
                    Vector3.TransformNormal(ref RotatedVector, Gyros[i].Orientation, out RotatedVector);
                    RollAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    RotatedVector = Base6Directions.GetVector(Left);
                    Vector3.TransformNormal(ref RotatedVector, Gyros[i].Orientation, out RotatedVector);
                    PitchAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    RotatedVector = Base6Directions.GetVector(Up);
                    Vector3.TransformNormal(ref RotatedVector, Gyros[i].Orientation, out RotatedVector);
                    YawAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    GetAxisAndDir(RefUp, out Axis, out sign);
                    Gyros[i].SetValueFloat(Axis, sign * yaw);
                    GetAxisAndDir(RefLeft, out Axis, out sign);
                    Gyros[i].SetValueFloat(Axis, sign * pitch);
                    GetAxisAndDir(RefForward, out Axis, out sign);
                    Gyros[i].SetValueFloat(Axis, sign * roll);
                }
            }
        }
        #endregion

        double CalculateYaw(Vector3D destination, IMyTerminalBlock Origin)
        {
            double yawAngle = 0;
            bool facingTarget = false;

            MatrixD refOrientation = GetBlock2WorldTransform(Origin);

            Vector3D vCenter = Origin.GetPosition();
            Vector3D vBack = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Backward);
//            Vector3D vUp = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Up);
            Vector3D vRight = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Right);
            Vector3D vLeft = vCenter - 1.0 * Vector3D.Normalize(refOrientation.Right);

 //           debugGPSOutput("vCenter", vCenter);
  //          debugGPSOutput("vBack", vBack);
 //           debugGPSOutput("vUp", vUp);
 //           debugGPSOutput("vRight", vRight);

            
  //          double centerTargetDistance = calculateDistance(vCenter, destination);
  //          double upTargetDistance = calculateDistance(vUp, destination);
  //          double backTargetDistance = calculateDistance(vBack, destination);
  //          double rightLocalDistance = calculateDistance(vRight, vCenter);
            double rightTargetDistance = calculateDistance(vRight, destination);

            double leftTargetDistance = calculateDistance(vLeft, destination);

            double yawLocalDistance = calculateDistance(vRight, vLeft);


            double centerTargetDistance = Vector3D.DistanceSquared(vCenter, destination);
            double backTargetDistance = Vector3D.DistanceSquared(vBack, destination);
            /*
            double upTargetDistance = Vector3D.DistanceSquared(vUp, destination);
            double rightLocalDistance = Vector3D.DistanceSquared(vRight, vCenter);
            double rightTargetDistance = Vector3D.DistanceSquared(vRight, destination);

            double leftTargetDistance = Vector3D.DistanceSquared(vLeft, destination);

            double yawLocalDistance = Vector3D.DistanceSquared(vRight, vLeft);
            */
            facingTarget = centerTargetDistance < backTargetDistance;

            yawAngle = (leftTargetDistance - rightTargetDistance) / yawLocalDistance;
            Echo("calc Angle=" + Math.Round(yawAngle, 5));

            if (!facingTarget)
            {
                //Echo("YAW:NOT FACING!"); 
                yawAngle += (yawAngle < 0) ? -1 : 1;
            }
            //	Echo("yawangle=" + Math.Round(yawAngle,5)); 
            return yawAngle;
        }

        double calculateDistance(Vector3D a, Vector3D b)
        {
            return Vector3D.Distance(a, b);
        }

        #region Grid2World
        // from http://forums.keenswh.com/threads/library-grid-to-world-coordinates.7284828/
        MatrixD GetGrid2WorldTransform(IMyCubeGrid grid)
        { Vector3D origin = grid.GridIntegerToWorld(new Vector3I(0, 0, 0)); Vector3D plusY = grid.GridIntegerToWorld(new Vector3I(0, 1, 0)) - origin; Vector3D plusZ = grid.GridIntegerToWorld(new Vector3I(0, 0, 1)) - origin; return MatrixD.CreateScale(grid.GridSize) * MatrixD.CreateWorld(origin, -plusZ, plusY); }
        MatrixD GetBlock2WorldTransform(IMyCubeBlock blk)
        { Matrix blk2grid; blk.Orientation.GetMatrix(out blk2grid); return blk2grid * MatrixD.CreateTranslation(((Vector3D)new Vector3D(blk.Min + blk.Max)) / 2.0) * GetGrid2WorldTransform(blk.CubeGrid); }
        #endregion

        bool DoRotate(double rollAngle, string sPlane = "Roll", float maxYPR=-1)
        {
            Echo("DR:angle=" + rollAngle.ToString("0.00"));
            float targetRoll = 0;
            IMyGyro gyro = gyros[0] as IMyGyro;
            float maxRoll = gyro.GetMaximum<float>(sPlane);
            if (maxYPR > 0) maxRoll = maxYPR;

            //           float minRoll = gyro.GetMinimum<float>(sPlane);

            if (Math.Abs(rollAngle) > 1.0)
            {
                Echo("MAx gyro");
                targetRoll = (float)maxRoll * (float)(rollAngle);
            }
            else if (Math.Abs(rollAngle) > .7)
            {
                // need to dampen 
                 Echo(".7 gyro");
               targetRoll = (float)maxRoll * (float)(rollAngle) / 4;
            }
            else if (Math.Abs(rollAngle) > 0.5)
            {
                 Echo(".5 gyro");
                targetRoll = 0.11f * Math.Sign(rollAngle);
            }
            else if (Math.Abs(rollAngle) > 0.1)
            {
                 Echo(".1 gyro");
                targetRoll = 0.11f * Math.Sign(rollAngle);
            }
            else if (Math.Abs(rollAngle) > 0.01)
            {
                 Echo(".01 gyro");
                targetRoll = 0.11f * Math.Sign(rollAngle);
            }
            else if (Math.Abs(rollAngle) > 0.001)
            {
                 Echo(".001 gyro");
                targetRoll = 0.09f * Math.Sign(rollAngle);
            }
            else targetRoll = 0;

            GyroControl.SetYaw(targetRoll);
            GyroControl.SetOverride(true);
            GyroControl.RequestEnable(true);

            return true;
        }

    }
}