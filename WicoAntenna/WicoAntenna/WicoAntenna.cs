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
        #region Antenna
        bool bGotAntennaName = false;

        List<IMyRadioAntenna> antennaList = new List<IMyRadioAntenna>();
        List<IMyLaserAntenna> antennaLList = new List<IMyLaserAntenna>();

        string antennaInit()
        {
            antennaList.Clear();
            antennaLList.Clear();

            GetTargetBlocks<IMyRadioAntenna>(ref antennaList, "");
            GetTargetBlocks<IMyLaserAntenna>(ref antennaLList, "");
            for (int i = 0; i < antennaList.Count; ++i)
            {
                if (antennaList[i].CustomName.Contains("unused") || antennaList[i].CustomData.Contains("unused"))
                    continue;
                if (!bGotAntennaName)
                {
                    OurName = "Wico " + antennaList[i].CustomName.Split('!')[0].Trim();
                    bGotAntennaName = true;
                }
            }
            return "A" + antennaList.Count.ToString("0");
        }

        //// Verify antenna stays on to fix keen bug where antenna will turn itself off when you try to remote control
        void verifyAntenna()
        {
            for (int i = 0; i < antennaList.Count; i++)
            {
                antennaList[i].Enabled = true;
            }
        }
        #endregion

        void antennaLowPower()
        {
            if (antennaList.Count < 1) antennaInit();

            foreach (var a in antennaList)
            {
                a.Radius = 200;

//                a.SetValueFloat("Radius", 200);
            }
        }

        void antennaMaxPower()
        {
            if (antennaList.Count < 1) antennaInit();

            foreach (var a in antennaList)
            {
                float maxPower = a.GetMaximum<float>("Radius");
                a.Radius = maxPower;
//                a.SetValueFloat("Radius", maxPower);
            }
        }
        #region AntennaSend

        List<string> lPendingMessages = new List<string>();

        void processPendingSends()
        {
            if (lPendingMessages.Count > 0)
            {
                antSend(lPendingMessages[0]);
                lPendingMessages.RemoveAt(0);
            }
            if (lPendingMessages.Count > 0) bWantFast = true; // if there are more, process quickly
        }
        void antSend(string message)
        {
            Echo("Sending:\n" + message);
            bool bSent = false;
            if (antennaList.Count < 1) antennaInit();
            for (int i = 0; i < antennaList.Count; i++)
            { // try all available antennas
              // try immediate send:
                bSent = antennaList[i].TransmitMessage(message);
                if (bSent)
                    break;
            }
            if (!bSent)
            {
                lPendingMessages.Add(message);
                bWantFast = true;
            }
        }
        #endregion

        #region AntennaReceive

        List<string> lPendingIncomingMessages = new List<string>();

        void processPendingReceives()
        {
            if (lPendingIncomingMessages.Count > 0)
            {
                if (sReceivedMessage == "")
                { // receiver signals processed by removing message
                    sReceivedMessage = lPendingIncomingMessages[0];
                    lPendingIncomingMessages.RemoveAt(0);
                }
            }
            if (lPendingIncomingMessages.Count > 0)
            {
                // if there are more, process quickly
                doTriggerMain();
                // NOTE: In MAIN module, this should be bWantFast=true;
//                bWantFast = true; 
            }
        }
        void antReceive(string message)
        {
            Echo("RECEIVE:\n" + message);
            lPendingIncomingMessages.Add(message);
            processPendingReceives();

//            doTriggerMain();
            //bWantFast = true;
        }
        #endregion


    }
}