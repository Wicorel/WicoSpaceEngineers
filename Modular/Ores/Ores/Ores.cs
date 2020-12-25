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
        public class OresLocal : CargoCheck
        {
            readonly Program _program;
            readonly WicoBlockMaster _wicoBlockMaster;
            readonly WicoControl _wicoControl;
            readonly WicoIGC _wicoIGC;
            readonly Asteroids _asteroids;
            readonly Displays _displays;
            readonly OreInfoLocs _oreInfoLocs;

            public OresLocal(Program program, WicoBlockMaster wbm, WicoControl wicoControl, WicoIGC wicoIGC, Asteroids asteroids, OreInfoLocs orelocs, Displays displays) : base(program, wbm, null)
            {
                _program = program;
                _wicoBlockMaster = wbm;
                _wicoControl = wicoControl;
                _wicoIGC = wicoIGC;
                _asteroids = asteroids;
                _displays = displays;
                _oreInfoLocs = orelocs;
                if(_oreInfoLocs==null)
                {
                    _oreInfoLocs = new OreInfoLocs(program, wbm, wicoIGC, asteroids,displays);
                }

//                _program.AddLoadHandler(LoadHandler);
//                _program.AddSaveHandler(SaveHandler);

                //                _wicoIGC.AddPublicHandler(sOreTag, BroadcastHandler);

 //               if (_displays != null) _displays.AddSurfaceHandler("ORELOCS", SurfaceHandler);

            }

            void BroadcastHandler(MyIGCMessage msg)
            {
            }

            public void OreDoCargoCheck(bool bInit = false)
            {
                _oreInfoLocs.OreClearAmounts();

                var itemsL = new List<MyInventoryItem>();
                for (int i = 0; i < lContainers.Count; i++)
                {
                    var inv = lContainers[i].GetInventory(0);
                    if (inv == null) continue;
                    //                var itemsL = inv.GetItems();
                    inv.GetItems(itemsL);
                    // go through all itemsL
                    for (int i2 = 0; i2 < itemsL.Count; i2++)
                    {
                        var item = itemsL[i2];

                        if (item.Type.ToString().Contains("Ore"))
                        {
                            //                        Echo("Adding " + item.Content.SubtypeId.ToString());
                            _oreInfoLocs.OreAddAmount(item.Type.SubtypeId.ToString(), (double)item.Amount, bInit);
                        }
                    }

                }
                for (int i = 0; i < localEjectors.Count; i++)
                {
                    var inv = localEjectors[i].GetInventory(0);
                    if (inv == null) continue;
                    //                var itemsL = inv.GetItems();
                    inv.GetItems(itemsL);
                    // go through all itemsL
                    for (int i2 = 0; i2 < itemsL.Count; i2++)
                    {
                        var item = itemsL[i2];

                        if (item.Type.ToString().Contains("Ore"))
                        {
                            //                        Echo("Adding " + item.Content.SubtypeId.ToString());
                            _oreInfoLocs.OreAddAmount(item.Type.SubtypeId.ToString(), (double)item.Amount, bInit);
                        }
                    }

                }
            }

        }

        public class OreInfoLocs
        {
            readonly Program _program;
            readonly WicoBlockMaster _wicoBlockMaster;
            readonly WicoIGC _wicoIGC;
            readonly Asteroids _asteroids;
            readonly Displays _displays;

            protected List<OreLocInfo> _oreLocs = new List<OreLocInfo>();

            const string sOreSection = "ORE";
            const string sOreDesirabilitySection = "OREDESIREABILITY";
            protected const string sOreTag = "WICOORE";

            public OreInfoLocs(Program program, WicoBlockMaster wbm, WicoIGC igc, Asteroids asteroids, Displays displays)
            {
                _program = program;
                _wicoBlockMaster = wbm;
                _wicoIGC = igc;
                _asteroids = asteroids;
                _displays = displays;

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                OreInitInfo(_program._CustomDataIni);

                if (_displays != null) _displays.AddSurfaceHandler("ORELOCS", SurfaceHandler);
            }

            public class OreLocInfo
            {
                public long AstEntityId;
                public int oreId;
                public Vector3D position;
                public Vector3D vector;
                public long detectionType;
                // detection types:
                // pointed at by player with camera
                // from ore detector (!)
                // 69  from 'tasting' with drills
                // player given GPS
            }
            void LoadHandler(MyIni ini)
            {
                int iCount;

                iCount = ini.Get(sOreSection, "count").ToInt32();

                _oreLocs.Clear();
                long eId = 0;
                int oreID = 0;
                Vector3D position = new Vector3D(0, 0, 0);
                Vector3D vector = new Vector3D(0, 0, 0);
                long detectionType = 0;

                for (int j1 = 0; j1 < iCount; j1++)
                {
                    eId = ini.Get(sOreSection, "AsteroidId" + j1.ToString()).ToInt32(0);

                    oreID = ini.Get(sOreSection, "oreId" + j1.ToString()).ToInt32(0);

                    Vector3D.TryParse(ini.Get(sOreSection, "position" + j1.ToString()).ToString(), out position);
                    Vector3D.TryParse(ini.Get(sOreSection, "vector" + j1.ToString()).ToString(), out vector);
                    detectionType = ini.Get(sOreSection, "detectiontype" + j1.ToString()).ToInt32();

                    OreLocInfo ore = new OreLocInfo
                    {
                        AstEntityId = eId,
                        oreId = oreID,
                        position = position,
                        vector = vector,
                        detectionType = detectionType
                    };
                    _oreLocs.Add(ore);
                }
            }

            void SaveHandler(MyIni ini)
            {
                var count = _oreLocs.Count;
                ini.Set(sOreSection, "count", count);
                //            Echo("OreSerialize Count=" + count);
                //            if (count > 0) sInitResults += "\nOSerialize count>0";
                for (int i1 = 0; i1 < _oreLocs.Count; i1++)
                {
                    ini.Set(sOreSection, "AsteroidId" + i1.ToString(), _oreLocs[i1].AstEntityId);
                    ini.Set(sOreSection, "oreId" + i1.ToString(), _oreLocs[i1].oreId);
                    ini.Set(sOreSection, "position" + i1.ToString(), _program.Vector3DToString(_oreLocs[i1].position));
                    ini.Set(sOreSection, "vector" + i1.ToString(), _program.Vector3DToString(_oreLocs[i1].vector));
                    ini.Set(sOreSection, "detectiontype" + i1.ToString(), _oreLocs[i1].detectionType);
                }
            }
            StringBuilder sbNotices = new StringBuilder(300);
            StringBuilder sbModeInfo = new StringBuilder(100);

            public void SurfaceHandler(string tag, IMyTextSurface tsurface, int ActionType)
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

            public void OreAddLoc(long asteroidId, int OreID, Vector3D Position, Vector3D vVec, long detectionType)
            {
                OreLocInfo oli = new OreLocInfo();
                oli.oreId = OreID;
                oli.AstEntityId = asteroidId;
                oli.position = Position;
                oli.vector = vVec;
                oli.detectionType = detectionType;
                //TODO:
                // search by position and only add if NOT 'near' another entry
                _oreLocs.Add(oli);
                
                _program.IGC.SendBroadcastMessage(sOreTag, asteroidId + ":" + OreID + ":" + _program.Vector3DToString(Position) + ":" + _program.Vector3DToString(vVec) + ":" + detectionType.ToString());
            }

            public void OreDumpLocs()
            {
                // WARNING: can cause too complex..
                for (int i = 0; i < _oreLocs.Count; i++)
                {
                    _program.Echo(OreName(_oreLocs[i].oreId) + ":" + _oreLocs[i].position.ToString() + ":" + _oreLocs[i].detectionType.ToString());
                }
            }

            List<OreInfo> oreInfos = new List<OreInfo>();
            public class OreInfo
            {
                public int oreID;
                public string oreName;
                public long desireability;
                public bool bFound;
                public double localAmount;
            }

            const string URANIUM = "Uranium";
            const string PLATINUM = "Platinum";
            const string ICE = "Ice";
            const string COBALT = "Cobalt";
            const string GOLD = "Gold";
            const string MAGNESIUM = "Magnesium";
            const string NICKEL = "Nickel";
            const string SILICON = "Silicon";
            const string SILVER = "Silver";
            const string IRON = "Iron";
            const string SCRAP = "Scrap";
            const string STONE = "Stone";
            string[] aOres = { "Unknown", URANIUM, PLATINUM, ICE, COBALT, GOLD, MAGNESIUM, NICKEL, SILICON, SILVER, IRON, STONE };
            long[] lOreDesirability = { 0, 100, 95, 75, 55, 45, 45, 45, 45, 45, 15, -1 };

            void OreInitInfo(MyIni ini)
            {
                oreInfos.Clear();
                bool bInfoChanged = false;
                bool bInforead = false;
                // read from text panel..

                if (ini != null)
                {
                    int iCount = 0;
                    iCount = ini.Get(sOreDesirabilitySection, "count").ToInt32();

                    if (iCount >= lOreDesirability.Length)
                    {
                        bInforead = true;
                        for (int j1 = 0; j1 < iCount; j1++)
                        {
                            int oreID = 0;
                            string oreName = "";
                            long desireability = -1;
                            bool bFound = false;
                            double localAmount = 0;
                            oreID = ini.Get(sOreDesirabilitySection, "oreId" + j1.ToString()).ToInt32();
                            //                           iniWicoCraftSave.GetValue(sOreDesirabilitySection, "oreId" + j1.ToString(), ref oreID);
                            oreName = ini.Get(sOreDesirabilitySection, "oreName" + j1.ToString()).ToString();
                            //                            iniWicoCraftSave.GetValue(sOreDesirabilitySection, "oreName" + j1.ToString(), ref oreName);
                            desireability = ini.Get(sOreDesirabilitySection, "desireability" + j1.ToString()).ToInt64();
                            // iniWicoCraftSave.GetValue(sOreDesirabilitySection, "desireability" + j1.ToString(), ref desireability);
                            bFound = ini.Get(sOreDesirabilitySection, "bFound" + j1.ToString()).ToBoolean();
                            //iniWicoCraftSave.GetValue(sOreDesirabilitySection, "bFound" + j1.ToString(), ref bFound);
                            localAmount = ini.Get(sOreDesirabilitySection, "localAmount" + j1.ToString()).ToDouble();
                            //                            iniWicoCraftSave.GetValue(sOreDesirabilitySection, "localAmount" + j1.ToString(), ref localAmount);

                            OreInfo oi = new OreInfo();
                            oi.oreID = oreID;
                            oi.oreName = oreName;
                            oi.desireability = desireability;
                            oi.bFound = bFound;
                            oi.localAmount = localAmount;
                            oreInfos.Add(oi);
                        }

                    }
                }

                if (!bInforead)
                {
                    //iff empty text panel, init
                    bInfoChanged = true;
                    for (int l1 = 0; l1 < aOres.Length; l1++)
                    {
                        OreInfo oi = new OreInfo();
                        oi.oreID = l1;
                        oi.oreName = aOres[l1];
                        oi.desireability = lOreDesirability[l1];
                        oi.bFound = false;
                        oi.localAmount = 0;
                        oreInfos.Add(oi);
                    }

                }
                // write data back to text panel if changed.
                if (bInfoChanged)
                {
                    // we need to write it back out
                    _program.CustomDataChanged();
                    var count = oreInfos.Count;
                    ini.Set(sOreDesirabilitySection, "count", count);
                    for (int i1 = 0; i1 < oreInfos.Count; i1++)
                    {
                        ini.Set(sOreDesirabilitySection, "oreId" + i1.ToString(), oreInfos[i1].oreID);
                        ini.Set(sOreDesirabilitySection, "oreName" + i1.ToString(), oreInfos[i1].oreName);
                        ini.Set(sOreDesirabilitySection, "desireability" + i1.ToString(), oreInfos[i1].desireability);
                        ini.Set(sOreDesirabilitySection, "bFound" + i1.ToString(), oreInfos[i1].bFound);
                        ini.Set(sOreDesirabilitySection, "localAmount" + i1.ToString(), oreInfos[i1].localAmount);
                    }
                }
            }

            public string OreName(int oreId)
            {
                for (int i = 0; i < oreInfos.Count; i++)
                    if (oreInfos[i].oreID == oreId)
                        return oreInfos[i].oreName;

                return "INVALID ID:" + oreId;
            }

            public void OreClearAmounts()
            {
                if (oreInfos.Count < 1)
                    OreInitInfo(_program._CustomDataIni);
                for (int i = 0; i < oreInfos.Count; i++)
                {
                    oreInfos[i].localAmount = 0;
                }
            }

            public void OreAddAmount(string sOre, double lAmount, bool bNoFind = false)
            {
                //            Echo("addOreAmount:" + sOre);// + ":" + lAmount.ToString() + " bNoFind="+bNoFind.ToString());
                if (oreInfos.Count < 1)
                    OreInitInfo(_program._CustomDataIni);

                for (int i = 0; i < oreInfos.Count; i++)
                {
                    //		Echo(oreInfos[i].oreName);
                    if (oreInfos[i].oreName == sOre)
                    {
                        //                   Echo("Is in list!");
                        //                   Echo("Current amount=" + oreInfos[i].localAmount);
                        oreInfos[i].localAmount += lAmount;
                        if (lAmount > 0)
                            oreInfos[i].bFound = true;
                        if (
                            !bNoFind
                            && lAmount > 0
                            && !oreInfos[i].bFound
                            )
                        {
                            OreFound(oreInfos[i].oreID);
                            //                        OreFound(oreInfos[i]);
                        }
                        return;
                        //                    else Echo("already 'found'");
                    }
                }
                _program.ErrorLog("Ore :'" + sOre + "' Not found");
            }

            public double CurrentUndesireableAmount()
            {
                double undesireableAmount = 0;

                for (int i = 0; i < oreInfos.Count; i++)
                {
                    if (oreInfos[i].desireability < 0)
                    {
                        undesireableAmount += oreInfos[i].localAmount;
                    }
                }
                return undesireableAmount;
            }

            void OreFound(int oreIndex)
            {
                //            Echo("FIRST FIND!:" + oreInfos[oreIndex].oreName);
                if (oreInfos[oreIndex].desireability > 0)
                {
                    // add ore found loc...
                    MatrixD refOrientation = new MatrixD();
                    refOrientation = _wicoBlockMaster.GetMainController().WorldMatrix;

                    //                MatrixD refOrientation = GetBlock2WorldTransform(shipOrientationBlock);
                    Vector3D vVec = Vector3D.Normalize(refOrientation.Forward);

                    // but only if we got it inside asteroid.
                    long astEntity = _asteroids.AsteroidFindNearest(true);
                    if (astEntity > 0)
                    {
                        //                    sInitResults += "\nOreAddLoc for  oreid=" + oreIndex.ToString();
                        OreAddLoc(astEntity, oreIndex, _wicoBlockMaster.GetMainController().GetPosition(), vVec, 69);
                    }
                    //                sInitResults += "\nNo Asteroid found for oreid=" + oreIndex.ToString();
                }
            }

            void OreDumpFound()
            {
                _program.Echo("Ore Contents:");
                for (int i = 0; i < oreInfos.Count; i++)
                {
                    if (oreInfos[i].localAmount > 0 || oreInfos[i].bFound)
                        _program.Echo(oreInfos[i].oreName + " " + oreInfos[i].localAmount.ToString("N0"));
                }
            }
        }

    }
}
