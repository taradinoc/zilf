﻿/* Copyright 2010, 2015 Jesse McGrew
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

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.DECL);
        }

        public override string ToString()
        {
            return "#DECL " + base.ToString();
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return "#DECL " + base.ToStringContextImpl(ctx, friendly);
        }

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            if (originalType != null)
                return ctx.ChangeType(this, originalType);
            else
                return this;
        }

        public IEnumerable<KeyValuePair<ZilAtom, ZilObject>> GetAtomDeclPairs()
        {
            ZilList list = this;

            while (!list.IsEmpty)
            {
                var atoms = list.First as ZilList;
                var decl = list.Rest.First;

                if (atoms == null || decl == null || !atoms.All(a => a is ZilAtom))
                    break;

                foreach (var atom in atoms)
                    yield return new KeyValuePair<ZilAtom, ZilObject>((ZilAtom)atom, decl);

                list = list.Rest.Rest;
            }

            if (!list.IsEmpty)
                throw new InterpreterError("malformed DECL object");
        }
    }
}