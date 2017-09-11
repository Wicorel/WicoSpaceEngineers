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

string sVersion = "3.0D";
string OurName = "Wico Craft";
string moduleName = "Ant Receive";

class OurException : Exception
{
    public OurException(string msg) : base("WicoCraft" + ": " + msg) { }
}

Dictionary<string, int> modeCommands = new Dictionary<string, int>();

const string sFastTimer="[WCCT]";
const string sSubModuleTimer = "[WCCS]";

string sBanner = "";
public Program() 
{
	sBanner=OurName + ":" + moduleName+" V"+sVersion + " ";
	Echo(sBanner + "Creator");
	initLogging();
	doSubModuleTimerTriggers("[WCCM]"); 
	if (!Me.CustomName.Contains(moduleName))
		Me.CustomName = "PB " + OurName+ " "+moduleName;
}

// sub-module common main
// 03/21 Add sBanner and ticks
#region MODULEMAIN

bool init = false;
bool bWasInit=false;
bool bWantFast=false;
bool bWorkingProjector=false;

double velocityShip=-1;

void Main(string sArgument)
{
	Echo(sBanner + tick());
	bWantFast = false;

	bWorkingProjector=false;
	var list = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyProjector>(list,localGridFilter);
	for(int i=0;i<list.Count;i++)
	{
		if(list[i].IsWorking) 
		{
			if (list[i].CustomName.Contains("!WCC") || list[i].CustomData.Contains("!WCC")) continue; // ignore
			Echo("Working local Projector found!");
			bWorkingProjector=true;
		}
	}

	if(sArgument!="" && sArgument!="timer"&& sArgument!="wccs") Echo("Arg="+sArgument);

	if(sArgument=="init")
	{
		sInitResults="";
		init=false;
		currentRun=0;
	}

	if (!init)
	{
		if(bWorkingProjector)
		{
			StatusLog("clear",getTextBlock(sTextPanelReport));

			StatusLog(moduleName +":Construction in Progress\nTurn off projector to continue",textPanelReport);
		}
		bWantFast=true;
		doInit();
		bWasInit=true;
	}
	else
	{
		if(bWasInit) StatusLog(DateTime.Now.ToString()+" " + OurName+ ":"+ sInitResults,textLongStatus,true);

		Deserialize();

		Echo(craftOperation());

		if(processArguments(sArgument))
			return;

		if(bWantFast) Echo("FAST!");

		moduleDoPreModes();

		doModes();
	}

	Serialize();

	if (bWantFast)
		doSubModuleTimerTriggers(sFastTimer);

	modulePostProcessing();

	bWasInit=false;
}

string[] aTicks = { "-", "\\", "|", "/", "-", "\\", "|", "/" };

int iTick = 99;
string tick()
{
	iTick++;
	if (iTick >= aTicks.Length)
		iTick = 0;
	return aTicks[iTick];

}


#endregion

void moduleDoPreModes()
{
	if (sReceivedMessage != "")
	{
		Echo("Processing Message:\n" + sReceivedMessage);

		if (sLastMessage == sReceivedMessage)
		{
			Echo("Clearing last message: Not processed");
			sReceivedMessage = ""; // clear it.
		}

		sLastMessage = sReceivedMessage;
	}
	else sLastMessage = "";
}

string sLastMessage = "";

void modulePostProcessing()
{
	Echo(lPendingIncomingMessages.Count + " Pending Incoming Messages");
	for (int i = 0; i < lPendingIncomingMessages.Count; i++)
		Echo(i + ":" + lPendingIncomingMessages[i]);

	Echo(sInitResults);

	float fper=0;
	fper=Runtime.CurrentInstructionCount/(float)Runtime.MaxInstructionCount;
	Echo("Instructions=" + (fper*100).ToString("0.00")+"%");
}

#region maininit

string sInitResults="";

string doInit()
{

	// initialization of each module goes here:

	// when all initialization is done, set init to true.
	initLogging();

	Echo("Init");
	if(currentRun==0) 
	{
StatusLog(DateTime.Now.ToString()+OurName+":"+moduleName+":INIT",textLongStatus,true);

//	if(!modeCommands.ContainsKey("launch")) modeCommands.Add("launch", MODE_LAUNCH);
//	if(!modeCommands.ContainsKey("godock")) modeCommands.Add("godock", MODE_DOCKING);

		sInitResults+=	gridsInit();

		initTimers();

		sInitResults+=initSerializeCommon();

		Deserialize();
		sInitResults+=	antennaInit();

		sInitResults+=modeOnInit(); // handle mode initializting from load/recompile..
		init=true;

	}

	currentRun++;
	if(init) currentRun=0;

	Log(sInitResults);

	return sInitResults;

}

