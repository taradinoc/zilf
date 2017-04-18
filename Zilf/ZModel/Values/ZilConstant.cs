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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values.Tied;
using System;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.CONSTANT, PrimType.LIST)]
    class ZilConstant : ZilTiedListBase
    {
        readonly ZilAtom name;
        readonly ZilObject value;

        public ZilConstant(ZilAtom name, ZilObject value)
        {
            this.name = name;
            this.value = value;
        }

        [ChtypeMethod]
#pragma warning disable RECS0154 // Parameter is never used
        public static ZilConstant FromList(Context ctx, ZilListBase list)
#pragma warning restore RECS0154 // Parameter is never used
        {
            if (list.IsEmpty || list.Rest.IsEmpty || !list.Rest.Rest.IsEmpty)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "list coerced to CONSTANT", 2);

            if (!(list.First is ZilAtom name))
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to CONSTANT", "an atom");

            var value = list.Rest.First;

            return new ZilConstant(name, value);
        }

        public ZilAtom Name => name;
        public ZilObject Value => value;

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilConstant>(
                x => x.Name,
                x => x.Value);
        }

        public override StdAtom StdTypeAtom => StdAtom.CONSTANT;
    }
}