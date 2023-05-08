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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Asteroids
        {
            readonly Program _program;
            readonly WicoControl _wicoControl;
            readonly WicoIGC _wicoIGC;
            readonly Displays _displays;

            public Asteroids(Program program, WicoControl wicoControl, WicoIGC wicoIGC, Displays displays)
            {
                _program = program;
                _wicoControl = wicoControl;
                _wicoIGC = wicoIGC;
                _displays = displays;

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);
                _wicoIGC.AddPublicHandler(sAsteroidTag, BroadcastHandler);

                if(_displays != null) _displays.AddSurfaceHandler("ASTEROIDS", SurfaceHandler);
            }
            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "ASTEROIDS")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        sbNotices.Clear();
                        sbModeInfo.Clear();
                        sbModeInfo.AppendLine(asteroidsInfo.Count + " Known Asteroids");
                        foreach(var ai in asteroidsInfo)
                        {
                            sbNotices.AppendLine(" " + (_program.Me.GetPosition() - ai.Position).Length().ToString("N0")+" Meters");
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

            void LoadHandler(MyIni Ini)
            {
                int iCount = 0;
                iCount = Ini.Get(sAsteroidSection, "count").ToInt32(0);

                asteroidsInfo.Clear();
                Vector3D v3D;
                long eId = 0;

                for (int j1 = 0; j1 < iCount; j1++)
                {
                    eId = Ini.Get(sAsteroidSection, "EntityId" + j1.ToString()).ToInt32(0);
                    if (eId <= 0)
                        continue; // skip bad data
                    BoundingBoxD box = new BoundingBoxD();

                    Vector3D.TryParse(Ini.Get(sAsteroidSection, "BBMin" + j1.ToString()).ToString(), out v3D);
                    box.Min = v3D;
                    Vector3D.TryParse(Ini.Get(sAsteroidSection, "BBMax" + j1.ToString()).ToString(), out v3D);
                    box.Max = v3D;

                    AsteroidInfo ast = new AsteroidInfo
                    {
                        EntityId = eId,
                        BoundingBox = box
                    };
                    asteroidsInfo.Add(ast);
                }
            }

            void SaveHandler(MyIni Ini)
            {
                var count = asteroidsInfo.Count;
                Ini.Set(sAsteroidSection, "count", count);

                for (int i1 = 0; i1 < asteroidsInfo.Count; i1++)
                {
                    Ini.Set(sAsteroidSection, "EntityId" + i1.ToString(), asteroidsInfo[i1].EntityId.ToString());
                    Ini.Set(sAsteroidSection, "BBMin" + i1.ToString(), _program.Vector3DToString(asteroidsInfo[i1].BoundingBox.Min));
                    Ini.Set(sAsteroidSection, "BBMax" + i1.ToString(), _program.Vector3DToString(asteroidsInfo[i1].BoundingBox.Max));
                }
            }

            void BroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag
                if (msg.Tag == sAsteroidTag)
                {
                    if (msg.Data is string)
                    {
                        string[] aMessage = ((string)msg.Data).Trim().Split(':');
                        double x1, y1, z1;

                        int iOffset = 0;

                        long id = 0;
                        long.TryParse(aMessage[iOffset++], out id);

                        long asteroidID = 0;
                        long.TryParse(aMessage[iOffset++], out asteroidID);

                        x1 = Convert.ToDouble(aMessage[iOffset++]);
                        y1 = Convert.ToDouble(aMessage[iOffset++]);
                        z1 = Convert.ToDouble(aMessage[iOffset++]);
                        Vector3D vMin = new Vector3D(x1, y1, z1);

                        x1 = Convert.ToDouble(aMessage[iOffset++]);
                        y1 = Convert.ToDouble(aMessage[iOffset++]);
                        z1 = Convert.ToDouble(aMessage[iOffset++]);
                        Vector3D vMax = new Vector3D(x1, y1, z1);

                        BoundingBoxD box = new BoundingBoxD(vMin, vMax);
                        AsteroidAdd(asteroidID, box, false);
                    }

                }
            }
            // Module code:

            List<AsteroidInfo> asteroidsInfo = new List<AsteroidInfo>();

            const string sAsteroidSection = "ASTEROIDS";
            readonly string sAsteroidTag = "WICOAST";

            public class AsteroidInfo
            {
                public long EntityId;
                public BoundingBoxD BoundingBox;
                public Vector3D Position { get { return BoundingBox.Center; } }
            }

            void AsteroidAdd(long entityid, BoundingBoxD box, bool bTransmitAsteroid = true)
            {
                bool bFound = false;

                if (entityid <= 0) return;
                for (int i = 0; i < asteroidsInfo.Count; i++)
                {
                    if (asteroidsInfo[i].EntityId == entityid)
                    {
                        bFound = true;
                        break;
                    }
                }

                if (!bFound)
                {
                    AsteroidInfo ai = new AsteroidInfo();
                    ai.EntityId = entityid;
                    ai.BoundingBox = box;
                    asteroidsInfo.Add(ai);
//                    AsteroidSerialize();
                    // info added: write output..

                    if (bTransmitAsteroid)
                    {
                        //                    antSend("WICO:AST:" + SaveFile.EntityId.ToString() + ":" + entityid.ToString() + ":" + Vector3DToString(box.Min) + ":" + Vector3DToString(box.Max));
                        _program.IGC.SendBroadcastMessage(sAsteroidTag, _program.Me.EntityId.ToString() + ":" + entityid.ToString() + ":" + _program.Vector3DToString(box.Min) + ":" + _program.Vector3DToString(box.Max));
                    }
                }
            }

            public void AsteroidAdd(MyDetectedEntityInfo thisDetectedInfo, bool bTransmitAsteroid = true)
            {
                if (thisDetectedInfo.IsEmpty() || thisDetectedInfo.Type != MyDetectedEntityType.Asteroid) return;
                AsteroidAdd((long)thisDetectedInfo.EntityId, thisDetectedInfo.BoundingBox, bTransmitAsteroid);
            }

            public bool AsteroidProcessLDEI(List<MyDetectedEntityInfo> lmyDEI)
            {
                bool bFoundAsteroid = false;
                for (int j = 0; j < lmyDEI.Count; j++)
                {
                    if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                    {
                        if (AsteroidProcessDEI(lmyDEI[j]))
                            bFoundAsteroid = true;
                    }
                }
                return bFoundAsteroid;
            }

            public bool AsteroidProcessDEI(MyDetectedEntityInfo dei)
            {
//                addDetectedEntity(dei);
                bool bFoundAsteroid = false;
                if (dei.Type == MyDetectedEntityType.Asteroid)
                {
                    AsteroidAdd(dei);
                    bFoundAsteroid = true;
                }
                return bFoundAsteroid;
            }

            public int AsteroidCount()
            {
                return asteroidsInfo.Count;
            }

            public long AsteroidFindNearest(bool bInsideOnly = false)
            {
                long AsteroidID = -1;

                double distanceSQ = double.MaxValue;
                foreach (var ast in asteroidsInfo)
                {
                    if (ast.EntityId <= 0) continue;

                    if (bInsideOnly)
                    {
                        if (ast.BoundingBox.Contains(_program.Me.GetPosition()) == ContainmentType.Contains)
                        {
                            AsteroidID = ast.EntityId;
                        }
                    }
                    else
                    {
                        double curDistanceSQ = Vector3D.DistanceSquared(ast.Position, _program.Me.GetPosition());
                        if (curDistanceSQ < distanceSQ)
                        {
                            AsteroidID = ast.EntityId;
                            distanceSQ = curDistanceSQ;
                        }
                    }
                }
                return AsteroidID;
            }

            public Vector3D AsteroidGetPosition(long AsteroidID)
            {

                Vector3D pos = new Vector3D(0, 0, 0);
                for (int i = 0; i < asteroidsInfo.Count; i++)
                {
                    if (asteroidsInfo[i].EntityId == AsteroidID)
                        pos = asteroidsInfo[i].Position;
                }
                return pos;
            }

            public BoundingBoxD AsteroidGetBB(long AsteroidID)
            {

                BoundingBoxD box = new BoundingBoxD();
                for (int i = 0; i < asteroidsInfo.Count; i++)
                {
                    if (asteroidsInfo[i].EntityId == AsteroidID)
                    {
                        box = asteroidsInfo[i].BoundingBox;
                        break;
                    }
                }
                return box;
            }

        }
    }
}
