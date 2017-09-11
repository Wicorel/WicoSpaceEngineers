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
#region camerasensors 

string sCameraViewOnly = "[VIEW]"; // do not use cameras with this in their name for scanning.

readonly Matrix cameraidentityMatrix = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

List<IMyTerminalBlock> cameraForwardList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> cameraBackwardList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> cameraDownList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> cameraUpList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> cameraLeftList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> cameraRightList = new List<IMyTerminalBlock>();
 
List<IMyTerminalBlock> cameraAllList = new List<IMyTerminalBlock>();

IMyTerminalBlock lastCamera = null;

private MyDetectedEntityInfo lastDetectedInfo;


bool doCameraScan(List<IMyTerminalBlock> cameraList, double scandistance=100, float pitch=0, float yaw=0)
{
	double foundmax = 0;
	lastCamera = null;
	for (int i = 0; i < cameraList.Count; i++)
	{
		double thismax = ((IMyCameraBlock)cameraList[i]).AvailableScanRange;
//		Echo(cameraList[i].CustomName + ":maxRange:" + thismax.ToString("N0"));
		// find camera with highest scan range.
		if (thismax > foundmax)
		{
			foundmax = thismax;
			lastCamera = cameraList[i];
		}
	}

	IMyCameraBlock camera = lastCamera as IMyCameraBlock;
	if (lastCamera == null)
	{
		return false;
	}

	if (camera.CanScan(scandistance))
	{
//		Echo("simple Scan with Camera:" + camera.CustomName);

		lastDetectedInfo = camera.Raycast(scandistance, pitch, yaw);
		lastCamera = camera;

		if(!lastDetectedInfo.IsEmpty())
			addDetectedEntity(lastDetectedInfo);

		return true;
	}
	else
	{
		Echo(camera.CustomName + ":" + camera.AvailableScanRange.ToString("N0"));
	}

	return false;

}

bool doCameraScan(List<IMyTerminalBlock> cameraList, Vector3D targetPos)
{
	Echo("target Scan");
	double foundmax = 0;
	lastCamera = null;
	for (int i = 0; i < cameraList.Count; i++)
	{
		double thismax = ((IMyCameraBlock)cameraList[i]).AvailableScanRange;
//		Echo(cameraList[i].CustomName + ":maxRange:" + thismax.ToString("N0"));
		// find camera with highest scan range.
		if (thismax > foundmax)
		{
			foundmax = thismax;
			lastCamera = cameraList[i];
		}
	}

	IMyCameraBlock camera = lastCamera as IMyCameraBlock;
	if (lastCamera == null)
		return false;

//	if (camera.CanScan(scandistance))
	{
		Echo("Scanning with Camera:" + camera.CustomName);
		lastDetectedInfo = camera.Raycast(targetPos);
		lastCamera = camera;

		if(!lastDetectedInfo.IsEmpty())
			addDetectedEntity(lastDetectedInfo);

		return true;
	}
	/*
	else
	{
		Echo(camera.CustomName + ":" + camera.AvailableScanRange.ToString("N0"));
	}
	return false;
		*/
}

double findMaxCameraRange(List<IMyTerminalBlock> cameraList)
{
	double maxCameraRangeAvailable = 0;
	for (int i = 0; i < cameraList.Count; i++)
	{
		IMyCameraBlock camera = cameraList[i] as IMyCameraBlock;
		if (maxCameraRangeAvailable < camera.AvailableScanRange)
			maxCameraRangeAvailable = camera.AvailableScanRange;

	}
	return maxCameraRangeAvailable;
}
string camerasensorsInit(IMyTerminalBlock orientationBlock)  
{
	cameraForwardList.Clear();

	cameraBackwardList.Clear();
	cameraDownList.Clear();
	cameraUpList.Clear();
	cameraLeftList.Clear();
	cameraRightList.Clear();
	cameraAllList.Clear();

	GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cameraAllList, (x1 => x1.CubeGrid == Me.CubeGrid));
	Matrix fromGridToReference;
	orientationBlock.Orientation.GetMatrix(out fromGridToReference);
	Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

	for (int i = 0; i < cameraAllList.Count; ++i)
	{
		if (cameraAllList[i].CustomName.Contains(sCameraViewOnly) )
			continue; // don't add it to our list.

		IMyCameraBlock camera = cameraAllList[i] as IMyCameraBlock;

		camera.EnableRaycast = true;

		Matrix fromcameraToGrid;
		camera.Orientation.GetMatrix(out fromcameraToGrid);
		Vector3 accelerationDirection = Vector3.Transform(fromcameraToGrid.Forward, fromGridToReference);
		if (accelerationDirection == cameraidentityMatrix.Left)
		{
			cameraLeftList.Add(cameraAllList[i]);
		}
		else if (accelerationDirection == cameraidentityMatrix.Right)
		{
			cameraRightList.Add(cameraAllList[i]);
		}
		else if (accelerationDirection == cameraidentityMatrix.Backward)
		{
			cameraBackwardList.Add(cameraAllList[i]);
		}
		else if (accelerationDirection == cameraidentityMatrix.Forward)
		{
			cameraForwardList.Add(cameraAllList[i]);
		}
		else if (accelerationDirection == cameraidentityMatrix.Up)
		{
			cameraUpList.Add(cameraAllList[i]);
		}
		else if (accelerationDirection == cameraidentityMatrix.Down)
		{
			cameraDownList.Add(cameraAllList[i]);
		}
	}
	string s;
	s = "CS:<";
	s += "F" + cameraForwardList.Count.ToString("00");
	s += "B" + cameraBackwardList.Count.ToString("00");
	s += "D" + cameraDownList.Count.ToString("00");
	s += "U" + cameraUpList.Count.ToString("00");
	s += "L" + cameraLeftList.Count.ToString("00");
	s += "R" + cameraRightList.Count.ToString("00");
	s += ">";
	return s;

} 

void nameCameras(List<IMyTerminalBlock> cameraList, string sDirection)
{
	for(int i=0;i<cameraList.Count; i++)
	{
		cameraList[i].CustomName="Camera " + (i+1).ToString() + " " + sDirection;
	}
}
  
#endregion



    }
}