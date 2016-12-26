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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    struct AsocResult
    {
        public ZilObject Item;
        public ZilObject Indicator;
        public ZilObject Value;
    }

    [BuiltinType(StdAtom.ASOC, PrimType.LIST)]
    class ZilAsoc : ZilObject
    {
        readonly AsocResult[] results;
        readonly int index;

        public ZilAsoc(AsocResult[] results, int index)
        {
            Contract.Requires(results != null);
            Contract.Requires(index >= 0 && index < results.Length);

            this.results = results;
            this.index = index;
        }

        [ChtypeMethod]
        public static ZilAsoc FromList(Context ctx, ZilList list)
        {
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "ASOC");
        }

        public ZilObject Item => results[index].Item;
        public ZilObject Indicator => results[index].Indicator;
        public ZilObject Value => results[index].Value;

        public ZilAsoc GetNext()
        {
            if (index + 1 < results.Length)
                return new ZilAsoc(results, index + 1);

            return null;
        }

        public override StdAtom StdTypeAtom => StdAtom.ASOC;

        public override PrimType PrimType => PrimType.LIST;

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(new[] { Item, Indicator, Value });
        }

        public override string ToString()
        {
            return $"#ASOC ({Item} {Indicator} {Value})";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return string.Format("#ASOC ({0} {1} {2})",
                Item.ToStringContext(ctx, friendly),
                Indicator.ToStringContext(ctx, friendly),
                Value.ToStringContext(ctx, friendly));
        }
    }
}