IMyTextPanel gpsPanel=null;

string BlockInit()
{
	string sInitResults="";


	return sInitResults;
}

string modeOnInit()
{
	return ">";
}

#endregion

#region arguments

bool processArguments(string sArgument)
{

	if (sArgument == "" || sArgument == "timer" || sArgument == "wccs" || sArgument == "wcct")
	{
		Echo("Arg=" + sArgument);
	}
	else antReceive(sArgument);

	return false; // keep processing in main
}
#endregion

#region profiler
// Whip's Profiler Graph Code
int count = 1;
int maxSeconds = 60;
StringBuilder profile = new StringBuilder();
void ProfilerGraph()
{
	if (count <= maxSeconds) // assume 1 tick per second.
	{
		Echo("Profiler:Add");
		double timeToRunCode = Runtime.LastRunTimeMs;

		profile.Append(timeToRunCode.ToString()).Append("\n");
		count++;
	}
	else
	{
		Echo("Profiler:DISPLAY");
		var screen = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel;
		screen?.WritePublicText(profile.ToString());
		screen?.ShowPublicTextOnScreen();
	}
}

void resetProfiler()
{
	count = 1;
	profile = new StringBuilder();
	var screen = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel;
	screen?.WritePublicText("");
	screen?.ShowPublicTextOnScreen();

}
#endregion

#region domodes
void doModes()
{
    Echo("mode=" + iMode.ToString());	doModeAlways();
	doModeAlways();
/*
	if(iMode==MODE_IDLE && (craft_operation & CRAFT_MODE_SLED) > 0)
		setMode(MODE_SLEDMMOVE);

	if(iMode==MODE_DOCKED){doModeDocked();return;}
*/
}
#endregion

#region modealways

	
void doModeAlways()
{
	processPendingReceives();
}

#endregion

// Static Code modules

// V3.0 - redo all variables & cleanup
#region serializecommon
const string SAVE_FILE_NAME = "Wico Craft Save";
float savefileversion = 3.00f;
IMyTextPanel SaveFile = null;

// Saved info:
int current_state = 0;

long allBlocksCount = 0;

Vector3D vCurrentPos;
Vector3D vDock;
Vector3D vLaunch1;
Vector3D vHome;
bool bValidDock = false;
bool bValidLaunch1 = false;
bool bValidHome = false;
double dGravity = -2;
int craft_operation = CRAFT_MODE_AUTO;
int currentRun = 0;
string sPassedArgument = "";

// valid vectors
bool bValidInitialContact = false;
bool bValidInitialExit = false;
bool bValidTarget = false;
bool bValidAsteroid = false;
bool bValidNextTarget = false;

// operation flags
bool bAutopilotSet = true;
bool bAutoRelaunch = false;

// 
int iAlertStates = 0;

// time outs
DateTime dtStartShip;
DateTime dtStartCargo;
DateTime dtStartSearch;
DateTime dtStartMining;
DateTime dtLastRan;
DateTime dtStartNav;

// positions
//Vector3D vLastPos;
Vector3D vInitialContact;
Vector3D vInitialExit;
Vector3D vLastContact;
Vector3D vLastExit;
Vector3D vTargetMine;
Vector3D vTargetAsteroid;
Vector3D vCurrentNavTarget;
Vector3D vNextTarget;
Vector3D vExpectedExit;

// detection
int iDetects = 0;
int batterypcthigh = 80;
int batterypctlow = 20;
int batteryPercentage = -1;

int cargopctmin = 5;
int cargopcent = -1;
double cargoMult = -1;

// tanks
double hydroPercent = -1;
double oxyPercent = -1;

double totalMaxPowerOutput = 0;
double maxReactorPower = -1;
double maxSolarPower = -1;
double maxBatteryPower = -1;

string sReceivedMessage = "";

string initSerializeCommon()
{

    string sInitResults = "S";

	SaveFile = null;
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    blocks = GetBlocksNamed<IMyTextPanel>(SAVE_FILE_NAME);

	if (blocks.Count > 1) Echo("Multiple blocks found: \"" + SAVE_FILE_NAME + "\"");
	else if (blocks.Count == 0) Echo("Missing: " + SAVE_FILE_NAME);
	else SaveFile = blocks[0] as IMyTextPanel;

    if (SaveFile == null)
    {
        sInitResults = "-";
        Echo(SAVE_FILE_NAME + " (TextPanel) is missing or Named incorrectly. ");
    }
    return sInitResults;
}

