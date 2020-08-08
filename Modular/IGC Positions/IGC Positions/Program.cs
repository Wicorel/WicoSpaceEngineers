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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        /// <summary>
        /// This is our unique ID for our message.  We've defined the format for the message data (it's just a string)
        /// </summary>
        string _broadCastTag = "Wicorel Positions";

        /// <summary>
        /// The broadcast listener for the channel we are interested in.
        /// </summary>
        IMyBroadcastListener _myBroadcastListener;

        double _elapsedSeconds = -1; // how long we have been running since last check
        const double _waitseconds = 5; // how long to wait between actions.

        public Program()
        {
            // let them know we are alive
            Echo("Creator");

            // register a broadcast channel for our tag
            _myBroadcastListener=IGC.RegisterBroadcastListener(_broadCastTag);

            // Ask to be called back --to Main()-- when a message is received
            _myBroadcastListener.SetMessageCallback(_broadCastTag); // the callback agrument does NOT need to be the same as the tag

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Grid Position Report");

            // if initiliazation or if time to send another message
            if(_elapsedSeconds<0 || _elapsedSeconds>_waitseconds)
            {
                string message = "";
                message += Me.CubeGrid.CustomName;
                message += ":" + Me.GetPosition().X;
                message += ":" + Me.GetPosition().Y;
                message += ":" + Me.GetPosition().Z;

                IGC.SendBroadcastMessage(_broadCastTag, message);
                Echo("Sending our position");
                _elapsedSeconds = 0;
            }
            else
            {
                // Add up the time.
                _elapsedSeconds += Runtime.TimeSinceLastRun.TotalSeconds;
            }

            // Process all incoming messages
            while (_myBroadcastListener.HasPendingMessage)
            {
                MyIGCMessage myIGCMessage = _myBroadcastListener.AcceptMessage();
                if(myIGCMessage.Tag==_broadCastTag)
                { // This is our tag
                    if(myIGCMessage.Data is string)
                    {
                        string message = myIGCMessage.Data.ToString();
                        string[] components = message.Split(':');
                        if(components.Length<4)
                        {
                            Echo("Invalid Command:(" + message + ")");
                            continue;
                        }
                        // 0=name
                        // 1=x
                        // 2=y
                        // 3=z
                        string sName = components[0];

                        double x, y, z;
                        bool xOk = double.TryParse(components[1].Trim(), out x);
                        bool yOk = double.TryParse(components[2].Trim(), out y);
                        bool zOk = double.TryParse(components[3].Trim(), out z);
                        if (!xOk || !yOk || !zOk)
                        {
                            Echo("Invalid Command:(" + message + ")");
                            continue;
                        }
                        RemotePositions newPosition;
                        newPosition.name = sName;
                        newPosition.position=new Vector3D(x, y, z);

                        if(_knownPositions.ContainsKey(myIGCMessage.Source))
                        {
                            _knownPositions[myIGCMessage.Source] = newPosition;
                        }
                        else
                        {
                            _knownPositions.Add(myIGCMessage.Source, newPosition);
                        }
                        Echo("Received IGC Public Message");
                    }
                    else // if(msg.Data is XXX)
                    {
                        // handle other data types here...
                    }
                }
                else
                {
                    // handle other tags here
                }
            }

            Echo(_knownPositions.Count.ToString() + " Known Positions");
            // Display all known positions and their distance
            foreach(var kvpPosition in _knownPositions)
            {
                Echo(kvpPosition.Value.name);
                Echo("   "+(Me.GetPosition() - kvpPosition.Value.position).Length().ToString("N0") +" Meters");
            }
        }

        struct RemotePositions
        {
            public string name;
            public Vector3D position;
        }

        Dictionary<long, RemotePositions> _knownPositions = new Dictionary<long, RemotePositions>();
    }
}
