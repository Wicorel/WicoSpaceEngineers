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
        // SE V1.172
        #region doors
        List<IMyTerminalBlock> hangarDoorList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> outterairlockDoorList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> innerairlockDoorList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> allDoorList = new List<IMyTerminalBlock>();

        string sDoorHangar = "Hangar Door";
        string sDoorBay = "bay";
        string sDoorAirlock = "airlock";
        string sDoorAirlockOutside = "outside";
        string sDoorAirlockInside = "inside";
        string sDoorBridge = "bridge";

        void DoorInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sGridSection, "DoorHangar", ref sDoorHangar, true);
            iNIHolder.GetValue(sGridSection, "DoorBay", ref sDoorBay, true);
            iNIHolder.GetValue(sGridSection, "DoorAirlock", ref sDoorAirlock, true);
            iNIHolder.GetValue(sGridSection, "DoorAirlockOutside", ref sDoorAirlockOutside, true);
            iNIHolder.GetValue(sGridSection, "DoorAirlockInside", ref sDoorAirlockInside, true);
            iNIHolder.GetValue(sGridSection, "DoorBridge", ref sDoorBridge, true);
        }


        string doorsInit()
        {
            {
                allDoorList = new List<IMyTerminalBlock>();
                allDoorList= GetTargetBlocks<IMyDoor>();
//                GridTerminalSystem.GetBlocksOfType<IMyDoor>(allDoorList, (x1 => x1.CubeGrid == Me.CubeGrid));
            }

            for (int i = 0; i < allDoorList.Count; i++)
            {
                if (allDoorList[i].CustomName.Contains("Hangar Door"))
                    hangarDoorList.Add(allDoorList[i]);
                if (allDoorList[i].CustomName.ToLower().Contains("bay"))
                    outterairlockDoorList.Add(allDoorList[i]);

                if (allDoorList[i].CustomName.ToLower().Contains("airlock"))
                    if (allDoorList[i].CustomName.ToLower().Contains("outside"))
                        outterairlockDoorList.Add(allDoorList[i]);
                    else if (allDoorList[i].CustomName.ToLower().Contains("inside"))
                        innerairlockDoorList.Add(allDoorList[i]);

                if (allDoorList[i].CustomName.ToLower().Contains("bridge"))
                    innerairlockDoorList.Add(allDoorList[i]);

            }
            string s = "";
            s += "D" + allDoorList.Count.ToString("00");
            s += "h" + hangarDoorList.Count.ToString("00");
            s += "o" + outterairlockDoorList.Count.ToString("00");
            s += "i" + innerairlockDoorList.Count.ToString("00");
            return s;
        }

        void closeDoors(List<IMyTerminalBlock> DoorList)
        {
            for (int i = 0; i < DoorList.Count; i++)
            {
                IMyDoor d = DoorList[i] as IMyDoor;
                if (d == null) continue;
                if (d.Status == DoorStatus.Open || d.Status == DoorStatus.Opening) d.ApplyAction("Open");
            }
        }
        void openDoors(List<IMyTerminalBlock> DoorList)
        {
            for (int i = 0; i < DoorList.Count; i++)
            {
                IMyDoor d = DoorList[i] as IMyDoor;

                if (d == null) continue;
                if (d.Status == DoorStatus.Closed || d.Status == DoorStatus.Closing) d.ApplyAction("Open");
            }
        }

        #endregion

    }
}