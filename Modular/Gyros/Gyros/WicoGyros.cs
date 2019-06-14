using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        #region Gyros
        class WicoGyros
        {
            // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461 
            List<IMyGyro> allLocalGyros = new List<IMyGyro>();
            List<IMyGyro> useGyros = new List<IMyGyro>();


            Program thisProgram;

            public IMyShipController gyroControl;

            /// <summary>
            /// GYRO:How much power to use 0 to 1.0
            /// </summary>
            double CTRL_COEFF = 0.9;

            /// <summary>
            /// GYRO:max number of gyros to use to align craft. Leaving some available allows for player control to continue during auto-align 
            /// </summary>
            int LIMIT_GYROS = 1;

            public WicoGyros(Program program, IMyShipController myShipController)
            {
                thisProgram = program;
                gyroControl = myShipController;
                GyrosInit();
            }

            string sGridSection = "GRIDS";

            public void GyrosInit()
            {
                thisProgram._CustomDataIni.Get(sGridSection, "CTRL_COEFF").ToDouble(CTRL_COEFF);
                thisProgram._CustomDataIni.Get(sGridSection, "LIMIT_GYROS").ToInt32(LIMIT_GYROS);

                thisProgram._CustomDataIni.Set(sGridSection, "CTRL_COEFF", CTRL_COEFF);
                thisProgram._CustomDataIni.Set(sGridSection, "LIMIT_GYROS", LIMIT_GYROS);

                // In case we had previous control info; reset it.
                allLocalGyros.Clear();
                foreach (var gyro in useGyros)
                {
                    gyro.GyroOverride = false;
                }
                useGyros.Clear();

                // Minimal init; just add handlers
                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyGyro)
                {
                    // TODO: Ignore cutters, etc
                    allLocalGyros.Add(tb as IMyGyro);
                }
            }
            void LocalGridChangedHandler()
            {
                gyrosOff();
                gyroControl = null;
                useGyros.Clear();
                allLocalGyros.Clear();
            }
            public void SetController(IMyShipController controller = null)
            {
                if (gyroControl == null || controller != gyroControl)
                {
                    gyroControl = controller;
                    GyroSetup();
                }
            }
            void GyroSetup()
            {
                foreach (var gyro in useGyros)
                {
                    gyro.GyroOverride = false;
                }
                useGyros.Clear();
                if (gyroControl == null)
                {
                    gyroControl = thisProgram.wicoBlockMaster.GetMainController();
                }
                if (gyroControl == null)
                {
                    // no good controller found
                    //                    throw new Exception("GYROS: No controller found");
                    return;
                }
                foreach (var tb in allLocalGyros)
                {
                    if (useGyros.Count >= LIMIT_GYROS)
                        break; // we are done adding
                    // only use gyros that are on same grid as the controller
                    if (tb.CubeGrid.EntityId == gyroControl.CubeGrid.EntityId)
                    {
                        // TODO: check limitations and naming options
                        useGyros.Add(tb);
                    }
                }
            }

            /// <summary>
            /// GYRO:how tight to maintain aim. Lower is tighter. Default is 0.01f
            /// </summary>
            float minAngleRad = 0.01f;

            public void SetMinAngle(float angleRad = 0.01f)
            {
                minAngleRad = angleRad;
            }

            public float GetMinAngle()
            {
                return minAngleRad;
            }

            public int GyrosAvailable()
            {
                return useGyros.Count;
            }

            /// <summary>
            /// Try to align the ship/grid with the given vector. Returns true if the ship is within minAngleRad of being aligned
            /// </summary>
            /// <param name="argument">The direction to point. "rocket" (backward),  "backward", "up","forward"</param>
            /// <param name="vDirection">the vector for the aim.</param>
            /// <param name="gyroControlPoint">the terminal block to use for orientation</param>
            /// <returns>true if aligned. Meaning the angle of error is less than minAngleRad</returns>
            public bool AlignGyros(string argument, Vector3D vDirection, IMyTerminalBlock gyroControlPoint)
            {
                Matrix or1;
                gyroControlPoint.Orientation.GetMatrix(out or1);

                Vector3D down;
                argument = argument.ToLower();
                if (argument.Contains("rocket"))
                    down = or1.Backward;
                else if (argument.Contains("up"))
                    down = or1.Up;
                else if (argument.Contains("backward"))
                    down = or1.Backward;
                else if (argument.Contains("forward"))
                    down = or1.Forward;
                else if (argument.Contains("right"))
                    down = or1.Right;
                else if (argument.Contains("left"))
                    down = or1.Left;
                else
                    down = or1.Down;

                return AlignGyros(down, vDirection, gyroControlPoint);

            }

            public bool AlignGyros(Vector3D down, Vector3D vDirection, IMyTerminalBlock gyroControlPoint)
            {
                bool bAligned = true;
                if (gyroControl == null)
                    GyroSetup();

                vDirection.Normalize();
                Matrix or1;

                for (int i1 = 0; i1 < useGyros.Count; ++i1)
                {
                    var g1 = useGyros[i1];
                    g1.Orientation.GetMatrix(out or1);

                    var localCurrent = Vector3D.Transform(down, MatrixD.Transpose(or1));
                    var localTarget = Vector3D.Transform(vDirection, MatrixD.Transpose(g1.WorldMatrix.GetOrientation()));

                    //Since the gyro ui lies, we are not trying to control yaw,pitch,roll but rather we 
                    //need a rotation vector (axis around which to rotate) 
                    var rot = Vector3D.Cross(localCurrent, localTarget);
                    double dot2 = Vector3D.Dot(localCurrent, localTarget);
                    double ang = rot.Length();
                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                    if (dot2 < 0) ang = Math.PI - ang; // compensate for >+/-90
                    if (ang < minAngleRad)
                    { // close enough 

                        g1.GyroOverride = false;
                        continue;
                    }
                    //		Echo("Auto-Level:Off level: "+(ang*180.0/3.14).ToString()+"deg"); 
                    thisProgram.Echo("Auto-Level:Off level: "+(ang*180.0/3.14).ToString()+"deg"); 

                    float yawMax = (float)(2 * Math.PI);

                    double ctrl_vel = yawMax * (ang / Math.PI) * CTRL_COEFF;

                    ctrl_vel = Math.Min(yawMax, ctrl_vel);
                    ctrl_vel = Math.Max(0.01, ctrl_vel);
                    rot.Normalize();
                    rot *= ctrl_vel;

                    float pitch = -(float)rot.X;
                    if (Math.Abs(g1.Pitch - pitch) > 0.01)
                        g1.Pitch = pitch;

                    float yaw = -(float)rot.Y;
                    if (Math.Abs(g1.Yaw - yaw) > 0.01)
                        g1.Yaw = yaw;

                    float roll = -(float)rot.Z;
                    if (Math.Abs(g1.Roll - roll) > 0.01)
                        g1.Roll = roll;

                    //		g.SetValueFloat("Power", 1.0f); 
                    g1.GyroOverride = true;

                    bAligned = false;
                }
                return bAligned;
            }
            public int NumberAllGyros()
            {
                return allLocalGyros.Count();
            }
            public int NumberUsedGyros()
            {
                return useGyros.Count();
            }
            /// <summary>
            /// Turns off all overrides on controlled Gyros
            /// </summary>
            public void gyrosOff()
            {
                if (useGyros != null)
                {
                    for (int i1 = 0; i1 < useGyros.Count; ++i1)
                    {
                        useGyros[i1].GyroOverride = false;
                        useGyros[i1].Enabled = true;
                    }
                }
            }

            // FROM: http://steamcommunity.com/sharedfiles/filedetails/?id=498780349  
            // IMyGyroControl class v1.0  
            // Developed by Lynnux  
            // The Avionics:IMyGyroControl is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.  
            // To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/4.0/.   
            public const Base6Directions.Direction Forward = Base6Directions.Direction.Forward;
            public const Base6Directions.Direction Backward = Base6Directions.Direction.Backward;
            public const Base6Directions.Direction Left = Base6Directions.Direction.Left;
            public const Base6Directions.Direction Right = Base6Directions.Direction.Right;
            public const Base6Directions.Direction Up = Base6Directions.Direction.Up;
            public const Base6Directions.Direction Down = Base6Directions.Direction.Down;

            public float MaxYPR = 30.0f;
            //            public List<IMyTerminalBlock> Gyros = new List<IMyTerminalBlock>();
            //            public List<IMyGyro> Gyros = new List<IMyGyro>();

            Base6Directions.Direction YawAxisDir = Up;
            Base6Directions.Direction PitchAxisDir = Left;
            Base6Directions.Direction RollAxisDir = Forward;
            Base6Directions.Direction RefUp = Up;
