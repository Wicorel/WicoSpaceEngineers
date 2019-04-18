// <mdk sortorder="-100" />
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

//          <Editable>true</Editable>

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // MDK 1.1
        #region mdk preserve
        #region mdk macros
        // This script was deployed at $MDK_DATETIME$
        #endregion

        /*

        Commands

        3.4 01272018

        3.4a 02112018

        3.4B Build with current Code base Mar 08 2018

        3.4C Build with current Code base mar 27 2018
        Call remote modules for NAV and DOSCAN.

        3.4D +Apr 03 2018

        3.4E Detect 'Boring' ship
        Back out of hole when boring ship gets full
        Apr 22 2018

        3.4F May 06 2018 + May 18 2018
        generate errors on startup for missing sensors/drills
        More mining implementation from mk3-like repeats
        Generate up/out/right vectors for asteroid
        Source published June 01 2018

        3.4G June 02 2018
        Allow editable Bore Height and Width for non-rectangular miners like W4M
        Add 'mine the bore I'm pointing at' mode..
        Fix exiting asteroid after ejecting a lot of stone

        June 03, 2018
        Fix alignment problems in bore
        use stopping distance for 140
        check battery remaining before starting/ending bore (doh)

        3.4H June 03,2018
        Do sensor and raycast scan of bore and don't do run if nothing found

        3.4I June 04, 2018
        When boring, check rear sensor for 'out'.  verify with raycast ahead to be sure it's 'clear'.
        change default boreheight/width to FULL bounding box.
        Calculate offset from beam of the bore and offset direction to compensate
        bore raycast scan does 100%, 55% and 35%

        3.4J June 15, 2018
        Next Bore Target needed to set target name correctly
        (NAV now supports using world mps as default)
        If no current asteroid and known is >5000 away, then don't go there by default
        Add "bore" command to do single bore at target in front MODE_BORESINGLE->MODE_MINE
        rename doModeFineOre to doModeMine since it's the core mining mode

        3.4K Sep 08, 2018
        Testing with large hollow asteroids

        3.5 Jan 25 2019 SE 1.189 
        Mar 29, 2019

        TODO:
            check for complete destruction of asteroid like MK3 did
            announce 'asteroid removed'
            Change to IGC system
            control sorter directly to set unwanted ore types (gravel, stone, etc)
            take commands to set unwanted/wanted ore types
            Handle survival kit (wait for conversion)
            Handle keeping stone, but getting rid of gravel
            handle survival kit(s) making iron, nickel and silicon
            push ore to connected when docked (really a 'docked' setting, maybe)

            Menu system.
            select asteroids
            select ore
            mine only target ore
            manual triangulation for ore locations
            manual triangulation for asteroid locations

            ::  add other mining modes
            spread mining for searching for ores
            'Manual mine' mode for player controlled mining.  and still report ore locations, etc
            'seam following' mode
            'surface deposit' mine mode (place bores around target and go until just mining stone/unwanted ore) (partial bore depth)

            

        NEED: 
            Handle survival kit
            Handle keeping stone, but getting rid of gravel
            handle survival kit(s) making iron, nickel and silicon


        WANTED: 

        */
        #endregion

        string OurName = "Wico Craft";
        string moduleName = "MINER";
        string sVersion = "3.5";

    }
}
