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

namespace Zilf.Diagnostics
{
    [MessageSet("ZIL")]
    public abstract class CompilerMessages
    {
        CompilerMessages()
        {
        }

        [Message("{0}")]
        public const int LegacyError = 0;

        // Syntax

        [Message("all clauses in {0} must be lists")]
        public const int All_Clauses_In_0_Must_Be_Lists = 22;
        [Message("bare atom '{0}' treated as true here", Severity = Severity.Warning)]
        public const int Bare_Atom_0_Treated_As_True_Here = 108;
        [Message("did you mean the variable?", Severity = Severity.Info)]
        public const int Did_You_Mean_The_Variable = 107;
        [Message("bare atom '{0}' used as operand is not a global variable")]
        public const int Bare_Atom_0_Used_As_Operand_Is_Not_A_Global_Variable = 92;
        [Message("conditions in in VERSION? clauses must be ATOMs")]
        public const int Conditions_In_In_VERSION_Clauses_Must_Be_ATOMs = 26;
        [Message("expected a FORM, ATOM, or ADECL but found: {0}")]
        public const int Expected_A_FORM_ATOM_Or_ADECL_But_Found_0 = 93;
        [Message("expected an atom after {0}")]
        public const int Expected_An_Atom_After_0 = 57;
        [Message("expected binding list at start of {0}")]
        public const int Expected_Binding_List_At_Start_Of_0 = 10;
        [Message("FORM must start with an atom")]
        public const int FORM_Must_Start_With_An_Atom = 64;
        [Message("invalid atom binding")]
        public const int Invalid_Atom_Binding = 7;
        [Message("property specification must start with an atom")]
        public const int Property_Specification_Must_Start_With_An_Atom = 70;
        [Message("unrecognized atom in VERSION? (must be ZIP, EZIP, XZIP, YZIP, ELSE/T)")]
        public const int Unrecognized_Atom_In_VERSION_Must_Be_ZIP_EZIP_XZIP_YZIP_ELSET = 24;
        [Message("version number out of range (must be 3-8)")]
        public const int Version_Number_Out_Of_Range_Must_Be_38 = 25;
        [Message("{0} requires {1} argument{1:s}")]
        public const int _0_Requires_1_Argument1s = 115;
        [Message("{0}: argument {1}: {2}")]
        public const int _0_Argument_1_2 = 83;
        [Message("{0}: expected {1} element{1:s} in binding list")]
        public const int _0_Expected_1_Element1s_In_Binding_List = 14;
        [Message("{0}: {1} element in binding list must be {2}")]
        public const int _0_1_Element_In_Binding_List_Must_Be_2 = 12;
        [Message("{0}: first list element must be an atom")]
        public const int _0_First_List_Element_Must_Be_An_Atom = 48;
        [Message("{0}: list must have 2 elements")]
        public const int _0_List_Must_Have_2_Elements = 47;
        [Message("{0}: missing binding list")]
        public const int _0_Missing_Binding_List = 36;
        [Message("{0}: one operand must be -1")]
        public const int _0_One_Operand_Must_Be_1 = 39;
        [Message("{0}: second list element must be 0 or 1")]
        public const int _0_Second_List_Element_Must_Be_0_Or_1 = 49;
        [Message("elements of binding list must be atoms or lists")]
        public const int Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists = 9;
        [Message("unrecognized {0}: 1")]
        public const int Unrecognized_0_1 = 69;

        // Types

        [Message("expressions of this type cannot be compiled")]
        public const int Expressions_Of_This_Type_Cannot_Be_Compiled = 6;
        [Message("misplaced bracket in COND?", Severity = Severity.Info)]
        public const int Misplaced_Bracket_In_COND = 5;

        // Definitions

        [Message("bare atom '{0}' interpreted as global variable index; be sure this is right", Severity = Severity.Warning)]
        public const int Bare_Atom_0_Interpreted_As_Global_Variable_Index_Be_Sure_This_Is_Right = 113;
        [Message("duplicate {0} definition: {1}")]
        public const int Duplicate_0_Definition_1 = 89;
        [Message("mentioned object '{0}' is never defined", Severity = Severity.Warning)]
        public const int Mentioned_Object_0_Is_Never_Defined = 101;
        [Message("missing 'GO' routine")]
        public const int Missing_GO_Routine = 2;
        [Message("no such {0} variable '{1}', using the {2} instead", Severity = Severity.Warning)]
        public const int No_Such_0_Variable_1_Using_The_2_Instead = 102;
        [Message("no such object: {0}")]
        public const int No_Such_Object_0 = 94;
        [Message("non-vocab constant '{0}' conflicts with vocab word '{1}'")]
        public const int Nonvocab_Constant_0_Conflicts_With_Vocab_Word_1 = 31;
        [Message("undefined {0}: {1}")]
        public const int Undefined_0_1 = 33;
        [Message("{0} mismatch for '{1}': using {2} as before", Severity = Severity.Warning)]
        public const int _0_Mismatch_For_1_Using_2_As_Before = 112;

