 
/* 
/// Whip's Turret Slaver v47 /// - revision: 8/10/17  
-------------------------------------------------------------------------------- 
================README================== 
-------------------------------------------------------------------------------- 
It is recommended that you read all the instructions before attempting  
to use this code! This will make troubleshooting any issues much easier <3 
 
---------------------------------------------------------------- 
/// Script Setup /// 
---------------------------------------------------------------- 
    1) Place this script in a program block 
    2) Make a timer with the following actions: 
        - "Start" itself 
        - "Trigger Now itself 
        - "Run with default argument" this program 
    3) Start the timer 
    4) Set up turret groups (see below sections) 
 
    DON'T FORGET TO SET YOUR ROTOR LIMITS! 
 
    (Optional): You can adjust the variables at the top of the code 
    if you dislike my default settings. I've found these values to 
    be sufficient for vanilla weapons :) 
 
---------------------------------------------------------------- 
/// Turret Group Names /// 
---------------------------------------------------------------- 
    Turret groups must be named like the following: 
 
        "Turret Group <ID>" 
 
    Where <ID> is the unique identification tag of the turret. 
 
    Example Turret Group Names: 
        - Turret Group 1 
        - Turret Group WhiplashIsAwesome 
        - Turret Group SamIsACow 
        - Turret Group 1A 
 
---------------------------------------------------------------- 
/// Turret Group Components /// 
---------------------------------------------------------------- 
    EACH turret group must have: 
    - One designator turret with "Designator" in its name 
    - One azimuthal (horizontal) rotor with "Azimuth" in its name 
    - One or Two elevation (vertical) rotor(s) with "Elevation" in its name 
    - At least one weapon ot tool (any name you desire) 
        can be a rocket launcher, gatling gun, camera, welder, grinder, or spotlight  
 
    (Names dont matter beyond what is required) 
 
---------------------------------------------------------------- 
/// Code Arguments (Optional) /// 
---------------------------------------------------------------- 
    Run the program ONCE with the following arguments if you desire 
     
    reset_targeting : Resets targeting of all non-designator turrets 
 
 
---------------------------------------------------------------- 
/// Whip's Notes /// 
---------------------------------------------------------------- 
Post any questions, suggestions, or issues you have on the workshop page :D 
 
Code by Whiplash141 
*/ 
 
//============================================================= 
//You can change these variables if you really want to. 
//You do not need to if you just want to use the vanilla script. 
//============================================================= 
 
//Base name tag of turret groups 
const string rotorTurretGroupNameTag = "Turret Group"; 
const string aiTurretGroupNameTag = "Slaved Group"; 
 
//These are the required block name tags in a turret group 
const string elevationRotorName = "Elevation"; //name of elevation (vertical) rotor for specific turret 
const string azimuthRotorName = "Azimuth"; //name of azimuth (horizontal) rotor for specific turret 
const string designatorName = "Designator"; //name of the designator turret for specific group 
 
//Angle that the turret will fire on if target is within this angle from the front of it 
const double toleranceAngle = 5; 
 
//Controls the speed of rotation; you probably shouldn't touch this 
const double rotationSpeedScalingFactor = 50; 
 
//Controls if the turrets will "sweep" the targeting range or aim at a static range 
//if true the turret will only aim at one point 
bool useStaticConverganceRange = false; 
 
//this is the distance that the turret will focus on IFF the above is set to TRUE 
double staticConverganceRange = 400; 
 
//Dynamic Convergance Parameters 
const double minRange = 400; //minimum engagement range to sweep guns 
const double maxRange = 800; //maximum engagement range to sweep guns 
const double spreadFrequency = .7; //frequency of one sweep 
 
 
//////////////////////////////////////////////////// 
//================================================= 
//No touchey anything below here 
//================================================= 
//////////////////////////////////////////////////// 
 
double timeElapsed = 0; 
double timeSinceLastRun = 0; 
 
bool shouldControl; 
 
