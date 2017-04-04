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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.DECL, PrimType.LIST)]
    class ZilDecl : ZilList
    {
        public ZilDecl(IEnumerable<ZilObject> sequence)
            : base(sequence)
        {
        }

        [ChtypeMethod]
        public static new ZilDecl FromList(Context ctx, ZilList list)
        {
            return new ZilDecl(list);
        }

        public override StdAtom StdTypeAtom => StdAtom.DECL;

        public override string ToString()
        {
#pragma warning disable RECS0106 // False alarm, ToString() is required here
            return "#DECL " + base.ToString();
#pragma warning restore RECS0106
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return "#DECL " + base.ToStringContextImpl(ctx, friendly);
        }

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            return originalType != null ? ctx.ChangeType(this, originalType) : this;
        }

        public IEnumerable<KeyValuePair<ZilAtom, ZilObject>> GetAtomDeclPairs()
        {
            ZilList list = this;

            while (!list.IsEmpty)
            {
                if (!(list.First is ZilList atoms) ||
                    !atoms.All(a => a is ZilAtom) ||
                    !(list.Rest?.First is ZilObject decl))
                {
                    break;
                }

                foreach (ZilAtom atom in atoms)
                    yield return new KeyValuePair<ZilAtom, ZilObject>(atom, decl);

                list = list.Rest.Rest;
            }

            if (!list.IsEmpty)
                throw new InterpreterError(InterpreterMessages.Malformed_DECL_Object);
        }
    }
}
