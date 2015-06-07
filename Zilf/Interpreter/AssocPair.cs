using System;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    internal struct AssocPair : IEquatable<AssocPair>
    {
        public readonly ZilObject First;
        public readonly ZilObject Second;

        public AssocPair(ZilObject first, ZilObject second)
        {
            this.First = first;
            this.Second = second;
        }

        public override bool Equals(object obj)
        {
            if (obj is AssocPair)
                return Equals((AssocPair)obj);
            else
                return false;
        }

        public bool Equals(AssocPair other)
        {
            return First.Equals(other.First) && Second.Equals(other.Second);
        }

        public override int GetHashCode()
        {
            return First.GetHashCode() ^ Second.GetHashCode();
        }
    }
}