        // Objects

        [Message("property has no value: {0}")]
        public const int Property_Has_No_Value_0 = 75;
        [Message("property '{0}' is too long (max {1} byte{1:s})")]
        public const int Property_0_Is_Too_Long_Max_1_Byte1s = 96;
        [Message("PROPSPEC for property '{0}' returned a bad value: {1}")]
        public const int PROPSPEC_For_Property_0_Returned_A_Bad_Value_1 = 71;
        [Message("value for '{0}' property must be {1}")]
        public const int Value_For_0_Property_Must_Be_1 = 76;
        [Message("values for '{0}' property must be {1}")]
        public const int Values_For_0_Property_Must_Be_1 = 79;

        // Vocab

        [Message("ONE-BYTE-PARTS-OF-SPEECH loses data for '{0}'", Severity = Severity.Warning)]
        public const int ONEBYTEPARTSOFSPEECH_Loses_Data_For_0 = 99;
        [Message("discarding the {0} part of speech for '{1}'", Severity = Severity.Warning)]
        public const int Discarding_The_0_Part_Of_Speech_For_1 = 114;
        [Message("GVAL of '{0}' must be {1}")]
        public const int GVAL_Of_0_Must_Be_1 = 28;
        [Message("too many parts of speech for '{0}': {1}", Severity = Severity.Warning)]
        public const int Too_Many_Parts_Of_Speech_For_0_1 = 100;
        [Message("WORD-FLAGS-LIST must have an even number of elements")]
        public const int WORDFLAGSLIST_Must_Have_An_Even_Number_Of_Elements = 29;

        // Platform Limits

        [Message("expression needs temporary variables, not allowed here")]
        public const int Expression_Needs_Temporary_Variables_Not_Allowed_Here = 32;
        [Message("header extensions not supported for this target")]
        public const int Header_Extensions_Not_Supported_For_This_Target = 3;
        [Message("too many call arguments: only {0} allowed in V{1}")]
        public const int Too_Many_Call_Arguments_Only_0_Allowed_In_V1 = 84;
        [Message("this arg count would be legal in other Z-machine versions, e.g. V{0}", Severity = Severity.Info)]
        public const int This_Arg_Count_Would_Be_Legal_In_Other_Zmachine_Versions_Eg_V0 = 116;
        [Message("too many {0}: {1} defined, only {2} allowed")]
        public const int Too_Many_0_1_Defined_Only_2_Allowed = 85;
        [Message("{0} is not supported in this Z-machine version")]
        public const int _0_Is_Not_Supported_In_This_Zmachine_Version = 97;
        [Message("optional args with non-constant defaults not supported for this target")]
        public const int Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target = 4;
        [Message("{0}: field '{1}' is not writable")]
        public const int _0_Field_1_Is_Not_Writable = 46;
        [Message("{0}: field '{1}' is not supported in this Z-machine version")]
        public const int _0_Field_1_Is_Not_Supported_In_This_Zmachine_Version = 45;
        [Message("{0}: field '{1}' is not a word field: {1}")]
        public const int _0_Field_1_Is_Not_A_Word_Field = 52;

        // Misc

        [Message("AGAIN requires a PROG/REPEAT block or routine")]
        public const int AGAIN_Requires_A_PROGREPEAT_Block_Or_Routine = 43;
        [Message("non-constant initializer for {0} '{1}': {2}")]
        public const int Nonconstant_Initializer_For_0_1_2 = 82;
        [Message("RETURN value ignored: block is in void context", Severity = Severity.Warning)]
        public const int RETURN_Value_Ignored_Block_Is_In_Void_Context = 98;
        [Message("soft variable '{0}' may not be used here")]
        public const int Soft_Variable_0_May_Not_Be_Used_Here = 91;
        [Message("treating SET to 0 as true here", Severity = Severity.Warning)]
        public const int Treating_SET_To_0_As_True_Here = 109;
        [Message("{0}: clauses after else part will never be evaluated", Severity = Severity.Warning)]
        public const int _0_Clauses_After_Else_Part_Will_Never_Be_Evaluated = 111;
    }
}
