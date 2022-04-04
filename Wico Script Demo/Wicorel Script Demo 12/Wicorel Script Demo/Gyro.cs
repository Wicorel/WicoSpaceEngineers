using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// GYRO:how tight to maintain aim. Lower is tighter. Default is 0.01f
        /// </summary>
        float minAngleRad = 0.01f;

        /// <summary>
        /// Align the direction of the ship to the aim at the target
        /// </summary>
        /// <param name="vDirection">GRID orientation to use for aiming</param>
        /// <param name="vTarget">World VECTOR to aim the grid</param>
        /// <returns>true if aimed within tolerances</returns>
        public bool AlignGyros(Vector3D vDirection, Vector3D vTarget)
        {
            // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461 
            bool bAligned = true;

            vTarget.Normalize();
            Matrix or1;

            for (int i1 = 0; i1 < myGyros.Count; ++i1)
            {
                var g1 = myGyros[i1];
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

                double ctrl_vel = yawMax * (ang / Math.PI);

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

        /// <summary>
        /// Turns off all overrides on controlled Gyros
        /// </summary>
        public void gyrosOff()
        {
            if (myGyros != null)
            {
                for (int i1 = 0; i1 < myGyros.Count; ++i1)
                {
                    myGyros[i1].GyroOverride = false;
                    myGyros[i1].Enabled = true;
                }
            }
        }

        List<IMyGyro> myGyros = new List<IMyGyro>();

        public void GyroInit()
        {
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(myGyros
                , x1 => x1.IsSameConstructAs(Me));

        }

    }


}
