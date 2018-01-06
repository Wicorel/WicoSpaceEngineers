/*
* Wico craft controller (TECHNIKER) Master Control Script
*
* Control Script for Rovers and Drones and Oribtal craft
* 
* Techniker Source here: https://github.com/Wicorel/WicoSpaceEngineers/tree/master/MDK%20Techniker
* 
* T3.1  Techniker version.
* 
* T3.1A Fixes for complexity
* 
* T3.1B fixes for (incorrectly) detecting mass change on dock
* 
* T3.2 01042018
* Current Source.  
* Change BatteryCheck() to not unset recharge when targetMax <=0
* 
* T3.2A 01052018
* flags for turning on/off Techniker features
* 
* 
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
