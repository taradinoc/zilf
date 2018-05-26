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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ADECL, PrimType.VECTOR)]
    sealed class ZilAdecl : ZilObject, IStructure
    {
        [NotNull]
        public ZilObject First;
        [NotNull]
        public ZilObject Second;

        /// <exception cref="InterpreterError"><paramref name="vector"/> has the wrong number of elements.</exception>
        [ChtypeMethod]
        public ZilAdecl([NotNull] ZilVector vector)
        {

            if (vector.GetLength() != 2)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "vector coerced to ADECL", 2);

            First = vector[0];
            Second = vector[1];
        }

        public ZilAdecl([NotNull] ZilObject first, [NotNull] ZilObject second)
        {
            First = first ?? throw new ArgumentNullException(nameof(first));
            Second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public override bool StructurallyEquals(ZilObject obj)
        {
            return obj is ZilAdecl other && other.First.StructurallyEquals(First) && other.Second.StructurallyEquals(Second);
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

        public override StdAtom StdTypeAtom => StdAtom.ADECL;

        public override PrimType PrimType => PrimType.VECTOR;

        [NotNull]
        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(First, Second);
        }

        protected override ZilResult EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            var result = First.Eval(ctx, environment);
            if (!result.ShouldPass())
            {
                ctx.MaybeCheckDecl(this, (ZilObject)result, Second, "result of evaluating {0}", First);
            }
            return result;
        }

        #region IStructure Members

        [NotNull]
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

        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public IStructure GetBack(int skip)
        {
            throw new NotSupportedException();
        }

        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public IStructure GetTop()
        {
            throw new NotSupportedException();
        }

        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            throw new NotSupportedException();
        }

        public bool IsEmpty => false;

        /// <exception cref="ArgumentOutOfRangeException" accessor="set"><paramref name="index"/> is out of range.</exception>
        [CanBeNull]
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
                Debug.Assert(value != null);

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