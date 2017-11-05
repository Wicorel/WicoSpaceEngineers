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
        #region domodes 
        void doModes()
        {
            Echo("mode=" + iMode.ToString());
            doModeAlways();
/*            
            if (AnyConnectorIsConnected() && !((craft_operation & CRAFT_MODE_ORBITAL) > 0))
            {
                Echo("DM:docked");
                setMode(MODE_DOCKED);
            }
*/

/*
            if (iMode == MODE_IDLE) doModeIdle();
            else if (iMode == MODE_DESCENT) doModeDescent();
*/
        }
        #endregion


        #region modeidle 
        void ResetToIdle()
        {
            StatusLog(DateTime.Now.ToString() + " ACTION: Reset To Idle", textLongStatus, true);
            ResetMotion();
            setMode(MODE_IDLE);
//            if (AnyConnectorIsConnected()) setMode(MODE_DOCKED);
        }
        void doModeIdle()
        {
              StatusLog(moduleName + " Manual Control", textPanelReport);
        }
        #endregion

        void doModeAlways()
        {
	        processPendingSends();
            if (processIncomingNavComMessage())
            {

            }
            else
            {
                // other message handlers go here.
            }



            NavComDataUpdate();
        }


        // return true if message processed, else false.
        bool processIncomingNavComMessage()
        {
            string[] aMessage = sReceivedMessage.Trim().Split(':');
            if (aMessage.Length > 1)
            {
                if (aMessage[0] == "NavCom")
                {
                    sReceivedMessage = "";
                    NavComDataReciver(sReceivedMessage);
                    return true;
                }
            }
            return false;
        }



        // NavCom  
        // Inter Grid Data Exchange  
        // by Yeto 
 
 
        // this could also be a .Save function, but I do not know how to do it 
        public static class Globals 
        { 
            public static int LastUpdate = 0;  // Modifiable in Code 
            public static String RackPrefix = ""; 
        } 
 
 
 
        // do I update, only if last update was more then 20 seconds ago 
        void NavComDataUpdate() 
        { 
            int seconds = Runtime.TimeSinceLastRun.Seconds; 
            int LastUpdate = Globals.LastUpdate + seconds; 
            if (LastUpdate > 20) 
            { 
                RackPrefix(); //get the unique Rack Code 
                UpdateEntityAndDocking(); // agregate Data 
                Globals.LastUpdate = 0; 
            } 
            else 
            { 
                Globals.LastUpdate = LastUpdate; 
            } 
        } 
        // aggregateing Data and send it 
        void UpdateEntityAndDocking() 
        { 
            List<IMyProjector> AllProjectors = new List<IMyProjector>(); 
            GridTerminalSystem.GetBlocksOfType(AllProjectors, x => x.CustomName.Contains("[master][repair]")); 
            if (AllProjectors.Count == 0) { Echo("No [master][repair] Projectors found"); return; } 
 
            string MyEntityInfo = ""; 
            string MyDockingInfo = ""; 
            string sendData = ""; 
            for (int h = 0; h < AllProjectors.Count; h++) 
            { 
                MyEntityInfo = GetDATA(AllProjectors[h].CustomName, "Ident"); 
                MyDockingInfo = GetDATA(AllProjectors[h].CustomName, "docking"); 
                sendData = sendData + "<entity" + h + ">\n<Ident>" + MyEntityInfo + "</Ident>\n" + "<docking>" + MyDockingInfo + "</docking>\n</entity" + h + ">"; 
            } 
            if (sendData != "") { antSend("NavCom:<IDs>" + AllProjectors.Count + "</IDs>" + sendData); } // TODO CHECK !!!!!!!!!!!!!!!!!!!!! TODO INTEGRATE 
        } 
 
 
 
        // receive Data 
        void NavComDataReciver(string ReceivedMessage) 
        { 
            // tell me what exists 
            string EntityDBentries = GetDATA("<DB_Entity>", "entries"); 
            string DockDBentries = GetDATA("<DB_Dock>", "entries"); 
 
            ReceivedMessage = ReceivedMessage.Replace("NavCom:", ""); // delete the indentifier 
            RackPrefix(); //get the unique Rack Code 
 
            // entities and docks 
            string IDstring = ExtractDATA("IDs", ReceivedMessage); 
            int IDnumber = Int16.Parse(IDstring); 
            for (int t = 0; t < IDnumber; t++) 
            { 
                string EntityData = ExtractDATA("entity" + t, ReceivedMessage); // get one entity of Data 
                string IdentData = ExtractDATA("Ident" + t, EntityData); // get IdentData 
                string ID = ExtractDATA("IdentCode", IdentData); // read identity 
                // Add Entity 
                if (!EntityDBentries.Contains(ID)) /* write new entry */ 
                { 
                    string IdentNew = "<new>\n<" + ID + ">\n" + IdentData + "</" + ID + ">\n"; // build surrounding tag 
                    ReplaceWriteDATA("<DB_Entity>", "<new>", IdentNew); // write entry 
 
                    string newEntityEntry = ID + "¤</entries>"; // prepare the entry locater  
                    ReplaceWriteDATA("<DB_Entity>", "</entries>", newEntityEntry); // entry locater 
                } 
                else /* update old entry */ 
                { 
                    WriteDATA("<DB_Entity>", ID, IdentData); // write  
                } 
 
                // docks 
                string DocksReceived = ExtractDATA("docking", EntityData); // get dockingdata of entity 
                string Docks = ExtractDATA("IdentDocks", EntityData); // how many docks 
                string[] dock = ExtractARRAY(' ', Docks); 
                for (int r = 0; r < dock.Length; r++) 
                { 
                    string OneDock = ExtractDATA(dock[r], EntityData); // get data for one dock 
                    if (!DockDBentries.Contains(dock[r])) /* write new entry */ 
                    { 
                        string DockNew = "<new>\n<" + dock[r] + ">\n" + OneDock + "</" + dock[r] + ">\n"; // build surrounding tag 
                        ReplaceWriteDATA("<DB_Dock>", "<new>", DockNew); // write entry 
 
                        string newDockEntry = dock[r] + "¤</entries>"; // prepare the entry locater (space is important) 
                        ReplaceWriteDATA("<DB_Dock>", "</entries>", newDockEntry); // entry locater 
                    } 
                    else /* update old entry */ 
                    { 
                        WriteDATA("<DB_Dock>", dock[r], OneDock); // write 
                    } 
 
                } 
            } 
        } 
 
 
 
        // TODO Yeto 
        // Vectoring for aproach Positions and landing trails is not done yet because vectors are broken  
        // when including it, the world position and world orientation of [master][rc] have to be re-added before sending 
 
 
 
        /* These are Helpers for NavCom Inter Grid Data Exchange*/ 
        /* These are Helpers for NavCom Inter Grid Data Exchange*/ 
        /* These are Helpers for NavCom Inter Grid Data Exchange*/ 
 
 
        // get the repair block on the rack to prevent writing to the wrong Rack 
        void RackPrefix()  
            { 
                var RackRepair = new List<IMyProjector>(); 
                GridTerminalSystem.GetBlocksOfType(RackRepair, block => block.CubeGrid == Me.CubeGrid); // only rack projector 
                GridTerminalSystem.GetBlocksOfType(RackRepair, x => x.CustomName.Contains("[master][repair]")); 
                Globals.RackPrefix = GetDATA(RackRepair[0].CustomName, "IdentCode");  
            } 
 
    // 
    // DB locate __________________________________________________________________________________ 
    // 
    //GET DB The Result is a text including line breaks and ASCII formatting  
    string DBlocate(string DB) 
        { 
            bool DBisSet = false; 
            string DBname = ""; 
            if (DB == "<DB_GPS>") { DBname = Globals.RackPrefix + "Rack LCD GPS"; DBisSet = true; } // if LCD2 DB 
            if (DB == "<DB_Entity>") { DBname = Globals.RackPrefix + "Rack LCD Entity"; DBisSet = true; } // if LCD2 DB         
            if (DB == "<DB_Dock>") { DBname = Globals.RackPrefix + "Rack LCD Dockto"; DBisSet = true; } // if LCD2 DB   
            if (DBisSet == false) { DBname = DB; } // if no internal DB then look for the "DB" string in all IMyTerminalBlocks 
            return DBname; 
        } 
 
        // 
        // DB GET DATA SECTION _________________________________________________________________________ 
        // 
        //GET DB The Result is a text including line breaks and ASCII formatting  
        string GetDATA(string DB, string ident) 
        { 
            // example string starts = "<dir_root>"; & string ends = "</dir_root>"; 
            string starts = "<" + ident + ">"; 
            string ends = "</" + ident + ">"; 
            string GetDATA = ""; 
            string DBname = ""; 
            // get fresh Data 
            if (DB == "<DB_Me>") // Default  
            { 
                GetDATA = Me.CustomData; // GetDATA by default to Me.CustomData 
                // DBname = "Me"; Echo("DB read:" + DBname); 
            }// Only for feedback reasons   
            else 
            { // if not <DB_Me> get a Block and access its DB from CustomData 
                DBname = DBlocate(DB); 
                var Database = GridTerminalSystem.GetBlockWithName(DBname) as IMyTerminalBlock; 
                if (Database != null) { GetDATA = Database.CustomData; } // alternative if success 
                else { GetDATA = "Database not found: " + DBname; } 
                // Echo("DB read:" + DBname + " Ident:" + ident); 
            } 
 
            // get a Segment of Data 
            int startPos = GetDATA.LastIndexOf(starts) + starts.Length; 
            if (!(startPos > -1)) { return ""; } // return empty if not found 
            int length = GetDATA.IndexOf(ends) - startPos; 
            if (!(length > -1)) { return ""; } // return empty if not found 
            string TableDATA = GetDATA.Substring(startPos, length); 
            return TableDATA; 
        } 
 
        // Extract & Split DATA SECTION _______________________________________________________________________________________________________________ 
 
        // 
        // Extract Data from string by Identifyer by Digi (Discord @Digi#9441)  
        // 
        string ExtractDATA(string ident, string ConsoleDATA) 
        { 
            // example string starts = "<dir_root>"; & string ends = "</dir_root>"; 
            string starts = "<" + ident + ">"; 
            string ends = "</" + ident + ">"; 
            // get a Segment of Data 
            int startPos = ConsoleDATA.LastIndexOf(starts); 
            int endPos = (startPos > -1 ? ConsoleDATA.IndexOf(ends, startPos) : -1); 
            if (endPos > -1) { startPos += starts.Length; return ConsoleDATA.Substring(startPos, endPos - startPos); } 
            else { return ""; } 
        } 
 
        // 
        // DB WRITE into DATA SECTION replacing that section _______________________________________________ 
        // 
        //Replace all Data between tags eg. WriteDATA(<DB_xy>,"Test", "this is new content"); <Test>this is new content</Test> 
        void WriteDATA(string DB, string ident, string write) 
        { 
            // example string starts = "<dir_root>"; & string ends = "</dir_root>"; 
            string starts = "<" + ident + ">"; string ends = "</" + ident + ">"; 
            string DBname = ""; string DATA; //prep DB vars 
 
            // GET fresh Data 
            if (DB == "<DB_Me>") // Default  
            { 
                DATA = Me.CustomData; // GetDATA by default to Me.CustomData 
                // DBname = "Me";  Echo("DB read4write:" + DBname + ident); 
            }// Only for feedback reasons   
            else 
            { // if not <DB_Me> get a Block and access its DB from CustomData 
                DBname = DBlocate(DB); 
                var Database = GridTerminalSystem.GetBlockWithName(DBname) as IMyTerminalBlock; 
                if (Database != null) { DATA = Database.CustomData; } // alternative if success 
                else { DATA = "Database not found: " + DBname; } 
                // Echo("DB read4write:" + DBname + ident); 
            } 
 
            // Calculate Substring DB Part1 
            int startPos = DATA.LastIndexOf(starts) + starts.Length; 
            if (!(startPos > -1)) { Echo("NoData found, thus no startPos. ERROR EXIT"); return; } // return empty if not found 
            string DATA1 = DATA.Substring(0, startPos); 
            // Calculate Substring DB Part3 
            int totalLength = DATA.Length; 
            int startPos3 = DATA.LastIndexOf(ends); 
            if (!(startPos3 > -1)) { Echo("NoData found, thus no startPos3. ERROR EXIT"); return; } // return empty if not found 
            int length3 = totalLength - startPos3; 
            string DATA3 = DATA.Substring(startPos3, length3); 
            // Adding Part 1, new Part 2 and Part 3 
            string NewDATA = DATA1 + write + DATA3; 
            //Echo("Old:" + DATA.Length + " NewPart:" + write.Length + " New:" + NewDATA.Length); //Debug 
 
            // Writing Data 
            if (DB == "<DB_Me>") // Default get fresh Data 
            { 
                Me.CustomData = NewDATA; // WriteDATA by default to Me.CustomData 
                // DBname = "Me"; Echo("DB write:" + DBname); 
            }// Only for feedback reasons 
            else 
            { // if not <DB_Me> get a Block and access its DB from CustomData 
                DBname = DBlocate(DB); 
                var Database = GridTerminalSystem.GetBlockWithName(DBname) as IMyTerminalBlock; 
                if (Database != null) { Database.CustomData = NewDATA; } // alternative if success 
                else { Echo("No Database found!:" + DBname); } 
                // Echo("DB write:" + DBname + ident); 
            } // only feedback 
        } 
 
        //Replace Data oldstring > newstring: ReplaceWriteDATA(<DB_xy>,"Test", "this is new content"); 
        void ReplaceWriteDATA(string DB, string oldContent, string newContent) 
        { 
            /* replace Write DB  Echo("ReplaceWriteDATA"); */ 
            DB = DBlocate(DB); 
            var DBBlock = GridTerminalSystem.GetBlockWithName(DB) as IMyTerminalBlock; 
            if (DBBlock == null) { Echo("ERROR: DB not found."); return; } 
            string DBcontent = DBBlock.CustomData; // read CustomData 
            DBBlock.CustomData = DBcontent.Replace(oldContent, newContent); // replace and write CustomData 
        } 
 
        // 
        //Input Data is split into an Array (rows) 
        // 
        string[] ExtractARRAY(char split, string TableDATA) 
        { 
            string[] RowDATA = TableDATA.Split(split); //get each "Row" or "Line" eg. char('\n') 
            for (int i = 0; i < RowDATA.Length; i++) { RowDATA[i] = RowDATA[i].Trim(); } //to prevent SE putting in spaces we need .Trim() 
                                                                                         //Echo(RowDATA[0]); // DEBUG 
            return RowDATA; 
        } 
 



    }
}