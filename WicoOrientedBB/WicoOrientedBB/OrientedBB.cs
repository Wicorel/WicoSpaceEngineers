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


        OrientedBoundingBoxFaces _obbf;// = OrientedBoundingBoxFaces(Me);

        void calculateGridBBPosition(IMyTerminalBlock sourceBlock = null)
        {
            if (sourceBlock == null) sourceBlock = shipOrientationBlock;
            _obbf = new OrientedBoundingBoxFaces(sourceBlock);
        }

        #region orientedBB

        // oriented bounding box from plaYer2k
        // Fixes/testing by Wicorel
        public struct OrientedBoundingBoxFaces
        {
            public Vector3D[] Corners;
            Vector3D localMax;
            Vector3D localMin;

            public Vector3D Position;

            static int[] PointsLookupRight = { 1, 3, 5, 7 };
            static int[] PointsLookupLeft = { 0, 2, 4, 6 };

            static int[] PointsLookupTop = { 2, 3, 6, 7 };
            static int[] PointsLookupBottom = { 0, 1, 4, 5 };

            static int[] PointsLookupBack = { 4, 5, 6, 7 };
            static int[] PointsLookupFront = { 0, 1, 2, 3 };

            static int[][] PointsLookup = {
        PointsLookupRight, PointsLookupLeft,
        PointsLookupTop, PointsLookupBottom,
        PointsLookupBack, PointsLookupFront
    };

            public OrientedBoundingBoxFaces(IMyTerminalBlock block)
            {
                //		points = new Vector3D[8];
                Corners = new Vector3D[8];

                // Reconstruct bounding box vectors in world dimensions
                // For a 1x1x1 cube where Min = Max we still have the 1x1x1 cube as bounding box,
                // hence half that gets added in each direction.
                //		var localMin = new Vector3D(block.CubeGrid.Min) - new Vector3D(0.5, 0.5, 0.5);
                localMin = new Vector3D(block.CubeGrid.Min) - new Vector3D(0.5, 0.5, 0.5);
                localMin *= block.CubeGrid.GridSize;
                //		var localMax = new Vector3D(block.CubeGrid.Max) + new Vector3D(0.5, 0.5, 0.5);
                localMax = new Vector3D(block.CubeGrid.Max) + new Vector3D(0.5, 0.5, 0.5);
                localMax *= block.CubeGrid.GridSize;



                // The reference-blocks orientation.
                var blockOrient = block.WorldMatrix.GetOrientation();

                // Get the matrix that transforms from the cube grids orientation to the blocks orientation.
                var matrix = block.CubeGrid.WorldMatrix.GetOrientation() * MatrixD.Transpose(blockOrient);

                // Transform the cubegrid-relative min/max to block-relative min/max.
                Vector3D.TransformNormal(ref localMin, ref matrix, out localMin);
                Vector3D.TransformNormal(ref localMax, ref matrix, out localMax);

                // Form clean min/max again.
                var tmpMin = Vector3D.Min(localMin, localMax);
                localMax = Vector3D.Max(localMin, localMax);
                localMin = tmpMin;


                // Get the center for the offset correction into worldspace.
                var center = block.CubeGrid.GetPosition();

                Vector3D tmp2;
                Vector3D tmp3;
                tmp2 = localMin;
                Vector3D.TransformNormal(ref tmp2, ref blockOrient, out tmp2);
                tmp2 += center;

                tmp3 = localMax;
                Vector3D.TransformNormal(ref tmp3, ref blockOrient, out tmp3);
                tmp3 += center;

                BoundingBox bb = new BoundingBox(tmp2, tmp3);
                Position = bb.Center;


                // Iterate over all edges and get them into world space.
                Vector3D tmp;
                for (int i = 0; i < 8; i++)
                {
                    tmp.X = ((i & 1) == 0 ? localMin : localMax).X;
                    tmp.Y = ((i & 2) == 0 ? localMin : localMax).Y;
                    tmp.Z = ((i & 4) == 0 ? localMin : localMax).Z;
                    Vector3D.TransformNormal(ref tmp, ref blockOrient, out tmp);
                    tmp += center;
                    Corners[i] = tmp;
                }
            }
            // face 0=right output order is  BL, TL, BR, TR
            // face 1=left output order is BL, TL, BR, TR
            // face 2=top output order is FTL, FR, BL, BR
            // face 3=bottom output order is FL, FR, BL, BR
            // face 4=back output order is BL, BR, TL, TR
            // face 5=front output order is BL, BR, TL, TR

            // Gets the points defining a face of the bounding box in world space.
            // 0 = right, 1 = left, 2 = top, 3 = bottom, 4 = back, 5 = front
            // alt: dir<<1 + sign with dir: 0 = X, 1 = Y, 2 = Z, sign: 0 = +, 1 = - (i.e. 3 = -Y (1<1+1))
            public void GetFaceCorners(int face, Vector3D[] points, int index = 0)
            {
                face %= PointsLookup.Length;
                for (int i = 0; i < PointsLookup[face].Length; i++)
                {
                    points[index++] = Corners[PointsLookup[face][i]];
                }
            }

        }
        #endregion


    }
}