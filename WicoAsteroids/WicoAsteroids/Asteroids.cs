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
        List<AsteroidInfo> asteroidsInfo = new List<AsteroidInfo>();

 //       AsteroidInfo currentAst = new AsteroidInfo();
        public class AsteroidInfo
        {
            public long EntityId;
            public BoundingBoxD BoundingBox;
            public Vector3D Position { get { return BoundingBox.Center; } }
        }

        void initAsteroidsInfo()
        {
            asteroidsInfo.Clear();
            // load from text panel...
        }

        void addAsteroid(MyDetectedEntityInfo thisDetectedInfo, bool bTransmitAsteroids = true)
        {
            if (thisDetectedInfo.IsEmpty() || thisDetectedInfo.Type != MyDetectedEntityType.Asteroid) return;

            bool bFound = false;

            for (int i = 0; i < asteroidsInfo.Count; i++)
            {
                if (asteroidsInfo[i].EntityId == (long)thisDetectedInfo.EntityId)
                {
                    bFound = true;
                    break;
                }
            }

            if (!bFound)
            {
                AsteroidInfo ai = new AsteroidInfo();
                ai.EntityId = thisDetectedInfo.EntityId;
                ai.BoundingBox = thisDetectedInfo.BoundingBox;
                asteroidsInfo.Add(ai);
                // info added: write output..

                if (thisDetectedInfo.Type == MyDetectedEntityType.Asteroid && bTransmitAsteroids)
                {
                    antSend("WICO:AST:" + SaveFile.EntityId.ToString() + ":" + thisDetectedInfo.EntityId.ToString() + ":" + Vector3DToString(thisDetectedInfo.Position) + ":" + thisDetectedInfo.BoundingBox.ToString());
                }
            }

        }

        bool AsteroidProcessLDEI(List<MyDetectedEntityInfo> lmyDEI)
        {
            bool bFoundAsteroid = false;
            for (int j = 0; j < lmyDEI.Count; j++)
            {
                if (lmyDEI[j].Type == MyDetectedEntityType.Asteroid)
                {
                    if (AsteroidProcessDEI(lmyDEI[j]))
                        bFoundAsteroid = true;
                }
            }
            return bFoundAsteroid;
        }

        bool AsteroidProcessDEI(MyDetectedEntityInfo dei)
        {
            bool bFoundAsteroid = false;
            if (dei.Type == MyDetectedEntityType.Asteroid)
            {
                addDetectedEntity(dei);
                addAsteroid(dei);
                bFoundAsteroid = true;
                string SX = "Found Asteroid";
                if (!bValidAsteroid)
                {
                    SX += " NEW!";// Echo("Found New Asteroid!");
                    bValidAsteroid = true;
                    vTargetAsteroid = dei.Position;
                }
            }
            return bFoundAsteroid;
        }
    }

}