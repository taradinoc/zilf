using System.Diagnostics.Contracts;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FALSE, PrimType.LIST)]
    class ZilFalse : ZilObject, IStructure
    {
        private readonly ZilList value;

        [ChtypeMethod]
        public ZilFalse(ZilList value)
        {
            Contract.Requires(value != null);
            this.value = value;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(value != null);
        }

        public override string ToString()
        {
            return "#FALSE " + value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FALSE);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return value;
        }

        public override bool IsTrue
        {
            get { return false; }
        }

        public override bool Equals(object obj)
        {
            ZilFalse other = obj as ZilFalse;
            return other != null && other.value.Equals(this.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return value.First;
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)value).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return value.IsEmpty;
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)value)[index];
            }
            set
            {
                ((IStructure)value)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)value).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)value).GetLength(limit);
        }

        #endregion
    }
}