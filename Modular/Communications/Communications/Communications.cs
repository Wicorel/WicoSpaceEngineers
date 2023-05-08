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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {

        public class Communications : Antennas
        {
            /*
            Manage local laser antennas
            handle connection requests over IGC.
            Say no when connected to 'other' within 5000 (settable) meters.Give location of 'other' in reply
            Turn off when not in use
            Control power/range
            handle 'low power' requests.
            handle setting regular antenna range to minimum needed
            heartbeat to verify connection
            disconnect when get within 5000 (settable) meters and switch to antenna communication
            handle lost connections (obscured, range issues, etc)
            Support relay ships. laser (and radio antenna?)



            power off laser antennas when this ship is docked
           

            Need role of this grid
                Drone
                Fixed base (no thrusters)
                Moveable base
                Relay ship

            Has Laser antennas

*/
            // need list of communication targets

            // they should tell us location.  and if they have laser


            WicoElapsedTime _wicoElapsedTime;
            WicoIGC _wicoIGC;
            Displays _displays;

            const string CommunicationsDisplayTag = "COMMUNICATIONS";
            const string RemoteShipsDisplayTag = "REMOTESHIPS";
            const string RelayShipsDisplayTag = "RELAYSHIPS";
            
            public string sRemoteAnnounce = "COM_IAMHERE";
            public string sComIGCTag = "COM?";
            public string sLaserConRequestIGCTag = "LASERCON?";
            public string sLaserIGCTag = "LASERCON";
            public string sRelayRequestTag = "RELAY?";

            double AnnounceSeconds = 10;
            const string COMIFFTIMER = "IGCIFFTIMER";
            long _EntityId;

            /// <summary>
            /// The area of control for radio for this grid
            /// </summary>
            double AreaOfControl = 500000;

            // TODO: Maybe role needs to be bitflags instead of enum...
            enum GridRole { unknown, SilentShip, RadioOnlyShip, LaserOnlyShip, LaserRadioShip, MultipleLaserShip, MultipleLaserRadioShip, SilentBase, RadioOnlyBase, LaserOnlyBase, LaserRadioBase };

            bool bDefaultsSet = false;
            /// <summary>
            /// are we serving everything in our area of control?
            /// </summary>
            bool bAreaController = true;
            /// <summary>
            /// Do we function as a relay for messages?
            /// </summary>
            bool bRelay = true;

            GridRole _gridRole;

            public Communications(Program program, WicoBlockMaster wicoBlockMaster, WicoElapsedTime wicoElapsedTime, WicoIGC wicoIGC, Displays displays
                ): base(program,wicoBlockMaster)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;
                _wicoElapsedTime = wicoElapsedTime;
                _wicoIGC = wicoIGC;
                _displays = displays;

                _EntityId = program.Me.CubeGrid.EntityId;

                _program.moduleName += " Communications";
                _program.moduleList += "\nCommunications V4.2o";

                _program.AddUpdateHandler(UpdateHandler);
                _program.AddTriggerHandler(ProcessTrigger);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddPostInitHandler(PostInitHandler());

                _program.AddMainHandler(MainHandler);

                _wicoIGC.AddPublicHandler(sRemoteAnnounce, BroadcastHandler);
                _wicoIGC.AddPublicHandler(sComIGCTag, BroadcastHandler);
                _wicoIGC.AddPublicHandler(sRelayRequestTag, BroadcastHandler);
                //                _wicoIGC.AddPublicHandler(sLaserConRequestIGCTag, BroadcastHandler);

                _wicoIGC.AddUnicastHandler( BroadcastHandler); // sLaserIGCTag (and all the others..)

                _displays.AddSurfaceHandler(CommunicationsDisplayTag, SurfaceHandler);
                _displays.AddSurfaceHandler(RemoteShipsDisplayTag, SurfaceHandler);
                _displays.AddSurfaceHandler(RelayShipsDisplayTag, SurfaceHandler);

                AnnounceSeconds = _program.CustomDataIni.Get(_program.OurName, "WicoComAnnounceSeconds").ToDouble(AnnounceSeconds);
                _program.CustomDataIni.Set(_program.OurName, "WicoComAnnounceSeconds", AnnounceSeconds);

                if(_program.CustomDataIni.ContainsKey(_program.OurName, "AreaController"))
                {
                    bAreaController = _program.CustomDataIni.Get(_program.OurName, "AreaController").ToBoolean(bAreaController);
                    bRelay = _program.CustomDataIni.Get(_program.OurName, "Relay").ToBoolean(bRelay);
                    bDefaultsSet = true;
                }

                if (AnnounceSeconds > 0)
                {
                    wicoElapsedTime.AddTimer(COMIFFTIMER, AnnounceSeconds, ElapsedTimehandler);
                    wicoElapsedTime.StartTimer(COMIFFTIMER);
                }

            }
            public void ElapsedTimehandler(string s)
            {
                // our timer has expired
                if (s == COMIFFTIMER)
                {
                    Announce();
                }
            }
            StringBuilder sbMessages=new StringBuilder(200);
            public void Announce(long targetID=0)
            {
                //               _program.ErrorLog("Announce("+targetID.ToString()+")");
                // send Remoteship info
                IMyShipController ship = _wicoBlockMaster.GetMainController();
                IMyTerminalBlock tb = ship;
                if (tb == null)
                    tb = _program.Me;

                sbMessages.Clear();
                sbMessages.AppendLine(_program.Me.CubeGrid.EntityId.ToString());
                sbMessages.AppendLine(_wicoBlockMaster.GetShipName().ToString());
                sbMessages.AppendLine(_program.Vector3DToString(tb.GetPosition()));
                sbMessages.AppendLine(_program.Vector3DToString(_wicoBlockMaster.GetShipVelocity()));
                sbMessages.AppendLine(((int)_gridRole).ToString());
                sbMessages.AppendLine(bAreaController.ToString());
                sbMessages.AppendLine(bRelay.ToString());

                sbMessages.AppendLine(laserList.Count.ToString());
                foreach(var laser in laserList)
                {
                    sbMessages.AppendLine(laser.CustomName);
                    sbMessages.AppendLine(_program.Vector3DToString(laser.GetPosition()));
                }

                string message;
                message = sbMessages.ToString();
                if(targetID>0)
                {
                    _program.IGC.SendUnicastMessage(targetID, sRemoteAnnounce, message);
                }
                else _program.IGC.SendBroadcastMessage(sRemoteAnnounce, message);
            }

            int numberLocalThrust = 0; // 
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            new void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyThrust)
                {
                    // note: This will also count 'cutter' thrusters
                    numberLocalThrust++;
                }
            }
            void LocalGridChangedHandler()
            {
                numberLocalThrust = 0;
            }

            StringBuilder sbComNotices = new StringBuilder(200);
            StringBuilder sbComInfo = new StringBuilder(200);
            StringBuilder sbRSNotices = new StringBuilder(200);
            StringBuilder sbRSInfo = new StringBuilder(200);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == CommunicationsDisplayTag)
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        if (tsurface.SurfaceSize.Y < 256)
                        { // small/corner LCD
                            tsurface.WriteText(sbComInfo);
                        }
                        else
                        {
                            tsurface.WriteText(sbComInfo);
                            tsurface.WriteText(sbComNotices, true);
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 256)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 3;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 2f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
                else if (tag == RemoteShipsDisplayTag)
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        if (tsurface.SurfaceSize.Y < 256)
                        { // small/corner LCD
                            tsurface.WriteText(sbRSInfo);
                        }
                        else
                        {
                            tsurface.WriteText(sbRSInfo);
                            tsurface.WriteText(sbRSNotices, true);
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        //                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 256)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 3.5f;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.75f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
                /* WIP
                else if (tag == RelayShipsDisplayTag)
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        if (tsurface.SurfaceSize.Y < 256)
                        { // small/corner LCD
                            tsurface.WriteText(sbRSInfo);
                        }
                        else
                        {
                            tsurface.WriteText(sbRSInfo);
                            tsurface.WriteText(sbRSNotices, true);
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        //                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 256)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 3.5f;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.75f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
                */
            }

            void LoadHandler(MyIni Ini)
            {
            }

            void SaveHandler(MyIni Ini)
            {
            }
            public IEnumerator<bool> PostInitHandler()
            {
                if(numberLocalThrust>0)
                {
                    // we have thrusters
                    if(laserList.Count<1)
                    {
                        // no lasers
                        if (antennaList.Count < 1)
                        {
                            // no antenna
                            _gridRole = GridRole.SilentShip;
                            if(!bDefaultsSet)
                            {
                                // can't do these without working antenna
                                bRelay = false;
                                bAreaController = false;
                            }
                        }
                        else _gridRole = GridRole.RadioOnlyShip;
                    }
                    else
                    { // we have some laser
                        if(laserList.Count >1)
                        { // possibility of relay ship
                            if (antennaList.Count < 1) _gridRole = GridRole.MultipleLaserShip; // No radio; can't serve as local radio relay
                            else _gridRole = GridRole.MultipleLaserRadioShip;  
                        }
                        else if(laserList.Count>0)
                        {
                            // we have one laser only
                            if (antennaList.Count < 1) _gridRole = GridRole.LaserOnlyShip;
                            else _gridRole = GridRole.LaserRadioShip;
                        }
                    }

                }
                else
                {
                    // No Thrusters
                    if (laserList.Count < 1)
                    {
                        // no lasers
                        if (antennaList.Count < 1)
                        {
                            _gridRole = GridRole.SilentBase;
                            if (!bDefaultsSet)
                            {
                                // can't do these without working antenna
                                bRelay = false;
                                bAreaController = false;
                            }
                        }
                        else _gridRole = GridRole.RadioOnlyBase;
                    }
                    else
                    { // we have some laser
                        if (laserList.Count > 1)
                        { // possibility of laser relay ship; multiple lasers
                            if (antennaList.Count < 1)
                            {
                                _gridRole = GridRole.LaserOnlyBase; // No radio; can't serve as local radio relay
                                if (!bDefaultsSet)
                                {
                                    // can't do this without working radio
                                    bAreaController = false;
                                }
                            }
                            else _gridRole = GridRole.LaserRadioBase;
                        }
                        else if (laserList.Count > 0)
                        {
                            // we have one laser only
                            if (antennaList.Count < 1)
                            {
                                // no radio; so we are laser receive only for relay
                                if (!bDefaultsSet)
                                {
                                    // can't do these without working antenna
                                    bRelay = false;
                                    bAreaController = false;
                                }
                                _gridRole = GridRole.LaserOnlyBase;
                            }
                            else
                            {
                                // One laser. and at least one radio
                                _gridRole = GridRole.LaserRadioBase;
                            }
                            
                        }
                    }
                }
                if (!bDefaultsSet)
                {
                    _program.CustomDataIni.Set(_program.OurName, "AreaController", bAreaController);
                    _program.CustomDataIni.Set(_program.OurName, "Relay", bRelay);
                    bDefaultsSet = true;
                }
                _program.CustomDataChanged();
                Announce();

                // request that relays immediatly announce themselves to us (everybody)
                _program.IGC.SendBroadcastMessage(sRelayRequestTag, "");

                yield return true;
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
                    if (myCommandLine.Argument(0) == "godock")
                    {
                    }
                }
            }

            void UpdateHandler(UpdateType updateSource)
            {
                // TODO: Change to use elapsedtime

                if ((updateSource & UpdateType.Update100) > 0)
                {

                    sbRSInfo.Clear();
                    sbRSNotices.Clear();

                    float farthestShip = 0;
                    float nearestAreaController = float.MaxValue;
                    string interestingName = "";
                    bool bAreaControllerFound = false;


//                    if (bAreaController) sbRSInfo.AppendLine("I am area controller");
//                    if (bRelay) sbRSInfo.AppendLine("I am a relay");
//                    sbRSInfo.AppendLine("Remote Ships");

                    foreach (var remoteship in RemoteShips)
                    {
                        // TODO: If we have lasers and a laser connection, we can reduce local radio antenna range to just things in our area.
                        // TODO: allow ships to go into 'silent' area between two radio areaofcontrol stations.
                        // TODO: maybe hand-off management of a ship as it leaves 'area of control'?
                        // TODO: ships area of control need to register so we can track them (and ignore others)
                        // TODO: relay ships need to register with each other to determine area of control..
                        // TODO: Need to know if a relay ship needs radio connection from us or can/is using laser


                        if (remoteship.ageMs < 10 * 1000)
                        {
                            // info is still valid
                            Vector3D shipPosition = remoteship.Position;
                            double estimatedrange = 0;
                            if(!remoteship.Velocity.IsZero())
                            {
                                Vector3D estimatedShipPosition=shipPosition + remoteship.Velocity * 2*AnnounceSeconds;
                                estimatedrange = (_wicoBlockMaster.CenterOfMass() - estimatedShipPosition).Length();
                            }
                            double range = (_wicoBlockMaster.CenterOfMass() - shipPosition).Length();
                            if (estimatedrange > range)
                                range = estimatedrange;
                            if (range > float.MaxValue) continue;

                            if(bAreaController)
                            {
                                // *I* am an area controller
                                if (range > farthestShip && range <= AreaOfControl)
                                {
                                    farthestShip = (float)range;
                                    interestingName = remoteship.sName;
                                }
                            }
                            else
                            {
                                // I'm not responsible for ships in my area
                                // but I do need to find the nearest area controller and make sure to maintain connection to it
                                if (remoteship.IsAreaController)
                                {
                                    bAreaControllerFound = true;
                                    if (range < nearestAreaController)
                                    {
                                        nearestAreaController = (float)range;
                                        interestingName = remoteship.sName;
                                    }
                                }
                            }
                        }
                    }
                    if(RemoteShips.Count>0)
                    {
                        if (bAreaController)
                        {
                            fAntennaDesiredRange = farthestShip + 420;
                            if (fAntennaDesiredRange > nearestAreaController)
                                fAntennaDesiredRange = nearestAreaController + 1000;
                        }
                        else
                        {
                            fAntennaDesiredRange = nearestAreaController + 200 + (float)(_wicoBlockMaster.GetShipSpeed()* AnnounceSeconds);
                        }

                    } // else leave range alone.
                    if(!bAreaController)
                    {
                        // I am not an area controller

                        if(!bAreaControllerFound)
                        {
                            sbRSInfo.AppendLine("No local area controller");
                        }
                        else
                        {
                            sbRSInfo.AppendLine("Closest Area Controller");
                            sbRSInfo.AppendLine(" "+ interestingName);
                            sbRSInfo.AppendLine(" " + _program.niceDoubleMeters(nearestAreaController));
                        }
                    }
                    else
                    {
                        // I AM an area controller
                        if(RemoteShips.Count>0)
                        {
                            sbRSInfo.AppendLine("Farthest Ship");
                            sbRSInfo.AppendLine(" "+ interestingName);
                            sbRSInfo.AppendLine(" " + _program.niceDoubleMeters(farthestShip));
                        }
                    }
                    //                    sbRSNotices.AppendLine("# ships=" + RemoteShips.Count);
                    sbRSNotices.AppendLine("");

                    foreach (var remoteship in RemoteShips)
                    {
                        string sAge = " ";
                        if (remoteship.ageMs > 2 * 1000)
                        {
                            if (remoteship.ageMs > 10 * 1000)
                                sAge = "!";
                            else sAge = "*";
                        }
                        if (remoteship.IsAreaController)
                            sAge += "A";
                        else sAge += " ";
                        if (remoteship.IsRelay)
                            sAge += "R";
                        else sAge += " ";
                        sbRSNotices.AppendLine(sAge + remoteship.sName);
                        sbRSNotices.AppendLine(" " + _program.niceDoubleMeters((_wicoBlockMaster.CenterOfMass() - remoteship.Position).Length()));
                    }

                    foreach (var laser in laserList)
                    {
                        if (laser.Status == MyLaserAntennaStatus.Idle)
                        {
                            // check status and turn them off if not neeed
//                            laser.Enabled = false;
// allow user to hand-link laser. means it needs to be left on

                        }

                    }
                    IMyRadioAntenna bestAntenna = null;
                    foreach (var ant in antennaList)
                    {
                        // we only need to keep one of them on.
                        if (bestAntenna == null)
                        {
                            if(ant.IsFunctional)
                                bestAntenna = ant;
                        }
                        else
                        {
                            if (ant.IsFunctional)
                            {
                                if(ant.Radius>bestAntenna.Radius)
                                {
                                    bestAntenna = ant;
                                }
                            }
                        }
                    }
                    if(bestAntenna!=null)
                    { 

                        if(RemoteShips.Count > 0)
                        {
                            bestAntenna.Radius = fAntennaDesiredRange;
                        }
                        foreach (var ant in antennaList)
                        {
                            if (ant != bestAntenna)
                            {
                                if(ant.Enabled)
                                    ant.Enabled = false;
                            }
                            else
                            {
                                if (bestAntenna.Enabled != true)
                                    bestAntenna.Enabled = true;
                                sbComInfo.AppendLine("Best Ant=" + bestAntenna.CustomName);
                            }
                        }
                    }

                    // TODO: Seperate from antenna range update on seperate elapsed timer
                    sbComInfo.Clear();
                    sbComNotices.Clear();
                    sbComInfo.AppendLine("Communications");
//                    sbComInfo.AppendLine("Ship Role=" + _gridRole.ToString());
                    if (bAreaController) sbComNotices.AppendLine("I am area controller");
                    if (bRelay) sbComNotices.AppendLine("I am a relay");

                    sbComNotices.AppendLine(antennaList.Count + " Radio Antennas");
                    sbComNotices.AppendLine(laserList.Count + " Laser Antennas");
                    foreach (var radio in antennaList)
                    {
                        sbComInfo.AppendLine(radio.CustomName + " (" + radio.Radius.ToString("N0") + "M)");
                    }
                    foreach (var laser in laserList)
                    {
                        sbComInfo.AppendLine(laser.CustomName + " (" + laser.Range.ToString("N0") + "M)");
                    }
//                    sbComNotices.AppendLine("# Thrusters=" + numberLocalThrust);

                }
            }

            void BroadcastHandler(MyIGCMessage msg) // also used as unicast handler
            {
                // NOTE: called on ALL received messages; not just 'our' tag
//                _program.ErrorLog("IGC Received" + msg.Tag);

                if (msg.Tag == sComIGCTag)
                {
                    // TODO: define this... currently TBD
                    _program.Echo("Com Request");
                    string sMessage = (string)msg.Data;
                    string[] aMessage = sMessage.Trim().Split('\n');
                    long incomingID = 0;
                    bool pOK = false;
                    pOK = long.TryParse(aMessage[0], out incomingID);
                }
                else if (msg.Tag == sRelayRequestTag)
                {
                    _program.Echo("Relay Request");
                    if (bRelay)
                        Announce(msg.Source);
                }
                else if (msg.Tag==sRemoteAnnounce)
                {
//                    _program.Echo("RemoteAnnounce");
                    string sMessage = (string)msg.Data;
                    string[] aMessage = sMessage.Trim().Split('\n');

                    long Shipid;
                    string ShipName;
                    Vector3D ShipPosition;
                    Vector3D Velocity;
                    GridRole role;
                    int LaserAntennaCount;

                    int iTemp;

                    int iLine = 0;

                    long.TryParse(aMessage[iLine++], out Shipid);
                    ShipName = aMessage[iLine++];
//                    _program.Echo(" From" + ShipName);
 //                   _program.ErrorLog(" " + ShipName);

                    ShipPosition = _program.StringToVector3D(aMessage[iLine++]);

                    Velocity= _program.StringToVector3D(aMessage[iLine++]);

                    int.TryParse(aMessage[iLine++], out iTemp);
                    role = (GridRole)iTemp;

                    bool IsAreaController;
                    bool.TryParse(aMessage[iLine++], out IsAreaController);
                    bool IsRelay;
                    bool.TryParse(aMessage[iLine++], out IsRelay);


                    int.TryParse(aMessage[iLine++], out LaserAntennaCount);

                    List<RemoteLaser> remoteLasers = new List<RemoteLaser>();

                    for(int i=0;i<LaserAntennaCount;i++)
                    {
                        string LaserName = aMessage[iLine++];
                        Vector3D LaserPosition = _program.StringToVector3D(aMessage[iLine++]);
                        RemoteLaser remote = new RemoteLaser();
                        remote.Name = LaserName;
                        remote.Position = LaserPosition;
                        remoteLasers.Add(remote);
                    }

                    bool bFound = false;
                    foreach(var remoteShip in RemoteShips)
                    {
                        if(remoteShip.ShipID == Shipid)
                        {
                            // update
//                            _program.ErrorLog("Updating " + remoteShip.sName);
                            remoteShip.sName = ShipName;
                            remoteShip.Position = ShipPosition;
                            remoteShip.Velocity = Velocity;
                            remoteShip.role = role;
                            remoteShip.IsAreaController = IsAreaController;
                            remoteShip.IsRelay = IsRelay;
                            remoteShip.NumberLasers = LaserAntennaCount;
                            remoteShip.RemoteLasers = remoteLasers;
                            remoteShip.ageMs = 0;
                            bFound = true;
                            break;
                        }

                    }
                    if(!bFound)
                    {
                        RemoteShip remoteShip = new RemoteShip();
                        remoteShip.ShipID = Shipid;
                        remoteShip.sName = ShipName;
                        remoteShip.Position = ShipPosition;
                        remoteShip.Velocity = Velocity;
                        remoteShip.role = role;
                        remoteShip.IsAreaController = IsAreaController;
                        remoteShip.IsRelay = IsRelay;
                        remoteShip.NumberLasers = LaserAntennaCount;
                        remoteShip.RemoteLasers = remoteLasers;
                        remoteShip.ageMs = 0;
                        RemoteShips.Add(remoteShip);
//                        _program.ErrorLog(" Adding new ship");
                    }
                }
            }

            void MainHandler(UpdateType updateSource)
            {
                foreach(var remoteship in RemoteShips)
                {
                    remoteship.ageMs += _program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                }
            }

            List<RemoteLaser> RemoteLasers = new List<RemoteLaser>();
            List<RemoteShip> RemoteShips = new List<RemoteShip>();

            class RemoteLaser
            {
                public string Name;
                public Vector3D Position;
                // WIP
            }

            class RemoteShip
            {
                public long ShipID;
                public string sName;
                public double ageMs;
                public Vector3D Position;
                public Vector3D Velocity;   
                public GridRole role;
                public bool IsAreaController;
                public bool IsRelay;
                public int NumberLasers;
                public List<RemoteLaser> RemoteLasers;
            }

        }
    }
}
