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

namespace Zilf.Emit.Glulx
{
    /// <summary>
    /// Glulx runtime library variant for the Glulx16 target. This uses the same
    /// function names as <see cref="RuntimeLib"/> but supplies replacements for
    /// vocabulary handling that rely on a string table instead of inline encoded
    /// words. Object handling is also overridden to match the Z-style object
    /// layout emitted by <see cref="GameBuilder16"/>.
    /// </summary>
    class RuntimeLib16 : RuntimeLib
    {
        #region Arithmetic and tables

        [RuntimeFunc]
        public const string add16 = @"
            function
            local a
            local b
            add a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string sub16 = @"
            function
            local a
            local b
            sub a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string mul16 = @"
            function
            local a
            local b
            mul a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string div16 = @"
            function
            local a
            local b
            sexs a -> a
            sexs b -> b
            div a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string mod16 = @"
            function
            local a
            local b
            sexs a -> a
            sexs b -> b
            mod a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string band16 = @"
            function
            local a
            local b
            bitand a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string bor16 = @"
            function
            local a
            local b
            bitor a b -> a
            bitand a 0xFFFF -> a
            return a";

        [RuntimeFunc]
        public const string bcom16 = @"
            function
            local value
            bitxor value 0xFFFF -> value
            return value";

        [RuntimeFunc]
        public const string neg16 = @"
            function
            local value
            neg value -> value
            bitand value 0xFFFF -> value
            return value";

        [RuntimeFunc]
        public const string cmp16 = @"
            function
            local a
            local b
            sexs a -> a
            sexs b -> b
            sub a b -> a
            return a";

        [RuntimeFunc(nameof(get_header_word))]
        public const string getword16 = @"
            function
            local table
            local offset
            local address
            sexs offset -> offset       ; offsets are signed
            ; Trap header reads
            mul offset 2 -> push
            add table pop -> address
            jlt address 64 -> .header
            aloads table offset -> push
            return pop
        .header:
            callfi _rt_get_header_word address -> push
            return pop";

        [RuntimeFunc]
        public const string putword16 = @"
            function
            local table
            local offset
            local value
            sexs offset -> offset       ; offsets are signed
            astores table offset value
            return";

        [RuntimeFunc(nameof(get_header_byte))]
        public const string getbyte16 = @"
            function
            local table
            local offset
            local address
            sexs offset -> offset
            ; Trap header reads
            add table offset -> address
            jlt address 64 -> .header
            aloadb table offset -> push
            return pop
        .header:
            callfi _rt_get_header_byte address -> push
            return pop";

        [RuntimeFunc]
        public const string get_header_byte = @"
            function
            local address
            ; Serial number in bytes 18-23
            jlt address 18 -> .not_serial
            jgt address 23 -> .not_serial
            sub address 18 -> address
            aloadb metadata_serial address -> push
            return pop
        .not_serial:
            return 0";

        [RuntimeFunc]
        public const string get_header_word = @"
            function
            local address
            ; Release number in bytes 2-3
            jne address 2 -> .not_release
            aloads metadata_releaseid 0 -> push
            return pop
        .not_release:
            return 0";

        #endregion

        #region Objects

        [RuntimeDefinitionSet]
        public new const string object_defines = @"
            obj_entry_len = GL16_OBJ_ENTRY_LEN
            obj_parent_off = GL16_OBJ_PARENT_OFF
            obj_sibling_off = GL16_OBJ_SIBLING_OFF
            obj_child_off = GL16_OBJ_CHILD_OFF
            obj_prop_off = GL16_OBJ_PROP_OFF
            obj_attr_bytes = GL16_ATTR_BYTES
            obj_max_properties = GL16_MAX_PROPERTIES";

