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
            if (bValidLaunch1)
            {
                Echo("Target is connector");
            }
            else if (bValidHome)
            {
                Echo("Target is hover");
            }
            else
                Echo("Target is blind landing");

            if (AnyConnectorIsConnected() && !((craft_operation & CRAFT_MODE_ORBITAL) > 0))
            {
                Echo("DM:docked");
                setMode(MODE_DOCKED);
            }
            if (iMode == MODE_IDLE) doModeIdle();
            else if (iMode == MODE_DESCENT) doModeDescent();
        }
        #endregion


        #region modeidle 
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            setMode(MODE_IDLE);
            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
              StatusLog(moduleName + " Manual Control", textPanelReport);
            if ((craft_operation & CRAFT_MODE_ORBITAL) > 0)
            {
                if (dGravity <= 0)
                {
                    if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
                    else setMode(MODE_INSPACE);
                }
                else setMode(MODE_HOVER);
            }
        }
        #endregion




    }
}