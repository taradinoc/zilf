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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    struct ArgItem
    {
        public enum ArgType { Required, Optional, Auxiliary }

        public readonly ZilAtom Atom;
        public readonly bool Quoted;
        public readonly ZilObject DefaultValue;
        public readonly ArgType Type;

        public ArgItem(ZilAtom atom, bool quoted, ZilObject defaultValue, ArgType type)
        {
            Atom = atom;
            Quoted = quoted;
            DefaultValue = defaultValue;
            Type = type;
        }
    }
}