bool validSavefile()
{
	return SaveFile != null;
}
string Vector3DToString(Vector3D v)
{
    string s;
    s = v.GetDim(0) + ":" + v.GetDim(1) + ":" + v.GetDim(2);
    return s;
}
bool ParseVector3d(string sVector, out double x, out double y, out double z)
{
    string[] coordinates = sVector.Trim().Split(',');
    if (coordinates.Length < 3)
    {
        coordinates = sVector.Trim().Split(':');
    }
    bool xOk = double.TryParse(coordinates[0].Trim(), out x);
    bool yOk = double.TryParse(coordinates[1].Trim(), out y);
    bool zOk = double.TryParse(coordinates[2].Trim(), out z);
    if (!xOk || !yOk || !zOk)
    {
        return false;
    }
    return true;
}

#endregion

//03/29 Optimization: don't write if same as when loaded 
#region mainserialize

// state variables
string sLastLoad = "";

void Serialize()
{
    string sb = "";
    sb += "Wico Craft Controller Saved State Do Not Edit" + "\n";
    sb += savefileversion.ToString("0.00") + "\n";

    sb += iMode.ToString() + "\n";
    sb += current_state.ToString() + "\n";
    sb += currentRun.ToString() + "\n";
	sb += sPassedArgument + "\n";
	sb += iAlertStates.ToString() + "\n";
    sb += dGravity.ToString() + "\n";

	sb += allBlocksCount.ToString() + "\n";

    sb += craft_operation.ToString() + "\n";


    sb += Vector3DToString(vDock) + "\n";
    sb += bValidDock.ToString() + "\n";

    sb += Vector3DToString(vLaunch1) + "\n";
    sb += bValidLaunch1.ToString() + "\n";

    sb += Vector3DToString(vHome) + "\n";
    sb += bValidHome.ToString() + "\n";

	sb += dtStartShip.ToString() + "\n";
	sb += dtStartCargo.ToString() + "\n";
	sb += dtStartSearch.ToString() + "\n";
	sb += dtStartMining.ToString() + "\n";
	sb += dtLastRan.ToString() + "\n";
	sb += dtStartNav.ToString() + "\n";

//	sb += Vector3DToString(vLastPos) + "\n";
	sb += Vector3DToString(vInitialContact) + "\n";
	sb += bValidInitialContact.ToString() + "\n";

	sb += Vector3DToString(vInitialExit) + "\n";
	sb += bValidInitialExit.ToString() + "\n";

	sb += Vector3DToString(vLastContact) + "\n";
	sb += Vector3DToString(vLastExit) + "\n";
	sb += Vector3DToString(vExpectedExit) + "\n";

	sb += Vector3DToString(vTargetMine) + "\n";
	sb += bValidTarget.ToString() + "\n";

	sb += Vector3DToString(vTargetAsteroid) + "\n";
	sb += bValidAsteroid.ToString() + "\n";

	sb += Vector3DToString(vNextTarget) + "\n";
	sb += bValidNextTarget.ToString() + "\n";

	sb += Vector3DToString(vCurrentNavTarget) + "\n";


	sb += bAutopilotSet.ToString() + "\n";
	sb += bAutoRelaunch.ToString() + "\n";
	sb += iDetects.ToString() + "\n";

	sb += batterypcthigh.ToString() + "\n";
	sb += batterypctlow.ToString() + "\n";
	sb += batteryPercentage.ToString() + "\n";

	sb += cargopctmin.ToString() + "\n";
	sb += cargopcent.ToString() + "\n";
	sb += cargoMult.ToString() + "\n";

	sb += hydroPercent.ToString() + "\n";
	sb += oxyPercent.ToString() + "\n";

	sb += totalMaxPowerOutput.ToString() + "\n";
	sb += maxReactorPower.ToString() + "\n";
	sb += maxSolarPower.ToString() + "\n";
	sb += maxBatteryPower.ToString() + "\n";
	sb += sReceivedMessage + "\n";

    if (SaveFile == null)
    {
        Storage = sb.ToString();
        return;
    }
	if (sLastLoad != sb) SaveFile.WritePublicText(sb.ToString(), false);
	else Echo("Not saving: Same");
}

