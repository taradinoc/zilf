/* Copyright 2010-2023 Tara McGrew
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
    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "VERSION")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "FORM")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ELSET")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "s")]
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "sIn")]
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "BeAn")]
    [MessageSet("ZIL")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class CompilerMessages
    {
        CompilerMessages()
        {
        }

        /*
                [Error("{0}")]
                public const int LegacyError = 0;
        */

        [Fatal("too many errors, stopping")]
        public const int Too_Many_Errors = 1;

        // Syntax - 0100

        [Error("all clauses in {0} must be lists")]
        public const int All_Clauses_In_0_Must_Be_Lists = 100;
        [Warning("bare atom '{0}' treated as true here")]
        public const int Bare_Atom_0_Treated_As_True_Here = 101;
        [Info("did you mean the variable?")]
        public const int Did_You_Mean_The_Variable = 102;
        [Error("bare atom '{0}' used as operand is not a global variable")]
        public const int Bare_Atom_0_Used_As_Operand_Is_Not_A_Global_Variable = 103;
        [Error("conditions in VERSION? clauses must be atoms")]
        public const int Conditions_In_In_VERSION_Clauses_Must_Be_Atoms = 104;
        //[Error("expected an atom after {0}")]
        //public const int Expected_An_Atom_After_0 = 105;
        [Error("expected binding list at start of {0}")]
        public const int Expected_Binding_List_At_Start_Of_0 = 106;
        [Error("FORM must start with an atom")]
        public const int FORM_Must_Start_With_An_Atom = 107;
        [Error("invalid atom binding")]
        public const int Invalid_Atom_Binding = 108;
        [Error("property specification must start with an atom")]
        public const int Property_Specification_Must_Start_With_An_Atom = 109;
        [Error("unrecognized atom in VERSION? (must be ZIP, EZIP, XZIP, YZIP, ELSE/T)")]
        public const int Unrecognized_Atom_In_VERSION_Must_Be_ZIP_EZIP_XZIP_YZIP_ELSET = 110;
        [Error("version number out of range (must be 3-8)")]
        public const int Version_Number_Out_Of_Range_Must_Be_38 = 111;
        [Error("{0} requires {1} argument{1:s}")]
        public const int _0_Requires_1_Argument1s = 112;
        [Error("{0}: argument {1}: {2}")]
        public const int _0_Argument_1_2 = 113;
        [Error("{0}: expected {1} element{1:s} in binding list")]
        public const int _0_Expected_1_Element1s_In_Binding_List = 114;
        [Error("{0}: {1} element in binding list must be {2}")]
        public const int _0_1_Element_In_Binding_List_Must_Be_2 = 115;
        [Error("{0}: first list element must be an atom")]
        public const int _0_First_List_Element_Must_Be_An_Atom = 116;
        [Error("{0}: list must have 2 elements")]
        public const int _0_List_Must_Have_2_Elements = 117;
        [Error("{0}: missing binding list")]
        public const int _0_Missing_Binding_List = 118;
        [Error("{0}: one operand must be -1")]
        public const int _0_One_Operand_Must_Be_1 = 119;
        [Error("{0}: second list element must be 0 or 1")]
        public const int _0_Second_List_Element_Must_Be_0_Or_1 = 120;
        [Error("elements of binding list must be atoms or lists")]
        public const int Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists = 121;
        [Error("unrecognized {0}: {1}")]
        public const int Unrecognized_0_1 = 122;
        [Error("expressions of type '{0}' cannot be compiled")]
        public const int Expressions_Of_Type_0_Cannot_Be_Compiled = 123;
        [Info("misplaced bracket in COND or loop?")]
        public const int Misplaced_Bracket_In_COND_Or_Loop = 124;
        [Error("bare atom '{0}' is not a TELL token or property")]
        public const int Bare_Atom_0_Is_Not_A_TELL_Token_Or_Property = 125;

        // Definitions - 0200

        [Warning("bare atom '{0}' interpreted as global variable index", Noisy = true)]
        public const int Bare_Atom_0_Interpreted_As_Global_Variable_Index = 200;
        [Error("duplicate {0} definition: {1}")]
        public const int Duplicate_0_Definition_1 = 201;
        [Warning("mentioned object '{0}' is never defined")]
        public const int Mentioned_Object_0_Is_Never_Defined = 202;
        [Error("missing 'GO' routine")]
        public const int Missing_GO_Routine = 203;
        [Warning("no such {0} variable '{1}', using the {2} instead", Noisy = true)]
        public const int No_Such_0_Variable_1_Using_The_2_Instead = 204;
        [Error("no such object: {0}")]
        public const int No_Such_Object_0 = 205;
        [Error("non-vocab constant '{0}' conflicts with vocab word '{1}'")]
        public const int Nonvocab_Constant_0_Conflicts_With_Vocab_Word_1 = 206;
        [Error("undefined {0}: {1}")]
        public const int Undefined_0_1 = 207;
        [Warning("{0} mismatch for '{1}': using {2} as before", Noisy = true)]
        public const int _0_Mismatch_For_1_Using_2_As_Before = 208;
        [Error("mentioned object '{0}' is defined elsewhere as a {1}")]
        public const int Mentioned_Object_0_Is_Defined_Elsewhere_As_A_1 = 209;
        [Warning("local variable '{0}' is never used", Noisy = true)]
        public const int Local_Variable_0_Is_Never_Used = 210;
        [Warning("flag '{0}' is defined but never used", Noisy = true)]
        public const int Flag_0_Is_Defined_But_Never_Used = 211;
        [Warning("property '{0}' is defined but never used", Noisy = true)]
        public const int Property_0_Is_Defined_But_Never_Used = 212;
        [Warning("routine '{0}' is defined but never used", Noisy = true)]
        public const int Routine_0_Is_Defined_But_Never_Used = 213;

        // Z-machine Structures - 0300

        [Error("property has no value: {0}")]
        public const int Property_Has_No_Value_0 = 300;
        [Error("property '{0}' is too long (max {1} byte{1:s})")]
        public const int Property_0_Is_Too_Long_Max_1_Byte1s = 301;
        [Error("PROPSPEC for property '{0}' returned a bad value: {1}")]
        public const int PROPSPEC_For_Property_0_Returned_A_Bad_Value_1 = 302;
        //[Error("value for '{0}' property must be {1}")]
        //public const int Value_For_0_Property_Must_Be_1 = 303;
        [Error("values for '{0}' property must be {1}")]
        public const int Values_For_0_Property_Must_Be_1 = 304;
        [Warning("ONE-BYTE-PARTS-OF-SPEECH loses data for '{0}'")]
        public const int ONEBYTEPARTSOFSPEECH_Loses_Data_For_0 = 305;
        [Warning("discarding the {0} part of speech for '{1}'", Noisy = true)]
        public const int Discarding_The_0_Part_Of_Speech_For_1 = 306;
        [Error("GVAL of '{0}' must be {1}")]
        public const int GVAL_Of_0_Must_Be_1 = 307;
        [Warning("too many parts of speech for '{0}': {1}", Noisy = true)]
        public const int Too_Many_Parts_Of_Speech_For_0_1 = 308;
        [Error("WORD-FLAGS-LIST must have an even number of elements")]
        public const int WORDFLAGSLIST_Must_Have_An_Even_Number_Of_Elements = 309;
        [Warning("vocab collision: '{0}' and '{1}' are indistinguishable and will be merged", Noisy = true)]
        public const int Vocab_Collision_0_And_1_Are_Indistinguishable_And_Will_Be_Merged = 310;
        [Info("they would be distinguishable in Z-machine V4 or above")]
        public const int They_Would_Be_Distinguishable_In_Zmachine_V4_Or_Above = 311;
        [Error("TOPIC in SYNTAX requires non-compact old parser syntax tables")]
        public const int TOPIC_In_SYNTAX_Requires_Noncompact_Old_Parser_Syntax_Tables = 312;


        // Platform Limits - 0400

        [Error("{0} are not allowed in the entry point routine")]
        public const int _0_Are_Not_Allowed_In_The_Entry_Point_Routine = 400;
        [Error("header extensions not supported for this target")]
        public const int Header_Extensions_Not_Supported_For_This_Target = 401;
        [Error("too many call arguments: only {0} allowed in V{1}")]
        public const int Too_Many_Call_Arguments_Only_0_Allowed_In_V1 = 402;
        [Info("this would be legal in other Z-machine versions, e.g. V{0}")]
        public const int This_Would_Be_Legal_In_Other_Zmachine_Versions_Eg_V0 = 403;
        [Error("too many {0}: {1} defined, only {2} allowed")]
        public const int Too_Many_0_1_Defined_Only_2_Allowed = 404;
        [Error("{0} is not supported in this Z-machine version")]
        public const int _0_Is_Not_Supported_In_This_Zmachine_Version = 405;
        [Error("optional args with non-constant defaults not supported for this target")]
        public const int Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target = 406;
        [Error("{0}: field '{1}' is not writable")]
        public const int _0_Field_1_Is_Not_Writable = 407;
        [Error("{0}: field '{1}' is not supported in this Z-machine version")]
        public const int _0_Field_1_Is_Not_Supported_In_This_Zmachine_Version = 408;
        [Error("{0}: field '{1}' is not a word field: {1}")]
        public const int _0_Field_1_Is_Not_A_Word_Field = 409;
        [Warning("ZSCII {0} ({1}) cannot safely be printed in Z-machine version {2}")]
        public const int ZSCII_0_1_Cannot_Safely_Be_Printed_In_Zmachine_Version_2 = 410;
        [Error("{0} is not supported when targeting Glulx")]
        public const int _0_Is_Not_Supported_When_Targeting_Glulx = 411;
        [Error("{0} is not supported when targeting the Z-machine")]
        public const int _0_Is_Not_Supported_When_Targeting_The_Zmachine = 412;
        [Error("{0}: field '{1}' is not supported when targeting Glulx")]
        public const int _0_Field_1_Is_Not_Supported_When_Targeting_Glulx = 413;
        [Error("character '{0}' is not part of standard ZSCII and cannot be printed in Z-machine version {1}")]
        public const int Character_0_Is_Not_Part_Of_Standard_ZSCII_And_Cannot_Be_Printed_In_Zmachine_Version_1 = 414;
        [Error("no room left in Unicode translation table for character '{0}'")]
        public const int No_Room_Left_In_Unicode_Translation_Table_For_Character_0 = 415;
        [Error("{0} is not supported when targeting Cornerstone")]
        public const int _0_Is_Not_Supported_When_Targeting_Cornerstone = 416;

        // Misc - 0500

        [Error("AGAIN requires a PROG/REPEAT block or routine")]
        public const int AGAIN_Requires_A_PROGREPEAT_Block_Or_Routine = 500;
        [Error("non-constant initializer for {0} '{1}': {2}")]
        public const int Nonconstant_Initializer_For_0_1_2 = 501;
        [Warning("RETURN value ignored: enclosing block/loop is in void context", Noisy = true)]
        public const int RETURN_Value_Ignored_Enclosing_BlockLoop_Is_In_Void_Context = 502;
        [Error("soft variable '{0}' may not be used here")]
        public const int Soft_Variable_0_May_Not_Be_Used_Here = 503;
        [Warning("treating SET to 0 as true here", Noisy = true)]
        public const int Treating_SET_To_0_As_True_Here = 504;
        [Warning("{0}: clauses after else part will never be evaluated")]
        public const int _0_Clauses_After_Else_Part_Will_Never_Be_Evaluated = 505;
        [Info("undeclared compilation flag '{0}'?")]
        public const int Undeclared_Compilation_Flag_0 = 506;
        [Warning("{0}: condition is always {1}")]
        public const int _0_Condition_Is_Always_1 = 507;
        [Error("argument must be a literal string")]
        public const int Argument_Must_Be_Literal_String = 508;
        [Warning("FSET always returns true, FSET? may have been intended")]
        public const int FSET_Always_Returns_True_FSET_May_Have_Been_Intended = 509;
        [Warning("possibly unintended complex DO loop condition (it should be a predicate)")]
        public const int Possibly_Unintended_Complex_DO_Loop_Condition_It_Should_Be_A_Predicate = 510;
        [Warning("possibly unintended complex DO loop increment (it should have side effects)")]
        public const int Possibly_Unintended_Complex_DO_Loop_Increment_It_Should_Have_Side_Effects = 511;
        [Error("Cornerstone backend limitation: {0}")]
        public const int Cornerstone_Backend_Limitation_0 = 512;
        [Warning("GET used with a property name constant, GETP may have been intended")]
        public const int GET_Used_With_A_Property_Name_Constant_GETP_May_Have_Been_Intended = 513;
        [Info("{0} is a pseudo-property and cannot be accessed directly")]
        public const int _0_Is_A_Pseudo_Property_And_Cannot_Be_Accessed_Directly = 514;
    }
}
