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

        #region General - 0000

        [Message("{0}")]
        public const int LegacyError = 0;

        #endregion

        [Message("PROPDEF patterns must be lists")]
        public const int PROPDEF_Patterns_Must_Be_Lists = 1;
        [Message("strings in PROPDEF patterns must be \"OPT\" or \"MANY\"")]
        public const int Strings_In_PROPDEF_Patterns_Must_Be_OPT_Or_MANY = 2;
        [Message("list in PROPDEF output pattern must have length 2")]
        public const int List_In_PROPDEF_Output_Pattern_Must_Have_Length_2 = 3;
        [Message("first item of list in PROPDEF output pattern must be an atom")]
        public const int First_Item_Of_List_In_PROPDEF_Output_Pattern_Must_Be_An_Atom = 4;
    }
}
