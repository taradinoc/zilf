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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinPrimType(PrimType.LIST)]
    abstract class ZilListBase : ZilListoidBase
    {
        ZilObject first;
        ZilListoidBase rest;

        public sealed override ZilObject First
        {
            get => first;

            set
            {
                if (rest == null && value != null)
                    throw new InvalidOperationException("Can't make an empty list non-empty");

                if (rest != null && value == null)
                    throw new InvalidOperationException("Can't make a non-empty list empty");

                first = value;
            }
        }

        public sealed override ZilListoidBase Rest
        {
            get => rest;

            set
            {
                if (first == null && value != null)
                    throw new InvalidOperationException("Can't make an empty list non-empty");

                if (first != null && value == null)
                    throw new InvalidOperationException("Can't make a non-empty list empty");

                rest = value;
            }
        }

        protected ZilListBase([NotNull] IEnumerable<ZilObject> sequence)
        {

            using (var tor = sequence.GetEnumerator())
            {
                if (tor.MoveNext())
                {
                    first = tor.Current;
                    rest = MakeRest(tor);
                }
                else
                {
                    first = null;
                    rest = null;
                }
            }
        }

        protected ZilListBase(ZilObject first, ZilListoidBase rest)
        {

            this.first = first;
            this.rest = rest;
        }

        [NotNull]
        protected ZilList MakeRest([NotNull] IEnumerator<ZilObject> tor)
        {

            if (tor.MoveNext())
            {
                var cur = tor.Current;
                var newRest = MakeRest(tor);
                return new ZilList(cur, newRest);
            }

            return new ZilList(null, null);
        }

        public sealed override bool IsEmpty => First == null;

        [NotNull]
        protected virtual string OpenBracket => $"#{StdTypeAtom} (";

        [NotNull]
        protected virtual string CloseBracket => ")";

        public override string ToString()
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return SequenceToString(this, OpenBracket, CloseBracket, zo => zo.ToString());
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return OpenBracket + "..." + CloseBracket;
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return SequenceToString(this, OpenBracket, CloseBracket, zo => zo.ToStringContext(ctx, friendly));
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return OpenBracket + "..." + CloseBracket;
        }

        [NotNull]
        public sealed override ZilObject GetPrimitive(Context ctx)
        {
            return GetType() == typeof(ZilList) ? this : new ZilList(First, Rest);
        }

        protected override ZilResult EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            return originalType != null ? ctx.ChangeType(this, originalType) : this;
        }

        public sealed override IEnumerator<ZilObject> GetEnumerator()
        {
            ZilListoidBase r = this;

            while (r.First != null)
            {
                yield return r.First;
                r = r.Rest;
                Debug.Assert(r != null);
            }
        }

        public override bool ExactlyEquals(ZilObject obj)
        {
            return ReferenceEquals(obj, this) ||
                obj is ZilListBase other && other.StdTypeAtom == StdTypeAtom && IsEmpty && other.IsEmpty;
        }

        public override int GetHashCode()
        {
            return IsEmpty ? StdTypeAtom.GetHashCode() : base.GetHashCode();
        }

        public sealed override bool StructurallyEquals(ZilObject obj)
        {
            if (ReferenceEquals(obj, this))
                return true;

            if (!(obj is ZilListBase other) || other.StdTypeAtom != StdTypeAtom)
                return false;

            if (First == null)
                return other.First == null;
            if (!First.StructurallyEquals(other.First))
                return false;

            if (Rest == null)
                return other.Rest == null;

            return Rest.StructurallyEquals(other.Rest);
        }

        public sealed override IStructure GetRest(int skip)
        {
            ZilListoidBase result = this;
            while (skip-- > 0 && result != null)
                result = result.Rest;
            return result;
        }

        public sealed override ZilObject this[int index]
        {
            get
            {
                var rested = GetRest(index);
                return rested?.GetFirst();
            }
            set
            {
                if (GetRest(index) is ZilList rested && !rested.IsEmpty)
                {
                    rested.First = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "writing past end of list");
                }
            }
        }

        public sealed override int GetLength() => this.Count();

        public sealed override int? GetLength(int limit)
        {
            using (var tor = GetEnumerator())
            {
                int count = 0;

                while (tor.MoveNext())
                {
                    count++;
                    if (count > limit)
                        return null;
                }

                return count;
            }
        }
    }
}