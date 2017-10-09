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
        List<OreLocInfo> oreLocs = new List<OreLocInfo>();

        public class OreLocInfo
        {
            public long AstEntityId;
            public int oreId;
            public Vector3D position;
            public Vector3D vector;
            public long detectionType;
        }

        void initOreLocInfo()
        {
            oreLocs.Clear();
            // load from text panel
        }

        void addOreLoc(long asteroidId, int OreID, Vector3D Position, Vector3D vVec, long detectionType)
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
            for (int i = 0; i < oreInfos.Count; i++)
            {
                oreInfos[i].localAmount = 0;
            }
        }

        void addOreAmount(string sOre, double lAmount, bool bNoFind = false)
        {
            //	Echo("addOreAmount:" + sOre + ":" + lAmount.ToString());
            for (int i = 0; i < oreInfos.Count; i++)
            {
                //		Echo(oreInfos[i].oreName);
                if (oreInfos[i].oreName == sOre)
                {
                    //			Echo("Is in list!");
                    //			Echo("Current amount=" + oreInfos[i].localAmount);
                    if (!bNoFind && lAmount > 0 && !oreInfos[i].bFound)
                    {
                        oreInfos[i].bFound = true;
                        foundOre(i);
                    }
                    //			else Echo("already 'found'");
                    oreInfos[i].localAmount += lAmount;
                }
            }
        }

        void foundOre(int oreIndex)
        {
            Echo("FIRST FIND!:" + oreInfos[oreIndex].oreName);
            if (oreInfos[oreIndex].desireability > 0)
            {
                // add ore found loc...
                MatrixD refOrientation = GetBlock2WorldTransform(gpsCenter);
                Vector3D vVec = Vector3D.Normalize(refOrientation.Forward);

                long astEntity = 0;
                if (currentAst != null) astEntity = currentAst.EntityId;
                addOreLoc(astEntity, oreIndex, gpsCenter.GetPosition(), vVec, 69);
                //		addOreLoc(currentAst.EntityId, oreIndex, gpsCenter.GetPosition(), vVec, 69);
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
    }
}