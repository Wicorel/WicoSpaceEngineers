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
        public class OresRemote:OreInfoLocs
        {
            readonly Program _program;
            readonly WicoIGC _wicoIGC;
            readonly Asteroids _asteroids;
            readonly Displays _displays;

            public OresRemote(Program program, WicoBlockMaster wbm, WicoControl wicoControl, WicoIGC wicoIGC, Asteroids asteroids, Displays displays) : base(program,wbm,wicoIGC,asteroids,displays)
            {
                _program = program;
                _wicoIGC = wicoIGC;
                _asteroids = asteroids;
                _displays = displays;

                //                _program.AddLoadHandler(LoadHandler);
                //                _program.AddSaveHandler(SaveHandler);
                _wicoIGC.AddPublicHandler(sOreTag, BroadcastHandler);
            }

            //                if (_displays != null) _displays.AddSurfaceHandler("ORELOCS", SurfaceHandler);
            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            new public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "ORELOCS")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        sbNotices.Clear();
                        sbModeInfo.Clear();
                        sbNotices.AppendLine(_oreLocs.Count + " Ore Locations");

                        {
                            foreach (var oreloc in _oreLocs)
                            {
                                sbNotices.AppendLine(oreloc.AstEntityId.ToString("N0") + " " + (_program.Me.GetPosition() - oreloc.position).Length().ToString("N0") + "Meters");
                            }
                            tsurface.WriteText(sbModeInfo);
                            if (tsurface.SurfaceSize.Y < 512)
                            { // small/corner LCD

                            }
                            else
                            {
                                tsurface.WriteText(sbNotices, true);
                            }
                        }
                    }
                    else if (ActionType == Displays.SETUPDRAW)
                    {
                        tsurface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                        tsurface.WriteText("");
                        if (tsurface.SurfaceSize.Y < 512)
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                            tsurface.FontSize = 2;
                        }
                        else
                        {
                            tsurface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
                            tsurface.FontSize = 1.5f;
                        }
                    }
                    else if (ActionType == Displays.CLEARDISPLAY)
                    {
                        tsurface.WriteText("");
                    }
                }
            }

            void BroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
                if (msg.Tag == sOreTag)
                {
                    if (msg.Data is string)
                    {
                        string[] aMessage = ((string)msg.Data).Trim().Split(':');
                        double x1, y1, z1;

                        int iOffset = 0;

                        //  antSend("WICO:ORE:" + Me.CubeGrid.EntityId.ToString() + ":" + asteroidId + ":" + OreID + ":" + Vector3DToString(Position) + ":" + Vector3DToString(vVec) + ":" + detectionType.ToString());
                        //  antSend("WICO:ORE:" + asteroidId + ":" + OreID + ":" + Vector3DToString(Position) + ":" + Vector3DToString(vVec) + ":" + detectionType.ToString());

                        long asteroidID = 0;
                        long.TryParse(aMessage[iOffset++], out asteroidID);

                        int oreID = 0;
                        int.TryParse(aMessage[iOffset++], out oreID);

                        x1 = Convert.ToDouble(aMessage[iOffset++]);
                        y1 = Convert.ToDouble(aMessage[iOffset++]);
                        z1 = Convert.ToDouble(aMessage[iOffset++]);
                        Vector3D position = new Vector3D(x1, y1, z1);

                        x1 = Convert.ToDouble(aMessage[iOffset++]);
                        y1 = Convert.ToDouble(aMessage[iOffset++]);
                        z1 = Convert.ToDouble(aMessage[iOffset++]);
                        Vector3D vector = new Vector3D(x1, y1, z1);

                        long detectionType = 0;
                        long.TryParse(aMessage[iOffset++], out detectionType);

                        OreLocInfo ore = new OreLocInfo
                        {
                            AstEntityId = asteroidID,
                            oreId = oreID,
                            position = position,
                            vector = vector,
                            detectionType = detectionType
                        };
                        _oreLocs.Add(ore);
                    }

                }
            }


        }
    }
}
