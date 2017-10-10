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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        bool bInit = false;

        IMyTextPanel screen = null;

        StringBuilder strb =new StringBuilder();

        public void Main(string argument)
        {
            strb.Clear();

            if(!bInit)
            {
                sensorInit(false);
                screen=(IMyTextPanel)GridTerminalSystem.GetBlockWithName("Sensor Dump");
                if (screen == null)
                {
                    List<IMyTerminalBlock> gtsTestBlocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(gtsTestBlocks);
                    if (gtsTestBlocks.Count > 0)
                        screen = (IMyTextPanel)gtsTestBlocks[0];
                }
                bInit = true;
            }


            List<IMySensorBlock> lactiveSensors = activeSensors();

            if (lactiveSensors.Count < 1)
                strb.Append("No Sensors Active\n");
            else
                strb.Append(lactiveSensors.Count + " Active Sensors\n");
                
            foreach(var s in lactiveSensors)
            {
                strb.Append(s.CustomName + "\n");
  //              List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();
 		List<MyDetectedEntityInfo> lmyDEI = new List<MyDetectedEntityInfo>();
			s.DetectedEntities(lmyDEI);

                s.DetectedEntities(lmyDEI);
                echoDetectedEntities(lmyDEI);

            }
            if(screen !=null)    screen.WritePublicText(strb);
            Echo(strb.ToString());

        }

        void echoDetectedEntities(List<MyDetectedEntityInfo> lmyDEI)
        {

            for (int j = 0; j < lmyDEI.Count; j++)
            {
                strb.Append("Name: " + lmyDEI[j].Name);
                strb.AppendLine();
                strb.Append("Type: " + lmyDEI[j].Type);
                strb.AppendLine();
                strb.Append("Velocity: " + lmyDEI[j].Velocity.ToString("0.000"));
                strb.AppendLine();
                strb.Append("Relationship: " + lmyDEI[j].Relationship);
                strb.AppendLine();
                strb.Append("Size: " + lmyDEI[j].BoundingBox.Size.ToString("0.000"));
                strb.AppendLine();
                strb.Append("Position: " + lmyDEI[j].Position.ToString("0.000"));
                strb.AppendLine();
            }
        }

        long allBlocksCount = 0;
    }
}