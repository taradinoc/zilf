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
using System.Linq;
using System.Reflection;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.SUBR, PrimType.STRING)]
    class ZilSubr : ZilObject, IApplicable
    {
        protected readonly string name;
        protected readonly SubrDelegate handler;

        public ZilSubr(string name, SubrDelegate handler)
        {
            this.name = name;
            this.handler = handler;
        }

        [ChtypeMethod]
        public static ZilSubr FromString(Context ctx, ZilString str)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);
            Contract.Ensures(Contract.Result<ZilSubr>() != null);

            return FromString(ctx, str.ToStringContext(ctx, true));
        }

        public static ZilSubr FromString(Context ctx, string name)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilSubr>() != null);

            var del = ctx.GetSubrDelegate(name);
            if (del != null)
            {
                return new ZilSubr(name, del);
            }
            throw new InterpreterError(InterpreterMessages.Unrecognized_SUBR_Name_0, name);
        }

        public override string ToString()
        {
            return $"#SUBR \"{name}\"";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.SUBR);
        }

        public override PrimType PrimType
        {
            get { return PrimType.STRING; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return ZilString.FromString(name);
        }

        public virtual ZilObject Apply(Context ctx, ZilObject[] args)
        {
            var result = handler(name, ctx, EvalSequence(ctx, args).ToArray());
            Contract.Assume(result != null);
            return result;
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            var result = handler(name, ctx, args);
            Contract.Assume(result != null);
            return result;
        }

        public override bool Equals(object obj)
        {
            ZilSubr other = obj as ZilSubr;

            return
                other != null &&
                other.GetType() == this.GetType() &&
                other.name.Equals(this.name) &&
                other.handler.Equals(this.handler);
        }

        public override int GetHashCode()
        {
            return handler.GetHashCode();
        }
    }
}