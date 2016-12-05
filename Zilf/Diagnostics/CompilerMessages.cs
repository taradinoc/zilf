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

        #region General

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

        #endregion
    }
}
