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
        #region modes

        int iMode = 0;

        const int MODE_IDLE = 0;
        const int MODE_SEARCH = 1; // old search method..
        const int MODE_MINE = 2;
        const int MODE_ATTENTION = 3;
        const int MODE_WAITINGCARGO = 4;// waiting for cargo to clear before mining.
        const int MODE_LAUNCH = 5;
        //const int MODE_TARGETTING = 6; // targetting mode to allow remote setting of asteroid target
        const int MODE_GOINGTARGET = 7; // going to target asteroid
        const int MODE_GOINGHOME = 8;
        const int MODE_DOCKING = 9;
        const int MODE_DOCKED = 13;

        const int MODE_SEARCHORIENT = 10; // orient to entrance location
        const int MODE_SEARCHSHIFT = 11; // shift to new lcoation
        const int MODE_SEARCHVERIFY = 12; // verify asteroid in front (then mine)'
        const int MODE_RELAUNCH = 14;
        const int MODE_SEARCHCORE = 15;// go to the center of asteroid and search from the core.


        const int MODE_HOVER = 16;
        const int MODE_LAND = 17;
        const int MODE_MOVE = 18;
        const int MODE_LANDED = 19;

        const int MODE_DUMBNAV = 20;

        const int MODE_SLEDMMOVE = 21;
        const int MODE_SLEDMRAMPD = 22;
        const int MODE_SLEDMLEVEL = 23;
        const int MODE_SLEDMDRILL = 24;
        const int MODE_SLEDMBDRILL = 25;

        const int MODE_LAUNCHPREP = 26; // oribital launch prep
        const int MODE_INSPACE = 27; // now in space (no gravity)
        const int MODE_ORBITALLAUNCH = 28; // start orbital launch

        const int MODE_DESCENT = 29;
        const int MODE_ARRIVEDTARGET = 30; // we have arrived at target

        const int MODE_UNDERCONSTRUCTION = 31;

        const int MODE_PET = 111; // pet that follows the player

        // new mining modes
        const int MODE_FINDORE = 200;
        const int MODE_GOTOORE = 201;
        const int MODE_MININGORE = 202;
        const int MODE_EXITINGASTEROID = 203;

        // attack/coodrination modes
        const int MODE_WAITINGCOHORT = 300;
        const int MODE_ATTACK = 310;

        //const string sgRL = "Running Lights";
        //const string sgML = "Mining Lights";

        void setMode(int newMode)
        {
            if (iMode == newMode) return;
            // process delta mode
            if (newMode == MODE_IDLE)
            {

            }
            iMode = newMode;
            current_state = 0;
        }

        #endregion


    }
}