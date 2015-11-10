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

namespace Zilf.Interpreter.Values
{
    class ZilHash : ZilObject
    {
        protected readonly ZilAtom type;
        protected readonly PrimType primtype;
        protected readonly ZilObject primvalue;

        internal ZilHash(ZilAtom type, PrimType primtype, ZilObject primvalue)
        {
            this.type = type;
            this.primtype = primtype;
            this.primvalue = primvalue;
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilHash && ((ZilHash)obj).type == this.type &&
                    ((ZilHash)obj).primvalue.Equals(this.primvalue));
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ primvalue.GetHashCode();
        }

        public ZilAtom Type
        {
            get { return type; }
        }

        // TODO: eliminate ZilHash.Parse in favor of Context.ChangeType
        public static ZilObject Parse(Context ctx, ZilObject[] initializer)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(initializer != null);

            if (initializer.Length != 2 || !(initializer[0] is ZilAtom) || initializer[1] == null)
                throw new ArgumentException("Expected 2 objects, the first a ZilAtom");

            ZilAtom type = (ZilAtom)initializer[0];
            ZilObject value = initializer[1];

            return ctx.ChangeType(value, type);
        }

        public override string ToString()
        {
            return "#" + type.ToString() + " " + primvalue.ToString();
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            var del = ctx.GetPrintTypeDelegate(type);
            if (del != null)
            {
                return del(this);
            }
            else
            {
                return "#" + type.ToStringContext(ctx, friendly) + " " + primvalue.ToStringContext(ctx, friendly);
            }
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return type;
        }

        public override PrimType PrimType
        {
            get { return primtype; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return primvalue;
        }

        public override ZilObject Eval(Context ctx)
        {
            return this;
        }
    }
}