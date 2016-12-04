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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ENVIRONMENT, PrimType.ATOM)]
    sealed class ZilEnvironment : ZilObject, IEvanescent
    {
        private readonly ZilAtom name;
        private readonly WeakReference<LocalEnvironment> env;

        [ChtypeMethod]
        public static ZilEnvironment FromAtom(Context ctx, ZilAtom atom)
        {
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_TYPENAME0_Not_Supported, "ENVIRONMENT");
        }

        public ZilEnvironment(LocalEnvironment env, ZilAtom name)
        {
            this.env = new WeakReference<LocalEnvironment>(env);
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilEnvironment;
            if (other == null)
                return false;

            LocalEnvironment thisTarget, otherTarget;

            if (this.env.TryGetTarget(out thisTarget) &&
                other.env.TryGetTarget(out otherTarget))
            {
                return thisTarget == otherTarget;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)StdAtom.ENVIRONMENT;
        }

        public override string ToString()
        {
            return string.Format("#ENVIRONMENT {0}", name.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return string.Format("#ENVIRONMENT {0}", name.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ENVIRONMENT);
        }

        public override PrimType PrimType
        {
            get { return PrimType.ATOM; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return name;
        }

        public LocalEnvironment LocalEnvironment
        {
            get
            {
                LocalEnvironment result;
                if (env.TryGetTarget(out result))
                {
                    return result;
                }

                throw new InterpreterError(InterpreterMessages.Environment_Has_Expired);
            }
        }

        public bool IsLegal
        {
            get
            {
                LocalEnvironment dummy;
                return env.TryGetTarget(out dummy);
            }
        }
    }
}
