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
using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FSUBR, PrimType.STRING)]
    class ZilFSubr : ZilSubr, IApplicable
    {
        public ZilFSubr(string name, SubrDelegate handler)
            : base(name, handler)
        {
        }

        [ChtypeMethod]
        public static new ZilFSubr FromString(Context ctx, ZilString str)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);
            Contract.Ensures(Contract.Result<ZilFSubr>() != null);

            return FromString(ctx, str.ToStringContext(ctx, true));
        }

        public static new ZilFSubr FromString(Context ctx, string name)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilFSubr>() != null);

            var del = ctx.GetSubrDelegate(name);
            if (del != null)
            {
                return new ZilFSubr(name, del);
            }
            throw new InterpreterError("unrecognized FSUBR name: " + name);
        }

        public override string ToString()
        {
            return $"#FSUBR \"{name}\"";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FSUBR);
        }

        public override ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, args);
        }
    }
}