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
        InterpreterMessages()
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
        [Message("multiple {0} clauses")]
        public const int Multiple_0_Clauses = 7;
        [Message("\"OPT\" after \"AUX\"")]
        public const int OPT_After_AUX = 8;
        [Message("multiple \"NAME\" clauses or activation atoms")]
        public const int Multiple_NAME_Clauses_Or_Activation_Atoms = 11;
        [Message("{0} must be followed by an atom")]
        public const int _0_Must_Be_Followed_By_An_Atom = 14;
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
        [Message("CHTYPE to {0} not supported")]
        public const int CHTYPE_To_0_Not_Supported = 43;
        [Message("CHTYPE away from {0} not supported")]
        public const int CHTYPE_Away_From_0_Not_Supported = 42;
        [Message("CHTYPE to {0} requires {1}")]
        public const int CHTYPE_To_0_Requires_1 = 155;
        [Message("{0} is not a registered type")]
        public const int _0_Is_Not_A_Registered_Type = 156;
        [Message("CHTYPE to {0} did not produce an applicable object")]
        public const int CHTYPE_To_0_Did_Not_Produce_An_Applicable_Object = 253;


        // Atom Errors

        [Message("no previously pushed value for OBLIST")]
        public const int No_Previously_Pushed_Value_For_OBLIST = 26;

        // Flow Control Errors

        [Message("{0}: no enclosing PROG/REPEAT")]
        public const int _0_No_Enclosing_PROGREPEAT = 27;

        // Argument Errors
        [Message("{0} requires {1} {2}{3}")]
        public const int _0_Requires_1_23 = 263;
        [Message("{0} requires {1} additional {2}{3}")]
        public const int _0_Requires_1_Additional_23 = 264;
        
        [Message("object options in syntax must be atoms")]
        public const int Object_Options_In_Syntax_Must_Be_Atoms = 36;
        [Message("NEW-SFLAGS vector must have an even number of elements")]
        public const int NEWSFLAGS_Vector_Must_Have_An_Even_Number_Of_Elements = 37;
        [Message("NEW-SFLAGS names must be strings or atoms")]
        public const int NEWSFLAGS_Names_Must_Be_Strings_Or_Atoms = 38;
        [Message("NEW-SFLAGS values must be FIXes between 0 and 255")]
        public const int NEWSFLAGS_Values_Must_Be_FIXes_Between_0_And_255 = 39;
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
        [Message("too many prepositions in syntax definition")]
        public const int Too_Many_Prepositions_In_Syntax_Definition = 51;
        [Message("did you mean to separate them with OBJECT?", Severity = Severity.Info)]
        public const int Did_You_Mean_To_Separate_Them_With_OBJECT = 52;
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
        [Message("{0}: MAKE-VWORD must return a VWORD")]
        public const int _0_MAKEVWORD_Must_Return_A_VWORD = 78;
        [Message("{0}: word would be overloaded")]
        public const int _0_Word_Would_Be_Overloaded = 79;
        [Message("{0}: GVAL of WORD-FLAGS-LIST must be a list")]
        public const int _0_GVAL_Of_WORDFLAGSLIST_Must_Be_A_List = 80;
        [Message("{0}: GET-CLASSIFICATION must return a FIX")]
        public const int _0_GETCLASSIFICATION_Must_Return_A_FIX = 81;
        [Message("list must have length 1")]
        public const int List_Must_Have_Length_1 = 82;
        [Message("{0}: first arg must be a non-negative FIX")]
        public const int _0_First_Arg_Must_Be_A_Nonnegative_FIX = 83;
        [Message("{0}: iterated values must be CHARACTERs")]
        public const int _0_Iterated_Values_Must_Be_CHARACTERs = 86;
        [Message("{0}: LVAL of OBLIST must be a list starting with 2 OBLISTs")]
        public const int _0_LVAL_Of_OBLIST_Must_Be_A_List_Starting_With_2_OBLISTs = 88;
        [Message("{0}: must be called from within a PACKAGE")]
        public const int _0_Must_Be_Called_From_Within_A_PACKAGE = 89;
        [Message("{0}: must be called from within a PACKAGE or DEFINITIONS")]
        public const int _0_Must_Be_Called_From_Within_A_PACKAGE_Or_DEFINITIONS = 91;
        [Message("{0}: not enough elements in 'CONSTRUCTOR spec")]
        public const int _0_Not_Enough_Elements_In_CONSTRUCTOR_Spec = 92;
        [Message("{0}: element after 'CONSTRUCTOR must be an atom")]
        public const int _0_Element_After_CONSTRUCTOR_Must_Be_An_Atom = 93;
        [Message("{0}: second element after 'CONSTRUCTOR must be an argument list")]
        public const int _0_Second_Element_After_CONSTRUCTOR_Must_Be_An_Argument_List = 94;
        [Message("{0}: 'NONE is not allowed after a default field value")]
        public const int _0_NONE_Is_Not_Allowed_After_A_Default_Field_Value = 95;
        [Message("{0}: parts of defaults section must be quoted atoms or lists")]
        public const int _0_Parts_Of_Defaults_Section_Must_Be_Quoted_Atoms_Or_Lists = 96;
        [Message("{0}: lists in defaults section must start with a quoted atom")]
        public const int _0_Lists_In_Defaults_Section_Must_Start_With_A_Quoted_Atom = 97;
        [Message("{0}: {1} must be followed by an atom")]
        public const int _0_1_Must_Be_Followed_By_An_Atom = 98;
        [Message("{0}: 'START-OFFSET must be followed by a FIX")]
        public const int _0_STARTOFFSET_Must_Be_Followed_By_A_FIX = 100;
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
        [Message("{0}: not enough elements")]
        public const int _0_Not_Enough_Elements = 116;
        [Message("{0}: sizes must be non-negative")]
        public const int _0_Sizes_Must_Be_Nonnegative = 118;
        [Message("{0}: reading past end of structure")]
        public const int _0_Reading_Past_End_Of_Structure = 119;
        [Message("{0}: negative element count")]
        public const int _0_Negative_Element_Count = 120;
        [Message("{0}: fourth arg must have same primtype as first")]
        public const int _0_Fourth_Arg_Must_Have_Same_Primtype_As_First = 121;
        [Message("{0}: destination too short")]
        public const int _0_Destination_Too_Short = 122;
        [Message("{0}: primtype TABLE not supported")]
        public const int _0_Primtype_TABLE_Not_Supported = 124;
        [Message("{0}: keys must have the same type to use default comparison")]
        public const int _0_Keys_Must_Have_The_Same_Type_To_Use_Default_Comparison = 125;
        [Message("{0}: key primtypes must be ATOM, FIX, or STRING to use default comparison")]
        public const int _0_Key_Primtypes_Must_Be_ATOM_FIX_Or_STRING_To_Use_Default_Comparison = 126;
        [Message("no OBLIST path")]
        public const int No_OBLIST_Path = 127;
        [Message("{0}: specifier must be NONE, BYTE, or WORD")]
        public const int _0_Specifier_Must_Be_NONE_BYTE_Or_WORD = 128;
        [Message("{0}: invalid table size")]
        public const int _0_Invalid_Table_Size = 129;
        [Message("{0}: second arg must not be negative")]
        public const int _0_Second_Arg_Must_Not_Be_Negative = 130;
        [Message("{0}: TIME is only meaningful in version 3")]
        public const int _0_TIME_Is_Only_Meaningful_In_Version_3 = 131;
        [Message("{0}: first arg must be DEFINED, ROOMS-FIRST, ROOMS-AND-LGS-FIRST, or ROOMS-LAST")]
        public const int _0_First_Arg_Must_Be_DEFINED_ROOMSFIRST_ROOMSANDLGSFIRST_Or_ROOMSLAST = 132;
        [Message("{0}: first arg must be REVERSE-DEFINED")]
        public const int _0_First_Arg_Must_Be_REVERSEDEFINED = 133;
        [Message("{0}: alphabet number must be between 0 and 2")]
        public const int _0_Alphabet_Number_Must_Be_Between_0_And_2 = 134;
        [Message("{0}: requires NEW-PARSER? option")]
        public const int _0_Requires_NEWPARSER_Option = 135;
        [Message("a SEGMENT can only be evaluated inside a structure")]
        public const int A_SEGMENT_Can_Only_Be_Evaluated_Inside_A_Structure = 136;
        [Message("{0}: bad OUTCHAN")]
        public const int _0_Bad_OUTCHAN = 138;
        [Message("{0}: not supported by this type of channel")]
        public const int _0_Not_Supported_By_This_Type_Of_Channel = 143;
        [Message("{0}: first arg must be non-negative")]
        public const int _0_First_Arg_Must_Be_Nonnegative = 144;
        [Message("{0}: not supported by type")]
        public const int _0_Not_Supported_By_Type = 145;
        [Message("FORM in PROPDEF output pattern must be BYTE, WORD, STRING, OBJECT, ROOM, GLOBAL, NOUN, ADJ, or VOC")]
        public const int FORM_In_PROPDEF_Output_Pattern_Must_Be_BYTE_WORD_STRING_OBJECT_ROOM_GLOBAL_NOUN_ADJ_Or_VOC = 147;
        [Message("{0}: writing past end of structure")]
        public const int _0_Writing_Past_End_Of_Structure = 149;
        [Message("{0}: expected 0 <= key offset < record size")]
        public const int _0_Expected_0__Key_Offset__Record_Size = 150;
        [Message("{0}: vector length must be a multiple of record size")]
        public const int _0_Vector_Length_Must_Be_A_Multiple_Of_Record_Size = 151;
        [Message("{0}: all vectors must have the same number of records")]
        public const int _0_All_Vectors_Must_Have_The_Same_Number_Of_Records = 154;
        [Message("unrecognized object option: {0}")]
        public const int Unrecognized_Object_Option_0 = 157;
        [Message("unrecognized object option (NEW-SFLAGS used): {0}")]
        public const int Unrecognized_Object_Option_NEWSFLAGS_Used_0 = 158;
        [Message("{0} must have a GVAL to use NEW-SFLAGS")]
        public const int _0_Must_Have_A_GVAL_To_Use_NEWSFLAGS = 159;
        [Message("GVAL of {0} must be a FIX")]
        public const int GVAL_Of_0_Must_Be_A_FIX = 160;
        [Message("GVAL of {0} must be between 0 and 255")]
        public const int GVAL_Of_0_Must_Be_Between_0_And_255 = 161;
        [Message("unrecognized ATOM in DECL pattern: {0}")]
        public const int Unrecognized_ATOM_In_DECL_Pattern_0 = 162;
        [Message("misplaced {0}")]
        public const int Misplaced_0 = 163;
        [Message("{0}: already defined: {1}")]
        public const int _0_Already_Defined_1 = 164;
        [Message("unrecognized FSUBR name: {0}")]
        public const int Unrecognized_FSUBR_Name_0 = 166;
        [Message("incompatible classifications merging words {0} ({1}) <- {2} ({3})")]
        public const int Incompatible_Classifications_Merging_Words_0_1__2_3 = 167;
        [Message("overloaded semantics merging words {0} <- {1}")]
        public const int Overloaded_Semantics_Merging_Words_0__1 = 168;
        [Message("word {0} is not a {1}")]
        public const int Word_0_Is_Not_A_1 = 169;
        [Message("{0}: new classification {1} is incompatible with previous {2}")]
        public const int _0_New_Classification_1_Is_Incompatible_With_Previous_2 = 171;
        [Message("index out of range: {0}")]
        public const int Index_Out_Of_Range_0 = 172;
        [Message("value too fancy for TELL output template: {0}")]
        public const int Value_Too_Fancy_For_TELL_Output_Template_0 = 173;
        [Message("expected {0} LVAL(s) in TELL output template but found {1}")]
        public const int Expected_0_LVALs_In_TELL_Output_Template_But_Found_1 = 174;
        [Message("unexpected type in TELL token spec: {0}")]
        public const int Unexpected_Type_In_TELL_Token_Spec_0 = 175;
        [Message("{0}: not a registered type: {1}")]
        public const int _0_Not_A_Registered_Type_1 = 176;
        [Message("{0}: already registered: {1}")]
        public const int _0_Already_Registered_1 = 177;
        [Message("{0}: unrecognized primtype: {1}")]
        public const int _0_Unrecognized_Primtype_1 = 178;
        [Message("unrecognized SUBR name: {0}")]
        public const int Unrecognized_SUBR_Name_0 = 179;
        [Message("{0}: all atoms must be on internal oblist {1}, failed for {2}")]
        public const int _0_All_Atoms_Must_Be_On_Internal_Oblist_1_Failed_For_2 = 180;
        [Message("{0}: bad LVAL of OBLIST")]
        public const int _0_Bad_LVAL_Of_OBLIST = 182;
        [Message("{0}: no such package: {1}")]
        public const int _0_No_Such_Package_1 = 183;
        [Message("{0}: wrong package type, expected {1}")]
        public const int _0_Wrong_Package_Type_Expected_1 = 184;
        [Message("{0}: type is already registered: {1}")]
        public const int _0_Type_Is_Already_Registered_1 = 185;
        [Message("{0}: unrecognized base type: {1}")]
        public const int _0_Unrecognized_Base_Type_1 = 186;
        [Message("{0}: unrecognized non-quoted value in field definition: {1}")]
        public const int _0_Unrecognized_Nonquoted_Value_In_Field_Definition_1 = 187;
        [Message("{0}: unrecognized tag in defaults section: {1}")]
        public const int _0_Unrecognized_Tag_In_Defaults_Section_1 = 188;
        [Message("calling undefined atom: {0}")]
        public const int Calling_Undefined_Atom_0 = 190;
        [Message("not an applicable type: {0}")]
        public const int Not_An_Applicable_Type_0 = 191;
        [Message("unexpected FORM in arg spec: {0}")]
        public const int Unexpected_FORM_In_Arg_Spec_0 = 192;
        [Message("expected atom in arg spec but found {0}")]
        public const int Expected_Atom_In_Arg_Spec_But_Found_0 = 193;
        [Message("unrecognized argument name in body DECL: {0}")]
        public const int Unrecognized_Argument_Name_In_Body_DECL_0 = 194;
        [Message("conflicting DECLs for atom: {0}")]
        public const int Conflicting_DECLs_For_Atom_0 = 195;
        [Message("{0}: {1} element(s) requested but only {2} available")]
        public const int _0_1_Elements_Requested_But_Only_2_Available = 196;
        [Message("{0}: destination type not supported: {1}")]
        public const int _0_Destination_Type_Not_Supported_1 = 197;
        [Message("variable in PROPDEF output pattern is not captured by input pattern: {0}")]
        public const int Variable_In_PROPDEF_Output_Pattern_Is_Not_Captured_By_Input_Pattern_0 = 200;
        [Message("{0} FORM in PROPDEF output pattern must have length {1}")]
        public const int _0_FORM_In_PROPDEF_Output_Pattern_Must_Have_Length_1 = 201;
        [Message("{0}: first argument must be an LVAL or FIX")]
        public const int _0_First_Argument_Must_Be_An_LVAL_Or_FIX = 202;
        [Message("{0}: second argument must be an atom")]
        public const int _0_Second_Argument_Must_Be_An_Atom = 203;
        [Message("PROPDEF constant '{0}' defined at conflicting positions")]
        public const int PROPDEF_Constant_0_Defined_At_Conflicting_Positions = 204;
        [Message("property '{0}' initializer doesn't match any supported patterns")]
        public const int Property_0_Initializer_Doesnt_Match_Any_Supported_Patterns = 205;
        [Message("{0}: file not found: {1}")]
        public const int _0_File_Not_Found_1 = 207;
        [Message("{0}: error loading file: {1}")]
        public const int _0_Error_Loading_File_1 = 208;
        [Message("{0}: unrecognized flag: {1}")]
        public const int _0_Unrecognized_Flag_1 = 209;
        [Message("{0}: section has already been referenced: {1}")]
        public const int _0_Section_Has_Already_Been_Referenced_1 = 210;
        [Message("{0}: section has already been inserted: {1}")]
        public const int _0_Section_Has_Already_Been_Inserted_1 = 211;
        [Message("{0}: duplicate replacement for section: {1}")]
        public const int _0_Duplicate_Replacement_For_Section_1 = 212;
        [Message("{0}: bad state: {1}")]
        public const int _0_Bad_State_1 = 213;
        [Message("{0}: duplicate default for section: {1}")]
        public const int _0_Duplicate_Default_For_Section_1 = 214;
        [Message("{0}: {1}")]
        public const int UserSpecifiedError_0_1 = 216;
        [Message("{0}: conflicting DECLs for atom: {1}")]
        public const int _0_Conflicting_DECLs_For_Atom_1 = 217;
        [Message("{0}: too many routine arguments: only {1} allowed in V{2}")]
        public const int _0_Too_Many_Routine_Arguments_Only_1_Allowed_In_V2 = 220;
        [Message("{0}: symbol is already defined: {1}")]
        public const int _0_Symbol_Is_Already_Defined_1 = 224;
        [Message("{0}: flags must be atoms")]
        public const int _0_Flags_Must_Be_Atoms = 226;
        [Message("{0}: expected a list after PATTERN")]
        public const int _0_Expected_A_List_After_PATTERN = 227;
        [Message("{0}: expected a value after SEGMENT")]
        public const int _0_Expected_A_Value_After_SEGMENT = 228;
        [Message("{0}: PATTERN must not be empty")]
        public const int _0_PATTERN_Must_Not_Be_Empty = 230;
        [Message("{0}: vector may only appear at the end of a PATTERN")]
        public const int _0_Vector_May_Only_Appear_At_The_End_Of_A_PATTERN = 231;
        [Message("{0}: vector in PATTERN must have at least 2 elements")]
        public const int _0_Vector_In_PATTERN_Must_Have_At_Least_2_Elements = 232;
        [Message("{0}: vector in PATTERN must start with REST")]
        public const int _0_Vector_In_PATTERN_Must_Start_With_REST = 233;
        [Message("{0}: following elements of vector in PATTERN must be BYTE or WORD")]
        public const int _0_Following_Elements_Of_Vector_In_PATTERN_Must_Be_BYTE_Or_WORD = 234;
        [Message("{0}: PATTERN may only contain BYTE, WORD, or a REST vector")]
        public const int _0_PATTERN_May_Only_Contain_BYTE_WORD_Or_A_REST_Vector = 235;
        [Message("{0}: unrecognized version name (must be ZIP, EZIP, XZIP, YZIP)")]
        public const int _0_Unrecognized_Version_Name_Must_Be_ZIP_EZIP_XZIP_YZIP = 236;
        [Message("{0}: version number out of range (must be 3-6)")]
        public const int _0_Version_Number_Out_Of_Range_Must_Be_36 = 237;
        [Message("{0}: arg must be an atom or a FIX")]
        public const int _0_Arg_Must_Be_An_Atom_Or_A_FIX = 238;
        [Message("{0}: unrecognized option {1}")]
        public const int _0_Unrecognized_Option_1 = 239;
        [Message("{0}: alphabet {1} needs {2} characters")]
        public const int _0_Alphabet_1_Needs_2_Characters = 240;
        [Message("{0}: unrecognized language: {1}")]
        public const int _0_Unrecognized_Language_1 = 241;
        [Message("{0}: unrecognized part of speech: {1}")]
        public const int _0_Unrecognized_Part_Of_Speech_1 = 242;
        [Message("{0}: not applicable: {1}")]
        public const int _0_Not_Applicable_1 = 244;
        [Message("{0}: no expressions found")]
        public const int _0_No_Expressions_Found = 245;
        [Message("{0}: OBLIST already contains an atom named '{1}'")]
        public const int _0_OBLIST_Already_Contains_An_Atom_Named_1 = 246;
        [Message("{0}: atom '{1}' is already on an OBLIST")]
        public const int _0_Atom_1_Is_Already_On_An_OBLIST = 247;
        [Message("{0}: atom '{1}' has no global value")]
        public const int _0_Atom_1_Has_No_Global_Value = 250;
        [Message("{0}: atom '{1}' has no local value")]
        public const int _0_Atom_1_Has_No_Local_Value = 251;
        [Message("{0}: atom '{1}' has no local or global value")]
        public const int _0_Atom_1_Has_No_Local_Or_Global_Value = 252;
        [Message("{0}: primtypes of {1} and {2} differ")]
        public const int _0_Primtypes_Of_1_And_2_Differ = 255;
        [Message("{0}: second arg must be an atom or applicable")]
        public const int _0_Second_Arg_Must_Be_An_Atom_Or_Applicable = 257;
        [Message("syntax error: {0}")]
        public const int Syntax_Error_0 = 258;
        [Message("expected {0} to match DECL {1}, but got {2}")]
        public const int Expected_0_To_Match_DECL_1_But_Got_2 = 259;
        [Message("{0}: only {1} routine arguments allowed in V{2}, so last {3} \"OPT\" argument{4} will never be passed", Severity = Severity.Warning)]
        public const int _0_Only_1_Routine_Arguments_Allowed_In_V2_So_Last_3_OPT_Argument4_Will_Never_Be_Passed = 260;
        [Message("overriding default value for property '{0}'", Severity = Severity.Warning)]
        public const int Overriding_Default_Value_For_Property_0 = 261;
        [Message("ignoring list of flags in syntax definition with no preceding OBJECT", Severity = Severity.Warning)]
        public const int Ignoring_List_Of_Flags_In_Syntax_Definition_With_No_Preceding_OBJECT = 262;
        [Message("{0}: too many {1}s, starting at {1} {2}")]
        public const int _0_Too_Many_1s_Starting_At_1_2 = 265;
        [Message("check types of earlier {0}s, e.g. {0} {1}", Severity = Severity.Info)]
        public const int Check_Types_Of_Earlier_0s_Eg_0_1 = 266;
        [Message("{0}: expected {1}")]
        public const int _0_Expected_1 = 267;
    }
}
