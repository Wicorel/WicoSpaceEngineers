/* Wico Craft ORIBTAL LAUNCH control sub-module
 * 
 * 
* Handles MODES:
* MODE_HOVER
* MODE_LAUNCHPREP
* MODE_LANDED
* MODE_ORBITALLAUNCH
* 
* Commands:
* 
* setmaxspeed <value>: sets the maximum speed in m/s. Default speed is 100. Only need to set if mod increases speed
* resetlaunch: reset any saved launch locations.
* orbitallaunch: Start launch to orbit
* 
* 1.66 (initial version)
*    
*   2.2 SE V1.72 Changes
*
* 3.0 Serialize changes and current code
* 3.0a Optimize for connectors init.
* 3.0b merge grid fixes
* 
* 3.0c MDK Version https://github.com/malware-dev/MDK-SE/wiki
* Uncompressed Source here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Orbital%20Launch
* 
* 3.1 Updates for SE V1.185
* 
* 3.1a build with current source
* 
* 3.1B current source 12/22/2017
* 
* 3.2 INI WCCM Serialize 01062018
* 
* 3.2a
* FilledRatio Change
* 
* Need:
*  circumnavigate planet to target spot
*  'ask' for docking position after arriving at 'spot'
*
*/
