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
        string sLastOreLoad = "";

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
            InitMinerInfo();

            // load from text panel...
            OreDeserialize();
        }

        int OreDeserialize()
        {
            string sOreSave;

            if (iniWicoCraftSave == null) return -1;

            sOreSave = iniWicoCraftSave.GetSection(sOreSection);
            if (sOreSave == sLastOreLoad) return -2; // no changes in saved info.

            if (sOreSave.Length < 1)           return -3;

            oreLocs.Clear();
            double x1, y1, z1;

            sLastOreLoad = sOreSave;
            string[] atheStorage = sOreSave.Split('\n');

            int iLine = 0;
            /*
            // Trick using a "local method", to get the next line from the array `atheStorage`.
            Func<string> getLine = () =>
            {
                return (iLine >= 0 && atheStorage.Length > iLine ? atheStorage[iLine++] : null);
            };
            */
            int iCount = -1;
            if (atheStorage.Length < 2)
                return -4; // nothing to parse

            //            Echo(atheStorage[iLine]);
            iCount = Convert.ToInt32(atheStorage[iLine++]);
            //            Echo("total="+iCount);

            for (int j = 0; j < iCount; j++)
            {
                //                Echo("#="+j);

                //                Echo(atheStorage[iLine]);
                long eId = Convert.ToInt64(atheStorage[iLine++]);

                int oreID = Convert.ToInt32(atheStorage[iLine++]);

                //                Echo(atheStorage[iLine]);
                ParseVector3d(atheStorage[iLine++], out x1, out y1, out z1);
                Vector3D position = new Vector3D(x1, y1, z1);

                //                Echo(atheStorage[iLine]);
                ParseVector3d(atheStorage[iLine++], out x1, out y1, out z1);
                Vector3D vector = new Vector3D(x1, y1, z1);

                //                Echo(atheStorage[iLine]);
                long detectionType = Convert.ToInt64(atheStorage[iLine++]);

                OreLocInfo ore = new OreLocInfo();
                ore.AstEntityId = eId;
                ore.oreId = oreID;
                ore.position = position;
                ore.vector = vector;
                ore.detectionType = detectionType;
                oreLocs.Add(ore);
                //                Echo("----");
            }
            return iCount;
        }


        void OreSerialize()
        {
            if (iniWicoCraftSave == null)
            {
                return;
            }
            string S1 = "";
            S1 += oreLocs.Count + "\n";
            for (int i1 = 0; i1 < oreLocs.Count; i1++)
            {
                S1 += oreLocs[i1].AstEntityId.ToString() + "\n";
                S1 += oreLocs[i1].oreId.ToString() + "\n";
                S1 += Vector3DToString(oreLocs[i1].position) + "\n";
                S1 += Vector3DToString(oreLocs[i1].vector) + "\n";
                S1 += oreLocs[i1].detectionType.ToString() + "\n";
            }
            if (sLastOreLoad != S1)
            {
                iniWicoCraftSave.WriteSection(sOreSection, S1);
            }
            else
            {
                if (bVerboseSerialize) Echo("Not saving ORE: Same");
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
            OreSerialize();
            // transmit found location...
            antSend("WICO:ORE:" + Me.CubeGrid.EntityId.ToString() + ":" + asteroidId + ":" + OreID + ":" + Vector3DToString(Position) + ":" + Vector3DToString(vVec) + ":" + detectionType.ToString());

            // write data back to text panel if changed..
        }

        void dumpOreLocs()
        {
            for (int i = 0; i < oreLocs.Count; i++)
            {
                Echo(oreName(oreLocs[i].oreId) + ":" + oreLocs[i].position.ToString() + ":" + oreLocs[i].detectionType.ToString());
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
        long[] lOreDesirability = { 0, 100, 95, 75, 55, 45, 45, 45, 45, 45, 45, -1 };

        void initOreInfo()
        {
            oreInfos.Clear();
            // read from text panel..

            //iff empty text panel, init
            for (int l = 0; l < aOres.Length; l++)
            {
                OreInfo oi = new OreInfo();
                oi.oreID = l;
                oi.oreName = aOres[l];
                oi.desireability = lOreDesirability[l];
                oi.bFound = false;
                oi.localAmount = 0;
                oreInfos.Add(oi);
            }
            // write data back to text panel if changed.
        }

        string oreName(int oreId)
        {
            for (int i = 0; i < oreInfos.Count; i++)
                if (oreInfos[i].oreID == oreId)
                    return oreInfos[i].oreName;

            return "INVALID ID";
        }

        void clearOreAmounts()
        {
            if (oreInfos.Count < 1)
                initOreInfo();
            for (int i = 0; i < oreInfos.Count; i++)
            {
                oreInfos[i].localAmount = 0;
            }
        }

        void addOreAmount(string sOre, double lAmount, bool bNoFind = false)
        {
 //       	Echo("addOreAmount:" + sOre + ":" + lAmount.ToString());
            if (oreInfos.Count < 1)
                initOreInfo();

            for (int i = 0; i < oreInfos.Count; i++)
            {
                //		Echo(oreInfos[i].oreName);
                if (oreInfos[i].oreName == sOre)
                {
 //                   Echo("Is in list!");
 //                   Echo("Current amount=" + oreInfos[i].localAmount);
                    if (!bNoFind && lAmount > 0 && !oreInfos[i].bFound)
                    {
                        oreInfos[i].bFound = true;
                        foundOre(i);
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

        void foundOre(int oreIndex)
        {
//            Echo("FIRST FIND!:" + oreInfos[oreIndex].oreName);
            if (oreInfos[oreIndex].desireability > 0)
            {
                // add ore found loc...
                MatrixD refOrientation = GetBlock2WorldTransform(gpsCenter);
                Vector3D vVec = Vector3D.Normalize(refOrientation.Forward);

                // but only if we got it inside asteroid.
                long astEntity = AsteroidFindNearest(true);
                if(astEntity > 0)  OreAddLoc(astEntity, oreIndex, gpsCenter.GetPosition(), vVec, 69);
            }
        }

        void dumpFoundOre()
        {
            Echo("Ore Contents:");
            for (int i = 0; i < oreInfos.Count; i++)
            {
                if (oreInfos[i].localAmount > 0)
                    Echo(oreInfos[i].oreName + " " + oreInfos[i].localAmount);
            }
        }

        // uses lContainers from WicoCargo
        void doCargoOreCheck()
        {
            if (lContainers.Count < 1)
                initCargoCheck();

            if (lContainers.Count < 1)
            {
                // No cargo containers found.
                return;
            }
            clearOreAmounts();
//            Echo(lContainers.Count + " Containers");
            for (int i = 0; i < lContainers.Count; i++)
            {
                IMyInventory inv = lContainers[i].GetInventory(0);
                var items = inv.GetItems();
                // go through all items
                for (int i2 = 0; i2 < items.Count; i2++)
                {
                    var item = items[i2];

                    string str;
                    str = item.Content.TypeId.ToString();
                    string[] aS = str.Split('_');
                    string sType = aS[1];

                    str = item.Content.SubtypeName + " " + sType + " " + item.Amount;
                    //			StatusLog("  " + str,getTextBlock(CargoStatus));
                    //			Echo(" " + str);
                    //			if (item.Content.SubtypeName == "Stone" && item.Content.ToString().Contains("Ore"))
//                    Echo(item.Content.ToString());
                    if (item.Content.ToString().Contains("Ore"))
                    {
//                        Echo("Adding " + item.Content.SubtypeName);
                        addOreAmount(item.Content.SubtypeName, (double)item.Amount);
                        //				stoneamount+=(double)item.Amount;
                    }
                }

            }

        }

    }
}