IMyMotorStator elevationRotor; 
List<IMyMotorStator> additionalElevationRotors = new List<IMyMotorStator>(); 
IMyMotorStator azimuthRotor; 
IMyLargeTurretBase designator; 
Vector3D targetPointVec; 
List<IMyTerminalBlock> allWeaponsAndTools = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> primaryWeaponsAndTools = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> additionalWeaponsAndTools = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> slavedTurrets = new List<IMyTerminalBlock>(); 
 
const double rad2deg = 180 / Math.PI; 
 
const double updatesPerSecond = 10; 
const double timeMax = 1 / updatesPerSecond; 
double toleranceDotProduct = Math.Cos(toleranceAngle * Math.PI / 180); 
 
double engagementRange = 0; 
double timeSpread = 0; 
 
string[] targetStatus = new string[] { "targeting", "idle" }; 
 
//bool resetTurretTargeting = false; 
//string argumentAcceptString = ""; 
 
void Main(string arg) 
{ 
    switch (arg.ToLower()) 
    { 
        case "reset_targeting": 
            ResetTurretTargeting(); 
            break; 
 
        default: 
            break; 
    } 
 
    timeSinceLastRun = Runtime.TimeSinceLastRun.TotalSeconds; 
 
    timeElapsed += timeSinceLastRun; 
    timeSpread += timeSinceLastRun; 
 
    engagementRange = WeaponSweep(); //run weapon sweeep method 
 
    if (timeElapsed >= timeMax) 
    { 
        Echo("WMI Turret Control\nSystems Online... " + RunningSymbol()); 
 
        List<IMyBlockGroup> groups = new List<IMyBlockGroup>(); 
        List<IMyBlockGroup> rotorTurretGroups = new List<IMyBlockGroup>(); 
        List<IMyBlockGroup> aiTurretGroups = new List<IMyBlockGroup>(); 
 
        GridTerminalSystem.GetBlockGroups(groups); 
 
        foreach (IMyBlockGroup thisGroup in groups) 
        { 
            if (thisGroup.Name.ToLower().Contains(aiTurretGroupNameTag.ToLower())) 
            { 
                aiTurretGroups.Add(thisGroup); 
            } 
            else if (thisGroup.Name.ToLower().Contains(rotorTurretGroupNameTag.ToLower())) 
            { 
                rotorTurretGroups.Add(thisGroup); 
            } 
        } 
 
        if (rotorTurretGroups.Count == 0 && aiTurretGroups.Count == 0) 
        { 
            Echo("\nRotor Turret List:"); 
            Echo("No rotor turret groups found"); 
            Echo("\nAI Turret Group List:"); 
            Echo("No AI turret groups found"); 
            return; 
        } 
 
        //Ai turret group handling 
        #region ai_turrets 
        Echo("\nAI Turret Group List:"); 
        if (aiTurretGroups.Count == 0) 
        { 
            Echo("No AI turret groups found"); 
        } 
 
        foreach (IMyBlockGroup thisGroup in aiTurretGroups) 
        { 
            Echo($"------------------------\nGroup: '{thisGroup.Name}'"); 
            var blockList = new List<IMyTerminalBlock>(); 
            thisGroup.GetBlocks(blockList); 
            bool isSetup = GrabBlocksAI(blockList); 
 
            if (!isSetup) 
            { 
                ShootWeapons(slavedTurrets, false); //force shooting off 
                continue; 
            } 
            //implied else 
 
            //Turn off idle rotation 
            /*if (designator.EnableIdleRotation) 
                designator.ResetTargetingToDefault(); ////Check if i still need this 
             
            designator.EnableIdleRotation = false;*/ 
 
            //get target from designator 
            shouldControl = GetTargetPoint(thisGroup); 
 
            //guide on target 
            if (shouldControl) 
            { 
                SlavedTurretControl(); 
                Echo($"Turret is {targetStatus[0]}"); 
            } 
            else 
            { 
                ShootWeapons(slavedTurrets, false); //force shooting off 
                Echo($"Turret is {targetStatus[1]}"); 
            } 
        } 
        #endregion 
 
        //Rotor turret group handling 
        #region rotor_turrets 
        Echo("\nRotor Turret List:"); 
        if (rotorTurretGroups.Count == 0) 
        { 
            Echo("No rotor turret groups found"); 
        } 
 
        foreach (IMyBlockGroup thisGroup in rotorTurretGroups) 
        { 
            Echo($"------------------------\nGroup: '{thisGroup.Name}'"); 
             
            bool setupError = GrabBlocks(thisGroup); 
             
            if (setupError) 
            { 
                StopRotorMovement(); 
                continue; 
            } 
            //implied else 
                 
            //Turn off idle rotation 
            /*if (designator.EnableIdleRotation) 
                designator.EnableIdleRotation = false;*/ 
 
            //get target from designator 
            shouldControl = GetTargetPoint(thisGroup); 
 
            //guide on target 
            if (shouldControl) 
            { 
                RotorControl(thisGroup); 
                Echo($"Turret is {targetStatus[0]}"); 
            } 
            else 
            { 
                StopRotorMovement(); 
                ShootWeapons(allWeaponsAndTools, false); 
                ReturnToEquillibrium(); 
                Echo($"Turret is {targetStatus[1]}"); 
            } 
        } 
        #endregion 
 
        //reset time count 
        timeElapsed = 0; 
        rotorTurretGroups.Clear(); 
    } 
} 
 
