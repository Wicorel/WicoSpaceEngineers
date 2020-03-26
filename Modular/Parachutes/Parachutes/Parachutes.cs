using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {
        class Parachutes
        {
            List<IMyParachute> parachuteList = new List<IMyParachute>();


            Program thisProgram;
            public Parachutes(Program program)
            {
                thisProgram = program;

                thisProgram.wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                thisProgram.wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyParachute)
                {
                    // TODO: Ignore cutters, etc
                    parachuteList.Add(tb as IMyParachute);
                }
            }
            void LocalGridChangedHandler()
            {
                parachuteList.Clear();
            }


            // https://spaceengineerswiki.com/Parachute_Hatch#Terminal_Velocity
            // https://docs.google.com/spreadsheets/d/18yHagVY32ehsCK7mxdm6j46cgrv74dd4-3kmR2rsqvI/edit#gid=0
            public double CalculateTerminalVelocity(double mass, float gridsize, double gravity)
            {
                double terminal = 0;
                int count = parachuteList.Count;
                if (count < 1) return 0;
                if (gravity < 0.1) return 0;

                var atmo = parachuteList[0].Atmosphere;
                terminal = CalculateTerminalVelocity(mass, gridsize, gravity, atmo);

                return terminal;
            }

            public double CalculateTerminalVelocity(double mass, float gridsize, double gravity, float atmo)
            {
                double terminal = 0;
                int count = parachuteList.Count;
                if (count < 1) return 0;
                //                if (atmo < 0.1) atmo = 0.85f;
                var afterreefing = atmo - 0.6;
                if (afterreefing <= 0) return 0;
                //(log((10*(ATM-REEFLEVEL))-0.99)+5)*RADMULT*GRIDSIZE
                //                double diameter = (MathD.Log((10 * (ATM - REEFLEVEL)) - 0.99) + 5) * RADMULT * GRIDSIZE
                double diameter = (Math.Log((10 * (afterreefing)) - 0.99) + 5) * 8 * gridsize;
                double halfArea = (Math.PI * (diameter / 2));

                double area = halfArea * halfArea;
                terminal = Math.Sqrt((mass * gravity) / (area * count * atmo * 1.225 * gridsize));
                //                _program.Echo("Gravity=" + gravity.ToString("0.00") + " area=" + area.ToString("0.00"));
                //                _program.Echo("mass="+mass.ToString("0.00")+ " area="

                return terminal;
            }
            public void OpenChutes()
            {
                foreach (var chute in parachuteList)
                {
                    chute.OpenDoor();
                }
            }

            public Vector3D ChuteOrientation()
            {
                Vector3D orientation = new Vector3D();

                if (parachuteList.Count > 1)
                {
                    //Matrix or1;
                    //                    parachuteList[0].Orientation.GetMatrix(out or1);
                    //                    orientation = or1.Forward;
                    orientation = parachuteList[0].WorldMatrix.Forward;
                }
                return orientation;
            }
        }
    }
}
