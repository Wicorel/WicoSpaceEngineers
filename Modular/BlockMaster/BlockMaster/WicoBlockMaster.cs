using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{

    partial class Program : MyGridProgram
    {

        #region WicoBlockMaster

        public class WicoBlockMaster //really "ship" master
        {
            Program _program;
            IMyGridTerminalSystem GridTerminalSystem;

            bool bMeGridOnly = false;

            public float fMaxWorldMps = 100f;

            public WicoBlockMaster(Program program, bool MeGridOnly=false)
            {
                _program = program;

                GridTerminalSystem = _program.GridTerminalSystem;
                bMeGridOnly = MeGridOnly;

                AddLocalBlockHandler(BlockParseHandler);
                AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddLoadHandler(LoadHandler);
                _program.AddSaveHandler(SaveHandler);

                //                _program.AddPostInitHandler(PostInitHandler());
                _program.AddPostInitHandler(LocalBlocksInit());
                _program.AddPostInitHandler(RemoteBlocksInit());

                DesiredMinTravelElevation = (float)_program.CustomDataIni.Get(_program.OurName, "MinTravelElevation").ToDouble(DesiredMinTravelElevation);
                _program.CustomDataIni.Set(_program.OurName, "MinTravelElevation", DesiredMinTravelElevation);

                fMaxWorldMps = (float)_program.CustomDataIni.Get(_program.OurName, "MaxWorldMps").ToDouble(fMaxWorldMps);
                _program.CustomDataIni.Set(_program.OurName, "MaxWorldMps", fMaxWorldMps);

                LoadLocalGrid();
            }
            void LoadHandler(MyIni theINI)
            {
            }
            void SaveHandler(MyIni theINI)
            {
            }
            #region SHIPCONTROLLER
            List<IMyShipController> shipControllers = new List<IMyShipController>();
            private IMyShipController MainShipController;
            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyShipController)
                {
                    // TODO: Check for other things for ignoring
                    // like toilets, etc.
                    shipControllers.Add(tb as IMyShipController);
//                    _program.Echo("WBM:BPH:Found shipController:" + tb.CustomName);
                }
            }

            public void LocalGridChangedHandler()
            {
                // forget what we thought we knew
                shipdimController = null;
                MainShipController = null;
                shipControllers.Clear();
            }
            //


            /// <summary>
            /// Checks if the specified block has been closed/deleted from the construct
            /// </summary>
            /// <param name="block">The block to check</param>
            /// <returns></returns>
            public bool IsClosed(IMyTerminalBlock block)
            {
                if (block == null || block.WorldMatrix == MatrixD.Identity) return true;
                return !(GridTerminalSystem.GetBlockWithId(block.EntityId) == block);
            }

            /// <summary>
            /// return the ship's remote control
            /// </summary>
            /// <returns></returns>
            public IMyRemoteControl GetRemoteControl()
            {
                foreach(var tb in shipControllers)
                {
                    if(tb is IMyRemoteControl && tb.IsUnderControl)
                    {

                        return tb as IMyRemoteControl;
                    }
                }
                foreach (var tb in shipControllers)
                {
                    if (tb is IMyRemoteControl)
                    {
                        return tb as IMyRemoteControl;
                    }
                }

                return null;
            }

            /// <summary>
            /// Returns the main ship controller
            /// </summary>
            /// <returns></returns>
            public IMyShipController GetMainController()
            {
//                _program.Echo("GetMainController()");
//                _program.Echo(shipControllers.Count.ToString() + " Ship Controllers");
                //  check for occupied, etc.
                foreach (var tb in shipControllers)
                {
                    if (tb.IsUnderControl && tb.CanControlShip)
                    {
                        // found a good one
                        MainShipController = tb;
                        break;
                    }
                }
                // TODO: ignore stuff like cyro, toilets, etc.
                if (MainShipController == null)
                {
                    // check in order of preference

                    foreach (var tb in shipControllers)
                    {
                        if (tb is IMyRemoteControl && tb.CanControlShip)
                        {
                            // found a good one
                            MainShipController = tb;
                            break;
                        }
                    }
                    // we didn't find one
                    if (MainShipController == null)
                    {
                        foreach (var tb in shipControllers)
                        {
                            if (tb is IMyCockpit && tb.CanControlShip)
                            {
                                // found a good one
                                MainShipController = tb;
                                break;
                            }
                        }
                    }
                    // we didn't find one
                    if (MainShipController == null)
                    {
                        foreach (var tb in shipControllers)
                        {
                            if (tb is IMyShipController && tb.CanControlShip)
                            {
                                // found a good one
                                MainShipController = tb;
                                break;
                            }
                        }
                    }
                    if (MainShipController != null)
                    {
                        // we found one
                        ShipDimensions(MainShipController);
                    }
                    else
                    {
//                        _program.Echo("GetMainController:No ship controller found");
//                        _program.ErrorLog("No Ship Controller Found");
                    }
                }

                return MainShipController;

            }

            public Vector3D CenterOfMass()
            {
                Vector3D com=_emptyV3D;
                var shipcontroller = GetMainController();
                if(shipcontroller!=null)
                    com= shipcontroller.CenterOfMass;
                else
                {
                    // No ship controller.
                    com=_program.Me.CubeGrid.GetPosition();
                }
                return com;
            }
            public double GetShipSpeed()
            {
                double shipspeed = -1;
                var shipcontroller = GetMainController();
                if (shipcontroller != null)
                    shipspeed = shipcontroller.GetShipSpeed();
                return shipspeed;
            }
            public Vector3D GetShipVelocity()
            {
                Vector3D velocity = _emptyV3D;
                var shipcontroller = GetMainController();
                if (shipcontroller != null)
                {
                    MyShipVelocities velocities = shipcontroller.GetShipVelocities();
                    velocity = velocities.LinearVelocity;
                }
                return velocity;
            }

            readonly StringBuilder ShipName = new StringBuilder(42);

            public StringBuilder GetShipName()
            {
                ShipName.Clear();
                if (GetMainController() != null)
                    ShipName.Append(GetMainController().CubeGrid.CustomName);
                else
                    ShipName.Append(_program.Me.CubeGrid.CustomName);
                return ShipName;
            }

            public Vector3D GetNaturalGravity()
            {
                Vector3D vNG = _emptyV3D;
                var shipcontroller = GetMainController();
                if (shipcontroller != null)
                    vNG = shipcontroller.GetNaturalGravity();
                return vNG;
            }

            public double GetAllPhysicalMass()
            {
                double effectiveMass = -1;
                effectiveMass = GetPhysicalMass();
                foreach(var grid in remoteCubeGrids)
                {
                    bool bGridDone = false;
                    foreach(var tb in gtsRemoteBlocks)
                    {
                        if(tb is IMyShipController && tb.CubeGrid==grid)
                        {
                            var sc = tb as IMyShipController;
                            MyShipMass myMass;
                            myMass = sc.CalculateShipMass();
                            effectiveMass += myMass.PhysicalMass;
                            bGridDone = true;
                            break;
                        }
                    }
                    if (bGridDone) break;
                }
                return effectiveMass;
            }

            public double GetPhysicalMass()
            {
                double effectiveMass = -1;
                var shipcontroller = GetMainController();
                if (shipcontroller != null)
                {
                    MyShipMass myMass;
                    myMass = shipcontroller.CalculateShipMass();
                    effectiveMass = myMass.PhysicalMass;
                }
                return effectiveMass;
            }

            #endregion

            public void DisplayInfo()
            {
                /*
                _program.Echo("LBlocks =" + localBlocksCount + " grids=" + localCubeGrids.Count);
                _program.Echo("RBlocks =" + remoteBlocksCount + " grids=" + remoteCubeGrids.Count);
                _program.Echo("PM=" + GetPhysicalMass().ToString("N2"));
                _program.Echo("APM=" + GetAllPhysicalMass().ToString("N2"));
                */
            }

            #region BLOCKHANDLING
            List<IMyTerminalBlock> gtsLocalBlocks = new List<IMyTerminalBlock>();
            public long localBlocksCount = 0;

            bool CollectRemote = false;

            List<IMyTerminalBlock> gtsRemoteBlocks = new List<IMyTerminalBlock>();
            long remoteBlocksCount = 0;

            List<IMyCubeGrid> localCubeGrids = new List<IMyCubeGrid>();

            List<IMyCubeGrid> remoteCubeGrids = new List<IMyCubeGrid>();

            List<Action<IMyTerminalBlock>> WicoLocalBlockParseHandlers = new List<Action<IMyTerminalBlock>>();
            List<Action<IMyTerminalBlock>> WicoRemoteBlockParseHandlers = new List<Action<IMyTerminalBlock>>();

            List<Action> WicoLocalBlockChangedHandlers = new List<Action>();
            List<Action> WicoRemoteBlockChangedHandlers = new List<Action>();

            List<Action> WicoLocalBlockDoneParsedHandlers = new List<Action>();
            List<Action> WicoRemoteBlockDoneParsedHandlers = new List<Action>();


            public bool AddLocalBlockHandler(Action<IMyTerminalBlock> handler)
            {
                if (!WicoLocalBlockParseHandlers.Contains(handler))
                    WicoLocalBlockParseHandlers.Add(handler);
                return true;
            }
            public void AddLocalBlockChangedHandler(Action handler)
            {
                if (!WicoLocalBlockChangedHandlers.Contains(handler))
                    WicoLocalBlockChangedHandlers.Add(handler);
            }

            public void AddLocalBlockParseDone(Action handler)
            {
                if (!WicoLocalBlockDoneParsedHandlers.Contains(handler))
                    WicoLocalBlockDoneParsedHandlers.Add(handler);
            }

            public bool AddRemoteBlockHandler(Action<IMyTerminalBlock> handler)
            {
                if (!WicoRemoteBlockParseHandlers.Contains(handler))
                    WicoRemoteBlockParseHandlers.Add(handler);
                return true;
            }
            public void AddRemoteBlocChangedHandler(Action handler)
            {
                if (!WicoRemoteBlockChangedHandlers.Contains(handler))
                    WicoRemoteBlockChangedHandlers.Add(handler);
            }
            public void AddRemoteBlockParseDone(Action handler)
            {
                if (!WicoRemoteBlockDoneParsedHandlers.Contains(handler))
                    WicoRemoteBlockDoneParsedHandlers.Add(handler);
            }
            public void SetMeGridOnly(bool bMeOnly = false)
            {
                bMeGridOnly = bMeOnly;
            }

            public void LoadLocalGrid()
            {
                localCubeGrids.Clear();
                gtsLocalBlocks.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsLocalBlocks, bLocalCheck);
            }

            bool bLocalCheck(IMyTerminalBlock tb)
            {
                bool bValid = true;
                if (!ValidBlock(tb)) return false;
                if (bMeGridOnly)
                    bValid = tb.CubeGrid.EntityId == _program.Me.CubeGrid.EntityId;
                else
                    bValid = tb.IsSameConstructAs(_program.Me);

                if (bValid)
                {
                    if (!localCubeGrids.Contains(tb.CubeGrid))
                    {
                        localCubeGrids.Add(tb.CubeGrid);
                    }
                }
                return bValid;
            }

            /// <summary>
            /// Call to initialize the local blocks and all subscribers
            /// </summary>
            public IEnumerator<bool> LocalBlocksInit()
            {
                yield return true;
                float fper = 0;

                if (gtsLocalBlocks.Count < 1)
                {
                    LoadLocalGrid();
                }

                fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                if (fper > 0.75f) yield return true;

                localBlocksCount = gtsLocalBlocks.Count;
//                _program.EchoInstructions("WBM:LBI: #Handlers=" + WicoLocalBlockParseHandlers.Count);
                foreach (var tb in gtsLocalBlocks)
                {
                    fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                    if (fper > 0.75f)
                    {
//                        _program.ErrorLog("Yield return");
                        yield return true;
                    }
//                    _program.EchoInstructions("WBM:LBI+TB:"+tb.CustomName);
                    foreach (var handler in WicoLocalBlockParseHandlers)
                    {
                        fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                        if (fper > 0.75f)
                        {
//                            _program.ErrorLog("Yield return");
                            yield return true;
                        }
                        handler(tb);
                    }
                }
                fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                if (fper > 0.75f)
                   yield return true;
                foreach(var handler in WicoLocalBlockDoneParsedHandlers)
                {
                    handler();
                }
//                _program.ErrorLog("WBM: LBI:EOR");
//                _program.EchoInstructions("WBM:LBI:EOR");
            }
            void LocalBlocksChanged()
            {
                foreach (var handler in WicoLocalBlockChangedHandlers)
                {
                    handler();
                }
            }

            /// <summary>
            /// Call to initialize the Remote blocks and all subscribers
            /// </summary>
            public IEnumerator<bool> RemoteBlocksInit()
            {
                yield return true;
                float fper = 0;

                gtsRemoteBlocks.Clear();
                remoteCubeGrids.Clear();

                if (CollectRemote)
                {
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsRemoteBlocks, (x1 => !x1.IsSameConstructAs(_program.Me) && ValidBlock(x1)));
                    fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                    if (fper > 0.75f) yield return true;

                    remoteBlocksCount = gtsRemoteBlocks.Count;
                    foreach (var tb in gtsRemoteBlocks)
                    {
                        if (!remoteCubeGrids.Contains(tb.CubeGrid))
                        {
                            remoteCubeGrids.Add(tb.CubeGrid);
                        }
                        fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                        if (fper > 0.75f)
                        {
                            //                        _program.ErrorLog("Yield return");
                            yield return true;
                        }
                        //                    _program.EchoInstructions("WBM:LBI+TB:"+tb.CustomName);
                        foreach (var handler in WicoRemoteBlockParseHandlers)
                        {
                            fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                            if (fper > 0.75f)
                            {
                                //                            _program.ErrorLog("Yield return");
                                yield return true;
                            }
                            handler(tb);
                        }
                    }
                    fper = _program.Runtime.CurrentInstructionCount / (float)_program.Runtime.MaxInstructionCount;
                    if (fper > 0.75f)
                        yield return true;
                    foreach (var handler in WicoRemoteBlockDoneParsedHandlers)
                    {
                        handler();
                    }
                }
            }
            void RemoteBlocksChanged()
            {
                foreach (var handler in WicoLocalBlockChangedHandlers)
                {
                    handler();
                }
            }

            public void SetCollectRemote(bool bUse=true)
            {
                CollectRemote = bUse;
            }
 
            public List<IMyTerminalBlock> GetBlocksContains<T>(string Keyword = null) where T : class
            {
                var Output = new List<IMyTerminalBlock>();
                if (gtsLocalBlocks.Count < 1) LocalBlocksInit();

                for (int e1 = 0; e1 < gtsLocalBlocks.Count; e1++)
                //                for (int e1 = 0; e1 < gtsAllBlocks.Count; e1++)
                {
                    if (gtsLocalBlocks[e1] is T
                        && Keyword != null && (gtsLocalBlocks[e1].CustomName.Contains(Keyword) || gtsLocalBlocks[e1].CustomData.Contains(Keyword))
                        )
                    {
                        Output.Add(gtsLocalBlocks[e1]);
                    }
                }
                return Output;
            }

            List<IMyTerminalBlock> gtsTestBlocks = new List<IMyTerminalBlock>();
            /// <summary>
            /// Call to check if the local grid needs to be re-initialized.
            /// </summary>
            /// <param name="bForceUpdate"></param>
            /// <returns></returns>
            public bool CalcLocalGridChange(bool bForceUpdate = false)
            {
                gtsTestBlocks.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsTestBlocks, (x1 => x1.IsSameConstructAs(_program.Me) && ValidBlock(x1)));
                //                _program.Echo("test block count=" + gtsTestBlocks.Count.ToString());
                if (localBlocksCount != gtsTestBlocks.Count || bForceUpdate)
                {
//                    _program.Echo("WBM:CGC:CHANGE DETECTED! New="+gtsTestBlocks.Count + " Old="+localBlocksCount);
                    LocalBlocksChanged(); // tell them something changed
                    localBlocksCount = gtsTestBlocks.Count;
                    gtsLocalBlocks = gtsTestBlocks;
                    foreach (var tb in gtsLocalBlocks)
                    {
                        foreach (var handler in WicoLocalBlockParseHandlers)
                        { // tell them about the new blocks
                            handler(tb);
                        }
                    }
                    foreach (var handler in WicoLocalBlockDoneParsedHandlers)
                    {
                        handler();
                    }
                    return true;
                }
                return false;
            }

            readonly Vector3D _emptyV3D=new Vector3D();

            bool ValidBlock(IMyTerminalBlock tb)
            {
                if (tb.GetPosition() == _emptyV3D)
                {
                    return false;
                }
                else return true;
            }

            /// <summary>
            /// Calculate if the remote blocks have changed and recalculate
            /// </summary>
            /// <returns></returns>
            public bool CalcRemoteGridChange()
            {
                List<IMyTerminalBlock> gtsTestBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gtsTestBlocks, (x1 => !x1.IsSameConstructAs(_program.Me)));
                if (remoteBlocksCount != gtsTestBlocks.Count)
                {
                    RemoteBlocksChanged();
                    remoteBlocksCount = gtsTestBlocks.Count;
                    gtsRemoteBlocks = gtsTestBlocks;
                    foreach (var tb in gtsRemoteBlocks)
                    {
                        foreach (var handler in WicoRemoteBlockParseHandlers)
                        {
                            handler(tb);
                        }
                    }
                    foreach (var handler in WicoRemoteBlockDoneParsedHandlers)
                    {
                        handler();
                    }
                    return true;
                }
                return false;
            }
            #endregion

            #region shipdim
            const float SMALL_BLOCK_VOLUME = 0.5f;
            const float LARGE_BLOCK_VOLUME = 2.5f;
            const float SMALL_BLOCK_LENGTH = 0.5f;
            const float LARGE_BLOCK_LENGTH = 2.5f;

            private float _length_blocks, _width_blocks, _height_blocks;
            private double _length, _width, _height;
            public float gridsize;
            private OrientedBoundingBoxFaces _obbf;

            IMyShipController shipdimController;
            void ShipDimensions(IMyShipController orientationBlock)//BoundingBox bb, double BlockMetricConversion)
            {
                shipdimController = orientationBlock;

                if (_program.Me.CubeGrid.GridSizeEnum.ToString().ToLower().Contains("small"))
                    gridsize = SMALL_BLOCK_LENGTH;
                else
                    gridsize = LARGE_BLOCK_LENGTH;

                _obbf = new OrientedBoundingBoxFaces(orientationBlock);
                Vector3D[] points = new Vector3D[4];
                _obbf.GetFaceCorners(OrientedBoundingBoxFaces.LookupFront, points); // 5 = front
                                                                                    // front output order is BL, BR, TL, TR
                _width = (points[0] - points[1]).Length();
                _height = (points[0] - points[2]).Length();
                _obbf.GetFaceCorners(0, points);
                // face 0=right output order is  BL, TL, BR, TR ???
                _length = (points[0] - points[2]).Length();

                _length_blocks = (float)(_length / gridsize);
                _width_blocks = (float)(_width / gridsize);
                _height_blocks = (float)(_height / gridsize);
            }
            public float LengthInBlocks()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return _length_blocks;
            }
            public double LengthInMeters()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return _length;
            }
            public float WidthInBlocks()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return _width_blocks;
            }
            public double WidthInMeters()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return _width;
            }
            public float HeightInBlocks()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return _height_blocks;
            }
            public double HeightInMeters()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return _height;
            }
            public double LargestSideInMeters()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                double largest = _height;
                if (_length > largest)
                    largest = _length;
                if (_width > largest)
                    largest = _width;
                return largest;
            }
            public double BlockMultiplier()
            {
                if (shipdimController == null) ShipDimensions(GetMainController());
                return gridsize;
            }
            #endregion

            /// <summary>
            /// Helper function.  Turn blocks in list on or off
            /// </summary>
            /// <param name="blocks"></param>
            /// <param name="bOn"></param>
            public void BlocksOnOff(List<IMyTerminalBlock> blocks, bool bOn = true)
            {
                foreach (var b in blocks)
                {
                    IMyFunctionalBlock f = b as IMyFunctionalBlock;
                    if (f == null) continue;
                    f.Enabled = bOn;
                }
            }
            public float DesiredMinTravelElevation = 0; // zero means none

        }

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
            public const int LookupRight = 0;
            public const int LookupLeft = 1;
            public const int LookupTop = 2;
            public const int LookupBottom = 3;
            public const int LookupBack = 4;
            public const int LookupFront = 5;

            public OrientedBoundingBoxFaces(IMyTerminalBlock block)
            {
                Corners = new Vector3D[8];
                if (block == null)
                {
                    Position = new Vector3D();
                    localMin = new Vector3D();
                    localMax = new Vector3D();
                    return;
                }

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

            /// <summary>
            /// Get the corners for a specified face.
            /// </summary>
            /// <param name="face">0=right, 1=left, 2=top, 3=bottom, 4=back, 5=front</param>
            /// <param name="points">array of points to return. See implementation source for corner order</param>
            /// <param name="index">optional offset</param>
            public void GetFaceCorners(int face, Vector3D[] points, int index = 0)
            {
                face %= PointsLookup.Length;
                for (int i = 0; i < PointsLookup[face].Length; i++)
                {
                    points[index++] = Corners[PointsLookup[face][i]];
                }
            }

        }

        double CalculateYaw(Vector3D destination, IMyTerminalBlock Origin)
        {
            double yawAngle = 0;
            bool facingTarget = false;

            MatrixD refOrientation = GetBlock2WorldTransform(Origin);

            Vector3D vCenter = Origin.GetPosition();
            Vector3D vBack = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Backward);
            //            Vector3D vUp = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Up);
            Vector3D vRight = vCenter + 1.0 * Vector3D.Normalize(refOrientation.Right);
            Vector3D vLeft = vCenter - 1.0 * Vector3D.Normalize(refOrientation.Right);

            //           debugGPSOutput("vCenter", vCenter);
            //          debugGPSOutput("vBack", vBack);
            //           debugGPSOutput("vUp", vUp);
            //           debugGPSOutput("vRight", vRight);


            //          double centerTargetDistance = calculateDistance(vCenter, destination);
            //          double upTargetDistance = calculateDistance(vUp, destination);
            //          double backTargetDistance = calculateDistance(vBack, destination);
            //          double rightLocalDistance = calculateDistance(vRight, vCenter);
            double rightTargetDistance = calculateDistance(vRight, destination);

            double leftTargetDistance = calculateDistance(vLeft, destination);

            double yawLocalDistance = calculateDistance(vRight, vLeft);


            double centerTargetDistance = Vector3D.DistanceSquared(vCenter, destination);
            double backTargetDistance = Vector3D.DistanceSquared(vBack, destination);
            /*
            double upTargetDistance = Vector3D.DistanceSquared(vUp, destination);
            double rightLocalDistance = Vector3D.DistanceSquared(vRight, vCenter);
            double rightTargetDistance = Vector3D.DistanceSquared(vRight, destination);

            double leftTargetDistance = Vector3D.DistanceSquared(vLeft, destination);

            double yawLocalDistance = Vector3D.DistanceSquared(vRight, vLeft);
            */
            facingTarget = centerTargetDistance < backTargetDistance;

            yawAngle = (leftTargetDistance - rightTargetDistance) / yawLocalDistance;
            //            Echo("calc Angle=" + Math.Round(yawAngle, 5));

            if (!facingTarget)
            {
                //Echo("YAW:NOT FACING!"); 
                yawAngle += (yawAngle < 0) ? -1 : 1;
            }
            //	Echo("yawangle=" + Math.Round(yawAngle,5)); 
            return yawAngle;
        }

        double calculateDistance(Vector3D a, Vector3D b)
        {
            return Vector3D.Distance(a, b);
        }

        #region Grid2World
        // from http://forums.keenswh.com/threads/library-grid-to-world-coordinates.7284828/
        MatrixD GetGrid2WorldTransform(IMyCubeGrid grid)
        { Vector3D origin = grid.GridIntegerToWorld(new Vector3I(0, 0, 0)); Vector3D plusY = grid.GridIntegerToWorld(new Vector3I(0, 1, 0)) - origin; Vector3D plusZ = grid.GridIntegerToWorld(new Vector3I(0, 0, 1)) - origin; return MatrixD.CreateScale(grid.GridSize) * MatrixD.CreateWorld(origin, -plusZ, plusY); }
        MatrixD GetBlock2WorldTransform(IMyCubeBlock blk)
        { Matrix blk2grid; blk.Orientation.GetMatrix(out blk2grid); return blk2grid * MatrixD.CreateTranslation(((Vector3D)new Vector3D(blk.Min + blk.Max)) / 2.0) * GetGrid2WorldTransform(blk.CubeGrid); }
        #endregion

        #endregion
    }
}
