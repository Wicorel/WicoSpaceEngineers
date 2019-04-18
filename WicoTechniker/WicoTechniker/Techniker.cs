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
        void doTechnikerCalcsandDisplay()
        {
            double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);

            StringBuilder sb = new StringBuilder();
            double targetRange = 0; // no target

            //    StatusLog("clear", textPanelReport);
            StatusLog(OurName + " Control", textPanelReport);

            string output = "";
            output += "Velocity: " + velocityShip.ToString("N0") + "m/s";

            Echo(output);
            StatusLog(output, textPanelReport);

            if (bCreative) Echo("Creative!");
 //           Echo("Cargomult=" + cargoMult);

            sb.Clear();
            sb.AppendLine();


            if (bLongRange)
                sb.Append("Long Range Scan Active");
            else
                sb.Append("Normal Range Scan Active");
            sb.AppendLine();

            string s = "";

            if (lastDetectedInfo.IsEmpty())
            {
                sb.Append("No Target Found");
                sb.AppendLine();

                sb.Append("Next scanner Range: " + currentScan.ToString("N0") + " m");
                sb.AppendLine();

                StatusLog("clear", textRangeReport);
                StatusLog("No Target found", textRangeReport);

            }
            else
            {
                Echo("EntityID: " + lastDetectedInfo.EntityId);
                Echo("Name: " + lastDetectedInfo.Name);
                sb.Append("Name: " + lastDetectedInfo.Name);
                //sb.AppendLine();
                sb.Append(" - ");
                sb.Append("Type: " + lastDetectedInfo.Type);
                sb.AppendLine();
                sb.Append("Relationship: " + lastDetectedInfo.Relationship);
                if (lastDetectedInfo.HitPosition.HasValue && lastCamera != null)
                {
                    sb.AppendLine();
                    double distance = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.HitPosition.Value);
                    if (lastDetectedInfo.Name == "Asteroid")
                    {
                        // calculate range to outter edge of boudning box of asteroid.
                        targetRange = Vector3D.Distance(lastCamera.GetPosition(), lastDetectedInfo.Position);
                        targetRange -= lastDetectedInfo.BoundingBox.Size.X / 2; // half of bounding box.
                        Echo("adjTargetRange=" + targetRange.ToString("0.0"));
                    }
                    else
                    {
                        targetRange = distance;
                    }

                    if (distance > 1000)
                        s += (distance / 1000).ToString("RANGE:   0000.0km");
                    else
                        s += (distance).ToString("RANGE:     00000m ");


                    StatusLog("clear", textRangeReport);
                    StatusLog(s, textRangeReport);

                    sb.Append("Distance: " + niceDoubleMeters(distance));
                    sb.AppendLine();

                    sb.Append("Safe Range: " + niceDoubleMeters(targetRange));
                    sb.AppendLine();

                }
                //		sb.AppendLine();
                //		sb.Append("TimeStamp: " + lastDetectedInfo.TimeStamp.ToString());

            }
            s = "";
            if (stoppingDistance > 1000)
                s += (stoppingDistance / 1000).ToString("STOP DIST: 000.0km");
            else
                s += (stoppingDistance).ToString("STOP DIST: 00000m ");
            StatusLog(s, textRangeReport);

            double maxRange = findMaxCameraRange(cameraForwardList);
            if (maxRange < currentScan)
            {
                sb.AppendLine();
                sb.Append("Awaiting Available Range");
                sb.AppendLine();

                sb.Append(progressBar(maxRange / currentScan * 100));
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.Append(detectedEntities.Count.ToString() + " detected items");


            if (dGravity > 0)
            {
                StatusLog("GRAVITY WELL", textPanelReport);
            }

            if (AnyConnectorIsConnected())
            {
                StatusLog("Docked!", textPanelReport);
            }
            else
            {
                StatusLog(doEStopCheck(targetRange, stoppingDistance), textPanelReport);

            }

            // rotor status code for Techniker
            output = "";
            IMyMotorStator drillrotor;
            drillrotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Advanced Rotor L Drills");

            if (drillrotor != null)
            {
                output += "\nDrill Arm -";
                double angle = MathHelper.ToDegrees(drillrotor.Angle);
                if (angle > 178) output += " Stowed";
                else if (angle < 1) output += " Deployed";
                else output += " Moving";
                /*
                if (drillrotor.SafetyLock)
                    output += " - (Locked)";
                else
                    output += " - Unlocked";
                    */
            }

            IMyMotorStator toolrotor;
            toolrotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Advanced Rotor R Tools");
            if (toolrotor != null)
            {
                output += "\nTool Arm -";
                double angle = MathHelper.ToDegrees(toolrotor.Angle);
                if (angle > 178) output += " Deployed";
                else if (angle < 1) output += " Stowed";
                else output += " Moving";
                /*
                if (toolrotor.SafetyLock)
                    output += " - (Locked)";
                else
                    output += " - Unlocked";
                    */


            }
            //	output = "Drill rotor=" + angle.ToString("0.0");

            if (output != "")
            {
                Echo(output);
                StatusLog(output, textPanelReport);
            }

            // output main status.
            StatusLog(sb.ToString(), textPanelReport);
            //	Echo(sb.ToString());

            sb.Clear();
        }

        void doOutputGPSFromEntities()
        {
	        // detected entity dump to GPS Panel
	        if (gpsPanel != null)
	        {
		        StatusLog("clear", gpsPanel);
		        string s = "Open 'Edit Public Text' to get GPS Points";
		        StatusLog(s, gpsPanel);

		        foreach (KeyValuePair<long, MyDetectedEntityInfo> entry in detectedEntities)
		        {
			        string sName = entry.Value.Name;
			        if (sName == "Planet" || sName == "Asteroid")
			        {
				        sName += " (" + niceDoubleMeters(entry.Value.BoundingBox.Size.X) + ")";
				        //				sName += " (" + (entry.Value.BoundingBox.Size.X/1000).ToString("0.0")+"km)";
			        }
			        //info.BoundingBox.Size.ToString("0.000")
			        s = "GPS:" +sName  + ":" + Vector3DToString(entry.Value.Position) + ":";
			        StatusLog(s, gpsPanel);
		        }
	        }

        }

        string doEStopCheck(double targetRange, double stoppingDistance)
        {
	        string s = "";
        //	double targetRange = 0;

	        IMyShipController imsc=shipOrientationBlock as IMyShipController;

	        // ESTOP code
	        if (targetRange > 0)
	        {

		        // something has been detected
		        if ((stoppingDistance * 1.25 + velocityShip * 2) > targetRange && velocityShip > 3)
		        {
			        // EMERGENCY STOP
			        powerDownThrusters(thrustAllList, thrustAll, false);  // turns on all thrusters (non-override)

                    if (imsc != null && !imsc.DampenersOverride)
                    {
                        imsc.DampenersOverride = true;
                    }
			        s = "EMERGENCY STOP";
		        }
		        else if (stoppingDistance * 6 > targetRange || targetRange < 100)
		        {
			        s = "PROXIMITY WARNING";
			        if (velocityShip > 0.1 && imsc != null && imsc.DampenersOverride && areThrustersOn(thrustBackwardList, thrustion))
				        s += "\nStopping";
		        }
	        }
	        else if (targetRange < 0)
	        {
		        // we are INSIDE the bounding box
		        s = "Inside: Manual Control";

	        }
	        else s = "No Target";

	        return s;
        }



    }
}