void ResetTurretTargeting() 
{ 
    var allTurrets = new List<IMyLargeTurretBase>(); 
    GridTerminalSystem.GetBlocksOfType(allTurrets); 
    foreach (IMyLargeTurretBase thisTurret in allTurrets) 
    { 
        thisTurret.ResetTargetingToDefault(); 
        thisTurret.ApplyAction("Shoot_Off"); 
        thisTurret.EnableIdleRotation = true; 
        thisTurret.SetValue("Range", float.MaxValue); 
    } 
} 
 
double WeaponSweep() 
{ 
    if (useStaticConverganceRange) 
        return staticConverganceRange; 
     
    if (timeSpread < spreadFrequency) 
    { 
        return (maxRange - minRange) * timeSpread / spreadFrequency + minRange; 
    } 
    else if (timeSpread < 2 * spreadFrequency) 
    { 
        return maxRange - (maxRange - minRange) * (timeSpread - spreadFrequency) / spreadFrequency; 
    } 
    else 
    { 
        timeSpread = 0; 
        return minRange; 
    } 
} 
 
bool GetTargetPoint(IMyBlockGroup thisGroup) 
{ 
    //get designator position 
    Vector3D designatorPos = designator.GetPosition(); 
 
    //get vector where designator is pointing 
    double designatorAzimuth = designator.Azimuth; 
    double designatorElevation = designator.Elevation; 
     
    bool shouldTrack = false; 
    if (designator.IsUnderControl) 
    { 
        shouldTrack = true; 
    } 
    else if (designator.HasTarget && designator.IsShooting) //if designator has line of sight 
    { 
        shouldTrack = true; 
    } 
 
    //convert azimuth and elevation to a useful vector 
    Vector3D targetVec = VectorAzimuthElevation(designator); 
 
    targetPointVec = designatorPos + targetVec * engagementRange; 
 
    return shouldTrack; 
} 
 
