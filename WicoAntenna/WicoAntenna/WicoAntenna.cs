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
        bool CommunicationsStealth = false;

        string sAntennaSection = "COMMUNICATIONS";

        void CommunicationsInitCustomData(INIHolder iNIHolder)
        {
            iNIHolder.GetValue(sAntennaSection, "CommunicationsStealth", ref CommunicationsStealth, true);
        }

        bool bGotAntennaName = false;

        List<IMyRadioAntenna> antennaList = new List<IMyRadioAntenna>();
        List<IMyLaserAntenna> antennaLList = new List<IMyLaserAntenna>();

        /// <summary>
        /// Initialize the antenna code. Also sets "OurName" to name of first found antenna
        /// </summary>
        /// <returns>string with antenna count</returns>
        string antennaInit()
        {
            antennaList.Clear();
            antennaLList.Clear();

            GetTargetBlocks<IMyRadioAntenna>(ref antennaList);
            GetTargetBlocks<IMyLaserAntenna>(ref antennaLList);
            for (int i1 = 0; i1 < antennaList.Count; ++i1)
            {
                if (antennaList[i1].CustomName.Contains("unused") || antennaList[i1].CustomData.Contains("unused"))
                    continue;
                if (!bGotAntennaName)
                {
                    OurName = "Wico " + antennaList[i1].CustomName.Split('!')[0].Trim();
                    bGotAntennaName = true;
                }
            }
            return "A" + antennaList.Count.ToString("0");
        }

        /// <summary>
        ///  Verify antenna stays on to fix keen bug where antenna will turn itself off when you try to remote control. Possibly obsolete if bug has been fixed
        /// </summary>
        void verifyAntenna()
        {
            for (int i = 0; i < antennaList.Count; i++)
            {
                antennaList[i].Enabled = true;
            }
        }

        string sLastReceivedMessage = "";
        
        /// <summary>
        /// Check if there are any messages that were waiting to be processed by modules and clear them if needed.
        /// </summary>
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

        /// <summary>
        /// debug.  Currently commented out
        /// </summary>
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

        /// <summary>
        /// Set the antenna with the highest radius to call this script.
        /// </summary>
        bool SetAntennaMe()
        {
            bool bGood = false;
            float maxRadius = 0;
            int iMax = -1;
            for(int i=0;i<antennaList.Count;i++)
            {
                if(antennaList[i].AttachedProgrammableBlock == Me.EntityId)
                {
                    // we are already set as a target, so stop looking
                    bGood = true;
                    iMax = i;
                    break;
                }
                if(antennaList[i].Radius>maxRadius && antennaList[i].AttachedProgrammableBlock==0)
                {
                    iMax = i;
                    maxRadius = antennaList[i].Radius;
                }
            }
            if (iMax >= 0)
            {
                if (antennaList[iMax].AttachedProgrammableBlock != Me.EntityId)
                    sInitResults += "\nSetting Antenna PB";
                antennaList[iMax].AttachedProgrammableBlock = Me.EntityId;
                bGood = true;
            }
            else
            {
                // no available antenna
            }
            return bGood;
        }

        /// <summary>
        /// Set All antennas to lower power mode
        /// </summary>
        /// <param name="bAll">Ensures All, or just ones that have script attached are also Enabled</param>
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

        /// <summary>
        /// Set antenna radius (power) to the specfied radius.
        /// </summary>
        /// <param name="fRadius">radius in meters.  Default 200</param>
        /// <param name="bAll">Set all antennas (true) or just ones that have script attached (default) (false)</param>
        void antennaSetRadius(float fRadius=200, bool bAll=false)
        {
            if (antennaList.Count < 1) antennaInit();
            foreach (var a1 in antennaList)
            {
                if (a1.AttachedProgrammableBlock > 0 || bAll)
                {
                    a1.Radius = fRadius;
                    a1.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Returns position of the antenna that we are attached to
        /// </summary>
        /// <returns>position of the antenna block, or empty</returns>
        Vector3D antennaPosition()
        {
            if (antennaList.Count < 1) antennaInit();
            foreach (var a1 in antennaList)
            {
                if (a1.AttachedProgrammableBlock == Me.EntityId )
                {
                    // return the position of one we are listening to
                    return a1.GetPosition();
                }
            }
            foreach (var a1 in antennaList)
            {
                // else any one will do
                return a1.GetPosition();
            }
            Vector3D vNone = new Vector3D();
            return vNone;
        }

        /// <summary>
        /// Internal: desired range of antennas when transmitting.
        /// </summary>
        float fAntennaDesiredRange = float.MaxValue;

        /// <summary>
        /// Sets the desired max power of the antenna(s)
        /// </summary>
        /// <param name="bAll">Sets all the antennas.  Default to set only the ones that have script attached</param>
        /// <param name="desiredRange">Range. Default is max</param>
        void antennaMaxPower(bool bAll=false, float desiredRange=float.MaxValue)
        {
//            if (antennaList==null || antennaList.Count < 1) antennaInit();
            if (desiredRange < 200) desiredRange = 200;
            fAntennaDesiredRange = desiredRange;

            // if silent mode
            // return;
            // else set range now
            AntennaSetDesiredPower(bAll);
        }

        void AntennaSetDesiredPower(bool bAll = false)
        {
            if (antennaList == null || antennaList.Count < 1) antennaInit();
            foreach (var a in antennaList)
            {
                if (a.AttachedProgrammableBlock > 0 || bAll)
                {
                    float maxPower = a.GetMaximum<float>("Radius");
                    if (fAntennaDesiredRange < maxPower) maxPower = fAntennaDesiredRange;
                    a.Radius = maxPower;
                    a.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Returns the number of antennas available
        /// </summary>
        /// <returns></returns>
        int AntennaCount()
        {
            if (antennaList.Count < 1) antennaInit();
            return (antennaList.Count);
        }

        #region AntennaSend

        List<string> lPendingMessages = new List<string>();

        /// <summary>
        /// Process any pending sends that are in the queue
        /// </summary>
        void processPendingSends()
        {
            if (lPendingMessages.Count > 0)
            {
                AntennaSetDesiredPower(); // another sub-module may have turned it down.
                Echo("pPS("+ lPendingMessages.Count+"):" + lPendingMessages[0]);
                AntennaSetDesiredPower();
                antSend(lPendingMessages[0],false);
                lPendingMessages.RemoveAt(0);
            }
            if (lPendingMessages.Count > 0)
            {
                Echo("Pending Send.  Request FAST!");
                AntennaSetDesiredPower();
                bWantFast = true; // if there are more, process quickly
            }
            else
            { // we have no messages to send.  check for 'silent' mode
                if(CommunicationsStealth)
                {
                    antennaLowPower(true);
                }
            }
        }

        /// <summary>
        /// Send a message. Queues messages if it cannot be sent immediately
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="bQueue">Should the message be queued (true)</param>
        void antSend(string message, bool bQueue = true)
        {
//            Echo("Sending:\n" + message);
            bool bSent = false;
            if (antennaList.Count < 1) antennaInit();

            for (int i = 0; i < antennaList.Count; i++)
            { // try all available antennas

                // queue if we are in silent mode or this is not immediate send
                //                if (!bQueue || CommunicationsStealth)
                /*
                if (bQueue || CommunicationsStealth)
                {
                    // request antenna powerup
                    AntennaSetDesiredPower();
                }
                else
                */
                if(!bQueue)
                {
                    // try immediate send:
                    bSent = antennaList[i].TransmitMessage(message);
                    if (bSent)
                    {
//                        sStartupError += "\nMessage Sent";
                        break;
                    }
                }
            }
            if (!bSent)
            {
                if (AntennaCount() > 0)
                { // no sense queueing if we don't have any antennas.
                    lPendingMessages.Add(message);
//                    sStartupError += "\nMessage Queued";
                    Echo("Adding outgoing message to queue. Request FAST!");
                    Echo(message);
                    bWantFast = true;
                }
                else
                {
//                    sStartupError += "\nNo Antenna to send message";
                    Echo("Unable to send Antenna Message!");
                }
            }
        }
        #endregion

        #region AntennaReceive
        // This is for the module(s) that are set to be targets of messages from antennas

        List<string> lPendingIncomingMessages = new List<string>();

        /// <summary>
        /// Process pending receives.
        /// </summary>
        void processPendingReceives()
        {
            if (lPendingIncomingMessages.Count > 0)
            {
                if (sReceivedMessage == "")
                { // receiver signals processed by removing message
                    sReceivedMessage = lPendingIncomingMessages[0];
                    lPendingIncomingMessages.RemoveAt(0);
                }
                else Echo("Waiting for message to be processed");
                doTriggerMain();
            }
            if (lPendingIncomingMessages.Count > 0)
            {
                // if there are more, process quickly
//                doTriggerMain();
                // NOTE: In MAIN module, this should be bWantFast=true;
//                bWantFast = true; 
            }
        }

        /// <summary>
        /// Add the received message to the queue
        /// </summary>
        /// <param name="message">The message to add to the queue</param>
        void antReceive(string message)
        {
//            Echo("RECEIVE:\n" + message);
            lPendingIncomingMessages.Add(message);
            processPendingReceives();

//            doTriggerMain();
            //bWantFast = true;
        }

        void AntDisplayPendingMessages()
        {
            if (antennaList.Count > 0)
            {
                Echo(lPendingIncomingMessages.Count + " Pending Incoming Messages");
                for (int i = 0; i < lPendingIncomingMessages.Count; i++)
                    Echo(i + ":" + lPendingIncomingMessages[i]);
            }
            else
                Echo("No antennas found");
        }
        #endregion


    }
}