/*
* Wico craft Antenna Receive sub-module
* 
* Full Source available here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Ant%20Receive
* Workshop here: http://steamcommunity.com/sharedfiles/filedetails/?id=883864500
*
* Handles:
* Receiving messages from antennas
* Passing messages on to other sub-modules
* queueing of received mesages
* 
* Dock Manager.  Handles managing connectors and requests for docking
*  
* Modes:
* none
* 
* Commands:
* 
* Need:

* Want:
* 
* 3.0 Match control code
* 3.0c Performance Optimizations
* 3.0D rotor NOFOLLOW
* ignore projectors with !WCC in name or customdata
* 
* 3.0E MDK Version
* 
* 3.0F Combined DockMgr into code
* 
* 3.0G New BASE communications.  remove MOM
* 
* 3.0G2  search order for text panels
* 
* 3.0H
* Increase time before active BASE send.  Randomize time before starting to send to avoid burst transmission at world start.
* 
* 3.1 Version for SE 1.185
* 
* 3.1A 
* Now using MDK Minify (size was >80k without)
* Moved a number of utility routines into WicoAntenna
* Check antenna for this PB set as attached.  Attach this PB to the antenna with maximum range
* 
*3.2 Drone height, width and length in docking requests
* 
* TODO: Process size info to select connectors
* TODO: drones may take up more than one connector depending on height, width.
* 
*/