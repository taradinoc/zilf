/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
 *
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Zilf.Common.StringEncoding;

namespace Zilf.Emit.Cornerstone
{
    public sealed partial class GameBuilder : IGameBuilder
    {
        private const string BaseModuleName = "Main";
        private const int MaxPropertyId = 31;
        internal const string ObjectRecordsLabel = "__OBJECT_RECORDS";
        internal const string ObjectNamesLabel = "__OBJECT_NAMES";
        internal const string PropertyDefaultsLabel = "__PROPERTY_DEFAULTS";
        internal const string ObjDataBufferLabel = "__OBJ_DATA_BUFFER";
        internal const string OutputBufferLabel = "__OUTPUT_BUFFER";
        internal const string OutputWindowGeometryLabel = "__OUTPUT_WINDOW_GEOMETRY";
        internal const string OutputWindowDescriptorLabel = "__OUTPUT_WINDOW_DESCRIPTOR";
        internal const string PackedTextAlphabet0Label = "__TEXT_ALPHABET_0";
        internal const string PackedTextAlphabet1Label = "__TEXT_ALPHABET_1";
        internal const string PackedTextAlphabet2Label = "__TEXT_ALPHABET_2";
        internal const string PackedTextUnpackBufferLabel = "__TEXT_UNPACK_BUFFER";
        internal const string TokenCompareBufferLabel = "__TOKEN_COMPARE_BUFFER";
        internal const string CommandFileInputBufferLabel = "__COMMAND_FILE_INPUT_BUFFER";
        internal const string CommandFileNameBufferLabel = "__COMMAND_FILE_NAME_BUFFER";
        internal const string CommandFileTransferBufferLabel = "__COMMAND_FILE_TRANSFER_BUFFER";
        internal const string VocabularyTableLabel = "VOCAB_TABLE";
        internal const string MetadataCreatorLabel = "__METADATA_CREATOR";
        internal const string MetadataReleaseIdLabel = "__METADATA_RELEASEID";
        internal const string MetadataSerialLabel = "__METADATA_SERIAL";
        internal const string SelfInsertingBreaksLabel = "__SI_BREAKS";
        internal const int FalseSentinel = 0x8001;
        internal const int OutputBufferCapacity = 256;
        internal const int CommandFileNameMaxLength = 63;
        internal const int VocabularyResolution = 6;
        internal const int VocabularyStringIdWordOffset = 0;
        internal const int VocabularyRecordWordPointerOffset = 4;
        internal const int VocabularyEntrySizeBytes = 10;
        internal const int VocabularyEntryWordCount = VocabularyEntrySizeBytes / WordSizeBytes;
        internal const int WordSizeBytes = 2;
        internal const int ObjectRecordParentOffset = 0;
        internal const int ObjectRecordSiblingOffset = 1;
        internal const int ObjectRecordChildOffset = 2;
        internal const int ObjectRecordFlagsOffset = 3;
        internal const int ObjectRecordPropertiesOffset = 4;
        internal const int ObjectRecordWordCount = 5;
        internal const int ScreenWidthSlot = 0xC7;
        internal const int ScreenHeightSlot = 0xC8;
        internal const int CursorRowSlot = 0xCA;
        internal const int CursorColumnSlot = 0xC9;
        internal const int ScrollBottomSlot = 0xC4;
        internal const int ScrollTopSlot = 0xC5;
        internal const int TextAttributeSlot = 0xD5;
        internal const int DisplayModeSlot = 0xDB;
        internal const int MainTextTopRow = 1;
        internal const int StatusLineHereGlobalIndex = 0;
        internal const int StatusLineScoreGlobalIndex = 1;
        internal const int StatusLineMovesGlobalIndex = 2;

        private readonly ICornerstoneStreamFactory streamFactory;
        private readonly List<NamedVariable> globals = [];
        private readonly List<NamedConstant> constants = [];
        private readonly List<TableBuilder> tables = [];
        private readonly List<RoutineBuilder> routines = [];
        private readonly List<ObjectBuilder> objects = [];
        private readonly List<PropertyBuilder> properties = [];
        private readonly List<FlagBuilder> flags = [];
        private readonly List<WordBuilder> words = [];
        private readonly Dictionary<string, string> globalDefinitions = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SymbolicOperand> stringOperands = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ObjDataReference> objDataStrings = new(StringComparer.Ordinal);
        private readonly List<RamBytesDefinition> ramBytes = [];
        private readonly List<ObjDataDefinition> objData = [];
        private readonly Dictionary<char, long> packedTextHistogram = [];
#if DEBUG
        private readonly Dictionary<string, int> compilerOptimizationStats = new(StringComparer.Ordinal);
#endif
        private readonly NamedVariable consoleRow;
        private readonly NamedVariable consoleColumn;
        private readonly NamedVariable commandInputChannel;
        private readonly NamedVariable commandOutputChannel;
        private readonly NamedVariable commandFileBypass;
        private readonly NamedVariable commandInputPendingByte;
        private readonly NamedVariable stream3Table;
        private readonly NamedVariable stream3StackPointer;
        private TableBuilder? stream3Stack;
        private TableBuilder? objDataBuffer;
        private TableBuilder? outputBuffer;
        private TableBuilder? outputWindowGeometry;
        private TableBuilder? outputWindowDescriptor;
        private TableBuilder? packedTextUnpackBuffer;
        private TableBuilder? tokenCompareBuffer;
        private TableBuilder? commandFileInputBuffer;
        private TableBuilder? commandFileNameBuffer;
        private TableBuilder? commandFileTransferBuffer;