void Deserialize()
{
    double x, y, z;

    string sSave;
    if (SaveFile == null)
        sSave = Storage;
    else
        sSave = SaveFile.GetPublicText();

    if (sSave.Length < 1)
    {
        Echo("Saved information not available");
        return;
    }
	sLastLoad = sSave;

    int i = 1;
    float fVersion = 0;

    string[] atheStorage = sSave.Split('\n');

    // Trick using a "local method", to get the next line from the array `atheStorage`.
    Func<string> getLine = () => {
        return (i >= 0 && atheStorage.Length > i ? atheStorage[i++] : null);
    };

    if (atheStorage.Length < 3)
    {
        // invalid storage
        Storage = "";
        Echo("Invalid Storage");
        return;
    }

    // Simple "local method" which returns false/true, depending on if the
    // given `txt` argument contains the text "True" or "true".
    Func<string, bool> asBool = (txt) => {
        txt = txt.Trim().ToLower();
        return (txt == "True" || txt == "true");
    };

    fVersion = (float)Convert.ToDouble(getLine());

    if (fVersion > savefileversion)
    {
        Echo("Save file version mismatch; it is newer. Check programming blocks.");
        return; // it is something NEWER than us..
    }
	if(fVersion<2.99)
	{
		Echo("Obsolete save. ignoring:"+ fVersion.ToString());
		return;
	}
    iMode = Convert.ToInt32(getLine());
    current_state = Convert.ToInt32(getLine());
    currentRun = Convert.ToInt32(getLine());
	sPassedArgument = getLine();

	iAlertStates = Convert.ToInt32(getLine());

    bool pOK;
    pOK = double.TryParse(getLine(), out dGravity);
    pOK = long.TryParse(getLine(), out allBlocksCount);

    craft_operation = Convert.ToInt32(getLine());

    ParseVector3d(getLine(), out x, out y, out z);
    vDock = new Vector3D(x, y, z);
    bValidDock = asBool(getLine());

    ParseVector3d(getLine(), out x, out y, out z);
    vLaunch1 = new Vector3D(x, y, z);
    bValidLaunch1 = asBool(getLine().ToLower());

    ParseVector3d(getLine(), out x, out y, out z);
    vHome = new Vector3D(x, y, z);
    bValidHome = asBool(getLine());

	dtStartShip = DateTime.Parse(getLine());
	dtStartCargo = DateTime.Parse(getLine());
	dtStartSearch = DateTime.Parse(getLine());
	dtStartMining = DateTime.Parse(getLine());
	dtLastRan = DateTime.Parse(getLine());
	dtStartNav = DateTime.Parse(getLine());
	/*
	ParseVector3d(getLine(), out x, out y, out z);
	vLastPos = new Vector3D(x, y, z);
	*/
	ParseVector3d(getLine(), out x, out y, out z);
	vInitialContact = new Vector3D(x, y, z);
	bValidInitialContact = asBool(getLine());

	ParseVector3d(getLine(), out x, out y, out z);
	vInitialExit = new Vector3D(x, y, z);
	bValidInitialExit = asBool(getLine());

	ParseVector3d(getLine(), out x, out y, out z);
	vLastContact = new Vector3D(x, y, z);

	ParseVector3d(getLine(), out x, out y, out z);
	vLastExit = new Vector3D(x, y, z);

	ParseVector3d(getLine(), out x, out y, out z);
	vExpectedExit = new Vector3D(x, y, z);

	ParseVector3d(getLine(), out x, out y, out z);
	vTargetMine = new Vector3D(x, y, z);
	bValidTarget = asBool(getLine());

	ParseVector3d(getLine(), out x, out y, out z);
	vTargetAsteroid = new Vector3D(x, y, z);
	bValidAsteroid = asBool(getLine());

	ParseVector3d(getLine(), out x, out y, out z);
	vNextTarget = new Vector3D(x, y, z);
	bValidNextTarget = asBool(getLine());

	ParseVector3d(getLine(), out x, out y, out z);
	vCurrentNavTarget = new Vector3D(x, y, z);

	bAutopilotSet = asBool(getLine());
	bAutoRelaunch = asBool(getLine());

	iDetects = Convert.ToInt32(getLine());

	batterypcthigh = Convert.ToInt32(getLine());
	batterypctlow = Convert.ToInt32(getLine());
	batteryPercentage = Convert.ToInt32(getLine());

	cargopctmin = Convert.ToInt32(getLine());
	cargopcent = Convert.ToInt32(getLine());
	cargoMult = Convert.ToDouble(getLine());

	hydroPercent = Convert.ToDouble(getLine());
	oxyPercent = Convert.ToDouble(getLine());

	totalMaxPowerOutput = Convert.ToDouble(getLine());
	maxReactorPower = Convert.ToDouble(getLine());
	maxSolarPower = Convert.ToDouble(getLine());
	maxBatteryPower = Convert.ToDouble(getLine());

	sReceivedMessage = getLine();
}

