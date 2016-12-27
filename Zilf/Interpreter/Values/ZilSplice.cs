/* Copyright 2010-2016 Jesse McGrew
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
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.SPLICE, PrimType.LIST)]
    class ZilSplice : ZilList
    {
        bool spliceable;

        [ChtypeMethod]
        public ZilSplice(ZilList other)
            : base(other.First, other.Rest)
        {
        }

        public void SetSpliceableFlag()
        {
            spliceable = true;
        }

        public bool PopSpliceableFlag()
        {
            var result = spliceable;
            spliceable = false;
            return result;
        }

        public override string ToString()
        {
#pragma warning disable RECS0106 // False alarm, ToString() is required here
            return "#SPLICE " + base.ToString();
#pragma warning restore RECS0106
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return "#SPLICE " + base.ToStringContextImpl(ctx, friendly);
        }

        public override StdAtom StdTypeAtom => StdAtom.SPLICE;

        protected override ZilObject EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            return this;
        }
    }
}
