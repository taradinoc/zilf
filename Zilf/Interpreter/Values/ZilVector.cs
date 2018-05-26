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
using System.Linq;
using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.VECTOR, PrimType.VECTOR)]
    sealed class ZilVector : ZilObject, IStructure
    {
        #region Storage

        class VectorStorage
        {
            [NotNull]
            ZilObject[] items;

            public VectorStorage()
                : this(new ZilObject[0])
            {
            }

            public int BaseOffset { get; private set; }

            public VectorStorage([NotNull] ZilObject[] items)
            {

                this.items = items;
            }

            [NotNull]
            public IEnumerable<ZilObject> GetSequence(int offset)
            {
                return items.Skip(offset + BaseOffset);
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

            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
            public void PutItem(int offset, int index, ZilObject value)
            {
                try
                {
                    items[index + offset + BaseOffset] = value;
                }
                catch (IndexOutOfRangeException ex)
                {
                    throw new ArgumentOutOfRangeException("Index out of range", ex);
                }
            }

            public void Grow(int end, int beginning, ZilObject defaultValue)
            {

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
        public ZilVector([NotNull] ZilVector other)
            : this(other.storage, other.offset)
        {
        }

        ZilVector([NotNull] VectorStorage storage, int offset)
        {

            this.storage = storage;
            this.offset = offset;
        }

        public ZilVector([NotNull] params ZilObject[] items)
        {

            storage = new VectorStorage(items);
            offset = 0;
        }

        public override bool StructurallyEquals(ZilObject obj)
        {
            return obj is ZilVector other && this.SequenceStructurallyEqual(other);
        }

        public override string ToString()
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    return SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToString());
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
                    return SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToStringContext(ctx, friendly));
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

        [NotNull]
        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        protected override ZilResult EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            ZilResult result = EvalSequence(ctx, this, environment).ToZilVectorResult(SourceLine);
            if (result.ShouldPass())
                return result;

            return originalType != null ? ctx.ChangeType((ZilObject)result, originalType) : result;
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
            return skip > GetLength() ? null : new ZilVector(storage, offset + skip);
        }

        public IStructure GetBack(int skip)
        {
            if (offset + storage.BaseOffset >= skip)
            {
                return new ZilVector(storage, offset - skip);
            }
            return null;
        }

        public IStructure GetTop()
        {
            if (offset == -storage.BaseOffset)
            {
                return this;
            }
            return new ZilVector(storage, -storage.BaseOffset);
        }

        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            storage.Grow(end, beginning, defaultValue);
        }

        public bool IsEmpty => storage.GetLength(offset) <= 0;

        /// <exception cref="ArgumentOutOfRangeException" accessor="set"><paramref name="index"/> is out of range.</exception>
        public ZilObject this[int index]
        {
            get => storage.GetItem(offset, index);
            set => storage.PutItem(offset, index, value);
        }

        public int GetLength() => storage.GetLength(offset);

        public int? GetLength(int limit)
        {
            var length = storage.GetLength(offset);
            return length <= limit ? length : (int?)null;
        }

        #endregion
    }
}