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

        // assumes 'back' connector.
        // needs to support:
        //  other sides for connectors
        // merge block
        // rotor head
        // rotor base.
        // combined connector and merge block (ex, tradeship)

        // opening hangar doors after connection
        // close hangar doors before de-connect


        #region docking

        /*
        state
        0 master inint
        100 init antenna power
        110 wait for slow speed
          choose base.  Send request.
        120 wait for reply from base
        125  timeout while waiting for a reply from a base; try to move closer to base ->110
        130 No known bases.  wait for reply

        //antSend("WICO:CON?:" + base.baseID, +":"+ "mini"+ ":"+ shipOrientationBlock.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +

        Use other connector position and vector for docking
        150	Move to 'wait' location (or current location) ?request 'wait' location? ->175 or ->200
        
        175 do travel to 'base' location  ->200

        200	request available docking connector

        210 wait for available
        250	when available, calculate approach locations
        300  Start:	Move through locations
        'Back' Connector:
        310 NAV move to Home Arrive->340

        340 Delay for motion
        350 slow move rest of way to Home. Arrival->400
        400 NAV move to Launch1
        410 slow move rest of way to Launch1 Arrival->430
        430 Arrived @Launch1 ->450 Reset docking distance check (future checks)
        450, 452 align to dock
            Aligned ->451 If no align, directly->500
        451 align to docking alignment align to dock 
            ->452
        500 'reverse' to dock, aiming our connector at target connector
                supports 'back' connector
                supports 'down' connector (kneeling required for wheeled vehicles?)
                supports 'forward' connector

                if error with align, etc, ->590
         590 abort dock.  Move away and try again.

        Always:	Lock connector iMode->MODE_DOCKED
            */

        DockableConnector targetConnector = new DockableConnector();
        IMyTerminalBlock dockingConnector;

        //int iDockingPushCount = 0;

        double dockingLastDistance = -1;

        List<IMyTerminalBlock> thrustDockBackwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustDockForwardList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustDockLeftList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustDockRightList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustDockUpList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> thrustDockDownList = new List<IMyTerminalBlock>();

        void doModeDocking()
        {
            StatusLog("clear", textPanelReport);
            StatusLog(moduleName + ":DOCKING!", textPanelReport);
//            StatusLog(moduleName + ":Docking: current_state=" + current_state, textPanelReport);
//            StatusLog(moduleName + ":Docking: current_state=" + current_state, textLongStatus, true);
 //           bWantFast = true;
            Echo("DOCKING: state=" + current_state);

            bWantSlow = true;

            IMySensorBlock sb;

            if (dockingConnector == null) current_state = 0;

//            sInitResults += "DOCKING: state=" + current_state+"\n";

            if (current_state == 0)
            {
//                sInitResults = "DOCKING: state=" + current_state+"\n";
                if (AnyConnectorIsConnected())
                {
                    setMode(MODE_DOCKED);
                    return;
                }
                dockingConnector = getDockingConnector();
                if (dockingConnector == null)// || getAvailableRemoteConnector(out targetConnector))
                {
                    Echo("No local connector for docking");
                    StatusLog(moduleName + ":No local Docking Connector Available!", textLongStatus, true);
                    // we could check for merge blocks.. or landing gears..
                    setMode(MODE_ATTENTION);
                    bWantFast = false;
                    return;
                }
                else
                {
                    ResetMotion();
                    turnDrillsOff();

                    thrustersInit(dockingConnector, ref thrustDockForwardList, ref  thrustDockBackwardList,
                        ref thrustDockDownList, ref thrustDockUpList,
                        ref thrustDockLeftList, ref thrustDockRightList);
                    current_state = 100;
                }
                lTargetBase = 0;// iTargetBase = -1;
            }
            Vector3D vPos = dockingConnector.GetPosition();
            if (!AnyConnectorIsConnected() && AnyConnectorIsLocked())
            {
                ConnectAnyConnectors();
                ResetMotion();
                setMode(MODE_DOCKED);
                powerDownThrusters(thrustAllList, thrustAll, true);
                return;
            }
            if (current_state == 100)
            {
                // TODO: allow for relay ships that are NOT bases..
                // TODO: if memory docking, don't need to adjust antenna
                // TODO: if stealth mode, don't mess with antenna
                float range = RangeToNearestBase() + 100f + (float)velocityShip * 5f;
                antennaMaxPower(false, range);
                if (sensorsList.Count > 0)
                {
                    sb = sensorsList[0];
                    //			setSensorShip(sb, 1, 1, 1, 1, 50, 1);
                }
                current_state = 110;
            }
            else if (current_state == 110)
            { // wait for slow
                if (velocityShip < 10)
                {
                    if (lTargetBase <= 0) lTargetBase = BaseFindBest();
                    //                    sInitResults += "110: Base=" + iTargetBase;
                    dtDockingActionStart = DateTime.Now;
                    if (lTargetBase > 0)
                    {
                        calculateGridBBPosition(dockingConnector);
                        Vector3D[] points = new Vector3D[4];
                        _obbf.GetFaceCorners(5, points); // 5 = front
                                                         // front output order is BL, BR, TL, TR
                        double width = (points[0] - points[1]).Length();
                        double height = (points[0] - points[2]).Length();
                        _obbf.GetFaceCorners(0, points);
                        // face 0=right output order is  BL, TL, BR, TR ???
                        double length = (points[0] - points[2]).Length();

                        string sMessage = "WICO:CON?:";
                        sMessage += lTargetBase.ToString() + ":";
                        //$"{height:N1},{width:N1},{length:N1}:";
                        sMessage += height.ToString("0.0") + "," + width.ToString("0.0") + "," + length.ToString("0.0") + ":";
                        //                    sMessage += shipDim.HeightInMeters() + "," + shipDim.WidthInMeters() + "," + shipDim.LengthInMeters() + ":";
                        sMessage += shipOrientationBlock.CubeGrid.CustomName + ":";
                        sMessage += SaveFile.EntityId.ToString() + ":";
                        sMessage += Vector3DToString(shipOrientationBlock.GetPosition());
                        antSend(sMessage);
                        //                        antSend("WICO:CON?:" + baseIdOf(iTargetBase).ToString() + ":" + "mini" + ":" + shipOrientationBlock.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                        current_state = 120;
                    }
                    else // No available base
                    {
                        // try to get a base to respond
                        checkBases(true);
                        current_state = 130;
                        //                        setMode(MODE_ATTENTION);
                    }
                }
                else
                    ResetMotion();
            }
            else if (current_state == 120)
            { // wait for reply from base
                StatusLog("Awaiting Response from Base", textPanelReport);

                bWantFast = false;
                DateTime dtMaxWait = dtDockingActionStart.AddSeconds(5.0f);
                DateTime dtNow = DateTime.Now;
                if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                {
                    current_state = 125;
                    return;
                }
                if (sReceivedMessage != "")
                {
                    Echo("Received Message=\n" + sReceivedMessage);
                    string[] aMessage = sReceivedMessage.Trim().Split(':');
                    Echo(aMessage.Length + ": Length");
                    for (int i = 0; i < aMessage.Length; i++)
                        Echo(i + ":" + aMessage[i]);
                    if (aMessage.Length > 1)
                    {
                        if (aMessage[0] != "WICO")
                        {
                            Echo("not wico system message");
                            return;
                        }
                        if (aMessage.Length > 2)
                        {
                            if (aMessage[1] == "CONA")
                            {
                                Echo("Approach answer!");
                                //antSend("WICO:CONA:" + droneId +":" + SaveFile.EntityId.ToString(), +":"+Vector3DToString(vApproachPosition))

                                long id = 0;
                                long.TryParse(aMessage[2], out id);
                                if (id == SaveFile.EntityId)
                                {
                                    // it's a message for us.
                                    sReceivedMessage = ""; // we processed it.
                                    long.TryParse(aMessage[3], out id);
                                    double x, y, z;
                                    int iOff = 4;
                                    x = Convert.ToDouble(aMessage[iOff++]);
                                    y = Convert.ToDouble(aMessage[iOff++]);
                                    z = Convert.ToDouble(aMessage[iOff++]);
                                    Vector3D vPosition = new Vector3D(x, y, z);

                                    vHome = vPosition;
                                    bValidHome = true;
                                    //                                        StatusLog("clear", gpsPanel);
                                    //                                        debugGPSOutput("Home", vHome);

                                    current_state = 150;
                                }
                            }
                            // TODO: need to process CONF
                        }
                    }
                }
                else
                { // uses timeout from above
                    Echo("Awaiting reply message");
                }
            }
            else if (current_state == 125)
            { // timeout waiting for reply from base..
                // move closer to the chosen base's last known position.
                if (lTargetBase <= 0)
                {
                    // TODO: remove base from list and try again.  ATTENTION if no remaining bases
                    setMode(MODE_ATTENTION);
                    return;
                }
                NavGoTarget(BasePositionOf(lTargetBase), iMode, 110, 3100, "DOCK Base Proximity");
                //                doTravelMovement(BasePositionOf(lTargetBase), 3100, 110, 106);
            }
            else if (current_state == 130)
            {
                // no known bases. requested response. wait for a while to see if we get one
                StatusLog("Trying to find a base", textPanelReport);
                bWantFast = false;
                DateTime dtMaxWait = dtDockingActionStart.AddSeconds(2.0f);
                DateTime dtNow = DateTime.Now;
                if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                {
                    setMode(MODE_ATTENTION);
                    return;
                }
                if (BaseFindBest() >= 0)
                    current_state = 110;
            }

            else if (current_state == 150)
            { //150	Move to 'approach' location (or current location) ?request 'wait' location?
                current_state = 175;
                /*
                if (bValidHome)
                {
                    double distancesqHome = Vector3D.DistanceSquared(vHome, shipOrientationBlock.GetPosition());
                    if (distancesqHome > 25000) // max SG antenna range //TODO: get max from antenna module
                    {
                        current_state = 175;
                    }
                    else current_state = 200;
                }
                else current_state = 200;
                */
            }
            else if (current_state == 175)
            { // get closer to approach location
                NavGoTarget(vHome, iMode, 200, 5, "DOCK Base Approach");
            }
            else if (current_state == 200)
            {//200	Arrived at approach location
                // request available docking connector
                StatusLog("Requsting Docking Connector", textPanelReport);
                if (velocityShip < 1)
                {

                    calculateGridBBPosition(dockingConnector);
                    Vector3D[] points = new Vector3D[4];
                    _obbf.GetFaceCorners(5, points); // 5 = front
                                                     // front output order is BL, BR, TL, TR
                    double width = (points[0] - points[1]).Length();
                    double height = (points[0] - points[2]).Length();
                    _obbf.GetFaceCorners(0, points);
                    // face 0=right output order is  BL, TL, BR, TR ???
                    double length = (points[0] - points[2]).Length();

                    string sMessage = "WICO:COND?:";
                    sMessage += lTargetBase.ToString() + ":";
                    sMessage += height.ToString("0.0") + "," + width.ToString("0.0") + "," + length.ToString("0.0") + ":";
                    //                    sMessage += shipDim.HeightInMeters() + "," + shipDim.WidthInMeters() + "," + shipDim.LengthInMeters() + ":";
                    sMessage += shipOrientationBlock.CubeGrid.CustomName + ":";
                    sMessage += SaveFile.EntityId.ToString() + ":";
                    sMessage += Vector3DToString(shipOrientationBlock.GetPosition());
                    antSend(sMessage);

                    //                    antSend("WICO:COND?:" + baseIdOf(iTargetBase) + ":" + "mini" + ":" + shipOrientationBlock.CubeGrid.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(shipOrientationBlock.GetPosition()));
                    {
                        dtDockingActionStart = DateTime.Now;
                        current_state = 210;
                    }
                }
                else ResetMotion();
            }
            else if (current_state == 210)
            { //210	wait for available connector
                StatusLog("Awating reply with Docking Connector", textPanelReport);
                bWantFast = false;
                DateTime dtMaxWait = dtDockingActionStart.AddSeconds(5.0f);
                DateTime dtNow = DateTime.Now;
                if (DateTime.Compare(dtNow, dtMaxWait) > 0)
                {
                    current_state = 100;
                    return;
                }
                if (getAvailableRemoteConnector(out targetConnector))
                {
                    current_state = 250;
                }
                else
                {
                    if (sReceivedMessage != "")
                    {
                        Echo("Received Message=\n" + sReceivedMessage);
                        string[] aMessage = sReceivedMessage.Trim().Split(':');
                        Echo(aMessage.Length + ": Length");
                        for (int i = 0; i < aMessage.Length; i++)
                            Echo(i + ":" + aMessage[i]);
                        if (aMessage.Length > 1)
                        {
                            if (aMessage[0] != "WICO")
                            {
                                Echo("not wico system message");
                                return;
                            }
                            if (aMessage.Length > 2)
                            {
                                //                                if (aMessage[1] == "DOCK" || aMessage[1] == "ADOCK")
                                if (aMessage[1] == "COND" || aMessage[1] == "ACOND")
                                {
                                    Echo("Docking answer!");
                                    // FORMAT:	antSend("WICO:DOCK:" + aMessage[3] + ":" + connector.EntityId + ":" + connector.CustomName + ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec));
                                    //	antSend("WICO:ADOCK:" + incomingID + ":" + connector.EntityId + ":" + connector.CustomName 	+ ":" + Vector3DToString(vPosition) + ":" + Vector3DToString(vVec)+":" + Vector3DToString(vAlign));

                                    long id = 0;
                                    long.TryParse(aMessage[2], out id);
                                    if (id == SaveFile.EntityId)
                                    {
                                        // it's a message for us.
                                        sReceivedMessage = ""; // we processed it.
                                        long.TryParse(aMessage[3], out id);
                                        string sName = aMessage[4];
                                        double x, y, z;
                                        int iOff = 5;
                                        x = Convert.ToDouble(aMessage[iOff++]);
                                        y = Convert.ToDouble(aMessage[iOff++]);
                                        z = Convert.ToDouble(aMessage[iOff++]);
                                        Vector3D vPosition = new Vector3D(x, y, z);

                                        x = Convert.ToDouble(aMessage[iOff++]);
                                        y = Convert.ToDouble(aMessage[iOff++]);
                                        z = Convert.ToDouble(aMessage[iOff++]);
                                        Vector3D vVec = new Vector3D(x, y, z);

                                        if (aMessage[1] == "ACOND")
                                        {
                                            x = Convert.ToDouble(aMessage[iOff++]);
                                            y = Convert.ToDouble(aMessage[iOff++]);
                                            z = Convert.ToDouble(aMessage[iOff++]);
                                            vDockAlign = new Vector3D(x, y, z);
                                            bDoDockAlign = true;
                                        }
                                        vDock = vPosition;
                                        vLaunch1 = vDock + vVec *(shipDim.LengthInMeters() * 1.5);
                                        vHome = vDock + vVec * (shipDim.LengthInMeters() * 3);
                                        bValidDock = true;
                                        bValidLaunch1 = true;
                                        bValidHome = true;
                                        StatusLog("clear", gpsPanel);
                                        debugGPSOutput("dock", vDock);
                                        debugGPSOutput("launch1", vLaunch1);
                                        debugGPSOutput("Home", vHome);

                                        current_state = 300;

                                    }
                                }
                                // TODO handle CONF
                            }
                        }

                    }
                    else
                    { // uses timeout from above
                        Echo("Awaiting reply message");
                    }
                }
            }
            else if (current_state == 250)
            { //250	when available, calculate approach locations from a saved targetconnector

                vDock = targetConnector.vPosition;
                vLaunch1 = vDock + targetConnector.vVector * (shipDim.LengthInMeters() * 1.5);
                vHome = vDock + targetConnector.vVector * (shipDim.LengthInMeters() * 3);
                bValidDock = true;
                bValidLaunch1 = true;
                bValidHome = true;
                current_state = 300;
                StatusLog("clear", gpsPanel);
                debugGPSOutput("dock", vDock);
                debugGPSOutput("launch1", vLaunch1);
                debugGPSOutput("Home", vHome);
                MoveForwardSlowReset();
                bWantFast = true;
            }
            else if (current_state == 300)
            { //300  Start:	Move through locations
                current_state = 310;
                MoveForwardSlowReset();
//                iDockingPushCount = 0;
                bWantFast = true;
            }
            else if (current_state == 310)
            { //	310 move to home
                Echo("Moving to Home");
                //		if(iPushCount<60) iPushCount++;
                //		else

                NavGoTarget(vHome, iMode, 340, 3, "DOCK Approach");
                //               doTravelMovement(vHome, 3.0f, 350, 161);
            }
            else if (current_state == 340)
            { // arrived at 'home' from NAV
                ResetMotion();
                Echo("Waiting for ship to stop");
                turnEjectorsOff();
                MoveForwardSlowReset();
//                iDockingPushCount = 0;
                if (velocityShip < 0.1f)
                {
                    bWantFast = true;
                    current_state = 350;
                }
                else
                {
                    bWantMedium = true;
                    //                    bWantFast = false;
                }
            }
            else if (current_state == 350)
            {
                // move connector closer to home
                double distanceSQ = (vHome - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
                Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                if (distanceSQ > shipDim.BlockMultiplier() * 3)
                {
                    MoveForwardSlow(3, 5, thrustForwardList, thrustBackwardList);
                    bWantMedium = true;
                }
                else
                {
                    ResetMotion();
                    MoveForwardSlowReset();
                    current_state = 400;
                    bWantFast = true;
                }
            }
            else if(current_state==400)
            {
                // move to Launch1
                Echo("Moving to Launch1");

                NavGoTarget(vLaunch1, iMode, 410, 3,"DOCK Connector Entry");
            }
            else if(current_state==410)
            {
                // move closer to Launch1
                double distanceSQ = (vLaunch1 - ((IMyShipController)shipOrientationBlock).CenterOfMass).LengthSquared();
                Echo("DistanceSQ=" + distanceSQ.ToString("0.0"));
                double stoppingDistance = calculateStoppingDistance(thrustBackwardList, velocityShip, 0);
                if (distanceSQ > shipDim.BlockMultiplier() * 3)
                {
                    MoveForwardSlow(3, 5, thrustForwardList, thrustBackwardList);
                    bWantMedium = true;
                }
                else
                {
                    ResetMotion();
                    MoveForwardSlowReset();
                    current_state = 430;
                    bWantFast = true;
                }
            }
            else if(current_state==430)
            {
                // arrived at launch1
                bWantFast = true;
                dockingLastDistance = -1;
                current_state = 450;
            }
            else if (current_state == 450 || current_state == 452)
            { //450 452 'reverse' to dock, aiming connector at dock location
              // align to docking alignment if needed
                StatusLog("Align Up to Docking Connector", textPanelReport);
                bWantFast = true;
                //                turnEjectorsOff();
                if (!bDoDockAlign)
                {
                    current_state = 500;
                    return;
                }
                Echo("Aligning to dock");
                bool bAimed = false;
                minAngleRad = 0.03f;

                // TODO: need to change direction if non- 'back' connector
                bAimed = GyroMain("up", vDockAlign, shipOrientationBlock);
                bWantFast = true;
                if (current_state == 452) current_state = 500;
                else if (bAimed) current_state++; // 450->451 
            }
            else if (current_state == 451)
            { //451 align to dock
                StatusLog("Align to Docking Connector", textPanelReport);
                bWantFast = true;
                Vector3D vTargetLocation = vDock;
                Vector3D vVec = vTargetLocation - dockingConnector.GetPosition();

                if (!bDoDockAlign)
                    current_state = 452;

                //		Vector3D vTargetLocation = shipOrientationBlock.GetPosition() +vDockAlign;
                //		Vector3D vVec = vTargetLocation - shipOrientationBlock.GetPosition();
                Echo("Aligning to dock");
                bool bAimed = false;
                minAngleRad = 0.03f;
                bAimed = GyroMain("forward", vVec, dockingConnector);
                if (bAimed) current_state = 452;
                else bWantFast = true;

            }
            else if (current_state == 500)
            { //500 'reverse' to dock, aiming connector at dock location (really it's connector-forward)
              // TODO: needs a time-out for when misaligned or base connector moves.
              //               bWantFast = true;
                StatusLog("Reversing to Docking Connector", textPanelReport);
                Echo("bDoDockAlign=" + bDoDockAlign);
                //                StatusLog(moduleName + ":Docking: Reversing to dock! Velocity=" + velocityShip.ToString("0.00"), textPanelReport);
                Echo("Reversing to Dock");
                CTRL_COEFF = 0.75;
                minAngleRad = 0.01f;

                Vector3D vTargetLocation = vDock;
                Vector3D vVec = vTargetLocation - dockingConnector.GetPosition();
                double distance = vVec.Length();
                Echo("distance=" + niceDoubleMeters(distance));
                Echo("velocity=" + velocityShip.ToString("0.00"));
                StatusLog("Distance=" + niceDoubleMeters(distance), textPanelReport);
                StatusLog("Velocity=" + niceDoubleMeters(velocityShip) + "/s", textPanelReport);

                if (dockingLastDistance < 0) dockingLastDistance = distance;
                if(dockingLastDistance<distance)
                {
                    // we are farther away than last time... something is wrong..
//                    sStartupError += "\nLast=" + niceDoubleMeters(dockingLastDistance) + " Cur=" + niceDoubleMeters(distance);
                    current_state = 590;
                }
                if (distance > 10)
                    minAngleRad = 0.03f;
                else
                    minAngleRad = 0.05f;

                //                debugGPSOutput("DockLocation", vTargetLocation);

                bool bAimed = false;
                /*
                        if ((craft_operation & CRAFT_MODE_SLED) > 0)
                        {
                            double yawangle = CalculateYaw(vTargetLocation, dockingConnector);
                            DoRotate(yawangle, "Yaw");
                            if (Math.Abs(yawangle) < .05) bAimed = true;
                        }
                        else
                */
                if (distance > 15)
                    bAimed = BeamRider(vLaunch1, vDock, dockingConnector);
                else
                    bAimed = GyroMain("forward", vVec, dockingConnector);

                /*
                double maxThrust = calculateMaxThrust(thrustDockForwardList);
                MyShipMass myMass;
                myMass = ((IMyShipController)shipOrientationBlock).CalculateShipMass();
                double effectiveMass = myMass.PhysicalMass;
                double maxDeltaV = (maxThrust) / effectiveMass;
                if (iDockingPushCount < 1)
                {
                    if (maxDeltaV < 2)
                        iDockingPushCount = 75;
                    else if (maxDeltaV < 5)
                        iDockingPushCount = 25;
                }
                */
                //               Echo("dockingPushCount=" + iDockingPushCount);
                // TODO: if we aren't moving and dockingpushcount>100, then we need to wiggle.

                if (bAimed)
                {
                    // we are aimed at location
                    Echo("Aimed");
                    if (distance > 15)
                    {
                        bWantMedium = true;
                        Echo(">15");
                        MoveForwardSlow(5, 10, thrustDockForwardList, thrustDockBackwardList);
                        /*
                        if (velocityShip < .5)
                        {
                            iDockingPushCount++;
                            powerUpThrusters(thrustDockForwardList, 25 + iDockingPushCount);
                        }
                        else if (velocityShip < 5)
                        {
                            powerUpThrusters(thrustDockForwardList, 1);
                        }
                        else
                            powerDownThrusters(thrustAllList);
                            */
                    }
                    else
                    {
                        Echo("<=15");
                        bWantFast = true;
                        MoveForwardSlow(.5f, 1.5f, thrustDockForwardList, thrustDockBackwardList);
                        /*
                        if (velocityShip < .5)
                        {
                            iDockingPushCount++;
                            powerUpThrusters(thrustDockForwardList, 25 + iDockingPushCount);
                        }
                        else if (velocityShip < 1.4)
                        {
                            powerUpThrusters(thrustDockForwardList, 1);
                            if (iDockingPushCount > 0) iDockingPushCount--;
                        }
                        else
                            powerDownThrusters(thrustAllList);
                            */
                    }
                }
                else
                {
                    Echo("Aiming");
                    powerDownThrusters(thrustAllList);
                    bWantFast = true;
                }
            }
            else if(current_state==590)
            {
                // abort dock and try again
                ResetMotion();
                Vector3D vVec = vDock - dockingConnector.GetPosition();
                double distance = vVec.Length();
                if (distance > shipDim.LengthInMeters() * 1.25)
                {
                    // we are far enough away.  Try again
                    current_state = 0;
                    bWantFast = true;
                    return;
                }
                bool bAimed = GyroMain("forward", vVec, dockingConnector);
                if (!bAimed) bWantFast = true;
                else bWantMedium = true;
                MoveForwardSlow(5, 10, thrustDockBackwardList, thrustDockForwardList);
            }
        }
        #endregion


    }
}