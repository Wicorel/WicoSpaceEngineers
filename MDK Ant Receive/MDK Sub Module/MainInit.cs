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
        #region maininit

        string sInitResults = "";
 //       string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {

            // initialization of each module goes here:

            // when all initialization is done, set init to true.
            initLogging();

            Echo("Init");
            if (currentInit == 0)
            {
                StatusLog(DateTime.Now.ToString() + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                //	if(!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
                //	if(!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

                sInitResults += gridsInit();

                initTimers();

                sInitResults += initSerializeCommon();

                Deserialize();
                sInitResults += antennaInit();

                sInitResults += modeOnInit(); // handle mode initializting from load/recompile..
                init = true;

            }

            currentInit++;
            if (init) currentInit = 0;

            Log(sInitResults);

            return sInitResults;

        }

        IMyTextPanel gpsPanel = null;

        string BlockInit()
        {
            string sInitResults = "";


            return sInitResults;
        }

        #endregion

        string modeOnInit()
        {
            // check current state and perform reload init to correct state
            return ">";
        }


    }
}