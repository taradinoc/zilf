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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ACTIVATION, PrimType.ATOM)]
    class ZilActivation : ZilObject, IDisposable, IEvanescent
    {
        readonly ZilAtom name;
        bool legal = true;

        public ZilActivation(ZilAtom name)
        {
            this.name = name;
        }

        public void Dispose()
        {
            legal = false;
        }

        [ChtypeMethod]
        public static ZilActivation FromAtom(Context ctx, ZilAtom name)
        {
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "ACTIVATION");
        }

        public override StdAtom StdTypeAtom => StdAtom.ACTIVATION;

        public override PrimType PrimType => PrimType.ATOM;

        public bool IsLegal => legal;

        public override ZilObject GetPrimitive(Context ctx)
        {
            return name;
        }

        public override string ToString()
        {
            return string.Format("#ACTIVATION {0}", name);
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return string.Format("#ACTIVATION {0}", name.ToStringContext(ctx, friendly));
        }
    }
}
