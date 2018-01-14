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

        List<AsteroidInfo> asteroidsInfo = new List<AsteroidInfo>();

        string sLastAsteroidLoad = "";
        const string sAsteroidSection = "ASTEROIDS";

        public class AsteroidInfo
        {
            public long EntityId;
            public BoundingBoxD BoundingBox;
            public Vector3D Position { get { return BoundingBox.Center; } }
        }

        void AsteroidSerialize()
        {
            //            Echo("AS()");
            if (iniWicoCraftSave == null) return;

            string S1 = ""; 
            S1 += asteroidsInfo.Count + "\n";
            for(int i=0;i<asteroidsInfo.Count;i++)
            {
                S1 += asteroidsInfo[i].EntityId.ToString() + "\n";
                S1 += Vector3DToString(asteroidsInfo[i].BoundingBox.Min)+"\n";
                S1 += Vector3DToString(asteroidsInfo[i].BoundingBox.Max) + "\n";
            }
//            Echo(sb);
            if (sLastAsteroidLoad != S1)
            {
                iniWicoCraftSave.WriteSection(sAsteroidSection, S1);
//                AsteroidSaveFile.WritePublicText(sb);
            }
            else
            {
                if (bVerboseSerialize) Echo("Not saving AST: Same");
            }
        }

        void AsteroidsDeserialize()
        {
            if (iniWicoCraftSave == null) return;

            string sAsteroidSave;
            /*
            if (AsteroidSaveFile == null)
                sAsteroidSave = Storage;
            else
            */
            //                sAsteroidSave = AsteroidSaveFile.GetPublicText();
            sAsteroidSave = iniWicoCraftSave.GetSection(sAsteroidSection);
//            sAsteroidSave = iniMiner.GetSection(sAsteroidSection);

            if (sAsteroidSave.Length < 1)
            {
//                Echo("Saved information not available");
                return;
            }

            if (sAsteroidSave == sLastAsteroidLoad) return; // no changes in saved info.

            asteroidsInfo.Clear();
            double x1, y1, z1;

            sLastAsteroidLoad = sAsteroidSave;
            string[] atheStorage = sAsteroidSave.Split('\n');

            int iLine=0;
            /*
            // Trick using a "local method", to get the next line from the array `atheStorage`.
            Func<string> getLine = () =>
            {
                return (iLine >= 0 && atheStorage.Length > iLine ? atheStorage[iLine++] : null);
            };
            */
            int iCount = -1;
            if (atheStorage.Length < 2)
                return; // nothing to parse

//            Echo(atheStorage[iLine]);
            iCount = Convert.ToInt32(atheStorage[iLine++]);
//            Echo("total="+iCount);

            for (int j=0;j<iCount;j++)
            {
//                Echo("#="+j);

                long eId;
//                Echo(atheStorage[iLine]);
                eId = Convert.ToInt64(atheStorage[iLine++]);

                BoundingBoxD box;
//                Echo(atheStorage[iLine]);
                ParseVector3d(atheStorage[iLine++], out x1, out y1, out z1);
                box.Min = new Vector3D(x1, y1, z1);

//                Echo(atheStorage[iLine]);
                ParseVector3d(atheStorage[iLine++], out x1, out y1, out z1);
                box.Max = new Vector3D(x1, y1, z1);

                AsteroidInfo ast = new AsteroidInfo();
                ast.EntityId = eId;
                ast.BoundingBox = box;
                asteroidsInfo.Add(ast);
//                Echo("----");
            }
//            Echo("EOL");
        }

        void initAsteroidsInfo()
        {
            asteroidsInfo.Clear();
            InitMinerInfo();

            // load from text panel...
            AsteroidsDeserialize();
        }

        void AsteroidAdd(long entityid, BoundingBoxD box, bool bTransmitAsteroid = true)
        {
            bool bFound = false;

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
                AsteroidSerialize();
                // info added: write output..

                if (bTransmitAsteroid)
                {
                    antSend("WICO:AST:" + SaveFile.EntityId.ToString() + ":" + entityid.ToString() + ":" + Vector3DToString(box.Min) + ":" + Vector3DToString(box.Max));
                }
            }

        }

        void addAsteroid(MyDetectedEntityInfo thisDetectedInfo, bool bTransmitAsteroid = true)
        {
            if (thisDetectedInfo.IsEmpty() || thisDetectedInfo.Type != MyDetectedEntityType.Asteroid) return;
            AsteroidAdd((long)thisDetectedInfo.EntityId, thisDetectedInfo.BoundingBox, bTransmitAsteroid);
        }

        bool AsteroidProcessLDEI(List<MyDetectedEntityInfo> lmyDEI)
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

        bool AsteroidProcessDEI(MyDetectedEntityInfo dei)
        {
            addDetectedEntity(dei);
            bool bFoundAsteroid = false;
            if (dei.Type == MyDetectedEntityType.Asteroid)
            {
                addAsteroid(dei);
                bFoundAsteroid = true;
            }
            return bFoundAsteroid;
        }

        long AsteroidFindNearest(bool bInsideOnly=false)
        {
            long AsteroidID = -1;
            if (gpsCenter == null) return AsteroidID;
            if (asteroidsInfo.Count < 1)
                AsteroidsDeserialize();

            double distanceSQ = double.MaxValue;
            foreach(var ast in asteroidsInfo)
            {
                if (bInsideOnly)
                {
                    if(ast.BoundingBox.Contains(gpsCenter.GetPosition())==ContainmentType.Contains)
                    {
                        AsteroidID = ast.EntityId;
                    }
                }
                else
                {
                    double curDistanceSQ = Vector3D.DistanceSquared(ast.Position, gpsCenter.GetPosition());
                    if (curDistanceSQ < distanceSQ)
                    {
                        AsteroidID = ast.EntityId;
                        distanceSQ = curDistanceSQ;
                    }
                }
            }
            return AsteroidID;
        }

        Vector3D AsteroidGetPosition(long AsteroidID)
        {
            if (asteroidsInfo.Count < 1)
                AsteroidsDeserialize();

            Vector3D pos = new Vector3D(0, 0, 0);
            for(int i=0;i<asteroidsInfo.Count;i++)
            {
                if (asteroidsInfo[i].EntityId == AsteroidID)
                    pos = asteroidsInfo[i].Position;
            }
            return pos;
        }

        BoundingBoxD AsteroidGetBB(long AsteroidID)
        {
            if (asteroidsInfo.Count < 1)
                AsteroidsDeserialize();

            BoundingBoxD box = new BoundingBoxD();
            for (int i = 0; i < asteroidsInfo.Count; i++)
            {
                if (asteroidsInfo[i].EntityId == AsteroidID)
                    box = asteroidsInfo[i].BoundingBox;
            }
            return box;
        }

        bool AsteroidProcessMessage(string sMessage)
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
                    if (aMessage[1] == "AST")
                    {
                       // antSend("WICO:AST:" + SaveFile.EntityId.ToString() + ":" + thisDetectedInfo.EntityId.ToString() + ":" + Vector3DToString(thisDetectedInfo.BoundingBox.Min) + ":" + Vector3DToString(thisDetectedInfo.BoundingBox.Max));

                        // 2      3           4                5:6:7                8:9:10
                        // name, srcshipID, asteroidentityID, boundingbox.min, boundingbox.max
                        // source and sink need to have "priorities".  support vechicle can take ore from a miner drone.  and then it can deliver to a base.
                        //
                        //                            Echo("BASE says hello!");
                        int iOffset = 2;

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
//                        sInitResults += "\nIncoming AST Processed!";
                        return true; // we processed it
                    }
                    else if(aMessage[1] == "AST?")
                    { // TODO: process request for known asteroids

                    }

                }
            }
            return false;
        }


        //INIHolder iniMiner;

        //IMyTextPanel MinerSaveFile = null;
        //const string MINER_SAVE_FILE_NAME = "Wico Miner Save";

        void InitMinerInfo()
        {
            /*
            if (iniMiner == null)
            {
                MinerSaveFile = null;
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                blocks = GetBlocksNamed<IMyTextPanel>(MINER_SAVE_FILE_NAME);

                if (blocks.Count > 1) Echo("Multiple blocks found: \"" + SAVE_FILE_NAME + "\"");
                else if (blocks.Count == 0)
                {
                    blocks = GetBlocksContains<IMyTextPanel>(MINER_SAVE_FILE_NAME);
                    if (blocks.Count == 1)
                        MinerSaveFile = blocks[0] as IMyTextPanel;
                    else
                    {
                        blocks = GetMeBlocksContains<IMyTextPanel>(MINER_SAVE_FILE_NAME);
                        if (blocks.Count == 1)
                            MinerSaveFile = blocks[0] as IMyTextPanel;
                    }
                }
                else MinerSaveFile = blocks[0] as IMyTextPanel;

                string sIni = "";
                if (MinerSaveFile == null)
                {
                    Echo(MINER_SAVE_FILE_NAME + " (TextPanel) is missing or Named incorrectly. ");
//                    return;
                }
                else sIni = MinerSaveFile.GetPublicText();

                iniMiner = new INIHolder(this, sIni);
            }
            */
        }

        void SaveMinerInfo()
        {
            /*
            if(iniMiner.IsDirty && MinerSaveFile!=null)
            {
                string sINI = iniMiner.GenerateINI(true);
                MinerSaveFile.WritePublicText(sINI);
            }
            */
        }

    }

}