#endregion

// 12/19 cleanup
#region config

const int CRAFT_MODE_AUTO = 0;
const int CRAFT_MODE_SLED = 2;
const int CRAFT_MODE_ROTOR = 4;
const int CRAFT_MODE_ORBITAL = 32;
const int CRAFT_MODE_ROCKET = 64;
const int CRAFT_MODE_PET = 128;
const int CRAFT_MODE_NAD = 256; // no auto dock
const int CRAFT_MODE_NOAUTOGYRO = 512;
const int CRAFT_MODE_NOPOWERMGMT = 1024;
const int CRAFT_MODE_NOTANK = 2048;
const int CRAFT_MODE_MASK = 0xfff;

//int craft_operation = CRAFT_MODE_AUTO;

string craftOperation()
{
    string sResult = "FLAGS:";
  //  sResult+=craft_operation.ToString();
    if ((craft_operation & CRAFT_MODE_SLED) > 0)
        sResult += "SLED ";
    if ((craft_operation & CRAFT_MODE_ORBITAL) > 0)
        sResult += "ORBITAL ";
    if ((craft_operation & CRAFT_MODE_ROCKET) > 0)
        sResult += "ROCKET ";
    if ((craft_operation & CRAFT_MODE_ROTOR) > 0)
        sResult += "ROTOR ";
    if ((craft_operation & CRAFT_MODE_PET) > 0)
        sResult += "PET ";
    if ((craft_operation & CRAFT_MODE_NAD) > 0)
        sResult += "NAD ";
    if ((craft_operation & CRAFT_MODE_NOAUTOGYRO) > 0)
        sResult += "NO Gyro ";
    if ((craft_operation & CRAFT_MODE_NOTANK) > 0)
        sResult += "No Tank ";
    if ((craft_operation & CRAFT_MODE_NOPOWERMGMT) > 0)
        sResult += "No Power ";
    return sResult;
}
#endregion

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
const int MODE_ORBITALLAUNCH = 28;

const int MODE_ARRIVEDTARGET=29; // we have arrived at target
const int MODE_ARRIVEDHOME=30; // we have arrived at home

const int MODE_UNDERCONSTRUCTION = 31;

const int MODE_PET = 111; // pet that follows the player

void setMode(int newMode)
{
    if (iMode == newMode) return;
    iMode = newMode;
    current_state = 0;
}

#endregion

// 04/08: NOFOLLOW for rotors (for print heads)
// 03/10 moved definition of allBlocksCount to serialize. Fixed piston localgrid
// check customdata for contains 01/07/17
// add allBlocksCount
// cross-grid 12/19
// split grids/blocks
#region getgrids
List<IMyTerminalBlock> gtsAllBlocks = new List<IMyTerminalBlock>();

List <IMyCubeGrid> localGrids =new List<IMyCubeGrid>();
List <IMyCubeGrid> remoteGrids =new List<IMyCubeGrid>();
List <IMyCubeGrid> dockedGrids =new List<IMyCubeGrid>();
List <IMyCubeGrid> allGrids =new List<IMyCubeGrid>();


