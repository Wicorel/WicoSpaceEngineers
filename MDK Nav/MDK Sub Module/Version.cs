// <mdk sortorder="-99" />
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
// MDK 1.1
#region mdk preserve
#region mdk macros
// Minified script was deployed at $MDK_DATETIME$
#endregion
#endregion

#region mdk preserve
/*
* 
* Commands:
* W <waypoint>
*   Go to waypoint
*   
* S <max speed>
*   Set max travel speed
*   
* D <min arrive distance
*  set min expected arrival distance in meters
*  
* C <comment>
*   Any text except ;
*   
* O <waypoint> (untestd)
*   Orient to waypoint
* 
* 
* Handles MODES:
* MODE_GOTARGET
* 
* 
* History:
* 
2.0.4 Upate to new save format
    .04A Camera Scans for Obstacles...!!!one

2.1 Use new blockInint and localgrids

    .1g Add Docked
    copy from SLED PATROL
    .1h fixed yaw only gyromain
    .1i tested in space. Added !NAV to gyro check
    .1j add doroll
    .1k use (and fix/test) IMyGyroControl

2.2: Update for 1.72

2.9 Copy from Sled Dock 2.2

Needs LOTS of updates.

3.0 Move code into 3.0

3.0A Start NAV processing: W and O
3.0B Add D, S, C
3.0C Add arrivedtarget

3.0D 110517  search order for text panels

3.1 Version for PB Updates SE 1.185
o Added support for GPS-formatted nav locations
    Ex:  W GPS:Wicorel #1:53970.01:128270.31:-123354.92:

3.1a
remove blockApplyActions() and make routines for each block type that needs it

3.2 Collision Avoidance from Docking module for thruster travel

Added Rotors

3.2A travelmovement calculating target speeds and distances with more precision

3.2B Sled Testing

3.2C INI Save
    fix bug in serialize wrting z,y z, instead of x,y,z (oops)

3.2D INI WCCM 01062018

3.2E Major INI settings

3.3 Lists of text panels
Only output to textpanels and end of run

3.3A Redo Serialize

3.4 Sled testing
(EFM Update 8 Drones)

3.4a Save NAV settings so nav can properly resume
(EFM Update 9 Drones)

3.4B

3.4C AvionicsGyro fixes (terminal properties changed units)
Fix for bug in SE wheel setter for friction
Add Gyro limits to CustomData
(EFM Update 11 wheeled Drones)

3.4D Air unit NAV changes
terrain auto-follow
alignment in gravity
(EFM Update 11 Air unit drones)

3.4E testing with space craft again.
Mar 08 2018
Fix gyro terminal properties

3.4F Fix vertical velocity calculation
Add HoverEngine detection support to thrusters

3.4G Timer Processing updates
More CustomData options for timer names, etc.

3.4H 03282018
Add desired arrivalmode and state to navcommon
'arrived' now sets desired mode and state

3.4I Apr 03 2018
push to max speed and then coast..

3.4J Apr 21 2018
Show initialization problems and retry if necessary items are missing

3.4K May 06 2018+
Sensor code update
Tested with higher world speeds
May 27 2018

3.4L June 02 2018
Raycast collision also checks half-way distance for size of ship instea of just full scan distance.
June 10 Added target name so it can be shown

3.4M
Set target name from GPS Waypointcommend

3.4N  default speed to world max speed, not 100
June 15, 2018
Renumber modes >150 to make room

TODO: 
    Do quadrant search for escape
    Support multiple waypoints
    Support 'Loop' (patrol) command
    handle sides without cameras
    TEST: handle no cameras forward (slower min speed)
    Timer triggers on mode/state changes

*/

        #endregion

        string OurName = "Wico Craft";

        string moduleName = "NAV";
        string sVersion = "3.4N";

    }
}
