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
using System.Diagnostics.Contracts;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.WORD, PrimType.LIST)]
    sealed class ZilWord : ZilObject
    {
        private readonly ZilObject value;

        public ZilWord(ZilObject value)
        {
            Contract.Requires(value != null);

            this.value = value;
        }

        [ChtypeMethod]
        public static ZilWord FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilWord>() != null);

            if (list.First == null || list.Rest == null || !list.Rest.IsEmpty)
                throw new InterpreterError(InterpreterMessages.List_Must_Have_Length_1);

            return new ZilWord(list.First);
        }

        public ZilObject Value
        {
            get
            {
                Contract.Ensures(Contract.Result<ZilObject>() != null);

                return value;
            }
        }

        public override string ToString()
        {
            return string.Format("#WORD ({0})", value);
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return string.Format("#WORD ({0})", value.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.WORD);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(value, new ZilList(null, null));
        }
    }
}