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

        ShipDimensions shipDim;

        void initShipDim()
        {
            if (Me.CubeGrid.GridSizeEnum.ToString().ToLower().Contains("small"))
                shipDim = new ShipDimensions(new BoundingBox(Me.CubeGrid.Min, Me.CubeGrid.Max), SMALL_BLOCK_LENGTH);
            else
                shipDim = new ShipDimensions(new BoundingBox(Me.CubeGrid.Min, Me.CubeGrid.Max), LARGE_BLOCK_LENGTH);
        }
        const float SMALL_BLOCK_VOLUME = 0.5f;
        const float LARGE_BLOCK_VOLUME = 2.5f;
        const double SMALL_BLOCK_LENGTH = 0.5;
        const double LARGE_BLOCK_LENGTH = 2.5;
        public class ShipDimensions
        {
            private float _length_blocks, _width_blocks, _height_blocks;
            private double _length, _width, _height;
            private double _block2metric;
            public ShipDimensions(BoundingBox bb, double BlockMetricConversion)
            {
                _length_blocks = bb.Size.GetDim(2) + 1;
                _width_blocks = bb.Size.GetDim(0) + 1;
                _height_blocks = bb.Size.GetDim(1) + 1;
                _block2metric = BlockMetricConversion;
                _length = Math.Round(_length_blocks * BlockMetricConversion, 2);
                _width = Math.Round(_width_blocks * BlockMetricConversion, 2);
                _height = Math.Round(_height_blocks * BlockMetricConversion, 2);
            }
            public float LengthInBlocks()
            {
                return _length_blocks;
            }
            public double LengthInMeters()
            {
                return _length;
            }
            public float WidthInBlocks()
            {
                return _width_blocks;
            }
            public double WidthInMeters()
            {
                return _width;
            }
            public float HeightInBlocks()
            {
                return _height_blocks;
            }
            public double HeightInMeters()
            {
                return _height;
            }
            public double BlockMultiplier()
            {
                return _block2metric;
            }
        }
    }
}