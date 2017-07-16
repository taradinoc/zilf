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

using JetBrains.Annotations;
using Zilf.Interpreter.Values;
using System.Diagnostics.Contracts;

namespace Zilf.Interpreter
{
    static class ApplicableExtensions
    {
        [CanBeNull]
        [ContractAnnotation("zo: null => null")]
        public static IApplicable AsApplicable([CanBeNull] this ZilObject zo, [NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);
            if (zo == null)
                return null;

            var del = ctx.GetApplyTypeDelegate(zo.GetTypeAtom(ctx));

            if (del != null)
                return new ApplicableWrapper(zo, del);

            return zo as IApplicable;
        }

        [ContractAnnotation("zo: null => false")]
        public static bool IsApplicable([CanBeNull] this ZilObject zo, [NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);
            if (zo == null)
                return false;

            return zo is IApplicable || ctx.GetApplyTypeDelegate(zo.GetTypeAtom(ctx)) != null;
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

            public ZilResult Apply(Context ctx, ZilObject[] args)
            {
                return ZilObject.EvalSequence(ctx, args).TryToZilObjectArray(out args, out var zr)
                    ? del(zo, args)
                    : zr;
            }

            public ZilResult ApplyNoEval(Context ctx, ZilObject[] args)
            {
                return del(zo, args);
            }
        }
    }
}
