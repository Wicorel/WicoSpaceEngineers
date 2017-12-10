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

        string sLastReceivedMessage = "";
        
        void AntennaCheckOldMessages()
        {
	        if (sReceivedMessage != "")
	        {
//		        Echo("Checking Message:\n" + sReceivedMessage);

		        if (sLastReceivedMessage == sReceivedMessage)
		        {
//			        Echo("Clearing last message: Not processed");
			        sReceivedMessage = ""; // clear it.
		        }
		        sLastReceivedMessage = sReceivedMessage;
	        }
	        else sLastReceivedMessage = "";
        }

        void DebugAntenna()
        {
/*
            Echo("Debug Antenna");
            Echo("Me=" + Me.EntityId.ToString());
            for(int i=0;i<antennaList.Count;i++)
            {
                Echo(antennaList[i].CustomName);
                Echo(antennaList[i].AttachedProgrammableBlock.ToString());
            }
*/
        }

        void SetAntennaMe()
        {
            float maxRadius = 0;
            int iMax = -1;
            for(int i=0;i<antennaList.Count;i++)
            {
                if(antennaList[i].Radius>maxRadius)
                {
                    iMax = i;
                    maxRadius = antennaList[i].Radius;
                }
                if(iMax>=0)
                {
                    if (antennaList[iMax].AttachedProgrammableBlock != Me.EntityId)
                        sInitResults += "\nSetting Antenna PB";
                    antennaList[iMax].AttachedProgrammableBlock = Me.EntityId;
                }
            }
        }

        void antennaLowPower(bool bAll = false)
        {
            if (antennaList.Count < 1) antennaInit();

            foreach (var a in antennaList)
            {
                a.Radius = 200;
                if (a.AttachedProgrammableBlock > 0 || bAll)
                {
                    a.Enabled = true;
                }
           }
        }

        void antennaSetRadius(float fRadius=200, bool bAll=false)
        {
            if (antennaList.Count < 1) antennaInit();
            foreach (var a in antennaList)
            {
                if (a.AttachedProgrammableBlock > 0 || bAll)
                {
                    a.Radius = fRadius;
                    a.Enabled = true;
                }
            }
        }

        Vector3D antennaPosition()
        {
           
            if (antennaList.Count < 1) antennaInit();
            foreach (var a in antennaList)
            {
                if (a.AttachedProgrammableBlock > 0 )
                {
                    // return the position of one we are listening to
                    return a.GetPosition();
                }
            }
            foreach (var a in antennaList)
            {
                // else any one will do
                return a.GetPosition();
            }
            Vector3D vNone = new Vector3D();
            return vNone;
        }

        void antennaMaxPower(bool bAll=false, float desiredRange=float.MaxValue)
        {
            if (antennaList.Count < 1) antennaInit();
            if (desiredRange < 200) desiredRange = 200;

            foreach (var a in antennaList)
            {
                if (a.AttachedProgrammableBlock > 0 || bAll)
                {
                    float maxPower = a.GetMaximum<float>("Radius");
                    if (desiredRange < maxPower) maxPower = desiredRange;
                    a.Radius = maxPower;
                    a.Enabled = true;
                }
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
//            Echo("Sending:\n" + message);
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
        // This is for the module(s) that are set to be targets of messages from antennas

        List<string> lPendingIncomingMessages = new List<string>();

        void processPendingReceives(bool bMain=false)
        {
            if (lPendingIncomingMessages.Count > 0)
            {
                if (sReceivedMessage == "")
                { // receiver signals processed by removing message
                    sReceivedMessage = lPendingIncomingMessages[0];
                    lPendingIncomingMessages.RemoveAt(0);
                    if (bMain)
                    {
                        bWantFast = true;
                    }
                    else
                    {
                       doTriggerMain();
                    }
                }
            }
            if (lPendingIncomingMessages.Count > 0)
            {
                // if there are more, process quickly
//                doTriggerMain();
                // NOTE: In MAIN module, this should be bWantFast=true;
//                bWantFast = true; 
            }
        }
        void antReceive(string message)
        {
//            Echo("RECEIVE:\n" + message);
            lPendingIncomingMessages.Add(message);
            processPendingReceives();

//            doTriggerMain();
            //bWantFast = true;
        }
        #endregion


    }
}