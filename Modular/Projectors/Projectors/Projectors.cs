using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Projectors
        {
            List<IMyProjector> projectorList = new List<IMyProjector>();

            List<ManagedProjector> managedProjectors = new List<ManagedProjector>();

            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            bool MeGridOnly = false;

            public  Projectors(Program program, WicoBlockMaster wicoBlockMaster, bool bMeGridOnly=false)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;
                MeGridOnly = bMeGridOnly;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            MyIni projectorIni = new MyIni();
            const string ProjectorSection = "PrintHeadControl";

            public void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (MeGridOnly
                    && !(tb.CubeGrid.EntityId == _program.Me.CubeGrid.EntityId))
                    return;

                if (tb is IMyProjector)
                {

//                    if (Local[i].CustomName.Contains("!WCC") || Local[i].CustomData.Contains("!WCC")) continue; // ignore
                    projectorList.Add(tb as IMyProjector );

                    MyIniParseResult result;
                    if (!projectorIni.TryParse(tb.CustomData, out result))
                    {
                        //                        tb.CustomData = "";
                        projectorIni.Clear();
                    }


                    ManagedProjector managedProjector = new ManagedProjector();
                    managedProjector.projector = tb as IMyProjector;

                    managedProjector.bControlled = projectorIni.Get(ProjectorSection, "Controlled").ToBoolean(false);
                    projectorIni.Set(ProjectorSection, "Controlled", managedProjector.bControlled);

                    managedProjector.bIsPrintHead = projectorIni.Get(ProjectorSection, "IsPrintHead").ToBoolean(false);
                    projectorIni.Set(ProjectorSection, "IsPrintHead", managedProjector.bIsPrintHead);

                    managedProjector.Name = projectorIni.Get(ProjectorSection, "Name").ToString("UnNamed");
                    projectorIni.Set(ProjectorSection, "Name", managedProjector.Name);

                    managedProjector.blueprintName = projectorIni.Get(ProjectorSection, "blueprintName").ToString("UnNamed");
                    projectorIni.Set(ProjectorSection, "blueprintName", managedProjector.blueprintName);

                    if(managedProjector.bControlled)
                        managedProjectors.Add(managedProjector);

                    tb.CustomData = projectorIni.ToString();
                }
            }

            void LocalGridChangedHandler()
            {
                projectorList.Clear();
                managedProjectors.Clear();
            }

            public int Count()
            {
                return projectorList.Count;
            }
            public int ManagedCount()
            {
                return managedProjectors.Count;
            }

            public bool DoProjectorCheck(bool bEcho = true, bool bCheckPrintHead=false)
            {
                bool bBuilding = false;
                //                Echo("ProjectorCheck()");

                foreach (var managed in managedProjectors)
                {
                    if (!managed.bIsPrintHead || bCheckPrintHead)
                    {
                        if (managed.projector.IsProjecting)
                        {
                            bBuilding = true;
                            bool bBuildable = false;
                            bBuildable = managed.projector.ShowOnlyBuildable;
                            managed.projector.ShowOnlyBuildable = !bBuildable; // toggle setting to cause refresh
                            if (bEcho) _program.Echo(managed.projector.CustomName);
                            if (bEcho) if (bEcho) _program.Echo("Buildable:" + managed.projector.BuildableBlocksCount);
                            if (bEcho) if (bEcho) _program.Echo("Remaining:" + managed.projector.RemainingBlocks);
//                            if (bBuildable || managed.projector.RemainingBlocks < 1)
                            if (managed.projector.RemainingBlocks < 1)
                            {
                                    bBuilding = false;
                            }
                        }
                    }
                }
                return bBuilding;
            }


            public bool IsWorkingProjector()
            {
                bool bIsWorking = false;
                foreach (var projector in projectorList)
                {
                    if (projector.IsProjecting)
                    {
                        bIsWorking = true;
                        break; // don't need to check more
                    }
                }
                return bIsWorking;
            }
            public void turnoffProjectors()
            {
                foreach(var projector in projectorList)
                {
                    projector.Enabled = false;
                    projector.ShowOnlyBuildable = false;
                }
            }

            public bool PrintHeadCompleted()
            {
                bool Completed = true;

                foreach (var managed in managedProjectors)
                {
                    if (managed.bIsPrintHead)
                    {
                        if (managed.projector.IsProjecting)
                        {
                            if (managed.projector.RemainingBlocks > 0)
                                Completed = false;
                        }
                    }
                }
                return Completed;
            }

            public void ControlManagedProjectors(bool bOn = true)
            {
                foreach (var managed in managedProjectors)
                {
                    managed.projector.Enabled = bOn;
                }
            }

            public void ControlPrintHead(bool bOn=true)
            {
                foreach(var managed in managedProjectors)
                {
                    if(managed.bIsPrintHead)
                    {
                        managed.projector.Enabled = bOn;
                        managed.projector.ShowOnlyBuildable = false;
                    }
                }
            }

            public class ManagedProjector
            {
                public bool bControlled;
                public IMyProjector projector;
                public string Name;
                public string blueprintName;
                public bool bIsPrintHead;
                // list of components required?
                // size of projection (boudingbox)

            }

        }
    }
}
