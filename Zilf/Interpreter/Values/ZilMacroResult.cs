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

using System;
using JetBrains.Annotations;
using Zilf.Compiler;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    class WrappedMacroResultException : Exception
    {
        public WrappedMacroResultException()
            : base("Macro result was not unwrapped before usage")
        {
        }
    }

    /// <summary>
    /// Wraps objects returned from macro expansion in some circumstances.
    /// </summary>
    /// <remarks>
    /// <para>This is an ugly hack. Objects wrapped this way don't implement most of the
    /// <see cref="ZilObject"/> methods, so if the wrapper isn't stripped with
    /// <see cref="CompilationExtensions.Unwrap"/>, doing anything with them will throw
    /// a <see cref="WrappedMacroResultException"/>.</para>
    /// <para>The intended use case involves detecting conditions that appear constant
    /// but were actually computed by macros at compile time, to suppress warnings.</para>
    /// <para>Other possible solutions:</para>
    /// <list type="bullet">
    /// <item><description>Record all the macro expansions done in each routine,
    /// and check that log to see if one of them produced the condition.
    /// (What happens when a macro returns a commonly reused object like ctx.TRUE or
    /// ctx.FALSE? We can't look that up in a log, but can we look up the form or clause
    /// that contains it?)</description></item>
    /// <item><description>Change the way macros are expanded in contexts where
    /// we'll need to do this test, to reduce the amount of code that might be exposed
    /// to ZilMacroResult.</description></item>
    /// </list>
    /// </remarks>
    [BuiltinMeta]
    sealed class ZilMacroResult : ZilObject
    {
        public ZilMacroResult([NotNull] ZilObject inner)
        {
            this.Inner = inner;
        }

        public ZilObject Inner { get; }

        public override ISourceLine SourceLine
        {
            get => Inner.SourceLine;
            set => Inner.SourceLine = value;
        }

        public override bool ExactlyEquals(ZilObject other) => throw new WrappedMacroResultException();
        public override int GetHashCode() => throw new WrappedMacroResultException();
        public override bool StructurallyEquals(ZilObject other) => throw new WrappedMacroResultException();

        public override string ToString() => throw new WrappedMacroResultException();
        protected override string ToStringContextImpl(Context ctx, bool friendly) =>
            throw new WrappedMacroResultException();

        public override ZilAtom GetTypeAtom(Context ctx) => throw new WrappedMacroResultException();
        public override StdAtom StdTypeAtom => throw new WrappedMacroResultException();
        public override PrimType PrimType => throw new WrappedMacroResultException();
        public override ZilObject GetPrimitive(Context ctx) => throw new WrappedMacroResultException();

        protected override ZilResult EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType) =>
            throw new WrappedMacroResultException();

        public override ZilResult Expand(Context ctx) => this;

        public override bool IsTrue => throw new WrappedMacroResultException();
        public override bool IsLVAL(out ZilAtom atom) => throw new WrappedMacroResultException();
        public override bool IsGVAL(out ZilAtom atom) => throw new WrappedMacroResultException();
    }
}