        private int nextRamLabelId;
        private bool finished;
        private bool packedTextPrepared;
        private PackedTextEncoding? packedTextEncoding;

        internal RuntimeLib RuntimeLib { get; }

        internal int ConsoleRowGlobalIndex => consoleRow.Index;

        internal int ConsoleColumnGlobalIndex => consoleColumn.Index;

        internal int CommandInputChannelGlobalIndex => commandInputChannel.Index;

        internal int CommandOutputChannelGlobalIndex => commandOutputChannel.Index;

        internal int CommandFileBypassGlobalIndex => commandFileBypass.Index;

        internal int CommandInputPendingByteGlobalIndex => commandInputPendingByte.Index;

        internal int Stream3TableGlobalIndex => stream3Table.Index;

        internal int Stream3StackPointerGlobalIndex => stream3StackPointer.Index;

        internal int EmulatedZMachineVersion => ((CornerstoneGameOptions)Options).ZMachineVersion;

        internal int EmulatedZVersionFlags => EmulatedZMachineVersion <= 3
            ? 0x20 | (((CornerstoneGameOptions)Options).TimeStatusLine ? 0x02 : 0x00)
            : 0x1C;

        internal IReadOnlyList<NamedVariable> Globals => globals;

        internal int PropertyCount => properties.Count;

        internal int HighestAssignedPropertyId => properties.Count == 0 ? 0 : properties.Max(property => property.Id);

        internal int FlagCount => flags.Count;

        public GameBuilder(ICornerstoneStreamFactory streamFactory, CornerstoneGameOptions? options = null, RuntimeLib? runtimeLib = null)
        {
            this.streamFactory = streamFactory;
            Options = options ?? new CornerstoneGameOptions();
            Zero = new NumericOperand(0);
            One = new NumericOperand(1);
            VocabularyTable = new SymbolicOperand(VocabularyTableLabel);

            CreateReservedStatusGlobal("HERE", StatusLineHereGlobalIndex);
            CreateReservedStatusGlobal("SCORE", StatusLineScoreGlobalIndex);
            CreateReservedStatusGlobal("MOVES", StatusLineMovesGlobalIndex);

            RuntimeLib = runtimeLib ?? new RuntimeLib();
            RuntimeLib.Attach(this);

            consoleRow = AddGlobal("__CONSOLE_ROW");
            // Default to the bottom row used by 24-row text mode until startup probes the real height.
            consoleRow.DefaultValue = MakeOperand(0x17);

            consoleColumn = AddGlobal("__CONSOLE_COLUMN");
            consoleColumn.DefaultValue = Zero;

            commandInputChannel = AddGlobal("__COMMAND_INPUT_CHANNEL");
            commandInputChannel.DefaultValue = MakeOperand(FalseSentinel);

            commandOutputChannel = AddGlobal("__COMMAND_OUTPUT_CHANNEL");
            commandOutputChannel.DefaultValue = MakeOperand(FalseSentinel);

            commandFileBypass = AddGlobal("__COMMAND_FILE_BYPASS");
            commandFileBypass.DefaultValue = Zero;

            commandInputPendingByte = AddGlobal("__COMMAND_INPUT_PENDING_BYTE");
            commandInputPendingByte.DefaultValue = MakeOperand(FalseSentinel);

            stream3Table = AddGlobal("__STREAM3_TABLE");
            stream3Table.DefaultValue = MakeOperand(FalseSentinel);

            stream3StackPointer = AddGlobal("__STREAM3_SP");
            stream3StackPointer.DefaultValue = Zero;
        }

        public IGameOptions Options { get; }

        public IDebugFileBuilder? DebugFile => null;

        public int MaxPropertyLength => ushort.MaxValue;

        public int MaxProperties => MaxPropertyId;

        public int MaxFlags => ushort.MaxValue;

        public int MaxCallArguments => byte.MaxValue;

        public INumericOperand Zero { get; }

        public INumericOperand One { get; }

        public IConstantOperand VocabularyTable { get; }

        public ICollection<char> SelfInsertingBreaks { get; } = new HashSet<char>();

        public IGlobalBuilder DefineGlobal(string name)
        {
            globalDefinitions[name] = nameof(IGlobalBuilder);

            if (TryGetReservedStatusGlobal(name, out var reservedGlobal))
                return reservedGlobal;

            return AddGlobal(name);
        }

        public ITableBuilder DefineTable(string? name, bool pure)
        {
            var result = new TableBuilder(name ?? $"TABLE_{tables.Count + 1}", pure);
            tables.Add(result);
            if (name != null)
                globalDefinitions[name] = nameof(ITableBuilder);
            return result;
        }

