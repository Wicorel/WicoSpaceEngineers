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
        IMyBroadcastListener _BASEIGCChannel;

        void BaseInitInfo()
        {
            baseList.Clear();
            // load from text panel
            BaseDeserialize();
            _BASEIGCChannel = IGC.RegisterBroadcastListener("BASE");
            _BASEIGCChannel.SetMessageCallback(_BASEIGCChannel.Tag);
        }

        void BaseSerialize()
        {
            if (iniWicoCraftSave == null) return;

            iniWicoCraftSave.SetValue(sBaseSavedListSection, "count", baseList.Count);

            for (int i1 = 0; i1 < baseList.Count; i1++)
            {
                iniWicoCraftSave.SetValue(sBaseSavedListSection, "ID" + i1.ToString(), baseList[i1].baseId);
                iniWicoCraftSave.SetValue(sBaseSavedListSection, "name" + i1.ToString(), baseList[i1].baseName);
                iniWicoCraftSave.SetValue(sBaseSavedListSection, "position" + i1.ToString(), baseList[i1].position);
                iniWicoCraftSave.SetValue(sBaseSavedListSection, "Jumpable" + i1.ToString(), baseList[i1].bJumpCapable);
            }
        }

        int BaseDeserialize()
        {
            if (iniWicoCraftSave == null)
            {
                return -2;
            }

            int iCount = -1;
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

            if(bForceRequest)
            {
                // empty the list because somebody might have been deleted/destroyed/etc
                baseList.Clear();
                BaseSerialize();
            }
            if (shipOrientationBlock != null)
            {
                sName = shipOrientationBlock.CubeGrid.CustomName;
                vPosition = shipOrientationBlock.GetPosition();
            }
            if (dBaseRequestLastTransmit > dBaseRequestTransmitWait || bForceRequest)
            {
                dBaseRequestLastTransmit = 0;
                antSend("BASE?", sName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(vPosition));
//                antSend("WICO:BASE?:" + sName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(vPosition));
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

        bool BaseProcessIGCMessage()
        {
            if (!_BASEIGCChannel.HasPendingMessage)
                return false;
            Echo("Base Response");
            var igcMessage = _BASEIGCChannel.AcceptMessage();
            string sMessage = (string)igcMessage.Data;
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

            bool bJumpCapable = stringToBool(aMessage[iOffset++]);

            BaseAdd(id, sName, vPosition, bJumpCapable);

            return false;
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