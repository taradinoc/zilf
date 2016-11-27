﻿/* Copyright 2010, 2016 Jesse McGrew
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

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.LINK, PrimType.ATOM)]
    sealed class ZilLink : ZilAtom
    {
        public ZilLink(string pname, ObList list)
            : base(pname, list, StdAtom.None)
        {
        }

        public override ZilAtom GetTypeAtom(Context ctx) => ctx.GetStdAtom(StdAtom.LINK);

        [ChtypeMethod]
        public static new ZilLink FromAtom(Context ctx, ZilAtom atom)
        {
            throw new InterpreterError("cannot CHTYPE to LINK");
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            throw new InterpreterError("cannot CHTYPE away from LINK");
        }
    }
}