bool calcGridSystemChanged()
{
	List<IMyTerminalBlock> gtsTestBlocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType < IMyTerminalBlock > (gtsTestBlocks);
	if (allBlocksCount != gtsTestBlocks.Count)
		return true;
	return false;
}
string gridsInit()
{
	gtsAllBlocks.Clear();
	allGrids.Clear();
	localGrids.Clear();
	remoteGrids.Clear();
	dockedGrids.Clear();

	GridTerminalSystem.GetBlocksOfType < IMyTerminalBlock > (gtsAllBlocks);
	allBlocksCount = gtsAllBlocks.Count;

	foreach (var block in gtsAllBlocks)
	{
		var grid = block.CubeGrid;
		if (!allGrids.Contains(grid))
		{
			allGrids.Add(grid);
		}
	}
	addGridToLocal(Me.CubeGrid); // the PB is known to be local..  Start there.

	foreach (var grid in allGrids)
	{
		if (localGrids.Contains(grid))
			continue; // already in the list;
		bool bConnected = false;

		List<IMyShipConnector> gridConnectors = new List<IMyShipConnector>();
		GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(gridConnectors, (x => x.CubeGrid == grid));
		foreach (var connector in gridConnectors)
		{
			if (connector.Status==MyShipConnectorStatus.Connected)
			{
				if(localGrids.Contains(connector.OtherConnector.CubeGrid) || remoteGrids.Contains(connector.OtherConnector.CubeGrid))
				{ // if the other connector is connected to an already known grid, ignore it.
					continue;
				}
				if (localGrids.Contains(connector.OtherConnector.CubeGrid))
					bConnected = true;
				else bConnected = false;
			}
		}

		if(bConnected)
		{
			if (!dockedGrids.Contains(grid))
			{
				dockedGrids.Add(grid);
			}

		}
		if (!remoteGrids.Contains(grid))
		{
			remoteGrids.Add(grid);
		}
	}


	string s = "";
	s += "B"+gtsAllBlocks.Count.ToString();
	s += "G"+allGrids.Count.ToString();
	s += "L"+localGrids.Count.ToString();
	s += "D"+dockedGrids.Count.ToString();
	s += "R"+remoteGrids.Count.ToString();

	Echo("Found " + gtsAllBlocks.Count.ToString() + " Blocks");
	Echo("Found " + allGrids.Count.ToString() + " Grids");
	Echo("Found " + localGrids.Count.ToString() + " Local Grids");
	for (int i = 0; i < localGrids.Count; i++) Echo("|"+localGrids[i].CustomName);
	Echo("Found " + dockedGrids.Count.ToString() + " Docked Grids");
	for (int i = 0; i < dockedGrids.Count; i++) Echo("|"+dockedGrids[i].CustomName);
	Echo("Found " + remoteGrids.Count.ToString() + " Remote Grids");
	for (int i = 0; i < remoteGrids.Count; i++) Echo("|"+remoteGrids[i].CustomName);

	return s;
}

void addGridToLocal(IMyCubeGrid grid)
{
	if (grid == null) return;
	if (!localGrids.Contains(grid))
	{
		localGrids.Add(grid);

		addRotorsConnectedToGrids(grid);
		addPistonsConnectedToGrids(grid);
		addGridsToLocalRotors(grid);
		addGridsToLocalPistons(grid);
	}
}

void addRotorsConnectedToGrids(IMyCubeGrid grid)
{ 
	List<IMyMotorStator> gridRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(gridRotors, (x => x.TopGrid == grid));
	foreach (var rotor in gridRotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		addGridToLocal(rotor.CubeGrid);
	}
	List<IMyMotorAdvancedStator> gridARotors = new List<IMyMotorAdvancedStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(gridARotors, (x => x.TopGrid == grid));
	foreach (var rotor in gridARotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		addGridToLocal(rotor.CubeGrid);
	}
}

void addPistonsConnectedToGrids(IMyCubeGrid grid)
{ 
	List<IMyPistonBase> gridPistons = new List<IMyPistonBase>();
	GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(gridPistons, (x => x.TopGrid == grid));
	foreach (var piston in gridPistons)
	{
		addGridToLocal(piston.CubeGrid);
	}
}

void addGridsToLocalRotors(IMyCubeGrid grid)
{
	List<IMyMotorStator> gridRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(gridRotors, (x => x.CubeGrid == grid));
	foreach (var rotor in gridRotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		IMyCubeGrid topGrid = rotor.TopGrid;
		if (topGrid != null && topGrid!=grid)
		{
			addGridToLocal(topGrid);
		}
	}
	gridRotors.Clear();

	List<IMyMotorAdvancedStator> gridARotors = new List<IMyMotorAdvancedStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(gridARotors, (x => x.CubeGrid == grid));
	foreach (var rotor in gridARotors)
	{
		if (rotor.CustomName.Contains("NOFOLLOW") || rotor.CustomData.Contains("NOFOLLOW"))
			continue;
		IMyCubeGrid topGrid = rotor.TopGrid;
		if (topGrid != null && topGrid!=grid)
		{
			addGridToLocal(topGrid);
		}
	}

}
void addGridsToLocalPistons(IMyCubeGrid grid)
{
	List<IMyPistonBase> gridPistons = new List<IMyPistonBase>();
	GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(gridPistons, (x => x.CubeGrid == grid));
	foreach (var piston in gridPistons)
	{
		IMyCubeGrid topGrid = piston.TopGrid;
//		if (topGrid != null) Echo(piston.CustomName + " Connected to grid:" + topGrid.CustomName);
		if (topGrid != null && topGrid != grid)
		{
			if (!localGrids.Contains(topGrid))
			{
				addGridToLocal(topGrid);
			}
		}
	}
}

