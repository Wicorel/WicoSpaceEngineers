/* Wico Craft DOCKING Control sub-module
 * 
 * Workshop: http://steamcommunity.com/sharedfiles/filedetails/?id=883865519
 * 
 * Uncompressed Source: https://github.com/Wicorel/WicoSpaceEngineers/tree/master/Wico%20Dock
* 
* Handles MODES:
* MODE_DOCKED
* MODE_DOCKING
* MODE_LAUNCH
* MODE_RELAUNCH
* 

3.0h 
Minify to make 100k room
Removed gyronics
Removed rolls and yaws (for sleds)
remember mom (id)
Get location from 'mom' via antenna. 
When docking, if out of antenna range from mom, move into antenna range before asking for dock

3.0i
Use sensors when moving. If hit something, move away from it.
say 'hello' when Orphan (to make mom respond)
minor adjustments to travel movement to reduce default ranges.

3.0j
timeout on requesting dock (5 seconds)
'fix' by force fZOffset in sensor size calc so sensor doens't extend many meters behind ship.
If secondary avoid hits asteroid, go to ATTENTION (punt on drone control)

3.0k
when 'stuck' use cameras to try to find a way out.
calcfoward now uses WorldMatrix instead of deriving it

3.0l
check for shorter hitpoints if none are clear.  Try to escape to furtherest.
move to half-way to hitpoint and then try again
set forward collision detection based on stopping distance and not target distance (needed for in-asteroid operations)

3.0m
use only stoppingdistance for travel modes/speed

3.0N
code reduction pass
NOTE: Can't dock with something attached to our front (mini cargo) because collision detection 'hits' it..
docking with alignment.

3.0O
update getgrids to NOFOLLOW
Fix crash when attached to print head

3.0P
minor opt in GyroMain
dont go FAST when waiting for reply from dock

3.0Q
localGridFilter in BlockInit
leave LIMIT_GYROS to default

3.0R MDK Version (compressed)

3.0S Don't clear GPS Panel
add 'forgetmom' command to make drone forget about a previous 'motherhsip.

3.0T remove MOM and use new 'BASE' system for docking

3.0T2 search order for text panels

3.0U 
Add delays for requests for BASE?
Turn drills and ejectors off when heading to dock. (drills at start and ejectors when arrive at approach).

3.1 Version for SE 1.185 PB Changes
Turn off logging because of text panel writes causing hang.

3.2 Handle docking with connectors in any orientation

delay for motion after going 'home' (added state 169)

improve long-range travel for docking (chooses optimal speed, uses dampeners, etc). (major changes to TravelMovement code)
Handle case where requested to dock, but have not heard from a base yet; request and wait a bit for a response. (added state 109)

3.2a Remove blockApplyActions and connector actions

3.2b Common DoTravelMemovement and collision code.

3.2c Docking messages now include size of ship in meters so base can choose best available connector

3.2d new travelmovement

3.2E 
Increase scan delay time in tm to .225
only use fast when needed for docking.
use .Once for FAST
12232017

3.2F INI Save 12262017
fix bug in serialize wrting z,y z, instead of x,y,z (oops)
MODE_DOCKED tries to fill tanks and batteries.

3.2g 01062018

3.2h

3.3 Multiple Text Panels.
Only write to panels at the end of script.

3.3a defaultorientationblock code moved

3.4A init optimization.  check instructions on each sub-init

3.4B
Current Code Compile Mar 08 2018

3.4C Current Code Compile Mar 28 2018
Redo BASE serilization to be key=value
Use NAV module for long-range travel

3.4D Current Code Apr 22, 2018

3.4E May 05 2018 + May 18 2018 May 27 2018
Added Report text panel output to docking states.

TODO:
forget known bases when new grid (DONE by craft control)
support 'memory' connector; like MK3 did
support multiple 'memmory' connectors
ability to turn off 'wico' communication docking (no antenna communications)
Need relaunch handling
timeout after requesting dock; remove base from list or mark 'dead?'

*/