        public IOperand DefineUnicodeTranslationTable(IEnumerable<char> characters)
        {
            var table = new TableBuilder($"UNICODE_{tables.Count + 1}", pure: true);
            foreach (var character in characters)
                table.AddWord(character);
            tables.Add(table);
            return table;
        }

        public IRoutineBuilder DefineRoutine(string name, bool entryPoint, bool cleanStack)
        {
            globalDefinitions[name] = nameof(IRoutineBuilder);
            var result = new RoutineBuilder(this, name, entryPoint, cleanStack);
            routines.Add(result);
            return result;
        }

        internal RoutineBuilder CreateRuntimeRoutine(string baseName)
        {
            var helperName = baseName;
            for (var suffix = 1; globalDefinitions.ContainsKey(helperName) || routines.Any(r => string.Equals(r.Name, helperName, StringComparison.OrdinalIgnoreCase)); suffix++)
                helperName = $"{baseName}_{suffix}";

            var routine = new RoutineBuilder(this, helperName, entryPoint: false, cleanStack: false);
            routines.Add(routine);
            globalDefinitions[helperName] = nameof(IRoutineBuilder);
            return routine;
        }

        public IObjectBuilder DefineObject(string name)
        {
            globalDefinitions[name] = nameof(IObjectBuilder);
            var result = new ObjectBuilder(name, objects.Count + 1);
            objects.Add(result);
            return result;
        }

        public IPropertyBuilder DefineProperty(string name)
        {
            globalDefinitions[name] = nameof(IPropertyBuilder);
            var propertyId = MaxPropertyId - properties.Count;
            if (propertyId < 1)
                throw new InvalidOperationException($"Too many properties for Cornerstone backend. Maximum supported is {MaxPropertyId}.");

            var result = new PropertyBuilder(name, propertyId);
            properties.Add(result);
            return result;
        }

        public IFlagBuilder DefineFlag(string name)
        {
            globalDefinitions[name] = nameof(IFlagBuilder);
            var result = new FlagBuilder(name, flags.Count + 1);
            flags.Add(result);
            return result;
        }

        public INumericOperand MakeOperand(int value) => new NumericOperand(value);

        public IOperand MakeOperand(char value) => new NumericOperand(value);

        public IOperand MakeOperand(string value) => RegisterObjString(value).Location;

        public IOperand DefineConstant(string name, IOperand value)
        {
            globalDefinitions[name] = nameof(IConstantOperand);
            var constant = new NamedConstant(name, value.StripIndirect());
            constants.Add(constant);
            return constant;
        }

        public IWordBuilder DefineVocabularyWord(string word)
        {
            var result = new WordBuilder(this, $"WORD_{words.Count + 1}", word);
            words.Add(result);
            return result;
        }

