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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values.Tied;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.MACRO, PrimType.LIST)]
    class ZilEvalMacro : ZilTiedListBase, IApplicable
    {
        public ZilEvalMacro(ZilObject value)
        {
            this.WrappedValue = value;
        }

        public ZilObject WrappedValue { get; set; }

        [ChtypeMethod]
        public static ZilEvalMacro FromList(Context ctx, ZilListBase list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilEvalMacro>() != null);

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

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            var expanded = Expand(ctx, args);
            return MakeSpliceExpandable(expanded.Eval(ctx));
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            var expanded = ExpandNoEval(ctx, args);
            return MakeSpliceExpandable(expanded.Eval(ctx));
        }

        public ZilObject Expand(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            var applicable = WrappedValue.AsApplicable(ctx);

            if (applicable == null)
                throw new InterpreterError(InterpreterMessages.Not_An_Applicable_Type_0, WrappedValue.GetTypeAtom(ctx));

            return MakeSpliceExpandable(
                ctx.ExecuteInMacroEnvironment(
                    () => WrappedValue.AsApplicable(ctx).Apply(ctx, args)));
        }

        public ZilObject ExpandNoEval(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            return MakeSpliceExpandable(
                ctx.ExecuteInMacroEnvironment(
                    () => WrappedValue.AsApplicable(ctx).ApplyNoEval(ctx, args)));
        }

        public override bool Equals(object obj)
        {
            return obj is ZilEvalMacro other && other.WrappedValue.Equals(this.WrappedValue);
        }

        public override int GetHashCode()
        {
            return WrappedValue.GetHashCode();
        }
    }
}