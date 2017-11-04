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
        // source and sink need to have "priorities".  support vechicle can take ore from a miner drone.  and then it can deliver to a base.
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

    }
}