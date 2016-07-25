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

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.VECTOR, PrimType.VECTOR)]
    sealed class ZilVector : ZilObject, IEnumerable<ZilObject>, IStructure
    {
        #region Storage

        private class VectorStorage
        {
            private int baseOffset = 0;
            private readonly ZilObject[] items;

            public VectorStorage()
                : this(new ZilObject[0])
            {
            }

            public VectorStorage(ZilObject[] items)
            {
                Contract.Requires(items != null);

                this.items = items;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(items != null);
            }

            public IEnumerable<ZilObject> GetSequence(int offset)
            {
                return items.Skip(offset - baseOffset);
            }

            public int GetLength(int offset)
            {
                return items.Length - offset - baseOffset;
            }

            public ZilObject GetItem(int offset, int index)
            {
                try
                {
                    return items[index + offset - baseOffset];
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
                    items[index + offset - baseOffset] = value;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InterpreterError("index out of range: " + index);
                }
            }
        }

        #endregion

        private readonly VectorStorage storage;
        private readonly int offset;

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

        private ZilVector(VectorStorage storage, int offset)
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
            ZilVector other = obj as ZilVector;
            if (other == null)
                return false;

            return this.SequenceEqual(other);
        }

        public override int GetHashCode()
        {
            int result = (int)StdAtom.VECTOR;
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
            else
            {
                return "[...]";
            }
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
            else
            {
                return "[...]";
            }
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.VECTOR);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

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
            if (this.offset >= skip)
            {
                return new ZilVector(this.storage, this.offset - skip);
            }
            else
            {
                return null;
            }
        }

        public IStructure GetTop()
        {
            if (this.offset == 0)
            {
                return this;
            }
            else
            {
                return new ZilVector(this.storage, 0);
            }
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
            if (length <= limit)
                return length;
            else
                return null;
        }

        #endregion
    }
}