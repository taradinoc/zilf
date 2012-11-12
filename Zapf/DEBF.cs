/* Copyright 2010, 2012 Jesse McGrew
 * 
 * This file is part of ZAPF.
 * 
 * ZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zapf
{
    /// <summary>
    /// Contains constants for the Inform debug file format.
    /// </summary>
    static class DEBF
    {
        public const byte EOF_DBR = 0;
        public const byte FILE_DBR = 1;
        public const byte CLASS_DBR = 2;
        public const byte OBJECT_DBR = 3;
        public const byte GLOBAL_DBR = 4;
        public const byte ARRAY_DBR = 12;
        public const byte ATTR_DBR = 5;
        public const byte PROP_DBR = 6;
        public const byte FAKE_ACTION_DBR = 7;
        public const byte ACTION_DBR = 8;
        public const byte HEADER_DBR = 9;
        public const byte ROUTINE_DBR = 11;
        public const byte LINEREF_DBR = 10;
        public const byte ROUTINE_END_DBR = 14;
        public const byte MAP_DBR = 13;

        public const string AbbrevMapName = "abbreviations table";
        public const string PropsMapName = "property defaults";
        public const string ObjectsMapName = "object tree";
        public const string GlobalsMapName = "global variables";
        public const string ArraysMapName = "array space";
        public const string GrammarMapName = "grammar table";
        public const string VocabMapName = "dictionary";
        public const string CodeMapName = "code area";
        public const string StringsMapName = "strings area";
    }
}
