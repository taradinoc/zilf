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

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.SUBR, PrimType.STRING)]
    class ZilSubr : ZilObject, IApplicable
    {
        protected readonly Subrs.SubrDelegate handler;

        public ZilSubr(Subrs.SubrDelegate handler)
        {
            this.handler = handler;
        }

        [ChtypeMethod]
        public static ZilSubr FromString(Context ctx, ZilString str)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);

            var name = str.ToStringContext(ctx, true);
            MethodInfo mi = typeof(Subrs).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
            if (mi != null)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 1)
                {
                    Subrs.SubrDelegate del = (Subrs.SubrDelegate)Delegate.CreateDelegate(
                        typeof(Subrs.SubrDelegate), mi);

                    return new ZilSubr(del);
                }
            }
            throw new InterpreterError("unrecognized SUBR name: " + name);
        }

        public override string ToString()
        {
            return "#SUBR \"" + handler.Method.Name + "\"";
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
            return new ZilString(handler.Method.Name);
        }

        public virtual ZilObject Apply(Context ctx, ZilObject[] args)
        {
            var result = handler(ctx, EvalSequence(ctx, args).ToArray());
            Contract.Assume(result != null);
            return result;
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            var result = handler(ctx, args);
            Contract.Assume(result != null);
            return result;
        }

        public override bool Equals(object obj)
        {
            ZilSubr other = obj as ZilSubr;
            return other != null && other.GetType() == this.GetType() && other.handler.Equals(this.handler);
        }

        public override int GetHashCode()
        {
            return handler.GetHashCode();
        }
    }
}