bool GrabBlocks(IMyBlockGroup thisGroup) 
{ 
    var blocks = new List<IMyTerminalBlock>(); 
    thisGroup.GetBlocks(blocks); 
 
    elevationRotor = null; 
    additionalElevationRotors.Clear(); 
    azimuthRotor = null; 
    designator = null; 
    allWeaponsAndTools.Clear(); 
    primaryWeaponsAndTools.Clear(); 
    additionalWeaponsAndTools.Clear(); 
 
    foreach (IMyTerminalBlock thisBlock in blocks) 
    { 
        if (IsWeaponOrTool(thisBlock)) 
            allWeaponsAndTools.Add(thisBlock); 
         
        if (thisBlock is IMyMotorStator) 
        { 
            if (thisBlock.CustomName.ToLower().Contains(elevationRotorName.ToLower())) 
            { 
                if (elevationRotor == null) //grabs parent elevation rotor first 
                { 
                    var thisRotor = thisBlock as IMyMotorStator; 
                     
                    if (thisRotor.IsAttached && thisRotor.IsFunctional) //checks if elevation rotor is attached 
                    { 
                        thisGroup.GetBlocks(primaryWeaponsAndTools, block => block.CubeGrid == thisRotor.TopGrid && IsWeaponOrTool(block)); 
                    } 
                    if (primaryWeaponsAndTools.Count != 0) 
                        elevationRotor = thisRotor; 
                    else 
                        additionalElevationRotors.Add(thisRotor); 
                } 
                else //then grabs any other elevation rotors it finds 
                    additionalElevationRotors.Add(thisBlock as IMyMotorStator); 
            } 
            else if (thisBlock.CustomName.ToLower().Contains(azimuthRotorName.ToLower())) 
            { 
                azimuthRotor = thisBlock as IMyMotorStator; 
            } 
        } 
        else if (thisBlock is IMyLargeTurretBase && thisBlock.CustomName.ToLower().Contains(designatorName.ToLower())) //grabs ship controller 
        { 
            designator = thisBlock as IMyLargeTurretBase; 
        } 
    } 
 
    if (elevationRotor != null && elevationRotor.IsAttached) //grabs weapons on elevation turret's rotor head grid 
    { 
        thisGroup.GetBlocks(primaryWeaponsAndTools, block => block.CubeGrid == elevationRotor.TopGrid && IsWeaponOrTool(block)); 
    } 
 
    bool noErrors = true; 
    if (designator == null && azimuthRotor != null) //first null check for designator 
    { 
        //grabs closest designator to the turret base 
        designator = GetClosestTargetingTurret(designatorName, azimuthRotor); 
    } 
 
    if (designator == null) //second null check (if STILL null) 
    { 
        Echo($"Error: No designator turret found for group '{thisGroup.Name}'"); 
        noErrors = false; 
    } 
 
    if (primaryWeaponsAndTools.Count == 0) 
    { 
        Echo("Error: No weapons or tools"); 
        noErrors = false; 
    } 
 
    if (azimuthRotor == null) 
    { 
        Echo("Error: No azimuth rotor"); 
        noErrors = false; 
    } 
 
    if (elevationRotor == null) 
    { 
        Echo("Error: No elevation rotor"); 
        noErrors = false; 
    } 
 
    if (additionalElevationRotors.Count == 0) 
    { 
        Echo("Optional: No opposite elevation rotors detected"); 
    } 
 
    return !noErrors; 
} 
 
bool IsWeaponOrTool(IMyTerminalBlock block) 
{ 
    if (block is IMyUserControllableGun && !(block is IMyLargeTurretBase)) 
    { 
        return true; 
    } 
    else if (block is IMyShipToolBase) 
    { 
        return true; 
    } 
    else if (block is IMyLightingBlock) 
    { 
        return true; 
    } 
    else if (block is IMyCameraBlock) 
    { 
        return true; 
    } 
    else 
    { 
        return false; 
    } 
} 
 
