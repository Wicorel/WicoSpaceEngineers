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
        string sVersion = "3.4C";

        void ModuleDeserialize(INIHolder iNIHolder)
        {
            ScansDeserialize(iNIHolder);
            AsteroidsDeserialize();
            OreDeserialize();

        }

        void ModuleSerialize(INIHolder iNIHolder)
        {
            ScansDeserialize(iNIHolder);
            AsteroidSerialize();
            OreSerialize();
        }

        void moduleDoPreModes()
        {
            AntennaCheckOldMessages();
        }

        void modulePostProcessing()
        {
            AntDisplayPendingMessages();
            Echo(asteroidsInfo.Count.ToString() + " Known Asteroids");
            Echo(oreLocs.Count.ToString() + " Known Ores");
            OreDumpLocs();
            Echo(sInitResults);
            echoInstructions();
        }

        void ResetMotion(bool bNoDrills = false)  
        { 
//            powerDownThrusters(thrustAllList);
//            gyrosOff();
//            powerDownRotors(rotorNavLeftList);
//            powerDownRotors(rotorNavRightList);
	        if (shipOrientationBlock is IMyRemoteControl) ((IMyRemoteControl)shipOrientationBlock).SetAutoPilotEnabled(false);
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;
//            if(!bNoDrills) turnDrillsOff();
        } 

    }
}