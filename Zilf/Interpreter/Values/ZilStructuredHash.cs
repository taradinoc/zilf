namespace Zilf.Interpreter.Values
{
    class ZilStructuredHash : ZilHash, IStructure
    {
        public ZilStructuredHash(ZilAtom type, PrimType primtype, ZilObject primvalue)
            : base(type, primtype, primvalue)
        {
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilStructuredHash && ((ZilStructuredHash)obj).type == this.type &&
                    ((ZilStructuredHash)obj).primvalue.Equals(this.primvalue));
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ primvalue.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return ((IStructure)primvalue).GetFirst();
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)primvalue).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return ((IStructure)primvalue).IsEmpty();
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)primvalue)[index];
            }
            set
            {
                ((IStructure)primvalue)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)primvalue).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)primvalue).GetLength(limit);
        }

        #endregion
    }
}