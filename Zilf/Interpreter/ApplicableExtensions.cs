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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    static class ApplicableExtensions
    {
        public static IApplicable AsApplicable(this ZilObject zo, Context ctx)
        {
            if (zo == null)
                return null;

            var del = ctx.GetApplyTypeDelegate(zo.GetTypeAtom(ctx));

            if (del != null)
                return new ApplicableWrapper(zo, del);

            return zo as IApplicable;
        }

        public static bool IsApplicable(this ZilObject zo, Context ctx)
        {
            if (zo == null)
                return false;

            return zo is IApplicable || ctx.GetApplyTypeDelegate(zo.GetTypeAtom(ctx)) != null;
        }

        private sealed class ApplicableWrapper : IApplicable
        {
            private readonly ZilObject zo;
            private readonly ApplyTypeDelegate del;

            public ApplicableWrapper(ZilObject zo, ApplyTypeDelegate del)
            {
                this.zo = zo;
                this.del = del;
            }

            public ZilObject Apply(Context ctx, ZilObject[] args)
            {
                return del(zo, ZilObject.EvalSequence(ctx, args).ToArray());
            }

            public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
            {
                return del(zo, args);
            }
        }
    }
}
