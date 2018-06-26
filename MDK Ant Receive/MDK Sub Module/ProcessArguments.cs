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



        bool moduleProcessArguments(string sArgument)
        {

            if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
            {
                //		Echo("Arg=" + sArgument);
                //		Echo("PassedArg=" + sPassedArgument);
                if (sPassedArgument != "" && sPassedArgument != "timer")
                {
                    Echo("Using Passed Arg=" + sPassedArgument);
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

            string[] args = sArgument.Trim().Split(' ');
            if (args[0] == "trigger")
            {
                Echo("trigger");
                if (args.Length > 1)
                {
                    string sCmd = "WICO:TRIGGER:";
                    sCmd += args[1].Trim();
                    antSend(sCmd);
                }
                else Echo("Incomplete command");

            }

            if (DockProcessMessage(sArgument))
            {
                Echo("Processed");
            }
            else
            {
                int iDMode;
                if (modeCommands.TryGetValue(args[0].ToLower(), out iDMode))
                {
//                    sArgResults = "mode set to " + iDMode;
                    setMode(iDMode);
                    // return true;
                }
                else
                {
//                    sArgResults = "Unknown argument:" + args[0];
                }
            }

            return false; // keep processing in main
        }
        bool moduleProcessAntennaMessage(string sArgument)
        {
            // process an antenna message locally.  If processed, return true
            if (DockProcessMessage(sArgument))
                return true;
            if (AsteroidProcessMessage(sArgument))
                return true;
            if (OreProcessMessage(sArgument))
                return true;
            if (PatrolProcessMessage(sArgument))
                return true;
            if (ProcessTriggerMessage(sArgument))
                return true;
            return false;
        }

        bool ProcessTriggerMessage(string sReceivedMessage)
        {
            sReceivedMessage = sReceivedMessage.Trim();
//sStartupError += "\n" + sReceivedMessage;
            string[] aMessage = sReceivedMessage.Trim().Split(':');
//sStartupError += "\n" + aMessage.Length + " Parts";
//            Echo(aMessage.Length + " Parts");
            if (aMessage.Length > 0)
            {
                if (aMessage[0] != "WICO")
                {
//sStartupError += "\nNot WICO";
                    Echo("not wico system message");
                    return false;
                }
                if (aMessage.Length > 1)
                {
//sStartupError += "\nType="+aMessage[1] +" Length="+aMessage.Length;
                    if (aMessage[1] == "TRIGGER" && aMessage.Length>2)
                    {
//                        sStartupError+="\nTriggering:"+aMessage[2];
                        bool bTriggered= doSubModuleTimerTriggers(aMessage[2]);
//sStartupError+="\nTriggered="+bTriggered.ToString();
                        return bTriggered;
                    }
                    else
                    {
//    sStartupError += "\nNot trigger or invalid";
                    }
                }
            }
            return false;
        }

        bool PatrolProcessMessage(string sReceivedMessage )
        {
//            sStartupError+="\nPatrolProcessMessage()";
            sReceivedMessage = sReceivedMessage.Trim();
//            sStartupError += "\n" + sReceivedMessage;
            string[] aMessage = sReceivedMessage.Trim().Split(':');
//            sStartupError += "\n" + aMessage.Length + " Parts";
            Echo(aMessage.Length + " Parts");   
            if (aMessage.Length > 1)
            {
                if (aMessage[0] != "WICO")
                {
//                    sStartupError += "\nNot WICO";
                    Echo("not wico system message");
                    return false;
                }
                if (aMessage.Length > 2)
                {
//                    sStartupError += "\nType="+aMessage[1];
                    if (aMessage[1] == "PATROL")
                    {
 //                       sStartupError+="\nPatrol request";
                        Echo("Patrol waypoint Request!");
                        if(!bAllowPatrol)
                        {
//                            sStartupError += "\nPatrol is turned off";
                            Echo("Patrol is turned off");
                            return false;
                        }
                        IMyRemoteControl rc;
                        rc = shipOrientationBlock as IMyRemoteControl;
                        if (rc == null)
                        {
//                            sStartupError += "\nNo Remote Control Block";
                            return false;
                        }

                        rc.ClearWaypoints();
//                        rc.FlightMode = FlightMode.Patrol; // goes back and forth in list
                        rc.FlightMode = FlightMode.Circle; // when at end of list, goes back to beginning.
                        //                       rc.SetAutoPilotEnabled(false);
                        rc.SetDockingMode(false);
                        rc.SetCollisionAvoidance(false);

                        //          0    1             2                   3
                        //antSend("WICO:PATROL:" + <int # waypoints> +":"+ <waypoint1>+":"+... <waypointn>

                        bool pOK = false;
                        int  NumberWaypoints = 0;
                        pOK = int.TryParse(aMessage[2], out NumberWaypoints);

                        int currOffset = 3;
                        for (int iWaypoint = 0; iWaypoint < NumberWaypoints; iWaypoint++)
                        {
//                            for (; currOffset < aMessage.Length;)
                            {
                                double x, y, z;
                                try
                                {
                                    bool xOk = double.TryParse(aMessage[currOffset++].Trim(), out x);
                                    bool yOk = double.TryParse(aMessage[currOffset++].Trim(), out y);
                                    bool zOk = double.TryParse(aMessage[currOffset++].Trim(), out z);
                                    if (!xOk || !yOk || !zOk)
                                    {
                                        //Echo("P:C");  
                                        Echo("Invalid Command:(" + aMessage + ")");
                                        //			shutdown(gyroList);
                                        continue;
                                    }
                                    rc.AddWaypoint(new Vector3D(x, y, z), "Patrol" + iWaypoint);
                                }
                                catch
                                {
                                    Echo("Invalid message");
                                    return false;
                                }
                            }
                        }
                        rc.SetAutoPilotEnabled(true);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}