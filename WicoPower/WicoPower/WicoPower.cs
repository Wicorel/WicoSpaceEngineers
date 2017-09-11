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
        #region power

        void initPower()
        {
            totalMaxPowerOutput = 0;
            Echo("Init power");
            initReactors();
            initSolars();
            initBatteries();
            if (maxReactorPower > 0)
                totalMaxPowerOutput += maxReactorPower;
            //	if (maxSolarPower > 0)
            //		totalMaxPowerOutput += maxSolarPower;
            if (maxBatteryPower > 0)
                totalMaxPowerOutput += maxBatteryPower;
        }

        #endregion

#region powerproducer

public static class PowerProducer
{

    /// <summary>
    /// Getting power level from its position in DetailedInfo.
    /// The order of power levels changes from block to block, so each block type needs functions.
    /// </summary>
    #region Positional

    private const byte
    Enum_BatteryLine_MaxOutput = 1,
    Enum_BatteryLine_MaxRequiredInput = 2,
    Enum_BatteryLine_MaxStored = 3,
    Enum_BatteryLine_CurrentInput = 4,
    Enum_BatteryLine_CurrentOutput = 5,
    Enum_BatteryLine_CurrentStored = 6;


    private const byte
    Enum_ReactorLine_MaxOutput = 1,
    Enum_ReactorLine_CurrentOutput = 2;

    private const byte
    Enum_SolarPanelLine_MaxOutput = 1,
    Enum_SolarPanelLine_CurrentOutput = 2;

    private const byte
    Enum_GravityLine_MaxRequiredInput = 1,
    Enum_GravityLine_CurrentInput = 2;

    private static readonly char[] wordBreak = { ' ' };


    public static bool GetMaxOutput(IMyBatteryBlock battery, out float value)
    { return GetPowerFromInfo(battery, Enum_BatteryLine_MaxOutput, out value); }

    public static bool GetMaxRequiredInput(IMyBatteryBlock battery, out float value)
    { return GetPowerFromInfo(battery, Enum_BatteryLine_MaxRequiredInput, out value); }

    public static bool GetCurrentInput(IMyBatteryBlock battery, out float value)
    { return GetPowerFromInfo(battery, Enum_BatteryLine_CurrentInput, out value); }

    public static bool GetCurrentOutput(IMyBatteryBlock battery, out float value)
    { return GetPowerFromInfo(battery, Enum_BatteryLine_CurrentOutput, out value); }

    public static bool GetMaxStored(IMyBatteryBlock battery, out float value)
    { return GetPowerFromInfo(battery, Enum_BatteryLine_MaxStored, out value); }

    public static bool GetCurrentStored(IMyBatteryBlock battery, out float value)
    { return GetPowerFromInfo(battery, Enum_BatteryLine_CurrentStored, out value); }


    public static bool GetMaxOutput(IMyReactor reactor, out float value)
    { return GetPowerFromInfo(reactor, Enum_ReactorLine_MaxOutput, out value); }

    public static bool GetCurrentOutput(IMyReactor reactor, out float value)
    { return GetPowerFromInfo(reactor, Enum_ReactorLine_CurrentOutput, out value); }


    public static bool GetMaxOutput(IMySolarPanel panel, out float value)
    { return GetPowerFromInfo(panel, Enum_SolarPanelLine_MaxOutput, out value); }

    public static bool GetCurrentOutput(IMySolarPanel panel, out float value)
    { return GetPowerFromInfo(panel, Enum_SolarPanelLine_CurrentOutput, out value); }


    public static bool GetMaxRequiredInput(IMyGravityGeneratorBase gravity, out float value)
    { return GetPowerFromInfo(gravity, Enum_GravityLine_MaxRequiredInput, out value); }

    public static bool GetCurrentInput(IMyGravityGeneratorBase gravity, out float value)
    { return GetPowerFromInfo(gravity, Enum_GravityLine_CurrentInput, out value); }


