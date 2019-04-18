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

        int iMode = -1;

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
        const int MODE_EXTRUSIONPROJECTION = 33;

        const int MODE_LAUNCHED = 50; // we have completed launch

        const int MODE_AIRDROP = 60; // we are doing a drop in gravity

        // auto-follow modes
        const int MODE_PET = 111; // pet that follows the player
        const int MODE_GRIDFOLLOW = 112; // follow a (specified?) grid 
        // 'follow front'(for trains)
        // TODO: add other alignments for formations. (maybe default to maintain alignment?)



        // new mining modes
        const int MODE_FINDORE = 200; // find ore on specified asteroid
        const int MODE_GOTOORE = 210; // go to known ore (on specified asteroid)
        const int MODE_BORINGMINE = 220; // old-school boring mine
        const int MODE_BORESINGLE = 225; // single bore. return to dock as needed. don't return to asteroid when bore is completed
        const int MODE_EXITINGASTEROID = 290; // getting out of an asteroid while full

        // Scaning
        const int MODE_DOSCAN = 400; // do a full scan of the area and report found entities
        const int MODE_SCANCOMPLETED = 410;       

        // attack/coodrination modes
        const int MODE_WAITINGCOHORT = 500;
        const int MODE_ATTACK = 510;

        const int MODE_STARTNAV = 600; // start the navigation operations
        const int MODE_NAVNEXTTARGET = 610; // go to the next target

        const int MODE_SCANTEST = 999;


        void setMode(int newMode)
        {
            if (iMode == newMode) return;
            // process delta mode
            iMode = newMode;
            current_state = 0;
            doTriggerMain();
        }

    }
}