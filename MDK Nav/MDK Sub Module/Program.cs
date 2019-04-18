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
            // check for IGC Listeners
            do
            {
                if(_StartNavListener.HasPendingMessage)
                {
                    var msg = _StartNavListener.AcceptMessage();
                    var src = msg.Source;
                    Vector3D vTarget;
                    int modeArrival;
                    int stateArrival;
                    double DistanceMin;
                    string TargetName;
                    double maxSpeed;
                    bool bGo;
                    NAVDeserializeCommand(msg.Data.ToString(), out vTarget, out modeArrival, out stateArrival, out DistanceMin, out TargetName, out maxSpeed, out bGo);
                    _NavGoTarget(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);

                }
            } while (_StartNavListener.HasPendingMessage); // Process all pending messages
            do
            {
                if (_AddNavListener.HasPendingMessage)
                {
                    var msg = _StartNavListener.AcceptMessage();
                    // information about the received message
                    Vector3D vTarget;
                    int modeArrival;
                    int stateArrival;
                    double DistanceMin;
                    string TargetName;
                    double maxSpeed;
                    bool bGo;
                    NAVDeserializeCommand(msg.Data.ToString(), out vTarget, out modeArrival, out stateArrival, out DistanceMin, out TargetName, out maxSpeed, out bGo);
                    _NavAddTarget(vTarget, modeArrival, stateArrival, DistanceMin, TargetName, maxSpeed, bGo);
                }
            } while (_AddNavListener.HasPendingMessage); // Process all pending messages
            do
            {
                if (_ResetNavListener.HasPendingMessage)
                {
                    var msg = _ResetNavListener.AcceptMessage();
                    // information about the received message
                    Echo("ResetNav Received Message");
//                  _NavReset();
                }
            } while (_ResetNavListener.HasPendingMessage); // Process all pending messages
            do
            {
                if (_LaunchNavListener.HasPendingMessage)
                {
                    var msg = _ResetNavListener.AcceptMessage();
                    // information about the received message
                    Echo("_NavQueueLaunch Received Message");
                    _NavQueueLaunch();
                }
            } while (_LaunchNavListener.HasPendingMessage); // Process all pending messages
            do
            {
                if (_OrbitalNavListener.HasPendingMessage)
                {
                    var msg = _ResetNavListener.AcceptMessage();
                    // information about the received message
                    Echo("_NavQueueOrbitalLaunch Received Message");
                    _NavQueueOrbitalLaunch();
                }
            } while (_OrbitalNavListener.HasPendingMessage); // Process all pending messages
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