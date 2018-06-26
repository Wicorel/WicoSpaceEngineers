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



        #region arguments

        bool moduleProcessArguments(string sArgument)
        {
//            sArgResults = "";
            // string output="";
            if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
            {
                Echo("Arg=" + sArgument);
                Echo("PassedArg=" + sPassedArgument);
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
            else if (args[0] == "namecameras")
            {
                nameCameras(cameraForwardList, "Front");
                nameCameras(cameraBackwardList, "Back");
                nameCameras(cameraDownList, "Down");
                nameCameras(cameraUpList, "Up");
                nameCameras(cameraLeftList, "Left");
                nameCameras(cameraRightList, "Right");
            }
            // increase horz
            // decrease horz
            // increase vert
            // decrease vert
            // increase forward
            // decrease forward
            // increase pitch
            // decrease pitch
            // increase yaw
            // decrease yaw
            // increase roll
            // decrease roll
            else if (args[0] == "+horz")
            {
                ProjectorsHorz();
            }
            else if (args[0] == "-horz")
            {
                ProjectorsHorz(false);
            }
            else if (args[0] == "+vert")
            {
                ProjectorsVert();
            }
            else if (args[0] == "-vert")
            {
                ProjectorsVert(false);
            }
            else if (args[0] == "+fw")
            {
                ProjectorsFw();
            }
            else if (args[0] == "-fw")
            {
                ProjectorsFw(false);
            }


            else if (args[0] == "+pitch")
            {
                ProjectorsPitch();
            }
            else if (args[0] == "-pitch")
            {
                ProjectorsPitch(false);
            }
            else if (args[0] == "+yaw")
            {
                ProjectorsYaw();
            }
            else if (args[0] == "-yaw")
            {
                ProjectorsYaw(false);
            }
            else if (args[0] == "+roll")
            {
                ProjectorsRoll();
            }
            else if (args[0] == "-roll")
            {
                ProjectorsRoll(false);
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
        #endregion
        bool moduleProcessAntennaMessage(string sArgument)
        {
            // we directly received an antenna message
            return false;
        }


    }
}