        public void RemoveVocabularyWord(string word)
        {
            var existing = words.FirstOrDefault(w => string.Equals(w.Word, word, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                words.Remove(existing);
        }

        public bool IsGloballyDefined(string name, [NotNullWhen(true)] out string? type)
        {
            return globalDefinitions.TryGetValue(name, out type);
        }

        public void RecordCompilerOptimizationStatistic(string name, int count)
        {
#if DEBUG
            compilerOptimizationStats[name] = compilerOptimizationStats.GetValueOrDefault(name) + count;
#endif
        }

        public void Finish()
        {
            if (finished)
                return;

            using var stream = streamFactory.CreateMainStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));

            writer.WriteLine("; Cornerstone backend output from ZILF");
            writer.WriteLine($"; Source: {streamFactory.GetMainFileName(withExt: true)}");
            writer.WriteLine();

            EmitConstants(writer);
            EmitProgramGlobalDefinitions(writer);
            EmitModules(writer);
            PreparePackedText();
            PrepareMetadata();
            EmitRam(writer);
            EmitObjData(writer);
#if DEBUG
            WriteCompilerOptimizationStatistics(writer);
#endif

            finished = true;
        }

#if DEBUG
        private void WriteCompilerOptimizationStatistics(TextWriter writer)
        {
            if (compilerOptimizationStats.Count == 0)
                return;

            writer.WriteLine();
            writer.WriteLine("; Compiler optimization statistics (debug build)");
            foreach (var entry in compilerOptimizationStats.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
                writer.WriteLine($";   {entry.Key}: {entry.Value}");
        }
#endif

        public void Dispose()
        {
            Finish();
        }

        internal ObjDataReference RegisterObjString(string value)
        {
            if (objDataStrings.TryGetValue(value, out var existing))
            {
                AccumulatePackedTextHistogram(value);
                return existing;
            }

            EnsureAscii(value);
            AccumulatePackedTextHistogram(value);

            if (packedTextPrepared)
                throw new InvalidOperationException("Packed Cornerstone text cannot be registered after RAM emission has been prepared.");

            var label = $"OBJSTR_{nextRamLabelId:X4}";
            nextRamLabelId++;

            var reference = new ObjDataReference(new SymbolicOperand(label));
            objDataStrings.Add(value, reference);
            objData.Add(new ObjDataDefinition(label, value, []));
            return reference;
        }

        internal RoutineBuilder? FindRoutine(string name) => routines.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

        internal string EnsureObjDataBufferName()
        {
            if (objDataBuffer != null)
                return objDataBuffer.Name;

            objDataBuffer = new TableBuilder(ObjDataBufferLabel, pure: false);
            for (var index = 0; index < 128; index++)
                objDataBuffer.AddWord(0);

            tables.Add(objDataBuffer);
            return objDataBuffer.Name;
        }

        internal string EnsureOutputBufferName()
        {
            if (outputBuffer != null)
                return outputBuffer.Name;

            outputBuffer = new TableBuilder(OutputBufferLabel, pure: false);
            outputBuffer.AddWord(0);
            for (var index = 0; index < OutputBufferCapacity; index++)
                outputBuffer.AddByte(0);

            tables.Add(outputBuffer);
            return outputBuffer.Name;
        }

        internal string EnsureOutputWindowDescriptorName()
        {
            if (outputWindowDescriptor != null)
                return outputWindowDescriptor.Name;

            outputWindowGeometry = new TableBuilder(OutputWindowGeometryLabel, pure: false);
            outputWindowGeometry.AddWord(0);
            outputWindowGeometry.AddWord(0);
            outputWindowGeometry.AddWord(0);
            outputWindowGeometry.AddWord(0);
            outputWindowGeometry.AddWord(0); // patched at runtime by EmitPrepareOutputWindowDescriptor
            outputWindowGeometry.AddWord(0x0100);
            tables.Add(outputWindowGeometry);

            outputWindowDescriptor = new TableBuilder(OutputWindowDescriptorLabel, pure: false);
            outputWindowDescriptor.AddWord(0);
            outputWindowDescriptor.AddWord(0);
            outputWindowDescriptor.AddWord(0);
            outputWindowDescriptor.AddWord(0);
            tables.Add(outputWindowDescriptor);
            return outputWindowDescriptor.Name;
        }

        internal string EnsureOutputWindowGeometryName()
        {
            EnsureOutputWindowDescriptorName();
            return outputWindowGeometry!.Name;
        }

        internal string EnsurePackedTextUnpackBufferName()
        {
            if (packedTextUnpackBuffer != null)
                return packedTextUnpackBuffer.Name;

            packedTextUnpackBuffer = new TableBuilder(PackedTextUnpackBufferLabel, pure: false);
            packedTextUnpackBuffer.AddWord(3);
            packedTextUnpackBuffer.AddByte(0);
            packedTextUnpackBuffer.AddByte(0);
            packedTextUnpackBuffer.AddByte(0);
            tables.Add(packedTextUnpackBuffer);
            return packedTextUnpackBuffer.Name;
        }

        internal string EnsureTokenCompareBufferName()
        {
            if (tokenCompareBuffer != null)
                return tokenCompareBuffer.Name;

            tokenCompareBuffer = new TableBuilder(TokenCompareBufferLabel, pure: false);
            tokenCompareBuffer.AddWord(0);
            for (var index = 0; index < VocabularyResolution; index++)
                tokenCompareBuffer.AddByte(0);

            tables.Add(tokenCompareBuffer);
            return tokenCompareBuffer.Name;
        }

        internal string EnsureCommandFileInputBufferName()
        {
            if (commandFileInputBuffer != null)
                return commandFileInputBuffer.Name;

            commandFileInputBuffer = new TableBuilder(CommandFileInputBufferLabel, pure: false);
            commandFileInputBuffer.AddByte(CommandFileNameMaxLength);
            commandFileInputBuffer.AddByte(0);
            for (var index = 0; index < CommandFileNameMaxLength; index++)
                commandFileInputBuffer.AddByte(0);
            commandFileInputBuffer.AddByte(0);

            tables.Add(commandFileInputBuffer);
            return commandFileInputBuffer.Name;
        }

        internal string EnsureCommandFileNameBufferName()
        {
            if (commandFileNameBuffer != null)
                return commandFileNameBuffer.Name;

            commandFileNameBuffer = new TableBuilder(CommandFileNameBufferLabel, pure: false);
            commandFileNameBuffer.AddWord(0);
            for (var index = 0; index < CommandFileNameMaxLength; index++)
                commandFileNameBuffer.AddByte(0);
            commandFileNameBuffer.AddByte(0);

            tables.Add(commandFileNameBuffer);
            return commandFileNameBuffer.Name;
        }

        internal string EnsureCommandFileTransferBufferName()
        {
            if (commandFileTransferBuffer != null)
                return commandFileTransferBuffer.Name;

            commandFileTransferBuffer = new TableBuilder(CommandFileTransferBufferLabel, pure: false);
            commandFileTransferBuffer.AddWord(0);
            commandFileTransferBuffer.AddWord(0);

            tables.Add(commandFileTransferBuffer);
            return commandFileTransferBuffer.Name;
        }

        internal string EnsureStream3StackName()
        {
            if (stream3Stack != null)
                return stream3Stack.Name;

            stream3Stack = new TableBuilder("__STREAM3_STACK", pure: false);
            for (var i = 0; i < 16; i++)
            {
                stream3Stack.AddWord(0);  // saved table address
                stream3Stack.AddWord(0);  // saved character count
            }

            tables.Add(stream3Stack);
            return stream3Stack.Name;
        }

        private NamedConstant? FindConstant(string name) =>
            constants.FirstOrDefault(constant => string.Equals(constant.Name, name, StringComparison.OrdinalIgnoreCase));

        private void EmitConstants(TextWriter writer)
        {
            var emittedAny = false;

            foreach (var obj in objects)
            {
                writer.WriteLine($"{obj.Name} = {FormatWord(obj.Id)}");
                emittedAny = true;
            }

            foreach (var property in properties)
            {
                writer.WriteLine($"P?{property.Name} = {FormatWord(property.Id)}");
                emittedAny = true;
            }

            foreach (var flag in flags)
            {
                writer.WriteLine($"{flag.Name} = {FormatWord(flag.Id)}");
                emittedAny = true;
            }

            foreach (var constant in constants)
            {
                writer.WriteLine($"{constant.Name} = {FormatConstantOperand(constant.Value)}");
                emittedAny = true;
            }

            if (emittedAny)
                writer.WriteLine();
        }

        private void EmitProgramGlobalDefinitions(TextWriter writer)
        {
            if (globals.Count == 0)
                return;

            foreach (var global in globals)
                writer.WriteLine($".global {global.Name}={FormatGlobalInitializer(global)}");

            writer.WriteLine();
        }

        private NamedVariable CreateReservedStatusGlobal(string name, int index)
        {
            var global = new NamedVariable(name, VariableKind.Global, index)
            {
                DefaultValue = Zero,
            };

            globals.Insert(index, global);
            ReindexGlobals();
            return global;
        }

        private NamedVariable AddGlobal(string name)
        {
            var result = new NamedVariable(name, VariableKind.Global, globals.Count);
            globals.Add(result);
            ReindexGlobals();
            return result;
        }

        private bool TryGetReservedStatusGlobal(string name, [NotNullWhen(true)] out NamedVariable? reservedGlobal)
        {
            reservedGlobal = name.ToUpperInvariant() switch
            {
                "HERE" => globals[StatusLineHereGlobalIndex],
                "SCORE" => globals[StatusLineScoreGlobalIndex],
                "MOVES" => globals[StatusLineMovesGlobalIndex],
                _ => null,
            };

            return reservedGlobal != null;
        }

        private void ReindexGlobals()
        {
            for (var index = 0; index < globals.Count; index++)
                globals[index].Index = index;
        }

        private void EmitRam(TextWriter writer)
        {
            writer.WriteLine(".ramorg 0x0000");
            foreach (var table in tables)
                table.EmitRam(writer);

            foreach (var word in words)
                word.EmitRam(writer);

            EmitVocabularyRam(writer);

            EmitObjectRam(writer);

            foreach (var definition in ramBytes)
            {
                writer.WriteLine($"{definition.Label}::");
                if (definition.StringValue != null)
                {
                    writer.WriteLine($".string {EscapeString(definition.StringValue)}");
                }
                else
                {
                    EmitByteDirectives(writer, definition.Bytes.Select(FormatByte));
                }
            }

            writer.WriteLine();
        }

        private void EmitVocabularyRam(TextWriter writer)
        {
            if (SelfInsertingBreaks.Any(ch => ch > 0x7F))
                throw new NotSupportedException("Cornerstone tokenizer currently supports ASCII self-inserting breaks only.");

            var sortedWords = words
                .OrderBy(word => NormalizeVocabularyWord(word.Word), StringComparer.Ordinal)
                .ToList();

            writer.WriteLine($"{SelfInsertingBreaksLabel}::");
            EmitByteDirectives(
                writer,
                [
                    FormatByte((byte)SelfInsertingBreaks.Count),
                    .. SelfInsertingBreaks.OrderBy(ch => ch).Select(ch => FormatByte((byte)ch)),
                ]);

            writer.WriteLine($"{VocabularyTableLabel}::");
            if (words.Count == 0)
            {
                EmitWordDirectives(writer, [FormatWord(0)]);
                return;
            }

            EmitWordDirectives(writer, [FormatWord(words.Count)]);

            foreach (var (word, index) in sortedWords.Select((word, index) => (word, index + 1)))
            {
                writer.WriteLine($"__VOCAB_ENTRY_{index:D4}::");

                var normalizedWord = NormalizeVocabularyWord(word.Word);
                var encodedWord = normalizedWord.Select(ch => FormatByte((byte)ch)).ToList();
                while (encodedWord.Count < VocabularyResolution)
                    encodedWord.Add("0x00");

                EmitWordDirectives(writer, [
                    FormatWord(normalizedWord.Length),
                ]);
                EmitByteDirectives(writer, encodedWord);
                EmitWordDirectives(writer, [
                    FormatConstantOperand(word),
                ]);
            }
        }

        private void EmitModules(TextWriter writer)
        {
            var modules = PlanModules();

            foreach (var module in modules)
            {
                writer.WriteLine($".module {module.Name}");
                writer.WriteLine();

                var exportedRoutines = module.ExportAllRoutines
                    ? module.Routines.ToList()
                    : module.Routines.Where(r => r.EntryPoint || r.RequiresFarSelector).ToList();

                for (var i = 0; i < exportedRoutines.Count; i++)
                    writer.WriteLine($".export {exportedRoutines[i].Name}, {i}");

                if (exportedRoutines.Count > 0)
                    writer.WriteLine();

                foreach (var routine in module.Routines)
                {
                    writer.WriteLine($".proc {routine.Name}");
                    foreach (var declaration in routine.GetLocalDeclarationDirectives())
                        writer.WriteLine(declaration);

                    var routineLines = routine.GetLines().ToList();
                    if (!routineLines.Any(line => string.Equals(line, "loc_0000:", StringComparison.Ordinal)))
                        writer.WriteLine("loc_0000:");

                    foreach (var line in routineLines)
                        writer.WriteLine(line);
                    writer.WriteLine(".endproc");
                    writer.WriteLine();
                }
            }

            var entryRoutine = routines.FirstOrDefault(r => r.EntryPoint);
            if (entryRoutine != null)
                writer.WriteLine($".entry {entryRoutine.AssignedModuleName}.{entryRoutine.Name}");
        }

        private void EmitObjData(TextWriter writer)
        {
            if (objData.Count == 0)
                return;

            writer.WriteLine("; OBJ trailer data");
            foreach (var definition in objData)
            {
                if (definition.PackedBytes.Length == 0)
                    writer.WriteLine($".objstring {definition.Label}, {EscapeString(definition.StringValue)}");
                else
                    writer.WriteLine($".objpacked {definition.Label}, {string.Join(", ", definition.PackedBytes.Select(FormatByte))}");
            }

            writer.WriteLine();
        }

        private IReadOnlyList<EmittedModule> PlanModules()
        {
            var maxProceduresPerModule = Math.Clamp(((CornerstoneGameOptions)Options).MaxProceduresPerModule, 1, byte.MaxValue);
            var modules = new List<EmittedModule>();
            EmittedModule? current = null;

            foreach (var routine in routines)
            {
                if (current == null || current.Routines.Count >= maxProceduresPerModule)
                {
                    current = new EmittedModule(GetModuleName(modules.Count));
                    modules.Add(current);
                }

                current.Routines.Add(routine);
            }

            var splitIntoMultipleModules = modules.Count > 1;
            foreach (var module in modules)
            {
                module.ExportAllRoutines = splitIntoMultipleModules;
                foreach (var routine in module.Routines)
                    routine.AssignedModuleName = module.Name;
            }

            return modules;
        }

        private static string GetModuleName(int ordinal) => ordinal == 0 ? BaseModuleName : $"{BaseModuleName}_{ordinal + 1}";

        private static string FormatByte(byte value) => $"0x{value:X2}";

        private static string FormatGlobalInitializer(NamedVariable global)
        {
            if (global.DefaultValue == null)
                return "0x8001";

            return FormatGlobalInitializerOperand(global.DefaultValue, [global]);
        }

        private static string FormatGlobalInitializerOperand(IOperand operand, HashSet<NamedVariable> seenGlobals)
        {
            operand = operand.StripIndirect();

            if (operand is NamedVariable variable)
            {
                if (variable.Kind != VariableKind.Global)
                    throw new NotSupportedException($"Cornerstone global initializers do not support operand type {operand.GetType().Name}.");

                if (!seenGlobals.Add(variable))
                    throw new NotSupportedException($"Cornerstone global initializers contain a cycle involving {variable.Name}.");

                try
                {
                    if (variable.DefaultValue == null)
                        return "0x8001";

                    return FormatGlobalInitializerOperand(variable.DefaultValue, seenGlobals);
                }
                finally
                {
                    seenGlobals.Remove(variable);
                }
            }

            if (operand is CompositeOperand composite)
                return $"({FormatGlobalInitializerOperand(composite.Left, seenGlobals)} + {FormatGlobalInitializerOperand(composite.Right, seenGlobals)})";

            return FormatConstantOperand(operand);
        }

        private static string FormatConstantOperand(IOperand operand)
        {
            return operand switch
            {
                NumericOperand numeric => FormatWord(numeric.Value),
                NamedConstant constant => constant.Name,
                SymbolicOperand symbolic => symbolic.Name,
                NamedVariable variable => FormatWord(GetVariableSelector(variable)),
                WordBuilder word => $"byte:{word.Name}",
                TableBuilder table => $"byte:{table.Name}",
                PropertyBuilder property => $"P?{property.Name}",
                FlagBuilder flag => flag.Name,
                ObjectBuilder obj => obj.Name,
                RoutineBuilder routine => MarkRoutineRequiresFarSelector(routine),
                CompositeOperand composite => $"({FormatConstantOperand(composite.Left)} + {FormatConstantOperand(composite.Right)})",
                _ => throw new NotSupportedException($"Cornerstone global initializers do not support operand type {operand.GetType().Name}."),
            };
        }

        private static int GetVariableSelector(NamedVariable variable)
        {
            return variable.Kind switch
            {
                VariableKind.Stack => 0,
                VariableKind.Local => variable.Index + 1,
                VariableKind.Global => variable.Index + 0x10,
                _ => throw new NotSupportedException($"Cornerstone global initializers do not support operand type {variable.GetType().Name}."),
            };
        }

        private static string MarkRoutineRequiresFarSelector(RoutineBuilder routine)
        {
            routine.MarkRequiresFarSelector();
            return routine.Name;
        }

        private static void MarkFarSelectorDependencies(IOperand operand)
        {
            operand = operand.StripIndirect();

            switch (operand)
            {
                case RoutineBuilder routine:
                    routine.MarkRequiresFarSelector();
                    break;

                case CompositeOperand composite:
                    MarkFarSelectorDependencies(composite.Left);
                    MarkFarSelectorDependencies(composite.Right);
                    break;
            }
        }

        internal static string GetObjectFlagsLabel(int objectId) => $"__OBJECT_FLAGS_{objectId:X4}";

        internal static string GetObjectPropertiesLabel(int objectId) => $"__OBJECT_PROPERTIES_{objectId:X4}";

        private void EmitObjectRam(TextWriter writer)
        {
            if (properties.Count > 0)
            {
                writer.WriteLine($"{PropertyDefaultsLabel}::");
                var defaultsByPropertyId = properties.ToDictionary(
                    property => property.Id,
                    property => FormatConstantOperand(property.DefaultValue ?? Zero));
                var propertyDefaults = Enumerable.Range(1, MaxPropertyId)
                    .Select(propertyId => defaultsByPropertyId.TryGetValue(propertyId, out var value)
                        ? value
                        : FormatWord(0));
                EmitWordDirectives(writer, propertyDefaults);
            }

            foreach (var obj in objects.OrderBy(obj => obj.Id))
            {
                writer.WriteLine($"{GetObjectFlagsLabel(obj.Id)}::");
                var flagWords = new List<string>(Math.Max(flags.Count, 1));
                if (flags.Count == 0)
                {
                    flagWords.Add(FormatWord(0));
                }
                else
                {
                    foreach (var flag in flags.OrderBy(flag => flag.Id))
                        flagWords.Add(FormatWord(obj.SetFlags.Contains(flag) ? 1 : 0));
                }

                EmitWordDirectives(writer, flagWords);

                writer.WriteLine($"{GetObjectPropertiesLabel(obj.Id)}::");
                var objectProperties = GetObjectPropertyEntries(obj).ToList();
                EmitWordDirectives(writer, [FormatWord(objectProperties.Count)]);
                foreach (var entry in objectProperties)
                {
                    EmitWordDirectives(writer, [FormatConstantOperand(entry.Property), FormatWord(entry.ByteLength)]);

                    if (entry.Values.Count > 0)
                    {
                        for (var index = 0; index < entry.Values.Count;)
                        {
                            bool emitBytes = entry.Values[index].IsByte;
                            var chunk = new List<TableEntry>();
                            while (index < entry.Values.Count && entry.Values[index].IsByte == emitBytes)
                            {
                                chunk.Add(entry.Values[index]);
                                index++;
                            }

                            if (emitBytes)
                                EmitByteDirectives(writer, chunk.Select(TableBuilder.FormatByteOperand));
                            else
                                EmitWordDirectives(writer, chunk.Select(item => FormatConstantOperand(item.Operand)));
                        }

                        if ((entry.ByteLength & 1) != 0)
                            EmitByteDirectives(writer, ["0x00"]);
                    }
                }
            }

            if (objects.Count <= 0)
                return;

            writer.WriteLine($"{ObjectRecordsLabel}::");
            var recordWords = new List<string>();
            foreach (var obj in objects.OrderBy(obj => obj.Id))
            {
                recordWords.Add(obj.Parent is ObjectBuilder parent ? FormatConstantOperand(parent) : FormatWord(0));
                recordWords.Add(obj.Sibling is ObjectBuilder sibling ? FormatConstantOperand(sibling) : FormatWord(0));
                recordWords.Add(obj.Child is ObjectBuilder child ? FormatConstantOperand(child) : FormatWord(0));
                recordWords.Add(GetObjectFlagsLabel(obj.Id));
                recordWords.Add(GetObjectPropertiesLabel(obj.Id));
            }

            EmitWordDirectives(writer, recordWords);

            writer.WriteLine($"{ObjectNamesLabel}::");
            EmitWordDirectives(writer, objects.OrderBy(obj => obj.Id)
                .SelectMany(obj =>
                {
                    if (!objDataStrings.TryGetValue(obj.DescriptiveName, out var nameRef))
                        throw new InvalidOperationException($"Object name '{obj.DescriptiveName}' was not registered before RAM emission.");

                    return new[] { FormatConstantOperand(nameRef.Location) };
                }));
        }

        private IEnumerable<(PropertyBuilder Property, IReadOnlyList<TableEntry> Values, int ByteLength)> GetObjectPropertyEntries(ObjectBuilder obj)
        {
            foreach (var pair in obj.PropertyValues.OrderByDescending(pair => ((PropertyBuilder)pair.Key).Id))
            {
                var values = pair.Value.Select(value => new TableEntry(value, IsByte: false)).ToList();
                yield return ((PropertyBuilder)pair.Key, values, values.Count * 2);
            }

            foreach (var pair in obj.ComplexPropertyTables.OrderByDescending(pair => ((PropertyBuilder)pair.Key).Id))
            {
                var values = pair.Value.Contents.ToList();
                var byteLength = values.Sum(entry => entry.IsByte ? 1 : 2);
                yield return ((PropertyBuilder)pair.Key, values, byteLength);
            }
        }

        private static string FormatWord(int value)
        {
            ushort raw = unchecked((ushort)value);
            return $"0x{raw:X4}";
        }

        private static string FormatLocalInitializerOperand(IOperand operand) => operand switch
        {
            NumericOperand numeric => FormatWord(numeric.Value),
            NamedVariable variable => FormatWord(GetVariableSelector(variable)),
            NamedConstant constant => constant.Name,
            PropertyBuilder property => $"P?{property.Name}",
            FlagBuilder flag => flag.Name,
            ObjectBuilder obj => obj.Name,
            TableBuilder table => table.Name,
            _ => throw new NotSupportedException($"Cornerstone local initializers do not support operand type {operand.GetType().Name}."),
        };

        private static string EscapeString(string value)
        {
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (var ch in value)
            {
                builder.Append(ch switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ => ch.ToString(),
                });
            }

            builder.Append('"');
            return builder.ToString();
        }

