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
using System.Linq;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FIX, PrimType.FIX)]
    class ZilFix : ZilObject, IApplicable
    {
        readonly int value;

        public static readonly ZilFix Zero = new ZilFix(0);

        public ZilFix(int value)
        {
            this.value = value;
        }

        [ChtypeMethod]
        public ZilFix(ZilFix other)
            : this(other.value)
        {
            Contract.Requires(other != null);
        }

        public int Value => value;

        public override string ToString() => value.ToString();

        public override StdAtom StdTypeAtom => StdAtom.FIX;

        public override PrimType PrimType => PrimType.FIX;

        public override ZilObject GetPrimitive(Context ctx) => this;

        public override bool Equals(object obj)
        {
            return obj is ZilFix other && other.value == this.value;
        }

        public override int GetHashCode() => value.GetHashCode();

        #region IApplicable Members

        public ZilResult Apply(Context ctx, ZilObject[] args)
        {
            if (ZilObject.EvalSequence(ctx, args).TryToZilObjectArray(out args, out var zr))
            {
                return ApplyNoEval(ctx, args);
            }
            else
            {
                return zr;
            }
        }

        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args)
        {
            try
            {
                switch (args.Length)
                {
                    case 1:
                        return Subrs.NTH(ctx, (IStructure)args[0], this.value);

                    case 2:
                        return Subrs.PUT(ctx, (IStructure)args[0], this.value, args[1]);

                    default:
                        throw new InterpreterError(
                            InterpreterMessages._0_Expected_1_After_2,
                            InterpreterMessages.NoFunction,
                            "1 or 2 args",
                            "the FIX");
                            
                }
            }
            catch (InvalidCastException)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1_After_2,
                    InterpreterMessages.NoFunction,
                    "a structured value",
                    "the FIX");
            }
        }

        #endregion
    }
}