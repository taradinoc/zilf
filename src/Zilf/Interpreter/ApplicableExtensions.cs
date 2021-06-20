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

using System;
using System.Diagnostics.CodeAnalysis;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    static class ApplicableExtensions
    {
        public static bool IsApplicable([NotNullWhen(true)] this ZilObject? zo, Context ctx)
        {
            if (zo == null)
                return false;

            return zo is IApplicable || ctx.GetApplyTypeDelegate(zo.GetTypeAtom(ctx)) != null;
        }

        public static bool IsApplicable([NotNullWhen(true)] this ZilObject? zo, Context ctx,
            [NotNullWhen(true)] out IApplicable? asApplicable)
        {
            if (zo == null)
            {
                asApplicable = null;
                return false;
            }

            var del = ctx.GetApplyTypeDelegate(zo.GetTypeAtom(ctx));

            if (del != null)
            {
                asApplicable = new ApplicableWrapper(zo, del);
                return true;
            }

            if (zo is IApplicable applicable)
            {
                asApplicable = applicable;
                return true;
            }

            asApplicable = null;
            return false;
        }

        sealed class ApplicableWrapper : IApplicable
        {
            readonly ZilObject zo;
            readonly ApplyTypeDelegate del;

            public ApplicableWrapper(ZilObject zo, ApplyTypeDelegate del)
            {
                this.zo = zo;
                this.del = del;
            }

            public ZilResult Apply(Context ctx, ZilObject[] args) =>
                ZilObject.EvalSequence(ctx, args).TryToZilObjectArray(out args!, out var zr)
                    ? del(zo, args)
                    : zr;

            public ZilResult ApplyNoEval(Context ctx, ZilObject[] args) => del(zo, args);
        }
    }
}
