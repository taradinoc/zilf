/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics.CodeAnalysis;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ENVIRONMENT, PrimType.ATOM)]
    sealed class ZilEnvironment : ZilObject, IEvanescent
    {
        readonly ZilAtom name;
        readonly WeakReference<LocalEnvironment> env;

        [ChtypeMethod]
        [DoesNotReturn]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        [SuppressMessage("Performance", "CA1801:Unused parameter")]

        public static ZilEnvironment FromAtom(Context ctx, ZilAtom atom) =>
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "ENVIRONMENT");

        public ZilEnvironment(LocalEnvironment env, ZilAtom name)
        {
            this.env = new WeakReference<LocalEnvironment>(env);
            this.name = name;
        }

        public override bool ExactlyEquals(ZilObject? obj)
        {
            if (obj is ZilEnvironment other &&
                env.TryGetTarget(out var thisTarget) &&
                other.env.TryGetTarget(out var otherTarget))
            {
                return thisTarget == otherTarget;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)StdAtom.ENVIRONMENT;
        }

        public override string ToString() =>
            $"#ENVIRONMENT {name}";

        protected override string ToStringContextImpl(Context ctx, bool friendly) =>
            $"#ENVIRONMENT {name.ToStringContext(ctx, friendly)}";

        public override StdAtom StdTypeAtom => StdAtom.ENVIRONMENT;

        public override PrimType PrimType => PrimType.ATOM;

        public override ZilObject GetPrimitive(Context ctx) => name;

        public LocalEnvironment LocalEnvironment =>
            env.TryGetTarget(out var result)
            ? result
            : throw new InterpreterError(InterpreterMessages.Environment_Has_Expired);

        public bool IsLegal => env.TryGetTarget(out _);
    }
}
