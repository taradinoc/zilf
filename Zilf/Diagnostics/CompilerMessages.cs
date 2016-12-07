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
    [MessageSet("ZIL")]
    public abstract class CompilerMessages
    {
        private CompilerMessages()
        {
        }

        [Message("{0}")]
        public const int LegacyError = 0;
        [Message("missing 'GO' routine")]
        public const int Missing_GO_Routine = 2;
        [Message("header extensions not supported for this target")]
        public const int Header_Extensions_Not_Supported_For_This_Target = 3;
        [Message("optional args with non-constant defaults not supported for this target")]
        public const int Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target = 4;
        [Message("lists cannot be returned (misplaced bracket in COND?)")]
        public const int Lists_Cannot_Be_Returned_Misplaced_Bracket_In_COND = 5;
        [Message("values of this type cannot be returned")]
        public const int Values_Of_This_Type_Cannot_Be_Returned = 6;
        [Message("invalid atom binding")]
        public const int Invalid_Atom_Binding = 7;
        [Message("binding with value must be a 2-element list")]
        public const int Binding_With_Value_Must_Be_A_2element_List = 8;
        [Message("elements of binding list must be atoms or lists")]
        public const int Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists = 9;
        [Message("expected binding list at start of DO")]
        public const int Expected_Binding_List_At_Start_Of_DO = 10;
        [Message("{0}: expected 3 or 4 elements in binding list")]
        public const int _0_Expected_3_Or_4_Elements_In_Binding_List = 11;
        [Message("{0}: first element in binding list must be an atom")]
        public const int _0_First_Element_In_Binding_List_Must_Be_An_Atom = 12;
        [Message("expected binding list at start of MAP-CONTENTS")]
        public const int Expected_Binding_List_At_Start_Of_MAPCONTENTS = 13;
        [Message("{0}: expected 2 or 3 elements in binding list")]
        public const int _0_Expected_2_Or_3_Elements_In_Binding_List = 14;
        [Message("{0}: middle element in binding list must be an atom")]
        public const int _0_Middle_Element_In_Binding_List_Must_Be_An_Atom = 16;
        [Message("expected binding list at start of MAP-DIRECTIONS")]
        public const int Expected_Binding_List_At_Start_Of_MAPDIRECTIONS = 17;
        [Message("{0}: expected 3 elements in binding list")]
        public const int _0_Expected_3_Elements_In_Binding_List = 18;
        [Message("{0}: last element in binding list must be an LVAL or GVAL")]
        public const int _0_Last_Element_In_Binding_List_Must_Be_An_LVAL_Or_GVAL = 21;
        [Message("all clauses in {0} must be lists")]
        public const int All_Clauses_In_0_Must_Be_Lists = 22;
        [Message("unrecognized atom in VERSION? (must be ZIP, EZIP, XZIP, YZIP, ELSE/T)")]
        public const int Unrecognized_Atom_In_VERSION_Must_Be_ZIP_EZIP_XZIP_YZIP_ELSET = 24;
        [Message("version number out of range (must be 3-6)")]
        public const int Version_Number_Out_Of_Range_Must_Be_36 = 25;
        [Message("conditions in in VERSION? clauses must be ATOMs")]
        public const int Conditions_In_In_VERSION_Clauses_Must_Be_ATOMs = 26;
        [Message("all clauses in IFFLAG must be lists")]
        public const int All_Clauses_In_IFFLAG_Must_Be_Lists = 27;
        [Message("GVAL of WORD-FLAGS-LIST must be a list")]
        public const int GVAL_Of_WORDFLAGSLIST_Must_Be_A_List = 28;
        [Message("WORD-FLAGS-LIST must have an even number of elements")]
        public const int WORDFLAGSLIST_Must_Have_An_Even_Number_Of_Elements = 29;
        [Message("non-vocab constant '{0}' conflicts with vocab word '{1}'")]
        public const int Nonvocab_Constant_0_Conflicts_With_Vocab_Word_1 = 31;
        [Message("expression needs temporary variables, not allowed here")]
        public const int Expression_Needs_Temporary_Variables_Not_Allowed_Here = 32;
        [Message("undefined action routine: {0}")]
        public const int Undefined_Action_Routine_0 = 33;
        [Message("undefined preaction routine: {0}")]
        public const int Undefined_Preaction_Routine_0 = 34;
        [Message("{0}: first arg must be an activation atom or binding list")]
        public const int _0_First_Arg_Must_Be_An_Activation_Atom_Or_Binding_List = 35;
        [Message("{0}: missing binding list")]
        public const int _0_Missing_Binding_List = 36;
        [Message("unexpected value returned from clause: {0}")]
        public const int Unexpected_Value_Returned_From_Clause_0 = 37;
        [Message("too many hard globals: {0} defined, only {1} allowed")]
        public const int Too_Many_Hard_Globals_0_Defined_Only_1_Allowed = 38;
        [Message("{0}: one operand must be -1")]
        public const int _0_One_Operand_Must_Be_1 = 39;
        [Message("{0}: argument 1 must be 1")]
        public const int _0_Argument_1_Must_Be_1 = 42;
        [Message("AGAIN requires a PROG/REPEAT block or routine")]
        public const int AGAIN_Requires_A_PROGREPEAT_Block_Or_Routine = 43;
        [Message("{0}: unrecognized header field {1}")]
        public const int _0_Unrecognized_Header_Field_1 = 44;
        [Message("{0}: field not supported in this Z-machine version: {1}")]
        public const int _0_Field_Not_Supported_In_This_Zmachine_Version_1 = 45;
        [Message("{0}: field is not writable: {1}")]
        public const int _0_Field_Is_Not_Writable_1 = 46;
        [Message("{0}: list must have 2 elements")]
        public const int _0_List_Must_Have_2_Elements = 47;
        [Message("{0}: first list element must be an atom")]
        public const int _0_First_List_Element_Must_Be_An_Atom = 48;
        [Message("{0}: second list element must be 0 or 1")]
        public const int _0_Second_List_Element_Must_Be_0_Or_1 = 49;
        [Message("{0}: not a word field: {1}")]
        public const int _0_Not_A_Word_Field_1 = 52;
        [Message("{0}: first arg must be an atom or list")]
        public const int _0_First_Arg_Must_Be_An_Atom_Or_List = 54;
        [Message("too many flags requiring high numbers")]
        public const int Too_Many_Flags_Requiring_High_Numbers = 55;
        [Message("FORM inside a routine must start with an atom")]
        public const int FORM_Inside_A_Routine_Must_Start_With_An_Atom = 56;
        [Message("expected an atom after GVAL")]
        public const int Expected_An_Atom_After_GVAL = 57;
        [Message("undefined global or constant: {0}")]
        public const int Undefined_Global_Or_Constant_0 = 58;
        [Message("expected an atom after LVAL")]
        public const int Expected_An_Atom_After_LVAL = 59;
        [Message("undefined local: {0}")]
        public const int Undefined_Local_0 = 60;
        [Message("{0} requires exactly 1 argument")]
        public const int _0_Requires_Exactly_1_Argument = 61;
        [Message("unrecognized routine or instruction: {0}")]
        public const int Unrecognized_Routine_Or_Instruction_0 = 62;
        [Message("unexpected expression in value+predicate context: {0}")]
        public const int Unexpected_Expression_In_Valuepredicate_Context_0 = 63;
        [Message("FORM must start with an atom")]
        public const int FORM_Must_Start_With_An_Atom = 64;
        [Message("bad value type for condition: {0}")]
        public const int Bad_Value_Type_For_Condition_0 = 65;
        [Message("NOT/F? requires exactly one argument")]
        public const int NOTF_Requires_Exactly_One_Argument = 67;
        [Message("unrecognized part of speech: {0}")]
        public const int Unrecognized_Part_Of_Speech_0 = 69;
        [Message("property specification must start with an atom")]
        public const int Property_Specification_Must_Start_With_An_Atom = 70;
        [Message("PROPSPEC for property '{0}' returned a bad value: {1}")]
        public const int PROPSPEC_For_Property_0_Returned_A_Bad_Value_1 = 71;
        [Message("value for IN/LOC property must be an atom")]
        public const int Value_For_INLOC_Property_Must_Be_An_Atom = 74;
        [Message("property has no value: {0}")]
        public const int Property_Has_No_Value_0 = 75;
        [Message("value for DESC property must be a string")]
        public const int Value_For_DESC_Property_Must_Be_A_String = 76;
        [Message("values for FLAGS property must be atoms")]
        public const int Values_For_FLAGS_Property_Must_Be_Atoms = 77;
        [Message("values for SYNONYM property must be atoms")]
        public const int Values_For_SYNONYM_Property_Must_Be_Atoms = 78;
        [Message("values for ADJECTIVE property must be atoms")]
        public const int Values_For_ADJECTIVE_Property_Must_Be_Atoms = 79;
        [Message("values for GLOBAL property must be atoms")]
        public const int Values_For_GLOBAL_Property_Must_Be_Atoms = 80;
        [Message("values for GLOBAL property must be object names")]
        public const int Values_For_GLOBAL_Property_Must_Be_Object_Names = 81;
        [Message("non-constant initializer for {0} {1}: {2}")]
        public const int Nonconstant_Initializer_For_0_1_2 = 82;
        [Message("{0} argument {1}: {2}")]
        public const int _0_Argument_1_2 = 83;
        [Message("too many call arguments: only {0} allowed in V{1}")]
        public const int Too_Many_Call_Arguments_Only_0_Allowed_In_V1 = 84;
        [Message("too many flags: {0} defined, only {1} allowed")]
        public const int Too_Many_Flags_0_Defined_Only_1_Allowed = 85;
        [Message("too many globals: {0} defined, only {1} allowed")]
        public const int Too_Many_Globals_0_Defined_Only_1_Allowed = 86;
        [Message("duplicate {0} definition: {1}")]
        public const int Duplicate_0_Definition_1 = 89;
        [Message("soft variable '{0}' may not be used here")]
        public const int Soft_Variable_0_May_Not_Be_Used_Here = 91;
        [Message("bare atom used as operand is not a global variable: {0}")]
        public const int Bare_Atom_Used_As_Operand_Is_Not_A_Global_Variable_0 = 92;
        [Message("expected a FORM, ATOM, or ADECL but found: {0}")]
        public const int Expected_A_FORM_ATOM_Or_ADECL_But_Found_0 = 93;
        [Message("no such object for IN/LOC property: {0}")]
        public const int No_Such_Object_For_INLOC_Property_0 = 94;
        [Message("property '{0}' is too long (max {1} bytes)")]
        public const int Property_0_Is_Too_Long_Max_1_Bytes = 96;
        [Message("{0} is not supported in this Z-machine version")]
        public const int _0_Is_Not_Supported_In_This_Zmachine_Version = 97;
    }
}
