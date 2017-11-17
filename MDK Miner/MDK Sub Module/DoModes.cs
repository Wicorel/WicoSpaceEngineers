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
        #region domodes 
        void doModes()
        {
            Echo("mode=" + iMode.ToString());
            doModeAlways();
/*            
            if (AnyConnectorIsConnected() && !((craft_operation & CRAFT_MODE_ORBITAL) > 0))
            {
                Echo("DM:docked");
                setMode(MODE_DOCKED);
            }
*/

            if (iMode == MODE_FINDORE) doModeFindOre();
            if (iMode == MODE_GOTOORE) doModeGotoOre();
            if (iMode == MODE_MININGORE) doModeMiningOre();
            if (iMode == MODE_EXITINGASTEROID) doModeExitingAsteroid();

            if (iMode == MODE_SEARCHORIENT) doModeSearchOrient();
            if (iMode == MODE_SEARCHSHIFT) doModeSearchShift();
            if (iMode == MODE_SEARCHVERIFY) doModeSearchVerify();
            if (iMode == MODE_SEARCHCORE) doModeSearchCore();

/*
            if (iMode == MODE_IDLE) doModeIdle();
            else if (iMode == MODE_DESCENT) doModeDescent();
*/
        }
        #endregion


        #region modeidle 
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            setMode(MODE_IDLE);
//            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
              StatusLog(moduleName + " Manual Control", textPanelReport);
        }
        #endregion

        void doModeAlways()
        {
	        processPendingSends();
	        processReceives();
        }
         void moduleDoPreModes()
        {
        }

        void modulePostProcessing()
        {
            if (init)
            {
            // only need to do these like once per second. or if something major changes.
                doCargoOreCheck();
                dumpFoundOre();
                //dumpOreLocs();



                double maxThrust = calculateMaxThrust(thrustForwardList);
                Echo("maxThrust=" + maxThrust.ToString("N0"));

                MyShipMass myMass;
                myMass = ((IMyShipController)gpsCenter).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass;
                Echo("effectiveMass=" + effectiveMass.ToString("N0"));

                double maxDeltaV = (maxThrust) / effectiveMass;
                Echo("maxDeltaV=" + maxDeltaV.ToString("0.00"));

                Echo("Cargo=" + cargopcent.ToString() + "%");
            }

            Echo(sInitResults);
            echoInstructions();
        }
        void processReceives()
        {

            if (sReceivedMessage != "")
            {
                Echo("Received Message=\n" + sReceivedMessage);
                string[] aMessage = sReceivedMessage.Trim().Split(':');

                if (aMessage.Length > 1)
                {
                    if (aMessage[0] != "WICO")
                    {
                        Echo("not wico system message");
                        return;
                    }
                    if (aMessage.Length > 2)
                    {
/*
                        if (aMessage[1] == "MOM")
                        {
                        }
                        */
                    }
                }
            }
/*
            if (lMomID == 0)
            {
                Echo("Orphan!!!");
                if (!bMomRequestSent)
                {
                    antSend("WICO:HELLO:" + Me.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(gpsCenter.GetPosition()));
                    bMomRequestSent = true;
                }
            }
            else
                Echo("Mom=" + sMomName);
                */
        }

    }
}