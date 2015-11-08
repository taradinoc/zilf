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
using System;
using System.Diagnostics.Contracts;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ADECL, PrimType.VECTOR)]
    sealed class ZilAdecl : ZilObject, IStructure
    {
        public ZilObject First;
        public ZilObject Second;

        [ChtypeMethod]
        public ZilAdecl(ZilVector vector)
        {
            Contract.Requires(vector != null);

            if (vector.GetLength() != 2)
                throw new InterpreterError("vector coerced to ADECL must have length 2");

            First = vector[0];
            Second = vector[1];
        }

        public ZilAdecl(ZilObject first, ZilObject second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            this.First = first;
            this.Second = second;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ZilAdecl;
            if (other == null)
                return false;

            return other.First.Equals(First) && other.Second.Equals(Second);
        }

        public override int GetHashCode()
        {
            var result = (int)StdAtom.ADECL;
            result = result * 31 + First.GetHashCode();
            result = result * 31 + Second.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return First.ToString() + ":" + Second.ToString();
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return First.ToStringContext(ctx, friendly) + ":" + Second.ToStringContext(ctx, friendly);
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ADECL);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(First, Second);
        }

        public override ZilObject Eval(Context ctx)
        {
            // TODO: check decl (Second) after evaluating First
            return First.Eval(ctx);
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            throw new NotImplementedException();
        }

        public IStructure GetRest(int skip)
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public ZilObject this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int GetLength()
        {
            throw new NotImplementedException();
        }

        public int? GetLength(int limit)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}