List <IMyCubeGrid> calculateLocalGrids()
{
	if (localGrids.Count < 1)
	{
		gridsInit();
	}
	return localGrids;
}
List <IMyCubeGrid> calculateDockedGrids()
{
	if (localGrids.Count < 1)
	{
		gridsInit();
	}
	return dockedGrids;
}

bool localGridFilter(IMyTerminalBlock block)
{
	return calculateLocalGrids().Contains(block.CubeGrid);
}

bool dockedGridFilter(IMyTerminalBlock block)
{
	List <IMyCubeGrid> g=calculateDockedGrids();
	if(g==null) return false;
	return g.Contains(block.CubeGrid);
}

#endregion

// 03/09: Init grids on get if needed
// 02/25: use cached block list from grids
// split code into grids and blocks
#region getblocks
IMyTerminalBlock get_block(string name)
{
    IMyTerminalBlock block;
	block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(name);
	if (block == null)
        throw new Exception(name + " Not Found"); return block;
}

public List<IMyTerminalBlock> GetTargetBlocks<T>(ref List<IMyTerminalBlock>  Output,string Keyword = null) where T : class
{
	if (gtsAllBlocks.Count < 1) gridsInit();
	Output.Clear();
    for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) && gtsAllBlocks is T && ((Keyword == null) || (Keyword != null && gtsAllBlocks[e].CustomName.StartsWith(Keyword))))
        {
            Output.Add(gtsAllBlocks[e]);
        }
    }
    return Output;
}
public List<IMyTerminalBlock> GetTargetBlocks<T>(string Keyword = null) where T : class
{
    List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
	GetTargetBlocks<T>(ref Output, Keyword);
    return Output;
}
public List<IMyTerminalBlock> GetBlocksContains<T>(string Keyword = null) where T : class
{
	if (gtsAllBlocks.Count < 1) gridsInit();
    List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
	for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) &&  gtsAllBlocks[e] is T && Keyword != null && (gtsAllBlocks[e].CustomName.Contains(Keyword) || gtsAllBlocks[e].CustomData.Contains(Keyword)))
        {
			Output.Add(gtsAllBlocks[e]);
		}
    }
    return Output;
}
public List<IMyTerminalBlock> GetBlocksNamed<T>(string Keyword = null) where T : class
{
	if (gtsAllBlocks.Count < 1) gridsInit();
    List<IMyTerminalBlock> Output = new List<IMyTerminalBlock>();
	for (int e = 0; e < gtsAllBlocks.Count; e++)
    {
        if (localGridFilter(gtsAllBlocks[e]) && gtsAllBlocks[e] is T && Keyword != null && gtsAllBlocks[e].CustomName == Keyword)
        {
			Output.Add(gtsAllBlocks[e]);
		}
    }
    return Output;
}

#endregion

#region blockactions
void groupApplyAction(string sGroup, string sAction)
{
    List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(groups);
	for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
    {
        if (groups[groupIndex].Name == sGroup)
        { //var blocks=null;
            List<IMyTerminalBlock> blocks = null;
            groups[groupIndex].GetBlocks(blocks, localGridFilter);

            //blocks=groups[groupIndex].Blocks;
            List<IMyTerminalBlock> theBlocks = blocks;
			for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
            {
				theBlocks[iIndex].ApplyAction(sAction);
			}
            return;
        }
    }
    return;
}
void listSetValueFloat(List<IMyTerminalBlock> theBlocks, string sProperty, float fValue)
{
    for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
    {
        if (theBlocks[iIndex].CubeGrid == Me.CubeGrid)
            theBlocks[iIndex].SetValueFloat(sProperty, fValue);
    }
    return;
}
void blockApplyAction(string sBlock, string sAction)
{ List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); blocks = GetBlocksNamed<IMyTerminalBlock>(sBlock); blockApplyAction(blocks, sAction); }
void blockApplyAction(IMyTerminalBlock sBlock, string sAction)
{ ITerminalAction ita; ita = sBlock.GetActionWithName(sAction); if (ita != null) ita.Apply(sBlock); else Echo("Unsupported action:" + sAction); }
void blockApplyAction(List<IMyTerminalBlock> lBlock, string sAction)
{
    if (lBlock.Count > 0)
    {
        for (int i = 0; i < lBlock.Count; i++)
        {
			ITerminalAction ita;
			ita = lBlock[i].GetActionWithName(sAction);
			if (ita != null)
				ita.Apply(lBlock[i]);
			else
				Echo("Unsupported action:" + sAction);
		}
    }
}
#endregion

