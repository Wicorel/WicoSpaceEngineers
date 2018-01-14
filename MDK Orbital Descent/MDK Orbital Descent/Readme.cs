/* Wico Craft ORIBTAL DESCENT control sub-module 
 * 
 *  * Handles MODES:
 * MODE_DESCENT
 * MODE_IDLE (if craft==ORBITAL)
 * 
 * Commands:
 * 
 * orbitaldescent: starts orbital descent mode
 * 
 * 2.04a Add camera scans.
 * Multiple arguments on a line
 * 
 * 2.1 ???
 * connectors check for [DOCK]
 * 
 * 2.1a connectors check for [DOCK] and [BASE]
 * 
 * 2.2 SE V1.172 API changes
 * 
 * 3.0 Update to V3.0 script modules
 * 
 * 3.0A effective mass and effective thrust
 * 
 * 3.0c MDK Version https://github.com/malware-dev/MDK-SE/wiki
 *  Uncompressed Source Here: https://github.com/Wicorel/SpaceEngineers/tree/master/MDK%20Orbital%20Descent
 *  
 *  3.0D search order for text panels
 *  
 *  3.1 Updates for SE V1.185
 *  
 *  3.1a Buld with current Source 11/26/2017
 *  
 *  3.1b Remove (almost) all ApplyActons 12032017
 *  12222017
 *  
 *  3.2 INI WCCM 01062018
 *  
 *  3.2a
 * FilledRatio Change

 * NEED:  
 *  
 *  trigger timers on stages of descent (see rotor legs example)
 *  'ask' for docking position on descent
 *  find planet if in space
 *  circumnavigate planet to drop from above target landing spot
 *  
 *  
 * Reactors on/off for docked. 
 *  
 * VTOL: has atmo for hovering/rising in horizonal postion. then transitions to vertical for orbital 
 *  
 *  
 * WANTED: 
 * VTOL Support 
 * calculate thrust available and determine if able to lift-off 
 * predict acceleration/decel and adjust thrust on launch based on what's it's GOING to be.. 
 * choose ROCKET/VTOL/NORMAL automatically based on thruster arrangement determination 
 *  
*/
