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
        void doModuleConstructor()
        {
            // called from main constructor.
        }

        void ModuleInitCustomData(INIHolder iniCustomData)
        {
            //           INIHolder iniCustomData = new INIHolder(this, Me.CustomData);

            //            string sValue = "";
            ConnectorInitCustomData(iniCustomData);
            BaseInitCustomData(iniCustomData);

            //            ThrustersInitCustomData(iniCustomData);

            /*
            if (iniCustomData.IsDirty)
            {
                Me.CustomData = iniCustomData.GenerateINI(true);
            }
            */
        }

        #region maininit

        string sInitResults = "";
        //       string sArgResults = "";

        int currentInit = 0;

        string doInit()
        {
            // initialization of each module goes here:

            // when all initialization is done, set init to true.
            Echo(moduleName+ " Init:" + currentInit);
            if (currentInit == 0)
            {
                StatusLog(DateTime.Now.ToString() + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                //	if(!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
                //	if(!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

                sInitResults += gridsInit();
                initLogging();
                initTimers();
                sInitResults += SerializeInit();
 
                Deserialize();
                sInitResults += antennaInit();
                SetAntennaMe();

                sInitResults += modeOnInit(); // handle mode initializting from load/recompile..

            }
            else if (currentInit == 1)
            {
                sInitResults += connectorsInit();
                sInitResults += initDockingInfo();
                sInitResults += DefaultOrientationBlockInit();
                init = true;
                if (localBaseConnectors.Count < 1)
                    sInitResults = "\nNo [BASE] Connectors found\n" + sInitResults;

            }
            currentInit++;
            if (init) currentInit = 0;

            Log(sInitResults);

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