        [RuntimeFunc(nameof(object_defines))]
        public const string gl16_objaddr = @"
            function
            local obj
            local addr
            jz obj -> rfalse
            sub obj 1 -> addr
            mul addr obj_entry_len -> addr
            add object_table_base addr -> addr
            return addr";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr))]
        public const string gl16_get_proptable = @"
            function
            local obj
            local rec
            local pt
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            add rec obj_prop_off -> rec
            aloads rec 0 -> pt
            bitand pt 0xFFFF -> pt
            return pt";

        // Version-specific object/property functions are supplied by
        // RuntimeLib16V3 and RuntimeLib16V4 to avoid runtime ZVERSION checks.

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string print_object = @"
            function
            local obj
            local pt
            local len
            local idx
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            aloadb pt 0 -> len
            add pt 1 -> pt
            copy 0 -> idx
        .next_char:
            jge idx len -> rtrue
            aloadb pt idx -> push
            streamchar pop
            add idx 1 -> idx
            jump .next_char";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr))]
        public new const string test_flag = @"
            function
            local obj
            local flag
            local rec
            local byte_off
            local bit_mask
            local shift
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            div flag 8 -> byte_off
            mod flag 8 -> shift
            add rec byte_off -> rec
            aloadb rec 0 -> rec
            copy 7 -> bit_mask
            sub bit_mask shift -> shift
            copy 1 -> bit_mask
            shiftl bit_mask shift -> bit_mask
            bitand rec bit_mask -> rec
            jz rec -> rfalse
            return 1";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr))]
        public new const string set_flag = @"
            function
            local obj
            local flag
            local value
            local rec
            local byte_off
            local bit_mask
            local shift
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            div flag 8 -> byte_off
            mod flag 8 -> shift
            copy 7 -> bit_mask
            sub bit_mask shift -> shift
            copy 1 -> bit_mask
            shiftl bit_mask shift -> bit_mask
            add rec byte_off -> rec
            aloadb rec 0 -> byte_off
            jz value -> .clear
            bitor byte_off bit_mask -> byte_off
            astoreb rec 0 byte_off
            return
        .clear:
            bitxor bit_mask 0xFF -> bit_mask
            bitand byte_off bit_mask -> byte_off
            astoreb rec 0 byte_off
            return";

        #endregion

        #region Vocab

        [RuntimeDefinitionSet]
        public new const string vocab_defines = @"
            ; word offsets after skipping VOCAB_RESOLUTION bytes
            vocab_PartOfSpeech = 0
            vocab_Value1 = 1
            vocab_Value2 = 2

            section .bss
            tokenize_key_buf: resb VOCAB_RESOLUTION";

        [RuntimeFunc(nameof(vocab_defines))]
        public new const string print_vocab_word = @"
            function
            local word
            local idx
            local addr
            local ch
            ; Load vocab string address from start of entry
            aload word 0 -> addr
            ; Stream up to VOCAB_RESOLUTION characters, stopping early on zero padding
            copy 0 -> idx
        .next_char:
            jge idx VOCAB_RESOLUTION -> rtrue
            aloadb addr idx -> ch
            jz ch -> rtrue
            streamchar ch
            add idx 1 -> idx
            jump .next_char";

        #endregion

        #region I/O

        [RuntimeFunc]
        public const string print_packed_string = @"
            function
            local addr
            mul addr PACKING_FACTOR -> addr
            streamstr addr
            return";

        [RuntimeFunc]
        public const string strlen = @"
            function
            local buf
            local i
            copy 0 -> i
        .loop:
            aloadb buf i -> push
            jz pop -> .done
            add i 1 -> i
            jump .loop
        .done:
            return i";

        #endregion

        #region Misc

        [RuntimeFunc]
        public new const string check_call = @"
            func_va
            local argc
            local func
            pull -> argc
            pull -> func
            jz func -> rfalse
            mul func PACKING_FACTOR -> func
            sub argc 1 -> argc
            tailcall func argc";

        #endregion
    }

