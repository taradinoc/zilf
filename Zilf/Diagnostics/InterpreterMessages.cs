/* Copyright 2010, 2016 Jesse McGrew
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
using System.Text;
using System.Threading.Tasks;

namespace Zilf.Diagnostics
{
    [MessageSet("MDL")]
    public abstract class InterpreterMessages
    {
        private InterpreterMessages()
        {
        }

        // Legacy

        [Message("{0}")]
        public const int LegacyError = 0;

        // Syntax Errors

        [Message("PROPDEF patterns must be lists")]
        public const int PROPDEF_Patterns_Must_Be_Lists = 1;
        [Message("strings in PROPDEF patterns must be \"OPT\" or \"MANY\"")]
        public const int Strings_In_PROPDEF_Patterns_Must_Be_OPT_Or_MANY = 2;
        [Message("list in PROPDEF output pattern must have length 2")]
        public const int List_In_PROPDEF_Output_Pattern_Must_Have_Length_2 = 3;
        [Message("first item of list in PROPDEF output pattern must be an atom")]
        public const int First_Item_Of_List_In_PROPDEF_Output_Pattern_Must_Be_An_Atom = 4;
        [Message("list elements must be 2-element lists")]
        public const int List_Elements_Must_Be_2element_Lists = 5;
        [Message("list elements must be string/atom pairs")]
        public const int List_Elements_Must_Be_Stringatom_Pairs = 6;
        [Message("multiple \"OPT\" clauses")]
        public const int Multiple_OPT_Clauses = 7;
        [Message("\"OPT\" after \"AUX\"")]
        public const int OPT_After_AUX = 8;
        [Message("multiple \"AUX\" clauses")]
        public const int Multiple_AUX_Clauses = 9;
        [Message("multiple \"ARGS\" or \"TUPLE\" clauses")]
        public const int Multiple_ARGS_Or_TUPLE_Clauses = 10;
        [Message("multiple \"NAME\" clauses or activation atoms")]
        public const int Multiple_NAME_Clauses_Or_Activation_Atoms = 11;
        [Message("multiple \"BIND\" clauses")]
        public const int Multiple_BIND_Clauses = 12;
        [Message("multiple \"VALUE\" clauses")]
        public const int Multiple_VALUE_Clauses = 13;
        [Message("\"ARGS\" or \"TUPLE\" must be followed by an atom")]
        public const int ARGS_Or_TUPLE_Must_Be_Followed_By_An_Atom = 14;
        [Message("\"NAME\" or \"ACT\" must be followed by an atom")]
        public const int NAME_Or_ACT_Must_Be_Followed_By_An_Atom = 15;
        [Message("\"BIND\" must be followed by an atom")]
        public const int BIND_Must_Be_Followed_By_An_Atom = 16;
        [Message("empty list in arg spec")]
        public const int Empty_List_In_Arg_Spec = 17;
        [Message("string in PROPDEF output pattern must be \"MANY\"")]
        public const int String_In_PROPDEF_Output_Pattern_Must_Be_MANY = 20;
        [Message("PROPDEF output elements must be FIX, FALSE, FORM, STRING, or SEMI")]
        public const int PROPDEF_Output_Elements_Must_Be_FIX_FALSE_FORM_STRING_Or_SEMI = 21;
        [Message("PROPDEF pattern must start with an atom")]
        public const int PROPDEF_Pattern_Must_Start_With_An_Atom = 22;
        [Message("FIX/FALSE in PROPDEF output pattern must be at the beginning")]
        public const int FIXFALSE_In_PROPDEF_Output_Pattern_Must_Be_At_The_Beginning = 23;
        [Message("FORM in PROPDEF pattern must start with an atom")]
        public const int FORM_In_PROPDEF_Pattern_Must_Start_With_An_Atom = 24;
        [Message("lists in TELL token specs must contain atoms")]
        public const int Lists_In_TELL_Token_Specs_Must_Contain_Atoms = 29;
        [Message("lists in TELL token specs must come at the beginning of a pattern")]
        public const int Lists_In_TELL_Token_Specs_Must_Come_At_The_Beginning_Of_A_Pattern = 30;
        [Message("lists and atoms in TELL token specs must come at the beginning")]
        public const int Lists_And_Atoms_In_TELL_Token_Specs_Must_Come_At_The_Beginning = 31;
        [Message("left side of ADECL in TELL token spec must be '*'")]
        public const int Left_Side_Of_ADECL_In_TELL_Token_Spec_Must_Be_ = 32;
        [Message("malformed GVAL in TELL token spec")]
        public const int Malformed_GVAL_In_TELL_Token_Spec = 33;
        [Message("TELL token spec ends with an unterminated pattern")]
        public const int TELL_Token_Spec_Ends_With_An_Unterminated_Pattern = 34;
        [Message("unexpected clause in arg spec: {0}")]
        public const int Unexpected_Clause_In_Arg_Spec_0 = 35;

        // Type/Format Errors

        [Message("routine rewriter result must contain an arg spec and a body")]
        public const int Routine_Rewriter_Result_Must_Contain_An_Arg_Spec_And_A_Body = 18;
        [Message("routine rewriter must return a LIST or FALSE")]
        public const int Routine_Rewriter_Must_Return_A_LIST_Or_FALSE = 19;
        [Message("CHTYPE to GVAL or LVAL requires ATOM")]
        public const int CHTYPE_To_GVAL_Or_LVAL_Requires_ATOM = 25;

        // Atom Errors

        [Message("no previously pushed value for OBLIST")]
        public const int No_Previously_Pushed_Value_For_OBLIST = 26;

        // Flow Control Errors

        [Message("{0}: no enclosing PROG/REPEAT")]
        public const int FUNCNAME0_No_Enclosing_PROGREPEAT = 27;
        [Message("object options in syntax must be atoms")]
        public const int Object_Options_In_Syntax_Must_Be_Atoms = 36;
        [Message("NEW-SFLAGS vector must have an even number of elements")]
        public const int NEWSFLAGS_Vector_Must_Have_An_Even_Number_Of_Elements = 37;
        [Message("NEW-SFLAGS names must be strings or atoms")]
        public const int NEWSFLAGS_Names_Must_Be_Strings_Or_Atoms = 38;
        [Message("NEW-SFLAGS values must be FIXes between 0 and 255")]
        public const int NEWSFLAGS_Values_Must_Be_FIXes_Between_0_And_255 = 39;
        [Message("cannot CHTYPE away from LINK")]
        public const int Cannot_CHTYPE_Away_From_LINK = 42;
        [Message("CHTYPE to {0} not supported")]
        public const int CHTYPE_To_TYPENAME0_Not_Supported = 43;
        [Message("environment has expired")]
        public const int Environment_Has_Expired = 44;
        [Message("segment evaluation must return a structure")]
        public const int Segment_Evaluation_Must_Return_A_Structure = 45;
        [Message("list must have 2 elements")]
        public const int List_Must_Have_2_Elements = 47;
        [Message("first element must be an atom")]
        public const int First_Element_Must_Be_An_Atom = 48;
        [Message("missing verb in syntax definition")]
        public const int Missing_Verb_In_Syntax_Definition = 49;
        [Message("too many OBJECT in syntax definition")]
        public const int Too_Many_OBJECT_In_Syntax_Definition = 50;
        [Message("too many prepositions in syntax definition (try defining another object)")]
        public const int Too_Many_Prepositions_In_Syntax_Definition_Try_Defining_Another_Object = 51;
        [Message("too many prepositions in syntax definition")]
        public const int Too_Many_Prepositions_In_Syntax_Definition = 52;
        [Message("list in syntax definition must start with an atom")]
        public const int List_In_Syntax_Definition_Must_Start_With_An_Atom = 53;
        [Message("too many synonym lists in syntax definition")]
        public const int Too_Many_Synonym_Lists_In_Syntax_Definition = 54;
        [Message("too many FIND lists in syntax definition")]
        public const int Too_Many_FIND_Lists_In_Syntax_Definition = 55;
        [Message("unexpected value in syntax definition")]
        public const int Unexpected_Value_In_Syntax_Definition = 56;
        [Message("too many = in syntax definition")]
        public const int Too_Many_EQ_In_Syntax_Definition = 57;
        [Message("values after = must be FALSE or atoms")]
        public const int Values_After_EQ_Must_Be_FALSE_Or_Atoms = 58;
        [Message("too many values after = in syntax definition")]
        public const int Too_Many_Values_After_EQ_In_Syntax_Definition = 59;
        [Message("verb synonyms must be atoms")]
        public const int Verb_Synonyms_Must_Be_Atoms = 60;
        [Message("action routine must be specified")]
        public const int Action_Routine_Must_Be_Specified = 61;
        [Message("FIND must be followed by a single atom")]
        public const int FIND_Must_Be_Followed_By_A_Single_Atom = 62;
        [Message("list does not match MACRO pattern")]
        public const int List_Does_Not_Match_MACRO_Pattern = 63;
        [Message("vector coerced to ADECL must have length 2")]
        public const int Vector_Coerced_To_ADECL_Must_Have_Length_2 = 64;
        [Message("list must have at least 2 elements")]
        public const int List_Must_Have_At_Least_2_Elements = 66;
        [Message("first element must be a list")]
        public const int First_Element_Must_Be_A_List = 67;
        [Message("elements of a string must be characters")]
        public const int Elements_Of_A_String_Must_Be_Characters = 68;
        [Message("writing past end of string")]
        public const int Writing_Past_End_Of_String = 69;
        [Message("expected a structured value after the FIX")]
        public const int Expected_A_Structured_Value_After_The_FIX = 72;
        [Message("expected 1 or 2 args after a FIX")]
        public const int Expected_1_Or_2_Args_After_A_FIX = 74;
        [Message("malformed DECL object")]
        public const int Malformed_DECL_Object = 75;
        [Message("GET-CLASSIFICATION must return different values for ADJ, BUZZ, DIR, NOUN, PREP, and VERB")]
        public const int GETCLASSIFICATION_Must_Return_Different_Values_For_ADJ_BUZZ_DIR_NOUN_PREP_And_VERB = 76;
        [Message("MAKE-VWORD must return a VWORD")]
        public const int MAKEVWORD_Must_Return_A_VWORD = 77;
        [Message("NEW-ADD-WORD: MAKE-VWORD must return a VWORD")]
        public const int NEWADDWORD_MAKEVWORD_Must_Return_A_VWORD = 78;
        [Message("NEW-ADD-WORD: word would be overloaded")]
        public const int NEWADDWORD_Word_Would_Be_Overloaded = 79;
        [Message("NEW-ADD-WORD: GVAL of WORD-FLAGS-LIST must be a list")]
        public const int NEWADDWORD_GVAL_Of_WORDFLAGSLIST_Must_Be_A_List = 80;
        [Message("NEW-ADD-WORD: GET-CLASSIFICATION must return a FIX")]
        public const int NEWADDWORD_GETCLASSIFICATION_Must_Return_A_FIX = 81;
        [Message("list must have length 1")]
        public const int List_Must_Have_Length_1 = 82;
        [Message("{0}: first arg must be a non-negative FIX")]
        public const int FUNCNAME0_First_Arg_Must_Be_A_Nonnegative_FIX = 83;
        [Message("ISTRING: iterated values must be CHARACTERs")]
        public const int ISTRING_Iterated_Values_Must_Be_CHARACTERs = 86;
        [Message("ENTRY: LVAL of OBLIST must be a list starting with 2 OBLISTs")]
        public const int ENTRY_LVAL_Of_OBLIST_Must_Be_A_List_Starting_With_2_OBLISTs = 88;
        [Message("ENTRY: must be called from within a PACKAGE")]
        public const int ENTRY_Must_Be_Called_From_Within_A_PACKAGE = 89;
        [Message("RENTRY: LVAL of OBLIST must be a list starting with 2 OBLISTs")]
        public const int RENTRY_LVAL_Of_OBLIST_Must_Be_A_List_Starting_With_2_OBLISTs = 90;
        [Message("RENTRY: must be called from within a PACKAGE or DEFINITIONS")]
        public const int RENTRY_Must_Be_Called_From_Within_A_PACKAGE_Or_DEFINITIONS = 91;
        [Message("DEFSTRUCT: not enough elements in 'CONSTRUCTOR spec")]
        public const int DEFSTRUCT_Not_Enough_Elements_In_CONSTRUCTOR_Spec = 92;
        [Message("DEFSTRUCT: element after 'CONSTRUCTOR must be an atom")]
        public const int DEFSTRUCT_Element_After_CONSTRUCTOR_Must_Be_An_Atom = 93;
        [Message("DEFSTRUCT: second element after 'CONSTRUCTOR must be an argument list")]
        public const int DEFSTRUCT_Second_Element_After_CONSTRUCTOR_Must_Be_An_Argument_List = 94;
        [Message("DEFSTRUCT: 'NONE is not allowed after a default field value")]
        public const int DEFSTRUCT_NONE_Is_Not_Allowed_After_A_Default_Field_Value = 95;
        [Message("DEFSTRUCT: parts of defaults section must be quoted atoms or lists")]
        public const int DEFSTRUCT_Parts_Of_Defaults_Section_Must_Be_Quoted_Atoms_Or_Lists = 96;
        [Message("DEFSTRUCT: lists in defaults section must start with a quoted atom")]
        public const int DEFSTRUCT_Lists_In_Defaults_Section_Must_Start_With_A_Quoted_Atom = 97;
        [Message("DEFSTRUCT: 'NTH must be followed by an atom")]
        public const int DEFSTRUCT_NTH_Must_Be_Followed_By_An_Atom = 98;
        [Message("DEFSTRUCT: 'PUT must be followed by an atom")]
        public const int DEFSTRUCT_PUT_Must_Be_Followed_By_An_Atom = 99;
        [Message("DEFSTRUCT: 'START-OFFSET must be followed by a FIX")]
        public const int DEFSTRUCT_STARTOFFSET_Must_Be_Followed_By_A_FIX = 100;
        [Message("DEFSTRUCT: 'PRINTTYPE must be followed by an atom")]
        public const int DEFSTRUCT_PRINTTYPE_Must_Be_Followed_By_An_Atom = 101;
        [Message("WORD-LEXICAL-WORD must return a string")]
        public const int WORDLEXICALWORD_Must_Return_A_String = 102;
        [Message("division by zero")]
        public const int Division_By_Zero = 103;
        [Message("list must have at least 1 element")]
        public const int List_Must_Have_At_Least_1_Element = 105;
        [Message("elements after first must be lists")]
        public const int Elements_After_First_Must_Be_Lists = 107;
        [Message("vector coerced to OFFSET must have length 2")]
        public const int Vector_Coerced_To_OFFSET_Must_Have_Length_2 = 110;
        [Message("first element must be a FIX")]
        public const int First_Element_Must_Be_A_FIX = 111;
        [Message("OFFSET is immutable")]
        public const int OFFSET_Is_Immutable = 112;
        [Message("expected a structured value after the OFFSET")]
        public const int Expected_A_Structured_Value_After_The_OFFSET = 113;
        [Message("expected 1 or 2 args after an OFFSET")]
        public const int Expected_1_Or_2_Args_After_An_OFFSET = 115;
        [Message("REST: not enough elements")]
        public const int REST_Not_Enough_Elements = 116;
        [Message("BACK: not enough elements")]
        public const int BACK_Not_Enough_Elements = 117;
        [Message("GROW: sizes must be non-negative")]
        public const int GROW_Sizes_Must_Be_Nonnegative = 118;
        [Message("NTH: reading past end of structure")]
        public const int NTH_Reading_Past_End_Of_Structure = 119;
        [Message("SUBSTRUC: negative element count")]
        public const int SUBSTRUC_Negative_Element_Count = 120;
        [Message("SUBSTRUC: fourth arg must have same primtype as first")]
        public const int SUBSTRUC_Fourth_Arg_Must_Have_Same_Primtype_As_First = 121;
        [Message("SUBSTRUC: destination too short")]
        public const int SUBSTRUC_Destination_Too_Short = 122;
        [Message("SUBSTRUC: primtype TABLE not supported")]
        public const int SUBSTRUC_Primtype_TABLE_Not_Supported = 124;
        [Message("SORT: keys must have the same type to use default comparison")]
        public const int SORT_Keys_Must_Have_The_Same_Type_To_Use_Default_Comparison = 125;
        [Message("SORT: key primtypes must be ATOM, FIX, or STRING to use default comparison")]
        public const int SORT_Key_Primtypes_Must_Be_ATOM_FIX_Or_STRING_To_Use_Default_Comparison = 126;
        [Message("no OBLIST path")]
        public const int No_OBLIST_Path = 127;
        [Message("ITABLE: specifier must be NONE, BYTE, or WORD")]
        public const int ITABLE_Specifier_Must_Be_NONE_BYTE_Or_WORD = 128;
        [Message("ITABLE: invalid table size")]
        public const int ITABLE_Invalid_Table_Size = 129;
        [Message("ZREST: second arg must not be negative")]
        public const int ZREST_Second_Arg_Must_Not_Be_Negative = 130;
        [Message("VERSION: TIME is only meaningful in version 3")]
        public const int VERSION_TIME_Is_Only_Meaningful_In_Version_3 = 131;
        [Message("ORDER-OBJECTS?: first arg must be DEFINED, ROOMS-FIRST, ROOMS-AND-LGS-FIRST, or ROOMS-LAST")]
        public const int ORDEROBJECTS_First_Arg_Must_Be_DEFINED_ROOMSFIRST_ROOMSANDLGSFIRST_Or_ROOMSLAST = 132;
        [Message("ORDER-TREE?: first arg must be REVERSE-DEFINED")]
        public const int ORDERTREE_First_Arg_Must_Be_REVERSEDEFINED = 133;
        [Message("CHRSET: alphabet number must be between 0 and 2")]
        public const int CHRSET_Alphabet_Number_Must_Be_Between_0_And_2 = 134;
        [Message("NEW-ADD-WORD: requires NEW-PARSER? option")]
        public const int NEWADDWORD_Requires_NEWPARSER_Option = 135;
        [Message("a SEGMENT can only be evaluated inside a structure")]
        public const int A_SEGMENT_Can_Only_Be_Evaluated_Inside_A_Structure = 136;
        [Message("PRINT: bad OUTCHAN")]
        public const int PRINT_Bad_OUTCHAN = 138;
        [Message("PRIN1: bad OUTCHAN")]
        public const int PRIN1_Bad_OUTCHAN = 139;
        [Message("PRINC: bad OUTCHAN")]
        public const int PRINC_Bad_OUTCHAN = 140;
        [Message("CRLF: bad OUTCHAN")]
        public const int CRLF_Bad_OUTCHAN = 141;
        [Message("IMAGE: bad OUTCHAN")]
        public const int IMAGE_Bad_OUTCHAN = 142;
        [Message("M-HPOS: not supported by this type of channel")]
        public const int MHPOS_Not_Supported_By_This_Type_Of_Channel = 143;
        [Message("INDENT-TO: first arg must be non-negative")]
        public const int INDENTTO_First_Arg_Must_Be_Nonnegative = 144;
        [Message("INDENT-TO: bad OUTCHAN")]
        public const int INDENTTO_Bad_OUTCHAN = 145;
        [Message("INDENT-TO: not supported by this type of channel")]
        public const int INDENTTO_Not_Supported_By_This_Type_Of_Channel = 146;
    }
}
