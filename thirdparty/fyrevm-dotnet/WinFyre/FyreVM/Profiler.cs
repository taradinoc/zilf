/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FyreVM.Profiling
{
    /// <summary>
    /// Describes a routine and collects performance statistics about it.
    /// </summary>
    public class ProfiledRoutine
    {
        private readonly uint address;
        private string name, source, desc;
        private long cycles;
        private TimeSpan time;
        private uint hitCount;

        internal ProfiledRoutine(uint address)
        {
            this.address = address;
        }

        /// <summary>
        /// Gets the routine's address.
        /// </summary>
        public uint Address
        {
            get { return address; }
        }

        /// <summary>
        /// Gets the routine's name, or <b>null</b> if the name is unknown.
        /// </summary>
        public string Name
        {
            get { return name; }
            internal set { name = value; }
        }

        /// <summary>
        /// Gets the source code reference where the routine was defined, or
        /// <b>null</b> if the definition point is unknown.
        /// </summary>
        public string Source
        {
            get { return source; }
            internal set { source = value; }
        }

        /// <summary>
        /// Gets a human-readable description for the routine, or <b>null</b>
        /// if no description is available.
        /// </summary>
        public string Description
        {
            get { return desc; }
            internal set { desc = value; }
        }

        /// <summary>
        /// Gets the number of opcodes that have been executed for this routine.
        /// </summary>
        public long Cycles
        {
            get { return cycles; }
            internal set { cycles = value; }
        }

        /// <summary>
        /// Gets the length of time that has been spent executing this routine.
        /// </summary>
        public TimeSpan Time
        {
            get { return time; }
            internal set { time = value; }
        }

        /// <summary>
        /// Gets the number of times this routine has been called.
        /// </summary>
        public uint HitCount
        {
            get { return hitCount; }
            internal set { hitCount = value; }
        }
    }

    /// <summary>
    /// Tracks and analyzes a game's performance.
    /// </summary>
    public class Profiler
    {
        private class SymbolInfo
        {
            public string Name;
            public string Source;
            public string Description;

            public SymbolInfo(string name, string source)
            {
                this.Name = name;
                this.Source = source;
                this.Description = "";
            }
        }

        private struct SourceLine
        {
            public int FileNum;
            public int LineNum;
            public int CharPos;
        }

        private readonly Dictionary<uint, ProfiledRoutine> dict = new Dictionary<uint, ProfiledRoutine>();
        private uint currentRoutine;
        private long entryCycles;
        private DateTime entryTime;
        private readonly Stack<uint> routineStack = new Stack<uint>();
        private Dictionary<uint, SymbolInfo> symbols;
        private Dictionary<string, SymbolInfo> symbolsByName;
        private bool meterRunning = true;

        public Profiler()
        {
        }

        /// <summary>
        /// Clears the performance information that has been collected so far.
        /// </summary>
        public void ResetStats()
        {
            dict.Clear();
        }

        /// <summary>
        /// Gets or sets a value indicating whether performance is currently
        /// being tracked.
        /// </summary>
        public bool MeterRunning
        {
            get { return meterRunning; }
            set { meterRunning = value; }
        }

        /// <summary>
        /// Records that the VM is entering a routine.
        /// </summary>
        /// <param name="address">The address of the routine.</param>
        /// <param name="cycles">The current cycle count.</param>
        internal void Enter(uint address, long cycles)
        {
            routineStack.Push(currentRoutine);

            if (currentRoutine != 0)
            {
                long elapsedCycles = cycles - entryCycles;
                TimeSpan elapsedTime = DateTime.Now - entryTime;

                BillCurrentRoutine(elapsedCycles, elapsedTime);
            }

            currentRoutine = address;
            BumpHitCount();
            entryCycles = cycles;
            entryTime = DateTime.Now;
        }

        /// <summary>
        /// Records that the VM is leaving the most recently entered routine.
        /// </summary>
        /// <param name="cycles">The current cycle count.</param>
        internal void Leave(long cycles)
        {
            long elapsedCycles = cycles - entryCycles;
            TimeSpan elapsedTime = DateTime.Now - entryTime;

            BillCurrentRoutine(elapsedCycles, elapsedTime);

            currentRoutine = routineStack.Pop();
            entryCycles = cycles;
            entryTime = DateTime.Now;
        }

        private ProfiledRoutine GetRoutineRecord()
        {
            ProfiledRoutine rec;
            if (dict.TryGetValue(currentRoutine, out rec) == false)
            {
                rec = new ProfiledRoutine(currentRoutine);
                dict.Add(currentRoutine, rec);

                SymbolInfo info;
                if (symbols != null && symbols.TryGetValue(currentRoutine, out info))
                {
                    rec.Name = info.Name;
                    rec.Source = info.Source;
                    rec.Description = info.Description;
                }
            }
            return rec;
        }

        private void BillCurrentRoutine(long elapsedCycles, TimeSpan elapsedTime)
        {
            if (meterRunning)
            {
                ProfiledRoutine rec = GetRoutineRecord();

                rec.Cycles += elapsedCycles;
                rec.Time += elapsedTime;
            }
        }

        private void BumpHitCount()
        {
            if (meterRunning)
            {
                ProfiledRoutine rec = GetRoutineRecord();

                rec.HitCount++;
            }
        }

        /// <summary>
        /// Reads function names and addresses from a game debugging file.
        /// </summary>
        /// <param name="gameFile">The game being profiled.</param>
        /// <param name="debugFile">The Inform debugging file (gameinfo.dbg).</param>
        public void ReadDebugSymbols(Stream gameFile, Stream debugFile)
        {
            debugFile.Seek(0, SeekOrigin.Begin);
            if (debugFile.ReadByte() != 0xDE || debugFile.ReadByte() != 0xBF)
                throw new ArgumentException("Not an Inform debug file", "debugFile");

            ushort version = BigEndian.ReadInt16(debugFile);
            if (version != 0)
                throw new ArgumentException("Unsupported debug file version", "debugFile");

            BigEndian.ReadInt16(debugFile); // discard Inform version

            byte[] header = null;
            uint codeOffset = 0;
            List<uint> routineAddrs = new List<uint>();
            List<string> routineNames = new List<string>();
            List<string> routineSources = new List<string>();

            Dictionary<int, string> fileNames = new Dictionary<int, string>();

            while (debugFile.Position < debugFile.Length)
            {
                int type = debugFile.ReadByte();
                switch (type)
                {
                    case 0: // EOF_DBR
                        debugFile.Seek(0, SeekOrigin.End);
                        break;

                    case 1: // FILE_DBR
                        int fileNum = debugFile.ReadByte();
                        SkipString(debugFile);
                        string fileName = ReadString(debugFile);
                        fileNames.Add(fileNum, Path.GetFileName(fileName));
                        break;

                    case 2: // CLASS_DBR
                        SkipString(debugFile);
                        debugFile.Seek(8, SeekOrigin.Current);
                        break;

                    case 3: // OBJECT_DBR
                        debugFile.Seek(2, SeekOrigin.Current);
                        SkipString(debugFile);
                        debugFile.Seek(8, SeekOrigin.Current);
                        break;

                    case 4: // GLOBAL_DBR
                        debugFile.ReadByte();
                        SkipString(debugFile);
                        break;

                    case 12: // ARRAY_DBR
                    case 5: // ATTR_DBR
                    case 6: // PROP_DBR
                    case 7: // FAKE_ACTION_DBR
                    case 8: // ACTION_DBR
                        debugFile.Seek(2, SeekOrigin.Current);
                        SkipString(debugFile);
                        break;

                    case 9: // HEADER_DBR
                        header = new byte[64];
                        debugFile.Read(header, 0, 64);
                        break;

                    case 11: // ROUTINE_DBR
                        debugFile.Seek(2, SeekOrigin.Current);
                        SourceLine line = ReadLine(debugFile);
                        switch (line.FileNum)
                        {
                            case 0:
                                routineSources.Add("");
                                break;

                            case 255:
                                routineSources.Add("<veneer>");
                                break;

                            default:
                                routineSources.Add(string.Format("{0}:{1}",
                                    fileNames[line.FileNum], line.LineNum));
                                break;
                        }
                        routineAddrs.Add(ReadAddress(debugFile));
                        routineNames.Add(ReadString(debugFile));
                        while (SkipString(debugFile) == true)
                        {
                            // keep skipping local variable names
                        }
                        break;

                    case 10: // LINEREF_DBR
                        debugFile.Seek(2, SeekOrigin.Current);
                        ushort numSeqPts = BigEndian.ReadInt16(debugFile);
                        debugFile.Seek(numSeqPts * 6, SeekOrigin.Current);
                        break;

                    case 14: // ROUTINE_END_DBR
                        debugFile.Seek(9, SeekOrigin.Current);
                        break;

                    case 13: // MAP_DBR
                        while (true)
                        {
                            string key = ReadString(debugFile);
                            if (key.Length == 0)
                                break;
                            uint value = ReadAddress(debugFile);
                            if (key == "code area")
                                codeOffset = value;
                        }
                        break;
                }
            }

            // verify header
            if (header != null)
            {
                byte[] origHeader = new byte[64];
                gameFile.Seek(0, SeekOrigin.Begin);
                gameFile.Read(origHeader, 0, 64);
                for (int i = 0; i < 64; i++)
                    if (header[i] != origHeader[i])
                        throw new ArgumentException("Debug file header does not match game", "debugFile");
            }

            // store routine addresses
            if (symbols == null)
            {
                symbols = new Dictionary<uint, SymbolInfo>();
                symbolsByName = new Dictionary<string, SymbolInfo>();
            }
            for (int i = 0; i < routineAddrs.Count; i++)
            {
                SymbolInfo info = new SymbolInfo(routineNames[i], routineSources[i]);
                symbols[codeOffset + routineAddrs[i]] = info;
                symbolsByName[info.Name] = info;
            }

            UpdateRecsFromSymbols();
        }

        /// <summary>
        /// Reads routine descriptions and definition points from a source file
        /// generated by Inform 7 (auto.inf).
        /// </summary>
        /// <param name="autoInfFile"></param>
        /// <remarks>This will probably break when the layout of auto.inf changes.</remarks>
        public void ReadDescriptions(string autoInfFile)
        {
            if (symbolsByName == null)
                throw new InvalidOperationException("Call ReadDebugSymbols first");

            int lineNum = 1;
            string currentObjID = "";
            List<string> routineNames = new List<string>();
            List<string> routineDescs = new List<string>();
            char[] spaceDelim = { ' ' };
            string lastLine = "", lastRoutine = "";

            using (FileStream stream = new FileStream(autoInfFile, FileMode.Open, FileAccess.Read))
            {
                autoInfFile = Path.GetFileName(autoInfFile);
                using (StreamReader rdr = new StreamReader(stream))
                {
                    while (!rdr.EndOfStream)
                    {
                        string line = rdr.ReadLine();
                        if (line.StartsWith("Class ") || line.StartsWith("Object "))
                        {
                            string tempLine = line.Replace("->", "");
                            string[] parts = tempLine.Split(spaceDelim, StringSplitOptions.RemoveEmptyEntries);
                            currentObjID = parts[1];
                        }
                        else if (line.StartsWith(" with parse_name "))
                        {
                            string[] parts = line.Split(spaceDelim, StringSplitOptions.RemoveEmptyEntries);
                            routineNames.Add(parts[2]);
                            routineDescs.Add("parses the name of " + currentObjID);
                        }
                        else if (line.StartsWith("  Relation_"))
                        {
                            // an entry in the relations table
                            string[] parts = line.Split(spaceDelim, 2, StringSplitOptions.RemoveEmptyEntries);
                            routineNames.Add(parts[0]);
                            int quote = line.IndexOf('"'), unquote = line.LastIndexOf('"');
                            string relDesc = line.Substring(quote + 1, unquote - quote - 2);
                            int relates = relDesc.IndexOf(" relates ");
                            routineDescs.Add("implements the \"" +
                                relDesc.Substring(0, relates) + "\" relation");
                        }
                        else if (line.StartsWith("[ Adj_"))
                        {
                            // an adjective routine definition
                            string[] parts = line.Split(spaceDelim);
                            routineNames.Add(parts[1]);
                            int bang = line.IndexOf('!');
                            routineDescs.Add(line.Substring(bang + 2));
                        }
                        else if (line.StartsWith("[ "))
                        {
                            // any other routine definition
                            int endPos = line.IndexOfAny(routineDelims, 2);
                            if (endPos == -1)
                                endPos = line.Length;
                            string routine = line.Substring(2, endPos - 2);

                            if (lastLine.StartsWith("! "))
                            {
                                routineNames.Add(routine);
                                string desc = lastLine.Substring(2);
                                if (desc.EndsWith(":"))
                                    desc = desc.Substring(0, desc.Length - 1);
                                routineDescs.Add(desc);
                            }
                            else if (routine.StartsWith("R_SHELL_") && lastRoutine.StartsWith("R_"))
                            {
                                routineNames.Add(routine);
                                routineDescs.Add("the main part of " + lastRoutine);
                            }

                            lastRoutine = routine;
                        }
                        lineNum++;
                        if (line.StartsWith("! ") &&
                            (lastLine.StartsWith("! From ") || lastLine.StartsWith("! Find ") ||
                             lastLine.StartsWith("! True or ") ||
                             lastLine.StartsWith("! How many ") ||
                             lastLine.StartsWith("! Make everything ")))
                            lastLine = "! [" + lastLine.Substring(2) + "] " + line.Substring(2);
                        else
                            lastLine = line;
                    }
                }
            }

            // label the routines we just found
            for (int i = 0; i < routineNames.Count; i++)
            {
                SymbolInfo info;
                if (symbolsByName.TryGetValue(routineNames[i], out info))
                    info.Description = routineDescs[i];
            }

            // change temporary file name to auto.inf for all routines
            foreach (SymbolInfo info in symbols.Values)
            {
                if (info.Source.StartsWith(autoInfFile))
                {
                    int colon = info.Source.LastIndexOf(':');
                    info.Source = "Inform 7 (auto.inf" + info.Source.Substring(colon) + ")";
                }
            }

            UpdateRecsFromSymbols();
        }

        /// <summary>
        /// Naively assigns descriptions to routines based on their names.
        /// </summary>
        public void SetDefaultDescriptions()
        {
            foreach (SymbolInfo info in symbols.Values)
                if (info.Description == "")
                    info.Description = GetDefaultDescription(info.Name);

            UpdateRecsFromSymbols();
        }

        private string GetDefaultDescription(string routine)
        {
            if (routine.StartsWith("Resolver_"))
                return "dispatch routine for an overload phrase";

            if (routine.StartsWith("PHR_"))
                return "a phrase";

            if (routine.StartsWith("R_"))
                return "a rule";

            if (routine.StartsWith("R_SHELL_"))
                return "the main part of a rule or phrase which uses dynamic blocks";

            if (routine.StartsWith("Prop_"))
                return "a set of objects or a query about objects)";

            if (routine.StartsWith("Consult_Grammar_"))
                return "a topic";

            if (routine.StartsWith("GPR_Line_"))
                return "assists with parsing topics";

            if (routine.StartsWith("Parse_Name_"))
                return "parses a complicated object or kind name";

            if (routine.StartsWith("Cond_Token_"))
                return "the condition for an understand-when line";

            if (routine.StartsWith("text_routine_"))
                return "text with bracketed substitutions";

            if (routine.StartsWith("LOS_"))
                return "tests \"in the presence of\"";

            if (routine.StartsWith("NAP_"))
                return "a named category of actions";

            if (routine.StartsWith("PAPR_"))
                return "a chronology test event";

            if (routine.EndsWith("found_in"))
                return "backdrop placement";

            // no match
            return "";
        }

        private static readonly char[] routineDelims = { ' ', ';' };

        /// <summary>
        /// Loads routine names and definition points from an Inform source
        /// file that wasn't generated by Inform 7 (for example, the .i6 template layer).
        /// </summary>
        /// <param name="sourceFile"></param>
        public void ReadSourceSymbols(string sourceFile)
        {
            if (symbolsByName == null)
                throw new InvalidOperationException("Call ReadDebugSymbols first");

            using (FileStream stream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                sourceFile = Path.GetFileName(sourceFile);
                using (StreamReader rdr = new StreamReader(stream))
                {
                    int lineNum = 1;
                    while (!rdr.EndOfStream)
                    {
                        string line = rdr.ReadLine();
                        if (line.StartsWith("[ "))
                        {
                            int endPos = line.IndexOfAny(routineDelims, 2);
                            if (endPos == -1)
                                endPos = line.Length;
                            string routine = line.Substring(2, endPos - 2);

                            SymbolInfo info;
                            if (symbolsByName.TryGetValue(routine, out info))
                                info.Source = string.Format("{0}:{1}", sourceFile, lineNum);
                        }
                        lineNum++;
                    }
                }
            }

            UpdateRecsFromSymbols();
        }

        private void UpdateRecsFromSymbols()
        {
            // update any profiling records that already exist
            foreach (ProfiledRoutine rec in dict.Values)
            {
                SymbolInfo info;
                if (symbols.TryGetValue(rec.Address, out info))
                {
                    rec.Name = info.Name;
                    rec.Source = info.Source;
                    rec.Description = info.Description;
                }
            }
        }

        private static SourceLine ReadLine(Stream debugFile)
        {
            SourceLine result;

            result.FileNum = debugFile.ReadByte();
            result.LineNum = BigEndian.ReadInt16(debugFile);
            result.CharPos = debugFile.ReadByte();

            return result;
        }

        private static string ReadString(Stream debugFile)
        {
            StringBuilder sb = new StringBuilder();

            int ch = debugFile.ReadByte();
            while (ch > 0)
            {
                sb.Append((char)ch);
                ch = debugFile.ReadByte();
            }

            return sb.ToString();
        }

        private static bool SkipString(Stream debugFile)
        {
            int ch = debugFile.ReadByte();
            if (ch == 0)
                return false;

            do { ch = debugFile.ReadByte(); } while (ch > 0);
            return true;
        }

        private static uint ReadAddress(Stream debugFile)
        {
            int a = debugFile.ReadByte();
            int b = debugFile.ReadByte();
            int c = debugFile.ReadByte();
            return (uint)((a << 16) + (b << 8) + c);
        }

        /// <summary>
        /// Gets the current set of profiler results.
        /// </summary>
        /// <returns>An array of <see cref="ProfiledRoutine"/> records.</returns>
        public ProfiledRoutine[] GetResults()
        {
            return dict.Values.ToArray();
        }
    }
}
