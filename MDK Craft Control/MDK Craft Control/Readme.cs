/*
* Wico craft controller Master Control Script
*
* Control Script for Rovers and Drones and Oribtal craft
* 
* Version 3.0K
* 
* 2.0 Removed many built-in functions to make script room. These functions were duplicated in sub-modules anyway.
* 2.0.1
* 0.2 Remove items from serialize that main control no longer calculates (cargo, battery, etc).
* if simspeed>1.01, assume 1.0 and recalculate.
* 0.3 re-org code sections
* Pass arguments to sub-modules 
* 0.4 (re)integrate power and cargo
* 0.4a process multiple arguments on a command line
* 0.4b check mass change and request reinit including sub-modules.
* 
* 2.1 Code Reorg
* Cache all blocks and grids.  Support for multi-grid constructions.
* !Needs handling for grids connected via connectors..
* 
* .1a Don't force re-init on working projector.
* .1b Add 'brake' command
* Add braking for sleds (added wheelinit)
* 
* 2.2 PB changes in 1.172
* 
* .2a Added modes. Default PB name
* 
* 2.3 Start to add Power information
* 
* .3a Add drills and ejectors to reset motion. Add welders, drills, connectors and grinders to cargo check.
* don't set PB name because it erases settings.. :(
* 
* .3b getblocks fixes when called before gridsinit
* 
* 3.0 remove older items from serialize that are no longer needed
* removed NAV support
* fixed battery maxoutput values
* 
* 3.0a support no remote control blocks. Check for Cryo when getting default controller.
* 3.0b sBanner
* 3.0c caching optimizations
* 3.0d fix connectorsanyconnectors not using localdock
* 3.0e Add Master Reset command
* 3.0f 
* check for grid changes and re-init 
* rotor NOFOLLOW
* ignore projectors with !WCC in name or customdata
* ignore 'cutter' thrusters
* 
* 3.0g Fix problem with allBlockCount being loaded after it has changed
* 
* 3.0H 
* fix problems with docking/undocking and perm re-init
* 
* 05/13: fix GetBlocksContains<T>()
* 
* 3.0I MDK Version 08/20/2017   MDK: https://github.com/malware-dev/MDK-SE/
* Uncompressed source for this script here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Craft%20Control
* 
* 3.0J Add moduleDoPreModes() to Main()
* Move pre-mode to moduleDoPreModes()
* add clearing of gpsPanel to moduleDoPreModes()
* 
* 3.0K more init states if larger number of blocks in grid system.
* 
* Handles:
* Master timer for sub-modules
* Calculates ship speed and vectors
* Calculates simspeed
* Configure craft_operation settings
* making sure antenna doesn't get turned off (bug in SE turn off antenna when trying to remotely connect to grid)
* 
* Calculates cargo and power percentages and cargo multiplier and hydro fill and oxy tank fill
 * 
 * Detects grid changes and initiates re-init
 * 
* * 
* MODE_IDLE
* MODE_ATTENTION
* 
* Commands:
* 
* setsimspeed <value>: sets the current simspeed so the calculations can be accurate.
* init: re-init all blocks
* idle : force MODE_IDLE
* coast: turns on/off backward thrusters
* setvaluef <blockname>:<property>:<value>  -> sets specified block's property to specified value
* Example:
*  setvaluef Advanced Rotor:UpperLimit:-24
*
* Need:

* Want:
* 
* menu management for commands (including sub-modules)
* 
* minimize serialized data and make sub-modules pass their own seperately, OR support extra data in state
* 
* common function for 'handle this' combining 'me' grid and an exclusion name check
*
* multi-script handling for modes
* 
* * advanced trigger: only when module handles that mode... (so need mode->module dictionary)
* override base modes?
*
*
*
* WANT:
* setvalueb
* Actions
* Trigger timers on 'events'.
* set antenna name to match mode?
*
*
*/
