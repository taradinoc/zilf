/* Copyright 2010-2018 Jesse McGrew
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
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.SPLICE, PrimType.LIST)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Late bound")]
    sealed class ZilSplice : ZilListBase, IMayExpandAfterEvaluation
    {
        bool spliceable;

        [ChtypeMethod]
        public ZilSplice(ZilListoidBase other)
            : base(other.First, other.Rest)
        {
        }

        public void SetSpliceableFlag()
        {
            spliceable = true;
        }

        public override StdAtom StdTypeAtom => StdAtom.SPLICE;

        public bool ShouldExpandAfterEvaluation => spliceable;

        public IEnumerable<ZilObject> ExpandAfterEvaluation()
        {
            spliceable = false;
            return this;
        }
    }
}
