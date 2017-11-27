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
        #region power

        void initPower()
        {
            totalMaxPowerOutput = 0;
            Echo("Init Reactors");
            initReactors();
            Echo("Init Solar");
            initSolars();
            Echo("Init Batteries");
            initBatteries();
            if (maxReactorPower > 0)
                totalMaxPowerOutput += maxReactorPower;
            //	if (maxSolarPower > 0)
            //		totalMaxPowerOutput += maxSolarPower;
            if (maxBatteryPower > 0)
                totalMaxPowerOutput += maxBatteryPower;
        }

        #endregion
    }
}