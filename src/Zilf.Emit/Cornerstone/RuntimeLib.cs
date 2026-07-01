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
using System.Linq;

namespace Zilf.Emit.Cornerstone
{
    public sealed class RuntimeLib
    {
        public const string ConsoleAdvanceLine = nameof(ConsoleAdvanceLine);
        public const string FlushOutputBuffer = nameof(FlushOutputBuffer);
        public const string BufferedPrintCharacter = nameof(BufferedPrintCharacter);
        public const string PrintBufferedVector = nameof(PrintBufferedVector);
        public const string PrintNumber = nameof(PrintNumber);
        public const string PrintObjDataString = nameof(PrintObjDataString);
        public const string PrintObject = nameof(PrintObject);
        public const string PrintVocabularyWord = nameof(PrintVocabularyWord);
        public const string GetScreenHeight = nameof(GetScreenHeight);
        public const string GetScreenWidth = nameof(GetScreenWidth);
        public const string GetLowCoreFlags = nameof(GetLowCoreFlags);
        public const string SetLowCoreFlags = nameof(SetLowCoreFlags);
        public const string GetHeaderByte = nameof(GetHeaderByte);
        public const string GetHeaderWord = nameof(GetHeaderWord);
        public const string PutHeaderByte = nameof(PutHeaderByte);
        public const string PutHeaderWord = nameof(PutHeaderWord);
        public const string LoadWordAtBytePointer = nameof(LoadWordAtBytePointer);
        public const string LoadByteAtBytePointer = nameof(LoadByteAtBytePointer);
        public const string StoreWordAtBytePointer = nameof(StoreWordAtBytePointer);
        public const string StoreByteAtBytePointer = nameof(StoreByteAtBytePointer);
        public const string Random = nameof(Random);
        public const string PrintStatusNumber = nameof(PrintStatusNumber);
        public const string PrintStatusString = nameof(PrintStatusString);
        public const string PrintStatusObject = nameof(PrintStatusObject);
        public const string DrawStatusLine = nameof(DrawStatusLine);
        public const string ReadLine = nameof(ReadLine);
        public const string TokenizeLine = nameof(TokenizeLine);
        public const string GetChild = nameof(GetChild);
        public const string GetSibling = nameof(GetSibling);
        public const string GetParent = nameof(GetParent);
        public const string TestAttribute = nameof(TestAttribute);
        public const string GetPropertyAddress = nameof(GetPropertyAddress);
        public const string GetNextProperty = nameof(GetNextProperty);
        public const string GetProperty = nameof(GetProperty);
        public const string GetPropertySize = nameof(GetPropertySize);
        public const string LoadIndirectGlobal = nameof(LoadIndirectGlobal);
        public const string SetFlag = nameof(SetFlag);
        public const string ClearFlag = nameof(ClearFlag);
        public const string PutProperty = nameof(PutProperty);
        public const string RemoveObject = nameof(RemoveObject);
        public const string MoveObject = nameof(MoveObject);
        public const string DirectInput = nameof(DirectInput);
        public const string DirectOutput = nameof(DirectOutput);
        public const string SetOutputStyle = nameof(SetOutputStyle);
        public const string TryReadCommandFileLine = nameof(TryReadCommandFileLine);
        public const string EchoReadLineToCommandFile = nameof(EchoReadLineToCommandFile);
        public const string Stream3WriteChar = nameof(Stream3WriteChar);
        private const string PrintPackedObjDataCore = nameof(PrintPackedObjDataCore);

        private readonly Dictionary<string, RuntimeRoutineDefinition> routines = new(StringComparer.Ordinal);
        private readonly Dictionary<string, GameBuilder.RoutineBuilder> usedRoutines = new(StringComparer.Ordinal);
        private GameBuilder.NamedVariable? randomState;

        private GameBuilder? gameBuilder;

