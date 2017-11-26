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
        List<IMyProjector> localProjectorList = new List<IMyProjector>();

        void initProjectors()
        {
            localProjectorList.Clear();
            List<IMyTerminalBlock> Local = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocksOfType<IMyProjector>(Local, (x1 => x1.CubeGrid == Me.CubeGrid));
            for (int i = 0; i < Local.Count; i++)
            {
                //		if (Local[i].IsWorking)
                {
                    if (Local[i].CustomName.Contains("!WCC") || Local[i].CustomData.Contains("!WCC")) continue; // ignore
                    localProjectorList.Add(Local[i] as IMyProjector);
                }
            }

        }

        bool doProjectorCheck(bool bEcho = true)
        {
            bool bBuilding = false;
            Echo("ProjectorCheck()");
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].IsProjecting)
                {

                    bBuilding = true;
                    // toggle: ShowOnlyBuildable
                    bool bBuildable = false;

//                    bBuildable=localProjectorList[i].GetValueBool("ShowOnlyBuildable");
                    bBuildable = localProjectorList[i].ShowOnlyBuildable;
//                    localProjectorList[i].SetValueBool("ShowOnlyBuildable", !bBuildable);
                    localProjectorList[i].ShowOnlyBuildable=!bBuildable;

                    if(bEcho) Echo(localProjectorList[i].CustomName);
                    if(bEcho) Echo("Buildable:" + localProjectorList[i].BuildableBlocksCount);
                    if(bEcho) Echo("Remaining:" + localProjectorList[i].RemainingBlocks);
                    if (bBuildable || localProjectorList[i].RemainingBlocks < 1)
                    {
                        bBuilding = true;
                    }
                }
            }
            return bBuilding;
        }

        void turnoffProjectors()
        {
		    for (int i = 0; i < localProjectorList.Count; i++)
		    {
			    localProjectorList[i].Enabled = false;
                localProjectorList[i].ShowOnlyBuildable = false;
		    }
        }
    }
}