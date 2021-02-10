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

        public class WicoBases
        {
            public class BaseInfo
            {
                //antSend("WICO:BASE:" + Me.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition())XXX

                // name, ID, position, velocity, Jump Capable, Source, Sink
                // source and sink need to have "priorities". So a support vechicle can take ore from a miner drone.  and then it can deliver to a base.
                public long baseId;
                public string baseName;
                public Vector3D position;
                public bool bJumpCapable;
                // SOURCE:
                // TODO

                // SINK:
                // TODO:

                // TODO: flag to Ignore for communications range calc

                // TODO: Age of last contact

                // TODO: time-out for docking selection.  So can choose 'next' if base says 'no room', etc.

            }
            List<BaseInfo> baseList = new List<BaseInfo>();

            const string sBaseSavedListSection = "BASE1.0";

            const string sIGCBaseAnnounceTag = "BASE";

            Program _program;
            WicoIGC _wicoIGC;
            Displays _displays;

            public WicoBases(Program program, WicoIGC iGC, Displays displays )
            {
                _program = program;
                _wicoIGC = iGC;
                _displays = displays;

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                _displays.AddSurfaceHandler("BASELOCS", SurfaceHandler);


                _wicoIGC.AddPublicHandler(sIGCBaseAnnounceTag, BaseBroadcastHandler);
            }

            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
            {
                if (tag == "BASELOCS")
                {
                    if (ActionType == Displays.DODRAW)
                    {
                        sbNotices.Clear();
                        sbModeInfo.Clear();
                        sbNotices.AppendLine(baseList.Count + " Known bases");

                        {
                            foreach (var myBase in baseList)
                            {
                                sbNotices.AppendLine(myBase.baseName + " " + (_program.Me.GetPosition() - myBase.position).Length().ToString("N0") + " Meters");
                            }
                            tsurface.WriteText(sbModeInfo);
                            if (tsurface.SurfaceSize.Y < 256)
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
                        if (tsurface.SurfaceSize.Y < 256)
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
                /*
                 *             int iCount = -1;
                            long eId = 0;
                            string sBaseName = "";
                            Vector3D position = new Vector3D();
                            bool bJumpCapable = false;

                            iniWicoCraftSave.GetValue(sBaseSavedListSection, "count", ref iCount);

                            for (int j1 = 0; j1 < iCount; j1++)
                            {
                                iniWicoCraftSave.GetValue(sBaseSavedListSection, "ID" + j1.ToString(), ref eId);
                                iniWicoCraftSave.GetValue(sBaseSavedListSection, "name" + j1.ToString(), ref sBaseName);
                                iniWicoCraftSave.GetValue(sBaseSavedListSection, "position" + j1.ToString(), ref position);
                                iniWicoCraftSave.GetValue(sBaseSavedListSection, "Jumpable" + j1.ToString(), ref bJumpCapable);

                                BaseInfo b1 = new BaseInfo
                                {
                                    baseId = eId,
                                    baseName = sBaseName,
                                    position = position,
                                    bJumpCapable = bJumpCapable
                                };

                                baseList.Add(b1);
                            }

                            return iCount;
*/
            }

            void SaveHandler(MyIni Ini)
            {
                /*
                iniWicoCraftSave.SetValue(sBaseSavedListSection, "count", baseList.Count);

                for (int i1 = 0; i1 < baseList.Count; i1++)
                {
                    iniWicoCraftSave.SetValue(sBaseSavedListSection, "ID" + i1.ToString(), baseList[i1].baseId);
                    iniWicoCraftSave.SetValue(sBaseSavedListSection, "name" + i1.ToString(), baseList[i1].baseName);
                    iniWicoCraftSave.SetValue(sBaseSavedListSection, "position" + i1.ToString(), baseList[i1].position);
                    iniWicoCraftSave.SetValue(sBaseSavedListSection, "Jumpable" + i1.ToString(), baseList[i1].bJumpCapable);
                }
                */
                //                Ini.Set(sNavSection, "vTarget", VNavTarget.ToString());
            }
            void BaseAdd(long baseId, string baseName, Vector3D Position, bool bJumpCapable = false)
            {

                // TODO: Add age of knowledge
                BaseInfo basei = new BaseInfo
                {
                    baseId = baseId,
                    baseName = baseName,
                    position = Position,
                    bJumpCapable = bJumpCapable
                };

                //TODO:
                // Source:
                // Sink:
                for (int i = 0; i < baseList.Count; i++)
                {
                    if (baseList[i].baseId == baseId)
                    {
                        // TODO: update age of knowledge
                        // update information on existing entry (TODO: STRUCTURE! so this probably dosn't work
                        baseList[i].baseName = baseName;
                        baseList[i].position = Position;
                        baseList[i].bJumpCapable = bJumpCapable;
                        return;
                    }
                }
                // else not found.  add it.
                baseList.Add(basei);
            }

            public string baseInfoString()
            {
                string s1;
                if (baseList.Count == 0)
                    return "No Known Bases";
                if (baseList.Count > 1)
                    s1 = baseList.Count.ToString() + " Known Bases\n";
                else s1 = baseList.Count.ToString() + " Known Base\n";

                for (int i = 0; i < baseList.Count; i++)
                {
//                    s1 += baseList[i].baseId + ":";
                    s1 += baseList[i].baseName + ":";
                    s1 += _program.Vector3DToString(baseList[i].position) + ":";
                    s1 += "\n";
                }
                return s1;
            }

            double dBaseRequestTransmitWait = 25; //seconds between active transmits

            double dBaseRequestLastTransmit = -1;

            public void checkBases(bool bForceRequest = false)
            {
                string sName = _program.Me.CubeGrid.CustomName;
                Vector3D vPosition = _program.Me.GetPosition();

                if (bForceRequest)
                {
                    // empty the list because somebody might have been deleted/destroyed/etc
                    baseList.Clear();
//                    BaseSerialize();
                }
                if (dBaseRequestLastTransmit > dBaseRequestTransmitWait || bForceRequest)
                {
                    dBaseRequestLastTransmit = 0;

                    _program.IGC.SendBroadcastMessage("BASE?", sName + ":" + _program.Me.EntityId.ToString() + ":" + _program.Vector3DToString(vPosition));
                }
                else
                {
                    if (dBaseRequestLastTransmit < 0)
                    {
                        // first-time init
                        dBaseRequestLastTransmit = _program.Me.EntityId % dBaseRequestTransmitWait; // randomize initial send

                    }
                    if (baseList.Count < 1)
                        dBaseRequestLastTransmit += _program.Runtime.TimeSinceLastRun.TotalSeconds;
                }
            }

            public float RangeToNearestBase()
            {
                double bestRange = double.MaxValue;
                int iBest = BaseIndexOf(BaseFindNearest());
                if (iBest >= 0 )
                {
                    bestRange = (_program.Me.GetPosition() - baseList[iBest].position).Length();
                }
                return (float)bestRange;
            }
            public long BaseFindNearest()
            {
                int iBest = -1;

                double distanceSQ = double.MaxValue;
                //sInitResults += baseList.Count + " Bases";
                for (int i = 0; i < baseList.Count; i++)
                {
                    double curDistanceSQ = Vector3D.DistanceSquared(baseList[i].position, _program.Me.GetPosition());
                    if (curDistanceSQ < distanceSQ)
                    {
                        //                    sInitResults += " Choosing" + baseList[i].baseName;
                        iBest = i;
                        distanceSQ = curDistanceSQ;
                    }
                }
                if (iBest < 0) return iBest;
                else return baseList[iBest].baseId;
            }


            public long BaseFindBest()
            {
                return BaseFindNearest();
            }

            public int GetDockingBases(ref List<long> dockingBaseList)
            {
                int count=0;
                if (dockingBaseList == null) return 0;
                dockingBaseList.Clear();
                foreach(var baseinfo in baseList)
                {
                    count++;
                    dockingBaseList.Add(baseinfo.baseId);
                }

                return count;

            }

            public int BaseIndexOf(long baseID)
            {
                for (int i1 = 0; i1 < baseList.Count; i1++)
                {
                    if (baseList[i1].baseId == baseID)
                        return i1;
                }
                return -1;
            }

            public Vector3D BasePositionOf(long baseId)
            {
                Vector3D vPos = new Vector3D();
                for (int i1 = 0; i1 < baseList.Count; i1++)
                    if (baseList[i1].baseId == baseId)
                        return baseList[i1].position;
                return vPos;
            }
            public string BaseName(long baseId)
            {
                for (int i1 = 0; i1 < baseList.Count; i1++)
                    if (baseList[i1].baseId == baseId)
                        return baseList[i1].baseName;
                return "INVALID";
            }

            void BaseBroadcastHandler(MyIGCMessage msg)
            {
                // NOTE: called on ALL received messages; not just 'our' tag

                if (msg.Tag != sIGCBaseAnnounceTag)
                    return; // not our message

                if (msg.Data is string)
                {
                    _program.Echo("Base Response");
                    string sMessage = (string)msg.Data;
                    string[] aMessage = sMessage.Trim().Split(':');

                    double x1, y1, z1;
                    int iOffset = 0;
                    string sName = aMessage[iOffset++];

                    long id = 0;
                    long.TryParse(aMessage[iOffset++], out id);

                    x1 = Convert.ToDouble(aMessage[iOffset++]);
                    y1 = Convert.ToDouble(aMessage[iOffset++]);
                    z1 = Convert.ToDouble(aMessage[iOffset++]);
                    Vector3D vPosition = new Vector3D(x1, y1, z1);

                    bool bJumpCapable = _program.stringToBool(aMessage[iOffset++]);

                    BaseAdd(id, sName, vPosition, bJumpCapable);
                }
            }
        }
    }
}
