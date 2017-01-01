/* Copyright 2010-2016 Jesse McGrew
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
    [BuiltinType(StdAtom.GLOBAL, PrimType.LIST)]
    class ZilGlobal : ZilObject
    {
        readonly ZilAtom name;
        readonly ZilObject value;

        public ZilGlobal(ZilAtom name, ZilObject value, GlobalStorageType storageType = GlobalStorageType.Any)
        {
            this.name = name;
            this.value = value;
            this.StorageType = storageType;
            this.IsWord = true;
        }

        [ChtypeMethod]
#pragma warning disable RECS0154 // Parameter is never used
        public static ZilGlobal FromList(Context ctx, ZilList list)
#pragma warning restore RECS0154 // Parameter is never used
        {
            if (list.IsEmpty || list.Rest.IsEmpty || !list.Rest.Rest.IsEmpty)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "list coerced to GLOBAL", 2);

            var name = list.First as ZilAtom;
            var value = list.Rest.First;

            if (name == null)
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to GLOBAL", "an atom");

            return new ZilGlobal(name, value);
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public GlobalStorageType StorageType { get; set; }

        public bool IsWord { get; set; }

        public override string ToString()
        {
            return "#GLOBAL (" + name + " " + value + ")";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return "#GLOBAL (" + name.ToStringContext(ctx, friendly) +
                " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override StdAtom StdTypeAtom => StdAtom.GLOBAL;

        public override PrimType PrimType => PrimType.LIST;

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(name,
                new ZilList(value,
                    new ZilList(null, null)));
        }
    }
}