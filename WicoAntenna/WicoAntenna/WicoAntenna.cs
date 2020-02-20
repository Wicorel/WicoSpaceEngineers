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
            iNIHolder.GetValue(sAntennaSection, "CommunicationsStealth", ref CommunicationsStealth, false);
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

        //string sLastReceivedMessage = "";
        
        /// <summary>
        /// Check if there are any messages that were waiting to be processed by modules and clear them if needed.
        /// </summary>
        void AntennaCheckOldMessages() // OBSOLETE
        {
            /*
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
            */
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
            return true;
            /* Obsolete with the new IGC
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
            */
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
                /* Removed in 1.193.100
                if (a.AttachedProgrammableBlock > 0 || bAll)
                {
                    a.Enabled = true;
                }
                */
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
                /* Removed in 1.193.100
                if (a1.AttachedProgrammableBlock > 0 || bAll)
                */
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
            /* Removed in 1.193.100

        foreach (var a1 in antennaList)
        {
            if (a1.AttachedProgrammableBlock == Me.EntityId )
            {
                // return the position of one we are listening to
                return a1.GetPosition();
            }
        }
        */
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
                /* Removed in 1.193.100
            if (a.AttachedProgrammableBlock > 0 || bAll)
            */
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

//    List<string> lPendingMessages = new List<string>();


        /// <summary>
        /// IGC broadcast send a message
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        void antSend(string tag, string message)
        {
            IGC.SendBroadcastMessage(tag, message);
        }

        /// <summary>
        /// IGC Unicast send a message
        /// </summary>
        /// <param name="targetID"></param>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        void antSend(long targetID, string tag, string message)
        {
            IGC.SendUnicastMessage(targetID, tag, message);
        }
        #endregion



    }
}