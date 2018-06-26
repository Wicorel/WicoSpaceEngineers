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
                    localProjectorList[i].ShowOnlyBuildable = !bBuildable;

                    if (bEcho) Echo(localProjectorList[i].CustomName);
                    if (bEcho) Echo("Buildable:" + localProjectorList[i].BuildableBlocksCount);
                    if (bEcho) Echo("Remaining:" + localProjectorList[i].RemainingBlocks);
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

        void ProjectorsHorz(bool bIncrease = true)
        {
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].Enabled)
                {
                    Vector3I vOffset =
                    localProjectorList[i].ProjectionOffset;
                    if (bIncrease)
                        vOffset.X++;
                    else
                        vOffset.X--;
                    localProjectorList[i].ProjectionOffset = vOffset;
                }
            }

        }

        void ProjectorsVert(bool bIncrease = true)
        {
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].Enabled)
                {
                    Vector3I vOffset =
                    localProjectorList[i].ProjectionOffset;
                    if (bIncrease)
                        vOffset.Y++;
                    else
                        vOffset.Y--;
                    localProjectorList[i].ProjectionOffset = vOffset;
                }
            }

        }
        void ProjectorsFw(bool bIncrease = true)
        {
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].Enabled)
                {
                    Vector3I vOffset =
                    localProjectorList[i].ProjectionOffset;
                    if (bIncrease)
                        vOffset.Z++;
                    else
                        vOffset.Z--;
                    localProjectorList[i].ProjectionOffset = vOffset;
                }
            }

        }
        void ProjectorsRoll(bool bIncrease = true)
        {
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].Enabled)
                {
                    Vector3I vOffset =
                    localProjectorList[i].ProjectionRotation;
                    if (bIncrease)
                        vOffset.X++; // -2 -1 0 +1 +2
                    else
                        vOffset.X--;
                    localProjectorList[i].ProjectionRotation = vOffset;
                }
            }

        }
        void ProjectorsYaw(bool bIncrease = true)
        {
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].Enabled)
                {
                    Vector3I vOffset =
                    localProjectorList[i].ProjectionRotation;
                    if (bIncrease)
                        vOffset.Y++; // -2 -1 0 +1 +2
                    else
                        vOffset.Y--;
                    localProjectorList[i].ProjectionRotation = vOffset;
                }
            }
        }

        void ProjectorsPitch(bool bIncrease = true)
        {
            for (int i = 0; i < localProjectorList.Count; i++)
            {
                if (localProjectorList[i].Enabled)
                {
                    Vector3I vOffset =
                    localProjectorList[i].ProjectionRotation;
                    if (bIncrease)
                        vOffset.Z++; // -2 -1 0 +1 +2
                    else
                        vOffset.Z--;
                    localProjectorList[i].ProjectionRotation = vOffset;
                }
            }

        }
    }
}