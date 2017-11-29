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
#region detectedentities
Dictionary<long, MyDetectedEntityInfo> detectedEntities = new Dictionary<long, MyDetectedEntityInfo>();

void addDetectedEntity(MyDetectedEntityInfo thisDetectedInfo)
{
	if (thisDetectedInfo.EntityId != 0)
	{
		if (!detectedEntities.ContainsKey(thisDetectedInfo.EntityId))
		{
			detectedEntities.Add(thisDetectedInfo.EntityId, thisDetectedInfo);
//			Echo("new entity found!" + thisDetectedInfo.Name);
		}
		else
		{
			detectedEntities[thisDetectedInfo.EntityId] = thisDetectedInfo;
//			Echo("Update Known Entity"+ thisDetectedInfo.Name);
		}
	}
	else Echo("Not adding: Zero Entity");

}

#endregion
        string deiInfo(MyDetectedEntityInfo dei)
        {
            string s = "";

            s += "ETBV";

            s += ":" + dei.EntityId.ToString();
            s += ":" + dei.TimeStamp;

            Vector3D min = dei.BoundingBox.Min;
            s += ":" + Vector3DToString(min);
            Vector3D max = dei.BoundingBox.Max;
            s += ":" + Vector3DToString(max);

            Vector3D vMaxd = (Vector3)dei.Velocity;
            s += ":" + Vector3DToString(vMaxd);
            return s;
        }

    }
}
