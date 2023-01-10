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
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.LINK, PrimType.ATOM)]
    sealed class ZilLink : ZilAtom
    {
        public ZilLink(string pname, ObList list)
            : base(pname, list, StdAtom.None)
        {
        }

        public override StdAtom StdTypeAtom => StdAtom.LINK;

        [ChtypeMethod]
        [DoesNotReturn]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        [SuppressMessage("Performance", "CA1801:Unused parameter")]
        public new static ZilLink FromAtom(ZilAtom atom) =>
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "LINK");

        [DoesNotReturn]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        [SuppressMessage("Performance", "CA1801:Unused parameter")]
        public override ZilObject GetPrimitive(Context ctx) =>
            throw new InterpreterError(InterpreterMessages.CHTYPE_Away_From_0_Not_Supported, "LINK");
    }
}
