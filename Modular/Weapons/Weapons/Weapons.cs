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
        public class Weapons
        {

            Program _program;
            WicoBlockMaster _wicoBlockMaster;

            //            bool _debug = false;
            List<IMySmallGatlingGun> gatlingsList = new List<IMySmallGatlingGun>(); // includes autocannon and gatling
            List<IMySmallMissileLauncherReload> missileList = new List<IMySmallMissileLauncherReload>(); // includes missile and railgun

            public Weapons(Program program, WicoBlockMaster wicoBlockMaster)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMySmallGatlingGun)
                {
                    gatlingsList.Add(tb as IMySmallGatlingGun);
                }
                if (tb is IMySmallMissileLauncherReload)
                {
                    missileList.Add(tb as IMySmallMissileLauncherReload);
                }
            }
            void LocalGridChangedHandler()
            {
                gatlingsList.Clear();
                missileList.Clear();
            }

            public void WeaponsFireForward()
            {
                // TODO: Only fire forward weapons.
                foreach(var gatling in gatlingsList)
                {
                    gatling.ShootOnce();
                }
                foreach (var missile in missileList)
                {
                    missile.ShootOnce();
                }
            }


        }
    }
}
