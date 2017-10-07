/*
 *   R e a d m e
 *   -----------
 * 
 * Test Script by Wicorel to show problem when loading world.
 * 
 * Grids are not initialized properly, so script only sees the one grid and not the attached grids.
 * 
 * On game load in the constructor, the number of grids will be 1 (incorrect).
 * 
 * On each run after, the number of grids will be 2 (correct)
 * 
 * You can re-run the constructor by recompiling the script. When run in the game, the number of grids will be 2 (correct)
 * 
 */