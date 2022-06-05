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
        public class WicoGyros
        {
            // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461 
            List<IMyGyro> allLocalGyros = new List<IMyGyro>();
            List<IMyGyro> useGyros = new List<IMyGyro>();


            public IMyShipController gyroControl;

            /// <summary>
            /// GYRO:How much power to use 0 to 1.0
            /// </summary>
            double CTRL_COEFF = 0.9;

            /// <summary>
            /// GYRO:max number of gyros to use to align craft. Leaving some available allows for player control to continue during auto-align 
            /// </summary>
            int LIMIT_GYROS = 1;

            Program _program;
            WicoBlockMaster _wicoBlockMaster;


            public WicoGyros(Program program, WicoBlockMaster wbm)
            {
                _program = program;
                _wicoBlockMaster = wbm;

                GyrosInit();
            }

            string sGridSection = "GRIDS";

            public void GyrosInit()
            {
                CTRL_COEFF=_program.CustomDataIni.Get(sGridSection, "CTRL_COEFF").ToDouble(CTRL_COEFF);
                LIMIT_GYROS = _program.CustomDataIni.Get(sGridSection, "LIMIT_GYROS").ToInt32(LIMIT_GYROS);

                _program.CustomDataIni.Set(sGridSection, "CTRL_COEFF", CTRL_COEFF);
                _program.CustomDataIni.Set(sGridSection, "LIMIT_GYROS", LIMIT_GYROS);

                // In case we had previous control info; reset it.
                allLocalGyros.Clear();
                foreach (var gyro in useGyros)
                {
                    gyro.GyroOverride = false;
                }
                useGyros.Clear();

                // Minimal init; just add handlers
                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddResetMotionHandler(ResetMotionHandler);
            }

            void ResetMotionHandler(bool bNoDrills=false)
            {
                gyrosOff();
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyGyro)
                {
                    allLocalGyros.Add(tb as IMyGyro);
                    // TODO: Limit gyros used so not all gyros are used so that player can still have control
//                    useGyros.Add(tb as IMyGyro);
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
                    gyroControl = _wicoBlockMaster.GetMainController();
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
            /// <param name="vDirection">the world vector for the aim.</param>
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

                return AlignGyros(down, vDirection);

            }

            /// <summary>
            /// Align the direction of the ship to the aim at the target
            /// </summary>
            /// <param name="vDirection">GRID orientation to use for aiming</param>
            /// <param name="vTarget">World VECTOR to aim the grid</param>
            /// <returns>true if aimed within tolerances</returns>
            public bool AlignGyros(Vector3D vDirection, Vector3D vTarget )
            {
                bool bAligned = true;
                if (gyroControl == null)
                    GyroSetup();
//                _program.Echo("vDirection= " + vDirection.ToString());
//                _program.Echo("vTarget= " + vTarget.ToString());

                vTarget.Normalize();
                Matrix or1;

                for (int i1 = 0; i1 < useGyros.Count; ++i1)
                {
                    var g1 = useGyros[i1];
                    g1.Orientation.GetMatrix(out or1);

                    var localCurrent = Vector3D.Transform(vDirection, MatrixD.Transpose(or1));
                    var localTarget = Vector3D.Transform(vTarget, MatrixD.Transpose(g1.WorldMatrix.GetOrientation()));

                    //Since the gyro ui lies, we are not trying to control yaw,pitch,roll but rather we 
                    //need a rotation vector (axis around which to rotate) 
                    var rot = Vector3D.Cross(localCurrent, localTarget);
                    double dot2 = Vector3D.Dot(localCurrent, localTarget);
                    double ang = rot.Length();
                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                    if (dot2 < 0) ang = Math.PI - ang; // compensate for >+/-90
//                    _program.Echo("Ang=" + ang);
                    if (ang < minAngleRad)
                    { // close enough 
                        g1.GyroOverride = false;
                        continue;
                    }
//                    _program.Echo("Auto-Level:Off level: " + (ang * 180.0 / 3.14).ToString("0.0") + " deg");

                    float yawMax = (float)(2 * Math.PI);

                    double ctrl_vel = yawMax * (ang / Math.PI) * CTRL_COEFF;

                    bAligned = false;

                    ctrl_vel = Math.Min(yawMax, ctrl_vel);
                    ctrl_vel = Math.Max(0.01, ctrl_vel);
                    rot.Normalize();
                    rot *= ctrl_vel;

                    float pitch = -(float)rot.X;
                    if (Math.Abs(g1.Pitch - pitch) > 0.01)
                    {
                        g1.Pitch = pitch;
                        g1.GyroOverride = true;
                    }

                    float yaw = -(float)rot.Y;
                    if (Math.Abs(g1.Yaw - yaw) > 0.01)
                    { 
                        g1.Yaw = yaw;
                        g1.GyroOverride = true;
                    }

                    float roll = -(float)rot.Z;
                    if (Math.Abs(g1.Roll - roll) > 0.01)
                    { 
                        g1.Roll = roll;
                        g1.GyroOverride = true;
                    }
                }
                return bAligned;
            }

            public bool BeamRider(Vector3D vStart, Vector3D vEnd, IMyTerminalBlock OrientationBlock)
            {
                // 'BeamRider' routine that takes start,end and tries to stay on that beam.
                bool bAimed = false;
                Vector3D vBoreEnd = (vEnd - vStart);
                Vector3D vPosition;
                if (OrientationBlock is IMyShipController)
                {
                    vPosition = ((IMyShipController)OrientationBlock).CenterOfMass;
                }
                else
                {
                    vPosition = OrientationBlock.GetPosition();
                }
                Vector3D vAimEnd = (vEnd - vPosition);
                Vector3D vRejectEnd = VectorRejection(vBoreEnd, vAimEnd);

                Vector3D vCorrectedAim = (vEnd - vRejectEnd * 2) - vPosition;
                Matrix or1;
                OrientationBlock.Orientation.GetMatrix(out or1);
                bAimed = AlignGyros(or1.Forward, vCorrectedAim);
                bAimed = AlignGyros("forward", vCorrectedAim, OrientationBlock);
                return bAimed;
            }

            // From Whip. on discord
            public Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
            {
                if (Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a - a.Dot(b) / b.LengthSquared() * b;
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

            /*
            Whip's GetRotationAnglesSimultaneous - Last modified: 2020/08/27

            Gets axis angle rotation and decomposes it upon each cardinal axis.
            Has the desired effect of not causing roll oversteer. Does NOT use
            sequential rotation angles.

            Set desiredUpVector to Vector3D.Zero if you don't care about roll.

            NOTE: 
            This is designed for use with Keen's gyroscopes where:
              + pitch = -X rotation
              + yaw   = -Y rotation
              + roll  = -Z rotation

            As such, the resulting angles are negated since they are computed
            using the opposite sign assumption.

            Dependencies:
            VectorMath.SafeNormalize
            */
            public void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll)
            {
                desiredForwardVector = VectorMath.SafeNormalize(desiredForwardVector);

                MatrixD transposedWm;
                MatrixD.Transpose(ref worldMatrix, out transposedWm);
                Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
                Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

                Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
                Vector3D axis;
                double angle;
                if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector))
                {
                    axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
                    angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
                }
                else
                {
                    leftVector = VectorMath.SafeNormalize(leftVector);
                    Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);

                    // Create matrix
                    MatrixD targetMatrix = MatrixD.Zero;
                    targetMatrix.Forward = desiredForwardVector;
                    targetMatrix.Left = leftVector;
                    targetMatrix.Up = upVector;

                    axis = new Vector3D(targetMatrix.M23 - targetMatrix.M32,
                                        targetMatrix.M31 - targetMatrix.M13,
                                        targetMatrix.M12 - targetMatrix.M21);

                    double trace = targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33;
                    angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1));
                }

                if (Vector3D.IsZero(axis))
                {
                    angle = desiredForwardVector.Z < 0 ? 0 : Math.PI;
                    yaw = angle;
                    pitch = 0;
                    roll = 0;
                    return;
                }

                axis = VectorMath.SafeNormalize(axis);
                // Because gyros rotate about -X -Y -Z, we need to negate our angles
                yaw = -axis.Y * angle;
                pitch = -axis.X * angle;
                roll = -axis.Z * angle;
            }
            /*
            Whip's ApplyGyroOverride - Last modified: 2020/08/27

            Takes pitch, yaw, and roll speeds relative to the gyro's backwards
            ass rotation axes. 
            */
            public void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
            {
                var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
                var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);
                
                if (useGyros.Count < 1) GyroSetup();
                if (gyroList == null) // added by wico
                    gyroList = useGyros;

                foreach (var thisGyro in gyroList)
                {
                    var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));

                    thisGyro.Pitch = (float)transformedRotationVec.X;
                    thisGyro.Yaw = (float)transformedRotationVec.Y;
                    thisGyro.Roll = (float)transformedRotationVec.Z;
                    thisGyro.GyroOverride = true;
                }
            }

            public float MaxYPR = 30.0f;
            //            public List<IMyTerminalBlock> Gyros = new List<IMyTerminalBlock>();
            //            public List<IMyGyro> Gyros = new List<IMyGyro>();

            public void SetYaw(float yaw)
            {
                //                for (int i = 0; i < Gyros.Count; i++)
                if (useGyros.Count < 1) GyroSetup();
                ApplyGyroOverride(0, yaw, 0, useGyros, _wicoBlockMaster.GetMainController().WorldMatrix);
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
                    _program.Echo("Max gyro");
                    targetNewSetting = maxRoll * (float)(rollAngle) * facingFactor;
                }
                else if (Math.Abs(rollAngle) > .7)
                {
                    // need to dampen 
                    _program.Echo(".7 gyro");
                    targetNewSetting = maxRoll * (float)(rollAngle) / 4;
                }
                else if (Math.Abs(rollAngle) > 0.5)
                {
                    _program.Echo(".5 gyro");
                    targetNewSetting = 0.11f * Math.Sign(rollAngle);
                }
                else if (Math.Abs(rollAngle) > 0.1)
                {
                    _program.Echo(".1 gyro");
                    targetNewSetting = 0.11f * Math.Sign(rollAngle);
                }
                else if (Math.Abs(rollAngle) > 0.01)
                {
                    _program.Echo(".01 gyro");
                    targetNewSetting = 0.11f * Math.Sign(rollAngle);
                }
                else if (Math.Abs(rollAngle) > 0.001)
                {
                    _program.Echo(".001 gyro");
                    targetNewSetting = 0.09f * Math.Sign(rollAngle);
                }
                else targetNewSetting = 0;

                SetYaw(targetNewSetting);
                if (Math.Abs(rollAngle) < minAngleRad)
                {
                    _program.Echo("rollAngle<minAngleRad");
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

            /// <summary>
            /// Returns desired Yaw to have Origin block face destination
            /// </summary>
            /// <param name="destination">World coordinates point to aim at</param>
            /// <param name="Origin">block to face forward</param>
            /// <returns></returns>
            public double CalculateYaw(Vector3D destination, IMyTerminalBlock Origin)
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
                facingTarget = centerTargetDistance < backTargetDistance;

                yawAngle = (leftTargetDistance - rightTargetDistance) / yawLocalDistance;
                //            Echo("calc Angle=" + Math.Round(yawAngle, 5));

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

            public double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians  
            {
                if (a.LengthSquared() == 0 || b.LengthSquared() == 0)
                    return 0;
                else
                    return Math.Acos(MathHelper.Clamp(a.Dot(b) / a.Length() / b.Length(), -1, 1));
            }

            #region Grid2World
            // from http://forums.keenswh.com/threads/library-grid-to-world-coordinates.7284828/
            MatrixD GetGrid2WorldTransform(IMyCubeGrid grid)
            { Vector3D origin = grid.GridIntegerToWorld(new Vector3I(0, 0, 0)); Vector3D plusY = grid.GridIntegerToWorld(new Vector3I(0, 1, 0)) - origin; Vector3D plusZ = grid.GridIntegerToWorld(new Vector3I(0, 0, 1)) - origin; return MatrixD.CreateScale(grid.GridSize) * MatrixD.CreateWorld(origin, -plusZ, plusY); }
            MatrixD GetBlock2WorldTransform(IMyCubeBlock blk)
            { Matrix blk2grid; blk.Orientation.GetMatrix(out blk2grid); return blk2grid * MatrixD.CreateTranslation(((Vector3D)new Vector3D(blk.Min + blk.Max)) / 2.0) * GetGrid2WorldTransform(blk.CubeGrid); }
            #endregion

            // https://github.com/Whiplash141/SpaceEngineersScripts/blob/master/Unpolished/VectorMath.cs
            public static class VectorMath
            {
                /// <summary>
                ///  Normalizes a vector only if it is non-zero and non-unit
                /// </summary>
                public static Vector3D SafeNormalize(Vector3D a)
                {
                    if (Vector3D.IsZero(a))
                        return Vector3D.Zero;

                    if (Vector3D.IsUnit(ref a))
                        return a;

                    return Vector3D.Normalize(a);
                }

                /// <summary>
                /// Reflects vector a over vector b with an optional rejection factor
                /// </summary>
                public static Vector3D Reflection(Vector3D a, Vector3D b, double rejectionFactor = 1) //reflect a over b
                {
                    Vector3D project_a = Projection(a, b);
                    Vector3D reject_a = a - project_a;
                    return project_a - reject_a * rejectionFactor;
                }

                /// <summary>
                /// Rejects vector a on vector b
                /// </summary>
                public static Vector3D Rejection(Vector3D a, Vector3D b) //reject a on b
                {
                    if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                        return Vector3D.Zero;

                    return a - a.Dot(b) / b.LengthSquared() * b;
                }

                /// <summary>
                /// Projects vector a onto vector b
                /// </summary>
                public static Vector3D Projection(Vector3D a, Vector3D b)
                {
                    if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                        return Vector3D.Zero;

                    if (Vector3D.IsUnit(ref b))
                        return a.Dot(b) * b;

                    return a.Dot(b) / b.LengthSquared() * b;
                }

                /// <summary>
                /// Scalar projection of a onto b
                /// </summary>
                public static double ScalarProjection(Vector3D a, Vector3D b)
                {
                    if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                        return 0;

                    if (Vector3D.IsUnit(ref b))
                        return a.Dot(b);

                    return a.Dot(b) / b.Length();
                }

                /// <summary>
                /// Computes angle between 2 vectors in radians.
                /// </summary>
                public static double AngleBetween(Vector3D a, Vector3D b)
                {
                    if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                        return 0;
                    else
                        return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
                }

                /// <summary>
                /// Computes cosine of the angle between 2 vectors.
                /// </summary>
                public static double CosBetween(Vector3D a, Vector3D b)
                {
                    if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                        return 0;
                    else
                        return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
                }

                /// <summary>
                /// Returns if the normalized dot product between two vectors is greater than the tolerance.
                /// This is helpful for determining if two vectors are "more parallel" than the tolerance.
                /// </summary>
                /// <param name="a">First vector</param>
                /// <param name="b">Second vector</param>
                /// <param name="tolerance">Cosine of maximum angle</param>
                /// <returns></returns>
                public static bool IsDotProductWithinTolerance(Vector3D a, Vector3D b, double tolerance)
                {
                    double dot = Vector3D.Dot(a, b);
                    double num = a.LengthSquared() * b.LengthSquared() * tolerance * Math.Abs(tolerance);
                    return Math.Abs(dot) * dot > num;
                }
            }
        }

    }
}
