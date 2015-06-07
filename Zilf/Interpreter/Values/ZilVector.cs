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
                return items[index + offset - baseOffset];
            }

            public void PutItem(int offset, int index, ZilObject value)
            {
                items[index + offset - baseOffset] = value;
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
            return ZilList.SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ZilList.SequenceToString(storage.GetSequence(offset), "[", "]", zo => zo.ToStringContext(ctx, friendly));
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

        public override ZilObject Eval(Context ctx)
        {
            return new ZilVector(EvalSequence(ctx, this).ToArray()) { SourceLine = this.SourceLine };
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