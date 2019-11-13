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
        string sVersion = "3.71";

        const string velocityFormat = "0.00";

        void moduleDoPreModes()
        {
//            string output = "";
//            Echo(output);
        }

        void modulePostProcessing()
        {
//            Echo(sInitResults);
//            Echo(craftOperation());
            echoInstructions();
        }

        void ResetMotion(bool bNoDrills = false)  
        { 
        //	if (navEnable != null)	blockApplyAction(navEnable,"OnOff_Off"); //navEnable.ApplyAction("OnOff_Off"); 
	        powerDownThrusters(thrustAllList);
            gyrosOff();
	        if (shipOrientationBlock is IMyRemoteControl) ((IMyRemoteControl)shipOrientationBlock).SetAutoPilotEnabled(false);
	        if (shipOrientationBlock is IMyShipController) ((IMyShipController)shipOrientationBlock).DampenersOverride = true;
        } 

        void ModuleSerialize(INIHolder iNIHolder)
        {
            DockingSerialize(iNIHolder);
            DockedSerialize(iNIHolder);
            LaunchSerialize(iNIHolder);
            NavSerialize(iNIHolder);
        }

        void ModuleDeserialize(INIHolder iNIHolder)
        {
            DockingDeserialize(iNIHolder);
            DockedDeserialize(iNIHolder);
            LaunchDeserialize(iNIHolder);
            NavDeserialize(iNIHolder);
        }

    }
}