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
        List<BaseInfo> baseList = new List<BaseInfo>();

        const string sBaseSavedListSection = "BASE1.0";

        /*
         * TODO:
         * 
         * check validity of saved bases every once in a while (age).
         * add info like source, sink
         * get messages for 'base moving', etc
         * 
         * 
         */

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

        void BaseInitInfo()
        {
            baseList.Clear();
            // load from text panel
            BaseDeserialize();
        }

        string sLastBaseLoad = "";

        void BaseSerialize()
        {
            if (iniWicoCraftSave == null) return;

            string sb="";
            sb += baseList.Count + "\n";
            for (int i = 0; i < baseList.Count; i++)
            {
                sb += baseList[i].baseId.ToString() + "\n";
                sb += baseList[i].baseName + "\n";
                sb += Vector3DToString(baseList[i].position) + "\n";
                sb += baseList[i].bJumpCapable.ToString() + "\n";
            }
            iniWicoCraftSave.WriteSection(sBaseSavedListSection, sb);

        }

        int BaseDeserialize()
        {
//            Echo("BD()");
            if (iniWicoCraftSave == null)
            {
                return -2;
            }

 //           string sSection = iniWicoCraftSave.GetSection(sBaseSection);

//            Echo("BD():A");
            //            string[] atheStorage = sSection.Split('\n');
            string sBaseSave= iniWicoCraftSave.GetSection(sBaseSavedListSection);
            string[] atheStorage = iniWicoCraftSave.GetLines(sBaseSavedListSection);
            //            Echo("BD():B");
            if (sBaseSave == sLastBaseLoad)
            {
                return -3; // no changes in saved info.
            }

            sLastBaseLoad = sBaseSave;

            double x1, y1, z1;
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
                return -1; // nothing to parse
//            Echo("BD():C");
//            Echo(atheStorage.Count() + " Lines");

            for (int j0 = 0; j0 < atheStorage.Count(); j0++)
                Echo(atheStorage[j0]);

//            Echo(atheStorage[iLine]);
//            Echo("total bases=" + iCount);

            iCount = Convert.ToInt32(atheStorage[iLine++]);

            for (int j1 = 0; j1 < iCount && j1<atheStorage.Count(); j1++)
            {
                                Echo("#="+j1);

                long eId;
                                Echo(atheStorage[iLine]);
                eId = Convert.ToInt64(atheStorage[iLine++]);

                string sBaseName=atheStorage[iLine++];

                Vector3D position;
                ParseVector3d(atheStorage[iLine++], out x1, out y1, out z1);
                position = new Vector3D(x1, y1, z1);

                bool bJumpCapable = atheStorage[iLine++].ToLower().Contains("true") ? true : false;

                BaseInfo b1 = new BaseInfo();
                b1.baseId = eId;
                b1.baseName = sBaseName;
                b1.position = position;
                b1.bJumpCapable = bJumpCapable;

                baseList.Add(b1);
            }

            return iCount;
        }

        void BaseAdd(long baseId, string baseName, Vector3D Position, bool bJumpCapable = false)
        {
            if (baseList.Count < 1)
                BaseInitInfo();

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
            for (int i=0; i<baseList.Count; i++)
            {
                if(baseList[i].baseId==baseId)
                {
                    baseList[i].baseName = baseName;
                    baseList[i].position = Position;
                    baseList[i].bJumpCapable = bJumpCapable;
                    return;
                }
            }
            // else not found.  add it.
            baseList.Add(basei);

            // write data back to text panel if changed..
            BaseSerialize();
        }

        string baseInfoString()
        {
            string s1;
            if (baseList.Count == 0)
                return "No Known Bases";
            if(baseList.Count>1)
                s1=  baseList.Count.ToString() +" Known Bases\n";
            else s1=  baseList.Count.ToString() +" Known Base\n";

            for(int i=0;i<baseList.Count; i++)
            {
//                s += baseList[i].baseId + ":";
                s1 += baseList[i].baseName + ":";
                s1+= Vector3DToString(baseList[i].position) +":";
                s1 += "\n";
            }
            return s1;
        }

        double dBaseRequestTransmitWait = 25; //seconds between active transmits

        double dBaseRequestLastTransmit = -1;

        void checkBases(bool bForceRequest=false)
        {
            string sName = Me.CubeGrid.CustomName;
            Vector3D vPosition = Me.GetPosition();
            if (shipOrientationBlock != null)
            {
                sName = shipOrientationBlock.CubeGrid.CustomName;
                vPosition = shipOrientationBlock.GetPosition();
            }
            if (dBaseRequestLastTransmit > dBaseRequestTransmitWait || bForceRequest)
            {
                dBaseRequestLastTransmit = 0;
                antSend("WICO:BASE?:" + sName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(vPosition));
            }
            else
            {
                if (dBaseRequestLastTransmit < 0)
                {
                    // first-time init
                    dBaseRequestLastTransmit = Me.EntityId % dBaseRequestTransmitWait; // randomize initial send

                }
                if (baseList.Count < 1)
                    dBaseRequestLastTransmit += Runtime.TimeSinceLastRun.TotalSeconds;
            }
        }

        float RangeToNearestBase()
        {
            double bestRange = double.MaxValue;
            int iBest = BaseIndexOf(BaseFindNearest());
            if (iBest >= 0 && shipOrientationBlock!=null)
            {
                bestRange = (shipOrientationBlock.GetPosition() - baseList[iBest].position).Length();
            }
            return (float) bestRange;
        }
        long BaseFindNearest()
        {
            int iBest = -1;
            if (shipOrientationBlock == null) return iBest;

            double distanceSQ = double.MaxValue;
//sInitResults += baseList.Count + " Bases";
            for(int i=0;i<baseList.Count;i++)
            {
                double curDistanceSQ = Vector3D.DistanceSquared(baseList[i].position, shipOrientationBlock.GetPosition());
                if( curDistanceSQ<distanceSQ)
                {
//                    sInitResults += " Choosing" + baseList[i].baseName;
                    iBest = i;
                    distanceSQ = curDistanceSQ;
                }
            }
            if (iBest < 0) return 0;
            else return baseList[iBest].baseId;

        }

        long BaseFindBest()
        {
            return BaseFindNearest();
        }


        int BaseIndexOf(long baseID)
        {
            for(int i1=0;i1<baseList.Count;i1++)
            {
                if (baseList[i1].baseId == baseID)
                    return i1;
            }
            return -1;
        }

        Vector3D BasePositionOf(long baseId)
        {
            Vector3D vPos = new Vector3D();
            for (int i1 = 0; i1 < baseList.Count; i1++)
                if (baseList[i1].baseId == baseId)
                    return baseList[i1].position;
            return vPos;
        }

        bool BaseProcessMessages(string sMessage)
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
                    if (aMessage[1] == "BASE")
                    {
                        // base reponds with BASE information
                        //antSend("WICO:BASE:" + Me.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(shipOrientationBlock.GetPosition() +":"+bJumpCapable)XXX

                        // 2      3   4         5          6           7+
                        // name, ID, position, velocity, Jump Capable, Source, Sink
                        // source and sink need to have "priorities".  support vechicle can take ore from a miner drone.  and then it can deliver to a base.
                        //
                        //                            Echo("BASE says hello!");
                        int iOffset = 2;
                        string sName = aMessage[iOffset++];

                        long id = 0;
                        long.TryParse(aMessage[iOffset++], out id);

                        x1 = Convert.ToDouble(aMessage[iOffset++]);
                        y1 = Convert.ToDouble(aMessage[iOffset++]);
                        z1 = Convert.ToDouble(aMessage[iOffset++]);
                        Vector3D vPosition = new Vector3D(x1, y1, z1);

                        bool bJumpCapable = stringToBool(aMessage[iOffset++]);

                        BaseAdd(id, sName, vPosition, bJumpCapable);
                        return true; // we processed it
                    }

                }
            }
            return false;
        }

    }
}