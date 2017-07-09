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
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "vector coerced to OFFSET", 3);

            if (!(vector[0] is ZilFix indexFix))
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "vector coerced to OFFSET", "a FIX");

            Index = indexFix.Value;
            StructurePattern = vector[1];
            ValuePattern = vector[2];
        }

        public ZilOffset(int index, ZilObject structurePattern, ZilObject valuePattern)
        {
            this.Index = index;
            this.StructurePattern = structurePattern ?? throw new ArgumentNullException(nameof(structurePattern));
            this.ValuePattern = valuePattern ?? throw new ArgumentNullException(nameof(valuePattern));
        }

        public override bool Equals(object obj)
        {
            return obj is ZilOffset other &&
                other.Index == Index &&
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

        public override StdAtom StdTypeAtom => StdAtom.OFFSET;

        public override PrimType PrimType => PrimType.VECTOR;

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

        public bool IsEmpty => false;

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

        public ZilResult Apply(Context ctx, ZilObject[] args)
        {
            if (ZilObject.EvalSequence(ctx, args).TryToZilObjectArray(out args, out var zr))
            {
                return ApplyNoEval(ctx, args);
            }
            else
            {
                return zr;
            }
        }

        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args)
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

                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1_After_2,
                    InterpreterMessages.NoFunction,
                    "1 or 2 args",
                    "the OFFSET");
            }
            catch (InvalidCastException)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1_After_2,
                    InterpreterMessages.NoFunction,
                    "a structured value",
                    "the OFFSET");
            }
        }
    }
}
