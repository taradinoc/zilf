/* Copyright 2010-2017 Jesse McGrew
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
    abstract class ZilHashBase<TPrim> : ZilObject
        where TPrim : class
    {
        protected readonly ZilAtom type;
        protected readonly PrimType primType;
        protected readonly TPrim primValue;

        protected ZilHashBase(ZilAtom type, PrimType primType, TPrim primValue)
        {
            this.type = type;
            this.primType = primType;
            this.primValue = primValue;
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilHashBase<TPrim> hash && hash.type == this.type &&
                    hash.primValue.Equals(this.primValue));
        }

        public override int GetHashCode() =>
            type.GetHashCode() ^ primValue.GetHashCode();

        public ZilAtom Type => type;

        public override string ToString() => "#" + type + " " + primValue;

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            if (Recursion.TryLock(this))
            {
                try
                {
                    var del = ctx.GetPrintTypeDelegate(type);
                    if (del != null)
                    {
                        return del(this);
                    }

                    return "#" + type.ToStringContext(ctx, friendly) + " " + GetPrimitive(ctx).ToStringContext(ctx, friendly);
                }
                finally
                {
                    Recursion.Unlock(this);
                }
            }

            return "#" + type.Text + "...";
        }

        public override ZilAtom GetTypeAtom(Context ctx) => type;

        public override StdAtom StdTypeAtom => type.StdAtom;

        public override PrimType PrimType => primType;
    }
}