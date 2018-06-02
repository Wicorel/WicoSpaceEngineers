/* Wico Gimbal Thrusters
 * 
 * http://steamcommunity.com/sharedfiles/filedetails/?id=864963505
 * 
 * Version 3.1
 * 
 * 2.1 : 
 * Gimbal system with sub-rotors.
 * Load settings from CustomData
 * 
 * 2.2: Rotor lock
 * 
 * 2.3: Detect grid changes (dock/undock) and re-init
 * Detects Main cockpit (if present) and only pays attention to it.
 * Changed PB name setting to only add sID to name if not present
 * 
 * 2.4 when no player inputs, align to counter (main direction of) current movement if velocity>1
 * 
 * 2.5 Performance optimizations
 * 
 * 2.6 Allow turning auto dampener mode off.
 * Updated grid and block code
 * "In-gravity" angles
 * 
 * 2.7 setting max velocity.  Use -1 to use rotor's max velocity automatically.  Default is 3
 * 
 * 2.8 Performance Optimizations (caching timer blocks)
 * 
 * 2.9 NOFOLLOW for rotors (for print heads)
 * 
 * 2.9B remove turning on safetylock
 * 
 * 3.1 SE 1.185 PB changes.
 * 
 * 3.1A changes for SE 1.185.200 (add RotorLock)
 * 
 * 3.1B Set "unlimited" upper & Lower limit using SetValueFloat since setter doesn't allow max/min
 * 
 * 3.1C Set upper/lower to constrain movement during rotor movement
 * 
 * 
 */

string sVersion = "3.1C";
string OurName = "Wico Gimbal";
string moduleName = "Gimbal Control";

//string sFast = "[GIMBAL]"; // put this in name of timer that runs this PB
string sID = "[GIMBAL]"; // put this name in CustomName or CustomData of rotors to be controlled

public Program() 
{
	initLogging();
//    StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
	if (!Me.CustomName.Contains(sID))
		Me.CustomName = Me.CustomName +" " + sID;
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

string Vector3DToString(Vector3D v)
{
    string s;
    s = v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00");
    return s;
}

#region MAIN

bool init = false;
bool bWasInit = false;
bool bWantFast = false;

//bool bWorkingProjector = false;

Vector3D vCurrentPos;
IMyTerminalBlock gpsCenter = null;
double dGravity = -2;
double velocityShip;//, velocityForward, velocityUp, velocityLeft;

class OurException : Exception
{
    public OurException(string msg) : base("WicoCraft" + ": " + msg) { }
}

void Main(string sArgument)
{

	Echo(OurName + ":" + moduleName+" V"+sVersion +" "+tick());
	Log("clear");
	Log(OurName + ":" + moduleName + " V" + sVersion);
	bWantFast = false;
	//ProfilerGraph();

	string output = "";
	if (sArgument == "init" || calcGridSystemChanged())
	{
		//		StatusLog("MASS CHANGE!", masstextBlock);

		Echo("Arg init or mass change!");
		gridBaseMass = 0;
		sInitResults = "";
		init = false;
		currentInit = 0;
		//		sPassedArgument = "init";
	}
	if (!init)
	{
		bWantFast = true;
		doInit();
		bWasInit = true;
	}
	else
	{
		if (bWasInit)
		{
			StatusLog(DateTime.Now.ToString() + " " + sInitResults, textLongStatus, true);
		}
		//        Echo(sInitResults);

		vCurrentPos = gpsCenter.GetPosition();

		if (gpsCenter is IMyRemoteControl)
		{
			Vector3D vNG = ((IMyRemoteControl)gpsCenter).GetNaturalGravity();
			double dLength = vNG.Length();
			dGravity = dLength / 9.81;

			if (dGravity > 0)
			{
				double elevation = 0;

				((IMyRemoteControl)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
				Echo("Elevation=" + elevation.ToString("0.00"));

				double altitude = 0;
				((IMyRemoteControl)gpsCenter).TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude);
				Echo("Sea Level=" + altitude.ToString("0.00"));
			}

		}
		else
		{
			dGravity = -1.0;
		}

		velocityShip = ((IMyShipController)gpsCenter).GetShipSpeed();
		IMyShipController isc = GetActiveController();
		if (isc == null)
		{
			output += "No Active Controller\n";
			stopGimbals();
		}
		else
		{
			bWantFast = true;
	Echo("Using Active Controller: " + isc.CustomName);
//			if (isc.CanControlShip) Echo("Can Control Ship"); else Echo("CANNOT control Ship");

			Vector3 vInputs = isc.MoveIndicator;
			Vector2 vRotate = isc.RotationIndicator;
			float fRoll = isc.RollIndicator;

			Vector3D av = isc.GetShipVelocities().AngularVelocity;
			Vector3D lv = isc.GetShipVelocities().LinearVelocity;
			output += "Player Inputs:\n";
			output += vInputs.ToString() + "\n";
			output += vRotate.ToString() + "\n";
			output += fRoll.ToString() + "\n";

//			MyShipVelocities msv=isc.GetShipVelocities();

			output += "\nShip Velocities:\n" + Vector3DToString(lv) + "\n";
			output += "AV=" + Vector3DToString(av) + "\n";
			
			double dInputs = Math.Abs(vInputs.X) + Math.Abs(vInputs.Y) + Math.Abs(vInputs.Z);
            if (dInputs < .5 && velocityShip > 1)
            {
                output += "Dampen\n";
                Vector3D vLv = lv;
                vLv.X = -vLv.X;
                vLv.Y = -vLv.Y;
                vLv.Z = -vLv.Z;
                processGimbals(vLv, true);
            }
            else
            {
                output += "Player Input\n";
                processGimbals(vInputs);
            }
		}
		output += "V=" + velocityShip.ToString("0.00") + "\n";
		Echo(output);
	    Log(output);

	}

    if (bWantFast)
    {
        Echo("FAST!");
        if (bWasInit)
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update1;
        }
        else
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
        }
        //		doSubModuleTimerTriggers(sFast);
    }
    else
    {
        Runtime.UpdateFrequency &= ~(UpdateFrequency.Update10 | UpdateFrequency.Update1);
    }

	bWasInit = false;

	EchoInstructionPer();

	//	Echo("Passing:'" + sPassedArgument + "'");
	Echo(sInitResults);
}

