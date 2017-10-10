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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;

using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace Zilf.Interpreter.Values
{
    [BuiltinPrimType(PrimType.LIST)]
    abstract class ZilListBase : ZilListoidBase
    {
        ZilObject first;
        ZilList rest;

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

        public sealed override ZilList Rest
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
            Contract.Requires(sequence != null);

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

        protected ZilListBase(ZilObject first, ZilList rest)
        {
            Contract.Requires((first == null && rest == null) || (first != null && rest != null));
            Contract.Ensures(First == first);
            Contract.Ensures(ReferenceEquals(Rest, rest));

            this.first = first;
            this.rest = rest;
        }

        [ContractInvariantMethod]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant((First == null && Rest == null) || (First != null && Rest != null));
        }

        [NotNull]
        protected ZilList MakeRest([NotNull] IEnumerator<ZilObject> tor)
        {
            Contract.Requires(tor != null);
            Contract.Ensures(Contract.Result<ZilList>() != null);

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
            var r = this;

            while (r.First != null)
            {
                yield return r.First;
                r = r.Rest;
                Debug.Assert(r != null);
            }
        }

        /// <summary>
        /// Enumerates the items of the list, yielding a final <see langword="null"/> instead of repeating if the list is recursive.
        /// </summary>
        /// <returns></returns>
        [ItemCanBeNull]
        [Pure]
        public IEnumerable<ZilObject> EnumerateNonRecursive()
        {
            var seen = new HashSet<ZilListBase>(IdentityEqualityComparer<ZilListBase>.Instance);
            var list = this;

            while (!list.IsEmpty)
            {
                if (seen.Contains(list))
                {
                    yield return null;
                    yield break;
                }

                seen.Add(list);
                yield return list.First;
                list = list.Rest;
                Debug.Assert(list != null);
            }
        }

        public sealed override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            if (!(obj is ZilListBase other) || other.StdTypeAtom != StdTypeAtom)
                return false;

            if (First == null)
                return other.First == null;
            if (!First.Equals(other.First))
                return false;

            if (Rest == null)
                return other.Rest == null;

            return Rest.Equals(other.Rest);
        }

        public sealed override int GetHashCode()
        {
            var result = (int)StdTypeAtom;
            foreach (ZilObject obj in EnumerateNonRecursive())
            {
                if (obj == null)
                    break;

                result = result * 31 + obj.GetHashCode();
            }
            return result;
        }

        public sealed override IStructure GetRest(int skip)
        {
            var result = this;
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
            int count = 0;

            foreach (var _ in this)
            {
                count++;
                if (count > limit)
                    return null;
            }

            return count;
        }
    }
}