        private static string NormalizeVocabularyWord(string word)
        {
            EnsureAscii(word);
            return word.ToLowerInvariant()[..Math.Min(word.Length, VocabularyResolution)];
        }

        private static byte[] EncodeVocabularyWord(string word)
        {
            var normalizedWord = NormalizeVocabularyWord(word);
            var encoder = new StringEncoder();
            return encoder.Encode(normalizedWord, VocabularyResolution, StringEncoderMode.NoAbbreviations);
        }

        private static void EmitByteDirectives(TextWriter writer, IEnumerable<string> values) =>
            EmitChunkedDirective(writer, ".byte", values, 16);

        private static void EmitWordDirectives(TextWriter writer, IEnumerable<string> values) =>
            EmitChunkedDirective(writer, ".word", values, 12);

        private static void EmitChunkedDirective(TextWriter writer, string directive, IEnumerable<string> values, int chunkSize)
        {
            var tokens = values.ToList();
            for (var index = 0; index < tokens.Count; index += chunkSize)
                writer.WriteLine($"{directive} {string.Join(", ", tokens.Skip(index).Take(chunkSize))}");
        }

        private static void EnsureAscii(string value)
        {
            if (value.Any(ch => ch > 0x7F))
                throw new NotSupportedException("Cornerstone string emission currently supports ASCII only.");
        }

