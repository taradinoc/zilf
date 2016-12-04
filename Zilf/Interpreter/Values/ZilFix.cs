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
using System.Linq;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FIX, PrimType.FIX)]
    class ZilFix : ZilObject, IApplicable
    {
        private readonly int value;

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

        public int Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FIX);
        }

        public override PrimType PrimType
        {
            get { return PrimType.FIX; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            ZilFix other = obj as ZilFix;
            return other != null && other.value == this.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IApplicable Members

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, EvalSequence(ctx, args).ToArray());
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            if (args.Length == 1)
            {
                try
                {
                    return Subrs.NTH(ctx, (IStructure)args[0], this.value);
                }
                catch (InvalidCastException)
                {
                    throw new InterpreterError(InterpreterMessages.Expected_A_Structured_Value_After_The_FIX);
                }
            }
            else if (args.Length == 2)
            {
                try
                {
                    return Subrs.PUT(ctx, (IStructure)args[0], this.value, args[1]);
                }
                catch (InvalidCastException)
                {
                    throw new InterpreterError(InterpreterMessages.Expected_A_Structured_Value_After_The_FIX);
                }
            }
            else
            {
                throw new InterpreterError(InterpreterMessages.Expected_1_Or_2_Args_After_A_FIX);
            }
        }

        #endregion
    }
}