/* Copyright 2010-2025 Tara McGrew
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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Zilf.Emit.Glulx
{
    [AttributeUsage(AttributeTargets.Field)]
    class RuntimeFuncAttribute(params string[] dependencies) : Attribute
    {
        public string[] Dependencies => dependencies;
    }

    [AttributeUsage(AttributeTargets.Field)]
    class RuntimeDefinitionSetAttribute : Attribute
    {
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
    public class RuntimeLib
    {
        private readonly Dictionary<string, (string, string[])> functions = [];
        private readonly Dictionary<string, string> definitionSets = [];
        private readonly HashSet<string> usedFunctions = [];
        private readonly HashSet<string> usedDefinitionSets = [];

        public RuntimeLib()
        {
            var definitionFields = GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .OrderBy(static f => ReferenceEquals(f.DeclaringType, typeof(RuntimeLib)) ? 1 : 0);

            foreach (var field in definitionFields)
            {
                var funcAttr = field.GetCustomAttribute<RuntimeFuncAttribute>();
                if (funcAttr != null)
                {
                    var funcName = field.Name;
                    var funcBody = (string)field.GetValue(this)!;
                    if (!functions.ContainsKey(funcName))
                    {
                        functions[funcName] = (funcBody, funcAttr.Dependencies);
                    }
                }
                else
                {
                    var constSetAttr = field.GetCustomAttribute<RuntimeDefinitionSetAttribute>();
                    if (constSetAttr != null)
                    {
                        var constSetName = field.Name;
                        var constSetBody = (string)field.GetValue(this)!;
                        if (!definitionSets.ContainsKey(constSetName))
                        {
                            definitionSets[constSetName] = constSetBody;
                        }
                    }
                }
            }
        }

        public string Use(string name)
        {
            if (!functions.TryGetValue(name, out var funcInfo))
            {
                throw new ArgumentException($"No such runtime function: {name}");
            }

            UseDependency(name);

            void UseDependency(string dep)
            {
                if (functions.TryGetValue(dep, out var funcInfo) && usedFunctions.Add(dep))
                {
                    var (_, dependencies) = funcInfo;
                    foreach (var d in dependencies)
                    {
                        UseDependency(d);
                    }
                }
                else if (definitionSets.ContainsKey(dep))
                {
                    usedDefinitionSets.Add(dep);
                }
            }

            return $"_rt_{name}";
        }

        private static readonly char[] lineDelimiters = ['\r', '\n'];

        public void DefineUsed(TextWriter writer, string? codeSectionDirective = null)
        {
            codeSectionDirective ??= GameBuilder.INDENT + "section .text";

            foreach (var name in usedDefinitionSets.Order())
            {
                writer.WriteLine();
                writer.WriteLine("{0}; Definition set {1}", GameBuilder.INDENT, name);
                var body = definitionSets[name];
                foreach (var line in body.Split(lineDelimiters, StringSplitOptions.RemoveEmptyEntries))
                {
                    writer.WriteLine("{0}{1}", GameBuilder.INDENT, line.Trim());
                }
            }

            if (usedFunctions.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine(codeSectionDirective);
            }

            foreach (var name in usedFunctions.Order())
            {
                writer.WriteLine();
                writer.WriteLine("_rt_{0}:", name);
                var (body, _) = functions[name];
                foreach (var line in body.Split(lineDelimiters, StringSplitOptions.RemoveEmptyEntries))
                {
                    writer.WriteLine("{0}{1}", GameBuilder.INDENT, line.Trim());
                }
            }
        }

#region Glk

        [RuntimeDefinitionSet]
        public const string glk_defines = @"
            MAX_OUTPUT_BUFFER = 4096

            glk_window_iterate = 0x20
            glk_window_open = 0x23
            glk_window_get_size = 0x25
            glk_window_set_arrangement = 0x26
            glk_window_get_parent = 0x29
            glk_window_clear = 0x2A
            glk_window_move_cursor = 0x2B
            glk_window_set_echo_stream = 0x2D
            glk_set_window = 0x2F
            glk_stream_iterate = 0x40
            glk_stream_open_file = 0x42
            glk_stream_open_memory = 0x43
            glk_stream_close = 0x44
            glk_stream_get_position = 0x46
            glk_stream_set_current = 0x47
            glk_stream_get_current = 0x48
            glk_fileref_create_by_prompt = 0x62
            glk_fileref_destroy = 0x63
            glk_fileref_iterate = 0x64
            glk_put_char_stream = 0x81
            glk_put_buffer = 0x84
            glk_put_buffer_stream = 0x85
            glk_set_style = 0x86
            glk_get_line_stream = 0x91
            glk_char_to_lower = 0xA0
            glk_stylehint_set = 0xB0
            glk_select = 0xC0
            glk_request_line_event = 0xD0
            glk_set_terminators_line_event = 0x151

            wintype_AllTypes = 0
            wintype_Pair = 1
            wintype_Blank = 2
            wintype_TextBuffer = 3
            wintype_TextGrid = 4
            wintype_Graphics = 5

            winmethod_Left  = 0x00
            winmethod_Right = 0x01
            winmethod_Above = 0x02
            winmethod_Below = 0x03
            winmethod_DirMask = 0x0f

            winmethod_Fixed = 0x10
            winmethod_Proportional = 0x20
            winmethod_DivisionMask = 0xf0

            winmethod_Border = 0x000
            winmethod_NoBorder = 0x100
            winmethod_BorderMask = 0x100

            filemode_Write = 1
            filemode_Read = 2
            filemode_ReadWrite = 3
            filemode_WriteAppend = 5

            fileusage_Data = 0x00
            fileusage_SavedGame = 0x01
            fileusage_Transcript = 0x02
            fileusage_InputRecord = 0x03
            fileusage_TypeMask = 0x0f
            fileusage_TextMode = 0x100
            fileusage_BinaryMode = 0x000

            evtype_LineInput = 3
            evtype_Arrange = 5

            event_Type = 0
            event_Window = 1
            event_Val1 = 2
            event_Val2 = 3

            style_Normal = 0
            style_Emphasized = 1
            style_Preformatted = 2
            style_Header = 3
            style_Subheader = 4
            style_Alert = 5
            style_Note = 6
            style_BlockQuote = 7
            style_Input = 8
            style_User1 = 9
            style_User2 = 10

            stylehint_Indentation = 0
            stylehint_ParaIndentation = 1
            stylehint_Justification = 2
            stylehint_Size = 3
            stylehint_Weight = 4
            stylehint_Oblique = 5
            stylehint_Proportional = 6
            stylehint_TextColor = 7
            stylehint_BackColor = 8
            stylehint_ReverseColor = 9

            GG_MAIN_WINDOW_ROCK = 31415
            GG_STATUS_WINDOW_ROCK = 92653
            GG_SAVE_STREAM_ROCK = 58979
            GG_COMMAND_INPUT_STREAM_ROCK = 32384
            GG_COMMAND_OUTPUT_STREAM_ROCK = 62643
            GG_TRANSCRIPT_STREAM_ROCK = 38327
            GG_TRANSCRIPT_FILEREF_ROCK = 95028

            section .data
            gg_main_window_id: dd 0
            gg_status_window_id: dd 0
            gg_save_stream_id: dd 0
            gg_command_input_stream_id: dd 0
            gg_command_output_stream_id: dd 0
            gg_transcript_stream_id: dd 0
            gg_transcript_fileref_id: dd 0
            gg_status_height: dd 0
            gg_current_style: dd style_Normal
            gg_prev_stream_sp: dd 0

            section .bss
            gg_prev_stream_stack: resd 16
            gg_stream_table_stack: resd 16
            gg_event: resd 4
            gg_temp_word: resd 4";

        [RuntimeFunc(nameof(glk_defines), nameof(recover_glk))]
        public const string initialize_glk = @"
            function
            ; Select Glk I/O system
            setiosys 2 0
            ; Set up style_User1 as reverse video
            push 1
            push stylehint_ReverseColor
            push style_User1
            push wintype_AllTypes
            glk glk_stylehint_set 4
            ; Set up style_User2 as italic
            push 1
            push stylehint_Oblique
            push style_User2
            push wintype_AllTypes
            glk glk_stylehint_set 4
            ; Recover existing Glk windows, if any
            callf _rt_recover_glk
            ; Open a main window
            jnz [gg_main_window_id] -> .recovered_main_window
            push GG_MAIN_WINDOW_ROCK
            push wintype_TextBuffer
            push 0
            push 0
            push 0
            glk glk_window_open 5 -> [gg_main_window_id]
            jump .check_status_window
        .recovered_main_window:
            ; Clear the previously opened main window
            push [gg_main_window_id]
            glk glk_window_clear 1
        .check_status_window:
            ; Open a status window (initially 0 height)
            jnz [gg_status_window_id] -> .recovered_status_window
            push GG_STATUS_WINDOW_ROCK
            push wintype_TextGrid
            push 0
            push (winmethod_Above | winmethod_Fixed)
            push [gg_main_window_id]
            glk glk_window_open 5 -> [gg_status_window_id]
            copy 0 -> [gg_status_height]
        .recovered_status_window:
            ; Select the main window
            push [gg_main_window_id]
            glk glk_set_window 1
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string recover_glk = @"
            function
            local id
            ; Clear all stored Glk IDs
            copy 0 -> [gg_main_window_id]
            copy 0 -> [gg_status_window_id]
            copy 0 -> [gg_save_stream_id]
            copy 0 -> [gg_command_input_stream_id]
            copy 0 -> [gg_command_output_stream_id]
            copy 0 -> [gg_transcript_stream_id]
            copy 0 -> [gg_transcript_fileref_id]
            ; Look for windows we recognize
            copy 0 -> id
        .next_window:
            push gg_temp_word
            push id
            glk glk_window_iterate 2 -> id
            ; Are we done?
            jz id -> .windows_done
            ; Is it the main window?
            jeq [gg_temp_word] GG_MAIN_WINDOW_ROCK -> .found_main
            ; Is it the status window?
            jeq [gg_temp_word] GG_STATUS_WINDOW_ROCK -> .found_status
            ; Keep looking
            jump .next_window
        .found_main:
            ; Save main window ID
            copy id -> [gg_main_window_id]
            jump .next_window
        .found_status:
            ; Save status window ID
            copy id -> [gg_status_window_id]
            ; Measure height
            push gg_temp_word
            push 0
            push [gg_status_window_id]
            glk glk_window_get_size 3
            copy [gg_temp_word] -> [gg_status_height]
            jump .next_window
        .windows_done:
            ; Look for streams we recognize
            copy 0 -> id
        .next_stream:
            push gg_temp_word
            push id
            glk glk_stream_iterate 2 -> id
            ; Are we done?
            jz id -> .streams_done
            ; Is it the save stream?
            jeq [gg_temp_word] GG_SAVE_STREAM_ROCK -> .found_save
            ; Is it the command input stream?
            jeq [gg_temp_word] GG_COMMAND_INPUT_STREAM_ROCK -> .found_command_input
            ; Is it the command output stream?
            jeq [gg_temp_word] GG_COMMAND_OUTPUT_STREAM_ROCK -> .found_command_output
            ; Is it the transcript stream?
            jeq [gg_temp_word] GG_TRANSCRIPT_STREAM_ROCK -> .found_transcript_stream
            ; Keep looking
            jump .next_stream
        .found_save:
            ; Save save [sic] stream ID
            copy id -> [gg_save_stream_id]
            jump .next_stream
        .found_command_input:
            ; Save command input stream ID
            copy id -> [gg_command_input_stream_id]
            jump .next_stream
        .found_command_output:
            ; Save command output stream ID
            copy id -> [gg_command_output_stream_id]
            jump .next_stream
        .found_transcript_stream:
            ; Save transcript stream ID
            copy id -> [gg_transcript_stream_id]
            jump .next_stream
        .streams_done:
            ; Look for filerefs we recognize
            copy 0 -> id
        .next_fileref:
            push gg_temp_word
            push id
            glk glk_fileref_iterate 2 -> id
            ; Are we done?
            jz id -> .filerefs_done
            ; Is it the transcript fileref?
            jeq [gg_temp_word] GG_TRANSCRIPT_FILEREF_ROCK -> .found_transcript_fileref
            ; Keep looking
            jump .next_fileref
        .found_transcript_fileref:
            ; Save transcript fileref ID
            copy id -> [gg_transcript_fileref_id]
            jump .next_fileref
        .filerefs_done:
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string split_window = @"
            function
            local new_size
            local parent
            ; Make sure the status window exists and needs changing
            jz [gg_status_window_id] -> rfalse
            jeq new_size [gg_status_height] -> rfalse
            ; Get its parent window
            push [gg_status_window_id]
            glk glk_window_get_parent 1 -> parent
            ; Update the arrangement
            push 0
            push new_size
            push (winmethod_Above | winmethod_Fixed)
            push parent
            glk glk_window_set_arrangement 4
            ; Remember the new size
            copy new_size -> [gg_status_height]
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string select_window = @"
            function
            local window
            ; Status window?
            jeq window 1 -> .status
            ; No, main window
            push [gg_main_window_id]
            glk glk_set_window 1
            return
        .status:
            jz [gg_status_window_id] -> rfalse
            push [gg_status_window_id]
            glk glk_set_window 1
            return";

        [RuntimeFunc(nameof(glk_defines), nameof(split_window))]
        public const string clear_window = @"
            function
            local window
            ; Status window?
            jeq window 1 -> .status
            ; Main window?
            jeq window 0 -> .main
            ; Unsplit and clear screen?
            jeq window -1 -> .unsplit
            ; No, clear all but don't unsplit
            jz [gg_status_window_id] -> .main
            push [gg_status_window_id]
            glk glk_window_clear 1
            jump .main
        .status:
            jz [gg_status_window_id] -> rfalse
            push [gg_status_window_id]
            glk glk_window_clear 1
            return
        .unsplit:
            callfi _rt_split_window 0
            ; Fall through to .main
        .main:
            push [gg_main_window_id]
            glk glk_window_clear 1
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string move_cursor = @"
            function
            local row
            local col
            ; Select status window
            jz [gg_status_window_id] -> rfalse
            push [gg_status_window_id]
            glk glk_set_window 1
            ; Ensure nonzero row number
            jnz row -> .set_cursor
            copy 1 -> row
            copy 1 -> col
            ; Set cursor position
        .set_cursor:
            sub row 1 -> push
            sub col 1 -> push
            push [gg_status_window_id]
            glk glk_window_move_cursor 3
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string get_screen_width = @"
            function
            ; Get status window width
            jz [gg_status_window_id] -> rfalse
            push 0
            push gg_temp_word
            push [gg_status_window_id]
            glk glk_window_get_size 3
            return [gg_temp_word]";

        [RuntimeFunc(nameof(glk_defines))]
        public const string get_screen_height = @"
            function
            local total
            ; Get status window height
            jz [gg_status_window_id] -> rfalse
            push gg_temp_word
            push 0
            push [gg_status_window_id]
            glk glk_window_get_size 3
            copy [gg_temp_word] -> total
            ; Add main window height
            push gg_temp_word
            push 0
            push [gg_main_window_id]
            glk glk_window_get_size 3
            add total [gg_temp_word] -> total
            return total";

        /* DIROUT 3 writes a slightly different format in Glulx:
             offset 0   4 bytes   number of characters actually written
             offset 4   N bytes   the N characters written
           thus,
             the length is still at <GET ,OUTPUT-BUF 0>
             the first char is still at <GETB ,OUTPUT-BUF ,WORD-SIZE>
         */
        [RuntimeFunc(
            nameof(enable_output_stream_2), nameof(disable_output_stream_2),
            nameof(enable_output_stream_3), nameof(disable_output_stream_3),
            nameof(enable_output_stream_4), nameof(disable_output_stream_4))]
        public const string direct_output = @"
            function
            local stream
            local table
            local width
            ; Enabling stream 2 (transcript)?
            jeq stream 2 -> .enable_2
            ; Disabling stream 2?
            jeq stream -2 -> .disable_2
            ; Enabling stream 3 (memory)?
            jeq stream 3 -> .enable_3
            ; Disabling stream 3?
            jeq stream -3 -> .disable_3
            ; Enabling stream 4 (command record)?
            jeq stream 4 -> .enable_4
            ; Disabling stream 4?
            jeq stream -4 -> .disable_4
            ; TODO: implement other output streams
            return
        .enable_2:
            callf _rt_enable_output_stream_2
            return
        .disable_2:
            callf _rt_disable_output_stream_2
            return
        .enable_3:
            callfii _rt_enable_output_stream_3 table width
            return
        .disable_3:
            callf _rt_disable_output_stream_3
            return
        .enable_4:
            callf _rt_enable_output_stream_4
            return
        .disable_4:
            callf _rt_disable_output_stream_4
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string enable_output_stream_2 = @"
            function
            ; Are we already rolling?
            jnz [gg_transcript_stream_id] -> rfalse
            ; Do we already have a fileref?
            jnz [gg_transcript_fileref_id] -> .got_fileref
            ; No, prompt player to select a file
            push GG_TRANSCRIPT_FILEREF_ROCK
            push filemode_WriteAppend
            push (fileusage_Transcript | fileusage_TextMode)
            glk glk_fileref_create_by_prompt 3 -> [gg_transcript_fileref_id]
            jz [gg_transcript_fileref_id] -> rfalse
        .got_fileref:
            ; Open stream
            push GG_TRANSCRIPT_STREAM_ROCK
            push filemode_WriteAppend
            push [gg_transcript_fileref_id]
            glk glk_stream_open_file 3 -> [gg_transcript_stream_id]
            jz [gg_transcript_stream_id] -> rfalse
            push [gg_transcript_stream_id]
            push [gg_main_window_id]
            glk glk_window_set_echo_stream 2
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string disable_output_stream_2 = @"
            function
            ; Are we rolling?
            jz [gg_transcript_stream_id] -> rfalse
            ; Close stream
            push 0
            push [gg_transcript_stream_id]
            glk glk_stream_close 2
            copy 0 -> [gg_transcript_stream_id]
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string enable_output_stream_3 = @"
            function
            local table
            local width
            ; Push current stream and new table address
            glk glk_stream_get_current 0 -> push
            astore gg_prev_stream_stack [gg_prev_stream_sp] pop
            astore gg_stream_table_stack [gg_prev_stream_sp] table
            add [gg_prev_stream_sp] 1 -> [gg_prev_stream_sp]
            ; Open new memory stream
            push 0
            push filemode_Write
            push MAX_OUTPUT_BUFFER
            add table 4 -> push
            glk glk_stream_open_memory 4 -> push
            ; Select it
            glk glk_stream_set_current 1
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string disable_output_stream_3 = @"
            function
            local stream
            ; Get current stream and decrement stream stack pointer
            glk glk_stream_get_current 0 -> stream
            sub [gg_prev_stream_sp] 1 -> [gg_prev_stream_sp]
            ; Store number of characters written
            push stream
            glk glk_stream_get_position 1 -> push
            aload gg_stream_table_stack [gg_prev_stream_sp] -> push
            astore pop 0 pop
            ; Close current stream
            push 0
            push stream
            glk glk_stream_close 2
            ; Pop previous stream
            aload gg_prev_stream_stack [gg_prev_stream_sp] -> push
            ; Select it
            glk glk_stream_set_current 1
            return";

        [RuntimeFunc]
        public const string enable_output_stream_4 = @"
            function
            local fref
            ; Are we already rolling?
            jnz [gg_command_output_stream_id] -> rfalse
            ; Prompt player to select a file
            push 0
            push filemode_Write
            push (fileusage_InputRecord | fileusage_TextMode)
            glk glk_fileref_create_by_prompt 3 -> fref
            jz fref -> rfalse
            ; Open stream
            push GG_COMMAND_OUTPUT_STREAM_ROCK
            push filemode_Write
            push fref
            glk glk_stream_open_file 3 -> [gg_command_output_stream_id]
            push fref
            glk glk_fileref_destroy 1
            return";

        [RuntimeFunc(nameof(glk_defines))]
        public const string disable_output_stream_4 = @"
            function
            ; Are we rolling?
            jz [gg_command_output_stream_id] -> rfalse
            ; Close stream
            push 0
            push [gg_command_output_stream_id]
            glk glk_stream_close 2
            copy 0 -> [gg_command_output_stream_id]
            return";

        [RuntimeFunc]
        public const string direct_input = @"
            function
            local stream
            local fref
            ; Enabling stream 1 (recorded commands)?
            jeq stream 1 -> .enable_1
            ; Disabling stream 1?
            jeq stream -1 -> .disable_1
            ; There is no other input stream
            return
        .enable_1:
            ; Prompt player to select a file
            push 0
            push filemode_Read
            push (fileusage_InputRecord | fileusage_TextMode)
            glk glk_fileref_create_by_prompt 3 -> fref
            jz fref -> rfalse
            ; Open stream
            push GG_COMMAND_INPUT_STREAM_ROCK
            push filemode_Read
            push fref
            glk glk_stream_open_file 3 -> [gg_command_input_stream_id]
            push fref
            glk glk_fileref_destroy 1
            return
        .disable_1:
            jz [gg_command_input_stream_id] -> rfalse
            ; Close stream
            push 0
            push [gg_command_input_stream_id]
            glk glk_stream_close 2
            copy 0 -> [gg_command_input_stream_id]
            return";

        [RuntimeFunc(nameof(glk_defines), nameof(tokenize_line), nameof(check_call))]
        public const string read_line = @"
            function
            local textbuf
            local lexbuf
            local evtype
            local nchars
            local tchar
            ; Are we playing back recorded input?
            jnz [gg_command_input_stream_id] -> .read_from_stream
        .read_from_window:
            ; Request line input in the main window
            callf init_terminating_chars_hook
            push 0
            aloadb textbuf 0 -> push    ; size of buffer
            add textbuf 2 -> push       ; buffer (skip first 2 bytes)
            push [gg_main_window_id]
            glk glk_request_line_event 4
        .select:
            ; Get an event
            push gg_event
            glk glk_select 1
            ; Is it line input?
            aload gg_event event_Type -> evtype
            jeq evtype evtype_LineInput -> .got_line_event
            ; Is it arrange?
            jeq evtype evtype_Arrange -> .got_arrange_event
            ; No, then we don't care
            jump .select
        .got_line_event:
            ; Store line length
            aload gg_event event_Val1 -> push
            astoreb textbuf 1 pop
        .got_command:
            ; Populate lexbuf
            callfii _rt_tokenize_line textbuf lexbuf
            ; Are we recording commands?
            jz [gg_command_output_stream_id] -> .not_recording
            ; Write command to stream
            aloadb textbuf 1 -> push
            add textbuf 2 -> push
            push [gg_command_output_stream_id]
            glk glk_put_buffer_stream 3
            push 10
            push [gg_command_output_stream_id]
            glk glk_put_char_stream 2
        .not_recording:
            ; Convert terminating character if needed
            aload gg_event event_Val2 -> tchar
            jnz tchar -> .convert_tchar
            return 13
        .convert_tchar:
            callfi convert_terminating_char_hook tchar -> tchar
            return tchar
        .got_arrange_event:
            callf update_status_line_hook
            jump .select
        .read_from_stream:
            aloadb textbuf 0 -> push    ; size of buffer
            add textbuf 2 -> push       ; buffer (skip first 2 bytes)
            push [gg_command_input_stream_id]
            glk glk_get_line_stream 3 -> nchars
            ; Did we run out of recorded input?
            jz nchars -> .eof
            ; Trim trailing newline
            add nchars 1 -> push
            aloadb textbuf pop -> push
            jne pop 10 -> .trim_done
            sub nchars 1 -> nchars
        .trim_done:
            ; Store character count
            astoreb textbuf 1 nchars
            ; Echo command to window
            push style_Input
            glk glk_set_style 1
            push nchars
            add textbuf 2 -> push
            glk glk_put_buffer 2
            push style_Normal
            glk glk_set_style 1
            streamchar 10
            jump .got_command
        .eof:
            ; Close stream
            push 0
            push [gg_command_input_stream_id]
            glk glk_stream_close 2
            copy 0 -> [gg_command_input_stream_id]
            jump .read_from_window";

        [RuntimeDefinitionSet]
        public const string keycode_defines = @"
            keycode_Unknown = 0xffffffff
            keycode_Left = 0xfffffffe
            keycode_Right = 0xfffffffd
            keycode_Up = 0xfffffffc
            keycode_Down = 0xfffffffb
            keycode_Return = 0xfffffffa
            keycode_Delete = 0xfffffff9
            keycode_Escape = 0xfffffff8
            keycode_Tab = 0xfffffff7
            keycode_PageUp = 0xfffffff6
            keycode_PageDown = 0xfffffff5
            keycode_Home = 0xfffffff4
            keycode_End = 0xfffffff3
            keycode_Func1 = 0xffffffef
            keycode_Func2 = 0xffffffee
            keycode_Func3 = 0xffffffed
            keycode_Func4 = 0xffffffec
            keycode_Func5 = 0xffffffeb
            keycode_Func6 = 0xffffffea
            keycode_Func7 = 0xffffffe9
            keycode_Func8 = 0xffffffe8
            keycode_Func9 = 0xffffffe7
            keycode_Func10 = 0xffffffe6
            keycode_Func11 = 0xffffffe5
            keycode_Func12 = 0xffffffe4

            section .data
        keycode_map:
            db 129
            dd keycode_Up
            db 130
            dd keycode_Down
            db 131
            dd keycode_Left
            db 132
            dd keycode_Right
            db 133
            dd keycode_Func1
            db 134
            dd keycode_Func2
            db 135
            dd keycode_Func3
            db 136
            dd keycode_Func4
            db 137
            dd keycode_Func5
            db 138
            dd keycode_Func6
            db 139
            dd keycode_Func7
            db 140
            dd keycode_Func8
            db 141
            dd keycode_Func9
            db 142
            dd keycode_Func10
            db 143
            dd keycode_Func11
            db 144
            dd keycode_Func12
            db 145
            dd keycode_Unknown      ; keypad 0
            db 146
            dd keycode_Unknown      ; keypad 1
            db 147
            dd keycode_Unknown      ; keypad 2
            db 148
            dd keycode_Unknown      ; keypad 3
            db 149
            dd keycode_Unknown      ; keypad 4
            db 150
            dd keycode_Unknown      ; keypad 5
            db 151
            dd keycode_Unknown      ; keypad 6
            db 152
            dd keycode_Unknown      ; keypad 7
            db 153
            dd keycode_Unknown      ; keypad 8
            db 154
            dd keycode_Unknown      ; keypad 9
            db 252
            dd keycode_Unknown      ; menu click
            db 253
            dd keycode_Unknown      ; double click
            db 254
            dd keycode_Unknown      ; single click
            KEYCODE_MAP_COUNT = 29";

        [RuntimeFunc(nameof(glk_defines), nameof(keycode_defines))]
        public const string init_terminating_chars = @"
            function
            local ztable
            local i
            local zkey
            local gkey
            ; Convert Z key codes into Glk key codes and copy into buffer
            copy 0 -> i
        .convert_next:
            aloadb ztable i -> zkey
            jz zkey -> .conversion_done
            binarysearch zkey 1 keycode_map 5 KEYCODE_MAP_COUNT 0 0 -> gkey
            add gkey 1 -> gkey
            aload gkey 0 -> gkey
            astore terminating_chars_translations i gkey
            add i 1 -> i
            jump .convert_next
        .conversion_done:
            push i
            push terminating_chars_translations
            push [gg_main_window_id]
            glk glk_set_terminators_line_event 3
            return";

        [RuntimeFunc(nameof(keycode_defines))]
        public const string convert_terminating_char = @"
            function
            local gkey
            local zkey
            ; Convert Glk key code to Z key code
            linearsearch gkey 4 keycode_map 5 KEYCODE_MAP_COUNT 1 0 -> zkey
            jz zkey -> .not_found
            aloadb zkey 0 -> zkey
            return zkey
        .not_found:
            return 13";

        [RuntimeFunc(nameof(glk_defines))]
        public const string output_style = @"
            function
            local style
            ; Fixed pitch?
            bitand style 8 -> push
            jnz pop -> .fixed
            ; Italic?
            bitand style 4 -> push
            jnz pop -> .italic
            ; Bold?
            bitand style 2 -> push
            jnz pop -> .bold
            ; Reverse video?
            bitand style 1 -> push
            jnz pop -> .reverse
            ; Roman
            copy style_Normal -> [gg_current_style]
            jump .set_style
        .fixed:
            copy style_Preformatted -> [gg_current_style]
            jump .set_style
        .italic:
            copy style_User2 -> [gg_current_style]
            jump .set_style
        .bold:
            copy style_Emphasized -> [gg_current_style]
            jump .set_style
        .reverse:
            copy style_User1 -> [gg_current_style]
        .set_style:
            push [gg_current_style]
            glk glk_set_style 1
            return";

        [RuntimeFunc(nameof(glk_defines), nameof(translate_save_result))]
        public const string save_game = @"
            function
            local fref
            local res
            ; Prompt player to select a file
            push 0
            push filemode_Write
            push fileusage_SavedGame
            glk glk_fileref_create_by_prompt 3 -> fref
            jz fref -> rfalse                   ; failure
            ; Open stream
            push GG_SAVE_STREAM_ROCK
            push filemode_Write
            push fref
            glk glk_stream_open_file 3 -> [gg_save_stream_id]
            push fref
            glk glk_fileref_destroy 1
            jz [gg_save_stream_id] -> rfalse    ; failure
            ; Save state
            save [gg_save_stream_id] -> res
            callfi _rt_translate_save_result res -> res     ; may call recover_glk
            ; Close stream
            push 0
            push [gg_save_stream_id]
            glk glk_stream_close 2
            copy 0 -> [gg_save_stream_id]
            return res";

        [RuntimeFunc(nameof(glk_defines), nameof(translate_save_result))]
        public const string restore_game = @"
            function
            local fref
            local res
            ; Prompt player to select a file
            push 0
            push filemode_Read
            push fileusage_SavedGame
            glk glk_fileref_create_by_prompt 3 -> fref
            jz fref -> rfalse                   ; failure
            ; Open stream
            push GG_SAVE_STREAM_ROCK
            push filemode_Read
            push fref
            glk glk_stream_open_file 3 -> [gg_save_stream_id]
            push fref
            glk glk_fileref_destroy 1
            jz [gg_save_stream_id] -> rfalse    ; failure
            ; Restore state
            restore [gg_save_stream_id] -> res
            callfi _rt_translate_save_result res -> res
            ; Close stream
            push 0
            push [gg_save_stream_id]
            glk glk_stream_close 2
            copy 0 -> [gg_save_stream_id]
            return res";

        [RuntimeFunc(nameof(glk_defines))]
        public const string get_lowcore_flags = @"
            function
            local result
            copy 0 -> result
            ; Bit 0: Set when transcripting is on
            jz [gg_transcript_stream_id] -> .no_transcript
            bitor result 1 -> result
        .no_transcript:
            ; Bit 1: Set when fixed-pitch printing is on
            jne [gg_current_style] style_Preformatted -> .no_fixed_pitch
            bitor result 2 -> result
        .no_fixed_pitch:
            ; Bit 4: Set when UNDO is available
            gestalt 3 0 -> push
            jz pop -> .no_undo
            bitor result 16 -> result
        .no_undo:
            return result";

        [RuntimeFunc(nameof(glk_defines), nameof(direct_output))]
        public const string set_lowcore_flags = @"
            function
            local value
            ; Bit 0: Set to enable transcripting
            bitand value 1 -> push
            jz pop -> .disable_transcript
            callfi _rt_direct_output 2
            jump .bit_0_done
        .disable_transcript:
            callfi _rt_direct_output -2
        .bit_0_done:
            ; Bit 1: Set to enable fixed-pitch printing
            bitand value 2 -> push
            jz pop -> .disable_fixed_pitch
            copy style_Preformatted -> [gg_current_style]
            push style_Preformatted
            glk glk_set_style 1
            jump .bit_1_done
        .disable_fixed_pitch:
            jne [gg_current_style] style_Preformatted -> .bit_1_done
            copy style_Normal -> [gg_current_style]
            push style_Normal
            glk glk_set_style 1
        .bit_1_done:
            ; Bit 4: Ignore (game can't enable/disable UNDO)
            return";

        #endregion

        #region Objects

        [RuntimeDefinitionSet]
        public const string object_defines = @"
            typeid_Object = 0x70
            NUM_FLAG_BYTES = 7
            objfield_NextObject = 2
            objfield_Desc = 3
            objfield_PropTable = 4
            objfield_Parent = 5
            objfield_Sibling = 6
            objfield_Child = 7
            proptable_Max = 0
            propentry_Address = 1";

        [RuntimeFunc(nameof(object_defines))]
        public const string move_object = @"
            function
            local obj
            local new_parent
            local old_parent
            local old_parent_child
            local sibling
            ; Unlink obj from old parent
            aload obj objfield_Parent -> old_parent
            jz old_parent -> .unlink_done
            ; Is obj the first child of its parent?
            aload old_parent objfield_Child -> old_parent_child
            jne old_parent_child obj -> .unlink_middle
            ; Yes, make its sibling the first child instead
            aload obj objfield_Sibling -> push
            astore old_parent objfield_Child pop
            jump .unlink_done
        .unlink_middle:
            ; No, find its older sibling
            ; Is this the older sibling?
            aload old_parent_child objfield_Sibling -> sibling
            jne sibling obj -> .unlink_middle_next
            ; Yes, unlink obj
            aload obj objfield_Sibling -> push
            astore old_parent_child objfield_Sibling pop
            jump .unlink_done
        .unlink_middle_next:
            ; No, keep looking
            copy sibling -> old_parent_child
            jump .unlink_middle
        .unlink_done:
            ; Link obj into new parent
            aload new_parent objfield_Child -> push
            astore obj objfield_Sibling pop
            astore obj objfield_Parent new_parent
            astore new_parent objfield_Child obj
            return";

        [RuntimeFunc(nameof(object_defines))]
        public const string remove_object = @"
            function
            local obj
            local old_parent
            local old_parent_child
            local sibling
            ; Unlink obj from old parent
            aload obj objfield_Parent -> old_parent
            jz old_parent -> rfalse
            ; Is obj the first child of its parent?
            aload old_parent objfield_Child -> old_parent_child
            jne old_parent_child obj -> .unlink_middle
            ; Yes, make its sibling the first child instead
            aload obj objfield_Sibling -> push
            astore old_parent objfield_Child pop
            jump .unlink_done
        .unlink_middle:
            ; No, find its older sibling
            ; Is this the older sibling?
            aload old_parent_child objfield_Sibling -> sibling
            jne sibling obj -> .unlink_middle_next
            ; Yes, unlink obj
            aload obj objfield_Sibling -> push
            astore old_parent_child objfield_Sibling pop
            jump .unlink_done
        .unlink_middle_next:
            ; No, keep looking
            copy sibling -> old_parent_child
            jump .unlink_middle
        .unlink_done:
            astore obj objfield_Sibling 0
            astore obj objfield_Parent 0
            return";

        [RuntimeFunc(nameof(object_defines))]
        public const string get_property_entry = @"
            function
            local obj
            local prop
            local ptbl
            local max
            ; Get object property table
            aload obj objfield_PropTable -> ptbl
            jz ptbl -> rfalse
            ; Get property count
            aload ptbl proptable_Max -> max
            ; Search for property entry
            add ptbl 4 -> ptbl
            binarysearch prop 2 ptbl 10 max 0 0 -> push
            return pop";

        [RuntimeFunc(nameof(object_defines), nameof(get_property_entry))]
        public const string get_property_address = @"
            function
            local obj
            local prop
            local entry
            ; Find property entry
            callfii _rt_get_property_entry obj prop -> entry
            jz entry -> rfalse
            ; Return address from entry
            aload entry propentry_Address -> push
            return pop";

        [RuntimeFunc]
        public const string get_property_size = @"
            function
            local addr
            ; Property size is stored in a short just before the property data
            aloads addr -1 -> push
            return pop";

        [RuntimeFunc(nameof(get_property_address))]
        public const string put_property = @"
            function
            local obj
            local prop
            local value
            local addr
            ; Get property address
            callfii _rt_get_property_address obj prop -> addr
            jz addr -> rfalse       ; Not found
            ; Write value
            astore addr 0 value
            return";

        [RuntimeFunc(nameof(get_property_address))]
        public const string get_property = @"
            function
            local obj
            local prop
            local addr
            ; Get property address
            callfii _rt_get_property_address obj prop -> addr
            jz addr -> .not_found
            ; Read value
            aload addr 0 -> push
            return pop
        .not_found:
            ; Look for default value
            binarysearch prop 2 property_defaults_table 6 [property_defaults_count] 4 0 -> addr
            jz addr -> rfalse       ; Default to zero
            aload addr 0 -> push
            return pop";

        [RuntimeFunc(nameof(object_defines))]
        public const string get_next_property = @"
            function
            local obj
            local prop
            local entry
            local ptbl
            local max
            ; Get object property table
            aload obj objfield_PropTable -> ptbl
            jz ptbl -> rfalse
            ; Finding the first property?
            jz prop -> .want_first
            ; Get property count
            aload ptbl proptable_Max -> max
            ; Search for property entry
            add ptbl 4 -> ptbl
            binarysearch prop 2 ptbl 10 max 0 0 -> entry
            jz entry -> rfalse      ; shouldn't happen
            ; Advance to the next entry
            add entry 10 -> entry
            ; Are we at the end?
            mul max 10 -> push
            add ptbl pop -> push
            jge entry pop -> rfalse
            jump .done
        .want_first:
            ; Get the first entry
            add ptbl 4 -> entry
        .done:
            ; Return the property number
            aloads entry 0 -> push
            return pop";

        [RuntimeFunc(nameof(object_defines))]
        public const string get_child = @"
            function
            local obj
            aload obj objfield_Child -> push
            return pop";

        [RuntimeFunc(nameof(object_defines))]
        public const string get_sibling = @"
            function
            local obj
            aload obj objfield_Sibling -> push
            return pop";

        [RuntimeFunc(nameof(object_defines))]
        public const string get_parent = @"
            function
            local obj
            aload obj objfield_Parent -> push
            return pop";

        [RuntimeFunc(nameof(object_defines))]
        public const string test_parent = @"
            function
            local obj
            local parent
            aload obj objfield_Parent -> push
            jeq pop parent -> rtrue
            return 0";

        [RuntimeFunc(nameof(object_defines))]
        public const string print_object = @"
            function
            local obj
            aload obj objfield_Desc -> push
            streamstr pop
            return";

        [RuntimeFunc]
        public const string test_flag = @"
            function
            local obj
            local flag
            add flag 8 -> flag
            aloadbit obj flag -> push
            return pop";

        [RuntimeFunc]
        public const string set_flag = @"
            function
            local obj
            local flag
            local value
            add flag 8 -> flag
            astorebit obj flag value
            return";
#endregion

#region Vocab

        [RuntimeDefinitionSet]
        public const string vocab_defines = @"
            ; word offsets after skipping VOCAB_RESOLUTION bytes
            vocab_PartOfSpeech = 0
            vocab_Value1 = 1
            vocab_Value2 = 2

            section .bss
            tokenize_key_buf: resb VOCAB_RESOLUTION";

        [RuntimeFunc(nameof(vocab_defines))]
        public const string print_vocab_word = @"
            function
            local word
            local i
            local c
            add word 1 -> word   ; skip type ID
            copy 0 -> i
        .next_char:
            aloadb word i -> c
            jz c -> rfalse
            streamchar c
            add i 1 -> i
            jge i VOCAB_RESOLUTION -> rfalse
            jump .next_char";

        // Glulx textbuf/lexbuf format:
        // - textbuf is same as the Z-machine:
        //    1 byte max length (input)
        //    1 byte actual length (output)
        //    then 1 byte per char with no terminator (output)
        // - lexbuf is different:
        //    1 byte max length (input)
        //    1 byte actual length (output)
        //    2 bytes padding
        //    then for each lexed word, an 8 byte record (output):
        //     1 word vocab address
        //     1 byte length
        //     1 byte offset in textbuf
        //     2 bytes padding
        // - thus, for word N in lexbuf:
        //    the vocab address is still at <GET ,LEXBUF <- <* .N 2> 1>>
        //    the length is still at <GETB ,LEXBUF <* .N <* ,WORD-SIZE 2>>>
        //    the offset is still at <GETB ,LEXBUF <+ <* .N <* ,WORD-SIZE 2>> 1>>
        [RuntimeFunc(nameof(glk_defines), nameof(vocab_defines))]
        public const string tokenize_line = @"
            function
            local textbuf
            local lexbuf
            local bx
            local cx
            local len
            local lexlen
            local numwords
            local sibreaks
            local sibreakcnt
            local ch
            local idx
            local wx
            local wlen
            local wpos
            ; Count self-inserting breaks
            aloadb si_breaks 0 -> sibreakcnt
            add si_breaks 1 -> sibreaks
            ; Get buffer lengths and advance past
            aloadb textbuf 1 -> len
            add textbuf 2 -> textbuf
            aloadb lexbuf 0 -> lexlen
            ; Split the line into words, respecting si_breaks (delimiter characters that form their own words)
            copy 0 -> cx
            copy 0 -> numwords
            ; Are there more characters?
        .check_more_chars:
            jge cx len -> .split_done
            ; Advance past spaces
            aloadb textbuf cx -> push
            jne pop ` ` -> .past_spaces
            add cx 1 -> cx
            jump .check_more_chars
        .past_spaces:
            copy cx -> bx
            ; Is this a self-inserting break?
            aloadb textbuf cx -> push
            linearsearch pop 1 sibreaks 1 sibreakcnt 0 0 -> push
            jnz pop -> .word_is_break
            ; No, advance past characters until we find a space or a self-inserting break
        .look_for_space_or_break:
            jge cx len -> .found_space_or_break
            aloadb textbuf cx -> ch
            jeq ch ` ` -> .found_space_or_break
            linearsearch ch 1 sibreaks 1 sibreakcnt 0 0 -> push
            jnz pop -> .found_space_or_break
            ; Keep looking
            add cx 1 -> cx
            jump .look_for_space_or_break
        .word_is_break:
            add cx 1 -> cx
        .found_space_or_break:
            add numwords 1 -> numwords
            ; Fill in word length
            mul numwords 8 -> idx
            sub cx bx -> push
            astoreb lexbuf idx pop
            ; Fill in word offset
            add idx 1 -> idx
            add bx 2 -> push
            astoreb lexbuf idx pop
            ; Next word
            jge numwords lexlen -> .split_done
            jump .check_more_chars
        .split_done:
            ; Store word count
            astoreb lexbuf 1 numwords

            ; Look up each word in the vocab table
            copy 1 -> wx
        .check_more_words:
            jgt wx numwords -> rfalse
            mul wx 8 -> idx
            aloadb lexbuf idx -> wlen
            add idx 1 -> idx
            aloadb lexbuf idx -> wpos
            ; Copy into tokenize_key_buf, in lowercase, with zero-padding
            jle wlen VOCAB_RESOLUTION -> .word_fits
            copy VOCAB_RESOLUTION -> wlen
        .word_fits:
            sub wpos 2 -> cx
            ; Copy and lowercase
            copy 0 -> idx
        .copy_next_char:
            jge idx wlen -> .copy_done
            add cx idx -> push
            aloadb textbuf pop -> push
            glk glk_char_to_lower 1 -> push
            astoreb tokenize_key_buf idx pop
            add idx 1 -> idx
            jump .copy_next_char
        .copy_done:
            ; Pad with zeros
        .pad_next_char:
            jge idx VOCAB_RESOLUTION -> .padding_done
            astoreb tokenize_key_buf idx 0
            add idx 1 -> idx
            jump .pad_next_char
        .padding_done:
            binarysearch tokenize_key_buf VOCAB_RESOLUTION vocab_entries [vocab_entry_length] [vocab_entry_count] 1 1 -> push
            ; Store vocab address
            mul wx 2 -> idx
            sub idx 1 -> idx
            astore lexbuf idx pop
            add wx 1 -> wx
            jump .check_more_words
            ; No return needed, the function is exited with rfalse above";

#endregion

#region Misc

        [RuntimeFunc]
        public const string scan_table = @"
            function
            local value
            local table
            local length
            local form
            local keysize
            local structsize
            ; form has the $80 bit set for words, clear for bytes
            copy 4 -> keysize
            bitand form 0x80 -> push
            jnz pop -> .size_done
            copy 1 -> keysize
        .size_done:
            bitand form 0x7F -> structsize
            linearsearch value keysize table structsize length 0 0 -> push
            return pop";

        [RuntimeFunc]
        public const string copy_table = @"
            function
            local src
            local dest
            local size
            local i
            ; Clear to zero?
            jz dest -> .use_mzero
            ; Positive?
            jge size 0 -> .use_mcopy
            ; No, force copying forwards
            neg size -> size
            copy 0 -> i
        .check_more_bytes:
            jge i size -> rfalse
            aloadb src i -> push
            astoreb dest i pop
            add i 1 -> i
            jump .check_more_bytes
        .use_mzero:
            mzero size src
            return
        .use_mcopy:
            mcopy size src dest
            return";

        [RuntimeFunc]
        public const string check_call = @"
            func_va
            local argc
            local func
            pull -> argc
            pull -> func
            jz func -> rfalse
            sub argc 1 -> argc
            tailcall func argc";

        [RuntimeFunc(nameof(recover_glk))]
        public const string translate_save_result = @"
            function
            local value
            ; Glulx 0 -> success -> Z-machine 1
            jz value -> rtrue
            ; Glulx 1 -> failure -> Z-machine 0
            jeq value 1 -> rfalse
            ; Glulx -1 -> restoring -> Z-machine 2
            callf _rt_recover_glk
            return 2";

        [RuntimeFunc]
        public const string random_number = @"
            function
            local range
            ; Reseeding?
            jz range -> .seed_random
            jlt range 0 -> .seed_predictable
            ; No, rolling a number
            random range -> push
            add 1 pop -> push   ; Z-machine returns 1..N, Glulx returns 0..N-1
            return pop
        .seed_random:
            setrandom 0
            return 0
        .seed_predictable:
            neg range -> range
            setrandom range
            return 0";

#endregion

#region Table Write Tracing

        [RuntimeFunc(nameof(trace_check_write_word))]
        public const string trace_word_write = @"
            function
            local base_addr
            local offset
            local value
            ; Check against traced tables before the write
            callfiii _rt_trace_check_write_word base_addr offset value
            ; Perform the actual write
            astore base_addr offset value
            return";

        [RuntimeFunc(nameof(trace_check_write_byte))]
        public const string trace_byte_write = @"
            function
            local base_addr
            local offset
            local value
            ; Check against traced tables before the write
            callfiii _rt_trace_check_write_byte base_addr offset value
            ; Perform the actual write
            astoreb base_addr offset value
            return";

        [RuntimeFunc(nameof(trace_report_word))]
        public const string trace_check_write_word = @"
            function
            local base_addr
            local offset
            local value
            local traced_tables
            local count
            local i
            local table_addr
            local table_size
            local table_name
            local table_end
            local write_start
            local write_end

            ; Calculate write address range for word (4 bytes)
            mul offset 4 -> push
            add base_addr pop -> write_start
            add write_start 4 -> write_end

            ; Load the _traced_tables pointer
            copy _traced_tables -> traced_tables

            ; Load count
            aload traced_tables 0 -> count
            jz count -> .done

            ; Loop through each traced table entry
            copy 0 -> i
        .check_next:
            jge i count -> .done

            ; Each entry is 3 words: address, size, name string
            mul i 3 -> push
            add pop 1 -> push
            aload traced_tables pop -> table_addr
            mul i 3 -> push
            add pop 2 -> push
            aload traced_tables pop -> table_size
            mul i 3 -> push
            add pop 3 -> push
            aload traced_tables pop -> table_name

            ; Calculate table_end
            add table_addr table_size -> table_end

            ; Check for overlap: write overlaps table if write_start < table_end AND write_end > table_addr
            jge write_start table_end -> .no_overlap
            jle write_end table_addr -> .no_overlap

            ; Overlap detected - calculate offset within table and call report
            ; offset_in_table = write_start - table_addr (may be negative for partial overlap before table)
            sub write_start table_addr -> push
            copy value -> push
            copy write_end -> push
            copy write_start -> push
            copy table_addr -> push
            copy table_name -> push
            call _rt_trace_report_word 6 -> push

        .no_overlap:
            add i 1 -> i
            jump .check_next

        .done:
            return";

        [RuntimeFunc(nameof(trace_report_byte))]
        public const string trace_check_write_byte = @"
            function
            local base_addr
            local offset
            local value
            local traced_tables
            local count
            local i
            local table_addr
            local table_size
            local table_name
            local table_end
            local write_addr

            ; Calculate write address for byte
            add base_addr offset -> write_addr

            ; Load the _traced_tables pointer
            copy _traced_tables -> traced_tables

            ; Load count
            aload traced_tables 0 -> count
            jz count -> .done

            ; Loop through each traced table entry
            copy 0 -> i
        .check_next:
            jge i count -> .done

            ; Each entry is 3 words: address, size, name string
            mul i 3 -> push
            add pop 1 -> push
            aload traced_tables pop -> table_addr
            mul i 3 -> push
            add pop 2 -> push
            aload traced_tables pop -> table_size
            mul i 3 -> push
            add pop 3 -> push
            aload traced_tables pop -> table_name

            ; Calculate table_end
            add table_addr table_size -> table_end

            ; Check if write_addr is within table bounds
            jlt write_addr table_addr -> .no_overlap
            jge write_addr table_end -> .no_overlap

            ; Write is within table - calculate offset within table and call report
            sub write_addr table_addr -> push
            copy value -> push
            copy write_addr -> push
            copy table_name -> push
            call _rt_trace_report_byte 4 -> push

        .no_overlap:
            add i 1 -> i
            jump .check_next

        .done:
            return";

        [RuntimeFunc]
        public const string trace_report_word = @"
            function
            local table_name
            local table_addr
            local write_start
            local write_end
            local value
            local offset_in_table
            local word_offset
            local misalignment

            ; Print: ""[TRACE] Write to <table_name>""
            streamstr _trace_msg_prefix
            streamstr table_name

            ; Calculate word offset and misalignment
            ; word_offset = offset_in_table / 4
            div offset_in_table 4 -> word_offset
            ; misalignment = offset_in_table % 4
            mod offset_in_table 4 -> misalignment

            ; Print "" word <n>"" or "" word <n> (+<m> bytes)""
            streamstr _trace_msg_word
            streamnum word_offset
            jz misalignment -> .no_misalign
            streamstr _trace_msg_unaligned
            streamnum misalignment
            streamstr _trace_msg_bytes_suffix
        .no_misalign:

            ; Print "" (addr <start>-<end>)""
            streamstr _trace_msg_addr
            streamnum write_start
            streamstr _trace_msg_dash
            sub write_end 1 -> push
            streamnum pop
            streamstr _trace_msg_value
            streamnum value
            streamstr _trace_msg_newline
            return";

        [RuntimeFunc]
        public const string trace_report_byte = @"
            function
            local table_name
            local write_addr
            local value
            local offset_in_table

            ; Print: ""[TRACE] Write to <table_name>""
            streamstr _trace_msg_prefix
            streamstr table_name

            ; Print "" byte <n>""
            streamstr _trace_msg_byte
            streamnum offset_in_table

            ; Print "" (addr <addr>)""
            streamstr _trace_msg_addr
            streamnum write_addr
            streamstr _trace_msg_value
            streamnum value
            streamstr _trace_msg_newline
            return";

#endregion
    }
}