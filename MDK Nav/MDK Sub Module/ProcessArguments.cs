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

        string sArgResults = "";


        // mult-arg
        #region arguments
        bool moduleProcessArguments(string sArgument)
        {
            sArgResults = "";
            // string output="";
            if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
            {
                //		Echo("Arg=" + sArgument);
                //		Echo("PassedArg=" + sPassedArgument);
                if (sPassedArgument != "" && sPassedArgument != "timer")
                {
                    //			Echo("Using Passed Arg=" + sPassedArgument);
                    sArgument = sPassedArgument;
                }
                // SPECIAL to emulate old Nav
                if (NAVEmulateOld)
                {
                    var tList = GetBlocksContains<IMyTerminalBlock>("NAV:");
                    for (int i1 = 0; i1 < tList.Count(); i1++)
                    {
                        // don't want to get blocks that have "NAV:" in customdata..
                        if (tList[i1].CustomName.StartsWith("NAV:"))
                        {
                            Echo("Found NAV: command:");
                            sArgument = tList[i1].CustomName.Substring("NAV:".Length);
//                            sStartupError += "Found OLDNAV:" + sArgument;
                            break;
                        }
                    }
                }
            }

            if (sArgument == "init")
            {
                sInitResults = "";
                init = false;
                currentInit = 0;
                doInit(); // do first pass.
                return false;
            }

            string[] varArgs = sArgument.Trim().Split(';');

            bool bFoundNAVCommands = false;

            for (int iArg = 0; iArg < varArgs.Length; iArg++)
            {
                string[] args = varArgs[iArg].Trim().Split(' ');

                if (args[0] == "timer")
                {
                    // do nothing for sub-module
                }
                else if (args[0] == "wccs")
                {

                }
                else if (args[0] == "wcct")
                {

                }
                else if (args[0] == "W" || args[0] == "O")
                { // [W|O] <x>:<y>:<z>  || W <x>,<y>,<z>
                  // W GPS:Wicorel #1:53970.01:128270.31:-123354.92:
                  // O means orient towards.  W means orient, then move to
                    bFoundNAVCommands = true;
                    Echo("Args:");
                    for (int icoord = 0; icoord < args.Length; icoord++)
                        Echo(args[icoord]);
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    string sArg = args[1].Trim();

                    if(args.Length>2)
                    {
                        sArg = args[1];
                        for (int kk = 2; kk < args.Length; kk++)
                            sArg += " " + args[kk];
                        sArg = sArg.Trim();
                    }

//                    Echo("sArg=\n'" + sArg+"'");
                    string[] coordinates = sArg.Split(',');
                    if (coordinates.Length < 3)
                    {
                        coordinates = sArg.Split(':');
                    }
//                    Echo(coordinates.Length + " Coordinates");
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

//                    sStartupError = "CMD Initiated NAV:\n" + sArgument;

                    //                    vNavTarget = new Vector3D(x, y, z);
                    //                    bValidNavTarget = true;
                    if (args[0] == "W")
                    {
                        _NavAddTarget(new Vector3D(x, y, z), MODE_NAVNEXTTARGET, 0, arrivalDistanceMin, sWaypointName, shipSpeedMax);
//                        bGoOption = true;
                    }
                    else
                    {
                        _NavAddTarget(new Vector3D(x, y, z), MODE_NAVNEXTTARGET, 0, arrivalDistanceMin, sWaypointName, shipSpeedMax, false);
//                        bGoOption = false;
                    }
//                    sStartupError += "\nW " + sWaypointName + ":" + wicoNavCommands.Count.ToString();
                    //                   setMode(MODE_GOINGTARGET);

                }
                else if (args[0] == "S")
                { // S <mps>
                    // TODO: Queue the command into NavCommands
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    double x;
                    bool xOk = double.TryParse(args[1].Trim(), out x);
                    if (xOk)
                    {
                        shipSpeedMax = x;
//                        Echo("Set speed to:" + shipSpeedMax.ToString("0.00"));
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
                    // TODO: Queue the command into NavCommands
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
//                        Echo("Set arrival distance to:" + arrivalDistanceMin.ToString("0.00"));
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
                else if (args[0] == "L")
                { // L launch
                    bFoundNAVCommands = true;
                    _NavQueueLaunch();
                }
                else if (args[0] == "launch")
                { // L launch
                    bFoundNAVCommands = true;
                    _NavQueueLaunch();
                }
                else if (args[0] == "OL")
                { // OL Orbital launch
                    bFoundNAVCommands = true;
                    _NavQueueOrbitalLaunch();
                }
                else if (args[0] == "orbitallaunch")
                { // OL Orbital launch
                    bFoundNAVCommands = true;
                    _NavQueueOrbitalLaunch();
                }
                else if (args[0] == "dock")
                { // dock
                    bFoundNAVCommands = true;
                    _NavQueueOrbitalLaunch();
                }
                // todo: add launch, dock, land, etc
                else
                {
                    int iDMode;
                    if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                    {
                        sArgResults = "mode set to " + iDMode;
                        setMode(iDMode);
                        // return true;
                    }
                    else
                    {
                        sArgResults = "Unknown argument:" + args[0];
                    }
                }
            }
            if(bFoundNAVCommands)
            {
//                sStartupError += "\nFound NAV Commands:" + wicoNavCommands.Count.ToString();
                _NavStart();
            }
            return false; // keep processing in main
        }
        #endregion
        bool moduleProcessAntennaMessage(string sArgument)
        {
            return false;
        }

        bool moduleProcessIGCMessage(string sArgument)
        {
            return false;
        }

    }
}