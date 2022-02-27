using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    partial class Program
    {
        public class AttackDrone
        {
            Program _program;
            WicoControl _wicoControl;
            WicoBlockMaster _wicoBlockMaster;
            WicoIGC _wicoIGC;
            WicoElapsedTime _wicoElapsedTime;
            WicoGyros _gyros;
            WicoThrusters _wicoThrusters;

            Cameras _Cameras;

            Weapons _weapons;

            public bool _Debug = false;
            public Vector3D VAttackTarget { get; set; }

            public AttackDrone(Program program, WicoControl wc, WicoBlockMaster wbm, WicoIGC wicoIGC
                , WicoElapsedTime wicoElapsedTime, WicoGyros wicoGyros, WicoThrusters wicoThrusters
                , Cameras wicoCameras, Weapons weapons
              ) 
            {
                _program = program;
                _wicoControl = wc;
                _wicoBlockMaster = wbm;
                _wicoIGC = wicoIGC;
                _wicoElapsedTime = wicoElapsedTime;
                _gyros = wicoGyros;
                _wicoThrusters = wicoThrusters;

                _Cameras = wicoCameras;

                _weapons = weapons;

                _program.moduleName += " AttackDrone";
                _program.moduleList += "\nAttack Drone V4.0";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoControl.AddModeInitHandler(ModeInitHandler);
                _wicoControl.AddControlChangeHandler(ModeChangeHandler);

                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

  //              _wicoIGC.AddPublicHandler(NavCommon.WICOB_NAVADDTARGET, BroadcastHandler);

            }
            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "MODE")
                {
                }
            }

            void LoadHandler(MyIni Ini)
            {
                Vector3D v3D;

                Vector3D.TryParse(Ini.Get(sAttackDroneSection, "vAttackTarget").ToString(), out v3D);
                VAttackTarget = v3D;
                Ini.Set(sAttackDroneSection, "vAttackTarget", VAttackTarget.ToString());
            }

            void SaveHandler(MyIni Ini)
            {
                Ini.Set(sAttackDroneSection, "vAttackTarget", VAttackTarget.ToString());
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
                if (toMode <= 0 || toMode == WicoControl.MODE_ATTENTION)
                {
//                    NavReset();
                }

            }
            /// <summary>
            /// just after program init, we are starting with these modes
            /// </summary>
            void ModeInitHandler()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_GOINGTARGET)
                {
                    // TODO: Check state and re-init as needed
                    _wicoControl.WantFast();
                }
            }
            void LocalGridChangedHandler()
            {
                //               shipController = null;
            }

            /// <summary>
            /// Handler for processing any of the 'trigger' upatetypes
            /// </summary>
            /// <param name="argument"></param>
            /// <param name="updateSource"></param>
            public void ProcessTrigger(string sArgument, MyCommandLine myCommandLine, UpdateType updateSource)
            {
                string[] varArgs = sArgument.Trim().Split(';');

                //                _program.ErrorLog("#Args=" + varArgs.Length);
                for (int iArg = 0; iArg < varArgs.Length; iArg++)
                {
                    string[] args = varArgs[iArg].Trim().Split(' ');
                    //                    _program.ErrorLog("Arg[" + iArg + "]=" + varArgs[iArg]);
                    if (args[0] == "allattack")
                    {
                        findAttackTarget();
                    }
                    else if (args[0] == "setstandoff")
                    {
                        if (args.Length < 2)
                        {
                            _program.Echo("setstandoff:nvalid arg");
                            continue;
                        }
                        long lValue = 0;
                        bool fOK = long.TryParse(args[1].Trim(), out lValue);
                        if (!fOK)
                        {
                            _program.Echo("invalid long value:" + args[1]);
                            continue;
                        }
                        if (lValue < 1999 && lValue > 10) sqStandoffDistance = lValue;

                    }
                }
                if (myCommandLine != null)
                {
                    for (int arg = 0; arg < myCommandLine.ArgumentCount; arg++)
                    {
                        string sArg = myCommandLine.Argument(arg);
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;

                if (iMode == WicoControl.MODE_ATTACKDRONE) { doModeAttackDrone(); return; }
            }

            void BroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
            }


            List<IMyTerminalBlock> thrustForwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustBackwardList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustDownList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustUpList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustLeftList = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> thrustRightList = new List<IMyTerminalBlock>();

            string sAttackDroneSection = "ATTACKDRONE";

            /*
             * 0 Masterinit
             * 10 check for target. If none. try to find one.
             * 11 process found target
             * 50 received attack target command.
             * 51 aim towards target and scan when ready
             * 
             * Attack Plans: 
             * 1 = danger close circle strafe
             * 20 = square Dance
             * 30 = tailpipe
             * 40 = long side fast strafe
             * 100 = track target
             * 
             * Attack Plans: Danger Close Circle Strafe
             * 100 found target. >1000m away
             * 101 found target. <=1000m away
             * 
             * Attack Plan: Square Dance
             * 200 found target
             * 
             * Attack Plan: tailpipe
             * 300 found target
             * 
             * Attack Plan: Long fast strafe
             * 400 found target
             * 
             * Attack Plan: Track Target
             * 1000 Found Target
             */
            bool bHitLastTarget = false;

            // squaredance settings
            long sqStandoffDistance = 850;

            long targetPaintStandoffDistance = 2000;

            long lSqDanceDist = 802; // distance to do square dance from..
            long sqDanceCount = 0;
            long ticksPerDirection = 64; // how many ticks to use a certain direction of movement.
            double dAimOffset = 50; // 0-100.  Default center
            double dAimDelta = 1;

            long panCorner1 = 0; // 
            long panCorner2 = 4;

            bool bBoxVerbose = false;
            bool bScanTargetVerbose = false;

            long defaultScanMax = 10000;

            MyDetectedEntityInfo targetDetectedInfo = new MyDetectedEntityInfo();
            MyDetectedEntityInfo initialTargetDetectedInfo = new MyDetectedEntityInfo();

            bool bWeaponsHot = true;
            long iAttackPlan = 0;
            bool bFriendlyFire = true;


            void doModeAttackDrone()
            {
                int iMode = _wicoControl.IMode;
                int iState = _wicoControl.IState;
                sbNotices.Clear();
                sbModeInfo.Clear();

                sbNotices.AppendLine("Attack Drone");
                _program.Echo("Attack Drone: state=" + iState.ToString());

                IMyShipController shipController = _wicoBlockMaster.GetMainController();
                Vector3D vNG = shipController.GetNaturalGravity();
                double dGravity = vNG.Length();
                double velocityShip= shipController.GetShipSpeed(); ;

                MyShipMass myMass;
                myMass = ((IMyShipController)shipController).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass; ;

                if (thrustForwardList.Count < 1)
                {
                    _wicoThrusters.ThrustersCalculateOrientation(shipController,
                        ref thrustForwardList, ref thrustBackwardList,
                        ref thrustDownList, ref thrustUpList,
                        ref thrustLeftList, ref thrustRightList
                        );
                }

                double maxThrust = _wicoThrusters.calculateMaxThrust(thrustForwardList);
                double maxDeltaV = (maxThrust) / effectiveMass;

                _program.Echo("velocity=" + _wicoBlockMaster.GetShipVelocity().ToString("0.00"));

                Vector3D currentpos = shipController.GetPosition();

                if (iState == 0)
                {
                    if (dGravity <= 0)
                        _wicoControl.SetState(160);
                    else
                        _wicoControl.SetState(150);
                    _wicoControl.WantFast();
                }
                else if (iState == 10)
                {
                    if (_Cameras.CameraForwardScan(defaultScanMax))
                    { 
                        if (!_Cameras.lastDetectedInfo.IsEmpty())
                        { // we found something.
                            if (this.IsValidAttackTarget(_Cameras.lastDetectedInfo))
                            {
                                bHitLastTarget = true;
                                targetDetectedInfo = _Cameras.lastDetectedInfo;
                                initialTargetDetectedInfo = _Cameras.lastDetectedInfo;
                                _wicoControl.SetState(11);
                                _wicoControl.WantFast();
                            }
                            else _program.Echo("Not Valid Attack Target");

                        }
                        else
                        {
                            _program.ResetMotion();
                            _wicoControl.SetMode(WicoControl.MODE_NAVNEXTTARGET);
                            _wicoControl.WantFast();
                        }
                    }
                }
                else if(iState==11)
                {
                    // should dynamically select attack plan based on target
                    if (iAttackPlan == 0) iAttackPlan = 20;

                    double distancesq;
                    if (targetDetectedInfo.HitPosition != null)
                        distancesq = Vector3D.DistanceSquared(currentpos, (Vector3D)targetDetectedInfo.HitPosition);
                    else
                        distancesq = Vector3D.DistanceSquared(currentpos, (Vector3D)targetDetectedInfo.Position);

                    if (iAttackPlan == 1)
                    {
                        if (distancesq > 1000000)
                        {
                            _wicoControl.SetState( 100);
                        }
                        else
                        {
                            _wicoControl.SetState(101);
                        }
                    }
                    else if (iAttackPlan == 20) _wicoControl.SetState(200);
                    else if (iAttackPlan == 30) _wicoControl.SetState(300);
                    else if (iAttackPlan == 40) _wicoControl.SetState(400);
                    else if (iAttackPlan == 100) _wicoControl.SetState(1000);
                    else
                    {
                        _program.ResetMotion();
                        _wicoControl.SetMode(WicoControl.MODE_ATTENTION); // unknown attack plan
                    }

                }
                else if(iState==50)
                {

                }
                else if(iState==51)
                {

                }
                else if(iState==100)
                {

                }
                else if(iState==101)
                {

                }
                else if(iState==200)
                {
//                    StatusLog("clear", gpsPanel);
                    _program.ResetMotion();
                    Vector3D vVec;

                    Vector3D targetPos = (Vector3D)targetDetectedInfo.HitPosition;
                    //				Vector3d targetVec = targetPos - currentpos;

                    //				vVec = targetDetectedInfo.Position - currentpos;
                    Vector3D vExpectedTargetPos = targetPos; // need to add in velocity
                    vVec = vExpectedTargetPos - currentpos;
                    double distance = vVec.Length();
                    _program.Echo("Distance=" + distance);
                    /*
                    strbAttack.Clear();
                    strbAttack.Append("Name: " + targetDetectedInfo.Name);
                    strbAttack.AppendLine(); strbAttack.Append("Type: " + targetDetectedInfo.Type);
                    strbAttack.AppendLine(); strbAttack.Append("RelationShip: " + targetDetectedInfo.Relationship);
                    strbAttack.AppendLine(); strbAttack.Append("Size: " + targetDetectedInfo.BoundingBox.Size);
                    strbAttack.AppendLine(); strbAttack.Append("Velocity: " + targetDetectedInfo.Velocity);
                    strbAttack.AppendLine(); strbAttack.Append("Orientation: " + targetDetectedInfo.Orientation);
                    if (bScanTargetVerbose) Echo(strbAttack.ToString());
                    */
                    bool bAimed = false;
                    //	holdStandoff(distance, sqStandoffDistance);
                    
                    double stoppingM = _wicoThrusters.calculateStoppingDistance(effectiveMass,thrustBackwardList, velocityShip, 0);

                    //				if (distance > lSqDanceDist * 2 )//|| distance >750)
                    if ((distance - stoppingM) > (sqStandoffDistance * 1.5))//|| distance >750)
                    {
                        _program.Echo("Long Distance:  Closing");
                        bAimed=_gyros.AlignGyros("forward", vVec, shipController);
//                        bAimed = GyroMain("forward", vVec, shipOrientationBlock);
                        if (bAimed)
                        {
                            if (distance > sqStandoffDistance * 3)
                            {
                                if (velocityShip < 90)
                                    _wicoThrusters.powerUpThrusters(thrustForwardList, 45);
                                else
                                    _wicoThrusters.powerUpThrusters(thrustForwardList, 1);
                            }
                            else
                            {
                                if (velocityShip < 20) // seems like a low approach speed...
                                    _wicoThrusters.powerUpThrusters(thrustForwardList, 25);
                                else if (velocityShip < 35)
                                    _wicoThrusters.powerUpThrusters(thrustForwardList, 1);
                                // else already did ResetMotion()
                            }
                        }
                        // else already did resetmotion
                    }
                    else
                    {
                        _program.Echo("closer: Aiming");
                        // we want to sweep aim from front to back..
                        Vector3D[] avCorners = targetDetectedInfo.BoundingBox.GetCorners();

                        Vector3D vMin;
                        Vector3D vMax;
                        //					vMin=targetDetectedInfo.BoundingBox.Min;
                        //					vMax=targetDetectedInfo.BoundingBox.Max;
                        if (bBoxVerbose) _program.Echo("panCorner1:" + panCorner1);
                        if (bBoxVerbose) _program.Echo("panCorner2:" + panCorner2);
                        vMin = avCorners[panCorner1];
                        vMax = avCorners[panCorner2];
                        //					if ((panCorner2 - 4) == panCorner1)						vMax = targetDetectedInfo.Position; // go to center.

                        Vector3D vVBox = vMax - vMin;
 //                       debugGPSOutput("MinBound", vMin);
//                        debugGPSOutput("MaxBound", vMax);

                        double boundingDist = vVBox.Length();
                        vVBox.Normalize();

                        targetPos = vMin + vVBox * ((dAimOffset * boundingDist) / 100);
  //                      debugGPSOutput("targetposPan", targetPos);

                        vVec = targetPos - currentpos;
                        bAimed = _gyros.AlignGyros("forward", vVec, shipController);
//                        if (bBoxVerbose) Echo("dAimOffset:" + dAimOffset);
                        // pan back and forth..
                        if (bAimed)
                        {
                            _program.Echo("AIMED");
                            dAimOffset += dAimDelta;
                        }
                        else _program.Echo("Aiming");

                        if (dAimOffset < 0)
                        {
                            dAimOffset = 0;
                            dAimDelta = -dAimDelta;
                            panCorner1++;
                            if (panCorner1 > 3)
                            {
                                panCorner1 = 0;
                                panCorner2++;
                            }
                            if (panCorner2 > 7)
                            {
                                panCorner2 = 4;
                            }

                        }
                        else if (dAimOffset > 100)
                        {
                            dAimOffset = 100;
                            dAimDelta = -dAimDelta;
                        }
                    }

                    if (distance < lSqDanceDist)
                    {
                        _program.Echo("sqDanceCount:" + sqDanceCount);
                        if (sqDanceCount < ticksPerDirection) _wicoThrusters.powerUpThrusters(thrustDownList);
                        else if (sqDanceCount < ticksPerDirection * 2) _wicoThrusters.powerUpThrusters(thrustLeftList);
                        else if (sqDanceCount < ticksPerDirection * 3) _wicoThrusters.powerUpThrusters(thrustUpList);
                        else _wicoThrusters.powerUpThrusters(thrustRightList);

                        sqDanceCount++;
                        if (sqDanceCount > ticksPerDirection * 4) sqDanceCount = 0;
                    }
                    if (_Cameras.CameraForwardScan( distance + 100))
                    {
//                        strbAttack.Clear();
                        if (_Cameras.lastDetectedInfo.IsEmpty() || !IsValidAttackTarget(_Cameras.lastDetectedInfo))
                        { // hit nothing???
                            dAimOffset += dAimDelta; // move faster..
                        }
                        else
                        { // we found something.
//                            strbAttack.Append("Name: " + _Cameras.lastDetectedInfo.Name);
//                            strbAttack.AppendLine(); strbAttack.Append("Type: " + _Cameras.lastDetectedInfo.Type);
//                            strbAttack.AppendLine(); strbAttack.Append("Relationship: " + _Cameras.lastDetectedInfo.Relationship);
                            //		strbAttack.AppendLine();		strbAttack.Append("Orientation: " + lastDetectedInfo.Orientation);
//                            if (bScanTargetVerbose) _program.Echo(strbAttack.ToString());
                            double minsize = 3;
                            if (_Cameras.lastDetectedInfo.Type == MyDetectedEntityType.SmallGrid) minsize = 0.75;

                            if (_Cameras.lastDetectedInfo.BoundingBox.Size.X > minsize && IsValidAttackTarget(_Cameras.lastDetectedInfo))
                            {
                                targetDetectedInfo = _Cameras.lastDetectedInfo;
                                if (distance < 975)
                                {
                                    // SHOOT!
                                    if (bWeaponsHot) _weapons.WeaponsFireForward();// blockApplyAction(gatlingsList, "ShootOnce");
                                }
                            }
                        }
                    }
                }
                else if(iState==300)
                {

                }
                else if(iState==400)
                {

                }
                else if(iState==1000)
                {

                }
            }
            void findAttackTarget()
            {
                if (_Cameras.CameraForwardScan( 10000))
                {
                    if (_Cameras.lastDetectedInfo.IsEmpty())
                    { // hit nothing???
                        _program.Echo("No Target found to attack");
                    }
                    else
                    { // we found something.
                        if (IsValidAttackTarget(_Cameras.lastDetectedInfo))
                        {

                            _program.Echo("setting attack target");

                            targetDetectedInfo = _Cameras.lastDetectedInfo;
                            broadcastAttackCommand();
                            _wicoControl.SetMode(WicoControl.MODE_ATTACKDRONE);
                            _wicoControl.SetState(11);
                        }
                        else _program.Echo("Not Valid Attack Target");
                    }
                }
            }
            bool IsValidAttackTarget(MyDetectedEntityInfo thisDetectInfo)
            {
                if (thisDetectInfo.IsEmpty()) return false;

                if (thisDetectInfo.Type == MyDetectedEntityType.Asteroid) return false;
                if (thisDetectInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies) return true;
                if (thisDetectInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Owner) if (bFriendlyFire) return true; else return false;
                if (thisDetectInfo.Relationship == MyRelationsBetweenPlayerAndBlock.FactionShare) if (bFriendlyFire) return true; else return false;

                return true;
            }
            void broadcastAttackCommand()
            {
                if (!targetDetectedInfo.IsEmpty())
                {
                    if (targetDetectedInfo.Velocity.Length() < 1)
                    {
//                        antSend("WICO:ATTACKP:" + Vector3DToString(targetDetectedInfo.Position));
                    }
                    else
                    {
//                        antSend("WICO:ATTACKM:" + deiInfo(targetDetectedInfo));
                    }
                }
            }

        }
    }
}
