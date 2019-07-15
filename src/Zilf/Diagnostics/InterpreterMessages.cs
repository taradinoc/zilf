/* Copyright 2010-2018 Jesse McGrew
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

namespace Zilf.Diagnostics
{
    [MessageSet("MDL")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class InterpreterMessages
    {
        InterpreterMessages()
        {
        }

        /// <summary>
        /// Used as the first argument for a prefixed message when it needs to be emitted in a context where
        /// no suitable function prefix is available, e.g. during compilation.
        /// </summary>
        public const string NoFunction = "<interpreter>";

        // Legacy

        [Error("{0}")]
        public const int LegacyError = 0;
        [Error("{0}: {1}")]
        public const int UserSpecifiedError_0_1 = 1;

        // Syntax - 0100

        [Error("syntax error: {0}")]
        public const int Syntax_Error_0 = 100;
        [Info("did you mean to separate them with OBJECT?")]
        public const int Did_You_Mean_To_Separate_Them_With_OBJECT = 101;
        [Error("element {0} of {1} in {2} must be {3}")]
        public const int Element_0_Of_1_In_2_Must_Be_3 = 102;
        [Error("element {0} of {1} must be {2}")]
        public const int Element_0_Of_1_Must_Be_2 = 103;
        [Error("{0}: expected {1} after {2}")]
        public const int _0_Expected_1_After_2 = 104;
        [Error("FIX/FALSE in PROPDEF output pattern must be at the beginning")]
        public const int FIXFALSE_In_PROPDEF_Output_Pattern_Must_Be_At_The_Beginning = 105;
        [Error("lists and atoms in TELL token specs must come at the beginning")]
        public const int Lists_And_Atoms_In_TELL_Token_Specs_Must_Come_At_The_Beginning = 106;
/*
        [Error("malformed GVAL in TELL token spec")]
        public const int Malformed_GVAL_In_TELL_Token_Spec = 107;
*/
        [Error("missing {0} in {1}")]
        public const int Missing_0_In_1 = 108;
        [Info("recognized versions are ZIP, EZIP, XZIP, YZIP, and numbers 3-8")]
        public const int Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_And_Numbers_38 = 109;
        [Info("since NEW-SFLAGS is set, the following options are recognized: {0}")]
        public const int Since_NEWSFLAGS_Is_Set_The_Following_Options_Are_Recognized_0 = 110;
        [Error("TELL token spec ends with an unterminated pattern")]
        public const int TELL_Token_Spec_Ends_With_An_Unterminated_Pattern = 111;
        [Error("too many {0} in syntax definition")]
        public const int Too_Many_0_In_Syntax_Definition = 112;
        [Error("unrecognized {0}: {1}")]
        public const int Unrecognized_0_1 = 113;
        [Error("{0} in {1} must be {2}")]
        public const int _0_In_1_Must_Be_2 = 114;
        [Error("{0} in {1} must have {2} element{2:s}")]
        public const int _0_In_1_Must_Have_2_Element2s = 115;
        [Error("{0} must be {1}")]
        public const int _0_Must_Be_1 = 116;
        [Error("{0} must have {1} element{1:s}")]
        public const int _0_Must_Have_1_Element1s = 117;
        [Error("{0}: \"OPT\" after \"AUX\"")]
        public const int _0_OPT_After_AUX = 118;
        [Error("{0}: empty list in arg spec")]
        public const int _0_Empty_List_In_Arg_Spec = 119;
        [Error("{0}: multiple {1} clauses")]
        public const int _0_Multiple_1_Clauses = 120;
        [Error("{0}: not enough elements in 'CONSTRUCTOR spec")]
        public const int _0_Not_Enough_Elements_In_CONSTRUCTOR_Spec = 121;
        [Error("{0}: unrecognized {1}: {2}")]
        public const int _0_Unrecognized_1_2 = 122;
        [Error("{0}: lists in defaults section must start with a quoted atom")]
        public const int _0_Lists_In_Defaults_Section_Must_Start_With_A_Quoted_Atom = 123;
        [Error("{0}: parts of defaults section must be quoted atoms or lists")]
        public const int _0_Parts_Of_Defaults_Section_Must_Be_Quoted_Atoms_Or_Lists = 124;
        [Error("{0}: second element after 'CONSTRUCTOR must be an argument list")]
        public const int _0_Second_Element_After_CONSTRUCTOR_Must_Be_An_Argument_List = 125;
        [Error("{0}: unrecognized argument name in body DECL: {1}")]
        public const int _0_Unrecognized_Argument_Name_In_Body_DECL_1 = 126;
        [Error("{0}: expected atom in arg spec but found '{1}'")]
        public const int _0_Expected_Atom_In_Arg_Spec_But_Found_1 = 127;
        [Error("{0}: expected {1}")]
        public const int _0_Expected_1 = 128;
        [Error("{0}: too many {1}s, starting at {1} {2}")]
        public const int _0_Too_Many_1s_Starting_At_1_2 = 129;
        [Error("{0}: unexpected FORM in arg spec: {1}")]
        public const int _0_Unexpected_FORM_In_Arg_Spec_1 = 130;

        // Type/Format/DECL - 0200

        [Error("calling unassigned atom: {0}")]
        public const int Calling_Unassigned_Atom_0 = 200;
        [Error("CHTYPE away from {0} not supported")]
        public const int CHTYPE_Away_From_0_Not_Supported = 201;
        [Error("CHTYPE to {0} did not produce an applicable object")]
        public const int CHTYPE_To_0_Did_Not_Produce_An_Applicable_Object = 202;
        [Error("CHTYPE to {0} not supported")]
        public const int CHTYPE_To_0_Not_Supported = 203;
        [Error("CHTYPE to {0} requires {1}")]
        public const int CHTYPE_To_0_Requires_1 = 204;
        [Error("environment has expired")]
        public const int Environment_Has_Expired = 205;
        [Error("expected {0} to match DECL {1}, but got {2}")]
        public const int Expected_0_To_Match_DECL_1_But_Got_2 = 206;
        [Error("{0} value of '{1}' must be {2}")]
        public const int _0_Value_Of_1_Must_Be_2 = 207;
        [Error("malformed DECL object")]
        public const int Malformed_DECL_Object = 208;
        [Error("no OBLIST path")]
        public const int No_OBLIST_Path = 209;
        [Error("not an applicable type: {0}")]
        public const int Not_An_Applicable_Type_0 = 210;
        [Error("OFFSET is immutable")]
        public const int OFFSET_Is_Immutable = 211;
        [Error("'{0}' must have a GVAL to use NEW-SFLAGS")]
        public const int _0_Must_Have_A_GVAL_To_Use_NEWSFLAGS = 212;
        [Error("{0}: 'NONE is not allowed after a default field value")]
        public const int _0_NONE_Is_Not_Allowed_After_A_Default_Field_Value = 213;
        [Error("{0}: already defined: {1}")]
        public const int _0_Already_Defined_1 = 214;
        [Error("{0}: atom '{1}' has no {2} value")]
        public const int _0_Atom_1_Has_No_2_Value = 215;
        [Error("{0}: atom '{1}' is already on an OBLIST")]
        public const int _0_Atom_1_Is_Already_On_An_OBLIST = 216;
        [Error("{0}: conflicting DECLs for atom: {1}")]
        public const int _0_Conflicting_DECLs_For_Atom_1 = 217;
        [Error("{0}: iterated values must be CHARACTERs")]
        public const int _0_Iterated_Values_Must_Be_CHARACTERs = 218;
        [Error("{0}: not applicable: {1}")]
        public const int _0_Not_Applicable_1 = 219;
        [Error("{0}: not supported by type")]
        public const int _0_Not_Supported_By_Type = 220;
        [Error("{0}: OBLIST already contains an atom named '{1}'")]
        public const int _0_OBLIST_Already_Contains_An_Atom_Named_1 = 221;
        [Error("{0}: primtypes of '{1}' and '{2}' differ")]
        public const int _0_Primtypes_Of_1_And_2_Differ = 222;
        [Error("{0}: {1} must return {2}")]
        public const int _0_1_Must_Return_2 = 223;
        [Info("check types of earlier {0}s, e.g. {0} {1}")]
        public const int Check_Types_Of_Earlier_0s_Eg_0_1 = 224;
        [Error("{0} requires {1} additional {2}{1:s}")]
        public const int _0_Requires_1_Additional_21s = 225;
        [Error("{0} requires {1} {2}{1:s}")]
        public const int _0_Requires_1_21s = 226;

        // Structured Values - 0300

        [Error("a SEGMENT can only be evaluated inside a structure")]
        public const int A_SEGMENT_Can_Only_Be_Evaluated_Inside_A_Structure = 300;
        [Error("{0}: all vectors must have the same number of records")]
        public const int _0_All_Vectors_Must_Have_The_Same_Number_Of_Records = 301;
        [Error("{0}: destination too short")]
        public const int _0_Destination_Too_Short = 302;
        [Error("{0}: destination type not supported: {1}")]
        public const int _0_Destination_Type_Not_Supported_1 = 303;
        [Error("{0}: expected 0 <= key offset < record size")]
        public const int _0_Expected_0__Key_Offset__Record_Size = 304;
        [Error("{0}: destination must have same primtype as source")]
        public const int _0_Destination_Must_Have_Same_Primtype_As_Source = 305;
        [Error("{0}: key primtypes must be ATOM, FIX, or STRING to use default comparison")]
        public const int _0_Key_Primtypes_Must_Be_ATOM_FIX_Or_STRING_To_Use_Default_Comparison = 306;
        [Error("{0}: keys must have the same type to use default comparison")]
        public const int _0_Keys_Must_Have_The_Same_Type_To_Use_Default_Comparison = 307;
        [Error("{0}: negative element count")]
        public const int _0_Negative_Element_Count = 308;
        [Error("{0}: not enough elements")]
        public const int _0_Not_Enough_Elements = 309;
        [Error("{0}: primtype TABLE not supported")]
        public const int _0_Primtype_TABLE_Not_Supported = 310;
        [Error("{0}: reading past end of structure")]
        public const int _0_Reading_Past_End_Of_Structure = 311;
        [Error("{0}: sizes must be non-negative")]
        public const int _0_Sizes_Must_Be_Nonnegative = 312;
        [Error("{0}: vector length must be a multiple of record size")]
        public const int _0_Vector_Length_Must_Be_A_Multiple_Of_Record_Size = 313;
        [Error("{0}: writing past end of structure")]
        public const int _0_Writing_Past_End_Of_Structure = 314;
        [Error("{0}: {1} element{1:s} requested but only {2} available")]
        public const int _0_1_Element1s_Requested_But_Only_2_Available = 315;
        [Error("templates cannot be used here")]
        public const int Templates_Cannot_Be_Used_Here = 316;
        [Error("{0}: unaligned table read: element at {1} offset {2} is not a {1}")]
        public const int _0_Unaligned_Table_Read_Element_At_1_Offset_2_Is_Not_A_1 = 317;
        [Error("{0}: element {1} is read-only")]
        public const int _0_Element_1_Is_Read_Only = 318;

        // Z-machine Structures - 0400

        [Error("'{0}' FORM in PROPDEF output pattern must have length {1}")]
        public const int _0_FORM_In_PROPDEF_Output_Pattern_Must_Have_Length_1 = 400;
        [Error("expected {0} LVAL{0:s} in TELL output template but found {1}")]
        public const int Expected_0_LVAL0s_In_TELL_Output_Template_But_Found_1 = 401;
        [Error("FORM in PROPDEF output pattern must be BYTE, WORD, STRING, OBJECT, ROOM, GLOBAL, NOUN, ADJ, or VOC")]
        public const int FORM_In_PROPDEF_Output_Pattern_Must_Be_BYTE_WORD_STRING_OBJECT_ROOM_GLOBAL_NOUN_ADJ_Or_VOC = 402;
        [Warning("ignoring list of flags in syntax definition with no preceding OBJECT")]
        public const int Ignoring_List_Of_Flags_In_Syntax_Definition_With_No_Preceding_OBJECT = 403;
        [Error("incompatible classifications merging words '{0}' ({1}) <- '{2}' ({3})")]
        public const int Incompatible_Classifications_Merging_Words_0_1__2_3 = 404;
        [Error("overloaded semantics merging words '{0}' <- '{1}'")]
        public const int Overloaded_Semantics_Merging_Words_0__1 = 405;
        [Warning("overriding default value for property '{0}'")]
        public const int Overriding_Default_Value_For_Property_0 = 406;
        [Error("PROPDEF constant '{0}' defined at conflicting positions")]
        public const int PROPDEF_Constant_0_Defined_At_Conflicting_Positions = 407;
        [Error("property '{0}' initializer doesn't match any supported patterns")]
        public const int Property_0_Initializer_Doesnt_Match_Any_Supported_Patterns = 408;
        [Error("variable in PROPDEF output pattern is not captured by input pattern: {0}")]
        public const int Variable_In_PROPDEF_Output_Pattern_Is_Not_Captured_By_Input_Pattern_0 = 409;
        [Error("word '{0}' is not a {1}")]
        public const int Word_0_Is_Not_A_1 = 410;
        [Error("{0}: alphabet number must be between 0 and 2")]
        public const int _0_Alphabet_Number_Must_Be_Between_0_And_2 = 411;
        [Error("{0}: alphabet {1} needs {2} character{2:s}")]
        public const int _0_Alphabet_1_Needs_2_Character2s = 412;
        [Error("{0}: flags must be atoms")]
        public const int _0_Flags_Must_Be_Atoms = 413;
        [Error("{0}: following elements of vector in PATTERN must be BYTE or WORD")]
        public const int _0_Following_Elements_Of_Vector_In_PATTERN_Must_Be_BYTE_Or_WORD = 414;
        [Error("{0}: invalid table size")]
        public const int _0_Invalid_Table_Size = 415;
        [Error("{0}: new classification {1} is incompatible with previous {2}")]
        public const int _0_New_Classification_1_Is_Incompatible_With_Previous_2 = 416;
        [Warning("{0}: only {1} routine argument{1:s} allowed in V{2}, so last {3} \"OPT\" argument{3:s} will never be passed", Noisy = true)]
        public const int _0_Only_1_Routine_Argument1s_Allowed_In_V2_So_Last_3_OPT_Argument3s_Will_Never_Be_Passed = 417;
        [Error("{0}: PATTERN may only contain BYTE, WORD, or a REST vector")]
        public const int _0_PATTERN_May_Only_Contain_BYTE_WORD_Or_A_REST_Vector = 418;
        [Error("{0}: PATTERN must not be empty")]
        public const int _0_PATTERN_Must_Not_Be_Empty = 419;
        [Error("{0}: requires NEW-PARSER? option")]
        public const int _0_Requires_NEWPARSER_Option = 420;
        [Error("{0}: specifier must be NONE, BYTE, or WORD")]
        public const int _0_Specifier_Must_Be_NONE_BYTE_Or_WORD = 421;
        [Error("{0}: TIME is only meaningful in version 3")]
        public const int _0_TIME_Is_Only_Meaningful_In_Version_3 = 422;
        [Error("{0}: too many routine arguments: only {1} allowed in V{2}")]
        public const int _0_Too_Many_Routine_Arguments_Only_1_Allowed_In_V2 = 423;
        [Error("{0}: vector may only appear at the end of a PATTERN")]
        public const int _0_Vector_May_Only_Appear_At_The_End_Of_A_PATTERN = 424;
        [Error("{0}: word would be overloaded")]
        public const int _0_Word_Would_Be_Overloaded = 425;
        [Error("too many {0}: only {1} allowed in this vocab format")]
        public const int Too_Many_0_Only_1_Allowed_In_This_Vocab_Format = 426;
        [Error("{0}: routines may not define \"BIND\", \"TUPLE\", or \"ARGS\" arguments")]
        public const int _0_Routines_May_Not_Define_BIND_TUPLE_Or_ARGS_Arguments = 427;

        // Modularity (package system, definitions sections) - 0500

        [Error("{0}: all atoms must be on internal oblist {1}, failed for {2}")]
        public const int _0_All_Atoms_Must_Be_On_Internal_Oblist_1_Failed_For_2 = 500;
        [Error("{0}: bad state: {1}")]
        public const int _0_Bad_State_1 = 501;
        [Warning("{0}: duplicate default for section: {1}")]
        public const int _0_Duplicate_Default_For_Section_1 = 502;
        [Error("{0}: duplicate replacement for section: {1}")]
        public const int _0_Duplicate_Replacement_For_Section_1 = 503;
        [Error("{0}: must be called from within a PACKAGE or DEFINITIONS")]
        public const int _0_Must_Be_Called_From_Within_A_PACKAGE_Or_DEFINITIONS = 504;
        [Error("{0}: must be called from within a PACKAGE")]
        public const int _0_Must_Be_Called_From_Within_A_PACKAGE = 505;
        [Error("{0}: section has already been inserted: {1}")]
        public const int _0_Section_Has_Already_Been_Inserted_1 = 506;
        [Error("{0}: section has already been referenced: {1}")]
        public const int _0_Section_Has_Already_Been_Referenced_1 = 507;
        [Error("{0}: wrong package type, expected {1}")]
        public const int _0_Wrong_Package_Type_Expected_1 = 508;

        // Misc - 0600

        [Error("division by zero")]
        public const int Division_By_Zero = 600;
        [Error("misplaced {0}")]
        public const int Misplaced_0 = 601;
        [Error("{0}: bad OUTCHAN")]
        public const int _0_Bad_OUTCHAN = 602;
        [Error("{0}: error loading file: {1}")]
        public const int _0_Error_Loading_File_1 = 603;
        [Error("{0}: file not found: {1}")]
        public const int _0_File_Not_Found_1 = 604;
        [Error("{0}: no enclosing PROG/REPEAT")]
        public const int _0_No_Enclosing_PROGREPEAT = 605;
        [Error("{0}: no expressions found")]
        public const int _0_No_Expressions_Found = 606;
        [Error("{0}: not supported by this type of channel")]
        public const int _0_Not_Supported_By_This_Type_Of_Channel = 607;
    }
}