        private void AccumulatePackedTextHistogram(string value)
        {
            foreach (var ch in value)
            {
                packedTextHistogram.TryGetValue(ch, out var count);
                packedTextHistogram[ch] = count + 1;
            }
        }

        private void PrepareMetadata()
        {
            ramBytes.Add(new RamBytesDefinition(MetadataCreatorLabel, [], Metadata.GetCreatorString()));

            var releaseRam = new TableBuilder(MetadataReleaseIdLabel, pure: false);
            releaseRam.AddWord((IOperand?)(FindConstant("RELEASEID") ?? FindConstant("ZORKID")) ?? Zero);
            tables.Add(releaseRam);

            ramBytes.Add(new RamBytesDefinition(MetadataSerialLabel, [], DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture)));
        }

        private void PreparePackedText()
        {
            if (packedTextPrepared)
                return;

            foreach (var obj in objects)
            {
                if (!string.IsNullOrEmpty(obj.DescriptiveName))
                    RegisterObjString(obj.DescriptiveName);
            }

            packedTextPrepared = true;
            if (objDataStrings.Count == 0)
                return;

            packedTextEncoding = PackedTextEncoding.Create(packedTextHistogram);

            ramBytes.Add(new RamBytesDefinition(PackedTextAlphabet0Label, packedTextEncoding.Alphabet0, null));
            ramBytes.Add(new RamBytesDefinition(PackedTextAlphabet1Label, packedTextEncoding.Alphabet1, null));
            ramBytes.Add(new RamBytesDefinition(PackedTextAlphabet2Label, packedTextEncoding.Alphabet2, null));

            for (var index = 0; index < objData.Count; index++)
            {
                var definition = objData[index];
                objData[index] = definition with { PackedBytes = packedTextEncoding.Encode(definition.StringValue) };
            }
        }

        private readonly record struct RamBytesDefinition(string Label, byte[] Bytes, string? StringValue);
        private readonly record struct ObjDataDefinition(string Label, string StringValue, byte[] PackedBytes);
        internal sealed class ObjDataReference(SymbolicOperand location)
        {
            public SymbolicOperand Location { get; } = location;
        }

        internal enum VariableKind
        {
            Global,
            Local,
            Stack,
        }
    }
}
