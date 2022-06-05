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
        public class SystemsMonitor
        {
            private Program _program;
            private WicoElapsedTime _elapsedTime;
            //            private WicoControl _wicoControl;
            //            private WicoBlockMaster _wicoBlockMaster;
            private Connectors _connectors;
            private WicoThrusters _thrusters;
            private Antennas _antennas;
            private GasTanks _tanks;
            public WicoGyros _gyros;
            private PowerProduction _power;
            //            private Timers _timers;
            //            private WicoIGC _wicoIGC;
            //            private WicoBases _wicoBases;
            //            private NavCommon _navCommon;
            private CargoCheck _cargoCheck;
            //            private Displays _displays;

            public bool bAutoRefuel = true;
            const string AutoRefuel = "AutoRefuel";

            const string SystemsSection = "SYSTEMS";

            public SystemsMonitor(Program program
                ,WicoElapsedTime elapsedTime
                //                , WicoControl wc, WicoBlockMaster wbm
                , WicoThrusters thrusters
                , Connectors connectors
                , Antennas ant
                , GasTanks gasTanks
                , WicoGyros wicoGyros
                , PowerProduction pp
                //                , Timers tim
                //                , WicoIGC iGC
                //                , WicoBases wicoBases
                //                , NavCommon navCommon
                , CargoCheck cargoCheck
                //                , Displays displays
                )
            {
                _program = program;
                _elapsedTime = elapsedTime;
                //                _wicoControl = wc;
                //                _wicoBlockMaster = wbm;
                _thrusters = thrusters;
                _connectors = connectors;
                _antennas = ant;
                _tanks = gasTanks;
                _gyros = wicoGyros;
                _power = pp;
                //                _timers = tim;
                //                _wicoIGC = iGC;
                //                _wicoBases = wicoBases;
                //                _navCommon = navCommon;
                _cargoCheck = cargoCheck;
                //               _displays = displays;

                bAutoRefuel = _program.CustomDataIni.Get(SystemsSection, AutoRefuel).ToBoolean(bAutoRefuel);
                _program.CustomDataIni.Set(SystemsSection, AutoRefuel, bAutoRefuel);

                _program.AddTriggerHandler(ProcessTrigger);

                _elapsedTime.AddTimer(AirWorthyTimerName, 1);

            }

            public bool BatteryGo = true;
            public bool TanksGo = true;
            public bool ReactorsGo = true;
            public bool CargoGo = true;

            const string AirWorthyTimerName = "AirWorthyCheck";

            public bool AirWorthy(bool bForceCheck = false, bool bLaunchCheck = true, int cargohighwater = 1)
            {
                BatteryGo = true;
                TanksGo = true;
                ReactorsGo = true;
                CargoGo = true;

                bool bDoChecks = bForceCheck;

                if (_elapsedTime.IsInActiveOrExpired(AirWorthyTimerName))
                {
//                    _program.Echo("Restarting " + AirWorthyTimerName);
                    _elapsedTime.RestartTimer(AirWorthyTimerName);
                    bDoChecks = true;
                }
//                else _program.Echo(AirWorthyTimerName + ": Not Expired");
//                _program.Echo("AirWorthy:DoChecks=" + bDoChecks);

                // Check battery charge
                if (bDoChecks) _power.BatteryCheck(0, false);
                if (bLaunchCheck)
                {
                    if (_power.batteryPercentage >= 0 && _power.batteryPercentage < _power.batterypcthigh)
                    {
                        //                        _program.ErrorLog("Battery not airworthy (launch)");
                        BatteryGo = false;
                    }

                }
                else
                {
                    // check if we need to go back and refill
                    if (_power.batteryPercentage >= 0 && _power.batteryPercentage < _power.batterypctlow)
                    {
                        //                        _program.ErrorLog("Battery not airworthy");
                        BatteryGo = false;
                    }
                }

                // check cargo emptied
                if (bDoChecks) _cargoCheck.doCargoCheck();
                if (bLaunchCheck)
                {
                    if (_cargoCheck.cargopcent > _cargoCheck.cargopctmin)
                    {
                        //                        _program.ErrorLog("Cargo not airworthy (launch)");
                        CargoGo = false;
                    }
                }
                else
                {
                    if (_cargoCheck.cargopcent > cargohighwater)
                    {
                        //                        _program.ErrorLog("Cargo not airworthy");
                        CargoGo = false;
                    }
                }
                if (bDoChecks) _tanks.TanksCalculate();
                if (bLaunchCheck)
                {
                    if (_tanks.HasHydroTanks() && _tanks.hydroPercent < _tanks.tankspcthigh)
                    {
                        //                        _program.ErrorLog("Tanks not airworthy (launch) "+_tanks.hydroPercent.ToString("0.00"));
                        TanksGo = false;
                    }
                }
                else
                {
                    if (_tanks.HasHydroTanks() && _tanks.hydroPercent < _tanks.tankspctlow)
                    {
                        //                        _program.ErrorLog("Tanks not airworthy " + _tanks.hydroPercent.ToString("0.00"));
                        TanksGo = false;
                    }
                }
                // TODO: check reactor fuel

                if (BatteryGo && TanksGo && ReactorsGo && CargoGo)
                {
                    return true;
                }
                else return false;

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
                    if (myCommandLine.Argument(0) == "refuel")
                    {
                        bAutoRefuel = !bAutoRefuel;
                        string s = "Autorefuel=" + bAutoRefuel.ToString();
                        _program.Echo(s);
                        _program.ErrorLog(s);
                        _program.CustomDataIni.Set(SystemsSection, AutoRefuel, bAutoRefuel);
                        _program.CustomDataChanged();
                    }
                    for (int arg = 0; arg < myCommandLine.ArgumentCount; arg++)
                    {
                        string sArg = myCommandLine.Argument(arg);
                        // commands here:
                    }
                }
            }

            public bool HasHydroTanks()
            {
                return _tanks.HasHydroTanks();
            }

            public double oxyPercent { get { return _tanks.oxyPercent; } }

            public double hydroPercent { get { return _tanks.hydroPercent; } }

            public int tankspcthigh { get { return _tanks.tankspcthigh; } }

            public void TanksCalculate()
            {
                _tanks.TanksCalculate();
            }

            public void TanksStockPile(bool bStockPile = true, int iTypes = 255)
            {
                _tanks.TanksStockpile(bStockPile, iTypes);
            }

            public void BatterySetNormal()
            {
                _power.BatterySetNormal();
            }
            public bool BatteryCheck(int targetMax, bool bEcho = true, bool bProgress = false, bool bFastRecharge = false)
            {
                return _power.BatteryCheck(targetMax, bEcho, bProgress, bFastRecharge);
            }
            public bool HasBatteries()
            {
                return _power.HasBatteries();
            }
            public int batteryPercentage { get { return _power.batteryPercentage; } }
            public int batterypctlow { get { return _power.batterypctlow; } }
            public int batterypcthigh { get { return _power.batterypcthigh; } }

            public int cargopcent { get { return _cargoCheck.cargopcent; } }

            public int cargopctmin { get { return _cargoCheck.cargopctmin; } }
            public int cargohighwater { get { return _cargoCheck.cargohighwater; } }

            public void doCargoCheck()
            {
                _cargoCheck.doCargoCheck();
            }

            public void RequestRefuel()
            {
                if (bAutoRefuel)
                {
                    _tanks.TanksStockpile(true);
                    _power.BatteryCheck(0, true);
                }
            }

            public void RequestLaunchSettings()
            {
                _tanks.TanksStockpile(false);
                _power.BatterySetNormal();
                _connectors.TurnEjectorsOff();
                _thrusters.powerDownThrusters(); // turns ON all thrusters.
            }

            public bool AnyConnectorIsConnected()
            {
                return _connectors.AnyConnectorIsConnected();
            }
            public bool AnyConnectorIsLocked()
            {
                return _connectors.AnyConnectorIsLocked();
            }
            public IMyTerminalBlock GetConnectedConnector(bool bMe = false)
            {
                return _connectors.GetConnectedConnector(bMe);
            }
            public void ConnectAnyConnectors(bool bConnect = true, bool bOn = true)
            {
                _connectors.ConnectAnyConnectors(bConnect, bOn);
            }
            public IMyTerminalBlock GetDockingConnector() // maybe pass in prefered orientation?
            {
                return _connectors.GetDockingConnector();
            }

            public void TurnEjectorsOff()
            {
                _connectors.TurnEjectorsOff();
            }
            public void TurnEjectorsOn()
            {
                _connectors.TurnEjectorsOn();
            }

            public double currentTotalOutput { get { return _power.currentTotalOutput; } }
            public double maxTotalPower { get { return _power.maxTotalPower; } }

            public int EnginesCount()
            {
                return _power.EnginesCount();
            }
            public bool EnginesAreOff()
            {
                return _power.EnginesAreOff();
            }

            /*
             * Gyros
             * 
             */
            public Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
            {
                return _gyros.VectorRejection(a, b);
            }
            public bool DoRotate(double rollAngle, string sPlane = "Roll", float maxYPR = -1, float facingFactor = 1f)
            {
                return _gyros.DoRotate(rollAngle, sPlane, maxYPR, facingFactor);
            }
            public float MaxYPR { get { return _gyros.MaxYPR; } }
            public double CalculateYaw(Vector3D destination, IMyTerminalBlock Origin)
            {
                return _gyros.CalculateYaw(destination, Origin);
            }
            public void gyrosOff()
            {
                _gyros.gyrosOff();
            }

            public void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll)
            {
                _gyros.GetRotationAnglesSimultaneous(desiredForwardVector, desiredUpVector, worldMatrix, out pitch, out yaw, out roll);
            }
            public void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
            {
                _gyros.ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, gyroList, worldMatrix);
            }

            public bool AlignGyros(string argument, Vector3D vDirection, IMyTerminalBlock gyroControlPoint)
            {
                return _gyros.AlignGyros(argument, vDirection, gyroControlPoint);
            }
            public void SetMinAngle(float angleRad = 0.01f)
            {
                _gyros.SetMinAngle(angleRad);
            }
            public bool BeamRider(Vector3D vStart, Vector3D vEnd, IMyTerminalBlock OrientationBlock)
            {
                return _gyros.BeamRider(vStart, vEnd, OrientationBlock);
            }


            public void ResetMotion()
            {
                _gyros.gyrosOff();
                _thrusters.powerDownThrusters();

            }

            /*
             * THRUSTERS
             * 
             */
            public double calculateMaxThrust(List<IMyTerminalBlock> thrusters, int iTypes = WicoThrusters.thrustAll)
            {
                return _thrusters.calculateMaxThrust(thrusters, iTypes);
            }

            public void ThrustersCalculateOrientation(IMyTerminalBlock orientationBlock, ref List<IMyTerminalBlock> thrustForwardList,
                ref List<IMyTerminalBlock> thrustBackwardList, ref List<IMyTerminalBlock> thrustDownList, ref List<IMyTerminalBlock> thrustUpList,
                ref List<IMyTerminalBlock> thrustLeftList, ref List<IMyTerminalBlock> thrustRightList)
            {
                _thrusters.ThrustersCalculateOrientation(orientationBlock
                    , ref thrustForwardList, ref thrustBackwardList
                    , ref thrustDownList, ref thrustUpList
                    , ref thrustLeftList, ref thrustRightList);
            }
            public int powerUpThrusters(List<IMyTerminalBlock> thrusters, float fPower = 100f, int iTypes = WicoThrusters.thrustAll)
            {
                return _thrusters.powerUpThrusters(thrusters, fPower, iTypes);

            }
            public int powerDownThrusters(int iTypes = WicoThrusters.thrustAll, bool bForceOff = false)
            {
                return _thrusters.powerDownThrusters(iTypes, bForceOff);
            }
            public int powerDownThrusters(List<IMyTerminalBlock> thrusters, int iTypes = WicoThrusters.thrustAll, bool bForceOff = false)
            {
                return _thrusters.powerDownThrusters(thrusters, iTypes, bForceOff);
            }
            public double calculateStoppingDistance(double physicalMass, List<IMyTerminalBlock> thrustStopList, double currentV, double dGrav)
            {
                return _thrusters.calculateStoppingDistance(physicalMass, thrustStopList, currentV, dGrav);
            }
            public void MoveForwardSlowReset()
            {
                _thrusters.MoveForwardSlowReset();
            }
            /// <summary>
            /// Move using specified thrusters at slow speed.
            /// </summary>
            /// <param name="fTarget">Target speed in mps</param>
            /// <param name="fAbort">Abort speed in mps.  Emergency stop if above this speed</param>
            /// <param name="mfsForwardThrust">thrusters for 'forward'</param>
            /// <param name="mfsBackwardThrust">reverse thrusters to slow down. 'Back'</param>
            /// <param name="effectiveMass"></param>
            /// <param name="shipSpeed"></param>
            public void MoveForwardSlow(float fTarget, float fAbort, List<IMyTerminalBlock> mfsForwardThrust,
                List<IMyTerminalBlock> mfsBackwardThrust, double effectiveMass, double shipSpeed)
            {
                _thrusters.MoveForwardSlow(fTarget, fAbort, mfsForwardThrust, mfsBackwardThrust, effectiveMass, shipSpeed);
            }

            public void GetBestThrusters(Vector3D v1,
                List<IMyTerminalBlock> thrustForwardList, List<IMyTerminalBlock> thrustBackwardList,
                List<IMyTerminalBlock> thrustDownList, List<IMyTerminalBlock> thrustUpList,
                List<IMyTerminalBlock> thrustLeftList, List<IMyTerminalBlock> thrustRightList,
                out List<IMyTerminalBlock> thrustTowards, out List<IMyTerminalBlock> thrustAway
                )
            {
                _thrusters.GetBestThrusters(v1,
                    thrustForwardList, thrustBackwardList,
                    thrustDownList, thrustUpList,
                    thrustLeftList, thrustRightList,
                    out thrustTowards, out thrustAway
                    );
            }
        }
    }
}
        