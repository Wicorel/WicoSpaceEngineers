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
        List<baseInfo> baseList = new List<baseInfo>();

        public class baseInfo
        {
          //antSend("WICO:BASE:" + Me.CubeGrid.CustomName+":"+SaveFile.EntityId.ToString()+":"+Vector3DToString(gpsCenter.GetPosition())XXX

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
        }

        void initbaseInfo()
        {
            baseList.Clear();
            // load from text panel
        }

        void addBase(long baseId, string baseName, Vector3D Position, bool bJumpCapable = false)
        {
            if (baseList.Count < 1)
                initbaseInfo();

            baseInfo basei= new baseInfo();
            basei.baseId = baseId;
            basei.baseName = baseName;
            basei.position = Position;
            basei.bJumpCapable = bJumpCapable;

            //TODO:
            // Source:
            // Sink:
            for(int i=0; i<baseList.Count; i++)
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
        }

        string baseInfoString()
        {
            string s = "Known Bases:\n";
            for(int i=0;i<baseList.Count; i++)
            {
                s += baseList[i].baseName + " " + baseList[i].baseId + "\n";
            }
            return s;
        }

        void checkBases(bool bForceRequest=false)
        {
            if(bForceRequest || baseList.Count<1)
                antSend("WICO:BASE?:" + gpsCenter.CustomName + ":" + SaveFile.EntityId.ToString() + ":" + Vector3DToString(gpsCenter.GetPosition()));
        }

        int findBestBase()
        {
            int iBest = -1;
            double distanceSQ = double.MaxValue;
            for(int i=0;i<baseList.Count;i++)
            {
                double curDistanceSQ = Vector3D.DistanceSquared(baseList[i].position, gpsCenter.GetPosition());
                if( curDistanceSQ<distanceSQ)
                {
                    iBest = i;
                    distanceSQ = curDistanceSQ;
                }
            }
            return iBest;
        }

        long baseIdOf(int baseIndex)
        {
            long lID = 0;

            if(baseIndex>=0 & baseIndex<baseList.Count)
            {
                lID = baseList[baseIndex].baseId;
            }
            return lID;
        }

        string baseNameOf(int baseIndex)
        {
            string sName = "INVALID";
            if(baseIndex>=0 & baseIndex<baseList.Count)
            {
                sName = baseList[baseIndex].baseName;
            }
            return sName;
        }

        Vector3D basePositionOf(int baseIndex)
        {
            Vector3D vPos = new Vector3D();
            if(baseIndex>=0 & baseIndex<baseList.Count)
            {
                vPos = baseList[baseIndex].position;
            }
            return vPos;
        }

    }
}