void RotorControl(IMyBlockGroup thisGroup) 
{ 
    //get orientation of reference 
    IMyTerminalBlock turretReference = primaryWeaponsAndTools[0]; 
     
    Vector3D turretFrontVec = turretReference.WorldMatrix.Forward; 
    Vector3D absUpVec = azimuthRotor.WorldMatrix.Up; 
    Vector3D turretSideVec = elevationRotor.WorldMatrix.Up; 
    Vector3D turretFrontCrossSide = turretFrontVec.Cross(turretSideVec); 
 
    //check elevation rotor orientation w.r.t. reference 
    Vector3D turretUpVec; 
    Vector3D turretLeftVec; 
    if (absUpVec.Dot(turretFrontCrossSide) >= 0) 
    { 
        turretUpVec = turretFrontCrossSide; 
        turretLeftVec = turretSideVec; 
    } 
    else 
    { 
        turretUpVec = -1 * turretFrontCrossSide; 
        turretLeftVec = -1 * turretSideVec; 
    } 
 
    //get vector to target point 
    Vector3D referenceToTargetVec = targetPointVec - turretReference.GetPosition(); 
     
    double azimuthAngle; 
    double elevationAngle; 
    GetVectorAzimuthElevation(referenceToTargetVec, turretFrontVec, turretLeftVec, turretUpVec, out azimuthAngle, out elevationAngle); 
     
    //CheckAzimuthAngle(ref azimuthAngle, azimuthRotor); 
 
    if (absUpVec.Dot(turretFrontCrossSide) >= 0) 
    { 
        elevationAngle *= -1; 
    } 
 
    double azimuthSpeed = rotationSpeedScalingFactor * azimuthAngle; //derivitave term is useless as rotors dampen by default 
    double elevationSpeed = rotationSpeedScalingFactor * elevationAngle; 
 
    //control rotors  
    azimuthRotor.SetValue("Velocity", -(float)azimuthSpeed); //negative because we want to cancel the positive angle via our movements 
    elevationRotor.SetValue("Velocity", -(float)elevationSpeed); 
 
    //calculate deviation angle 
    double deviationAngle = VectorAngleBetween(turretFrontVec,referenceToTargetVec); 
    WeaponControl(deviationAngle, designator, primaryWeaponsAndTools); 
     
    //Check opposite elevation rotor 
    if (additionalElevationRotors.Count != 0) 
    { 
        foreach(var additionalElevationRotor in additionalElevationRotors) //Determine how to move opposite elevation rotor (if any) 
        { 
            if (!additionalElevationRotor.IsAttached) //checks if opposite elevation rotor is attached 
            { 
                Echo($"Warning: No rotor head for additional elevation\nrotor named '{additionalElevationRotor.CustomName}'\nSkipping this rotor..."); 
                continue; 
            } 
             
            thisGroup.GetBlocks(additionalWeaponsAndTools, block => block.CubeGrid == additionalElevationRotor.TopGrid && IsWeaponOrTool(block)); 
             
            if (additionalWeaponsAndTools.Count == 0) 
            { 
                Echo($"Warning: No weapons or tools for additional elevation\nrotor named '{additionalElevationRotor.CustomName}'\nSkipping this rotor..."); 
                continue; 
            } 
 
            var oppositeFrontVec = additionalWeaponsAndTools[0].WorldMatrix.Forward; 
             
            float multiplier = Math.Sign(additionalElevationRotor.WorldMatrix.Up.Dot(elevationRotor.WorldMatrix.Up)); 
 
            var diff = (float)VectorAngleBetween(oppositeFrontVec, turretFrontVec) * Math.Sign(oppositeFrontVec.Dot(turretFrontCrossSide)) * 100; 
            additionalElevationRotor.SetValue("Velocity", (float)elevationSpeed + diff); 
             
            WeaponControl(deviationAngle, designator, additionalWeaponsAndTools); //use same deviation angle b/c im assuming that it will be close 
        } 
    } 
} 
 
bool GrabBlocksAI(List<IMyTerminalBlock> blocks) 
{ 
    designator = null; 
    slavedTurrets.Clear(); 
 
    foreach (IMyTerminalBlock thisBlock in blocks) 
    { 
        if (thisBlock is IMyLargeTurretBase) 
        { 
            if (thisBlock.CustomName.Contains(designatorName)) 
            { 
                designator = thisBlock as IMyLargeTurretBase; //grabs designator turret 
            } 
            else 
            { 
                var turret = thisBlock as IMyLargeTurretBase; 
                turret.SetValue("Range", 1f); 
 
                if (turret.EnableIdleRotation) 
                    turret.EnableIdleRotation = false; 
 
                slavedTurrets.Add(turret); 
            } 
        } 
    } 
     
    bool setupError = false; 
    if (slavedTurrets.Count == 0) 
    { 
        Echo($"Error: No slaved AI turrets found"); 
        setupError = true; 
    } 
 
    if (designator == null && slavedTurrets.Count > 0) //first null check 
    { 
        //grabs closest designator to the slaved turret group 
        designator = GetClosestTargetingTurret(designatorName, slavedTurrets[0]); 
    } 
 
    if (designator == null) //second null check (If STILL null) 
    { 
        Echo($"Error: No designator turret found"); 
        setupError = true; 
    } 
     
    return !setupError; 
} 
 
