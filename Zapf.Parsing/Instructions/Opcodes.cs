/* Copyright 2010-2017 Jesse McGrew
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

using System.Diagnostics.CodeAnalysis;

namespace Zapf.Parsing.Instructions
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum Opcodes : ushort
    {
        [ZOp("ADD", "add", 1, 6, ZOpFlags.Store)]
        Add = 20,
        [ZOp("ASHIFT", "art_shift", 5, 6, ZOpFlags.Store)]
        Ashift = 259,
        [ZOp("ASSIGNED?", "check_arg_count", 5, 6, ZOpFlags.Branch | ZOpFlags.IndirectVar)]
        Assigned_P = 255,
        [ZOp("BAND", "and", 1, 6, ZOpFlags.Store)]
        Band = 9,
        [ZOp("BCOM", "not", 1, 4, ZOpFlags.Store)]
        Bcom_V1 = 143,
        [ZOp("BCOM", "not", 5, 6, ZOpFlags.Store)]
        Bcom_V5 = 248,
        [ZOp("BOR", "or", 1, 6, ZOpFlags.Store)]
        Bor = 8,
        [ZOp("BTST", "test", 1, 6, ZOpFlags.Branch)]
        Btst = 7,
        [ZOp("BUFOUT", "buffer_mode", 4, 6, 0)]
        Bufout = 242,
        [ZOp("CALL", "call_vs", 1, 6, ZOpFlags.Store | ZOpFlags.Call, WhenExtra = "XCALL")]
        Call = 224,
        [ZOp("CALL1", "call_1s", 4, 6, ZOpFlags.Store | ZOpFlags.Call)]
        Call1 = 136,
        [ZOp("CALL2", "call_2s", 4, 6, ZOpFlags.Store | ZOpFlags.Call)]
        Call2 = 25,
        [ZOp("CATCH", "catch", 5, 6, ZOpFlags.Store)]
        Catch = 185,
        [ZOp("CHECKU", "check_unicode", 5, 6, ZOpFlags.Store)]
        Checku = 268,
        [ZOp("CLEAR", "erase_window", 4, 6, 0)]
        Clear = 237,
        [ZOp("COLOR", "set_colour", 5, 5, 0)]
        [ZOp("COLOR", "set_colour", 6, 6, ZOpFlags.VarArgs)]
        Color = 27,
        [ZOp("COPYT", "copy_table", 5, 6, 0)]
        Copyt = 253,
        [ZOp("CRLF", "new_line", 1, 6, 0)]
        Crlf = 187,
        [ZOp("CURGET", "get_cursor", 4, 6, 0)]
        Curget = 240,
        [ZOp("CURSET", "set_cursor", 4, 6, 0)]
        Curset = 239,
        [ZOp("DCLEAR", "erase_picture", 6, 6, 0)]
        Dclear = 263,
        [ZOp("DEC", "dec", 1, 6, ZOpFlags.IndirectVar)]
        Dec = 134,
        [ZOp("DIRIN", "input_stream", 3, 6, 0)]
        Dirin = 244,
        [ZOp("DIROUT", "output_stream", 3, 6, 0)]
        Dirout = 243,
        [ZOp("DISPLAY", "draw_picture", 6, 6, 0)]
        Display = 261,
        [ZOp("DIV", "div", 1, 6, ZOpFlags.Store)]
        Div = 23,
        [ZOp("DLESS?", "dec_chk", 1, 6, ZOpFlags.Branch | ZOpFlags.IndirectVar)]
        Dless_P = 4,
        /*[ZOp("ENDMOVE", "ENDMOVE", 5, 6, ZOpFlags.Store)]
        Endmove = 271,*/
        [ZOp("EQUAL?", "je", 1, 6, ZOpFlags.Branch | ZOpFlags.VarArgs)]
        Equal_P = 1,
        [ZOp("ERASE", "erase_line", 4, 6, 0)]
        Erase = 238,
        [ZOp("FCLEAR", "clear_attr", 1, 6, 0)]
        Fclear = 12,
        [ZOp("FIRST?", "get_child", 1, 6, ZOpFlags.Store | ZOpFlags.Branch)]
        First_P = 130,
        [ZOp("FONT", "set_font", 5, 6, ZOpFlags.Store)]
        Font = 260,
        [ZOp("FSET", "set_attr", 1, 6, 0)]
        Fset = 11,
        [ZOp("FSET?", "test_attr", 1, 6, ZOpFlags.Branch)]
        Fset_P = 10,
        [ZOp("FSTACK", "pop", 1, 4, 0)]
        Fstack_V1 = 185,
        [ZOp("FSTACK", "pop_stack", 6, 6, 0)]
        Fstack_V6 = 277,
        [ZOp("GET", "loadw", 1, 6, ZOpFlags.Store)]
        Get = 15,
        [ZOp("GETB", "loadb", 1, 6, ZOpFlags.Store)]
        Getb = 16,
        [ZOp("GETP", "get_prop", 1, 6, ZOpFlags.Store)]
        Getp = 17,
        [ZOp("GETPT", "get_prop_addr", 1, 6, ZOpFlags.Store)]
        Getpt = 18,
        [ZOp("GRTR?", "jg", 1, 6, ZOpFlags.Branch)]
        Grtr_P = 3,
        [ZOp("HLIGHT", "set_text_style", 4, 6, 0)]
        Hlight = 241,
        [ZOp("ICALL", "call_vn", 5, 6, ZOpFlags.Call, WhenExtra = "IXCALL")]
        Icall = 249,
        [ZOp("ICALL1", "call_1n", 5, 6, ZOpFlags.Call)]
        Icall1 = 143,
        [ZOp("ICALL2", "call_2n", 5, 6, ZOpFlags.Call)]
        Icall2 = 26,
        [ZOp("IGRTR?", "inc_chk", 1, 6, ZOpFlags.Branch | ZOpFlags.IndirectVar)]
        Igrtr_P = 5,
        [ZOp("IN?", "jin", 1, 6, ZOpFlags.Branch)]
        In_P = 6,
        [ZOp("INC", "inc", 1, 6, ZOpFlags.IndirectVar)]
        Inc = 133,
        [ZOp("INPUT", "read_char", 4, 6, ZOpFlags.Store)]
        Input = 246,
        [ZOp("INTBL?", "scan_table", 4, 6, ZOpFlags.Store | ZOpFlags.Branch)]
        Intbl_P = 247,
        [ZOp("IRESTORE", "restore_undo", 5, 6, ZOpFlags.Store)]
        Irestore = 266,
        [ZOp("ISAVE", "save_undo", 5, 6, ZOpFlags.Store)]
        Isave = 265,
        [ZOp("IXCALL", "call_vn2", 5, 6, ZOpFlags.Extra | ZOpFlags.Call)]
        Ixcall = 250,
        [ZOp("JUMP", "jump", 1, 6, ZOpFlags.Label | ZOpFlags.Terminates)]
        Jump = 140,
        [ZOp("LESS?", "jl", 1, 6, ZOpFlags.Branch)]
        Less_P = 2,
        [ZOp("LEX", "tokenise", 5, 6, 0)]
        Lex = 251,
        [ZOp("LOC", "get_parent", 1, 6, ZOpFlags.Store)]
        Loc = 131,
        [ZOp("MARGIN", "set_margins", 6, 6, 0)]
        Margin = 264,
        [ZOp("MENU", "make_menu", 6, 6, ZOpFlags.Branch)]
        Menu = 283,
        [ZOp("MOD", "mod", 1, 6, ZOpFlags.Store)]
        Mod = 24,
        [ZOp("MOUSE-INFO", "read_mouse", 6, 6, 0)]
        MouseInfo = 278,
        [ZOp("MOUSE-LIMIT", "mouse_window", 6, 6, 0)]
        MouseLimit = 279,
        [ZOp("MOVE", "insert_obj", 1, 6, 0)]
        Move = 14,
        [ZOp("MUL", "mul", 1, 6, ZOpFlags.Store)]
        Mul = 22,
        [ZOp("NEXT?", "get_sibling", 1, 6, ZOpFlags.Store | ZOpFlags.Branch)]
        Next_P = 129,
        [ZOp("NEXTP", "get_next_prop", 1, 6, ZOpFlags.Store)]
        Nextp = 19,
        [ZOp("NOOP", "nop", 1, 6, 0)]
        Noop = 180,
        [ZOp("ORIGINAL?", "piracy", 5, 6, ZOpFlags.Branch)]
        Original_P = 191,
        [ZOp("PICINF", "picture_data", 6, 6, ZOpFlags.Branch)]
        Picinf = 262,
        [ZOp("PICSET", "picture_table", 6, 6, 0)]
        Picset = 284,
        [ZOp("POP", "pull", 1, 5, 0)]
        [ZOp("POP", "pull", 6, 6, ZOpFlags.Store)]
        Pop = 233,
        [ZOp("PRINT", "print_paddr", 1, 6, 0)]
        Print = 141,
        [ZOp("PRINTB", "print_addr", 1, 6, 0)]
        Printb = 135,
        [ZOp("PRINTC", "print_char", 1, 6, 0)]
        Printc = 229,
        [ZOp("PRINTD", "print_obj", 1, 6, 0)]
        Printd = 138,
        [ZOp("PRINTF", "print_form", 6, 6, 0)]
        Printf = 282,
        [ZOp("PRINTI", "print", 1, 6, ZOpFlags.String)]
        Printi = 178,
        /*[ZOp("PRINTMOVE", "PRINTMOVE", 5, 6, ZOpFlags.Store)]
        Printmove = 267,*/
        [ZOp("PRINTN", "print_num", 1, 6, 0)]
        Printn = 230,
        [ZOp("PRINTR", "print_ret", 1, 6, ZOpFlags.String | ZOpFlags.Terminates)]
        Printr = 179,
        [ZOp("PRINTT", "print_table", 5, 6, 0)]
        Printt = 254,
        [ZOp("PRINTU", "print_unicode", 5, 6, 0)]
        Printu = 267,
        [ZOp("PTSIZE", "get_prop_len", 1, 6, ZOpFlags.Store)]
        Ptsize = 132,
        [ZOp("PUSH", "push", 1, 6, 0)]
        Push = 232,
        [ZOp("PUT", "storew", 1, 6, 0)]
        Put = 225,
        [ZOp("PUTB", "storeb", 1, 6, 0)]
        Putb = 226,
        [ZOp("PUTP", "put_prop", 1, 6, 0)]
        Putp = 227,
        [ZOp("QUIT", "quit", 1, 6, ZOpFlags.Terminates)]
        Quit = 186,
        [ZOp("RANDOM", "random", 1, 6, ZOpFlags.Store)]
        Random = 231,
        [ZOp("READ", "sread", 1, 4, 0)]
        [ZOp("READ", "aread", 5, 6, ZOpFlags.Store)]
        Read = 228,
        [ZOp("REMOVE", "remove_obj", 1, 6, 0)]
        Remove = 137,
        [ZOp("RESTART", "restart", 1, 6, ZOpFlags.Terminates)]
        Restart = 183,
        [ZOp("RESTORE", "restore", 1, 3, ZOpFlags.Branch)]
        [ZOp("RESTORE", "restore", 4, 4, ZOpFlags.Store)]
        Restore_V1 = 182,
        [ZOp("RESTORE", "restore", 5, 6, ZOpFlags.Store)]
        Restore_V5 = 257,
        [ZOp("RETURN", "ret", 1, 6, ZOpFlags.Terminates)]
        Return = 139,
        [ZOp("RFALSE", "rfalse", 1, 6, ZOpFlags.Terminates)]
        Rfalse = 177,
        [ZOp("RSTACK", "ret_popped", 1, 6, ZOpFlags.Terminates)]
        Rstack = 184,
        /*[ZOp("RTIME", "RTIME", 5, 6, ZOpFlags.Store)]
        Rtime = 268,*/
        [ZOp("RTRUE", "rtrue", 1, 6, ZOpFlags.Terminates)]
        Rtrue = 176,
        [ZOp("SAVE", "save", 1, 3, ZOpFlags.Branch)]
        [ZOp("SAVE", "save", 4, 4, ZOpFlags.Store)]
        Save_V1 = 181,
        [ZOp("SAVE", "save", 5, 6, ZOpFlags.Store)]
        Save_V5 = 256,
        [ZOp("SCREEN", "set_window", 3, 6, 0)]
        Screen = 235,
        [ZOp("SCROLL", "scroll_window", 6, 6, 0)]
        Scroll = 276,
        /*[ZOp("SEND", "SEND", 5, 6, ZOpFlags.Store)]
        Send = 269,
        [ZOp("SERVER", "SERVER", 5, 6, ZOpFlags.Store)]
        Server = 270,*/
        [ZOp("SET", "store", 1, 6, ZOpFlags.IndirectVar)]
        Set = 13,
        [ZOp("SHIFT", "log_shift", 5, 6, ZOpFlags.Store)]
        Shift = 258,
        [ZOp("SOUND", "sound_effect", 3, 6, 0)]
        Sound = 245,
        [ZOp("SPLIT", "split_window", 3, 6, 0)]
        Split = 234,
        [ZOp("SUB", "sub", 1, 6, ZOpFlags.Store)]
        Sub = 21,
        [ZOp("THROW", "throw", 5, 6, ZOpFlags.Terminates)]
        Throw = 28,
        [ZOp("USL", "show_status", 1, 3, 0)]
        Usl = 188,
        [ZOp("VALUE", "load", 1, 6, ZOpFlags.Store | ZOpFlags.IndirectVar)]
        Value = 142,
        [ZOp("VERIFY", "verify", 3, 6, ZOpFlags.Branch)]
        Verify = 189,
        [ZOp("WINATTR", "window_style", 6, 6, 0)]
        Winattr = 274,
        [ZOp("WINGET", "get_wind_prop", 6, 6, ZOpFlags.Store)]
        Winget = 275,
        [ZOp("WINPOS", "move_window", 6, 6, 0)]
        Winpos = 272,
        [ZOp("WINPUT", "put_wind_prop", 6, 6, 0)]
        Winput = 281,
        [ZOp("WINSIZE", "window_size", 6, 6, 0)]
        Winsize = 273,
        [ZOp("XCALL", "call_vs2", 4, 6, ZOpFlags.Store | ZOpFlags.Extra | ZOpFlags.Call)]
        Xcall = 236,
        [ZOp("XPUSH", "push_stack", 6, 6, ZOpFlags.Branch)]
        Xpush = 280,
        [ZOp("ZERO?", "jz", 1, 6, ZOpFlags.Branch)]
        Zero_P = 128,
        [ZOp("ZWSTR", "encode_text", 5, 6, 0)]
        Zwstr = 252,
    }
}