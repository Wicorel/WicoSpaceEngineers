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
        // read INI defaults from CustomData.  If you don't want to use my INI, remove or modify the following routine
        string sGyroIgnore = "!NAV";

        void GyroInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sGridSection, "GyroIgnore", ref sGyroIgnore, true);
            iNIHolder.GetValue(sGridSection, "LIMIT_GYROS", ref LIMIT_GYROS, true);
            iNIHolder.GetValue(sGridSection, "LEAVE_GYROS", ref LEAVE_GYROS, true);
            iNIHolder.GetValue(sGridSection, "CTRL_COEFF", ref CTRL_COEFF, true);
        }

        // 03/08/2018: Fix for change in Terminal Properties. Was using MaxValue of "Yaw" to determine range...  now that's WRONG!
        // 12/09 Add Summaries to members and functions
        // 09/11 Turn on gyros we are going to use
        // 04/30 only .ToLower ONCE
        // 04/12: add "Up"
        //04/01 90 fix 
        // 2/7: 180
        // 1/24 Update to support no RC.
        // with vDir
        // 12/24 added forward
        // fix RC init
        // !NAV to not use a gyro. Tested in space.. 
        #region Autogyro 
        // Originally from: http://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461 

        // NOTE: uses: shipOrientationBlock from other code as the designated remote or ship controller

        /// <summary>
        /// GYRO:How much power to use 0 to 1.0
        /// </summary>
        double CTRL_COEFF = 0.9;

        /// <summary>
        /// GYRO:max number of gyros to use to align craft. Leaving some available allows for player control to continue during auto-align 
        /// </summary>
        int LIMIT_GYROS = 3;

        /// <summary>
        /// GYRO:leave this many gyros free for user. less than 0 means none. (not fully tested)
        /// </summary>
        int LEAVE_GYROS = -1;

        /// <summary>
        /// GYRO:The ship controller used by Gyro control
        /// </summary>
        IMyShipController gyroControl;

        /// <summary>
        /// GYRO:The list of approved gyros to use for aiming
        /// </summary>
        List<IMyGyro> gyros;

        /// <summary>
        /// GYRO:how tight to maintain aim. Lower is tighter. Default is 0.01f
        /// </summary>
        float minAngleRad = 0.01f;

        bool GyroMain(string argument)
        {
            if (gyroControl == null)
                gyrosetup();
//            Echo("GyroMain(" + argument + ")");

            if (gyroControl is IMyShipController)
            {
                Vector3D grav = (gyroControl as IMyShipController).GetNaturalGravity();
                return GyroMain(argument, grav, shipOrientationBlock);
            }
            else
            {
                Echo("No Controller for gravity");
            }

            return true;
        }

        /// <summary>
        /// Try to align the ship/grid with the given vector. Returns true if the ship is within minAngleRad of being aligned
        /// </summary>
        /// <param name="argument">The direction to point. "rocket" (backward),  "backward", "up","forward"</param>
        /// <param name="vDirection">the vector for the aim.</param>
        /// <param name="gyroControlPoint">the terminal block to use for orientation</param>
        /// <returns>true if aligned. Meaning the angle of error is less than minAngleRad</returns>
        bool GyroMain(string argument, Vector3D vDirection, IMyTerminalBlock gyroControlPoint)
        {
            bool bAligned = true;
            if (gyroControl == null)
                gyrosetup();
//            Echo("GyroMain(" + argument + ",VECTOR3D) #Gyros=" + gyros.Count);
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
            else
                down = or1.Down;

            vDirection.Normalize();

            for (int i1 = 0; i1 < gyros.Count; ++i1)
            {
                var g1 = gyros[i1];
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

                    //g.SetValueBool("Override", false);
                    g1.GyroOverride = false;
                    continue;
                }
                //		Echo("Auto-Level:Off level: "+(ang*180.0/3.14).ToString()+"deg"); 

                /// !KEEN  Change in 1.185 or .186..  gah...
                //                float yawMax = g.GetMaximum<float>("Yaw"); // we assume all three are the same max

                float yawMax = (float)(2 * Math.PI);

                double ctrl_vel = yawMax * (ang / Math.PI) * CTRL_COEFF;

                ctrl_vel = Math.Min(yawMax, ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel);
                rot.Normalize();
                rot *= ctrl_vel;

                float pitch = -(float)rot.X;
                g1.Pitch = pitch;

                float yaw = -(float)rot.Y;
                g1.Yaw = yaw;

                float roll = -(float)rot.Z;
                g1.Roll = roll;

                //		g.SetValueFloat("Power", 1.0f); 
                g1.GyroOverride = true;

                bAligned = false;
            }
            return bAligned;
        }


        /// <summary>
        /// Initialize the gyro controls.
        /// </summary>
        /// <returns>String representing what was initialized</returns>
        string gyrosetup()
        {
            string s = "";
            var l1 = new List<IMyTerminalBlock>();
            gyroControl = shipOrientationBlock as IMyShipController;

            if (gyroControl == null)
            {
                // purposefully dont search on our own for a controller
                if (l1.Count < 1) return "No RC!";
//                gyroControl = (IMyRemoteControl)l[0];
            }
            gyrosOff(); // turn off any working gyros from previous runs
                        // NOTE: Uses grid of controller, not ME, nor localgridfilter
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(l1, x => x.CubeGrid == shipOrientationBlock.CubeGrid);
            //    s += "ALLGYRO#=" + l.Count + "#";
            var l2 = new List<IMyTerminalBlock>();
            int skipped = 0;
            for (int i1 = 0; i1 < l1.Count; i1++)
            {
                //       s += "\n" + l[i].CustomName;
                if (l1[i1].CustomName.Contains(sGyroIgnore) || l1[i1].CustomData.Contains(sGyroIgnore))
                {
                    skipped++;
                    continue;
                }
                //        s += " ADDED";
                l2.Add(l1[i1]);
            }
            gyros = l2.ConvertAll(x => (IMyGyro)x);
            if (LIMIT_GYROS > 0)
            {
                if (gyros.Count > LIMIT_GYROS)
                {
                    gyros.RemoveRange(LIMIT_GYROS, gyros.Count - LIMIT_GYROS);
                }
                else
                {
                    if ((LEAVE_GYROS - skipped) > 0)
                    {
                        int index = gyros.Count - (LEAVE_GYROS - skipped);
                        gyros.RemoveRange(index, (LEAVE_GYROS - skipped));
                    }
                }
            }
            gyrosOff(); // turn off all overrides

            s += "GYRO#" + gyros.Count.ToString("00") + "#";
            return s;
        }
        /// <summary>
        /// Turns off all overrides on controlled Gyros
        /// </summary>
        void gyrosOff()
        {
            if (gyros != null)
            {
                for (int i1 = 0; i1 < gyros.Count; ++i1)
                {
                    //gyros[i].SetValueBool("Override", false);
                    gyros[i1].GyroOverride = false;
                    gyros[i1].Enabled = true;
                }
            }
        }
        #endregion



    }
}