//            Base6Directions.Direction RefLeft = Left;
//            Base6Directions.Direction RefForward = Forward;

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
            public void SetAxis(IMyGyro gyro, string Axis, float value)
            {
                if (Axis == "Yaw")
                {
                    gyro.Yaw = value;
                }
                else if (Axis == "Pitch")
                {
                    gyro.Pitch = value;

                }
                else //roll
                {
                    gyro.Roll = value;
                }

            }
            public void SetYaw(float yaw)
            {
                //                for (int i = 0; i < Gyros.Count; i++)
                foreach (var gyro in useGyros)
                {
                    string Axis;
                    float sign;
                    Vector3 RotatedVector = Base6Directions.GetVector(Up);
                    Vector3.TransformNormal(ref RotatedVector, gyro.Orientation, out RotatedVector);
                    YawAxisDir = Base6Directions.GetDirection(ref RotatedVector);
                    GetAxisAndDir(RefUp, out Axis, out sign);
                    //                    Echo("Set axis=" + Azis + " yaw="+yaw.ToString("0.00")+" sign=" + sign);

                    SetAxis(gyro, Axis, sign * yaw);

                    gyro.Enabled = true;
                    gyro.GyroOverride = true;
                }
            }
            public bool DoRotate(double rollAngle, string sPlane = "Roll", float maxYPR = -1, float facingFactor = 1f)
            {
                //            Echo("DR:angle=" + rollAngle.ToString("0.00"));
                float targetNewSetting = 0;


                //            float maxRoll = (float)(2 * Math.PI);  this SHOULD be the new constant.. but code must use old constant
                // TODO: this constant is no longer reasonable..  The adjustments are just magic calculations and should be redone.
                float maxRoll = 60f;

                //                       IMyGyro gyro = gyros[0] as IMyGyro;
                //           float maxRoll = gyro.GetMaximum<float>(sPlane);
                //            Echo("MAXROLL=" + maxRoll);

                if (maxYPR > 0) maxRoll = maxYPR;

                //           float minRoll = gyro.GetMinimum<float>(sPlane);

                if (Math.Abs(rollAngle) > 1.0)
                {
                    //                Echo("MAx gyro");
                    targetNewSetting = maxRoll * (float)(rollAngle) * facingFactor;
                }
                else if (Math.Abs(rollAngle) > .7)
                {
                    // need to dampen 
                    //                 Echo(".7 gyro");
                    targetNewSetting = maxRoll * (float)(rollAngle) / 4;
                }
                else if (Math.Abs(rollAngle) > 0.5)
                {
                    //                 Echo(".5 gyro");
                    targetNewSetting = 0.11f * Math.Sign(rollAngle);
                }
                else if (Math.Abs(rollAngle) > 0.1)
                {
                    //                 Echo(".1 gyro");
                    targetNewSetting = 0.11f * Math.Sign(rollAngle);
                }
                else if (Math.Abs(rollAngle) > 0.01)
                {
                    //                 Echo(".01 gyro");
                    targetNewSetting = 0.11f * Math.Sign(rollAngle);
                }
                else if (Math.Abs(rollAngle) > 0.001)
                {
                    //                 Echo(".001 gyro");
                    targetNewSetting = 0.09f * Math.Sign(rollAngle);
                }
                else targetNewSetting = 0;

                SetYaw(targetNewSetting);
                if (Math.Abs(rollAngle) < minAngleRad)
                {
                    gyrosOff(); //SetOverride(false);
                    //                GyroControl.RequestEnable(true);
                }
                else
                {
                    // now done in the routines
                    //                    SetOverride(true);
                    //                    RequestEnable(true);
                    return false;
                }

                return true;
            }
        }

        #endregion
    }
}
