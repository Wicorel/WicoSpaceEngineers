using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        WicoIGC _wicoIGC;
        WicoBlockMaster _wicoBlockMaster;
        WicoElapsedTime _wicoElapsedTime;
        WicoControl _wicoControl;

        Projectors _projectors;
        WicoBasicThrusters _basicThrusters;
        Connectors _connectors;
        PowerProduction _power;
        GasTanks _tanks;
        PrintHeadCutters _cutters;
        Welders _welders;

        Underconstruction _underconstruction;
        

        void ModuleProgramInit()
        {

            _wicoIGC = new WicoIGC(this);

            _wicoBlockMaster = new WicoBlockMaster(this); // must be before any other block-oriented modules
//            _wicoBlockMaster.SetMeGridOnly(true);
            _wicoBlockMaster.LoadLocalGrid();

            _wicoControl = new WicoControl(this, _wicoIGC);

            _wicoElapsedTime = new WicoElapsedTime(this, _wicoControl);

            _wicoElapsedTime.AddTimer("GridCheck");

            _projectors = new Projectors(this, _wicoBlockMaster, true);
            _basicThrusters = new WicoBasicThrusters(this, _wicoBlockMaster, true);
            _connectors = new Connectors(this, _wicoBlockMaster, true);
            _power = new PowerProduction(this, _wicoBlockMaster, true);
            _tanks = new GasTanks(this, _wicoBlockMaster, true);
            _cutters = new PrintHeadCutters(this, _wicoBlockMaster, true);
            _welders = new Welders(this, _wicoBlockMaster);

            _underconstruction = new Underconstruction(this, _wicoControl, _wicoBlockMaster
                , _wicoIGC
                , _connectors
                , _basicThrusters
                , _projectors
                , _power
                , _tanks
                , _cutters
                , _welders
                );


        }

        public void ModulePreMain(string argument, UpdateType updateSource)
        {
            Echo("run=" + runCount);
            if (_wicoControl != null && _wicoControl._bUpdateDebug) Echo("Update=" + updateSource.ToString());
            //            Echo("UpdateSource=" + updateSource.ToString());
            //            Echo(" Main=" + wicoControl.IamMain().ToString());
        }

        public void ModulePostMain(UpdateType updateSource)
        {
            if(bInitDone)
            {
                // ensure we run at least at slow speed for updates.
                _wicoControl.WantSlow();

                if(_wicoElapsedTime.IsInActiveOrExpired("GridCheck"))
                {
                    _wicoElapsedTime.RestartTimer("GridCheck");

                    _wicoBlockMaster.CalcLocalGridChange(false);
                    _wicoBlockMaster.CalcRemoteGridChange();
                    _power.CalcPower();
                    _power.BatteryCheck(0, true);
                }
//                Echo("projectors=" + _projectors.Count());
//                _projectors.DoProjectorCheck(true);

            }
            _wicoControl.AnnounceState();
            Runtime.UpdateFrequency = _wicoControl.GenerateUpdate();

            Echo("LastRun=" + LastRunMs.ToString("0.00") + "ms Max=" + MaxRunMs.ToString("0.00") + "ms");
            EchoInstructions();
        }
        public void ModulePostInit()
        {

        }


    }
}
