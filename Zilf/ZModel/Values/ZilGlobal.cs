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
    [BuiltinType(StdAtom.GLOBAL, PrimType.LIST)]
    class ZilGlobal : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilObject value;

        public ZilGlobal(ZilAtom name, ZilObject value, GlobalStorageType storageType = GlobalStorageType.Any)
        {
            this.name = name;
            this.value = value;
            this.StorageType = storageType;
            this.IsWord = true;
        }

        [ChtypeMethod]
        public static ZilGlobal FromList(Context ctx, ZilList list)
        {
            if (list.IsEmpty || list.Rest.IsEmpty || !list.Rest.Rest.IsEmpty)
                throw new InterpreterError(InterpreterMessages.List_Must_Have_2_Elements);

            var name = list.First as ZilAtom;
            var value = list.Rest.First;

            if (name == null)
                throw new InterpreterError(InterpreterMessages.First_Element_Must_Be_An_Atom);

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
            return "#GLOBAL (" + name.ToString() + " " + value.ToString() + ")";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return "#GLOBAL (" + name.ToStringContext(ctx, friendly) +
                " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.GLOBAL);
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