void EchoInstructionPer(string sPrefix="")
{
	float fper = 0;
	fper = Runtime.CurrentInstructionCount / (float)Runtime.MaxInstructionCount;
	Echo(sPrefix+"Instructions=" + (fper * 100).ToString() + "%");
}

#endregion

// Change per module
#region maininit

string sInitResults = "";
int currentInit = 0;

double gridBaseMass = 0;

string doInit()
{

	// initialization of each module goes here:

	// when all initialization is done, set init to true.
	initLogging();
	initTimers();

    Log("Init:" + currentInit.ToString());
    double progress = currentInit * 100 / 3; // 3=Number of expected INIT phases.
    string sProgress = progressBar(progress);
    StatusLog(moduleName + sProgress, textPanelReport);
	
    Echo("Init");
    if (currentInit == 0)
    {
//        StatusLog("clear", textLongStatus, true); // only MAIN module should clear long status on init.
        StatusLog(DateTime.Now.ToString() + " " + OurName + ":" + moduleName + ":INIT", textLongStatus, true);

        /*
        if(!modeCommands.ContainsKey("launchprep")) modeCommands.Add("launchprep", MODE_LAUNCHPREP);
        */
        // parseConfiguration();
 //       sInitResults += initSerializeCommon();
		sInitResults+=	gridsInit();

//		Deserialize(); // get info from savefile to avoid blind-rewrite of (our) defaults
    }
    else if (currentInit == 1)
    {
//		Deserialize();// get info from savefile to avoid blind-rewrite of (our) defaults

        sInitResults += BlockInit();
		//		initCargoCheck();

		//        Serialize();
		sInitResults += gimbalsInit();
		sInitResults += controllersInit();
        bWantFast = false;

		if (gpsCenter != null)
		{
			MyShipMass myMass;
			myMass = ((IMyShipController)gpsCenter).CalculateShipMass();

			gridBaseMass = myMass.BaseMass;

	        init = true; // we are donw

		}
		else
		{
			// we are not complete.  try again..
			currentInit=0;
			bWantFast = false;
			Echo("Missing Required Item; Please add");
			return sInitResults;
		}

    }

    currentInit++;
    if (init) currentInit = 0;

    Log(sInitResults);

    return sInitResults;
}

