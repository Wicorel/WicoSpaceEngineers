using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class OreDetectorReforged
        {
            // Interface for the MOD 
            // https://steamcommunity.com/sharedfiles/filedetails/?id=2790047923

            Program _program;
            OreInfoLocs _oreInfoLocs;

            OreDetectorReforged(Program program, OreInfoLocs oreInfoLocs)
            {
                _program = program;
                _oreInfoLocs = oreInfoLocs;
            }

            public void RequestDetection(Vector3D CenterPosition, long Radius, string minedOre, Action<List<Vector3D>> callBack, int count=1 )
            {
                BoundingSphereD SphereD = new BoundingSphereD(CenterPosition, Radius);
                _program.Me.SetValue("ReforgedDetectN", new ValueTuple<BoundingSphereD, string, int, Action<List<Vector3D>>>(SphereD, minedOre, count, callBack));

                /*
                void ReforgedDetectN(BoundingSphereD area, string minedOre, int count, Action<List<Vector3D>> callBack)
                {
                    Me.SetValue("ReforgedDetectN", new ValueTuple<BoundingSphereD, string, int, Action<List<Vector3D>>>(area, minedOre, count, callBack));
                }
                public void Main(string argument, UpdateType updateSource)
                {
                    Me.CustomData = "";
                    Me.CustomData += "[Nickel]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Nickel", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "[Iron]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Iron", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "[Magnesium]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Magnesium", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "[Ice]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Ice", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "[Cobalt]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Cobalt", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());

                    Me.CustomData += "[Platinum]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Platinum", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "[Uranium]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Uranium", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "Silicon]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Silicon", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());
                    Me.CustomData += "[Nickel]\n";

                    Me.CustomData += "[Gold]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Gold", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());

                    Me.CustomData += "[Silver]\n";
                    ReforgedDetectN(new BoundingSphereD(Me.GetPosition(), 3e4), "Silver", 1, (vs) => Me.CustomData += "\n" + vs.FirstOrDefault());

                }
                */
            }
        }
    }
}