    /// <summary>
    /// Glulx16 runtime for Z-machine v3 compatibility (byte-sized object fields,
    /// v3 property header layout).
    /// </summary>
    sealed class RuntimeLib16V3 : RuntimeLib16
    {
        #region V3 Objects

        // TODO: replace gl16_load_field/gl16_store_field with aloadb/astoreb
        [RuntimeFunc(nameof(object_defines))]
        public const string gl16_load_field = @"
            function
            local rec
            local off
            local val
            add rec off -> val
            aloadb val 0 -> val
            return val";

        [RuntimeFunc(nameof(object_defines))]
        public const string gl16_store_field = @"
            function
            local rec
            local off
            local val
            add rec off -> rec
            astoreb rec 0 val
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field), nameof(gl16_store_field))]
        public new const string move_object = @"
            function
            local obj
            local new_parent
            local rec
            local old_parent
            local parent_rec
            local first_child
            local sibling
            local next
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> old_parent
            jz old_parent -> .link_new
            callfi _rt_gl16_objaddr old_parent -> parent_rec
            callfii _rt_gl16_load_field parent_rec obj_child_off -> first_child
            jeq first_child obj -> .unlink_first
            copy first_child -> sibling
        .find_prev:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field next obj_sibling_off -> next
            jeq next obj -> .unlink_after
            copy next -> sibling
            jump .find_prev
        .unlink_after:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field next obj_sibling_off first_child
            jump .unlinked
        .unlink_first:
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field parent_rec obj_child_off first_child
        .unlinked:
            callfiii _rt_gl16_store_field rec obj_parent_off 0
        .link_new:
            callfi _rt_gl16_objaddr new_parent -> parent_rec
            jz parent_rec -> rfalse
            callfii _rt_gl16_load_field parent_rec obj_child_off -> first_child
            callfiii _rt_gl16_store_field rec obj_sibling_off first_child
            callfiii _rt_gl16_store_field parent_rec obj_child_off obj
            callfiii _rt_gl16_store_field rec obj_parent_off new_parent
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field), nameof(gl16_store_field))]
        public new const string remove_object = @"
            function
            local obj
            local rec
            local old_parent
            local parent_rec
            local first_child
            local sibling
            local next
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> old_parent
            jz old_parent -> rfalse
            callfi _rt_gl16_objaddr old_parent -> parent_rec
            callfii _rt_gl16_load_field parent_rec obj_child_off -> first_child
            jeq first_child obj -> .unlink_first
            copy first_child -> sibling
        .find_prev:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field next obj_sibling_off -> next
            jeq next obj -> .unlink_after
            copy next -> sibling
            jump .find_prev
        .unlink_after:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field next obj_sibling_off first_child
            jump .done_unlink
        .unlink_first:
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field parent_rec obj_child_off first_child
        .done_unlink:
            callfiii _rt_gl16_store_field rec obj_sibling_off 0
            callfiii _rt_gl16_store_field rec obj_parent_off 0
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string get_child = @"
            function
            local obj
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_child_off -> rec
            return rec";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string get_sibling = @"
            function
            local obj
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_sibling_off -> rec
            return rec";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string get_parent = @"
            function
            local obj
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> rec
            return rec";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string test_parent = @"
            function
            local obj
            local parent
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> rec
            jeq rec parent -> rtrue
            return 0";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string get_property_address = @"
            function
            local obj
            local prop
            local pt
            local hdr
            local pnum
            local len
            local data
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x1F -> pnum
            ushiftr hdr 5 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .found:
            return data";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string get_property = @"
            function
            local obj
            local prop
            local pt
            local hdr
            local pnum
            local len
            local data
            ; Get property table
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> .default
            ; Skip object name
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
        .scan:
            ; Get header (size/length) byte
            aloadb pt 0 -> hdr
            jz hdr -> .default
            ; property number = bottom 5 bits of header
            bitand hdr 0x1F -> pnum
            ; length = top 3 bits of header + 1
            ushiftr hdr 5 -> len
            add 1 len -> len
            add pt 1 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .found:
            jgt len 1 -> .word_value
            aloadb data 0 -> data
            return data
        .word_value:
            aloads data 0 -> data
            bitand data 0xFFFF -> data
            return data
        .default:
            jle prop 0 -> rfalse
            jgt prop obj_max_properties -> rfalse
            sub prop 1 -> data
            mul data 2 -> data
            add property_defaults_table data -> data
            aloads data 0 -> data
            bitand data 0xFFFF -> data
            return data";

        [RuntimeFunc]
        public new const string get_property_size = @"
            function
            local addr
            local hdr
            local len
            jz addr -> rfalse
            aloadb addr -1 -> hdr
            ushiftr hdr 5 -> len
            add len 1 -> len
            return len";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string put_property = @"
            function
            local obj
            local prop
            local value
            local pt
            local hdr
            local pnum
            local len
            local data
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x1F -> pnum
            ushiftr hdr 5 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .found:
            jgt len 1 -> .store_word
            astoreb data 0 value
            return
        .store_word:
            bitand value 0xFFFF -> value
            astores data 0 value
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string get_next_property = @"
            function
            local obj
            local prop
            local pt
            local hdr
            local pnum
            local len
            local data
            ; Get property table
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            ; Skip object name
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
            jz prop -> .first
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x1F -> pnum
            ushiftr hdr 5 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .after_match
            add data len -> pt
            jump .scan
        .after_match:
            add data len -> pt
        .first:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x1F -> pnum
            return pnum";

        #endregion

        #region V3 I/O

        [RuntimeFunc(nameof(read_line_inner), nameof(update_status_line))]
        public new const string read_line = @"
            func_va
            callf _rt_update_status_line
            tailcall _rt_read_line_inner pop";

        // also used for V4 (as read_line), but not V5+
        [RuntimeFunc(
            nameof(glk_defines), nameof(tokenize_line), nameof(check_call),
            nameof(update_status_line))]
        public const string read_line_inner = @"
            ; V3/V4 version
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
            add textbuf 1 -> push       ; buffer (skip first byte)
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
            ; Store null terminator
            aload gg_event event_Val1 -> nchars
            add nchars 1 -> push
            astoreb textbuf pop 0
        .got_command:
            ; Populate lexbuf
            callfii _rt_tokenize_line textbuf lexbuf
            ; Are we recording commands?
            jz [gg_command_output_stream_id] -> .not_recording
            ; Write command to stream
            push nchars
            add textbuf 1 -> push
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
            callf _rt_update_status_line
            jump .select
        .read_from_stream:
            aloadb textbuf 0 -> push    ; size of buffer
            add textbuf 1 -> push       ; buffer (skip first byte)
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
            ; Store null terminator
            astoreb textbuf 1 nchars
            add nchars 1 -> push
            astoreb textbuf pop 0
            ; Echo command to window
            push style_Input
            glk glk_set_style 1
            push nchars
            add textbuf 1 -> push
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

        [RuntimeFunc(nameof(glk_defines), nameof(vocab_defines), nameof(strlen))]
        public new const string tokenize_line = @"
            ; V3/V4 version
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
            local entry
            ; Count self-inserting breaks
            aloadb si_breaks 0 -> sibreakcnt
            add si_breaks 1 -> sibreaks
            ; Get buffer lengths and advance past
            add textbuf 1 -> textbuf        ; skip first byte
            callfi _rt_strlen textbuf -> len
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
            mul numwords 4 -> idx
            sub cx bx -> push
            astoreb lexbuf idx pop
            ; Fill in word offset
            add idx 1 -> idx
            add bx 1 -> push
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
            mul wx 4 -> idx
            aloadb lexbuf idx -> wlen
            add idx 1 -> idx
            aloadb lexbuf idx -> wpos
            ; Copy into tokenize_key_buf, in lowercase, with zero-padding
            jle wlen VOCAB_RESOLUTION -> .word_fits
            copy VOCAB_RESOLUTION -> wlen
        .word_fits:
            sub wpos 1 -> cx
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
            ; Binary search vocab using string table
            binarysearch tokenize_key_buf VOCAB_RESOLUTION vocab_strings VOCAB_RESOLUTION [vocab_entry_count] 0 5 -> idx
            jne idx -1 -> .found_vocab
            copy 0 -> entry
            jump .store_vocab
        .found_vocab:
            ; Compute vocab address from index
            mul idx [vocab_entry_length] -> push
            add vocab_entries pop -> entry
        .store_vocab:
            ; Store vocab address (or 0 if not found)
            mul wx 2 -> idx
            sub idx 1 -> idx
            astores lexbuf idx entry
            add wx 1 -> wx
            jump .check_more_words";

        [RuntimeFunc(nameof(split_window), nameof(clear_window))]
        public const string init_status_line = @"
            function
            callfi _rt_split_window 1
            callfi _rt_clear_window 1
            return";

        [RuntimeDefinitionSet]
        public const string status_defines = @"
            section .data
            status_score_text: huffstr ""Score: ""
            status_moves_text: huffstr ""Moves: """;

        [RuntimeFunc(
            nameof(select_window), nameof(output_style), nameof(get_screen_width),
            nameof(move_cursor), nameof(print_object), nameof(status_defines),
            nameof(glk_defines))]
        public const string update_status_line = @"
            function
            local width
            local i
            local here
            local score
            local moves
            jz [gg_status_window_id] -> rfalse
            ; Get global values
            aload global_variables 0 -> here
            aload global_variables 1 -> score
            aload global_variables 2 -> moves
            ; Select upper window and reverse video
            callfi _rt_select_window 1
            callfi _rt_output_style 1
            ; Fill the line with spaces
            callfii _rt_move_cursor 1 1
            callf _rt_get_screen_width -> width
            copy width -> i
        .loop:
            jz i -> .spaces_done
            streamchar ` `
            sub i 1 -> i
            jump .loop
        .spaces_done:
            ; Move to top left + 1 space
            callfii _rt_move_cursor 1 1
            streamchar ` `
            ; Print location
            callfi _rt_print_object here
            ; Move over and print score
            sub width 22 -> push
            callfii _rt_move_cursor 1 pop
            streamstr status_score_text
            streamnum score
            ; Move over again and print moves
            sub width 10 -> push
            callfii _rt_move_cursor 1 pop
            streamstr status_moves_text
            streamnum moves
            ; Return to main window and normal video
            callfi _rt_select_window 0
            callfi _rt_output_style 0
            return";

        #endregion
    }

    /// <summary>
    /// Glulx16 runtime for Z-machine v4+ compatibility (word-sized object fields,
    /// v4 property header layout).
    /// </summary>
    class RuntimeLib16V4 : RuntimeLib16
    {
        #region V4+ Objects

        [RuntimeFunc(nameof(object_defines))]
        public const string gl16_load_field = @"
            function
            local rec
            local off
            local val
            add rec off -> val
            aloads val 0 -> val
            bitand val 0xFFFF -> val
            return val";

        [RuntimeFunc(nameof(object_defines))]
        public const string gl16_store_field = @"
            function
            local rec
            local off
            local val
            add rec off -> rec
            bitand val 0xFFFF -> val
            astores rec 0 val
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field), nameof(gl16_store_field))]
        public new const string move_object = @"
            function
            local obj
            local new_parent
            local rec
            local old_parent
            local parent_rec
            local first_child
            local sibling
            local next
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> old_parent
            jz old_parent -> .link_new
            callfi _rt_gl16_objaddr old_parent -> parent_rec
            callfii _rt_gl16_load_field parent_rec obj_child_off -> first_child
            jeq first_child obj -> .unlink_first
            copy first_child -> sibling
        .find_prev:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field next obj_sibling_off -> next
            jeq next obj -> .unlink_after
            copy next -> sibling
            jump .find_prev
        .unlink_after:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field next obj_sibling_off first_child
            jump .unlinked
        .unlink_first:
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field parent_rec obj_child_off first_child
        .unlinked:
            callfiii _rt_gl16_store_field rec obj_parent_off 0
        .link_new:
            callfi _rt_gl16_objaddr new_parent -> parent_rec
            jz parent_rec -> rfalse
            callfii _rt_gl16_load_field parent_rec obj_child_off -> first_child
            callfiii _rt_gl16_store_field rec obj_sibling_off first_child
            callfiii _rt_gl16_store_field parent_rec obj_child_off obj
            callfiii _rt_gl16_store_field rec obj_parent_off new_parent
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field), nameof(gl16_store_field))]
        public new const string remove_object = @"
            function
            local obj
            local rec
            local old_parent
            local parent_rec
            local first_child
            local sibling
            local next
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> old_parent
            jz old_parent -> rfalse
            callfi _rt_gl16_objaddr old_parent -> parent_rec
            callfii _rt_gl16_load_field parent_rec obj_child_off -> first_child
            jeq first_child obj -> .unlink_first
            copy first_child -> sibling
        .find_prev:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field next obj_sibling_off -> next
            jeq next obj -> .unlink_after
            copy next -> sibling
            jump .find_prev
        .unlink_after:
            callfi _rt_gl16_objaddr sibling -> next
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field next obj_sibling_off first_child
            jump .done_unlink
        .unlink_first:
            callfii _rt_gl16_load_field rec obj_sibling_off -> first_child
            callfiii _rt_gl16_store_field parent_rec obj_child_off first_child
        .done_unlink:
            callfiii _rt_gl16_store_field rec obj_sibling_off 0
            callfiii _rt_gl16_store_field rec obj_parent_off 0
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string get_child = @"
            function
            local obj
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_child_off -> rec
            return rec";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string get_sibling = @"
            function
            local obj
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_sibling_off -> rec
            return rec";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string get_parent = @"
            function
            local obj
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> rec
            return rec";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_objaddr), nameof(gl16_load_field))]
        public new const string test_parent = @"
            function
            local obj
            local parent
            local rec
            callfi _rt_gl16_objaddr obj -> rec
            jz rec -> rfalse
            callfii _rt_gl16_load_field rec obj_parent_off -> rec
            jeq rec parent -> rtrue
            return 0";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string get_property_address = @"
            function
            local obj
            local prop
            local pt
            local hdr
            local pnum
            local len
            local data
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x3F -> pnum
            bitand hdr 0xC0 -> len
            jeq len 0xC0 -> .ext
            copy hdr -> len
            div len 64 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .found:
            return data
        .ext:
            aloadb pt 1 -> len
            jz len -> copy 64 -> len
            add pt 2 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string get_property = @"
            function
            local obj
            local prop
            local pt
            local hdr
            local pnum
            local len
            local data
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> .default
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> .default
            bitand hdr 0x3F -> pnum
            bitand hdr 0xC0 -> len
            jeq len 0xC0 -> .ext
            copy hdr -> len
            div len 64 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .ext:
            aloadb pt 1 -> len
            jz len -> copy 64 -> len
            add pt 2 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .found:
            jgt len 1 -> .word_value
            aloadb data 0 -> data
            return data
        .word_value:
            aloads data 0 -> data
            bitand data 0xFFFF -> data
            return data
        .default:
            jle prop 0 -> rfalse
            jgt prop obj_max_properties -> rfalse
            sub prop 1 -> data
            mul data 2 -> data
            add property_defaults_table data -> data
            aloads data 0 -> data
            bitand data 0xFFFF -> data
            return data";

        [RuntimeFunc]
        public new const string get_property_size = @"
            function
            local addr
            local hdr
            local len
            jz addr -> rfalse
            sub addr 2 -> hdr
            aloadb hdr 0 -> hdr
            bitand hdr 0xC0 -> len
            jeq len 0xC0 -> .ext
            add addr -1 -> hdr
            aloadb hdr 0 -> hdr
            div hdr 64 -> len
            add len 1 -> len
            return len
        .ext:
            aloadb addr -1 -> len
            jz len -> copy 64 -> len
            return len";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string put_property = @"
            function
            local obj
            local prop
            local value
            local pt
            local hdr
            local pnum
            local len
            local data
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x3F -> pnum
            bitand hdr 0xC0 -> len
            jeq len 0xC0 -> .ext
            copy hdr -> len
            div len 64 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .ext:
            aloadb pt 1 -> len
            jz len -> copy 64 -> len
            add pt 2 -> data
            jeq pnum prop -> .found
            add data len -> pt
            jump .scan
        .found:
            jgt len 1 -> .store_word
            astoreb data 0 value
            return
        .store_word:
            bitand value 0xFFFF -> value
            astores data 0 value
            return";

        [RuntimeFunc(nameof(object_defines), nameof(gl16_get_proptable))]
        public new const string get_next_property = @"
            function
            local obj
            local prop
            local pt
            local hdr
            local pnum
            local len
            local data
            callfi _rt_gl16_get_proptable obj -> pt
            jz pt -> rfalse
            aloadb pt 0 -> len
            add pt 1 -> pt
            add pt len -> pt
            jz prop -> .first
        .scan:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x3F -> pnum
            bitand hdr 0xC0 -> len
            jeq len 0xC0 -> .ext
            copy hdr -> len
            div len 64 -> len
            add len 1 -> len
            add pt 1 -> data
            jeq pnum prop -> .after_match
            add data len -> pt
            jump .scan
        .ext:
            aloadb pt 1 -> len
            jz len -> copy 64 -> len
            add pt 2 -> data
            jeq pnum prop -> .after_match
            add data len -> pt
            jump .scan
        .after_match:
            add data len -> pt
        .first:
            aloadb pt 0 -> hdr
            jz hdr -> rfalse
            bitand hdr 0x3F -> pnum
            return pnum";

        #endregion

        #region V4 I/O

        [RuntimeFunc(nameof(glk_defines), nameof(tokenize_line), nameof(check_call))]
        public new const string read_line = RuntimeLib16V3.read_line_inner;

        [RuntimeFunc(nameof(glk_defines), nameof(vocab_defines))]
        public new const string tokenize_line = RuntimeLib16V3.tokenize_line;

        #endregion
    }

    /// <summary>
    /// Glulx16 runtime for Z-machine v5+ compatibility (new readbuf format).
    /// </summary>
    sealed class RuntimeLib16V5 : RuntimeLib16V4
    {
        #region V5 I/O

        [RuntimeFunc(nameof(glk_defines), nameof(tokenize_line), nameof(check_call))]
        public new const string read_line = RuntimeLib.read_line;

        [RuntimeFunc(nameof(glk_defines), nameof(vocab_defines))]
        public new const string tokenize_line = @"
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
            local entry
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
            mul numwords 4 -> idx
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
            mul wx 4 -> idx
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
            ; Binary search vocab using string table
            binarysearch tokenize_key_buf VOCAB_RESOLUTION vocab_strings VOCAB_RESOLUTION [vocab_entry_count] 0 5 -> idx
            jne idx -1 -> .found_vocab
            copy 0 -> entry
            jump .store_vocab
        .found_vocab:
            ; Compute vocab address from index
            mul idx [vocab_entry_length] -> push
            add vocab_entries pop -> entry
        .store_vocab:
            ; Store vocab address (or 0 if not found)
            mul wx 2 -> idx
            sub idx 1 -> idx
            astores lexbuf idx entry
            add wx 1 -> wx
            jump .check_more_words";

        #endregion
    }
}