IMyTextPanel gpsPanel = null;
string sGPSCenter = "XXXXX";
string BlockInit()
{
    string sInitResults = "";

    List<IMyTerminalBlock> centerSearch = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(sGPSCenter, centerSearch, localGridFilter);
    if (centerSearch.Count == 0)
    {
        centerSearch = GetBlocksContains<IMyRemoteControl>("[NAV]");
        if (centerSearch.Count == 0) centerSearch = GetBlocksContains<IMyRemoteControl>("Reference"); // RHINO main seat
        if (centerSearch.Count == 0)
        {
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(centerSearch, localGridFilter);
            if (centerSearch.Count == 0)
            {
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(centerSearch, localGridFilter);
//                GridTerminalSystem.GetBlocksOfType<IMyShipController>(centerSearch, localGridFilter);
                if (centerSearch.Count == 0)
                {
                    sInitResults += "!!NO Controller";
                    Echo("No Controller found");
                }
                else
                {
                    sInitResults += "S";
                    Echo("Using first ship Controller found: " + centerSearch[0].CustomName);
                }
            }
            else
            {
                sInitResults += "R";
                Echo("Using First Remote control found: " + centerSearch[0].CustomName);
            }
        }
    }
    else
    {
        sInitResults += "N";
        Echo("Using Named: " + centerSearch[0].CustomName);
    }
	if(centerSearch.Count>0)
	    gpsCenter = centerSearch[0];

    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    blocks = GetBlocksContains<IMyTextPanel>("[GPS]");
    if (blocks.Count > 0)
        gpsPanel = blocks[0] as IMyTextPanel;

    return sInitResults;
}

string modeOnInit()
{
    return ">";
}


#endregion

long allBlocksCount = 0;


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
	{
		return true;
	}
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

