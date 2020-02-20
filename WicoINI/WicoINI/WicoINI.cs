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
        /// <summary>
        /// INI Processor.  Takes a string parameter from text panel, Customdata or wherever
        /// </summary>
        public class INIHolder
        {

            /// <summary>
            /// section names start with this character
            /// </summary>
            char _sectionStart = '[';
            /// <summary>
            /// section names end with this character
            /// </summary>
            char _sectionEnd = ']';
            /// <summary>
            /// comment lines start with this char
            /// </summary>
            string _CommentStart = ";";

            /// <summary>
            /// the text BEFORE any sections.  Will be saved as-is and regenerated
            /// </summary>
            string BeginningContent = "";

            /// <summary>
            /// flag to support content before any sections <see cref="BeginningContent"/>
            /// </summary>
            public bool bSupportBeginning = false;

            /// <summary>
            /// The text after the end of parsing designator
            /// </summary>
            public string EndContent = "";

            /// <summary>
            ///  line containing just this designates end of INI parsing.  All other text goes into <see cref="EndContent"/>
            /// </summary>
            string EndDesignator = "---";

            char MultLineStart = '|';

            private MyGridProgram _pg;

            private Dictionary<string, string> _Sections;
            private Dictionary<string, string[]> _Lines;
            private Dictionary<string, Dictionary<string, string>> _Keys;

            private string _sLastINI = "";

            // From Malware:
            static readonly string[] TrueValues = { "true", "yes", "on", "1" };
            const StringComparison Cmp = StringComparison.OrdinalIgnoreCase;
            const char SeparatorChar = '=';


            /// <summary>
            /// Have the sections been modified?  If so, they should be written back out/saved
            /// </summary>
            public bool IsDirty { get; private set; } = false;


            /// <summary>
            /// Constructor,  Pass MyGridProgram so it can access things like Echo()
            /// pass String to parse.
            /// </summary>
            /// <param name="pg">Allow access to things like Echo()</param>
            /// <param name="sINI">String to parse</param>
            public INIHolder(MyGridProgram pg, string sINI)
            {
                _pg = pg;
                _Sections = new Dictionary<string, string>();
                _Lines = new Dictionary<string, string[]>();
                _Keys = new Dictionary<string, Dictionary<string, string>>();

                ParseINI(sINI);
            }

            /// <summary>
            /// Re-parse string after construction
            /// </summary>
            /// <param name="sINI">String to parse</param>
            /// <returns>number of sections found</returns>
            public int ParseINI(string sINI)
            {
                // optimize if it is the same as last time..
                sINI.TrimEnd();

                if (_sLastINI == sINI)
                {
//                                        _pg.Echo("INI:Same"); // DEBUG
                    return _Sections.Count;
                }
//                else _pg.Echo("INI: NOT SAME"); // DEBUG


                _Sections.Clear();
                _Lines.Clear();
                _Keys.Clear();
                BeginningContent = "";
                EndContent = "";
                IsDirty = false;
                _sLastINI = sINI;

                // get an array of the all of lines
                string[] aLines = sINI.Split('\n');

//                               _pg.Echo("INI: " + aLines.Count() + " Lines to process"); // DEBUG

                // walk through all of the lines
                for (int iLine = 0; iLine < aLines.Count(); iLine++)
                {
                    string sSection = "";
                    aLines[iLine].Trim();
                    if (aLines[iLine].StartsWith(_sectionStart.ToString()))
                    {
                        //                        _pg.Echo(iLine + ":" + aLines[iLine]); // DEBUG
                        string sName = "";
                        for (int iChar = 1; iChar < aLines[iLine].Length; iChar++)
                            if (aLines[iLine][iChar] == _sectionEnd)
                                break;
                            else
                                sName += aLines[iLine][iChar];
                        if (sName != "")
                        {
                            sSection = sName.ToUpper();
                        }
                        else continue; // malformed line?

                        iLine++;
                        string sText = "";
                        var asLines = new string[aLines.Count() - iLine]; // maximum size.
                        int iSectionLine = 0;
                        var dKeyValue = new Dictionary<string, string>();

                        for (; iLine < aLines.Count(); iLine++)
                        {
                            aLines[iLine].Trim();

                            //                       _pg.Echo(iLine+":"+aLines[iLine]); // DEBUG

                            if (
                                aLines[iLine].StartsWith(_sectionStart.ToString()) 
                                || aLines[iLine].StartsWith(EndDesignator)
                                )
                            {
                                iLine--;
                                break;
                            }

                            // TODO: Support Mult-line strings?
                            // TODO: Support comments

                            sText += aLines[iLine] + "\n";
                            asLines[iSectionLine++] = aLines[iLine];

                            if (aLines[iLine].Contains(SeparatorChar))
                            {
                                string[] aKeyValue = aLines[iLine].Split('=');
                                if (aKeyValue.Count() > 1)
                                {
                                    string key = aKeyValue[0];
                                    string value = "";
                                    for (int i1 = 1; i1 < aKeyValue.Count(); i1++)
                                    {
                                        value += aKeyValue[i1];
                                        if (i1 + 1 < aKeyValue.Count()) value += SeparatorChar; // untested: add back together values with multiple seperatorChar
                                    }
                                    if(value=="") // blank line
                                    {
                                        // support malware style multi-line (needs testing)
                                        /*
                                        *
                                        ;The following line is a special format which allows for multiline text in a single key:
                                        MultiLine=
                                        |The first line of the value
                                        |The second line of the value
                                        |And so on
                                        */
                                        int iMulti = iLine + 1;
                                        for(;iMulti<aKeyValue.Count(); iMulti++)
                                        {
                                            aLines[iMulti].Trim();
                                            
                                            if(aLines[iMulti].Length>1 && aLines[iMulti][0]== MultLineStart)
                                            {
                                                value += aLines[iMulti].Substring(1).Trim()+"\n";
                                                break;
                                            }
                                        }
                                        iLine = iMulti;
                                    }
                                    dKeyValue.Add(key, value);
                                }
                            }
                            else if(aLines[iLine].StartsWith(_CommentStart))
                            {
                                // comment line... ignore for now
                            }
                        }
                        if (!_Keys.ContainsKey(sSection))
                        {
                            _Keys.Add(sSection, dKeyValue);
                            if (!_Lines.ContainsKey(sSection)) _Lines.Add(sSection, asLines);
                        }
                        else
                        {
                            // duplicate section..  Add each keyvalue to existing

                        }
                        if (!_Sections.ContainsKey(sSection))
                        {
                            _Sections.Add(sSection, sText);
                        }
                        else
                        {
                            IsDirty = true; // we have combined two sections from the source.
                        }
                    }
                    else if(aLines[iLine].StartsWith(EndDesignator))
                    {
                        iLine++;
                        
                        // end of INI section.  Save rest of text into EndContent
                        for(; iLine<aLines.Count();iLine++)
                        {
                            EndContent += aLines[iLine];
                        }
                    }
                    else
                    {
                        // we are before any sections. Save the text as-is
                        BeginningContent += aLines[iLine] + "\n";
                    }
                }
                return _Sections.Count;
            }

            /// <summary>
            /// Get the raw text of a specified section
            /// </summary>
            /// <param name="section">The name of the section</param>
            /// <returns>The text of the section</returns>
            public string GetSection(string section)
            {
                string sText = "";
                if (_Sections.ContainsKey(section))
                    sText = _Sections[section];
                return sText;
            }

            /// <summary>
            /// Get the parsed lines of a specified section
            /// </summary>
            /// <param name="section">The name of the section</param>
            /// <returns>string array of the lines in the section</returns>
            public string[] GetLines(string section)
            {
                string[] as1 = { "" };
                if (_Lines.ContainsKey(section))
                    as1 = _Lines[section];
                //                _pg.Echo("GetLines(" + section + ") : " + as1.Count() + " Lines");
                return as1;
            }

            /// <summary>
            /// Gets the string value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="sValue">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref string sValue, bool bSetDefault = false)
            {
                //                sValue = null;
                //                _pg.Echo(".GetValue(" + section+", " + key + ")");
                section=section.ToUpper();
                if (_Keys.ContainsKey(section)) // case sensitive
                {
                    var dValue = _Keys[section];
                    if (dValue.ContainsKey(key)) // case sensitive
                    {
                        sValue = dValue[key];
                        //                        _pg.Echo(" value=" + sValue);
                        return true;
                    }
                }
                if (bSetDefault)
                    SetValue(section, key, sValue);

                return false;
            }

            /// <summary>
            /// gets the long value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="lValue">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref long lValue, bool bSetDefault = false)
            {
                string sVal = "";

                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, lValue);
                    }
                    return false;
                }

                lValue = Convert.ToInt64(sVal);
                return true;
            }

            /// <summary>
            /// gets the long value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="iValue">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref int iValue, bool bSetDefault = false)
            {
                string sVal = "";

                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, iValue);
                    }
                    return false;
                }

                iValue = Convert.ToInt32(sVal);
                return true;
            }

            /// <summary>
            /// gets the double value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="dVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref double dVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, dVal);
                    }
                    return false;
                }

                bool pOK = double.TryParse(sVal, out dVal);
                return true;
            }

            /// <summary>
            /// gets the float value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="fVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref float fVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, fVal.ToString());
                    }
                    return false;
                }

                bool pOK = float.TryParse(sVal, out fVal);
                return true;
            }

            /// <summary>
            /// gets the DateTime value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="dtVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref DateTime dtVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, dtVal);
                    }
                    return false;
                }


                dtVal = DateTime.Parse(sVal);
                return true;
            }

            /// <summary>
            /// gets the Vector3D value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="vVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref Vector3D vVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, vVal);
                    }
                    return false;
                }

                double x1, y1, z1;
                ParseVector3d(sVal, out x1, out y1, out z1);
                vVal.X = x1;
                vVal.Y = y1;
                vVal.Z = z1;
                return true;
            }

            /// <summary>
            /// gets the Bool value of the key in the sectin
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="bVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref bool bVal, bool bSetDefault=false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, bVal);
                    }
                    return false;
                }

                bVal = TrueValues.Any(c => string.Equals(sVal, c, Cmp)); // From Malware
                return true;
            }

            public bool SetValue(string section, string key, string sVal)
            {
//                _pg.Echo("SetValue(" + section + "," + key + "," + sVal+")");

                // we are no longer caching direct text
                if (_Sections.ContainsKey(section))
                {
//                   _pg.Echo("ContainsKey(" + section + ")");
                    _Sections[section] = "";
                }
                else
                {
//                    _pg.Echo("addsection(" + section + ")");
                    _Sections.Add(section, "");// no cached text for now.
                    IsDirty = true; 
                }
                // if there is a set of keys for the section
                if (_Keys.ContainsKey(section))
                {
//                                        _pg.Echo("keysContain");
                    var dKeyValue = new Dictionary<string, string>();

                    var dValue = _Keys[section];
                    if (dValue.ContainsKey(key))
                    {
//                        _pg.Echo("valueContains");
                        if (dValue[key] == sVal) return false;

                        dValue[key] = sVal;
                    }
                    else
                    {
//                        _pg.Echo("addkey");
                        dValue.Add(key, sVal);
                    }
                    IsDirty = true;
                }
                else
                { // no keys for the section
//               _pg.Echo("keysNoContain");
                  // add the key value dictionary and the new section
                    var dKeyValue = new Dictionary<string, string>();
                    dKeyValue.Add(key, sVal);

//                    _pg.Echo("keyvalueadd");
                    _Keys.Add(section, dKeyValue);


                    IsDirty = true;
                }
//                _pg.Echo("SetValue:X");
                return true;
            }

            public bool SetValue(string section, string key, Vector3D vVal)
            {
                SetValue(section, key, Vector3DToString(vVal));
                return true;
            }
            public bool SetValue(string section, string key, bool bVal)
            {
                SetValue(section, key, bVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, int iVal)
            {
                SetValue(section, key, iVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, long lVal)
            {
                SetValue(section, key, lVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, DateTime dtVal)
            {
                SetValue(section, key, dtVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, float fVal)
            {
                SetValue(section, key, fVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, double dVal)
            {
                SetValue(section, key, dVal.ToString());
                return true;
            }


            /// <summary>
            /// Modify the section to have the specified text.. NOTE: This will overwrite any keys.  Use either full text and lines interfaces or keys interface
            /// </summary>
            /// <param name="section">the name of the section to modify</param>
            /// <param name="sText">the text to set as the new text</param>
            public void WriteSection(string section, string sText)
            {
                sText.TrimEnd();
                section = section.ToUpper();
                if (_Sections.ContainsKey(section))
                {
                    if (_Sections[section] != sText)
                    {
                        //                        _pg.Echo("INI WriteSection: Now Dirty:"+section);
                        _Sections[section] = sText;
                        IsDirty = true;
                    }
                }
                else
                {
                    //                    _pg.Echo("INI WriteSection: Adding new Section:" + section);
                    IsDirty = true;
                    _Sections.Add(section, sText);
                }
            }

            /// <summary>
            /// Generate the full text again. This includes any modifications that have been made
            /// </summary>
            /// <param name="bClearDirty">clear the dirty flag. Use if you are writing the text back to the original location</param>
            /// <returns>full text including ALL sections and header information</returns>
            public string GenerateINI(bool bClearDirty = true)
            {
                string sIni = "";
                string s1 = BeginningContent.Trim();
                if (bSupportBeginning && s1 != "") sIni = s1 + "\n";

//_pg.Echo("INI Generate: " + _Sections.Count() + "sections");
                foreach (var kv in _Sections)
                {
                    // TODO: if key values set, regenerate ini text from keys
//_pg.Echo("Section:" + kv.Key);
                    sIni += _sectionStart + kv.Key.Trim() + _sectionEnd + "\n";
                    if (kv.Value.TrimEnd() == "")
                    {
//_pg.Echo("Generate from keys");
                        string sSectionText = "";
                        // if raw text is cleared, regenerate from keys
                        if (_Keys.ContainsKey(kv.Key))
                        {
                            foreach (var dk in _Keys[kv.Key])
                            {
//_pg.Echo(" Key:" + dk.Key);
                                sSectionText += dk.Key + SeparatorChar + dk.Value + "\n";
                            }
                        }
                        sSectionText += "\n"; // add empty line at end
                        sIni += sSectionText;
//_pg.Echo("Set Cached Vavlue");
//                        _Sections[kv.Key] = sSectionText; // set cached value -- CANNOT because we are in enumeration loop
//_pg.Echo("Set");
                    }
                    else
                    {
                        sIni += kv.Value.Trim() + "\n\n"; // close last line + add empty line at end
                    }
                }
                if(EndContent!="")
                {
                    sIni += "\n" + EndDesignator + "\n";
                    sIni += EndContent + "\n";
                }
                if (bClearDirty)
                {
                    IsDirty = false;
                    _sLastINI = sIni;
                }
                return sIni;
            }

            bool ParseVector3d(string sVector, out double x, out double y, out double z)
            {
                string[] coordinates = sVector.Trim().Split(',');
                if (coordinates.Length < 3)
                {
                    coordinates = sVector.Trim().Split(':');
                }
                x = 0;
                y = 0;
                z = 0;
                if (coordinates.Length < 3) return false;

                bool xOk = double.TryParse(coordinates[0].Trim(), out x);
                bool yOk = double.TryParse(coordinates[1].Trim(), out y);
                bool zOk = double.TryParse(coordinates[2].Trim(), out z);
                if (!xOk || !yOk || !zOk)
                {
                    return false;
                }
                return true;
            }
            string Vector3DToString(Vector3D v)
            {
                string s;
                s = v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00");
                return s;
            }

        }


    }
}
