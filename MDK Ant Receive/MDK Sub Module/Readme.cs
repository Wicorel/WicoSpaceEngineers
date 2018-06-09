/*
Wico craft Antenna Receive sub-module

Full Source available here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Ant%20Receive
Workshop here: http://steamcommunity.com/sharedfiles/filedetails/?id=883864500
*
Handles:
Receiving messages from antennas
Passing messages on to other sub-modules
queueing of received mesages

Dock Manager.  Handles managing connectors and requests for docking
 
Modes:
none

Commands:

Need:

Want:

3.0 Match control code
3.0c Performance Optimizations
3.0D rotor NOFOLLOW
ignore projectors with !WCC in name or customdata

3.0E MDK Version

3.0F Combined DockMgr into code

3.0G New BASE communications.  remove MOM

3.0G2  search order for text panels

3.0H
Increase time before active BASE send.  Randomize time before starting to send to avoid burst transmission at world start.

3.1 Version for SE 1.185

3.1A 
Now using MDK Minify (size was >80k without)
Moved a number of utility routines into WicoAntenna
Check antenna for this PB set as attached.  Attach this PB to the antenna with maximum range

*3.2 Drone height, width and length in docking requests

3.2A 12262017 INI Save
01062018

3.2B 01132018
Getting settings from CustomData
All ignore for !WCC
SenorUse
For docking, only use lights with [BASE]

3.3 0119 Lists of panels.
Output to panels at END of script.

3.3a redo blockinit

3.4a optimizations for text panel init.

3.4B Current Code Compile. Mar 08 2018

3.4C Current Code Compile Mar 29 2018
Added MODE_DOSCANS
Add asteroids and ore message processing.

3.4D  May 05 2018
text panel reports during scans
May 27, 2018
published June 1, 2018

3.4E June 02, 2018
June 08,2018 FAST doscan mode current 0->410 FAST

TODO: Choose closest connector as 'best'
TODO: Process size info to select connectors
TODO: drones may take up more than one connector depending on height, width.

TODO: Hangar door control: open/close by antenna request

TODO: send/receive 'timer' messages. Like https://steamcommunity.com/sharedfiles/filedetails/?id=1287452364


*/