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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Displays
        {

            const string DisplayCheckTimer = "DisplayCheck";
            const double DisplayInterval = 0.5;

            public const int CLEARDISPLAY = 0;
            public const int DODRAW= 1;
            public const int SETUPDRAW = 99;

            bool _Debug = false;

            List<IMyTerminalBlock> _SurfaceProviders = new List<IMyTerminalBlock>();
            List<WicoDisplay> _wicoDisplays = new List<WicoDisplay>();

            public class WicoDisplay
            {
                public string tag;
                List<IMyTextSurface> _surfaces;
                List<Action<string,IMyTextSurface, int>> SurfaceDrawHandlers = new List<Action<string,IMyTextSurface, int>>();

                public WicoDisplay(List<IMyTerminalBlock> lsp, string Tag)
                {
                    tag = Tag;
                    _surfaces = new List<IMyTextSurface>();
                    SurfaceDrawHandlers = new List<Action<string,IMyTextSurface, int>>();
                    // surfaces initialized at PostInitHandler by calling OfferSurface
                }

                public void ResetSurfaces()
                {
                    _surfaces.Clear();
                }

                public void OfferSurface(IMyTerminalBlock tb)
                {
                    if(tb.CustomName.Contains(tag))
                    {
                        var tsp = tb as IMyTextSurfaceProvider;
                        var x=tsp.SurfaceCount;
                        var tsurface = tsp.GetSurface(0);
                        if(tsurface!=null)
                            _surfaces.Add(tsurface);
                    }
                }

                public void OfferHandler(Action<string,IMyTextSurface, int> handler)
                {
                    if (!SurfaceDrawHandlers.Contains(handler))
                        SurfaceDrawHandlers.Add(handler);
                }

                public void CallHandlers(int ActionType)
                {
                    foreach(var handler in SurfaceDrawHandlers)
                    {
                        foreach(var surface in _surfaces)
                        {
                            handler(tag,surface, ActionType);
                        }
                    }
                }
            }

            Program _program;
            WicoBlockMaster _wicoBlockMaster;
            WicoElapsedTime _wicoElapsedTime;

            readonly string WicoDisplaySection = "WicoDisplay";

            public Displays(Program program, WicoBlockMaster wicoBlockMaster, WicoElapsedTime wicoElapsedTime)
            {
                _program = program;
                _wicoBlockMaster = wicoBlockMaster;
                _wicoElapsedTime = wicoElapsedTime;

                _wicoBlockMaster.AddLocalBlockHandler(BlockParseHandler);
                _wicoBlockMaster.AddLocalBlockChangedHandler(LocalGridChangedHandler);

                _program.AddPostInitHandler(PostInitHandler());

                _Debug = _program._CustomDataIni.Get(WicoDisplaySection, "Debug").ToBoolean(_Debug);
                _program._CustomDataIni.Set(WicoDisplaySection, "Debug", _Debug);

                _wicoElapsedTime.AddTimer(DisplayCheckTimer, DisplayInterval, ElapsedTimerHandler);
                _wicoElapsedTime.StartTimer(DisplayCheckTimer);
            }

            public IEnumerator<bool> PostInitHandler()
            {
                foreach(var surface in _wicoDisplays)
                {
                    foreach(var tb in _SurfaceProviders)
                    {
                        surface.OfferSurface(tb);
                    }
                }
                foreach(var surface in _wicoDisplays)
                {
                    surface.CallHandlers(SETUPDRAW);
                }
                yield return false;
            }

            /// <summary>
            /// gets called for every block on the local construct
            /// </summary>
            /// <param name="tb"></param>
            void BlockParseHandler(IMyTerminalBlock tb)
            {
                if (tb is IMyTextPanel)
                {
                   // assume all text panels are surface providers.
                    _SurfaceProviders.Add(tb);
                }
            }

            void LocalGridChangedHandler()
            {
                _SurfaceProviders.Clear();
                foreach (var display in _wicoDisplays)
                {
                    display.ResetSurfaces();
                }
            }

            /// <summary>
            /// called when specified elapsed time has... elapsed.
            /// </summary>
            /// <param name="timerName">The timer that has elapsed</param>
            void ElapsedTimerHandler(string timerName)
            {
                // do stuff..
                foreach(var display in _wicoDisplays)
                {
                    display.CallHandlers(DODRAW);
                }
                _wicoElapsedTime.RestartTimer(timerName);
            }

            /// <summary>
            /// Add a handler for the tagged displays
            /// </summary>
            /// <param name="tag">The tag for the displays</param>
            /// <param name="handler">the handler.  Called by displays module when displays need to be updated (on a timer)</param>
            /// <returns>true</returns>
            public bool AddSurfaceHandler(string tag, Action<string,IMyTextSurface, int> handler)
            {
//                _program.Echo("ASH:" + tag + ":");
                if (handler == null)
                    _program.Echo("handler is NULL!");

                bool bFound = false;
                WicoDisplay FoundDisplay=null;
                foreach(var display in _wicoDisplays)
                {
                    if(display.tag==tag)
                    {
                        FoundDisplay = display;
                        bFound = true;
                        break;
                    }
                }
                _program.Echo("display found="+bFound.ToString());
                if (!bFound)
                {
                    FoundDisplay = new WicoDisplay(_SurfaceProviders, tag);
                    FoundDisplay.OfferHandler(handler);
                    _wicoDisplays.Add(FoundDisplay);
                }
                else
                {
                    FoundDisplay.OfferHandler(handler);
                }
                
                return true;
            }

            public void ClearDisplays(string tag)
            {
                foreach (var display in _wicoDisplays)
                {
                    if (display.tag == tag)
                    {
                        display.CallHandlers(CLEARDISPLAY);
                    }
                }
            }

            public void EchoInfo()
            {
                _program.Echo("Displays:");
                _program.Echo(" " +_wicoDisplays.Count + " DisplayTypes");
                foreach(var display in _wicoDisplays)
                {
                    _program.Echo(" " + display.tag);
                }
                _program.Echo(" " + _SurfaceProviders.Count + " Surface Providers");
            }

        }
    }
}