void SlavedTurretControl() 
{ 
    //control AI turrets (if any) 
    //aim all slaved turrets at target point 
    foreach (IMyLargeTurretBase thisTurret in slavedTurrets) 
    { 
        //This shit broke yo 
        //thisTurret.SetTarget(targetPointVec); 
        var turretMatrix = thisTurret.WorldMatrix; 
         
        var turretDirection = VectorAzimuthElevation(thisTurret); 
        var normalizedTargetDirection = Vector3D.Normalize(targetPointVec - turretMatrix.Translation); 
 
        double azimuth = 0; double elevation = 0; 
         
        GetVectorAzimuthElevation(normalizedTargetDirection, turretMatrix.Forward, turretMatrix.Left, turretMatrix.Up, out azimuth, out elevation); 
        thisTurret.Azimuth = (float)azimuth; 
        thisTurret.Elevation = (float)elevation; 
 
        SyncTurretAngles(thisTurret); 
 
        if (turretDirection.Dot(normalizedTargetDirection) >= toleranceDotProduct) 
        { 
            if (designator.IsShooting || (designator.HasTarget && !designator.IsUnderControl)) 
            { 
                thisTurret.ApplyAction("ShootOnce"); //Had to add this or the guns wont shoot... 
                thisTurret.ApplyAction("Shoot_On"); 
            } 
            else 
                thisTurret.ApplyAction("Shoot_Off"); 
        } 
        else 
        { 
            thisTurret.ApplyAction("Shoot_Off"); 
        } 
    } 
} 
 
void ReturnToEquillibrium() 
{ 
    if (azimuthRotor != null && azimuthRotor.LowerLimit > -MathHelper.TwoPi && azimuthRotor.UpperLimit < MathHelper.TwoPi) 
    { 
        double avgAzimuth = (azimuthRotor.LowerLimit + azimuthRotor.UpperLimit) / 2; 
        double azimuthVelocity = (avgAzimuth - azimuthRotor.Angle) * rotationSpeedScalingFactor / 10; 
        azimuthRotor.TargetVelocity = (float)azimuthVelocity; 
    } 
    else if (azimuthRotor != null) 
    { 
        azimuthRotor.TargetVelocity = 0f; 
    } 
 
    if (elevationRotor != null && elevationRotor.LowerLimit > -MathHelper.TwoPi && elevationRotor.UpperLimit < MathHelper.TwoPi) 
    { 
        double avgElevation = (elevationRotor.LowerLimit + elevationRotor.UpperLimit) / 2; 
        double elevationVelocity = (avgElevation - elevationRotor.Angle) * rotationSpeedScalingFactor / 10; 
        elevationRotor.TargetVelocity = (float)elevationVelocity; 
    } 
    else if (elevationRotor != null) 
    { 
        elevationRotor.TargetVelocity = 0f; 
    } 
     
    double avgOppositeElevation; 
    double additionalElevationVelocity; 
 
    foreach (var additionalElevationRotor in additionalElevationRotors) 
    { 
        if (additionalElevationRotor.LowerLimit > -MathHelper.TwoPi && additionalElevationRotor.UpperLimit < MathHelper.TwoPi) 
        { 
            avgOppositeElevation = (additionalElevationRotor.LowerLimit + additionalElevationRotor.UpperLimit) / 2; 
            additionalElevationVelocity = (avgOppositeElevation - additionalElevationRotor.Angle) * rotationSpeedScalingFactor / 10; 
            additionalElevationRotor.TargetVelocity = (float)additionalElevationVelocity; 
        } 
        else 
        { 
            additionalElevationRotor.TargetVelocity = 0f; 
        } 
    }    
} 
 
void SyncTurretAngles(IMyLargeTurretBase turret) 
{ 
    turret.SyncAzimuth(); 
    turret.SyncElevation(); 
    turret.SyncEnableIdleRotation(); 
} 
 
void WeaponControl(double deviation, IMyLargeTurretBase designator, List<IMyTerminalBlock> weaponsAndTools) 
{ 
    if (designator.IsUnderControl && designator.IsShooting) 
        ShootWeapons(weaponsAndTools, true); 
    else if (deviation * rad2deg < toleranceAngle && designator.HasTarget) //fires if in tolerance angle 
        ShootWeapons(weaponsAndTools, true); 
    else //doesnt fire if not in tolerance angle or designator isnt controlled 
        ShootWeapons(weaponsAndTools, false); 
} 
 
