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
        string moduleName = "AntReceive";
        string sVersion = "3.2B";

        const string sGPSCenter = "Craft Remote Control";

        IMyTerminalBlock gpsCenter = null;

        class OurException : Exception
        {
            public OurException(string msg) : base("WicoAntReceiveModule" + ": " + msg) { }
        }

        
        void moduleDoPreModes()
        {
            AntennaCheckOldMessages();
        }


        void modulePostProcessing()
        {
            AntDisplayPendingMessages();

            Echo(sInitResults);
            echoInstructions();
        }

        void ResetMotion(bool bNoDrills = false)  
        { 
//            powerDownThrusters(thrustAllList);
//            gyrosOff();
//            powerDownRotors(rotorNavLeftList);
//            powerDownRotors(rotorNavRightList);
	        if (gpsCenter is IMyRemoteControl) ((IMyRemoteControl)gpsCenter).SetAutoPilotEnabled(false);
	        if (gpsCenter is IMyShipController) ((IMyShipController)gpsCenter).DampenersOverride = true;
//            if(!bNoDrills) turnDrillsOff();
        } 

    }
}