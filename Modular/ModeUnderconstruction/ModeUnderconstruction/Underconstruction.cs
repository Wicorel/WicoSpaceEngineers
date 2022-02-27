using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Underconstruction
        {
            /*
             * TODO:
             *  need to rename constructed grid
             *  
             */
            private Program _program;
            private WicoControl _wicoControl;
            private WicoBlockMaster _wicoBlockMaster;

            private Connectors _connectors;
            private WicoBasicThrusters _thrusters;
            private Projectors _projectors;
            private PowerProduction _power;
            private GasTanks _tanks;
            private PrintHeadCutters _cutters;
            private Welders _welders;

            List<IMyProgrammableBlock> pbList = new List<IMyProgrammableBlock>();

            /// <summary>
            /// The name of this printer head
            /// </summary>
            string PrinterName = "";
            MyIni blockIni = new MyIni();
            const string ProjectorSection = "PrintHeadControl";

            public Underconstruction(Program program, WicoControl wc, WicoBlockMaster wbm
                , WicoIGC iGC
                , Connectors connectors
                , WicoBasicThrusters thrusters
                , Projectors projectors
                , PowerProduction power
                , GasTanks tanks
                , PrintHeadCutters cutters
                , Welders welders

                ) 

            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;

                _connectors = connectors;
                _thrusters = thrusters;
                _connectors = connectors;
                _projectors = projectors;
                _power = power;
                _tanks = tanks;
                _cutters = cutters;
                _welders = welders;

                _program.moduleName += " Underconstruction";
                _program.moduleList += "\nUnderconstruction V4.2a";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
                _wicoBlockMaster.AddLocalBlockParseDone(LocalGridParseDone);

                PrinterName = _program._CustomDataIni.Get(ProjectorSection, "PrinterName").ToString("none");
                _program._CustomDataIni.Set(ProjectorSection, "PrinterName", PrinterName);
                _program.CustomDataChanged();
            }
            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_DOCKED)
                {
                    _wicoControl.WantFast();
                }
            }

            /// <summary>
            /// Modes have changed and we are being called as a handler
            /// </summary>
            /// <param name="fromMode"></param>
            /// <param name="fromState"></param>
            /// <param name="toMode"></param>
            /// <param name="toState"></param>
            public void ModeChangeHandler(int fromMode, int fromState, int toMode, int toState)
            {
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                string[] varArgs = sArgument.Trim().Split(';');

                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
                    // Commands here:

                }
                if (myCommandLine != null)
                {
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if(iMode!= WicoControl.MODE_UNDERCONSTRUCTION)
                {
                    if (_projectors.IsWorkingProjector())
                        _wicoControl.SetMode(WicoControl.MODE_UNDERCONSTRUCTION);
                }
                if (_cutters.AvailableCutters() < 1)
                {
                    _program.Echo("WARNING: No local Cutters");
                }
                if (_projectors.ManagedCount() < 1)
                    _program.Echo("WARNING: No Managed Projectors");

                if (_projectors.Count() < 1)
                {
                    _program.Echo("WARNING: No Projectors");
                }
                if (_projectors.ManagedCount() < 1)
                {
                    _program.Echo("WARNING: No Welders");
                }

                //                _program.Echo("Cutters=" + _cutters.AvailableCutters());

                if (iMode == WicoControl.MODE_UNDERCONSTRUCTION) { doModeUnderconstruction(); return; }
            }

            int oldPBCount = 0;
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyProgrammableBlock)
                {
                    pbList.Add(tb as IMyProgrammableBlock);
                }

            }
            void LocalGridChangedHandler()
            {
                pbList.Clear();
            }
            void LocalGridParseDone()
            {
                if (oldPBCount > 0 && oldPBCount != pbList.Count)
                {
                    _wicoControl.WicoControlInit(); // renegotiate who is in controll
                }
                oldPBCount = pbList.Count;

            }


            /*
            0 init
            50 make sure print head is complete 1) turn it on ->50
            55 2) wait for it to complete ->60
            60 turn on requested projector to make desired ship ->100

            100 build in progress.
            200 completed. check for power, etc. turn off projector
            300 Start cut
            400 cut in progress.
            500 waiting for... TBD

            TODO: Handle piston wall of welders
            TODO: Handle rotor wall/brush of welders

            TODO: Handle sequencing of welders so all don't have to be on at same time. (no timing problems unless wall of welders)

            */

            long startingblocksCount = 0;

            void doModeUnderconstruction()
            {
//                StatusLog("clear", textPanelReport);
//                StatusLog(moduleName + ":Under Construction!", textPanelReport);
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                _program.Echo("Under Contruction");

                if (_connectors.AnyConnectorIsLocked())
                    _connectors.ConnectAnyConnectors();

                if (iState == 0)
                {
                    _thrusters.powerDownThrusters(WicoBasicThrusters.thrustAll, true);

                    // TODO: needed welders should be defined in projector customdata.

                    _welders.RequestEnable(PrinterName);
                    _wicoControl.SetState(50);
                }
                else if(iState == 50)
                {
                    // make sure print head is complete
                    _projectors.ControlPrintHead(); // turn it on
                    _wicoControl.SetState(55);

                }
                else if(iState == 55)
                {
                    // wait for print head to be completed
                    // TODO: add timeout
                    if(_projectors.PrintHeadCompleted())
                    {
                        _wicoControl.SetState(60);
                    }
                }
                else if(iState==60)
                {
                    // print head is completed.  Start with projection build
                    // TODO: Turn on requested projector
                    _wicoControl.SetState(100);

                }
                else if (iState == 100)
                {
                    // waiting for build to complete
                    // recreate getLocalConnectors, but don't ignore connectors that are connected to 'us'
                    //                    connectorsInit();

                    _connectors.ConnectAnyConnectors();
                    if(_cutters.AvailableCutters()<1)
                    {
                        _program.Echo("WARNING: No Cutters");
                    }

                    // TODO: 
                    // Try to pull stuff
                    //  Uranium to reactors

                    //  Ice to Gas Gens.
                    //                    gasgenInit();

                    // Turn batteries to recharge
                    _power.BatteryCheck(100, true);// batteryDischargeSet(true, false);

                    // put tanks to "Stockpile"
//                    tanksInit();
                    _tanks.TanksStockpile(true);

                    if (!_projectors.DoProjectorCheck())
                    { // we are done projecting (or somebody else turned them off)
                        _wicoControl.SetState(200);
                    }
                }
                else if (iState == 200)
                { // turn off managed projectors.
                    _projectors.ControlManagedProjectors(false);
                    // turn off managed welders
                    _welders.RequestEnable(PrinterName, false);

                    //TODO:
                    // check for 'enough' power to continue alone.
                    {

                        // when 'enough':
                        // turn tanks off stockpile
                        _tanks.TanksStockpile(false);// _TanksStockpile(false);
                        _power.BatteryCheck(0, true);// batteryDischargeSet(true, false);

                        // turn batteries off recharge
                        //                        batteryDischargeSet(true, true);
                        // (request to) turn off welders

                        // then change state:
                        _wicoControl.SetState(300);
                    }
                }
                else if (iState == 300)
                { // start the cut 
                    startingblocksCount = _wicoBlockMaster.localBlocksCount;
                    _cutters.DoCut();
                    _projectors.ControlPrintHead(); // turn on print head so we can track build progress
                    _wicoControl.SetState(400);
                }
                else if (iState == 400)
                { // cut-off in progress
                    _cutters.DoCut();
                    if (_projectors.DoProjectorCheck(true,true) // the printhead blueprint is missing something; assume it's the spru that we just cut off
                        || startingblocksCount!= _wicoBlockMaster.localBlocksCount)
                    {
                        _cutters.DoCut(false);
                        _wicoControl.SetState(500);
                    }
                    // maybe need a time-out?
                }
                else if (iState == 500)
                { // cut-off done.
                    _projectors.ControlPrintHead(false); // turn off print head


                    // we are now the print head and not the drone.  so we do not dock...
                    // TODO: tell the new drone what to do (ie dock/autolaunch)

                }
            }

        }
    }
}
