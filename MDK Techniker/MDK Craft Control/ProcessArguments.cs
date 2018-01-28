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



        // multi-arg
        #region arguments

        bool processArguments(string sArgument)
        {
            string[] varArgs = sArgument.Trim().Split(';');

            for (int iArg = 0; iArg < varArgs.Length; iArg++)
            {
                string[] args = varArgs[iArg].Trim().Split(' ');

                if (args[0] == "timer")
                {
                    processTimerCommand();

                }
                else if (args[0] == "idle")
                    ResetToIdle();
                else if (args[0] == "masterreset")
                    MasterReset();
                else if (args[0].ToLower() == "coast")
                { // from techniker
                    if (countThrusters(thrustForwardList, thrusthydro) > 0)
                    { // we have hydro thrusters to check
                        if (areThrustersOn(thrustForwardList, thrusthydro))
                        {
                            Echo("FTO");
                            if (countThrusters(thrustBackwardList, thrustion) > 0)
                            {
                                if (areThrustersOn(thrustBackwardList, thrustion))
                                {
                                    // hydro back is on and ion front is on.
                                    // turn off hydro and ion back
                                    powerDownThrusters(thrustBackwardList, thrustAll, true);
                                }
                                else
                                { // front ion are OFF. and back hydro are on. turn ON all backward(front) thrusters
                                    powerDownThrusters(thrustBackwardList, thrustAll, false);
                                }

                            }
                            else // no front ion thrusters to check
                            {
                                if (areThrustersOn(thrustBackwardList)) //check any thrusters.
                                { // turn them off
                                    powerDownThrusters(thrustBackwardList, thrustAll, true);
                                }
                                else
                                { // turn them ON
                                    powerDownThrusters(thrustBackwardList, thrustAll, false);
                                }

                            }

                        }
                        else
                        { // back hydro are NOT on.  don't touch front hydro
                            Echo("BNO");
                            if (areThrustersOn(thrustBackwardList, thrustion))
                            {
                                // hydro back is off and ion front is on.
                                // turn off ion backward (front) only
                                powerDownThrusters(thrustBackwardList, thrustion, true);
                            }
                            else
                            { // front ion are OFF. and back hydro are Off. turn ON ion-only backward(front) thrusters
                                powerDownThrusters(thrustBackwardList, thrustion, false);
                            }
                        }
                    }
                    else
                    {
                        // just toggle

                        if (thrustBackwardList.Count > 1)
                        {
                            foreach (var t1 in thrustBackwardList)
                                if(t1 is IMyFunctionalBlock)
                                {
                                    var f1 = t1 as IMyFunctionalBlock;
                                    f1.Enabled = !f1.Enabled;
                                }
                            //                            blockApplyAction(thrustBackwardList, "OnOff");
                        }

                    }
                }
                /*
                 *else if (args[0].ToLower() == "coast")
                                {
                                    //	Echo("Coast: backward =" + thrustBackwardList.Count.ToString());
                                    if (thrustBackwardList.Count > 1)
                                    {
                                        blockApplyAction(thrustBackwardList, "OnOff");
                                        //				blockApplyAction(thrustBackwardList, "OnOff_Off");
                                    }
                                }
                                */
                else if (args[0] == "setvaluef")
                {
                    Echo("SetValueFloat");
                    //Miner Advanced Rotor:UpperLimit:-24
                    string sArg = "";
                    for (int i = 1; i < args.Length; i++)
                    {
                        sArg += args[i];
                        if (i < args.Length - 1)
                        {
                            sArg += " ";
                        }
                    }
                    string[] cargs = sArg.Trim().Split(':');

                    if (cargs.Length < 3)
                    {
                        Echo("Invalid Args");
                        continue;
                    }
                    IMyTerminalBlock block;
                    block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(cargs[0]);
                    if (block == null)
                    {
                        Echo("Block not found:" + cargs[0]);
                        continue;
                    }
                    float fValue = 0;
                    bool fOK = float.TryParse(cargs[2].Trim(), out fValue);
                    if (!fOK)
                    {
                        Echo("invalid float value:" + cargs[2]);
                        continue;
                    }
                    Echo("SetValueFloat:" + cargs[0] + " " + cargs[1] + " to:" + fValue.ToString());
                    block.SetValueFloat(cargs[1], fValue);
                }
                else if (args[0] == "brake")
                {
                    Echo("brake");
                    //toggle brake
                    if (shipOrientationBlock is IMyShipController)
                    {
                        IMyShipController msc = shipOrientationBlock as IMyShipController;
                        bool bBrake = msc.HandBrake;
                        msc.ApplyAction("HandBrake");
                    }
                    else Echo("No Ship Controller found");

                }
		        else if (args[0] == "namecameras")
		        {
			        nameCameras(cameraForwardList, "FRONT");
			        nameCameras(cameraBackwardList, "BACK");
			        nameCameras(cameraDownList, "DOWN");
			        nameCameras(cameraUpList, "UP");
			        nameCameras(cameraLeftList, "LEFT");
			        nameCameras(cameraRightList, "RIGHT");

		        }
		        else if (args[0] == "togglerange")
		        {
			        bLongRange = !bLongRange;
			        if (bLongRange)
				        maxScan = longRangeMax;
			        else
				        maxScan = shortRangeMax;
			        if (currentScan > maxScan)
				        currentScan = maxScan;
		        }
                /*
                        else if (args[0] == "setsimspeed")
                        {
                            if (args.Length < 2)
                            {
                                Echo("setsimspeed:nvalid arg");
                                continue;
                            }
                            float fValue = 0;
                            bool fOK = float.TryParse(args[1].Trim(), out fValue);
                            if (!fOK)
                            {
                                Echo("invalid float value:" + args[1]);
                                continue;
                            }
                            fAssumeSimSpeed = fValue;
                            bCalcAssumed = true;

                        }
                */
                else if (args[0] == "wcct" || args[0]=="")
                {
                    // do nothing special
                }
                else if (args[0] == "W" || args[0] == "O")
                { // [W|O] <x>:<y>:<z>  || W <x>,<y>,<z>
                  // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                  // O means orient towards.  W means orient, then move to
                    Echo("Args:");
                    for (int icoord = 0; icoord < args.Length; icoord++)
                        Echo(args[icoord]);
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    string sArg = args[1].Trim();

                    if (args.Length > 2)
                    {
                        sArg = args[1];
                        for (int kk = 2; kk < args.Length; kk++)
                            sArg += " " + args[kk];
                        sArg = sArg.Trim();
                    }

                    Echo("sArg=\n'" + sArg + "'");
                    string[] coordinates = sArg.Split(',');
                    if (coordinates.Length < 3)
                    {
                        coordinates = sArg.Split(':');
                    }
                    Echo(coordinates.Length + " Coordinates");
                    for (int icoord = 0; icoord < coordinates.Length; icoord++)
                        Echo(coordinates[icoord]);
                    //Echo("coordiantes.Length="+coordinates.Length);  
                    if (coordinates.Length < 3)
                    {
                        //Echo("P:B");  

                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        gyrosOff();// shutdown(gyroList);
                        return false;
                    }
                    int iCoordinate = 0;
                    string sWaypointName = "Waypoint";
                    //  -  0   1           2        3          4       5
                    // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                    if (coordinates[0] == "GPS")
                    {
                        if (coordinates.Length > 4)
                        {
                            sWaypointName = coordinates[1];
                            iCoordinate = 2;
                        }
                        else
                        {
                            Echo("Invalid Command");
                            gyrosOff();
                            return false;
                        }
                    }

                    double x, y, z;
                    bool xOk = double.TryParse(coordinates[iCoordinate++].Trim(), out x);
                    bool yOk = double.TryParse(coordinates[iCoordinate++].Trim(), out y);
                    bool zOk = double.TryParse(coordinates[iCoordinate++].Trim(), out z);
                    if (!xOk || !yOk || !zOk)
                    {
                        //Echo("P:C");  
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        //			shutdown(gyroList);
                        continue;
                    }
                    vNavTarget = new Vector3D(x, y, z);
                    bValidNavTarget = true;
                    if (args[0] == "W")
                        bGoOption = true;
                    else bGoOption = false;

                    setMode(MODE_GOINGTARGET);

                }
                else if (args[0] == "S")
                { // S <mps>
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    double x1;
                    bool xOk = double.TryParse(args[1].Trim(), out x1);
                    if (xOk)
                    {
                        shipSpeedMax = (float)x1;
                        Echo("Set speed to:" + shipSpeedMax.ToString("0.00"));
                        //             setMode(MODE_ARRIVEDTARGET);
                    }
                    else
                    {
                        //Echo("P:C");  
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                }
                else if (args[0] == "D")
                { // D <meters>
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    double x;
                    bool xOk = double.TryParse(args[1].Trim(), out x);
                    if (xOk)
                    {
                        arrivalDistanceMin = x;
                        Echo("Set arrival distance to:" + arrivalDistanceMin.ToString("0.00"));
                    }

                    else
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                }
                else if (args[0] == "C")
                { // C <anything>
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    else
                    {
                        Echo(varArgs[iArg]);
                    }
                }
                else
                {
                    int iDMode;
                    if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                    {
                        setMode(iDMode);
                    }
                    else Echo("Unrecognized Command:" + varArgs[iArg]);
                }
            }
            return false; // keep processing in main
        }
        #endregion


    }
}