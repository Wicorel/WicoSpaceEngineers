/* Wico Craft Miner control sub-module 
 * 
 *  * Handles MODES:
 * MODE_MINE
 * 
 * Commands:
 * 
 * findore : starts searching for asteroid, then chooses one, then searches for ore
 * 
 * 
 * WorkShop: http://steamcommunity.com/sharedfiles/filedetails/?id=883866371
 * Source: https://github.com/Wicorel/WicoSpaceEngineers/tree/master/MDK%20Miner
 * MDK: https://github.com/malware-dev/MDK-SE/wiki
 * 
 * 3.0a 1st MDK Version
 * 
 * 3.1 First version for new PB changes in SE 1.185
 * 
 * 3.1A Turn Horizontally in tunnel when trying to exit asteroid
 * 
 * SearchShift/Orient WIP
 * 
 * 3.1B
 * Quadrant scans for asteroid if not in scanner range
 * use DoTravelMovemnent without collision detection if long-range to target asteroid
 * 
 * 3.1C
 * More WIP for mining/searching
 * 
 * 3.1D Asteroid processing and saving to text panel
 * 
 * 3.1E 12262017
 * INI for Save
 * INI for Asteroids->Mining
 * fix bug in serialize wrting z,y z, instead of x,y,z (oops)
 * 
 * 3.2 01062018
 * 
 * 3.2A
 * search shift work
 * remember previous asteroidID and go back to it
 * also use sensors for approach (120/1)
 * 
 * 3.2B INI setting lots of things
 * 
 * 3.3 Lists of Text panels
 * Only output to panels at end of script.
 * 
 * 3.3A Redo serialize
 * Module Serialize
 * 
 * 3.4 01272018
 * 
 * 3.4a 02112018
 * 
 * NEED:  
 *  
 * WANTED: 
 *  
*/
