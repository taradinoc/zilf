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
using System.Diagnostics.CodeAnalysis;
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

        /// <exception cref="InterpreterError"><paramref name="vector"/> has the wrong number or types of elements.</exception>
        [ChtypeMethod]
        public ZilOffset(ZilVector vector)
        {
            if (vector.GetLength() != 3)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "vector coerced to OFFSET", 3);

            if (vector[0] is not ZilFix indexFix)
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "vector coerced to OFFSET", "a FIX");

            Index = indexFix.Value;
            StructurePattern = vector[1]!;
            ValuePattern = vector[2]!;
        }

        public ZilOffset(int index, ZilObject structurePattern, ZilObject valuePattern)
        {
            Index = index;
            StructurePattern = structurePattern;
            ValuePattern = valuePattern;
        }

        public override bool StructurallyEquals(ZilObject? obj)
        {
            return obj is ZilOffset other &&
                other.Index == Index &&
                StructurePattern.StructurallyEquals(other.StructurePattern) &&
                ValuePattern.StructurallyEquals(other.ValuePattern);
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
            static string MaybeQuote(ZilObject zo)
            {
                return zo is ZilAtom ? "'" + zo : zo.ToString();
            }

            if (Recursion.TryLock(this))
            {
                try
                {
                    return $"%<OFFSET {Index} {MaybeQuote(StructurePattern)} {MaybeQuote(ValuePattern)}>";
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
            string MaybeQuote(ZilObject zo)
            {
                return zo is ZilAtom ? "'" + zo : zo.ToStringContext(ctx, friendly);
            }

            if (Recursion.TryLock(this))
            {
                try
                {
                    return $"%<OFFSET {Index} {MaybeQuote(StructurePattern)} {MaybeQuote(ValuePattern)}>";
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

        public IStructure? GetRest(int skip)
        {
            return skip switch
            {
                0 => (IStructure)this,
                1 => new ZilVector(StructurePattern, ValuePattern),
                2 => new ZilVector(ValuePattern),
                _ => null
            };
        }

        /// <exception cref="NotSupportedException">Always thrown.</exception>
        [DoesNotReturn]
        public IStructure? GetBack(int skip) => throw new NotSupportedException();

        /// <exception cref="NotSupportedException">Always thrown.</exception>
        [DoesNotReturn]
        public IStructure GetTop() => throw new NotSupportedException();

        /// <exception cref="NotSupportedException">Always thrown.</exception>
        [DoesNotReturn]
        public void Grow(int end, int beginning, ZilObject defaultValue) => throw new NotSupportedException();

        public bool IsEmpty => false;

        /// <exception cref="InterpreterError" accessor="set">Always thrown.</exception>
        [DisallowNull]
        public ZilObject? this[int index]
        {
            get => index switch
            {
                0 => new ZilFix(Index),
                1 => StructurePattern,
                2 => ValuePattern,
                _ => null
            };

            [DoesNotReturn]
            set => throw new InterpreterError(InterpreterMessages.OFFSET_Is_Immutable);
        }

        public int GetLength() => 2;

        public int? GetLength(int limit) => 3 <= limit ? 3 : (int?)null;

        #endregion

        public IEnumerator<ZilObject> GetEnumerator()
        {
            yield return new ZilFix(Index);
            yield return StructurePattern;
            yield return ValuePattern;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public ZilResult Apply(Context ctx, ZilObject[] args)
        {
            if (EvalSequence(ctx, args).TryToZilObjectArray(out var args2, out var zr))
                return ApplyNoEval(ctx, args2);

            return zr;
        }

        /// <exception cref="InterpreterError"><paramref name="args"/> has the wrong number or types of elements.</exception>
        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args)
        {
            try
            {
                switch (args.Length)
                {
                    case 1:
                        ctx.MaybeCheckDecl(args[0], StructurePattern, "argument {0}", 1);
                        var result = Subrs.NTH(ctx, (IStructure)args[0], Index);
                        ctx.MaybeCheckDecl(result, ValuePattern, "element {0}", Index);
                        return result;
                    case 2:
                        ctx.MaybeCheckDecl(args[0], StructurePattern, "argument {0}", 1);
                        ctx.MaybeCheckDecl(args[1], ValuePattern, "argument {0}", 2);
                        return Subrs.PUT(ctx, (IStructure)args[0], Index, args[1]);
                    default:
                        throw new InterpreterError(
                            InterpreterMessages._0_Expected_1_After_2,
                            InterpreterMessages.NoFunction,
                            "1 or 2 args",
                            "the OFFSET");
                }
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
