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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FORM, PrimType.LIST)]
    sealed class ZilForm : ZilListBase
    {
        public ZilForm(IEnumerable<ZilObject> sequence)
            : base(sequence) { }

        public ZilForm(ZilObject first, ZilList rest)
            : base(first, rest) { }

        public override ISourceLine SourceLine
        {
            get
            {
                Contract.Ensures(Contract.Result<ISourceLine>() != null);

                return base.SourceLine ?? SourceLines.Unknown;
            }
            set
            {
                base.SourceLine = value;
            }
        }

        [ChtypeMethod]
        public static ZilForm FromList(Context ctx, ZilListBase list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);

            return new ZilForm(list.First, list.Rest);
        }

        protected override string OpenBracket => "<";
        protected override string CloseBracket => ">";

        string ToString(Func<ZilObject, string> convert)
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    // check for special forms
                    if (First is ZilAtom firstAtom && GetLength(2) == 2)
                    {
                        var arg = Rest.First;

                        switch (firstAtom.StdAtom)
                        {
                            case StdAtom.GVAL:
                                return "," + convert(arg);

                            case StdAtom.LVAL:
                                return "." + convert(arg);

                            case StdAtom.QUOTE:
                                return "'" + convert(arg);
                        }
                    }

                    // otherwise display like a list with angle brackets
                    return SequenceToString(this, "<", ">", convert);
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return "<...>";
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override StdAtom StdTypeAtom => StdAtom.FORM;

        static ZilObject[] EmptyObjArray = new ZilObject[0];

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            if (environment != null)
            {
                return ctx.ExecuteInEnvironment(environment, () => this.Eval(ctx));
            }

            if (First == null)
                throw new InvalidOperationException("Can't evaluate null");

            ZilObject target;
            if (First is ZilAtom fa)
            {
                target = ctx.GetGlobalVal(fa) ?? ctx.GetLocalVal(fa);
                if (target == null)
                    throw new InterpreterError(this, InterpreterMessages.Calling_Unassigned_Atom_0, fa.ToStringContext(ctx, false));
            }
            else
                target = First.Eval(ctx);

            if (target.IsApplicable(ctx))
            {
                using (var frame = ctx.PushFrame(this))
                using (DiagnosticContext.Push(this.SourceLine, frame))
                {
                    return target.AsApplicable(ctx).Apply(ctx, Rest.ToArray());
                }
            }
            throw new InterpreterError(this, InterpreterMessages.Not_An_Applicable_Type_0, target.GetTypeAtom(ctx).ToStringContext(ctx, false));
        }

        public override ZilObject Expand(Context ctx)
        {
            ZilObject target;

            if (First is ZilAtom fa)
            {
                target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
            }
            else
            {
                target = First;
            }

            if (target != null && target.StdTypeAtom == StdAtom.MACRO)
            {
                using (var frame = ctx.PushFrame(this))
                using (DiagnosticContext.Push(this.SourceLine, frame))
                {
                    var result = ((ZilEvalMacro)target).Expand(ctx,
                        Rest == null ? EmptyObjArray : Rest.ToArray());

                    if (!(result is ZilForm resultForm) || resultForm == this)
                        return result;

                    // set the source info on the expansion to match the macro invocation
                    resultForm = DeepRewriteSourceInfo(resultForm, this.SourceLine);
                    return resultForm.Expand(ctx);
                }
            }

            if (target is ZilFix)
            {
                // TODO: is rewriting in place really the right behavior here?

                if (Rest.First != null)
                {
                    if (Rest.Rest.First == null)
                    {
                        // <1 FOO> => <GET FOO 1>
                        Rest = new ZilList(Rest.First,
                            new ZilList(First,
                                new ZilList(null, null)));
                        First = ctx.GetStdAtom(StdAtom.GET);
                    }
                    else
                    {
                        // <1 FOO BAR> => <PUT FOO 1 BAR>
                        Rest = new ZilList(Rest.First,
                            new ZilList(First,
                                Rest.Rest));
                        First = ctx.GetStdAtom(StdAtom.PUT);
                    }
                }
            }

            return this;
        }

        static ZilForm DeepRewriteSourceInfo(ZilForm other, ISourceLine src)
        {
            return new ZilForm(DeepRewriteSourceInfoContents(other, src)) { SourceLine = src };
        }

        static IEnumerable<ZilObject> DeepRewriteSourceInfoContents(
            IEnumerable<ZilObject> contents, ISourceLine src)
        {
            foreach (var item in contents)
            {
                if (item is ZilForm form)
                {
                    yield return DeepRewriteSourceInfo(form, src);
                }
                else
                {
                    yield return item;
                }
            }
        }

        public override bool IsLVAL(out ZilAtom atom)
        {
            if (First is ZilAtom head &&
                head.StdAtom == StdAtom.LVAL &&
                Rest.Rest?.IsEmpty == true
                && Rest.First is ZilAtom name)
            {
                atom = name;
                return true;
            }

            atom = null;
            return false;
        }

        public override bool IsGVAL(out ZilAtom atom)
        {
            if (First is ZilAtom head &&
                head.StdAtom == StdAtom.GVAL &&
                Rest.Rest?.IsEmpty == true
                && Rest.First is ZilAtom name)
            {
                atom = name;
                return true;
            }

            atom = null;
            return false;
        }
    }
}