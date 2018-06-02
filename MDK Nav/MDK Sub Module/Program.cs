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

        const string velocityFormat = "0.00";

        void ModuleDeserialize(INIHolder iNIHolder)
        {
            NavDeserialize(iNIHolder);
        }

        void ModuleSerialize(INIHolder iNIHolder)
        {
            NavSerialize(iNIHolder);
        }

        void moduleDoPreModes()
        {
        }

        void modulePostProcessing()
        {
            Echo(sInitResults);
            echoInstructions();
            Echo(craftOperation());
        }

        void ResetMotion(bool bNoDrills = false)  
        { 
	        powerDownThrusters(thrustAllList);
            gyrosOff();
            powerDownRotors(rotorNavLeftList);
            powerDownRotors(rotorNavRightList);
            WheelsPowerUp(0,75);

	        if (shipOrientationBlock is IMyRemoteControl) ((IMyRemoteControl)shipOrientationBlock).SetAutoPilotEnabled(false);
            if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;
//            if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).HandBrake = true;
        }

    }
}