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
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.MACRO, PrimType.LIST)]
    class ZilEvalMacro : ZilObject, IApplicable, IStructure
    {
        ZilObject value;

        public ZilEvalMacro(ZilObject value)
        {
            this.value = value;
        }

        [ChtypeMethod]
        public static ZilEvalMacro FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilEvalMacro>() != null);

            if (list.First != null && list.First.IsApplicable(ctx) &&
                list.Rest != null && list.Rest.First == null)
            {
                return new ZilEvalMacro(list.First);
            }

            throw new InterpreterError(InterpreterMessages.List_Does_Not_Match_MACRO_Pattern);
        }

        string ToString(Func<ZilObject, string> convert)
        {
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (Recursion.TryLock(this))
            {
                try
                {
                    return "#MACRO (" + convert(value) + ")";
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            else
            {
                return "#MACRO...";
            }
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
            return ctx.GetStdAtom(StdAtom.MACRO);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(value, new ZilList(null, null));
        }

        static ZilObject MakeSpliceExpandable(ZilObject zo)
        {
            var splice = zo as ZilSplice;

            if (splice != null)
                splice.SetSpliceableFlag();

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

            return MakeSpliceExpandable(
                ctx.ExecuteInMacroEnvironment(
                    () => value.AsApplicable(ctx).Apply(ctx, args)));
        }

        public ZilObject ExpandNoEval(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            return MakeSpliceExpandable(
                ctx.ExecuteInMacroEnvironment(
                    () => value.AsApplicable(ctx).ApplyNoEval(ctx, args)));
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilEvalMacro;
            return other != null && other.value.Equals(this.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return value;
        }

        public IStructure GetRest(int skip)
        {
            return null;
        }

        public IStructure GetBack(int skip)
        {
            throw new NotSupportedException();
        }

        public IStructure GetTop()
        {
            throw new NotSupportedException();
        }

        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            throw new NotSupportedException();
        }

        public bool IsEmpty()
        {
            return false;
        }

        public ZilObject this[int index]
        {
            get
            {
                return index == 0 ? value : null;
            }
            set
            {
                if (index == 0)
                    this.value = value;
            }
        }

        public int GetLength()
        {
            return 1;
        }

        public int? GetLength(int limit)
        {
            return limit >= 1 ? 1 : (int?)null;
        }

        #endregion

        public IEnumerator<ZilObject> GetEnumerator()
        {
            yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}