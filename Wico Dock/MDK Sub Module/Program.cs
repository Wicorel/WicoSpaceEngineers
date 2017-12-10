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


        string OurName = "Wico Craft";
        string moduleName = "Dock";
        string sVersion = "3.2D";

        const string sGPSCenter = "Craft Remote Control";

        Vector3I iForward = new Vector3I(0, 0, 0);
        Vector3I iUp = new Vector3I(0, 0, 0);
        Vector3I iLeft = new Vector3I(0, 0, 0);
 //       Vector3D currentPosition;
        const string velocityFormat = "0.00";

//        double shipWidth = 0, shipHeight = 0, shipLength = 0;

 //       IMyTerminalBlock anchorPosition;
        IMyTerminalBlock gpsCenter = null;
//        Vector3D vCurrentPos;
        //IMyTerminalBlock gpsCenter = null;
        class OurException : Exception
        {
            public OurException(string msg) : base("WicoExampleModule" + ": " + msg) { }
        }


        void moduleDoPreModes()
        {
            string output = "";
            if (AnyConnectorIsConnected()) output += "Connected";
            else
            {
                output += "Not Connected";
                if (AnyConnectorIsLocked())
                    output += " : Locked";
                else
                    output += " : Not Locked";
            }
            Echo(output);
        }

        void modulePostProcessing()
        {
            Echo(sInitResults);
            echoInstructions();
        }

        void ResetMotion(bool bNoDrills = false)  
        { 
        //	if (navEnable != null)	blockApplyAction(navEnable,"OnOff_Off"); //navEnable.ApplyAction("OnOff_Off"); 
	        powerDownThrusters(thrustAllList);
            gyrosOff();
	        if (gpsCenter is IMyRemoteControl) ((IMyRemoteControl)gpsCenter).SetAutoPilotEnabled(false);
	        if (gpsCenter is IMyShipController) ((IMyShipController)gpsCenter).DampenersOverride = true;
        } 

    }
}