        public RuntimeLib()
        {
            routines.Add(
                ConsoleAdvanceLine,
                new RuntimeRoutineDefinition(
                    "__ConsoleAdvanceLine",
                    [],
                    static (builder, routine) =>
                    {
                        // Default uninitialized screen height to 24-row text mode (0x17 max row).
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenHeightSlot:X2}");
                        routine.EmitRawLine("    JUMPNZ have_screen_height");
                        routine.EmitRawLine("    PUSHB 0x17");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.ScreenHeightSlot:X2}");
                        routine.EmitRawLine("have_screen_height:");

                        // Keep output pinned to the bottom line and always scroll upward on line advance.
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenHeightSlot:X2}");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.ScrollBottomSlot:X2}");
                        routine.EmitRawLine($"    PUSHB 0x{GameBuilder.MainTextTopRow:X2}");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.ScrollTopSlot:X2}");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    XDISP 0x04");
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenHeightSlot:X2}");
                        routine.EmitRawLine($"    STOREG {builder.ConsoleRowGlobalIndex}");
                        routine.EmitRawLine($"    LOADG {builder.ConsoleRowGlobalIndex}");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    STOREG {builder.ConsoleColumnGlobalIndex}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                FlushOutputBuffer,
                new RuntimeRoutineDefinition(
                    "__FlushOutputBuffer",
                    [],
                    static (builder, routine) =>
                    {
                        var bufferName = builder.EnsureOutputBufferName();
                        var descriptorName = builder.EnsureOutputWindowDescriptorName();

                        routine.DefineLocal("length");
                        routine.DefineLocal("index");
                        routine.DefineLocal("chunkCount");

                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPZ flush_done");
                        EmitPrepareOutputWindowDescriptor(builder, routine, descriptorName);
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("flush_chunk:");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPEQ flush_emit_done");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHB 0x64");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPLE use_remaining_chunk");
                        routine.EmitRawLine("    PUSHB 0x64");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("use_remaining_chunk:");
                        routine.EmitRawLine($"    PUSHW {descriptorName}");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    WPRINTV");
                        routine.EmitRawLine("    POP");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    JUMP flush_chunk");
                        routine.EmitRawLine("flush_emit_done:");
                        EmitSyncConsoleColumnFromDescriptor(builder, routine, descriptorName);
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("flush_done:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                Stream3WriteChar,
                new RuntimeRoutineDefinition(
                    "__Stream3WriteChar",
                    [LoadWordAtBytePointer, StoreWordAtBytePointer, StoreByteAtBytePointer],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("character");
                        routine.DefineLocal("tableAddress");
                        routine.DefineLocal("count");

                        // Safety: if no stream 3 table is active, return.
                        routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
                        routine.EmitRawLine("    JUMPF no_stream3_table");
                        routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
                        routine.EmitRawLine("    PUTL 1");

                        // Convert newlines (0x0A LF, 0x0D CR) to byte 13.
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x0A");
                        routine.EmitRawLine("    JUMPEQ write_newline_byte");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x0D");
                        routine.EmitRawLine("    JUMPEQ write_newline_byte");
                        routine.EmitRawLine("    JUMP write_byte_to_table");
                        routine.EmitRawLine("write_newline_byte:");
                        routine.EmitRawLine("    PUSHB 0x0D");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine("write_byte_to_table:");

                        // Read count from table[0] (word at byte offset 0).
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadWordAtBytePointer)}");
                        routine.EmitRawLine("    PUTL 2");

                        // table[2 + count] = character (byte at byte offset 2 + count).
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    CALL3 {builder.RuntimeLib.Use(StoreByteAtBytePointer)}");

                        // table[0] = count + 1 (word at byte offset 0).
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine($"    CALL3 {builder.RuntimeLib.Use(StoreWordAtBytePointer)}");

                        routine.EmitRawLine("no_stream3_table:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                BufferedPrintCharacter,
                new RuntimeRoutineDefinition(
                    "__BufferedPrintCharacter",
                    [ConsoleAdvanceLine, FlushOutputBuffer, Stream3WriteChar],
                    static (builder, routine) =>
                    {
                        var bufferName = builder.EnsureOutputBufferName();
                        var descriptorName = builder.EnsureOutputWindowDescriptorName();

                        routine.DefineRequiredParameter("character");
                        routine.DefineLocal("length");
                        routine.DefineLocal("available");
                        routine.DefineLocal("index");
                        routine.DefineLocal("spaceIndex");
                        routine.DefineLocal("prefixCount");
                        routine.DefineLocal("sourceIndex");
                        routine.DefineLocal("targetIndex");
                        routine.DefineLocal("currentColumn");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ have_character");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("have_character:");
                        routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
                        routine.EmitRawLine("    JUMPF normal_buffered_output");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(Stream3WriteChar)}");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("normal_buffered_output:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x0A");
                        routine.EmitRawLine("    JUMPEQ emit_newline");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x0D");
                        routine.EmitRawLine("    JUMPEQ emit_newline");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    VPUTB");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("wrap_loop:");
                        routine.EmitRawLine($"    LOADG {builder.ConsoleColumnGlobalIndex}");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPLE append_done");
                        routine.EmitRawLine("    PUSHW 0xFFFF");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("search_space:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPZ no_space");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    VLOADB");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    JUMPEQ found_space");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP search_space");
                        routine.EmitRawLine("found_space:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("no_space:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSHW 0xFFFF");
                        routine.EmitRawLine("    JUMPNE wrap_at_space");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    JUMPZ hard_break_word");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
                        routine.EmitRawLine("    JUMP wrap_loop");
                        routine.EmitRawLine("hard_break_word:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUTL 6");
                        routine.EmitRawLine("    JUMP flush_prefix");
                        routine.EmitRawLine("wrap_at_space:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 6");
                        routine.EmitRawLine("flush_prefix:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    JUMPZ skip_prefix_emit");
                        EmitPrepareOutputWindowDescriptor(builder, routine, descriptorName);
                        routine.EmitRawLine($"    PUSHW {descriptorName}");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    WPRINTV");
                        routine.EmitRawLine("    POP");
                        routine.EmitRawLine("skip_prefix_emit:");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("skip_leading_spaces:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPEQ shift_done");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    VLOADB");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    JUMPNE shift_copy");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    JUMP skip_leading_spaces");
                        routine.EmitRawLine("shift_copy:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPEQ shift_done");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    VLOADB");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    VPUTB");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP shift_copy");
                        routine.EmitRawLine("shift_done:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine($"    PUSHW {bufferName}");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    JUMP wrap_loop");
                        routine.EmitRawLine("emit_newline:");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(FlushOutputBuffer)}");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
                        routine.EmitRawLine("append_done:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PrintBufferedVector,
                new RuntimeRoutineDefinition(
                    "__PrintBufferedVector",
                    [BufferedPrintCharacter],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("handle");
                        routine.DefineLocal("length");
                        routine.DefineLocal("index");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ have_handle");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("have_handle:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("vector_loop:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPEQ vector_done");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHB 0x03");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    VLOADB");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(BufferedPrintCharacter)}");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    JUMP vector_loop");
                        routine.EmitRawLine("vector_done:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PrintNumber,
                new RuntimeRoutineDefinition(
                    "__PrintNumber",
                    [BufferedPrintCharacter],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("value");
                        routine.DefineLocal("magnitude");
                        routine.DefineLocal("digitCount");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPGEZ print_nonnegative");
                        routine.EmitRawLine("    PUSHB 0x2D");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(BufferedPrintCharacter)}");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    NEG");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    JUMP print_prepare");
                        routine.EmitRawLine("print_nonnegative:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("print_prepare:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ extract_digits");
                        routine.EmitRawLine("    PUSHB 0x30");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(BufferedPrintCharacter)}");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("extract_digits:");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHB 0x0A");
                        routine.EmitRawLine("    MOD");
                        routine.EmitRawLine("    PUSHB 0x30");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHB 0x0A");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ extract_digits");
                        routine.EmitRawLine("print_digits:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPZ print_done");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(BufferedPrintCharacter)}");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    JUMP print_digits");
                        routine.EmitRawLine("print_done:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PrintStatusNumber,
                new RuntimeRoutineDefinition(
                    "__PrintStatusNumber",
                    [],
                    static (_, routine) =>
                    {
                        routine.DefineRequiredParameter("value");
                        routine.DefineLocal("magnitude");
                        routine.DefineLocal("digitCount");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPGEZ print_nonnegative");
                        routine.EmitRawLine("    PUSHB 0x2D");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    NEG");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    JUMP print_prepare");
                        routine.EmitRawLine("print_nonnegative:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("print_prepare:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ extract_digits");
                        routine.EmitRawLine("    PUSHB 0x30");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("extract_digits:");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHB 0x0A");
                        routine.EmitRawLine("    MOD");
                        routine.EmitRawLine("    PUSHB 0x30");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHB 0x0A");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ extract_digits");
                        routine.EmitRawLine("print_digits:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPZ print_done");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    JUMP print_digits");
                        routine.EmitRawLine("print_done:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PrintPackedObjDataCore,
                new RuntimeRoutineDefinition(
                    "__PrintPackedObjDataCore",
                    [BufferedPrintCharacter],
                    static (builder, routine) => EmitPackedStringRoutine(builder, routine),
                    ReturnsValue: false));

            routines.Add(
                PrintObjDataString,
                new RuntimeRoutineDefinition(
                    "__PrintObjDataString",
                    [PrintPackedObjDataCore],
                    static (builder, routine) => EmitPackedStringWrapper(builder, routine, directOutput: false),
                    ReturnsValue: false));

            routines.Add(
                PrintStatusString,
                new RuntimeRoutineDefinition(
                    "__PrintStatusString",
                    [PrintPackedObjDataCore],
                    static (builder, routine) => EmitPackedStringWrapper(builder, routine, directOutput: true),
                    ReturnsValue: false));

            routines.Add(
                PrintObject,
                new RuntimeRoutineDefinition(
                    "__PrintObject",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineLocal("zeroBasedIndex");
                        routine.DefineLocal("offsetIndex");
                        routine.DefineLocal("stringId");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ have_object_id");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("have_object_id:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.ObjectNamesLabel}");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    VLOADW");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PrintStatusObject,
                new RuntimeRoutineDefinition(
                    "__PrintStatusObject",
                    [PrintStatusString],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineLocal("zeroBasedIndex");
                        routine.DefineLocal("offsetIndex");
                        routine.DefineLocal("stringId");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ have_object_id");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("have_object_id:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.ObjectNamesLabel}");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    VLOADW");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine($"    PUSHL 3");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusString)}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                DrawStatusLine,
                new RuntimeRoutineDefinition(
                    "__DrawStatusLine",
                    [FlushOutputBuffer, PrintStatusObject, PrintStatusString, PrintStatusNumber],
                    static (builder, routine) =>
                    {
                        var scoreLabel = builder.RegisterObjString("Score: ");
                        var movesLabel = builder.RegisterObjString("Moves: ");
                        var useTimeStatus = builder.Options is CornerstoneGameOptions { TimeStatusLine: true };

                        routine.DefineLocal("savedRow");
                        routine.DefineLocal("savedColumn");
                        routine.DefineLocal("savedAttribute");
                        routine.DefineLocal("remaining");
                        routine.DefineLocal("here");
                        routine.DefineLocal("score");
                        routine.DefineLocal("moves");

                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(FlushOutputBuffer)}");
                        routine.EmitRawLine($"    LOADG {builder.ConsoleRowGlobalIndex}");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine($"    LOADG {builder.ConsoleColumnGlobalIndex}");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.TextAttributeSlot:X2}");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine($"    LOADG {GameBuilder.StatusLineHereGlobalIndex}");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine($"    LOADG {GameBuilder.StatusLineScoreGlobalIndex}");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine($"    LOADG {GameBuilder.StatusLineMovesGlobalIndex}");
                        routine.EmitRawLine("    PUTL 6");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    OR");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.TextAttributeSlot:X2}");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("fill_status_line:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPZ status_line_filled");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP fill_status_line");
                        routine.EmitRawLine("status_line_filled:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine($"    PUSHL 4");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusObject)}");

                        if (useTimeStatus)
                        {
                            routine.EmitRawLine("    PUSH0");
                            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                            routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
                            routine.EmitRawLine("    PUSH8");
                            routine.EmitRawLine("    SUB");
                            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                            routine.EmitRawLine("    PUSHB 0x0A");
                            routine.EmitRawLine("    PUSHL 5");
                            routine.EmitRawLine("    JUMPGE print_status_hours");
                            routine.EmitRawLine("    PUSHB 0x30");
                            routine.EmitRawLine("    PRCHAR");
                            routine.EmitRawLine("print_status_hours:");
                            routine.EmitRawLine($"    PUSHL 5");
                            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusNumber)}");
                            routine.EmitRawLine("    PUSHB 0x3A");
                            routine.EmitRawLine("    PRCHAR");
                            routine.EmitRawLine("    PUSHB 0x0A");
                            routine.EmitRawLine("    PUSHL 6");
                            routine.EmitRawLine("    JUMPGE print_status_minutes");
                            routine.EmitRawLine("    PUSHB 0x30");
                            routine.EmitRawLine("    PRCHAR");
                            routine.EmitRawLine("print_status_minutes:");
                            routine.EmitRawLine($"    PUSHL 6");
                            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusNumber)}");
                        }
                        else
                        {
                            routine.EmitRawLine("    PUSH0");
                            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                            routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
                            routine.EmitRawLine("    PUSH 22");
                            routine.EmitRawLine("    SUB");
                            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                            routine.EmitRawLine($"    PUSHW {scoreLabel.Location.Name}");
                            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusString)}");
                            routine.EmitRawLine($"    PUSHL 5");
                            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusNumber)}");
                            routine.EmitRawLine("    PUSH0");
                            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                            routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
                            routine.EmitRawLine("    PUSH 10");
                            routine.EmitRawLine("    SUB");
                            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                            routine.EmitRawLine($"    PUSHW {movesLabel.Location.Name}");
                            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusString)}");
                            routine.EmitRawLine($"    PUSHL 6");
                            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintStatusNumber)}");
                        }

                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.TextAttributeSlot:X2}");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                SetOutputStyle,
                new RuntimeRoutineDefinition(
                    "__SetOutputStyle",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("style");

                        // Clear bits 0 (reverse) and 3 (bold) from the current text attribute (0xD5).
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.TextAttributeSlot:X2}");
                        routine.EmitRawLine("    PUSH 0xFFF6");  // ~0x09: clears bits 0 and 3
                        routine.EmitRawLine("    AND");

                        // Map style bit 0 (reverse video) → D5 bit 0.
                        routine.EmitRawLine("    PUSHL 0");      // style
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    AND");          // style & 1
                        routine.EmitRawLine("    OR");           // attr | reverse_bit

                        // Map style bits 1 and 2 (bold, italic) → D5 bit 3 (bright/bold).
                        routine.EmitRawLine("    PUSHL 0");      // style
                        routine.EmitRawLine("    PUSH 6");       // mask for bits 1 and 2
                        routine.EmitRawLine("    AND");          // style & 6
                        routine.EmitRawLine("    JUMPZ set_output_style_done");
                        routine.EmitRawLine("    PUSH8");        // D5 bit 3
                        routine.EmitRawLine("    OR");
                        routine.EmitRawLine("set_output_style_done:");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.TextAttributeSlot:X2}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PrintVocabularyWord,
                new RuntimeRoutineDefinition(
                    "__PrintVocabularyWord",
                    [LoadWordAtBytePointer],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("byteAddress");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ have_vocab_address");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("have_vocab_address:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    PUSHB 0x{GameBuilder.VocabularyStringIdWordOffset:X2}");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadWordAtBytePointer)}");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                GetScreenHeight,
                new RuntimeRoutineDefinition(
                    "__GetScreenHeight",
                    [],
                    static (_, routine) =>
                    {
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenHeightSlot:X2}");
                        routine.EmitRawLine("    JUMPNZ have_screen_height");
                        routine.EmitRawLine("    PUSHB 0x18");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("have_screen_height:");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetScreenWidth,
                new RuntimeRoutineDefinition(
                    "__GetScreenWidth",
                    [],
                    static (_, routine) =>
                    {
                        routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
                        routine.EmitRawLine("    JUMPNZ have_screen_width");
                        routine.EmitRawLine("    PUSHB 0x50");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("have_screen_width:");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetLowCoreFlags,
                new RuntimeRoutineDefinition(
                    "__GetLowCoreFlags",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineLocal("result");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
                        routine.EmitRawLine("    JUMPF no_transcript_stream");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine("no_transcript_stream:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                SetLowCoreFlags,
                new RuntimeRoutineDefinition(
                    "__SetLowCoreFlags",
                    [DirectOutput],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("value");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine("    JUMPNZ want_transcript");
                        routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
                        routine.EmitRawLine("    JUMPF flags_done");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(DirectOutput)}");
                        routine.EmitRawLine("    JUMP flags_done");
                        routine.EmitRawLine("want_transcript:");
                        routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
                        routine.EmitRawLine("    JUMPNZ flags_done");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSHW 0x0004");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(DirectOutput)}");
                        routine.EmitRawLine("flags_done:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                GetHeaderByte,
                new RuntimeRoutineDefinition(
                    "__GetHeaderByte",
                    [GetLowCoreFlags, GetScreenHeight, GetScreenWidth],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("address");

                        const int serialLength = 6;
                        for (var index = 0; index < serialLength; index++)
                        {
                            string nextLabel = index == serialLength - 1
                                ? "not_serial_byte"
                                : $"not_serial_byte_{index:X2}";

                            routine.EmitRawLine("    PUSHL 0");
                            routine.EmitRawLine($"    PUSHB 0x{0x12 + index:X2}");
                            routine.EmitRawLine($"    JUMPNE {nextLabel}");
                            routine.EmitRawLine($"    PUSHW {GameBuilder.MetadataSerialLabel}");
                            routine.EmitRawLine($"    PUSHB 0x{index + 1:X2}");
                            routine.EmitRawLine("    VLOADB");
                            routine.EmitRawLine("    RETURN");

                            if (index < serialLength - 1)
                                routine.EmitRawLine($"{nextLabel}:");
                        }

                        routine.EmitRawLine("not_serial_byte:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ not_zversion_high");
                        routine.EmitRawLine($"    PUSHW 0x{builder.EmulatedZMachineVersion:X4}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_zversion_high:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    JUMPNE not_zversion_low");
                        routine.EmitRawLine($"    PUSHW 0x{builder.EmulatedZVersionFlags:X4}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_zversion_low:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x02");
                        routine.EmitRawLine("    JUMPNE not_release_high");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.MetadataReleaseIdLabel}");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUSHm8");
                        routine.EmitRawLine("    SHIFT");
                        routine.EmitRawLine("    PUSH 0x00FF");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_release_high:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x03");
                        routine.EmitRawLine("    JUMPNE not_release_low");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.MetadataReleaseIdLabel}");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUSH 0x00FF");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_release_low:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x10");
                        routine.EmitRawLine("    JUMPNE not_flags_high");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_flags_high:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x11");
                        routine.EmitRawLine("    JUMPNE not_flags_low");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(GetLowCoreFlags)}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_flags_low:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    JUMPNE not_screen_height");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(GetScreenHeight)}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_screen_height:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x21");
                        routine.EmitRawLine("    JUMPNE not_screen_width");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(GetScreenWidth)}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_screen_width:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x32");
                        routine.EmitRawLine("    JUMPNE not_stdrev_high");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_stdrev_high:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x33");
                        routine.EmitRawLine("    JUMPNE unknown_header_byte");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("unknown_header_byte:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetHeaderWord,
                new RuntimeRoutineDefinition(
                    "__GetHeaderWord",
                    [GetLowCoreFlags],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("address");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    JUMPNE not_zversion_word");
                        routine.EmitRawLine($"    PUSHW 0x{(builder.EmulatedZMachineVersion << 8) | builder.EmulatedZVersionFlags:X4}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_zversion_word:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x02");
                        routine.EmitRawLine("    JUMPNE not_release_word");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.MetadataReleaseIdLabel}");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_release_word:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x10");
                        routine.EmitRawLine("    JUMPNE not_flags_word");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(GetLowCoreFlags)}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("not_flags_word:");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x32");
                        routine.EmitRawLine("    JUMPNE unknown_header_word");
                        routine.EmitRawLine("    PUSH 0x0101");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("unknown_header_word:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                PutHeaderByte,
                new RuntimeRoutineDefinition(
                    "__PutHeaderByte",
                    [SetLowCoreFlags],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("address");
                        routine.DefineRequiredParameter("value");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x11");
                        routine.EmitRawLine("    JUMPNE ignore_header_byte_write");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(SetLowCoreFlags)}");
                        routine.EmitRawLine("ignore_header_byte_write:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                PutHeaderWord,
                new RuntimeRoutineDefinition(
                    "__PutHeaderWord",
                    [SetLowCoreFlags],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("address");
                        routine.DefineRequiredParameter("value");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHB 0x10");
                        routine.EmitRawLine("    JUMPNE ignore_header_word_write");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(SetLowCoreFlags)}");
                        routine.EmitRawLine("ignore_header_word_write:");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                LoadByteAtBytePointer,
                new RuntimeRoutineDefinition(
                    "__LoadByteAtBytePointer",
                    [GetHeaderByte],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("byteAddress");
                        routine.DefineRequiredParameter("byteOffset");
                        routine.DefineLocal("absoluteByteAddress");
                        routine.DefineLocal("handle");
                        routine.DefineLocal("rawIndex");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ load_regular_byte");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(GetHeaderByte)}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("load_regular_byte:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHm1");
                        routine.EmitRawLine("    SHIFT");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                StoreByteAtBytePointer,
                new RuntimeRoutineDefinition(
                    "__StoreByteAtBytePointer",
                    [PutHeaderByte],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("byteAddress");
                        routine.DefineRequiredParameter("byteOffset");
                        routine.DefineRequiredParameter("value");
                        routine.DefineLocal("absoluteByteAddress");
                        routine.DefineLocal("handle");
                        routine.DefineLocal("rawIndex");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ store_regular_byte");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(PutHeaderByte)}");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("store_regular_byte:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHm1");
                        routine.EmitRawLine("    SHIFT");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUTVB2");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                LoadWordAtBytePointer,
                new RuntimeRoutineDefinition(
                    "__LoadWordAtBytePointer",
                    [LoadByteAtBytePointer, GetHeaderWord],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("byteAddress");
                        routine.DefineRequiredParameter("wordOffset");
                        routine.DefineLocal("absoluteByteAddress");
                        routine.DefineLocal("lowByte");
                        routine.DefineLocal("highByte");

                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ load_regular_word");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(GetHeaderWord)}");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("load_regular_word:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadByteAtBytePointer)}");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadByteAtBytePointer)}");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH 0x0100");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                StoreWordAtBytePointer,
                new RuntimeRoutineDefinition(
                    "__StoreWordAtBytePointer",
                    [StoreByteAtBytePointer, PutHeaderWord],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("byteAddress");
                        routine.DefineRequiredParameter("wordOffset");
                        routine.DefineRequiredParameter("value");
                        routine.DefineLocal("absoluteByteAddress");
                        routine.DefineLocal("lowByte");
                        routine.DefineLocal("highByte");

                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ store_regular_word");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(PutHeaderWord)}");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("store_regular_word:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH 0x00FF");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHm8");
                        routine.EmitRawLine("    SHIFT");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine($"    CALL3 {builder.RuntimeLib.Use(StoreByteAtBytePointer)}");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine($"    CALL3 {builder.RuntimeLib.Use(StoreByteAtBytePointer)}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                Random,
                new RuntimeRoutineDefinition(
                    "__Random",
                    [],
                    static (builder, routine) =>
                    {
                        if (builder.RuntimeLib.randomState is null)
                            throw new InvalidOperationException("Cornerstone random state is not initialized.");

                        routine.DefineRequiredParameter("range");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPGEZ random_nonnegative");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    NEG");
                        routine.EmitRawLine($"    STOREG {builder.RuntimeLib.randomState.Index}");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("random_nonnegative:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ random_positive");
                        routine.EmitRawLine("    PUSHW 0xACE1");
                        routine.EmitRawLine($"    STOREG {builder.RuntimeLib.randomState.Index}");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("random_positive:");
                        routine.EmitRawLine($"    LOADG {builder.RuntimeLib.randomState.Index}");
                        routine.EmitRawLine("    PUSHW 0x6255");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSHW 0x3619");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHW 0x7FFF");
                        routine.EmitRawLine("    AND");
                        routine.EmitRawLine($"    STOREG {builder.RuntimeLib.randomState.Index}");
                        routine.EmitRawLine($"    LOADG {builder.RuntimeLib.randomState.Index}");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    MOD");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                LoadIndirectGlobal,
                new RuntimeRoutineDefinition(
                    "__LoadIndirectGlobal",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("selector");

                        var globals = builder.Globals.OrderBy(global => global.Index).ToArray();

                        if (globals.Length == 0)
                        {
                            routine.EmitRawLine("    RZERO");
                            return;
                        }

                        static void EmitSearchNode(
                            GameBuilder.NamedVariable[] globals,
                            GameBuilder.RoutineBuilder routine,
                            int start,
                            int end,
                            string? entryLabel,
                            ref int nextNodeId)
                        {
                            if (entryLabel is not null)
                                routine.EmitRawLine($"{entryLabel}:");

                            if (start == end)
                            {
                                routine.EmitRawLine($"    LOADG {globals[start].Name}");
                                routine.EmitRawLine("    RETURN");
                                return;
                            }

                            var midpoint = start + ((end - start) / 2);
                            var midpointGlobal = globals[midpoint];
                            var midpointSelector = midpointGlobal.Index + 0x10;
                            var leftStart = start;
                            var leftEnd = midpoint - 1;
                            var rightStart = midpoint + 1;
                            var rightEnd = end;
                            var loadLabel = $"load_global_{midpointGlobal.Index:X4}";
                            var hasLeftBranch = leftStart <= leftEnd;
                            var hasRightBranch = rightStart <= rightEnd;

                            if (!hasRightBranch)
                            {
                                var leftLabel = $"load_global_search_{nextNodeId++:X4}";
                                routine.EmitRawLine("    PUSHL 0");
                                routine.EmitRawLine($"    PUSHW 0x{midpointSelector:X4}");
                                routine.EmitRawLine($"    JUMPL {leftLabel}");
                                routine.EmitRawLine($"    LOADG {midpointGlobal.Name}");
                                routine.EmitRawLine("    RETURN");
                                EmitSearchNode(globals, routine, leftStart, leftEnd, leftLabel, ref nextNodeId);
                                return;
                            }

                            routine.EmitRawLine("    PUSHL 0");
                            routine.EmitRawLine($"    PUSHW 0x{midpointSelector:X4}");
                            routine.EmitRawLine($"    JUMPEQ {loadLabel}");

                            string? leftBranchLabel = null;
                            if (hasLeftBranch)
                            {
                                leftBranchLabel = $"load_global_search_{nextNodeId++:X4}";
                                routine.EmitRawLine("    PUSHL 0");
                                routine.EmitRawLine($"    PUSHW 0x{midpointSelector:X4}");
                                routine.EmitRawLine($"    JUMPL {leftBranchLabel}");
                            }

                            EmitSearchNode(globals, routine, rightStart, rightEnd, null, ref nextNodeId);
                            routine.EmitRawLine($"{loadLabel}:");
                            routine.EmitRawLine($"    LOADG {midpointGlobal.Name}");
                            routine.EmitRawLine("    RETURN");

                            if (leftBranchLabel is not null)
                                EmitSearchNode(globals, routine, leftStart, leftEnd, leftBranchLabel, ref nextNodeId);
                        }

                        var missTargetLabel = "load_global_missing";
                        var nextNodeId = 0;
                        var lowSelector = globals[0].Index + 0x10;
                        var highSelector = globals[^1].Index + 0x10;

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    PUSHW 0x{lowSelector:X4}");
                        routine.EmitRawLine($"    JUMPL {missTargetLabel}");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    PUSHW 0x{highSelector:X4}");
                        routine.EmitRawLine($"    JUMPG {missTargetLabel}");
                        EmitSearchNode(globals, routine, 0, globals.Length - 1, null, ref nextNodeId);
                        routine.EmitRawLine($"{missTargetLabel}:");
                        routine.EmitRawLine("    RZERO");
                    }));

            routines.Add(
                TokenizeLine,
                new RuntimeRoutineDefinition(
                    "__TokenizeLine",
                    [LoadWordAtBytePointer],
                    static (builder, routine) =>
                    {
                        var tokenCompareBufferName = builder.EnsureTokenCompareBufferName();

                        routine.DefineRequiredParameter("charBuffer");
                        routine.DefineRequiredParameter("lexBuffer");
                        routine.DefineLocal("textLength");
                        routine.DefineLocal("lexMax");
                        routine.DefineLocal("scanIndex");
                        routine.DefineLocal("wordCount");
                        routine.DefineLocal("wordStart");
                        routine.DefineLocal("wordLength");
                        routine.DefineLocal("compareLength");
                        routine.DefineLocal("scratch");
                        routine.DefineLocal("breakRemaining");
                        routine.DefineLocal("breakPointer");
                        routine.DefineLocal("vocabLow");
                        routine.DefineLocal("vocabHigh");
                        routine.DefineLocal("entryPointer");
                        routine.DefineLocal("compareIndex");
                        routine.DefineLocal("matchedWord");
                        routine.DefineLocal("entryString");
                        routine.DefineLocal("entryChar");
                        routine.DefineLocal("vocabMid");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 1");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ have_lex_buffer");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("have_lex_buffer:");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTVB2");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 5");

                        routine.EmitRawLine("scan_words:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPEQ ABS:tokenize_done");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:tokenize_done");

                        routine.EmitRawLine("skip_spaces:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPEQ ABS:tokenize_done");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 9");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    PUSHL 9");
                        routine.EmitRawLine("    JUMPNE ABS:scan_word_start");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    JUMP ABS:skip_spaces");

                        routine.EmitRawLine("scan_word_start:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUTL 6");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.SelfInsertingBreaksLabel}");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 10");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    PUTL 11");

                        routine.EmitRawLine("check_initial_break:");
                        routine.EmitRawLine("    PUSHL 10");
                        routine.EmitRawLine("    JUMPZ ABS:scan_regular_word");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.SelfInsertingBreaksLabel}");
                        routine.EmitRawLine("    PUSHL 11");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 18");
                        routine.EmitRawLine("    PUSHL 18");
                        routine.EmitRawLine("    PUSHL 9");
                        routine.EmitRawLine("    JUMPEQ ABS:single_break_word");
                        routine.EmitRawLine("    PUSHL 11");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSHL 10");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 10");
                        routine.EmitRawLine("    JUMP ABS:check_initial_break");

                        routine.EmitRawLine("single_break_word:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    JUMP ABS:emit_token");

                        routine.EmitRawLine("scan_regular_word:");
                        routine.EmitRawLine("scan_regular_word_loop:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPEQ ABS:finish_regular_word");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 9");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    PUSHL 9");
                        routine.EmitRawLine("    JUMPEQ ABS:finish_regular_word");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.SelfInsertingBreaksLabel}");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 10");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("check_inner_break:");
                        routine.EmitRawLine("    PUSHL 10");
                        routine.EmitRawLine("    JUMPZ ABS:advance_regular_word");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.SelfInsertingBreaksLabel}");
                        routine.EmitRawLine("    PUSHL 11");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 18");
                        routine.EmitRawLine("    PUSHL 18");
                        routine.EmitRawLine("    PUSHL 9");
                        routine.EmitRawLine("    JUMPEQ ABS:finish_regular_word");
                        routine.EmitRawLine("    PUSHL 11");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSHL 10");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 10");
                        routine.EmitRawLine("    JUMP ABS:check_inner_break");
                        routine.EmitRawLine("advance_regular_word:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    JUMP ABS:scan_regular_word_loop");

                        routine.EmitRawLine("finish_regular_word:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 7");

                        routine.EmitRawLine("emit_token:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine($"    PUSH {GameBuilder.VocabularyResolution}");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    JUMPLE ABS:word_length_fits");
                        routine.EmitRawLine($"    PUSH {GameBuilder.VocabularyResolution}");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("    JUMP ABS:compare_length_ready");
                        routine.EmitRawLine("word_length_fits:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("compare_length_ready:");
                        routine.EmitRawLine($"    PUSHW {tokenCompareBufferName}");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 15");
                        routine.EmitRawLine("copy_token_chars:");
                        routine.EmitRawLine("    PUSHL 15");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    JUMPEQ ABS:token_compare_ready");
                        routine.EmitRawLine($"    PUSHW {tokenCompareBufferName}");
                        routine.EmitRawLine("    PUSHL 15");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUSHL 15");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSH3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    VPUTB");
                        routine.EmitRawLine("    PUSHL 15");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 15");
                        routine.EmitRawLine("    JUMP ABS:copy_token_chars");
                        routine.EmitRawLine("token_compare_ready:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 16");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 12");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.VocabularyTableLabel}");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 13");

                        routine.EmitRawLine("binary_search_vocabulary:");
                        routine.EmitRawLine("    PUSHL 12");
                        routine.EmitRawLine("    PUSHL 13");
                        routine.EmitRawLine("    JUMPEQ ABS:store_token");
                        routine.EmitRawLine("    PUSHL 12");
                        routine.EmitRawLine("    PUSHL 13");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 19");
                        routine.EmitRawLine($"    PUSHW {GameBuilder.VocabularyTableLabel}");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 19");
                        routine.EmitRawLine($"    PUSH {GameBuilder.VocabularyEntryWordCount}");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 14");
                        routine.EmitRawLine("    PUSHL 14");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 18");
                        routine.EmitRawLine("    PUSHL 14");
                        routine.EmitRawLine($"    PUSHW {tokenCompareBufferName}");
                        routine.EmitRawLine("    PUSHL 18");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    STRICMP");
                        routine.EmitRawLine("    PUTL 18");
                        routine.EmitRawLine("    PUSHL 18");
                        routine.EmitRawLine("    JUMPZ ABS:vocabulary_match");
                        routine.EmitRawLine("    PUSHL 18");
                        routine.EmitRawLine("    JUMPGZ ABS:search_lower_half");
                        routine.EmitRawLine("    JUMP ABS:search_upper_half");

                        routine.EmitRawLine("vocabulary_match:");
                        routine.EmitRawLine("    PUSHL 14");
                        routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.VocabularyRecordWordPointerOffset:X2}");
                        routine.EmitRawLine("    PUTL 16");
                        routine.EmitRawLine("    JUMP ABS:store_token");

                        routine.EmitRawLine("search_lower_half:");
                        routine.EmitRawLine("    PUSHL 19");
                        routine.EmitRawLine("    PUTL 13");
                        routine.EmitRawLine("    JUMP ABS:binary_search_vocabulary");

                        routine.EmitRawLine("search_upper_half:");
                        routine.EmitRawLine("    PUSHL 19");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 12");
                        routine.EmitRawLine("    JUMP ABS:binary_search_vocabulary");

                        routine.EmitRawLine("store_token:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 15");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHL 15");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 16");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSH4");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTVB2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSH4");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTVB2");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUTVB2");
                        routine.EmitRawLine("    JUMP ABS:scan_words");

                        routine.EmitRawLine("tokenize_done:");
                        routine.EmitRawLine("    RZERO");
                    }));

            routines.Add(
                ReadLine,
                new RuntimeRoutineDefinition(
                    "__ReadLine",
                    [ConsoleAdvanceLine, TokenizeLine, FlushOutputBuffer, DrawStatusLine, TryReadCommandFileLine, EchoReadLineToCommandFile],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("charBuffer");
                        routine.DefineRequiredParameter("lexBuffer");
                        routine.DefineLocal("maxLength");
                        routine.DefineLocal("cursor");
                        routine.DefineLocal("key");
                        routine.DefineLocal("startRow");
                        routine.DefineLocal("startColumn");
                        routine.DefineLocal("length");
                        routine.DefineLocal("temp");
                        routine.DefineLocal("charBufferBytePointer");
                        routine.DefineLocal("lexBufferBytePointer");
                        routine.DefineLocal("previousLength");
                        routine.DefineLocal("commandLength");
                        routine.DefineLocal("usedCommandFile");

                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(FlushOutputBuffer)}");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(DrawStatusLine)}");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUTL 9");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUTL 10");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    DIV");
                        routine.EmitRawLine("    PUTL 1");

                        // maxLength = raw charBuffer[1] in the Z-machine-style input buffer.
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    LOADVB2");
                        routine.EmitRawLine("    PUTL 2");

                        // Capture logical prompt position from tracked console state.
                        routine.EmitRawLine($"    LOADG {builder.ConsoleRowGlobalIndex}");
                        routine.EmitRawLine("    PUTL 5");
                        routine.EmitRawLine($"    LOADG {builder.ConsoleColumnGlobalIndex}");
                        routine.EmitRawLine("    PUTL 6");

                        // cursor = 0; internal length word = 0
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 13");
                        routine.EmitRawLine($"    LOADG {builder.CommandFileBypassGlobalIndex}");
                        routine.EmitRawLine("    JUMPNZ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(TryReadCommandFileLine)}");
                        routine.EmitRawLine("    PUTL 12");
                        routine.EmitRawLine("    PUSH_NIL");
                        routine.EmitRawLine("    PUSHL 12");
                        routine.EmitRawLine("    JUMPEQ ABS:read_key");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    PUTL 13");
                        routine.EmitRawLine("    PUSHL 12");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    JUMP ABS:input_done");

                        routine.EmitRawLine("read_key:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine("    KBINPUT");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPF ABS:read_key");

                        // Enter finishes input.
                        routine.EmitRawLine("    PUSHB 0x0D");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:input_done");

                        // Arrow and editing keys arrive as ESC followed by a second byte.
                        routine.EmitRawLine("    PUSHB 0x1B");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:read_escape");

                        // Ignore NUL input.
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:read_key");

                        // Keep the current payload length in L7 for editing decisions.
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 7");

                        // Backspace deletes one character.
                        routine.EmitRawLine("    PUSHB 0x08");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:erase_char");

                        // Ctrl+Backspace arrives as a direct 7F and deletes to the beginning.
                        routine.EmitRawLine("    PUSHB 0x7F");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:erase_to_start");

                        // Ignore input when full.
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    JUMPEQ ABS:read_key");

                        // Insert in the middle by shifting the tail one byte to the right.
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:store_char");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    VECCPYB");
                        routine.EmitRawLine("    POP");

                        routine.EmitRawLine("store_char:");
                        // charBuffer[length + 1] = key
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    VPUTB");

                        // length++
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 7");

                        // internal length word = length
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    VPUTW_ 0x00");

                        // Redraw from the insertion point, then restore the final cursor.
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUTL 8");

                        // cursor++ and redraw
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP ABS:redraw_after_edit");

                        routine.EmitRawLine("read_escape:");
                        routine.EmitRawLine("escape_key:");
                        routine.EmitRawLine("    KBINPUT");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPF ABS:escape_key");

                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 7");

                        routine.EmitRawLine("    PUSHB 0x4B");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:arrow_left");
                        routine.EmitRawLine("    PUSHB 0x4D");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:arrow_right");
                        routine.EmitRawLine("    PUSHB 0x48");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:arrow_home");
                        routine.EmitRawLine("    PUSHB 0x47");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:arrow_home");
                        routine.EmitRawLine("    PUSHB 0x50");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:arrow_end");
                        routine.EmitRawLine("    PUSHB 0x4F");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:arrow_end");
                        routine.EmitRawLine("    PUSHB 0x53");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:delete_char");
                        routine.EmitRawLine("    PUSHB 0x93");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPEQ ABS:delete_to_end");
                        routine.EmitRawLine("    JUMP ABS:read_key");

                        routine.EmitRawLine("arrow_home:");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP ABS:position_cursor_only");

                        routine.EmitRawLine("arrow_end:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP ABS:position_cursor_only");

                        routine.EmitRawLine("arrow_left:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPZ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP ABS:position_cursor_only");

                        routine.EmitRawLine("arrow_right:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    JUMP ABS:position_cursor_only");

                        routine.EmitRawLine("erase_char:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPZ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 11");

                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 8");

                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:shrink_buffer");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    VECCPYB");
                        routine.EmitRawLine("    POP");

                        routine.EmitRawLine("shrink_buffer:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUTL 8");

                        // internal length word = length
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    VPUTW_ 0x00");

                        routine.EmitRawLine("    JUMP ABS:redraw_after_edit");

                        routine.EmitRawLine("erase_to_start:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPZ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:clear_prefix");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    VECCPYB");
                        routine.EmitRawLine("    POP");
                        routine.EmitRawLine("clear_prefix:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    JUMP ABS:redraw_after_edit");

                        routine.EmitRawLine("delete_char:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    JUMPEQ ABS:shorten_buffer");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    VECCPYB");
                        routine.EmitRawLine("    POP");
                        routine.EmitRawLine("shorten_buffer:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    JUMP ABS:redraw_after_edit");

                        routine.EmitRawLine("delete_to_end:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPEQ ABS:read_key");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTL 11");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    VPUTW_ 0x00");
                        routine.EmitRawLine("    JUMP ABS:redraw_after_edit");

                        routine.EmitRawLine("redraw_after_edit:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("render_tail:");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    JUMPZ ABS:clear_deleted_tail");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    VLOADB");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUTL 4");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("    JUMP ABS:render_tail");
                        routine.EmitRawLine("clear_deleted_tail:");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUSHL 11");
                        routine.EmitRawLine("    JUMPLE ABS:position_cursor");
                        routine.EmitRawLine("    PUSHL 11");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("clear_deleted_loop:");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    JUMPZ ABS:position_cursor");
                        routine.EmitRawLine("    PUSHB 0x20");
                        routine.EmitRawLine("    PRCHAR");
                        routine.EmitRawLine("    PUSHL 8");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUTL 8");
                        routine.EmitRawLine("    JUMP ABS:clear_deleted_loop");
                        routine.EmitRawLine("position_cursor_only:");
                        routine.EmitRawLine("position_cursor:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorRowSlot:X2}");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUSHB 0x00");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
                        routine.EmitRawLine("    PUSHL 6");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine($"    STOREG {builder.ConsoleColumnGlobalIndex}");
                        routine.EmitRawLine("    JUMP ABS:read_key");

                        routine.EmitRawLine("input_done:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUTL 7");
                        routine.EmitRawLine($"    LOADG {builder.CommandFileBypassGlobalIndex}");
                        routine.EmitRawLine("    JUMPNZ ABS:skip_command_echo");
                        routine.EmitRawLine("    PUSHL 13");
                        routine.EmitRawLine("    JUMPNZ ABS:skip_command_echo");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(EchoReadLineToCommandFile)}");
                        routine.EmitRawLine("skip_command_echo:");

                        // Restore the external Z-machine-style buffer header in raw bytes.
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUTVB2");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    PUSHL 7");
                        routine.EmitRawLine("    PUTVB2");

                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPF ABS:no_lexbuf");
                        routine.EmitRawLine("    PUSHL 9");
                        routine.EmitRawLine("    PUSHL 10");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(TokenizeLine)}");
                        routine.EmitRawLine("    POP");
                        routine.EmitRawLine("no_lexbuf:");
                        routine.EmitRawLine("    PUSHL 13");
                        routine.EmitRawLine("    JUMPF ABS:skip_command_flush");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(FlushOutputBuffer)}");
                        routine.EmitRawLine("skip_command_flush:");
                        routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
                        routine.EmitRawLine("    PUSHB 0x0D");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                DirectInput,
                new RuntimeRoutineDefinition(
                    "__DirectInput",
                    [ReadLine, PrintObjDataString, ConsoleAdvanceLine],
                    static (builder, routine) =>
                    {
                        EmitDirectInputRoutine(builder, routine);
                    },
                    ReturnsValue: false));

            routines.Add(
                DirectOutput,
                new RuntimeRoutineDefinition(
                    "__DirectOutput",
                    [ReadLine, PrintObjDataString, ConsoleAdvanceLine, Stream3WriteChar,
                     LoadWordAtBytePointer, StoreWordAtBytePointer],
                    static (builder, routine) =>
                    {
                        EmitDirectOutputRoutine(builder, routine);
                    },
                    ReturnsValue: false));

            routines.Add(
                TryReadCommandFileLine,
                new RuntimeRoutineDefinition(
                    "__TryReadCommandFileLine",
                    [BufferedPrintCharacter],
                    static (builder, routine) =>
                    {
                        EmitTryReadCommandFileLineRoutine(builder, routine);
                    }));

            routines.Add(
                EchoReadLineToCommandFile,
                new RuntimeRoutineDefinition(
                    "__EchoReadLineToCommandFile",
                    [],
                    static (builder, routine) =>
                    {
                        EmitEchoReadLineToCommandFileRoutine(builder, routine);
                    },
                    ReturnsValue: false));

            routines.Add(
                GetParent,
                new RuntimeRoutineDefinition(
                    "__GetParent",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineLocal("recordAddress");
                        EmitObjectRecordLookup(routine);
                        routine.EmitRawLine($"    PUSHL 1");
                        routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordParentOffset:X2}");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetSibling,
                new RuntimeRoutineDefinition(
                    "__GetSibling",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineLocal("recordAddress");
                        EmitObjectRecordLookup(routine);
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordSiblingOffset:X2}");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetChild,
                new RuntimeRoutineDefinition(
                    "__GetChild",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineLocal("recordAddress");
                        EmitObjectRecordLookup(routine);
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordChildOffset:X2}");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                TestAttribute,
                new RuntimeRoutineDefinition(
                    "__TestAttribute",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineRequiredParameter("flagId");
                        routine.DefineLocal("recordAddress");
                        routine.DefineLocal("flagsAddress");
                        EmitObjectRecordLookup(routine, resultLocalIndex: 2);
                        if (builder.FlagCount <= 0)
                        {
                            routine.EmitRawLine("    RZERO");
                            return;
                        }

                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordFlagsOffset:X2}");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ have_flag_id");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("have_flag_id:");
                        routine.EmitRawLine($"    PUSHL 1");
                        routine.EmitRawLine($"    PUSH {builder.FlagCount}");
                        routine.EmitRawLine("    JUMPL invalid_flag_id");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    VLOADW");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("invalid_flag_id:");
                        routine.EmitRawLine("    RZERO");
                    }));

            routines.Add(
                SetFlag,
                new RuntimeRoutineDefinition(
                    "__SetFlag",
                    [],
                    static (builder, routine) =>
                    {
                        EmitSetFlagRoutine(builder, routine, enabled: true);
                    },
                    ReturnsValue: false));

            routines.Add(
                ClearFlag,
                new RuntimeRoutineDefinition(
                    "__ClearFlag",
                    [],
                    static (builder, routine) =>
                    {
                        EmitSetFlagRoutine(builder, routine, enabled: false);
                    },
                    ReturnsValue: false));

            routines.Add(
                GetPropertyAddress,
                new RuntimeRoutineDefinition(
                    "__GetPropertyAddress",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineRequiredParameter("propertyId");
                        routine.DefineLocal("recordAddress");
                        routine.DefineLocal("propertiesAddress");
                        routine.DefineLocal("remaining");
                        routine.DefineLocal("pointer");
                        routine.DefineLocal("length");
                        EmitPropertyScanPrologue(routine);
                        routine.EmitRawLine("scan_properties:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPNZ have_property");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("have_property:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPEQ property_found");
                        EmitAdvancePropertyPointer(routine);
                        routine.EmitRawLine("    JUMP scan_properties");
                        routine.EmitRawLine("property_found:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    ADD");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    MUL");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetNextProperty,
                new RuntimeRoutineDefinition(
                    "__GetNextProperty",
                    [],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineRequiredParameter("propertyId");
                        routine.DefineLocal("recordAddress");
                        routine.DefineLocal("propertiesAddress");
                        routine.DefineLocal("remaining");
                        routine.DefineLocal("pointer");
                        routine.DefineLocal("length");
                        EmitPropertyScanPrologue(routine);
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ search_next_property");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPNZ first_property_present");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("first_property_present:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    RETURN");
                        routine.EmitRawLine("search_next_property:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    JUMPNZ have_next_candidate");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("have_next_candidate:");
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPEQ current_property_found");
                        EmitAdvancePropertyPointer(routine);
                        routine.EmitRawLine("    JUMP search_next_property");
                        routine.EmitRawLine("current_property_found:");
                        routine.EmitRawLine("    PUSHL 4");
                        routine.EmitRawLine("    PUSH1");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    JUMPNZ next_property_present");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("next_property_present:");
                        EmitAdvancePropertyPointer(routine);
                        routine.EmitRawLine("    PUSHL 5");
                        routine.EmitRawLine("    VLOADW_ 0x00");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetProperty,
                new RuntimeRoutineDefinition(
                    "__GetProperty",
                    [GetPropertyAddress, LoadWordAtBytePointer],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineRequiredParameter("propertyId");
                        routine.DefineLocal("propertyAddress");
                        routine.DefineLocal("defaultAddress");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(GetPropertyAddress)}");
                        routine.EmitRawLine("    PUTL 2");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    JUMPNZ property_value_found");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ property_default_lookup");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("property_default_lookup:");
                        if (builder.PropertyCount > 0)
                        {
                            routine.EmitRawLine($"    PUSHL 1");
                            routine.EmitRawLine($"    PUSH {builder.HighestAssignedPropertyId}");
                            routine.EmitRawLine("    JUMPL invalid_property_id");
                            routine.EmitRawLine("    PUSHL 1");
                            routine.EmitRawLine("    PUSH1");
                            routine.EmitRawLine("    SUB");
                            routine.EmitRawLine("    PUSH2");
                            routine.EmitRawLine("    MUL");
                            routine.EmitRawLine($"    PUSHW {GameBuilder.PropertyDefaultsLabel}");
                            routine.EmitRawLine("    ADD");
                            routine.EmitRawLine("    PUTL 3");
                            routine.EmitRawLine("    PUSHL 3");
                            routine.EmitRawLine("    VLOADW_ 0x00");
                            routine.EmitRawLine("    RETURN");
                            routine.EmitRawLine("invalid_property_id:");
                        }
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("property_value_found:");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadWordAtBytePointer)}");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                GetPropertySize,
                new RuntimeRoutineDefinition(
                    "__GetPropertySize",
                    [LoadWordAtBytePointer],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("propertyAddress");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    JUMPNZ has_property_address");
                        routine.EmitRawLine("    RZERO");
                        routine.EmitRawLine("has_property_address:");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSH2");
                        routine.EmitRawLine("    SUB");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadWordAtBytePointer)}");
                        routine.EmitRawLine("    RETURN");
                    }));

            routines.Add(
                PutProperty,
                new RuntimeRoutineDefinition(
                    "__PutProperty",
                    [GetPropertyAddress, StoreWordAtBytePointer],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineRequiredParameter("propertyId");
                        routine.DefineRequiredParameter("value");
                        routine.DefineLocal("propertyAddress");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(GetPropertyAddress)}");
                        routine.EmitRawLine("    PUTL 3");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    JUMPNZ write_property_value");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("write_property_value:");
                        routine.EmitRawLine("    PUSHL 3");
                        routine.EmitRawLine("    PUSH0");
                        routine.EmitRawLine("    PUSHL 2");
                        routine.EmitRawLine($"    CALL3 {builder.RuntimeLib.Use(StoreWordAtBytePointer)}");
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));

            routines.Add(
                RemoveObject,
                new RuntimeRoutineDefinition(
                    "__RemoveObject",
                    [GetParent, GetChild, GetSibling],
                    static (builder, routine) =>
                    {
                        EmitRemoveObjectRoutine(routine);
                    },
                    ReturnsValue: false));

            routines.Add(
                MoveObject,
                new RuntimeRoutineDefinition(
                    "__MoveObject",
                    [RemoveObject, GetChild],
                    static (builder, routine) =>
                    {
                        routine.DefineRequiredParameter("objectId");
                        routine.DefineRequiredParameter("destinationId");
                        routine.DefineLocal("objectRecord");
                        routine.DefineLocal("destinationRecord");
                        routine.DefineLocal("destinationChild");
                        routine.EmitRawLine("    PUSHL 0");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(RemoveObject)}");
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine("    JUMPNZ have_destination");
                        routine.EmitRawLine("    RET");
                        routine.EmitRawLine("have_destination:");
                        EmitObjectRecordLookup(routine, objectLocalIndex: 0, resultLocalIndex: 2, returnsValue: false);
                        EmitObjectRecordLookup(routine, objectLocalIndex: 1, resultLocalIndex: 3, returnsValue: false);
                        routine.EmitRawLine("    PUSHL 1");
                        routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(GetChild)}");
                        routine.EmitRawLine("    PUTL 4");
                        EmitStoreObjectRecordSlot(routine, recordLocalIndex: 2, slotOffset: GameBuilder.ObjectRecordParentOffset, valueLocalIndex: 1);
                        EmitStoreObjectRecordSlot(routine, recordLocalIndex: 2, slotOffset: GameBuilder.ObjectRecordSiblingOffset, valueLocalIndex: 4);
                        EmitStoreObjectRecordSlot(routine, recordLocalIndex: 3, slotOffset: GameBuilder.ObjectRecordChildOffset, valueLocalIndex: 0);
                        routine.EmitRawLine("    RET");
                    },
                    ReturnsValue: false));
        }

        internal void Attach(GameBuilder builder)
        {
            if (gameBuilder is not null && !ReferenceEquals(gameBuilder, builder))
                throw new InvalidOperationException("This Cornerstone runtime library is already attached to a different game builder.");

            gameBuilder = builder;

            if (randomState is null)
            {
                randomState = (GameBuilder.NamedVariable)builder.DefineGlobal("__RANDOM_STATE");
                randomState.DefaultValue = builder.MakeOperand(0xACE1);
            }
        }

        public string Use(string name)
        {
            if (gameBuilder is null)
                throw new InvalidOperationException("The Cornerstone runtime library must be attached before use.");

            if (!routines.TryGetValue(name, out var definition))
                throw new ArgumentException($"No such Cornerstone runtime routine: {name}", nameof(name));

            EnsureUsed(name);
            return definition.BaseName;

            void EnsureUsed(string routineName)
            {
                if (usedRoutines.ContainsKey(routineName))
                    return;

                var routineDefinition = routines[routineName];

                // Mark as in-progress before recursing into dependencies to break cycles.
                // We use a placeholder value; the real RoutineBuilder replaces it below.
                usedRoutines.Add(routineName, null!);

                foreach (var dependency in routineDefinition.Dependencies)
                {
                    if (!usedRoutines.ContainsKey(dependency))
                        EnsureUsed(dependency);
                }

                var runtimeRoutine = gameBuilder.CreateRuntimeRoutine(routineDefinition.BaseName);
                usedRoutines[routineName] = runtimeRoutine;
                routineDefinition.Emit(gameBuilder, runtimeRoutine);
            }
        }

        public bool ReturnsValue(string name)
        {
            if (!routines.TryGetValue(name, out var definition))
                throw new ArgumentException($"No such Cornerstone runtime routine: {name}", nameof(name));

            return definition.ReturnsValue;
        }

        public IEnumerable<string> UsedRoutineNames => usedRoutines.Values.Select(routine => routine.Name);

        private static void EmitObjectRecordLookup(
            GameBuilder.RoutineBuilder routine,
            int objectLocalIndex = 0,
            int resultLocalIndex = 1,
            bool returnsValue = true)
        {
            string haveObjectIdLabel = $"have_object_id_{objectLocalIndex}_{resultLocalIndex}";
            routine.EmitRawLine($"    PUSHL {objectLocalIndex}");
            routine.EmitRawLine($"    JUMPNZ {haveObjectIdLabel}");
            routine.EmitRawLine(returnsValue ? "    RZERO" : "    RET");
            routine.EmitRawLine($"{haveObjectIdLabel}:");
            routine.EmitRawLine($"    PUSHL {objectLocalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    SUB");
            routine.EmitRawLine($"    PUSH {GameBuilder.ObjectRecordWordCount}");
            routine.EmitRawLine("    MUL");
            routine.EmitRawLine($"    PUSHW {GameBuilder.ObjectRecordsLabel}");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine($"    PUTL {resultLocalIndex}");
        }

        private static void EmitStoreObjectRecordSlot(GameBuilder.RoutineBuilder routine, int recordLocalIndex, int slotOffset, int valueLocalIndex)
        {
            routine.EmitRawLine($"    PUSHL {recordLocalIndex}");
            routine.EmitRawLine($"    PUSHL {valueLocalIndex}");
            routine.EmitRawLine($"    VPUTW_ 0x{slotOffset:X2}");
        }

        private static void EmitSetFlagRoutine(GameBuilder builder, GameBuilder.RoutineBuilder routine, bool enabled)
        {
            routine.DefineRequiredParameter("objectId");
            routine.DefineRequiredParameter("flagId");
            routine.DefineLocal("recordAddress");
            routine.DefineLocal("flagsAddress");
            EmitObjectRecordLookup(routine, resultLocalIndex: 2, returnsValue: false);
            if (builder.FlagCount <= 0)
            {
                routine.EmitRawLine("    RET");
                return;
            }

            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine("    JUMPNZ have_flag_id");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("have_flag_id:");
            routine.EmitRawLine($"    PUSHL 1");
            routine.EmitRawLine($"    PUSH {builder.FlagCount}");
            routine.EmitRawLine("    JUMPL invalid_flag_id");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordFlagsOffset:X2}");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine(enabled ? "    PUSH1" : "    PUSH0");
            routine.EmitRawLine("    VPUTW");
            routine.EmitRawLine("invalid_flag_id:");
            routine.EmitRawLine("    RET");
        }

        private static void EmitRemoveObjectRoutine(GameBuilder.RoutineBuilder routine)
        {
            routine.DefineRequiredParameter("objectId");
            routine.DefineLocal("recordAddress");
            routine.DefineLocal("parentId");
            routine.DefineLocal("parentRecord");
            routine.DefineLocal("nextSiblingId");
            routine.DefineLocal("cursorId");
            routine.DefineLocal("cursorRecord");
            routine.DefineLocal("candidateSiblingId");
            routine.DefineLocal("zeroId");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL 8");

            EmitObjectRecordLookup(routine, resultLocalIndex: 1, returnsValue: false);
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordParentOffset:X2}");
            routine.EmitRawLine("    PUTL 2");
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordSiblingOffset:X2}");
            routine.EmitRawLine("    PUTL 4");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    JUMPNZ has_parent");
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordSiblingOffset, valueLocalIndex: 2);
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("has_parent:");
            EmitObjectRecordLookup(routine, objectLocalIndex: 2, resultLocalIndex: 3, returnsValue: false);
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordChildOffset:X2}");
            routine.EmitRawLine("    PUTL 5");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    SUB");
            routine.EmitRawLine("    JUMPNZ seek_previous_sibling");
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 3, slotOffset: GameBuilder.ObjectRecordChildOffset, valueLocalIndex: 4);
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordParentOffset, valueLocalIndex: 8);
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordSiblingOffset, valueLocalIndex: 8);
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("seek_previous_sibling:");
            routine.EmitRawLine("scan_siblings:");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    JUMPNZ sibling_candidate_present");
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordParentOffset, valueLocalIndex: 8);
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordSiblingOffset, valueLocalIndex: 8);
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("sibling_candidate_present:");
            EmitObjectRecordLookup(routine, objectLocalIndex: 5, resultLocalIndex: 6, returnsValue: false);
            routine.EmitRawLine("    PUSHL 6");
            routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordSiblingOffset:X2}");
            routine.EmitRawLine("    PUTL 7");
            routine.EmitRawLine("    PUSHL 7");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    SUB");
            routine.EmitRawLine("    JUMPZ found_previous_sibling");
            routine.EmitRawLine("    PUSHL 7");
            routine.EmitRawLine("    PUTL 5");
            routine.EmitRawLine("    JUMP scan_siblings");
            routine.EmitRawLine("found_previous_sibling:");
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 6, slotOffset: GameBuilder.ObjectRecordSiblingOffset, valueLocalIndex: 4);
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordParentOffset, valueLocalIndex: 8);
            EmitStoreObjectRecordSlot(routine, recordLocalIndex: 1, slotOffset: GameBuilder.ObjectRecordSiblingOffset, valueLocalIndex: 8);
            routine.EmitRawLine("    RET");
        }

        private static void EmitPropertyScanPrologue(GameBuilder.RoutineBuilder routine)
        {
            EmitObjectRecordLookup(routine, resultLocalIndex: 2);
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine("    JUMPNZ have_property_id");
            routine.EmitRawLine("    RZERO");
            routine.EmitRawLine("have_property_id:");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine($"    VLOADW_ 0x{GameBuilder.ObjectRecordPropertiesOffset:X2}");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    VLOADW_ 0x00");
            routine.EmitRawLine("    PUTL 4");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 5");
        }

        private static void EmitDirectInputRoutine(GameBuilder builder, GameBuilder.RoutineBuilder routine)
        {
            routine.DefineRequiredParameter("streamId");
            routine.DefineLocal("channelId");
            routine.DefineLocal("length");
            routine.DefineLocal("index");

            string promptLabel = builder.RegisterObjString("Input file: ").Location.Name;
            string errorLabel = builder.RegisterObjString("Command file failed.").Location.Name;
            string inputBufferName = builder.EnsureCommandFileInputBufferName();
            string fileNameBufferName = builder.EnsureCommandFileNameBufferName();

            routine.EmitRawLine($"    LOADG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF no_existing_channel");
            routine.EmitRawLine($"    LOADG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    CLOSE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine("no_existing_channel:");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHW 0x0001");
            routine.EmitRawLine("    JUMPEQ open_command_stream");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("open_command_stream:");
            routine.EmitRawLine($"    PUSHW {promptLabel}");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine($"    STOREG {builder.CommandFileBypassGlobalIndex}");
            routine.EmitRawLine($"    PUSHW byte:{inputBufferName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(ReadLine)}");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    STOREG {builder.CommandFileBypassGlobalIndex}");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine($"    PUSHW {inputBufferName}");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine("    PUTL 2");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    JUMPNZ have_file_name");
            routine.EmitRawLine($"    PUSHW {errorLabel}");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
            routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("have_file_name:");
            routine.EmitRawLine($"    PUSHW {fileNameBufferName}");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    VPUTW_ 0x00");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("copy_file_name_loop:");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    JUMPLE copy_file_name_byte");
            routine.EmitRawLine("    JUMP open_file_channel");
            routine.EmitRawLine("copy_file_name_byte:");
            routine.EmitRawLine($"    PUSHW {fileNameBufferName}");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine($"    PUSHW {inputBufferName}");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine("    VPUTB");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    JUMP copy_file_name_loop");
            routine.EmitRawLine("open_file_channel:");
            routine.EmitRawLine($"    PUSHW {fileNameBufferName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    OPEN 0x00");
            routine.EmitRawLine($"    STOREG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF open_file_failed");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("open_file_failed:");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine($"    PUSHW {errorLabel}");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
            routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
            routine.EmitRawLine("    RET");
        }

        private static void EmitDirectOutputRoutine(GameBuilder builder, GameBuilder.RoutineBuilder routine)
        {
            routine.DefineRequiredParameter("streamId");
            routine.DefineRequiredParameter("tableAddress");
            routine.DefineLocal("channelId");
            routine.DefineLocal("length");
            routine.DefineLocal("index");

            string promptLabel = builder.RegisterObjString("Output file: ").Location.Name;
            string errorLabel = builder.RegisterObjString("Record file failed.").Location.Name;
            string inputBufferName = builder.EnsureCommandFileInputBufferName();
            string fileNameBufferName = builder.EnsureCommandFileNameBufferName();
            string stackName = builder.EnsureStream3StackName();

            // --- Stream 4 (command file / transcript) enable ---
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHW 0x0004");
            routine.EmitRawLine("    JUMPEQ enable_stream_4");

            // --- Stream 3 (memory table) enable ---
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHW 0x0003");
            routine.EmitRawLine("    JUMPEQ enable_stream_3");

            // --- Stream 0 (disable all) ---
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    JUMPNZ handle_negative_stream");
            routine.EmitRawLine("disable_all:");

            // Clear stream 4.
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF stream4_not_active");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    CLOSE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("stream4_not_active:");

            // Clear stream 3 state.
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    PUTG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    RET");

            // --- Negative stream values (disable) ---
            routine.EmitRawLine("handle_negative_stream:");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHW 0xFFFC");  // -4
            routine.EmitRawLine("    JUMPNE check_negative_3");

            // streamId == -4: disable stream 4.
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF stream4_not_active2");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    CLOSE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("stream4_not_active2:");
            routine.EmitRawLine("    RET");

            routine.EmitRawLine("check_negative_3:");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHW 0xFFFD");  // -3
            routine.EmitRawLine("    JUMPNE direct_output_done");

            // streamId == -3: disable stream 3 (pop nesting stack).
            routine.EmitRawLine($"    LOADG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    JUMPZ clear_stream3");
            // Pop from stack.
            routine.EmitRawLine($"    LOADG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    SUB");
            routine.EmitRawLine($"    STOREG {builder.Stream3StackPointerGlobalIndex}");
            // Restore saved table address from stack.
            routine.EmitRawLine($"    LOADG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    MUL");
            routine.EmitRawLine($"    PUSHW {stackName}");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    VLOADW_ 0x00");
            routine.EmitRawLine($"    STOREG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("clear_stream3:");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    PUTG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    RET");

            // --- Stream 4 enable ---
            routine.EmitRawLine("enable_stream_4:");

            // Close any existing stream 3 state — streams 3 and 4 are mutually exclusive.
            routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    JUMPF no_stream3_to_close");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    PUTG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("no_stream3_to_close:");

            // Close existing stream 4 if open.
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF no_existing_stream4");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    CLOSE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("no_existing_stream4:");
            routine.EmitRawLine($"    PUSHW {promptLabel}");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine($"    STOREG {builder.CommandFileBypassGlobalIndex}");
            routine.EmitRawLine($"    PUSHW byte:{inputBufferName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(ReadLine)}");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    STOREG {builder.CommandFileBypassGlobalIndex}");
            routine.EmitRawLine($"    PUSHW {inputBufferName}");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine("    PUTL 2");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    JUMPNZ have_stream4_file_name");
            routine.EmitRawLine($"    PUSHW {errorLabel}");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
            routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("have_stream4_file_name:");
            routine.EmitRawLine($"    PUSHW {fileNameBufferName}");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    VPUTW_ 0x00");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("stream4_copy_file_name_loop:");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    JUMPLE stream4_copy_file_name_byte");
            routine.EmitRawLine("    JUMP open_stream4_file_channel");
            routine.EmitRawLine("stream4_copy_file_name_byte:");
            routine.EmitRawLine($"    PUSHW {fileNameBufferName}");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine($"    PUSHW {inputBufferName}");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine("    VPUTB");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    JUMP stream4_copy_file_name_loop");
            routine.EmitRawLine("open_stream4_file_channel:");
            routine.EmitRawLine($"    PUSHW {fileNameBufferName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    OPEN 0x01");
            routine.EmitRawLine($"    STOREG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF open_stream4_file_failed");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("open_stream4_file_failed:");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine($"    PUSHW {errorLabel}");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(PrintObjDataString)}");
            routine.EmitRawLine($"    CALL0 {builder.RuntimeLib.Use(ConsoleAdvanceLine)}");
            routine.EmitRawLine("    RET");

            // --- Stream 3 enable ---
            routine.EmitRawLine("enable_stream_3:");

            // Close any existing stream 4 — streams 3 and 4 are mutually exclusive.
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF no_stream4_to_close");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    CLOSE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("no_stream4_to_close:");

            // If already active, push current state onto the nesting stack.
            routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    JUMPF stream3_not_active");
            // Save current table address to stack[sp*2].
            routine.EmitRawLine($"    LOADG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    MUL");
            routine.EmitRawLine($"    PUSHW {stackName}");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    VPUTW_ 0x00");
            // Save current count (byte-pointer read) to stack[sp*2 + 1].
            routine.EmitRawLine($"    LOADG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(LoadWordAtBytePointer)}");
            routine.EmitRawLine($"    LOADG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    MUL");
            routine.EmitRawLine($"    PUSHW {stackName}");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    VPUTW_ 0x00");
            routine.EmitRawLine($"    LOADG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine($"    STOREG {builder.Stream3StackPointerGlobalIndex}");
            routine.EmitRawLine("stream3_not_active:");

            // Set stream 3 table to tableAddress, initialize count to 0 (byte-pointer write).
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine($"    STOREG {builder.Stream3TableGlobalIndex}");
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine($"    CALL3 {builder.RuntimeLib.Use(StoreWordAtBytePointer)}");
            routine.EmitRawLine("direct_output_done:");
            routine.EmitRawLine("    RET");
        }

        private static void EmitTryReadCommandFileLineRoutine(GameBuilder builder, GameBuilder.RoutineBuilder routine)
        {
            routine.DefineRequiredParameter("charBuffer");
            routine.DefineRequiredParameter("maxLength");
            routine.DefineLocal("readStatus");
            routine.DefineLocal("lowByte");
            routine.DefineLocal("highByte");
            routine.DefineLocal("length");

            string transferBufferName = builder.EnsureCommandFileTransferBufferName();

            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL 5");
            routine.EmitRawLine($"    LOADG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine("    JUMPF check_command_channel");
            routine.EmitRawLine($"    LOADG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    STOREG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL 4");
            routine.EmitRawLine("    JUMP process_command_low");
            routine.EmitRawLine("check_command_channel:");
            routine.EmitRawLine($"    LOADG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF no_command_channel");
            routine.EmitRawLine("command_read_loop:");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine($"    LOADG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    READ");
            routine.EmitRawLine("    PUTL 2");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    VLOADB_ 0x00");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    VLOADB_ 0x01");
            routine.EmitRawLine("    PUTL 4");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    JUMPZ process_command_low");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    JUMPNZ process_command_low");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    JUMPNZ process_command_low");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    JUMPNZ command_line_ready");
            routine.EmitRawLine($"    LOADG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    CLOSE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH_NIL");
            routine.EmitRawLine($"    PUTG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine("    RFALSE");
            routine.EmitRawLine("process_command_low:");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    JUMPZ process_command_high");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSHB 0x0D");
            routine.EmitRawLine("    JUMPEQ command_low_newline");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    PUSHB 0x0A");
            routine.EmitRawLine("    JUMPEQ command_low_newline");
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    JUMPEQ process_command_high");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    VPUTB");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(BufferedPrintCharacter)}");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 5");
            routine.EmitRawLine("    JUMP process_command_high");
            routine.EmitRawLine("command_low_newline:");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    JUMPNZ command_low_newline_ready");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    JUMPZ command_read_loop");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSHB 0x0D");
            routine.EmitRawLine("    JUMPEQ command_read_loop");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSHB 0x0A");
            routine.EmitRawLine("    JUMPEQ command_read_loop");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL 4");
            routine.EmitRawLine("    JUMP process_command_low");
            routine.EmitRawLine("command_low_newline_ready:");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    JUMPZ command_line_ready");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSHB 0x0D");
            routine.EmitRawLine("    JUMPEQ command_line_ready");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSHB 0x0A");
            routine.EmitRawLine("    JUMPEQ command_line_ready");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine($"    STOREG {builder.CommandInputPendingByteGlobalIndex}");
            routine.EmitRawLine("    JUMP command_line_ready");
            routine.EmitRawLine("process_command_high:");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    JUMPZ command_read_loop");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSHB 0x0D");
            routine.EmitRawLine("    JUMPEQ command_high_newline");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSHB 0x0A");
            routine.EmitRawLine("    JUMPEQ command_high_newline");
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    JUMPEQ command_read_loop");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    VPUTB");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine($"    CALL1 {builder.RuntimeLib.Use(BufferedPrintCharacter)}");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 5");
            routine.EmitRawLine("    JUMP command_read_loop");
            routine.EmitRawLine("command_high_newline:");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    JUMPNZ command_line_ready");
            routine.EmitRawLine("    JUMP command_read_loop");
            routine.EmitRawLine("command_line_ready:");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    VPUTW_ 0x00");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    RETURN");
            routine.EmitRawLine("no_command_channel:");
            routine.EmitRawLine("    RFALSE");
        }

        private static void EmitEchoReadLineToCommandFileRoutine(GameBuilder builder, GameBuilder.RoutineBuilder routine)
        {
            routine.DefineRequiredParameter("charBuffer");
            routine.DefineRequiredParameter("length");
            routine.DefineLocal("index");
            routine.DefineLocal("lowByte");
            routine.DefineLocal("highByte");

            string transferBufferName = builder.EnsureCommandFileTransferBufferName();

            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    JUMPF echo_done");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL 2");
            routine.EmitRawLine("echo_char_loop:");
            routine.EmitRawLine("    PUSHL 1");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    JUMPL echo_char_body");
            routine.EmitRawLine("    JUMP append_command_crlf");
            routine.EmitRawLine("echo_char_body:");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    VLOADB");
            routine.EmitRawLine("    PUTL 3");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL 4");
            routine.EmitRawLine("echo_write_char:");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    PUSHL 3");
            routine.EmitRawLine("    VPUTB_ 0x00");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    VPUTB_ 0x01");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    WRITE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSHL 2");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 2");
            routine.EmitRawLine("    JUMP echo_char_loop");
            routine.EmitRawLine("append_command_crlf:");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    PUSHB 0x0D");
            routine.EmitRawLine("    VPUTB_ 0x00");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    PUSHB 0x0A");
            routine.EmitRawLine("    VPUTB_ 0x01");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    WRITE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    PUSHB 0x0A");
            routine.EmitRawLine("    VPUTB_ 0x00");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    VPUTB_ 0x01");
            routine.EmitRawLine($"    PUSHW {transferBufferName}");
            routine.EmitRawLine($"    LOADG {builder.CommandOutputChannelGlobalIndex}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    WRITE");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("echo_done:");
            routine.EmitRawLine("    RET");
        }

        private static void EmitPackedStringWrapper(GameBuilder builder, GameBuilder.RoutineBuilder routine, bool directOutput)
        {
            routine.DefineRequiredParameter("stringId");
            routine.EmitRawLine("    PUSHL 0");
            routine.EmitRawLine(directOutput ? "    PUSH1" : "    PUSH0");
            routine.EmitRawLine($"    CALL2 {builder.RuntimeLib.Use(PrintPackedObjDataCore)}");
            routine.EmitRawLine("    RET");
        }

        private static void EmitPackedStringRoutine(GameBuilder builder, GameBuilder.RoutineBuilder routine)
        {
            var bufferName = builder.EnsureObjDataBufferName();
            var unpackBufferName = builder.EnsurePackedTextUnpackBufferName();
            const string labelPrefix = "packed";
            var decodeWordLoopLabel = $"decode_word_loop_{labelPrefix}";
            var decodeSymbolLoopLabel = $"decode_symbol_loop_{labelPrefix}";
            var advanceWordLabel = $"advance_word_{labelPrefix}";
            var restoreRecordSizeLabel = $"restore_record_size_{labelPrefix}";

            routine.DefineRequiredParameter("stringId");
            routine.DefineRequiredParameter("directOutput");
            routine.DefineLocal("remainingSymbols");
            routine.DefineLocal("currentWordHandle");
            routine.DefineLocal("pendingAlphabet");
            routine.DefineLocal("escapeState");
            routine.DefineLocal("escapeHighBits");
            routine.DefineLocal("currentSymbol");
            routine.DefineLocal("decodedChar");
            routine.DefineLocal("savedRecordSize");
            routine.DefineLocal("channelId");
            routine.DefineLocal("currentRecord");
            routine.DefineLocal("currentByteOffset");
            routine.DefineLocal("channelIndex");
            routine.DefineLocal("baseRecord");
            routine.DefineLocal("symbolIndex");

            routine.EmitRawLine("    PUSHL stringId");
            routine.EmitRawLine("    JUMPNZ have_string_id");
            routine.EmitRawLine("    RET");
            routine.EmitRawLine("have_string_id:");
            routine.EmitRawLine("    LOADMG 0xCC");
            routine.EmitRawLine("    PUTL savedRecordSize");
            routine.EmitRawLine("    LOADMG 0xDA");
            routine.EmitRawLine("    PUTL channelId");
            routine.EmitRawLine("    PUSHL channelId");
            routine.EmitRawLine("    PUSHB 0x1F");
            routine.EmitRawLine("    AND");
            routine.EmitRawLine("    PUTL channelIndex");
            routine.EmitRawLine("    PUSHL channelId");
            routine.EmitRawLine("    PUSHW 0xFFFB");
            routine.EmitRawLine("    SHIFT");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ASHIFT");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL baseRecord");
            routine.EmitRawLine("    PUSHL stringId");
            routine.EmitRawLine("    PUSHW 0x7FFF");
            routine.EmitRawLine("    AND");
            routine.EmitRawLine("    PUSHW 0xFFF9");
            routine.EmitRawLine("    SHIFT");
            routine.EmitRawLine("    PUSHL baseRecord");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentRecord");
            routine.EmitRawLine("    PUSHL stringId");
            routine.EmitRawLine("    PUSHW 0x8000");
            routine.EmitRawLine("    AND");
            routine.EmitRawLine("    JUMPZ have_objdata_record");
            routine.EmitRawLine("    PUSHL currentRecord");
            routine.EmitRawLine("    PUSHW 0x0100");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentRecord");
            routine.EmitRawLine("have_objdata_record:");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL pendingAlphabet");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL escapeState");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL escapeHighBits");
            routine.EmitRawLine("    PUSHL stringId");
            routine.EmitRawLine("    PUSHB 0x7F");
            routine.EmitRawLine("    AND");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ASHIFT");
            routine.EmitRawLine("    PUTL currentByteOffset");
            routine.EmitRawLine($"    PUSHW {bufferName}");
            routine.EmitRawLine("    PUSHL channelIndex");
            routine.EmitRawLine("    PUSHL currentRecord");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUSHW 0x0100");
            routine.EmitRawLine("    PUTMG 0xCC");
            routine.EmitRawLine("    READREC");
            routine.EmitRawLine("    PUSHL savedRecordSize");
            routine.EmitRawLine("    PUTMG 0xCC");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine($"    PUSHW {bufferName}");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine("    PUTL remainingSymbols");
            routine.EmitRawLine($"    PUSHW {bufferName}");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine("    PUSHB 0x08");
            routine.EmitRawLine("    ASHIFT");
            routine.EmitRawLine("    PUSHL remainingSymbols");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL remainingSymbols");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentByteOffset");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSHW 0x0100");
            routine.EmitRawLine($"    JUMPNE {decodeWordLoopLabel}");
            routine.EmitRawLine("    PUSHL currentRecord");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentRecord");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL currentByteOffset");
            routine.EmitRawLine($"{decodeWordLoopLabel}:");
            routine.EmitRawLine("    PUSHL remainingSymbols");
            routine.EmitRawLine($"    JUMPZ {restoreRecordSizeLabel}");
            routine.EmitRawLine($"    PUSHW {bufferName}");
            routine.EmitRawLine("    PUSHL channelIndex");
            routine.EmitRawLine("    PUSHL currentRecord");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUSHW 0x0100");
            routine.EmitRawLine("    PUTMG 0xCC");
            routine.EmitRawLine("    READREC");
            routine.EmitRawLine("    PUSHL savedRecordSize");
            routine.EmitRawLine("    PUTMG 0xCC");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine($"    PUSHW {bufferName}");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    DIV");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentWordHandle");
            routine.EmitRawLine("    PUSHL currentWordHandle");
            routine.EmitRawLine($"    PUSHW {unpackBufferName}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    UNPACK");
            routine.EmitRawLine("    POP");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUTL symbolIndex");
            routine.EmitRawLine($"{decodeSymbolLoopLabel}:");
            routine.EmitRawLine($"    PUSHW {unpackBufferName}");
            routine.EmitRawLine("    PUSHL symbolIndex");
            routine.EmitRawLine("    VLOADB");
            routine.EmitRawLine("    PUTL currentSymbol");
            EmitPackedSymbolDecode(routine, labelPrefix, directOutputLocalName: "directOutput");
            routine.EmitRawLine("    PUSHL remainingSymbols");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    SUB");
            routine.EmitRawLine("    PUTL remainingSymbols");
            routine.EmitRawLine("    PUSHL remainingSymbols");
            routine.EmitRawLine($"    JUMPZ {advanceWordLabel}");
            routine.EmitRawLine("    PUSHL symbolIndex");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL symbolIndex");
            routine.EmitRawLine("    PUSHL symbolIndex");
            routine.EmitRawLine("    PUSHB 0x04");
            routine.EmitRawLine($"    JUMPEQ {advanceWordLabel}");
            routine.EmitRawLine($"    JUMP {decodeSymbolLoopLabel}");
            routine.EmitRawLine($"{advanceWordLabel}:");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentByteOffset");
            routine.EmitRawLine("    PUSHL currentByteOffset");
            routine.EmitRawLine("    PUSHW 0x0100");
            routine.EmitRawLine($"    JUMPNE {decodeWordLoopLabel}");
            routine.EmitRawLine("    PUSHL currentRecord");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL currentRecord");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL currentByteOffset");
            routine.EmitRawLine($"    JUMP {decodeWordLoopLabel}");
            routine.EmitRawLine($"{restoreRecordSizeLabel}:");
            routine.EmitRawLine("    PUSHL savedRecordSize");
            routine.EmitRawLine("    PUTMG 0xCC");
            routine.EmitRawLine("    RET");
        }

        private static void EmitPackedSymbolDecode(GameBuilder.RoutineBuilder routine, string labelPrefix, string directOutputLocalName)
        {
            var haveEscapeStateLabel = $"have_escape_state_{labelPrefix}";
            var havePendingAlphabetLabel = $"have_pending_alphabet_{labelPrefix}";
            var decodeAlphabetOneLabel = $"decode_alphabet_one_{labelPrefix}";
            var decodeAlphabetTwoLabel = $"decode_alphabet_two_{labelPrefix}";
            var setEscapeHighLabel = $"set_escape_high_{labelPrefix}";
            var combineEscapeLabel = $"combine_escape_{labelPrefix}";
            var markAlphabetOneLabel = $"mark_alphabet_one_{labelPrefix}";
            var markAlphabetTwoLabel = $"mark_alphabet_two_{labelPrefix}";
            var beginEscapeLabel = $"begin_escape_{labelPrefix}";
            var decodeCompleteLabel = $"decode_complete_{labelPrefix}";

            routine.EmitRawLine("    PUSHL escapeState");
            routine.EmitRawLine($"    JUMPNZ {haveEscapeStateLabel}");
            routine.EmitRawLine("    PUSHL pendingAlphabet");
            routine.EmitRawLine($"    JUMPNZ {havePendingAlphabetLabel}");
            routine.EmitRawLine("    PUSHL currentSymbol");
            routine.EmitRawLine($"    PUSHB 0x{PackedTextEncoding.ShiftAlphabet1Symbol:X2}");
            routine.EmitRawLine($"    JUMPEQ {markAlphabetOneLabel}");
            routine.EmitRawLine("    PUSHL currentSymbol");
            routine.EmitRawLine($"    PUSHB 0x{PackedTextEncoding.ShiftAlphabet2Symbol:X2}");
            routine.EmitRawLine($"    JUMPEQ {markAlphabetTwoLabel}");
            routine.EmitRawLine("    PUSHL currentSymbol");
            routine.EmitRawLine($"    PUSHB 0x{PackedTextEncoding.EscapeAsciiSymbol:X2}");
            routine.EmitRawLine($"    JUMPEQ {beginEscapeLabel}");
            EmitAlphabetLookup(routine, GameBuilder.PackedTextAlphabet0Label, resultLocalName: "decodedChar");
            EmitPackedCharacterOutput(routine, directOutputLocalName, $"{labelPrefix}_base");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{haveEscapeStateLabel}:");
            routine.EmitRawLine("    PUSHL escapeState");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine($"    JUMPEQ {setEscapeHighLabel}");
            routine.EmitRawLine($"    JUMP {combineEscapeLabel}");

            routine.EmitRawLine($"{setEscapeHighLabel}:");
            routine.EmitRawLine("    PUSHL currentSymbol");
            routine.EmitRawLine("    PUTL escapeHighBits");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    PUTL escapeState");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{combineEscapeLabel}:");
            routine.EmitRawLine("    PUSHL escapeHighBits");
            routine.EmitRawLine("    PUSHB 0x05");
            routine.EmitRawLine("    ASHIFT");
            routine.EmitRawLine("    PUSHL currentSymbol");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL decodedChar");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL escapeState");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL escapeHighBits");
            EmitPackedCharacterOutput(routine, directOutputLocalName, $"{labelPrefix}_escape");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{havePendingAlphabetLabel}:");
            routine.EmitRawLine("    PUSHL pendingAlphabet");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine($"    JUMPEQ {decodeAlphabetOneLabel}");
            routine.EmitRawLine($"    JUMP {decodeAlphabetTwoLabel}");

            routine.EmitRawLine($"{decodeAlphabetOneLabel}:");
            EmitAlphabetLookup(routine, GameBuilder.PackedTextAlphabet1Label, resultLocalName: "decodedChar");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL pendingAlphabet");
            EmitPackedCharacterOutput(routine, directOutputLocalName, $"{labelPrefix}_alphabet1");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{decodeAlphabetTwoLabel}:");
            EmitAlphabetLookup(routine, GameBuilder.PackedTextAlphabet2Label, resultLocalName: "decodedChar");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    PUTL pendingAlphabet");
            EmitPackedCharacterOutput(routine, directOutputLocalName, $"{labelPrefix}_alphabet2");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{markAlphabetOneLabel}:");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUTL pendingAlphabet");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{markAlphabetTwoLabel}:");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    PUTL pendingAlphabet");
            routine.EmitRawLine($"    JUMP {decodeCompleteLabel}");

            routine.EmitRawLine($"{beginEscapeLabel}:");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    PUTL escapeState");

            routine.EmitRawLine($"{decodeCompleteLabel}:");
        }

        private static void EmitAlphabetLookup(GameBuilder.RoutineBuilder routine, string alphabetLabel, string resultLocalName)
        {
            routine.EmitRawLine($"    PUSHW {alphabetLabel}");
            routine.EmitRawLine("    PUSHL currentSymbol");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    LOADVB2");
            routine.EmitRawLine($"    PUTL {resultLocalName}");
        }

        private static void EmitPackedCharacterOutput(GameBuilder.RoutineBuilder routine, string directOutputLocalName, string labelSuffix)
        {
            var directOutputLabel = $"packed_direct_output_{labelSuffix}";
            var outputDoneLabel = $"packed_output_done_{labelSuffix}";

            routine.EmitRawLine("    PUSHL decodedChar");
            routine.EmitRawLine($"    PUSHL {directOutputLocalName}");
            routine.EmitRawLine($"    JUMPNZ {directOutputLabel}");
            routine.EmitRawLine("    CALL1 __BufferedPrintCharacter");
            routine.EmitRawLine($"    JUMP {outputDoneLabel}");
            routine.EmitRawLine($"{directOutputLabel}:");
            routine.EmitRawLine("    PRCHAR");
            routine.EmitRawLine($"{outputDoneLabel}:");
        }

        private static void EmitPrepareOutputWindowDescriptor(
            GameBuilder builder,
            GameBuilder.RoutineBuilder routine,
            string descriptorName)
        {
            var geometryName = builder.EnsureOutputWindowGeometryName();

            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    VPUTW_ 0x00");
            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine($"    PUSHB 0x{GameBuilder.MainTextTopRow:X2}");
            routine.EmitRawLine("    VPUTW_ 0x01");
            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine("    PUSH0");
            routine.EmitRawLine("    VPUTW_ 0x02");
            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine($"    PUSHB 0x{GameBuilder.MainTextTopRow:X2}");
            routine.EmitRawLine("    VPUTW_ 0x03");
            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenWidthSlot:X2}");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    VPUTW_ 0x04");
            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine($"    LOADMG 0x{GameBuilder.ScreenHeightSlot:X2}");
            routine.EmitRawLine("    VPUTW_ 0x05");
            routine.EmitRawLine($"    PUSHW {descriptorName}");
            routine.EmitRawLine($"    LOADG {builder.ConsoleColumnGlobalIndex}");
            routine.EmitRawLine("    VPUTW_ 0x00");
            routine.EmitRawLine($"    PUSHW {descriptorName}");
            routine.EmitRawLine($"    LOADG {builder.ConsoleRowGlobalIndex}");
            routine.EmitRawLine("    VPUTW_ 0x01");
            routine.EmitRawLine($"    PUSHW {descriptorName}");
            routine.EmitRawLine($"    LOADMG 0x{GameBuilder.TextAttributeSlot:X2}");
            routine.EmitRawLine("    VPUTW_ 0x02");
            routine.EmitRawLine($"    PUSHW {descriptorName}");
            routine.EmitRawLine($"    PUSHW {geometryName}");
            routine.EmitRawLine("    VPUTW_ 0x03");
        }

        private static void EmitSyncConsoleColumnFromDescriptor(
            GameBuilder builder,
            GameBuilder.RoutineBuilder routine,
            string descriptorName)
        {
            routine.EmitRawLine($"    PUSHW {descriptorName}");
            routine.EmitRawLine("    VLOADW_ 0x00");
            routine.EmitRawLine($"    STOREG {builder.ConsoleColumnGlobalIndex}");
            routine.EmitRawLine($"    LOADG {builder.ConsoleColumnGlobalIndex}");
            routine.EmitRawLine($"    PUTMG 0x{GameBuilder.CursorColumnSlot:X2}");
        }

        private static void EmitAdvancePropertyPointer(GameBuilder.RoutineBuilder routine)
        {
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    VLOADW_ 0x01");
            routine.EmitRawLine("    PUTL 6");
            routine.EmitRawLine("    PUSHL 6");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    DIV");
            routine.EmitRawLine("    PUSHL 5");
            routine.EmitRawLine("    PUSH2");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    ADD");
            routine.EmitRawLine("    PUTL 5");
            routine.EmitRawLine("    PUSHL 4");
            routine.EmitRawLine("    PUSH1");
            routine.EmitRawLine("    SUB");
            routine.EmitRawLine("    PUTL 4");
        }

        private sealed record RuntimeRoutineDefinition(
            string BaseName,
            IReadOnlyList<string> Dependencies,
            Action<GameBuilder, GameBuilder.RoutineBuilder> Emit,
            bool ReturnsValue = true);
    }
}