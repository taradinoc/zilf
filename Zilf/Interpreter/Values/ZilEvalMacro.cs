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

using System.Diagnostics.CodeAnalysis;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values.Tied;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.MACRO, PrimType.LIST)]
    class ZilEvalMacro : ZilTiedListBase, IApplicable
    {
        public ZilEvalMacro(ZilObject value)
        {
            WrappedValue = value;
        }

        public ZilObject WrappedValue { get; set; }

        /// <exception cref="InterpreterError"><paramref name="list"/> has the wrong number or types of elements.</exception>
        [ChtypeMethod]
        [NotNull]
        public static ZilEvalMacro FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {
            if (list.First == null || list.Rest == null || list.Rest.First != null)
                throw new InterpreterError(
                    InterpreterMessages._0_Must_Have_1_Element1s,
                    "list coerced to MACRO",
                    1);

            if (!list.First.IsApplicable(ctx))
                throw new InterpreterError(
                    InterpreterMessages.Element_0_Of_1_Must_Be_2,
                    1,
                    "list coerced to MACRO",
                    "applicable");

            return new ZilEvalMacro(list.First);
        }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilEvalMacro>(x => x.WrappedValue);
        }

        public override StdAtom StdTypeAtom => StdAtom.MACRO;

        static ZilObject MakeSpliceExpandable(ZilObject zo)
        {
            (zo as ZilSplice)?.SetSpliceableFlag();
            return zo;
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public ZilResult Apply(Context ctx, ZilObject[] args)
        {
            var expanded = Expand(ctx, args);
            if (expanded.ShouldPass())
                return expanded;

            var result = ((ZilObject)expanded).Eval(ctx);
            if (result.ShouldPass())
                return result;

            return MakeSpliceExpandable((ZilObject)result);
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args)
        {
            var expanded = ExpandNoEval(ctx, args);
            if (expanded.ShouldPass())
                return expanded;

            var result = ((ZilObject)expanded).Eval(ctx);
            if (result.ShouldPass())
                return result;

            return MakeSpliceExpandable((ZilObject)result);
        }

        /// <exception cref="InterpreterError">The contained value is not an applicable type.</exception>
        public ZilResult Expand([NotNull] Context ctx, [NotNull] ZilObject[] args)
        {
            var applicable = WrappedValue.AsApplicable(ctx);

            if (applicable == null)
                throw new InterpreterError(InterpreterMessages.Not_An_Applicable_Type_0, WrappedValue.GetTypeAtom(ctx));

            var result = ctx.ExecuteInMacroEnvironment(
                () => applicable.Apply(ctx, args));

            return result.ShouldPass() ? result : MakeSpliceExpandable((ZilObject)result);
        }

        /// <exception cref="InterpreterError">The contained value is not an applicable type.</exception>
        public ZilResult ExpandNoEval([NotNull] Context ctx, [NotNull] ZilObject[] args)
        {
            var applicable = WrappedValue.AsApplicable(ctx);

            if (applicable == null)
                throw new InterpreterError(InterpreterMessages.Not_An_Applicable_Type_0, WrappedValue.GetTypeAtom(ctx));

            var result = ctx.ExecuteInMacroEnvironment(
                () => applicable.ApplyNoEval(ctx, args));

            return result.ShouldPass() ? result : MakeSpliceExpandable((ZilObject)result);
        }

        public override bool StructurallyEquals(ZilObject obj)
        {
            return obj is ZilEvalMacro other && other.WrappedValue.StructurallyEquals(WrappedValue);
        }
    }
}