void ShootWeapons(List<IMyTerminalBlock> weaponList, bool shouldFire) 
{ 
    if (shouldFire) 
    { 
        for (int i = 0; i < weaponList.Count; i++) 
        { 
            var weaponToShoot = weaponList[i] as IMyUserControllableGun; 
 
            weaponToShoot?.ApplyAction("Shoot_On"); 
            weaponToShoot?.ApplyAction("ShootOnce"); 
        } 
    } 
    else 
    { 
        for (int i = 0; i < weaponList.Count; i++) 
        { 
            var weaponToShoot = weaponList[i] as IMyUserControllableGun; 
 
            weaponToShoot?.ApplyAction("Shoot_Off"); 
        } 
    } 
} 
 
void StopRotorMovement() 
{ 
    azimuthRotor?.SetValue("Velocity", 0f); 
    elevationRotor?.SetValue("Velocity", 0f); 
 
    foreach (var additionalElevationRotor in additionalElevationRotors) 
    { 
        additionalElevationRotor.TargetVelocity = 0f; 
    }  
 
    for (int i = 0; i < allWeaponsAndTools.Count; i++) 
    { 
        var thisWeapon = allWeaponsAndTools[0] as IMyUserControllableGun; 
        thisWeapon?.ApplyAction("Shoot_Off"); 
    } 
} 
 
Vector3D VectorProjection(Vector3D a, Vector3D b) 
{ 
    return a.Dot(b) / b.LengthSquared() * b; 
} 
 
double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians  
{ 
    if (a.LengthSquared() == 0 || b.LengthSquared() == 0) 
        return 0; 
    else 
        return Math.Acos(MathHelper.Clamp(a.Dot(b) / a.Length() / b.Length(), -1, 1)); 
} 
 
//Whip's Vector from Elevation and Azimuth 
Vector3D VectorAzimuthElevation(IMyLargeTurretBase designator) 
{ 
    double el = designator.Elevation; 
    double az = designator.Azimuth; 
 
    //CreateFromAzimuthAndElevation(az, el, out localTargetVector) 
 
    el = el % (2 * Math.PI); 
    az = az % (2 * Math.PI); 
 
    if (az != Math.Abs(az)) 
    { 
        az = 2 * Math.PI + az; 
    } 
 
    int x_mult = 1; 
 
    if (az > Math.PI / 2 && az < Math.PI) 
    { 
        az = Math.PI - (az % Math.PI); 
        x_mult = -1; 
    } 
    else if (az > Math.PI && az < Math.PI * 3 / 2) 
    { 
        az = 2 * Math.PI - (az % Math.PI); 
        x_mult = -1; 
    } 
 
    double x; double y; double z; 
 
    if (el == Math.PI / 2) 
    { 
        x = 0; 
        y = 0; 
        z = 1; 
    } 
    else if (az == Math.PI / 2) 
    { 
        x = 0; 
        y = 1; 
        z = y * Math.Tan(el); 
    } 
    else 
    { 
        x = 1 * x_mult; 
        y = Math.Tan(az); 
        double v_xy = Math.Sqrt(1 + y * y); 
        z = v_xy * Math.Tan(el); 
    } 
 
    var worldMatrix = designator.WorldMatrix; 
    return Vector3D.Normalize(worldMatrix.Forward * x + worldMatrix.Left * y + worldMatrix.Up * z); 
    //return new Vector3D(x, y, z); 
} 
 
//Whip's Get Azimuth and Elevation from angle 
void GetVectorAzimuthElevation(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double az, out double el) 
{ 
    //Dependencies: VectorProjection() | VectorAngleBetween() 
    var projTargetUp = VectorProjection(v_target, v_up); 
    var projTargetFrontLeft = v_target - projTargetUp; 
     
    az = VectorAngleBetween(v_front, projTargetFrontLeft); 
    el = VectorAngleBetween(v_target, projTargetFrontLeft); 
 
    //---Check if az angle is left or right   
    az = Math.Sign(v_left.Dot(v_target)) * az; 
 
    //---Check if el angle is up or down     
    el = Math.Sign(v_up.Dot(v_target)) * el; 
 
    //---Check if target vector is pointing opposite the front vector 
    if (el == 0 && az == 0 && v_target.Dot(v_front) < 0) 
    { 
        az = Math.PI; 
    } 
} 
 
