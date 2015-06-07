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

        private string ToString(Func<ZilObject, string> convert)
        {
            // check for special forms
            if (First is ZilAtom && Rest.Rest != null && Rest.Rest.First == null)
            {
                ZilObject arg = ((ZilList)Rest).First;

                switch (((ZilAtom)First).StdAtom)
                {
                    case StdAtom.GVAL:
                        return "," + arg.ToString();
                    case StdAtom.LVAL:
                        return "." + arg.ToString();
                    case StdAtom.QUOTE:
                        return "'" + arg.ToString();
                }
            }

            // otherwise display like a list with angle brackets
            return ZilList.SequenceToString(this, "<", ">", convert);
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FORM);
        }

        private static ZilObject[] EmptyObjArray = new ZilObject[0];

        public override ZilObject Eval(Context ctx)
        {
            if (First == null)
                throw new NotImplementedException("Can't evaluate null");

            ZilObject target;
            if (First is ZilAtom)
            {
                ZilAtom fa = (ZilAtom)First;
                target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
                if (target == null)
                    throw new InterpreterError(this, "calling undefined atom: " + fa.ToStringContext(ctx, false));
            }
            else
                target = First.Eval(ctx);

            if (target is IApplicable)
            {
                ZilForm oldCF = ctx.CallingForm;
                ctx.CallingForm = this;
                try
                {
                    return ((IApplicable)target).Apply(ctx, ((ZilList)Rest).ToArray());
                }
                catch (ZilError ex)
                {
                    if (ex.SourceLine == null)
                        ex.SourceLine = this.SourceLine;
                    throw;
                }
                finally
                {
                    ctx.CallingForm = oldCF;
                }
            }
            else
                throw new InterpreterError(this, "not an applicable type: " +
                                                 target.GetTypeAtom(ctx).ToStringContext(ctx, false));
        }

        public override ZilObject Expand(Context ctx)
        {
            if (First is ZilAtom)
            {
                ZilAtom fa = (ZilAtom)First;
                ZilObject target = ctx.GetGlobalVal(fa);
                if (target == null)
                    target = ctx.GetLocalVal(fa);
                if (target != null && target.GetTypeAtom(ctx).StdAtom == StdAtom.MACRO)
                {
                    ZilForm oldCF = ctx.CallingForm;
                    ctx.CallingForm = this;
                    try
                    {
                        ZilObject result = ((ZilEvalMacro)target).Expand(ctx,
                            Rest == null ? EmptyObjArray : ((ZilList)Rest).ToArray());

                        ZilForm resultForm = result as ZilForm;
                        if (resultForm == null || resultForm == this)
                            return result;

                        // set the source info on the expansion to match the macro invocation
                        resultForm = DeepRewriteSourceInfo(resultForm, this.SourceLine);
                        return resultForm.Expand(ctx);
                    }
                    catch (ZilError ex)
                    {
                        if (ex.SourceLine == null)
                            ex.SourceLine = this.SourceLine;
                        throw;
                    }
                    finally
                    {
                        ctx.CallingForm = oldCF;
                    }
                }
            }
            else if (First is ZilFix)
            {
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

        private static ZilForm DeepRewriteSourceInfo(ZilForm other, ISourceLine src)
        {
            return new ZilForm(DeepRewriteSourceInfoContents(other, src)) { SourceLine = src };
        }

        private static IEnumerable<ZilObject> DeepRewriteSourceInfoContents(
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