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

// uses: gpsCenter from other code as the designated remote or ship controller

double CTRL_COEFF = 0.9; 
int LIMIT_GYROS = 3; // max number of gyros to use to align craft. Leaving some available allows for player control to continue during auto-align 
int LEAVE_GYROS = -1;  // leave this many gyros free for user.


IMyShipController rc; 
List < IMyGyro > gyros;

float minAngleRad=0.01f; // how tight to maintain aim Lower is tighter. 

bool GyroMain(string argument)
{
	if (rc == null)
		gyrosetup();
//	Echo("GyroMain(" + argument + ")");

	if (rc is IMyShipController)
	{
		Vector3D grav = (rc as IMyShipController).GetNaturalGravity();
		return GyroMain(argument, grav, gpsCenter);
	}
	else
	{
		Echo("No Remote Control for gravity");
	}

	return true;
} 

bool GyroMain(string argument, Vector3D vDirection, IMyTerminalBlock gyroControlPoint)  
{
	bool bAligned=true; 
	if (rc == null) 
		gyrosetup();
	Echo("GyroMain(" + argument + ",VECTOR3D) #Gyros="+gyros.Count);	 
	Matrix or; 
	gyroControlPoint.Orientation.GetMatrix(out or); 

	Vector3D down;
	argument = argument.ToLower();
	if (argument.Contains("rocket"))
		down = or.Backward;
	else if (argument.Contains("up"))
		down = or.Up;
	else if (argument.Contains("backward"))
		down = or.Backward;
	else if (argument.Contains("forward"))
		down = or.Forward;
	else
		down = or.Down; 
 
	vDirection.Normalize();

	for (int i = 0; i < gyros.Count; ++i)  
	{ 
		var g = gyros[i];
		g.Orientation.GetMatrix(out or); 
		var localDown = Vector3D.Transform(down, MatrixD.Transpose(or)); 
		var localGrav = Vector3D.Transform(vDirection, MatrixD.Transpose(g.WorldMatrix.GetOrientation()));

		//Since the gyro ui lies, we are not trying to control yaw,pitch,roll but rather we 
		//need a rotation vector (axis around which to rotate) 
		var rot = Vector3D.Cross(localDown, localGrav);
		double dot2 = Vector3D.Dot(localDown, localGrav);
		double ang = rot.Length(); 
		ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
		if (dot2 < 0) ang = Math.PI - ang; // compensate for >+/-90
		if (ang < minAngleRad)  
		{ // close enough 
			g.SetValueBool("Override", false); 
			continue; 
		} 
//		Echo("Auto-Level:Off level: "+(ang*180.0/3.14).ToString()+"deg"); 
 
		double ctrl_vel = g.GetMaximum < float > ("Yaw") * (ang / Math.PI) * CTRL_COEFF; 
		ctrl_vel = Math.Min(g.GetMaximum < float > ("Yaw"), ctrl_vel); 
		ctrl_vel = Math.Max(0.01, ctrl_vel); 
		rot.Normalize(); 
		rot *= ctrl_vel;
		float pitch = (float)rot.GetDim(0);
		g.SetValueFloat("Pitch", pitch);

		float yaw = -(float)rot.GetDim(1);
		g.SetValueFloat("Yaw", yaw);

		float roll = -(float)rot.GetDim(2);
		g.SetValueFloat("Roll", roll); 
//		g.SetValueFloat("Power", 1.0f); 
		g.SetValueBool("Override", true);
		bAligned=false; 
	} 
	return bAligned; 
}

 
string gyrosetup()  
{
    string s = "";
	var l = new List < IMyTerminalBlock > ();
	rc = gpsCenter as IMyShipController;

	if (rc == null)  
	{ 
		if (l.Count < 1) return "No RC!"; 
		rc = (IMyRemoteControl) l[0]; 
	}
	gyrosOff(); // turn off any working gyros from previous runs
	// NOTE: Uses grid of controller, not ME, nor localgridfilter
	GridTerminalSystem.GetBlocksOfType < IMyGyro > (l, x => x.CubeGrid == gpsCenter.CubeGrid);
//    s += "ALLGYRO#=" + l.Count + "#";
	var l2 = new List < IMyTerminalBlock > ();
            int skipped = 0;
	for (int i = 0; i < l.Count; i++)
	{
 //       s += "\n" + l[i].CustomName;
		if (l[i].CustomName.Contains("!NAV") || l[i].CustomData.Contains("!NAV"))
		{
            skipped++;
			continue;
		}
//        s += " ADDED";
		l2.Add(l[i]);
	}
	gyros = l2.ConvertAll(x => (IMyGyro)x);
	if (LIMIT_GYROS>0)
                if( gyros.Count > LIMIT_GYROS) 
		gyros.RemoveRange(LIMIT_GYROS, gyros.Count - LIMIT_GYROS);
        else
                if((LEAVE_GYROS-skipped)>0)
                {
                    int index = gyros.Count-(LEAVE_GYROS-skipped);
                    gyros.RemoveRange(index, (LEAVE_GYROS - skipped));
                }
    
    gyrosOff(); // turn off all overrides

    s+= "GYRO#" + gyros.Count.ToString("00")+"#";
    return s;
} 
void gyrosOff() 
{
	if (gyros != null)
	{
		for (int i = 0; i < gyros.Count; ++i)
		{
			gyros[i].SetValueBool("Override", false);
            gyros[i].Enabled = true;
		}
	} 
}
#endregion



    }
}