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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.LIST, PrimType.LIST)]
    class ZilList : ZilObject, IEnumerable<ZilObject>, IStructure
    {
        public ZilObject First;
        public ZilList Rest;

        public ZilList(IEnumerable<ZilObject> sequence)
        {
            Contract.Requires(sequence != null);

            using (IEnumerator<ZilObject> tor = sequence.GetEnumerator())
            {
                if (tor.MoveNext())
                {
                    this.First = tor.Current;
                    this.Rest = MakeRest(tor);
                }
                else
                {
                    this.First = null;
                    this.Rest = null;
                }
            }
        }

        public ZilList(ZilObject current, ZilList rest)
        {
            Contract.Requires((current == null && rest == null) || (current != null && rest != null));
            Contract.Ensures(First == current);
            Contract.Ensures(Rest == rest);

            this.First = current;
            this.Rest = rest;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant((First == null && Rest == null) || (First != null && Rest != null));
        }

        [ChtypeMethod]
        public static ZilList FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);

            return new ZilList(list.First, list.Rest);
        }

        private ZilList MakeRest(IEnumerator<ZilObject> tor)
        {
            Contract.Requires(tor != null);
            Contract.Ensures(Contract.Result<ZilList>() != null);

            if (tor.MoveNext())
            {
                ZilObject cur = tor.Current;
                ZilList rest = MakeRest(tor);
                return new ZilList(cur, rest);
            }
            else
                return new ZilList(null, null);
        }

        public bool IsEmpty
        {
            get
            {
                Contract.Ensures(
                    (Contract.Result<bool>() == false && First != null && Rest != null) ||
                    (Contract.Result<bool>() == true && First == null && Rest == null));
                return First == null && Rest == null;
            }
        }

        public static string SequenceToString(IEnumerable<ZilObject> items,
            string start, string end, Func<ZilObject, string> convert)
        {
            Contract.Requires(items != null);
            Contract.Requires(start != null);
            Contract.Requires(end != null);
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder(2);
            sb.Append(start);

            foreach (ZilObject obj in items)
            {
                if (sb.Length > 1)
                    sb.Append(' ');

                sb.Append(convert(obj));
            }

            sb.Append(end);
            return sb.ToString();
        }

        public override string ToString()
        {
            return SequenceToString(this, "(", ")", zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return SequenceToString(this, "(", ")", zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.LIST);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            if (this.GetType() == typeof(ZilList))
                return this;
            else
                return new ZilList(First, Rest);
        }

        public override ZilObject Eval(Context ctx)
        {
            return new ZilList(EvalSequence(ctx, this)) { SourceLine = this.SourceLine };
        }

        public IEnumerator<ZilObject> GetEnumerator()
        {
            ZilList r = this;
            while (r.First != null)
            {
                yield return r.First;
                r = r.Rest;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            ZilList other = obj as ZilList;
            if (other == null)
                return false;

            if (this.First == null)
                return other.First == null;
            if (!this.First.Equals(other.First))
                return false;

            if (this.Rest == null)
                return other.Rest == null;

            return this.Rest.Equals(other.Rest);
        }

        public override int GetHashCode()
        {
            int result = (int)StdAtom.LIST;
            foreach (ZilObject obj in this)
                result = result * 31 + obj.GetHashCode();
            return result;
        }

        ZilObject IStructure.GetFirst()
        {
            return First;
        }

        IStructure IStructure.GetRest(int skip)
        {
            ZilList result = this;
            while (skip-- > 0 && result != null)
                result = (ZilList)result.Rest;
            return result;
        }

        bool IStructure.IsEmpty()
        {
            return First == null;
        }

        ZilObject IStructure.this[int index]
        {
            get
            {
                IStructure rested = ((IStructure)this).GetRest(index);
                if (rested == null)
                    return null;
                else
                    return rested.GetFirst();
            }
            set
            {
                IStructure rested = ((IStructure)this).GetRest(index);
                if (rested == null)
                    throw new ArgumentOutOfRangeException("index", "writing past end of list");
                ((ZilList)rested).First = value;
            }
        }

        int IStructure.GetLength()
        {
            return this.Count();
        }

        int? IStructure.GetLength(int limit)
        {
            int count = 0;

            foreach (ZilObject obj in this)
            {
                count++;
                if (count > limit)
                    return null;
            }

            return count;
        }
    }
}