//Whip's Get Closest Targeted Turret v1 - 6/12/17 
IMyLargeTurretBase GetClosestTargetingTurret(string name, IMyTerminalBlock reference = null) 
{ 
    var allBlocks = new List<IMyLargeTurretBase>(); 
 
    if (name == "") 
        GridTerminalSystem.GetBlocksOfType(allBlocks); 
    else 
        GridTerminalSystem.GetBlocksOfType(allBlocks, block => block.CustomName.Contains(name)); 
 
    if (allBlocks.Count == 0) 
    { 
        return null; 
    } 
     
    //Sort guns automatically by name 
    //allBlocks.Sort((block1, block2) => block1.CustomName.CompareTo(block2.CustomName)); 
 
    var closestBlock = allBlocks[0]; 
 
    if (reference == null) 
        reference = Me; 
 
    var shortestDistance = Vector3D.DistanceSquared(reference.GetPosition(), closestBlock.GetPosition()); 
    allBlocks.Remove(closestBlock); //remove this block from the list 
 
    foreach (var thisBlock in allBlocks) 
    { 
        var thisDistance = Vector3D.DistanceSquared(reference.GetPosition(), thisBlock.GetPosition()); 
 
        if (thisDistance + 0.01 < shortestDistance && (thisBlock.HasTarget || thisBlock.IsUnderControl)) 
        { 
            closestBlock = thisBlock; 
            shortestDistance = thisDistance; 
        } 
        //otherwise move to next one 
    } 
 
    return closestBlock; 
} 
 
//Whip's Running Symbol Method v6 
int runningSymbolVariant = 0; 
string RunningSymbol() 
{ 
    runningSymbolVariant++; 
    string strRunningSymbol = ""; 
 
    if (runningSymbolVariant == 0) 
        strRunningSymbol = "|"; 
    else if (runningSymbolVariant == 1) 
        strRunningSymbol = "/"; 
    else if (runningSymbolVariant == 2) 
        strRunningSymbol = "--"; 
    else if (runningSymbolVariant == 3) 
    { 
        strRunningSymbol = "\\"; 
        runningSymbolVariant = 0; 
    } 
 
    return strRunningSymbol; 
} 
 
/* 
Changelog: 
- Clamped values to account for floating point errors - v31 
- Fixed syntax error - v32 
- Added AI turret slaving support - v33 
- Redesigned targeting parameters - v33 
- Added rotor turret equillibrium function - v34 
- Cleaned, simplified, and removed some functions - v35 
- Redesigned turret sweeping function - v35 
- Reverted back to old turret sweeping function XD - v35 
- Added in support for AI turret groups - v35 
- Workaround for turret angle setting bug -DONE - v37-1 
- Grabs Weapons/Tools based on grid id of rotor head - v37-3 
- Works with 2 elevation rotors per turret group - v39 
- Tweaked get rotation angle method - v39 
- Fixed broke ass WorldMatricies. Thanks keen... - v40 
- Optimized position getting by adding GetWorldPosition() method - v41 
- Changed GetClosestBlock method to GetClosestTargetingTurret - v42 
- Fixed turrets spinning when idle when no rotor limits were set - v43 
- Adjusted range computation for GetClosestTargetingTurret to avoid musical turrets - v44 
- Removed useage of GetWorldMatrix and GetWorldPosition since the bug that necessitated their use is gone - v45 
- Removed lots of unused math and methods - v45 
- Added support for infinite numbers of elevation rotors - v46 
- Turrets will only fire automatically when designator has line of sight - v47 
- Decreased equillibrium turn speed for safety reasons - v47 
- Designators can now rotate idly if the user so desires - v47 
 
To-do list: 
- Finish argument accept display readout 
*/

