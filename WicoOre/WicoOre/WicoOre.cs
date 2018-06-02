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

        const string sOreSection = "ORE";
        List<OreLocInfo> oreLocs = new List<OreLocInfo>();


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
            // from 'tasting' with drills
            // player given GPS
        }

        void initOreLocInfo()
        {
            oreLocs.Clear();
            OreInitInfo();

            // load from text panel...
            OreDeserialize();
        }

        int OreDeserialize()
        {
            if (iniWicoCraftSave == null) return -1;

            int iCount = 0;
            iniWicoCraftSave.GetValue(sOreSection, "count", ref iCount);
//            Echo("OreDeserialize Count=" + iCount);
//            if (iCount > 0) sInitResults += "\nODeserialize count>0";

            oreLocs.Clear();
            long eId = 0;
            int oreID = 0;
            Vector3D position = new Vector3D(0, 0, 0);
            Vector3D vector = new Vector3D(0, 0, 0);
            long detectionType = 0;

            for (int j1 = 0; j1 < iCount; j1++)
            {

                iniWicoCraftSave.GetValue(sOreSection, "AsteroidId" + j1.ToString(), ref eId);
                iniWicoCraftSave.GetValue(sOreSection, "oreId" + j1.ToString(), ref oreID);
                iniWicoCraftSave.GetValue(sOreSection, "position" + j1.ToString(), ref position);
                iniWicoCraftSave.GetValue(sOreSection, "vector" + j1.ToString(), ref vector);
                iniWicoCraftSave.GetValue(sOreSection, "detectiontype" + j1.ToString(), ref detectionType);

                OreLocInfo ore = new OreLocInfo
                {
                    AstEntityId = eId,
                    oreId = oreID,
                    position = position,
                    vector = vector,
                    detectionType = detectionType
                };
                oreLocs.Add(ore);
            }
            return iCount;
        }

        void OreSerialize()
        {
            if (iniWicoCraftSave == null) return;

            var count = oreLocs.Count;
            iniWicoCraftSave.SetValue(sOreSection, "count", count);
//            Echo("OreSerialize Count=" + count);
//            if (count > 0) sInitResults += "\nOSerialize count>0";
            for (int i1 = 0; i1 < oreLocs.Count; i1++)
            {
                iniWicoCraftSave.SetValue(sOreSection, "AsteroidId" + i1.ToString(), oreLocs[i1].AstEntityId);
                iniWicoCraftSave.SetValue(sOreSection, "oreId" + i1.ToString(), oreLocs[i1].oreId);
                iniWicoCraftSave.SetValue(sOreSection, "position" + i1.ToString(), oreLocs[i1].position);
                iniWicoCraftSave.SetValue(sOreSection, "vector" + i1.ToString(), oreLocs[i1].vector);
                iniWicoCraftSave.SetValue(sOreSection, "detectiontype" + i1.ToString(), oreLocs[i1].detectionType);
            }
        }

        void OreAddLoc(long asteroidId, int OreID, Vector3D Position, Vector3D vVec, long detectionType)
        {
            OreLocInfo oli = new OreLocInfo();
            oli.oreId = OreID;
            oli.AstEntityId = asteroidId;
            oli.position = Position;
            oli.vector = vVec;
            oli.detectionType = detectionType;
            //TODO:
            // search by position and only add if NOT 'near' another entry
            oreLocs.Add(oli);
//should be done by caller            OreSerialize();
            // transmit found location...
            antSend("WICO:ORE:" + Me.CubeGrid.EntityId.ToString() + ":" + asteroidId + ":" + OreID + ":" + Vector3DToString(Position) + ":" + Vector3DToString(vVec) + ":" + detectionType.ToString());
//            sInitResults += "\nAdded Ore id=" + OreID + " count=" + oreLocs.Count();
        }

        void OreDumpLocs()
        {
            for (int i = 0; i < oreLocs.Count; i++)
            {
                Echo(OreName(oreLocs[i].oreId) + ":" + oreLocs[i].position.ToString() + ":" + oreLocs[i].detectionType.ToString());
            }

        }

        List<OreInfo> oreInfos = new List<OreInfo>();
        public class OreInfo
        {
            public int      oreID;
            public string   oreName;
            public long     desireability;
            public bool     bFound;
            public double   localAmount;
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

        void OreInitInfo()
        {
            oreInfos.Clear();
            // read from text panel..

            //iff empty text panel, init
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
            // write data back to text panel if changed.
        }

        string OreName(int oreId)
        {
            for (int i = 0; i < oreInfos.Count; i++)
                if (oreInfos[i].oreID == oreId)
                    return oreInfos[i].oreName;

            return "INVALID ID:" + oreId;
        }

        void OreClearAmounts()
        {
            if (oreInfos.Count < 1)
                OreInitInfo();
            for (int i = 0; i < oreInfos.Count; i++)
            {
                oreInfos[i].localAmount = 0;
            }
        }

        void OreAddAmount(string sOre, double lAmount, bool bNoFind = false)
        {
//            Echo("addOreAmount:" + sOre);// + ":" + lAmount.ToString() + " bNoFind="+bNoFind.ToString());
            if (oreInfos.Count < 1)
                OreInitInfo();

            for (int i = 0; i < oreInfos.Count; i++)
            {
                //		Echo(oreInfos[i].oreName);
                if (oreInfos[i].oreName == sOre)
                {
 //                   Echo("Is in list!");
 //                   Echo("Current amount=" + oreInfos[i].localAmount);
                    if (
                        !bNoFind 
                        && lAmount > 0 
                        && !oreInfos[i].bFound
                        )
                    {
                        oreInfos[i].bFound = true;
                        OreFound(oreInfos[i].oreID);
                    }
//                    else Echo("already 'found'");
                    oreInfos[i].localAmount += lAmount;
                }
            }
        }

        int iStoneOreId = -1;
        double currentStoneAmount()
        {
            if(iStoneOreId<0)
            {
                for(int i=0;i<oreInfos.Count;i++)
                {
                    if(oreInfos[i].oreName=="Stone")
                    {
                        iStoneOreId = i;
                        break;
                    }
                }
            }
            if (iStoneOreId < 0) return 0;
            return oreInfos[iStoneOreId].localAmount;
        }

        void OreFound(int oreIndex)
        {
//            Echo("FIRST FIND!:" + oreInfos[oreIndex].oreName);
            if (oreInfos[oreIndex].desireability > 0)
            {
                // add ore found loc...
                MatrixD refOrientation = new MatrixD();
                refOrientation = shipOrientationBlock.WorldMatrix;

//                MatrixD refOrientation = GetBlock2WorldTransform(shipOrientationBlock);
                Vector3D vVec = Vector3D.Normalize(refOrientation.Forward);

                // but only if we got it inside asteroid.
                long astEntity = AsteroidFindNearest(true);
                if (astEntity > 0)
                {
//                    sInitResults += "\nOreAddLoc for  oreid=" + oreIndex.ToString();
                    OreAddLoc(astEntity, oreIndex, shipOrientationBlock.GetPosition(), vVec, 69);
                }
//                sInitResults += "\nNo Asteroid found for oreid=" + oreIndex.ToString();
            }
        }

        void OreDumpFound()
        {
            Echo("Ore Contents:");
            for (int i = 0; i < oreInfos.Count; i++)
            {
                if (oreInfos[i].localAmount > 0  || oreInfos[i].bFound)
                    Echo(oreInfos[i].oreName + " " + oreInfos[i].localAmount.ToString("N0"));
            }
        }

        // uses lContainers from WicoCargo
        void OreDoCargoCheck(bool bInit = false)
        {
            if (lContainers==null || lContainers.Count < 1)
                initCargoCheck();

            if (lContainers==null || lContainers.Count < 1)
            {
                // No cargo containers found.
                return;
            }

            OreClearAmounts();

            //            Echo(lContainers.Count + " Containers");
            for (int i = 0; i < lContainers.Count; i++)
            {
                var inv = lContainers[i].GetInventory(0);
                if (inv == null) continue;
                var items = inv.GetItems();
                // go through all items
                for (int i2 = 0; i2 < items.Count; i2++)
                {
                    var item = items[i2];

                    //string str;
                    //str = item.Content.TypeId.ToString();
                    //string[] aS = str.Split('_');
                    //string sType = aS[1];

                    //str = item.Content.SubtypeName + " " + sType + " " + item.Amount;
                    //			StatusLog("  " + str,getTextBlock(CargoStatus));
                    //			Echo(" " + str);
                    //			if (item.Content.SubtypeName == "Stone" && item.Content.ToString().Contains("Ore"))
//                    Echo(item.Content.ToString());
                    if (item.Content.ToString().Contains("Ore"))
                    {
//                        Echo("Adding " + item.Content.SubtypeId.ToString());
                        OreAddAmount(item.Content.SubtypeId.ToString(), (double)item.Amount, bInit);
                        //                        item.Content.SubtypeId;


                        // PROHIBITED as of 1.187
                        //                        OreAddAmount(item.Content.SubtypeName, (double)item.Amount, bInit);
                        //				stoneamount+=(double)item.Amount;
                    }
                }

            }

        }

        bool OreProcessMessage(string sMessage)
        {
            double x1, y1, z1;

            string[] aMessage = sMessage.Trim().Split(':');

            if (aMessage.Length > 1)
            {
                if (aMessage[0] != "WICO")
                {
                    Echo("not wico system message");
                    return false;
                }
                if (aMessage.Length > 2)
                {
                    if (aMessage[1] == "ORE")
                    {
//                        sInitResults += "Got ORE Message:\n" + sMessage;
                        //           0      1           2                                    3               4                5                                6                                7  
                        //  antSend("WICO:ORE:" + Me.CubeGrid.EntityId.ToString() + ":" + asteroidId + ":" + OreID + ":" + Vector3DToString(Position) + ":" + Vector3DToString(vVec) + ":" + detectionType.ToString());

                        int iOffset = 2;

                        long id = 0; // message source
                        long.TryParse(aMessage[iOffset++], out id);

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
                        oreLocs.Add(ore);
 //                       sInitResults += "\noreLocs After=" + oreLocs.Count;
//    sInitResults += "\nIncoming ORE  Processed!";
                        return true; // we processed it
                    }
                    else if (aMessage[1] == "ORE?")
                    { // TODO: process request for known Ore

                    }

                }
            }
            return false;
        }

    }
}