// 02/15 localGridFilter
#region MIN_blockactions
void groupApplyAction(string sGroup, string sAction)
{
	List<IMyBlockGroup> groups = new List<IMyBlockGroup>(); GridTerminalSystem.GetBlockGroups(groups); for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
	{
		if (groups[groupIndex].Name == sGroup)
		{
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			groups[groupIndex].GetBlocks(blocks, localGridFilter);
			List<IMyTerminalBlock> theBlocks = blocks;
			for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
			{ theBlocks[iIndex].ApplyAction(sAction); }
			return;
		}
	}
	return;
}
void listSetValueFloat(List<IMyTerminalBlock> theBlocks, string sProperty, float fValue)
{
	for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
	{
//		if (theBlocks[iIndex].CubeGrid == Me.CubeGrid)
			theBlocks[iIndex].SetValueFloat(sProperty, fValue);
	}
	return;
}
void listSetValueBool(List<IMyTerminalBlock> theBlocks, string sProperty, bool bValue)
{
	for (int iIndex = 0; iIndex < theBlocks.Count; iIndex++)
	{
//		if (theBlocks[iIndex].CubeGrid == Me.CubeGrid)
			theBlocks[iIndex].SetValueBool(sProperty, bValue);
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
		{ ITerminalAction ita; ita = lBlock[i].GetActionWithName(sAction); if (ita != null) ita.Apply(lBlock[i]); else Echo("Unsupported action:" + sAction); }
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


#region shipcontrollers
 
List < IMyTerminalBlock > controllersList = new List < IMyTerminalBlock > (); 

List <IMyTerminalBlock> remoteControl1List = new List < IMyTerminalBlock > (); 
 
string controllersInit()
{
	controllersList.Clear();
	remoteControl1List.Clear();
//	controllersList=GetTargetBlocks<IMyShipController>(ref controllersList);
	GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllersList, localGridFilter);

	for (int i = 0; i < controllersList.Count; i++)
	{
		if (controllersList[i] is IMyRemoteControl)
			remoteControl1List.Add(controllersList[i]);
	}
	return "SC" + controllersList.Count.ToString("0");
}

IMyShipController GetActiveController()
{
	IMyShipController sc = null;

	bool bHasMain = false;
	for (int i = 0; i < controllersList.Count; i++)
	{
		IMyCockpit imyc = controllersList[i] as IMyCockpit;
		if (imyc != null)
		{
			if (imyc.IsMainCockpit)
			{
				bHasMain = true;
				if (imyc.IsUnderControl)
					return imyc;
				else Echo("Main cockpit not occupied:" + imyc.CustomName);
			}
		}
	}
	if (bHasMain) return sc; // there IS a main and it's not occupied.

	for (int i = 0; i < controllersList.Count; i++)
	{
		if (((IMyShipController)controllersList[i]).IsUnderControl)
		{
			sc = controllersList[i] as IMyShipController;
			break;
		}
	}
	return sc;
}

#endregion

#region GIMBAL_ROTORS
 
List < GimbalRotor > gimbalList = new List < GimbalRotor > (); 

public class GimbalRotor
{
	public IMyMotorStator r;
	public float zminus; // forward
	public float zplus; // backward
	public float yminus; // down
	public float yplus; // up
	public float xminus; // right
	public float xplus; // left

	public bool bAutoDampen;
	public bool bGravity;

	public float gzminus;
	public float gzplus;
	public float gyminus;
	public float gyplus;
	public float gxminus;
	public float gxplus;

	public float gdefault;

	public float maxVelocity;

	public GimbalRotor subRotor;
	public float targetAngle;
	public List<IMyTerminalBlock> thrusters;
}

string gimbalsInit()
{
	gimbalList.Clear();

	List < IMyTerminalBlock > rotorsList = new List < IMyTerminalBlock > (); 
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(rotorsList, (x=>x.CubeGrid==Me.CubeGrid));

	for (int i = 0; i < rotorsList.Count; i++)
	{
		if(rotorsList[i].CustomName.Contains(sID ) || rotorsList[i].CustomData.Contains(sID ) )
		{
			GimbalRotor gr = new GimbalRotor();
			gimbalLoad(rotorsList[i], gr);
			Echo(gr.r.CustomName);
			gimbalList.Add(gr);
		}
	}
	string s = "";
	s+="GR"+gimbalList.Count.ToString()+ ":";
	return s; 
}

void gimbalLoad(IMyTerminalBlock r, GimbalRotor gr)
{
    Func<string, bool> asBool = (txt) => 
	{
        txt = txt.Trim().ToLower();
        return (txt == "True" || txt == "true");
    };

	gr.r = r as IMyMotorStator;
	gr.subRotor = null;
	gr.zminus=-1;
	gr.zplus=-1;
	gr.yminus=-1;
	gr.yplus=-1;
	gr.xminus=-1;
	gr.xplus=-1;

	gr.bAutoDampen = true;
	gr.bGravity = false;

	gr.gzminus=-1;
	gr.gzplus=-1;
	gr.gyminus=-1;
	gr.gyplus=-1;
	gr.gxminus=-1;
	gr.gxplus=-1;

	gr.gdefault=-1;
		float maxVelocity = r.GetMaximum < float > ("Velocity");
	gr.maxVelocity = maxVelocity;

	gr.targetAngle = -1;

Echo("Loading Gimbal:" + r.CustomName);

	Echo("rotor maxV=" + maxVelocity);
	string sData = r.CustomData;
//Echo("data=" + sData);
	string[] lines = sData.Trim().Split('\n');
//	Echo(lines.Length + " Lines");
	for(int i=0;i<lines.Length;i++)
	{
		string[] keys = lines[i].Trim().Split('=');
		if(lines[i].Contains("GimbalZMinus"))
		{
			if(keys.Length>1)
				gr.zminus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalZPlus"))
		{
			if(keys.Length>1)
				gr.zplus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalYPlus"))
		{
			if(keys.Length>1)
				gr.yplus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalYMinus"))
		{
			if(keys.Length>1)
				gr.yminus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalXPlus"))
		{
			if(keys.Length>1)
				gr.xplus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalXMinus"))
		{
			if(keys.Length>1)
				gr.xminus=(float)Convert.ToDouble(keys[1]);
		}
		// gravity
		if(lines[i].Contains("GimbalGZMinus"))
		{
			if(keys.Length>1)
				gr.gzminus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalGZPlus"))
		{
			if(keys.Length>1)
				gr.gzplus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalGYPlus"))
		{
			if(keys.Length>1)
				gr.gyplus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalGYMinus"))
		{
			if(keys.Length>1)
				gr.gyminus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalGXPlus"))
		{
			if(keys.Length>1)
				gr.gxplus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalGXMinus"))
		{
			if(keys.Length>1)
				gr.gxminus=(float)Convert.ToDouble(keys[1]);
		}
		if(lines[i].Contains("GimbalGDefault"))
		{
			if(keys.Length>1)
				gr.gdefault=(float)Convert.ToDouble(keys[1]);
		}

		if(lines[i].Contains("AutoDampen"))
		{
			if(keys.Length>1)
				gr.bAutoDampen=asBool(keys[1]);
		}
		if(lines[i].Contains("GimbalAutoDampen"))
		{
			if(keys.Length>1)
				gr.bAutoDampen=asBool(keys[1]);
		}
		if(lines[i].Contains("Gravity"))
		{
			if(keys.Length>1)
				gr.bGravity=asBool(keys[1]);
		}
		if(lines[i].Contains("GimbalGravity"))
		{
			if(keys.Length>1)
				gr.bGravity=asBool(keys[1]);
		}

		if(lines[i].Contains("GimbalMaxVelocity"))
		{
//			Echo("setting maxV to:" + keys[1]);
			if(keys.Length>1)
				gr.maxVelocity=(float)Convert.ToDouble(keys[1]);
			if (gr.maxVelocity < 0 || gr.maxVelocity > maxVelocity)
			{
//				Echo("Reset maxV to" + maxVelocity);
				gr.maxVelocity = maxVelocity;
			}
		}

		
		//		Echo(lines[i]);

	}
	if(gr.zminus>=0) Echo("zminus="+gr.zminus.ToString());
	if(gr.zplus>=0) Echo("zplus="+gr.zplus.ToString());
	if(gr.yminus>=0) Echo("yminus="+gr.yminus.ToString());
	if(gr.yplus>=0) Echo("yplus="+gr.yplus.ToString());
	if(gr.xminus>=0) Echo("xminus="+gr.xminus.ToString());
	if(gr.xplus>=0) Echo("xplus="+gr.xplus.ToString());

	if(gr.gzminus>=0) Echo("gzminus="+gr.zminus.ToString());
	if(gr.gzplus>=0) Echo("gzplus="+gr.zplus.ToString());
	if(gr.gyminus>=0) Echo("gyminus="+gr.yminus.ToString());
	if(gr.gyplus>=0) Echo("gyplus="+gr.yplus.ToString());
	if(gr.gxminus>=0) Echo("gxminus="+gr.xminus.ToString());
	if(gr.gxplus>=0) Echo("gxplus="+gr.xplus.ToString());
	Echo("Max Velocity=" + gr.maxVelocity.ToString());

    List<IMyTerminalBlock> thrusters = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters, (x=>x.CubeGrid==gr.r.TopGrid));
	gr.thrusters = thrusters;
	Echo(gr.thrusters.Count + " thrusters");

	List<IMyTerminalBlock> rotorsList = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(rotorsList, (x=>x.CubeGrid==gr.r.TopGrid));
	Echo(rotorsList.Count + " rotors on sub-grid");
	for(int i=0;i<rotorsList.Count;i++)
	{
		if(rotorsList[i].CustomName.Contains(sID ) || rotorsList[i].CustomData.Contains(sID ) )
		{
			GimbalRotor gr2 = new GimbalRotor();
			Echo("Subrotor:"+rotorsList[i].CustomName);
			gimbalLoad(rotorsList[i], gr2);
			gr.subRotor = gr2;
			break; // only one allowed.
		}
	}

/*
 * ZMinus is forward
 * ZPlus is backward
 * YPlus is up
 * YMinus is down
 * XPlus is left
 * XMinus is right
 * 
 * AutoMCD Rhino Freighter
 * 
 * // Right
	GimbalZMinus=180
	GimbalZPlus=0
	GimbalYPlus=90
	GimbalYMinus=270

	// Left
	GimbalZMinus=180
	GimbalZPlus=0
	GimbalYPlus=270
	GimbalYMinus=90

	// sub left
	GimbalZMinus=60
	GimbalZPlus=60
	GimbalYPlus=60
	GimbalYMinus=60
	GimbalXPlus=0

	// sub right
	GimbalZMinus=60
	GimbalZPlus=60
	GimbalYPlus=60
	GimbalYMinus=60
	GimbalXMinus=0

*/
}

void processGimbal(GimbalRotor gr, Vector3 vInputs,bool bDampeners)
{
	if (gr == null) return;
    string s="";
    s =  gr.r.CustomName;

	float x = vInputs.X;
	float y = vInputs.Y;
	float z = vInputs.Z;
	if(bDampeners && !gr.bAutoDampen)
	{
        s += ": No AutoDampen:";
		x = y = z = 0;
	}
	float targetAngle = -1;
	if (dGravity > 0)
	{
		if (gr.gdefault >= 0) targetAngle = gr.gdefault;
	}
	if(gr.bGravity && dGravity>0)
	{ 
		if(z<0 && gr.gzminus>=0)	targetAngle = gr.gzminus;
		if (z > 0 && gr.gzplus>=0) targetAngle = gr.gzplus;
		if (y < 0 && gr.gyminus>=0) targetAngle = gr.gyminus;
		if (y > 0 && gr.gyplus>=0) targetAngle = gr.gyplus;
		if (x < 0 && gr.gxminus>=0) targetAngle = gr.gxminus;
		if (x > 0 && gr.gxplus>=0) targetAngle = gr.gxplus;
	}
	else
	{ 
		if(z<0 && gr.zminus>=0)	targetAngle = gr.zminus;
		if (z > 0 && gr.zplus>=0) targetAngle = gr.zplus;
		if (y < 0 && gr.yminus>=0) targetAngle = gr.yminus;
		if (y > 0 && gr.yplus>=0) targetAngle = gr.yplus;
		if (x < 0 && gr.xminus>=0) targetAngle = gr.xminus;
		if (x > 0 && gr.xplus>=0) targetAngle = gr.xplus;
	}

    //	Log(gr.r.CustomName);
    s += " A=" + (gr.r.Angle * 57.295779513f).ToString("0.00");
    if (gr.r.Angle < 0) s += "NEG!";
    s+= ":" + targetAngle;

    //    Log(gr.r.Angle+ ":"+targetAngle);
    Log(s);
	if (targetAngle >= 0)
	{
		if(Math.Abs(processRotorTargetAngle(gr, targetAngle))<5)
		{
		}
	}
	else
	{
		gr.r.TargetVelocityRPM = 0;
//		gr.r.TargetVelocity = 0;
//		gr.r.SafetyLock = false;
        gr.r.SetValueBool("RotorLock", true);
        gr.r.UpperLimitRad=gr.r.Angle;
        gr.r.LowerLimitRad=gr.r.Angle;

	}


}
void processGimbals(Vector3 vInputs,bool bDampeners=false)
{
	float z = vInputs.Z;
	float y = vInputs.Y;
	float x = vInputs.X;

	float ax = Math.Abs(x);
	float ay = Math.Abs(y);
	float az = Math.Abs(z);
	string output = "";

	// only process one direction at a time. Choose the direction that's the 'largest'..

	output+="pg:A " + ax.ToString("0.0") + " Y" + ay.ToString("0.00") + " Z" + az.ToString("0.00");
	if (az > ay)
	{
		if (ax < ay) // z>y>x
		{
			y = x = 0;
		}
		else if (az > ax)
		{ // z>y z>x
			y = x = 0;
		}
		else
		{ // x>z z>y x>y
			z = y = 0;
		}
	}
	else // y>=z
	{
		if (ay > ax)
		{ // y>z  y>x
			z = x = 0;
		}
		else // x>y>z
			y = z = 0;
	}
	output+="\npg:V " + x.ToString("0.0") + " Y" + y.ToString("0.00") + " Z" + z.ToString("0.00");
	vInputs.X = x;
	vInputs.Y = y;
	vInputs.Z = z;

	Echo(output);
	Log(output);

	for (int i = 0; i < gimbalList.Count; i++)
	{
		processGimbal(gimbalList[i], vInputs,bDampeners);
		if (gimbalList[i].subRotor != null)
			processGimbal(gimbalList[i].subRotor,vInputs,bDampeners);
	}
}

void stopGimbals()
{
	for (int i = 0; i < gimbalList.Count; i++)
	{
        //		gimbalList[i].r.SafetyLock = false;
        gimbalList[i].r.SetValueBool("RotorLock", true);
		gimbalList[i].r.TargetVelocityRPM = 0;
        gimbalList[i].r.UpperLimitRad=gimbalList[i].r.Angle;
        gimbalList[i].r.LowerLimitRad=gimbalList[i].r.Angle;

//		gimbalList[i].r.TargetVelocity = 0;
//		listSetValueFloat(gimbalList[i].thrusters, "Override", 0);
		if (gimbalList[i].subRotor != null)
		{
			gimbalList[i].subRotor.r.TargetVelocityRPM = 0;
//			gimbalList[i].subRotor.r.TargetVelocity = 0;

//			listSetValueFloat(gimbalList[i].subRotor.thrusters, "Override", 0);
		}
	}
}

float deltaAngleD(float angle1, float angle2)
{
	float delta = angle1 - angle2;
	if (delta < -180) delta += 360;
	if (delta > 180) delta -= 360;

Echo("DeltaAngle(" + angle1.ToString("0") + "," + angle2.ToString("0") + ")=" + delta.ToString("0"));
	return delta;
}
float processRotorTargetAngle(GimbalRotor gr, float targetAngleD)
//float processRotorTargetAngle(IMyMotorStator r, float targetAngleD)
{

	IMyMotorStator r = gr.r;
	float angleR = r.Angle;

	float angleD = angleR * 57.295779513f;

	float newVelocity = 0;
	float angleDelta = deltaAngleD(targetAngleD, angleD);
	//Echo(r.CustomName + " angle=" + angleD.ToString() + " T:" + targetAngleD);
	if (Math.Abs(angleDelta) <0.5)
	{
//		r.SafetyLock = true;
        gr.r.SetValueBool("RotorLock", true);
        newVelocity = 0; // .25f * Math.Sign(angleDelta);
        gr.r.UpperLimitRad=angleR;
        gr.r.LowerLimitRad=angleR;
	}
	else
	{
        //		r.SafetyLock = false;
        //        gr.r.UpperLimitDeg=361; // doesn't work correctly on 1.185.200
        //        gr.r.LowerLimitDeg=-361;
         gr.r.SetValueFloat("UpperLimit", float.MaxValue);
        gr.r.SetValueFloat("LowerLimit", float.MinValue);
        /*
       if (angleDelta < 0)
        {
            gr.r.UpperLimitRad = gr.r.Angle;
//            gr.r.LowerLimitDeg=targetAngleD;
        }
        else
        {
//            gr.r.UpperLimitDeg =targetAngleD;
            gr.r.LowerLimitRad= gr.r.Angle;
        }
        */
        gr.r.SetValueBool("RotorLock", false);
		if (Math.Abs(angleDelta) > 0.5)
		{
			newVelocity = .25f * Math.Sign(angleDelta);
		}
		if (Math.Abs(angleDelta) > 5)
		{
			newVelocity = Math.Max(.25f,gr.maxVelocity/12) * Math.Sign(angleDelta);
		}
		if (Math.Abs(angleDelta) > 25)
		{
			newVelocity = Math.Max(.25f,gr.maxVelocity*0.75f) * Math.Sign(angleDelta);
		}
		if (Math.Abs(angleDelta) > 55)
		{
			newVelocity = gr.maxVelocity * Math.Sign(angleDelta);
		}
	}

	r.TargetVelocityRPM = newVelocity;
//	r.TargetVelocity = newVelocity;
	return newVelocity;
}
#endregion


string[] aTicks = { "-", "\\", "|", "/", "-", "\\", "|", "/" };

int iTick = 99;
string tick()
{
	iTick++;
	if (iTick >= aTicks.Length)
		iTick = 0;
	return aTicks[iTick];

}

