/* Copyright 2010-2016 Jesse McGrew
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
            return "#MACRO...";
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override StdAtom StdTypeAtom => StdAtom.MACRO;

        public override PrimType PrimType => PrimType.LIST;

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(value, new ZilList(null, null));
        }

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
            switch (skip)
            {
                case 0:
                    return new ZilList(this);

                case 1:
                    return new ZilList(null, null);

                default:
                    return null;
            }
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