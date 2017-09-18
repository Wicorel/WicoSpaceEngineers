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



        // mult-arg
        #region arguments
        bool processArguments(string sArgument)
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
                /*
                else if (args[0].ToLower() == "sled")
                {
                    if (args.Length > 1)
                    {
                        if (args[1].ToLower() == "stop")
                        {
                            sledStop();
                        }
                        else if (args[1].ToLower() == "start")
                        {
                            sledStart();
                        }

                    }

                }
                */
                else if (args[0] == "W" || args[0] == "O")
                { // [W|O] <x>:<y>:<z>  || W <x>,<y>,<z>
                  // O means orient towards.  W means orient, then move to
                    Echo("Args:");
                    for (int icoord = 0; icoord < args.Length; icoord++)
                        Echo(args[icoord]);
                    if (args.Length < 1)
                    {
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        continue;
                    }
                    string[] coordinates = args[1].Trim().Split(',');
                    if (coordinates.Length < 3)
                    {
                        coordinates = args[1].Trim().Split(':');
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
                    double x, y, z;
                    bool xOk = double.TryParse(coordinates[0].Trim(), out x);
                    bool yOk = double.TryParse(coordinates[1].Trim(), out y);
                    bool zOk = double.TryParse(coordinates[2].Trim(), out z);
                    if (!xOk || !yOk || !zOk)
                    {
                        //Echo("P:C");  
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        //			shutdown(gyroList);
                        continue;
                    }
                    vHome = new Vector3D(x, y, z);
                    bValidHome = true;
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
                    double x;
                    bool xOk = double.TryParse(args[1].Trim(), out x);
                    if (xOk)
                    {
                        speedMax = x;
                        Echo("Set speed to:" + speedMax.ToString("0.00"));
                        //             setMode(MODE_ARRIVEDTARGET);
                    }
                    else
                    {
                        //Echo("P:C");  
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        //			shutdown(gyroList);
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
                        //            setMode(MODE_ARRIVEDTARGET);
                    }

                    else
                    {
                        //Echo("P:C");  
                        Echo("Invalid Command:(" + varArgs[iArg] + ")");
                        //			shutdown(gyroList);
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
                        //             setMode(MODE_ARRIVEDTARGET);
                    }
                }
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
            return false; // keep processing in main
        }
        #endregion


    }
}