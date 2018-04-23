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
        // Apr 05 2018 Add Wheel
        // 12/19 cleanup

        const int CRAFT_MODE_AUTO = 0;
        const int CRAFT_MODE_SLED = 2;
        const int CRAFT_MODE_ROTOR = 4;
        const int CRAFT_MODE_WHEEL = 8;
        const int CRAFT_MODE_HASGYROS = 16;

        const int CRAFT_MODE_ORBITAL = 32;
        const int CRAFT_MODE_ROCKET = 64;
        const int CRAFT_MODE_PET = 128;
        const int CRAFT_MODE_NAD = 256; // no auto dock
        const int CRAFT_MODE_NOAUTOGYRO = 512;
        const int CRAFT_MODE_NOPOWERMGMT = 1024;
        const int CRAFT_MODE_NOTANK = 2048;
        const int CRAFT_MODE_MASK = 0xfff;

        //int craft_operation = CRAFT_MODE_AUTO;

        string craftOperation()
        {
            string sResult = "FLAGS:";
            //  sResult+=craft_operation.ToString();
            if ((craft_operation & CRAFT_MODE_SLED) > 0)
                sResult += "SLED ";
            if ((craft_operation & CRAFT_MODE_ORBITAL) > 0)
                sResult += "ORBITAL ";
            if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
                sResult += "ROCKET ";
            if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
                sResult += "ROTOR ";
            if ((craft_operation & CRAFT_MODE_WHEEL) > 0)
                sResult += "WHEEL ";
            if ((craft_operation & CRAFT_MODE_PET) > 0)
                sResult += "PET ";
            if ((craft_operation & CRAFT_MODE_NAD) > 0)
                sResult += "NAD ";
            if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
                sResult += "NO Gyro ";
            if ((craft_operation & CRAFT_MODE_NOTANK) > 0)
                sResult += "No Tank ";
            if ((craft_operation & CRAFT_MODE_NOPOWERMGMT) > 0)
                sResult += "No Power ";
            return sResult;
        }


    }
}