// 2/25: Performance: only check blocks once, re-check on init.
// use cached blocks 12/xx
#region logging

string sLongStatus = "Wico Craft Log";
string sTextPanelReport = "Craft Report";
IMyTextPanel statustextblock = null;
IMyTextPanel textLongStatus = null;
IMyTextPanel textPanelReport = null;
bool bLoggingInit = false;

void initLogging()
{
	statustextblock = getTextStatusBlock(true);
	textLongStatus = getTextBlock(sLongStatus);;
	textPanelReport = getTextBlock(sTextPanelReport);
	bLoggingInit = true;
}

IMyTextPanel getTextBlock(string stheName)
{
    IMyTextPanel textblock = null;
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
	blocks = GetBlocksNamed<IMyTerminalBlock>(stheName);
	if (blocks.Count < 1)
    {
        blocks = GetBlocksContains<IMyTextPanel>(stheName);
		if (blocks.Count > 0)
            textblock = blocks[0] as IMyTextPanel;
    }
    else if (blocks.Count > 1)
        throw new OurException("Multiple status blocks found: \"" + stheName + "\"");
    else textblock = blocks[0] as IMyTextPanel;
	return textblock;
}

IMyTextPanel getTextStatusBlock(bool force_update = false)
{
	if ((statustextblock != null || bLoggingInit) && !force_update ) return statustextblock;
	statustextblock = getTextBlock(OurName + " Status");
	return statustextblock;
}
void StatusLog(string text, IMyTextPanel block, bool bReverse = false)
{
    if (block == null) return;
    if (text.Equals("clear"))
    {
        block.WritePublicText("");
    }
    else
    {
        if (bReverse)
        {
            string oldtext = block.GetPublicText();
            block.WritePublicText(text + "\n" + oldtext);
        }
        else block.WritePublicText(text + "\n", true);
        // block.WritePublicTitle(DateTime.Now.ToString());
    }
    block.ShowTextureOnScreen();
    block.ShowPublicTextOnScreen();
}

void Log(string text)
{
	StatusLog(text, getTextStatusBlock());
}
string progressBar(double percent)
{
	int barSize = 75;
	if (percent < 0) percent = 0;
	int filledBarSize = (int)(percent * barSize) / 100;
	if (filledBarSize > barSize) filledBarSize = barSize;
	string sResult = "[" + new String('|', filledBarSize) + new String('\'', barSize - filledBarSize) + "]";
	return sResult;
}

#endregion

//03/27: Added caching for performance
#region triggers
Dictionary<string, List<IMyTerminalBlock>> dTimers = new Dictionary<string, List<IMyTerminalBlock>>();

void initTimers()
{
	dTimers.Clear();
}

void doSubModuleTimerTriggers(string sKeyword = "[WCCS]")
{
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

	IMyTimerBlock theTriggerTimer = null;

	if (dTimers.ContainsKey(sKeyword))
	{
		blocks = dTimers[sKeyword];
	}
	else
	{
		blocks = GetBlocksContains<IMyTerminalBlock>(sKeyword);
		dTimers.Add(sKeyword, blocks);
	}

	for (int i = 0; i < blocks.Count; i++)
    {
        theTriggerTimer = blocks[i] as IMyTimerBlock;
        if (theTriggerTimer != null)
        {
//            Echo("dSMT:" + blocks[i].CustomName);
            theTriggerTimer.ApplyAction("TriggerNow");
        }
    }

}

#endregion

#region Antenna

List<IMyTerminalBlock> antennaList = new List<IMyTerminalBlock>();

string antennaInit()
{
	antennaList.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennaList, localGridFilter);

    return "A" + antennaList.Count.ToString("0");
}

//// Verify antenna stays on to fix keen bug where antenna will turn itself off when you try to remote control
void verifyAntenna()
{
    blockApplyAction(antennaList, "OnOff_On");
}
#endregion

#region AntennaReceive

List<string> lPendingIncomingMessages = new List<string>();

void processPendingReceives()
{
	if(lPendingIncomingMessages.Count>0)
	{
		if (sReceivedMessage == "")
		{ // receiver signals processed by removing message
			sReceivedMessage = lPendingIncomingMessages[0];
			lPendingIncomingMessages.RemoveAt(0);
		}
	}
	if(lPendingIncomingMessages.Count>0) bWantFast = true; // if there are more, process quickly
}
void antReceive(string message)
{
	Echo("RECEIVE:\n" + message);
	lPendingIncomingMessages.Add(message);
	bWantFast = true;
}
#endregion
    }
}