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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FORM, PrimType.LIST)]
    class ZilForm : ZilList
    {
        public ZilForm(IEnumerable<ZilObject> sequence)
            : base(sequence)
        {
            Contract.Requires(sequence != null);
        }

        protected ZilForm(ZilObject first, ZilList rest)
            : base(first, rest)
        {
            Contract.Requires((first == null && rest == null) || (first != null && rest != null));
            Contract.Ensures(First == first);
            Contract.Ensures(Rest == rest);
        }

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
        public static new ZilForm FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);

            return new ZilForm(list.First, list.Rest);
        }

        string ToString(Func<ZilObject, string> convert)
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    // check for special forms
                    if (First is ZilAtom && Rest.Rest != null && Rest.Rest.First == null)
                    {
                        ZilObject arg = ((ZilList)Rest).First;

                        switch (((ZilAtom)First).StdAtom)
                        {
                            case StdAtom.GVAL:
                                return "," + arg;
                            case StdAtom.LVAL:
                                return "." + arg;
                            case StdAtom.QUOTE:
                                return "'" + arg;
                        }
                    }

                    // otherwise display like a list with angle brackets
                    return ZilList.SequenceToString(this, "<", ">", convert);
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

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FORM);
        }

        static ZilObject[] EmptyObjArray = new ZilObject[0];

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            if (environment != null)
            {
                return ctx.ExecuteInEnvironment(environment, () => this.Eval(ctx));
            }

            if (First == null)
                throw new NotImplementedException("Can't evaluate null");

            ZilObject target;
            if (First is ZilAtom)
            {
                var fa = (ZilAtom)First;
                target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
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
                    return target.AsApplicable(ctx).Apply(ctx, ((ZilList)Rest).ToArray());
                }
            }
            throw new InterpreterError(this, InterpreterMessages.Not_An_Applicable_Type_0, target.GetTypeAtom(ctx).ToStringContext(ctx, false));
        }

        public override ZilObject Expand(Context ctx)
        {
            ZilObject target;

            if (First is ZilAtom)
            {
                var fa = (ZilAtom)First;
                target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
            }
            else
            {
                target = First;
            }

            if (target != null && target.GetTypeAtom(ctx).StdAtom == StdAtom.MACRO)
            {
                using (var frame = ctx.PushFrame(this))
                using (DiagnosticContext.Push(this.SourceLine, frame))
                {
                    var result = ((ZilEvalMacro)target).Expand(ctx,
                        Rest == null ? EmptyObjArray : ((ZilList)Rest).ToArray());

                    var resultForm = result as ZilForm;
                    if (resultForm == null || resultForm == this)
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
                if (item is ZilForm)
                {
                    yield return DeepRewriteSourceInfo((ZilForm)item, src);
                }
                else
                {
                    yield return item;
                }
            }
        }

        public override bool IsLVAL()
        {
            var atom = First as ZilAtom;
            return (atom != null && atom.StdAtom == StdAtom.LVAL && Rest.Rest != null && Rest.Rest.First == null);
        }

        public override bool IsGVAL()
        {
            var atom = First as ZilAtom;
            return (atom != null && atom.StdAtom == StdAtom.GVAL && Rest.Rest != null && Rest.Rest.First == null);
        }
    }
}