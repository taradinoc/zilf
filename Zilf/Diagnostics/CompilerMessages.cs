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
                [Message("{0}")]
                public const int LegacyError = 0;
        */

        [Message("too many errors, stopping", Severity = Severity.Fatal)]
        public const int Too_Many_Errors = 1;


        // Syntax - 0100

        [Message("all clauses in {0} must be lists")]
        public const int All_Clauses_In_0_Must_Be_Lists = 100;
        [Message("bare atom '{0}' treated as true here", Severity = Severity.Warning)]
        public const int Bare_Atom_0_Treated_As_True_Here = 101;
        [Message("did you mean the variable?", Severity = Severity.Info)]
        public const int Did_You_Mean_The_Variable = 102;
        [Message("bare atom '{0}' used as operand is not a global variable")]
        public const int Bare_Atom_0_Used_As_Operand_Is_Not_A_Global_Variable = 103;
        [Message("conditions in VERSION? clauses must be atoms")]
        public const int Conditions_In_In_VERSION_Clauses_Must_Be_Atoms = 104;
        [Message("expected an atom after {0}")]
        public const int Expected_An_Atom_After_0 = 105;
        [Message("expected binding list at start of {0}")]
        public const int Expected_Binding_List_At_Start_Of_0 = 106;
        [Message("FORM must start with an atom")]
        public const int FORM_Must_Start_With_An_Atom = 107;
        [Message("invalid atom binding")]
        public const int Invalid_Atom_Binding = 108;
        [Message("property specification must start with an atom")]
        public const int Property_Specification_Must_Start_With_An_Atom = 109;
        [Message("unrecognized atom in VERSION? (must be ZIP, EZIP, XZIP, YZIP, ELSE/T)")]
        public const int Unrecognized_Atom_In_VERSION_Must_Be_ZIP_EZIP_XZIP_YZIP_ELSET = 110;
        [Message("version number out of range (must be 3-8)")]
        public const int Version_Number_Out_Of_Range_Must_Be_38 = 111;
        [Message("{0} requires {1} argument{1:s}")]
        public const int _0_Requires_1_Argument1s = 112;
        [Message("{0}: argument {1}: {2}")]
        public const int _0_Argument_1_2 = 113;
        [Message("{0}: expected {1} element{1:s} in binding list")]
        public const int _0_Expected_1_Element1s_In_Binding_List = 114;
        [Message("{0}: {1} element in binding list must be {2}")]
        public const int _0_1_Element_In_Binding_List_Must_Be_2 = 115;
        [Message("{0}: first list element must be an atom")]
        public const int _0_First_List_Element_Must_Be_An_Atom = 116;
        [Message("{0}: list must have 2 elements")]
        public const int _0_List_Must_Have_2_Elements = 117;
        [Message("{0}: missing binding list")]
        public const int _0_Missing_Binding_List = 118;
        [Message("{0}: one operand must be -1")]
        public const int _0_One_Operand_Must_Be_1 = 119;
        [Message("{0}: second list element must be 0 or 1")]
        public const int _0_Second_List_Element_Must_Be_0_Or_1 = 120;
        [Message("elements of binding list must be atoms or lists")]
        public const int Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists = 121;
        [Message("unrecognized {0}: {1}")]
        public const int Unrecognized_0_1 = 122;
        [Message("expressions of this type cannot be compiled")]
        public const int Expressions_Of_This_Type_Cannot_Be_Compiled = 123;
        [Message("misplaced bracket in COND?", Severity = Severity.Info)]
        public const int Misplaced_Bracket_In_COND = 124;

        // Definitions - 0200

        [Message("bare atom '{0}' interpreted as global variable index", Severity = Severity.Warning)]
        public const int Bare_Atom_0_Interpreted_As_Global_Variable_Index = 200;
        [Message("duplicate {0} definition: {1}")]
        public const int Duplicate_0_Definition_1 = 201;
        [Message("mentioned object '{0}' is never defined", Severity = Severity.Warning)]
        public const int Mentioned_Object_0_Is_Never_Defined = 202;
        [Message("missing 'GO' routine")]
        public const int Missing_GO_Routine = 203;
        [Message("no such {0} variable '{1}', using the {2} instead", Severity = Severity.Warning)]
        public const int No_Such_0_Variable_1_Using_The_2_Instead = 204;
        [Message("no such object: {0}")]
        public const int No_Such_Object_0 = 205;
        [Message("non-vocab constant '{0}' conflicts with vocab word '{1}'")]
        public const int Nonvocab_Constant_0_Conflicts_With_Vocab_Word_1 = 206;
        [Message("undefined {0}: {1}")]
        public const int Undefined_0_1 = 207;
        [Message("{0} mismatch for '{1}': using {2} as before", Severity = Severity.Warning)]
        public const int _0_Mismatch_For_1_Using_2_As_Before = 208;

        // Z-machine Structures - 0300

        [Message("property has no value: {0}")]
        public const int Property_Has_No_Value_0 = 300;
        [Message("property '{0}' is too long (max {1} byte{1:s})")]
        public const int Property_0_Is_Too_Long_Max_1_Byte1s = 301;
        [Message("PROPSPEC for property '{0}' returned a bad value: {1}")]
        public const int PROPSPEC_For_Property_0_Returned_A_Bad_Value_1 = 302;
        [Message("value for '{0}' property must be {1}")]
        public const int Value_For_0_Property_Must_Be_1 = 303;
        [Message("values for '{0}' property must be {1}")]
        public const int Values_For_0_Property_Must_Be_1 = 304;
        [Message("ONE-BYTE-PARTS-OF-SPEECH loses data for '{0}'", Severity = Severity.Warning)]
        public const int ONEBYTEPARTSOFSPEECH_Loses_Data_For_0 = 305;
        [Message("discarding the {0} part of speech for '{1}'", Severity = Severity.Warning)]
        public const int Discarding_The_0_Part_Of_Speech_For_1 = 306;
        [Message("GVAL of '{0}' must be {1}")]
        public const int GVAL_Of_0_Must_Be_1 = 307;
        [Message("too many parts of speech for '{0}': {1}", Severity = Severity.Warning)]
        public const int Too_Many_Parts_Of_Speech_For_0_1 = 308;
        [Message("WORD-FLAGS-LIST must have an even number of elements")]
        public const int WORDFLAGSLIST_Must_Have_An_Even_Number_Of_Elements = 309;

        // Platform Limits - 0400

        [Message("expression needs temporary variables, not allowed here")]
        public const int Expression_Needs_Temporary_Variables_Not_Allowed_Here = 400;
        [Message("header extensions not supported for this target")]
        public const int Header_Extensions_Not_Supported_For_This_Target = 401;
        [Message("too many call arguments: only {0} allowed in V{1}")]
        public const int Too_Many_Call_Arguments_Only_0_Allowed_In_V1 = 402;
        [Message("this arg count would be legal in other Z-machine versions, e.g. V{0}", Severity = Severity.Info)]
        public const int This_Arg_Count_Would_Be_Legal_In_Other_Zmachine_Versions_Eg_V0 = 403;
        [Message("too many {0}: {1} defined, only {2} allowed")]
        public const int Too_Many_0_1_Defined_Only_2_Allowed = 404;
        [Message("{0} is not supported in this Z-machine version")]
        public const int _0_Is_Not_Supported_In_This_Zmachine_Version = 405;
        [Message("optional args with non-constant defaults not supported for this target")]
        public const int Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target = 406;
        [Message("{0}: field '{1}' is not writable")]
        public const int _0_Field_1_Is_Not_Writable = 407;
        [Message("{0}: field '{1}' is not supported in this Z-machine version")]
        public const int _0_Field_1_Is_Not_Supported_In_This_Zmachine_Version = 408;
        [Message("{0}: field '{1}' is not a word field: {1}")]
        public const int _0_Field_1_Is_Not_A_Word_Field = 409;

        // Misc - 0500

        [Message("AGAIN requires a PROG/REPEAT block or routine")]
        public const int AGAIN_Requires_A_PROGREPEAT_Block_Or_Routine = 500;
        [Message("non-constant initializer for {0} '{1}': {2}")]
        public const int Nonconstant_Initializer_For_0_1_2 = 501;
        [Message("RETURN value ignored: PROG/REPEAT block is in void context", Severity = Severity.Warning)]
        public const int RETURN_Value_Ignored_PROGREPEAT_Block_Is_In_Void_Context = 502;
        [Message("soft variable '{0}' may not be used here")]
        public const int Soft_Variable_0_May_Not_Be_Used_Here = 503;
        [Message("treating SET to 0 as true here", Severity = Severity.Warning)]
        public const int Treating_SET_To_0_As_True_Here = 504;
        [Message("{0}: clauses after else part will never be evaluated", Severity = Severity.Warning)]
        public const int _0_Clauses_After_Else_Part_Will_Never_Be_Evaluated = 505;
        [Message("undeclared compilation flag '{0}'?", Severity = Severity.Info)]
        public const int Undeclared_Compilation_Flag_0 = 506;
        [Message("{0}: condition is always {1}", Severity = Severity.Warning)]
        public const int _0_Condition_Is_Always_1 = 507;
    }
}