    private static bool GetPowerFromInfo(IMyTerminalBlock block, byte lineNumber, out float value)
    {
        value = -1;
        float multiplier;

        string[] lines = block.DetailedInfo.Split('\n');
        string[] words = lines[lineNumber].Split(wordBreak, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2
        || !float.TryParse(words[words.Length - 2], out value)
        || !getMultiplier('W', words[words.Length - 1], out multiplier))
            return false;

        value *= multiplier;
        value /= 1000 * 1000f;

        return true;
    }

    #endregion

    /// <summary>
    /// Getting power level from DetailedInfo using regular expressions.
    /// No localization.
    /// </summary>
    #region Regular Expressions
    private static readonly System.Text.RegularExpressions.Regex CurrentInput = new System.Text.RegularExpressions.Regex(@"(\nCurrent Input:)\s+(-?\d+\.?\d*)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase); private static readonly System.Text.RegularExpressions.Regex CurrentOutput = new System.Text.RegularExpressions.Regex(@"(\nCurrent Output:)\s+(-?\d+\.?\d*)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase); private static readonly System.Text.RegularExpressions.Regex MaxPowerOutput = new System.Text.RegularExpressions.Regex(@"(\nMax Output:)\s+(-?\d+\.?\d*)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase); private static readonly System.Text.RegularExpressions.Regex MaxRequiredInput = new System.Text.RegularExpressions.Regex(@"(\nMax Required Input:)\s+(-?\d+\.?\d*)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase); private static readonly System.Text.RegularExpressions.Regex RequiredInput = new System.Text.RegularExpressions.Regex(@"(\nRequired Input:)\s+(-?\d+\.?\d*)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase); public static bool GetCurrentInput(IMyTerminalBlock block, out float value)
    { return GetPowerFromInfo(block, CurrentInput, out value); }
    public static bool GetCurrentOutput(IMyTerminalBlock block, out float value)
    { return GetPowerFromInfo(block, CurrentOutput, out value); }
    public static bool GetMaxPowerOutput(IMyTerminalBlock block, out float value)
    { return GetPowerFromInfo(block, MaxPowerOutput, out value); }
    public static bool GetMaxRequiredInput(IMyTerminalBlock block, out float value)
    { return GetPowerFromInfo(block, MaxRequiredInput, out value); }
    public static bool GetRequiredInput(IMyTerminalBlock block, out float value)
    { return GetPowerFromInfo(block, RequiredInput, out value); }
	private static bool GetPowerFromInfo(IMyTerminalBlock block, System.Text.RegularExpressions.Regex regex, out float value)
	{
		value = -1; float multiplier; System.Text.RegularExpressions.Match match = regex.Match(block.DetailedInfo); if (!match.Success || !float.TryParse(match.Groups[2].ToString(), out value) || !getMultiplier('W', match.Groups[3].ToString(), out multiplier))
			return false; value *= multiplier; return true;
	}
	#endregion
	public const string depleted_in = "Fully depleted in:"; public const string recharged_in = "Fully recharged in:"; private const float
    k = 1000f, M = k * k, G = k * M, T = k * G, m = 0.001f; public static bool IsRecharging(IMyBatteryBlock battery)
    { return battery.DetailedInfo.Contains(recharged_in); }
    public static bool IsDepleting(IMyBatteryBlock battery)
    { return battery.DetailedInfo.Contains(depleted_in); }
    private static bool getMultiplier(char unit, string expr, out float result)
    {
        result = 0; char firstChar = expr[0]; if (firstChar == unit)
        { result = 1; return true; }
        if (expr[1] != unit)
            return false; float k = 1000; if (firstChar == 'k')
            result = k;
        else if (firstChar == 'M')
            result = M;
        else if (firstChar == 'G')
            result = G;
        else if (firstChar == 'T')
            result = T;
        else if (firstChar == 'm')
            result = m; return result != 0;
    }
}

#endregion


    }
}