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
            CamerasInitCustomData(iniCustomData);

            //            ThrustersInitCustomData(iniCustomData);

            /*
            if (iniCustomData.IsDirty)
            {
                Me.CustomData = iniCustomData.GenerateINI(true);
            }
            */
        }

        #region maininit

        string doInit()
        {
            // initialization of each module goes here:

            // when all initialization is done, set init to true.
            Echo(moduleName+ " Init:" + currentInit);
            if (currentInit == 0)
            {
                StatusLog(DateTime.Now.ToString() + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

                	if(!modeCommands.ContainsKey("doscan")) modeCommands.Add("doscan", MODE_DOSCAN);
                //	if(!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
                //	if(!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

                sInitResults += gridsInit();
                initLogging();
                initTimers();
                sInitResults += SerializeInit();
 
                Deserialize();
                sInitResults += DefaultOrientationBlockInit();
                sInitResults += antennaInit();
                if(!SetAntennaMe())
                {
                    bStartupError = true;
                    sStartupError += "\nNo Antenna Available";
                }


            }
            else if (currentInit == 1)
            {
                initAsteroidsInfo();
                initOreLocInfo();
                sInitResults += camerasensorsInit(shipOrientationBlock);
                sInitResults += connectorsInit();
                sInitResults += initDockingInfo();
                if (localBaseConnectors.Count < 1)
                    sStartupError+="\nNo [BASE] Connectors found";

                sInitResults += modeOnInit(); // handle mode initializting from load/recompile..

                init = true;
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