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
using System.Linq;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.OFFSET, PrimType.VECTOR)]
    sealed class ZilOffset : ZilObject, IStructure, IApplicable
    {
        public int Index { get; }
        public ZilObject StructurePattern { get; }
        public ZilObject ValuePattern { get; }

        [ChtypeMethod]
        public ZilOffset(ZilVector vector)
        {
            Contract.Requires(vector != null);

            if (vector.GetLength() != 3)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Elements, "vector coerced to OFFSET", 3);

            var indexFix = vector[0] as ZilFix;

            if (indexFix == null)
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "vector coerced to OFFSET", "a FIX");

            Index = indexFix.Value;
            StructurePattern = vector[1];
            ValuePattern = vector[2];
        }

        public ZilOffset(int index, ZilObject structurePattern, ZilObject valuePattern)
        {
            if (structurePattern == null)
                throw new ArgumentNullException(nameof(structurePattern));
            if (valuePattern == null)
                throw new ArgumentNullException(nameof(valuePattern));

            this.Index = index;
            this.StructurePattern = structurePattern;
            this.ValuePattern = valuePattern;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilOffset;
            if (other == null)
                return false;

            return other.Index == Index &&
                this.StructurePattern.Equals(other.StructurePattern) &&
                this.ValuePattern.Equals(other.ValuePattern);
        }

        public override int GetHashCode()
        {
            var result = (int)StdAtom.OFFSET;
            result = result * 31 + Index.GetHashCode();
            result = result * 31 + StructurePattern.GetHashCode();
            result = result * 31 + ValuePattern.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return string.Format(
                        "%<OFFSET {0} {1}{2} {3}{4}>",
                        Index,
                        StructurePattern is ZilAtom ? "" : "'",
                        StructurePattern,
                        ValuePattern is ZilAtom ? "" : "'",
                        ValuePattern);
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return "%<OFFSET ...>";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return string.Format(
                        "%<OFFSET {0} {1}{2} {3}{4}>",
                        Index,
                        StructurePattern is ZilAtom ? "" : "'",
                        StructurePattern.ToStringContext(ctx, friendly),
                        ValuePattern is ZilAtom ? "" : "'",
                        ValuePattern.ToStringContext(ctx, friendly));
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return "%<OFFSET ...>";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.OFFSET);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(new ZilFix(Index), StructurePattern, ValuePattern);
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return new ZilFix(Index);
        }

        public IStructure GetRest(int skip)
        {
            switch (skip)
            {
                case 0:
                    return this;

                case 1:
                    return new ZilVector(StructurePattern, ValuePattern);

                case 2:
                    return new ZilVector(ValuePattern);

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
                        return new ZilFix(Index);

                    case 1:
                        return StructurePattern;

                    case 2:
                        return ValuePattern;

                    default:
                        return null;
                }
            }
            set
            {
                throw new InterpreterError(InterpreterMessages.OFFSET_Is_Immutable);
            }
        }

        public int GetLength()
        {
            return 2;
        }

        public int? GetLength(int limit)
        {
            return 3 <= limit ? 3 : (int?)null;
        }

        #endregion

        public IEnumerator<ZilObject> GetEnumerator()
        {
            yield return new ZilFix(Index);
            yield return StructurePattern;
            yield return ValuePattern;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, EvalSequence(ctx, args).ToArray());
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    ctx.MaybeCheckDecl(args[0], this.StructurePattern, "argument {0}", 1);
                    var result = Subrs.NTH(ctx, (IStructure)args[0], this.Index);
                    ctx.MaybeCheckDecl(result, this.ValuePattern, "element {0}", this.Index);
                    return result;
                }

                if (args.Length == 2)
                {
                    ctx.MaybeCheckDecl(args[0], this.StructurePattern, "argument {0}", 1);
                    ctx.MaybeCheckDecl(args[1], this.ValuePattern, "argument {0}", 2);
                    return Subrs.PUT(ctx, (IStructure)args[0], this.Index, args[1]);
                }

                throw new InterpreterError(InterpreterMessages.Expected_1_Or_2_Args_After_An_OFFSET);
            }
            catch (InvalidCastException)
            {
                throw new InterpreterError(InterpreterMessages.Expected_A_Structured_Value_After_The_OFFSET);
            }
        }
    }
}
