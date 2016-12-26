using System;
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.VECTOR, PrimType.VECTOR)]
    sealed class ZilVector : ZilObject, IEnumerable<ZilObject>, IStructure
    {
        #region Storage

        class VectorStorage
        {
            ZilObject[] items;

            public VectorStorage()
                : this(new ZilObject[0])
            {
            }

            public int BaseOffset { get; private set; }

            public VectorStorage(ZilObject[] items)
            {
                Contract.Requires(items != null);

                this.items = items;
            }

            [ContractInvariantMethod]
            void ObjectInvariant()
            {
                Contract.Invariant(items != null);
                Contract.Invariant(BaseOffset >= 0);
                Contract.Invariant(BaseOffset <= items.Length);
            }

            public IEnumerable<ZilObject> GetSequence(int offset)
            {
                return items.Skip(offset - BaseOffset);
            }

            public int GetLength(int offset)
            {
                return items.Length - offset - BaseOffset;
            }

            public ZilObject GetItem(int offset, int index)
            {
                try
                {
                    return items[index + offset + BaseOffset];
                }
                catch (IndexOutOfRangeException)
                {
                    return null;
                }
            }

            public void PutItem(int offset, int index, ZilObject value)
            {
                try
                {
                    items[index + offset + BaseOffset] = value;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public void Grow(int end, int beginning, ZilObject defaultValue)
            {
                Contract.Requires(end >= 0);
                Contract.Requires(beginning >= 0);

                if (end > 0 || beginning > 0)
                {
                    var newItems = new ZilObject[items.Length + end + beginning];
                    Array.Copy(items, 0, newItems, beginning, items.Length);

                    for (int i = 0; i < beginning; i++)
                    {
                        newItems[i] = defaultValue;
                    }

                    for (int i = newItems.Length - end; i < newItems.Length; i++)
                    {
                        newItems[i] = defaultValue;
                    }

                    items = newItems;
                    BaseOffset += beginning;
                }
            }
        }

        #endregion

        readonly VectorStorage storage;
        readonly int offset;

        public ZilVector()
        {
            storage = new VectorStorage();
            offset = 0;
        }

        [ChtypeMethod]
        public ZilVector(ZilVector other)
            : this(other.storage, other.offset)
        {
            Contract.Requires(other != null);
        }

        ZilVector(VectorStorage storage, int offset)
        {
            Contract.Requires(storage != null);

            this.storage = storage;
            this.offset = offset;
        }

        public ZilVector(params ZilObject[] items)
        {
            Contract.Requires(items != null);

            storage = new VectorStorage(items);
            offset = 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilVector;
            if (other == null)
                return false;

            return this.SequenceEqual(other);
        }

        public override int GetHashCode()
        {
            var result = (int)StdAtom.VECTOR;
            foreach (ZilObject obj in this)
                result = result * 31 + obj.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return ZilList.SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToString());
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return "[...]";
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return ZilList.SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToStringContext(ctx, friendly));
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }
            return "[...]";
        }

        public override StdAtom StdTypeAtom => StdAtom.VECTOR;

        public override PrimType PrimType => PrimType.VECTOR;

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            ZilObject result = new ZilVector(EvalSequence(ctx, this, environment).ToArray()) { SourceLine = this.SourceLine };
            if (originalType != null)
                result = ctx.ChangeType(result, originalType);
            return result;
        }

        #region IEnumerable<ZilObject> Members

        public IEnumerator<ZilObject> GetEnumerator()
        {
            return storage.GetSequence(offset).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return storage.GetItem(offset, 0);
        }

        public IStructure GetRest(int skip)
        {
            return new ZilVector(this.storage, this.offset + skip);
        }

        public IStructure GetBack(int skip)
        {
            if (this.offset + storage.BaseOffset >= skip)
            {
                return new ZilVector(this.storage, this.offset - skip);
            }
            return null;
        }

        public IStructure GetTop()
        {
            if (this.offset == -storage.BaseOffset)
            {
                return this;
            }
            return new ZilVector(storage, -storage.BaseOffset);
        }

        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            storage.Grow(end, beginning, defaultValue);
        }

        public bool IsEmpty()
        {
            return storage.GetLength(offset) <= 0;
        }

        public ZilObject this[int index]
        {
            get { return storage.GetItem(offset, index); }
            set { storage.PutItem(offset, index, value); }
        }

        public int GetLength()
        {
            return storage.GetLength(offset);
        }

        public int? GetLength(int limit)
        {
            var length = storage.GetLength(offset);
            return length <= limit ? length : (int?)null;
        }

        #endregion
    }
}