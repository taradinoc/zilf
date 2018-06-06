/* Copyright 2010-2018 Jesse McGrew
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

using Zilf.Language;
using Zilf.Diagnostics;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.SUBR, PrimType.STRING)]
    class ZilSubr : ZilObject, IApplicable
    {
        [NotNull]
        protected readonly string name;

        [NotNull]
        protected readonly SubrDelegate handler;

        public ZilSubr([NotNull] string name, [NotNull] SubrDelegate handler)
        {
            this.name = name;
            this.handler = handler;
        }

        [ChtypeMethod]
        [NotNull]
        public static ZilSubr FromString([NotNull] Context ctx, [NotNull] ZilString str)
        {
            return FromString(ctx, str.ToStringContext(ctx, true));
        }

        [NotNull]
        public static ZilSubr FromString([NotNull] Context ctx, [NotNull] string name)
        {
            var del = ctx.GetSubrDelegate(name);
            if (del != null)
            {
                return new ZilSubr(name, del);
            }
            throw new InterpreterError(InterpreterMessages.Unrecognized_0_1, "SUBR name", name);
        }

        public override string ToString()
        {
            return $"#SUBR \"{name}\"";
        }

        public override StdAtom StdTypeAtom => StdAtom.SUBR;

        public override PrimType PrimType => PrimType.STRING;

        [NotNull]
        public override ZilObject GetPrimitive(Context ctx)
        {
            return ZilString.FromString(name);
        }

        public virtual ZilResult Apply(Context ctx, ZilObject[] args)
        {
            var argList = new List<ZilObject>(args.Length);
            foreach (var r in EvalSequence(ctx, args))
            {
                if (r.ShouldPass())
                    return r;

                argList.Add((ZilObject)r);
            }

            var result = handler(name, ctx, argList.ToArray());
            return result;
        }

        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args)
        {
            var result = handler(name, ctx, args);
            return result;
        }

        public override bool ExactlyEquals(ZilObject obj)
        {
            return
                obj is ZilSubr other &&
                other.GetType() == GetType() &&
                other.name.Equals(name) &&
                other.handler.Equals(handler);
        }

        public override int GetHashCode()
        {
            return handler.GetHashCode();
        }
    }
}