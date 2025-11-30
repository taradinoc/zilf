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

    class RuntimeLib
    {
        private readonly Dictionary<string, (string, string[])> functions = [];
        private readonly Dictionary<string, string> definitionSets = [];
        private readonly HashSet<string> usedFunctions = [];
        private readonly HashSet<string> usedDefinitionSets = [];

        public RuntimeLib()
        {
            foreach (var field in typeof(RuntimeLib).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var funcAttr = field.GetCustomAttribute<RuntimeFuncAttribute>();
                if (funcAttr != null)
                {
                    var funcName = field.Name;
                    var funcBody = (string)field.GetValue(this)!;
                    functions[funcName] = (funcBody, funcAttr.Dependencies);
                }
                else
                {
                    var constSetAttr = field.GetCustomAttribute<RuntimeDefinitionSetAttribute>();
                    if (constSetAttr != null)
                    {
                        var constSetName = field.Name;
                        var constSetBody = (string)field.GetValue(this)!;
                        definitionSets[constSetName] = constSetBody;
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

        public void DefineUsed(TextWriter writer)
        {
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
                writer.WriteLine(GameBuilder.INDENT + "section .text");
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
            MAX_OUTPUT_BUFFER = 65536

            glk_window_iterate = 0x20
            glk_window_open = 0x23
            glk_window_get_size = 0x25
            glk_window_set_arrangement = 0x26
            glk_window_get_parent = 0x29
            glk_window_clear = 0x2A
            glk_window_move_cursor = 0x2B
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
            glk_set_style = 0x86
            glk_char_to_lower = 0xA0
            glk_stylehint_set = 0xB0
            glk_select = 0xC0
            glk_request_line_event = 0xD0

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

            section .data
            gg_main_window_id: dd 0
            gg_status_window_id: dd 0
            gg_save_stream_id: dd 0
            gg_status_height: dd 0
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
            jz id -> rfalse
            ; Is it the save stream?
            jeq [gg_temp_word] GG_SAVE_STREAM_ROCK -> .found_save
            ; Keep looking
            jump .next_stream
        .found_save:
            ; Save save [sic] stream ID
            copy id -> [gg_save_stream_id]
            jump .next_stream
            ; No return needed, function is exited with rfalse above";

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
        [RuntimeFunc(nameof(glk_defines))]
        public const string direct_output = @"
            function
            local stream
            local table
            local width
            ; Enabling stream 3 (memory)?
            jeq stream 3 -> .enable_3
            ; Disabling stream 3?
            jeq stream -3 -> .disable_3
            ; TODO: implement other output streams
            return
        .enable_3:
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
            return
        .disable_3:
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

        [RuntimeFunc(nameof(glk_defines), nameof(tokenize_line), nameof(check_call))]
        public const string read_line = @"
            function
            local textbuf
            local lexbuf
            local evtype
            ; Request line input in the main window
            ; TODO: handle terminating characters
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
            ; Populate lexbuf
            callfii _rt_tokenize_line textbuf lexbuf
            return
        .got_arrange_event:
            callf update_status_line_hook
            jump .select";

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
            push style_Normal
            jump .set_style
        .fixed:
            push style_Preformatted
            jump .set_style
        .italic:        ; TODO: distinguish between italic and bold
        .bold:
            push style_Emphasized
            jump .set_style
        .reverse:
            push style_User1
        .set_style:
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
            jz addr -> rfalse       ; Not found
            ; Read value
            aload addr 0 -> push
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
    }
}