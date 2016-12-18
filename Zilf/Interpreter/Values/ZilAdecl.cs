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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ADECL, PrimType.VECTOR)]
    sealed class ZilAdecl : ZilObject, IStructure
    {
        public ZilObject First;
        public ZilObject Second;

        [ChtypeMethod]
        public ZilAdecl(ZilVector vector)
        {
            Contract.Requires(vector != null);

            if (vector.GetLength() != 2)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "vector coerced to ADECL", 2);

            First = vector[0];
            Second = vector[1];
        }

        public ZilAdecl(ZilObject first, ZilObject second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));

            this.First = first;
            this.Second = second;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilAdecl;
            if (other == null)
                return false;

            return other.First.Equals(First) && other.Second.Equals(Second);
        }

        public override int GetHashCode()
        {
            var result = (int)StdAtom.ADECL;
            result = result * 31 + First.GetHashCode();
            result = result * 31 + Second.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return First + ":" + Second;
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return ":...";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return First.ToStringContext(ctx, friendly) + ":" + Second.ToStringContext(ctx, friendly);
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ADECL);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(First, Second);
        }

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            var result = First.Eval(ctx, environment);
            ctx.MaybeCheckDecl(this, result, Second, "result of evaluating {0}", First);
            return result;
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return First;
        }

        public IStructure GetRest(int skip)
        {
            switch (skip)
            {
                case 0:
                    return this;

                case 1:
                    return new ZilVector(Second);

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
                switch (index)
                {
                    case 0:
                        return First;

                    case 1:
                        return Second;

                    default:
                        return null;
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        First = value;
                        break;

                    case 1:
                        Second = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(index), "writing past end of ADECL");
                }
            }
        }

        public int GetLength()
        {
            return 2;
        }

        public int? GetLength(int limit)
        {
            return 2 <= limit ? 2 : (int?)null;
        }

        #endregion

        public IEnumerator<ZilObject> GetEnumerator()
        {
            yield return First;
            yield return Second;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}