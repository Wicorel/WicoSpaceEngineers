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
 /*
  * 
  * OLD:
 * 0=init sensors
 * 8 check sensors
 * 1=push down
 * 2=push left ->3
 * 9=push right ->3
 *
 * 3 = check sensors; go to 4 or 5
 * 4 = rotating ->5 or ->7
 * 5. asteroid check on 'bottom' after rotate and nothing in front.
 * 6. rotate after 5 ->7
 * 7. Wait for motion to stop (velocityShip) ->mine
 * 
 * 20. Init search top/left (sensor setting)
 * 30. check sensors
 * 29 move backward until NOT bfrontnasteroid
 * 27. Move forward until bfrontnasteroid
 * 28. delay for motion. reset sensors.

 * * 21. move up until bfront clear
 * 22. move right until roll control clear
 * 23 delay for motion. reset sensors
 * 24 shift left 1/2 ship width - 2m *.35
 * 25 shift down until bfront, bfrontnear. stop if !brollcontrol and do searchverify
 * 26 delay for motion. then start mining
 */
       /*
         * readonly Dictionary < int, string > SearchShiftStates = new Dictionary < int, string > { 
            { 
                0, "Initializing" 
            }, { 
                1, "Moving Down" 
            }, { 
                2, "Moving Left" 
            }, { 
                3, "Calculate rotate" 
            }, { 
                4, "Rotating" 
            }, { 
                5, "Check Bottom" 
            }, { 
                6, "Rotate" 
            }, { 
                7, "Delay for motion" 
            }, { 
                8, "Check Sensors" 
            }, { 
                9, "Moving Right" 
            }, { 
                20, "STR:Init search top right" 
            }, { 
                21, "STR:move up" 
            }, { 
                22, "STR:move right" 
            }, { 
                23, "STR:delay for motion" 
            }, { 
                24, "STR:shift left" 
            }, { 
                25, "STR:shift down" 
            }, { 
                26, "STR:delay for motion (2)" 
            }, { 
                27, "STR:Move in range" 
            }, { 
                28, "STR:delay for motion (3)" 
            }, { 
                29, "STR:Move back to clear" 
            }, { 
                30, "STR:Check Sensors" 
            } 

        }; 
        */

    }
}