/* Copyright 2010, 2015 Jesse McGrew
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

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.CONSTANT, PrimType.LIST)]
    class ZilConstant : ZilObject
    {
        readonly ZilAtom name;
        readonly ZilObject value;

        public ZilConstant(ZilAtom name, ZilObject value)
        {
            this.name = name;
            this.value = value;
        }

        [ChtypeMethod]
        public static ZilConstant FromList(Context ctx, ZilList list)
        {
            if (list.IsEmpty || list.Rest.IsEmpty || !list.Rest.Rest.IsEmpty)
                throw new InterpreterError(InterpreterMessages.List_Must_Have_2_Elements);

            var name = list.First as ZilAtom;
            var value = list.Rest.First;

            if (name == null)
                throw new InterpreterError(InterpreterMessages.First_Element_Must_Be_An_Atom);

            return new ZilConstant(name, value);
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return "#CONSTANT (" + name + " " + value + ")";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return "#CONSTANT (" + name.ToStringContext(ctx, friendly) +
            " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CONSTANT);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(name,
            new ZilList(value,
            new ZilList(null, null)));
        }
    }
}