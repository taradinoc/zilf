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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [FSubr]
        public static ZilResult COND(Context ctx, [Required] CondClause[] clauses)
        {
            SubrContracts(ctx);

            ZilResult result = null;

            foreach (var clause in clauses)
            {
                result = clause.Condition.Eval(ctx);
                if (result.ShouldPass())
                    break;

                if (((ZilObject)result).IsTrue)
                {
                    foreach (var inner in clause.Body)
                    {
                        result = inner.Eval(ctx);
                        if (result.ShouldPass())
                            break;
                    }

                    break;
                }
            }

            return result;
        }

        [ZilStructuredParam(StdAtom.LIST)]
        public struct CondClause
        {
            public ZilObject Condition;
            public ZilObject[] Body;
        }

        [FSubr]
        public static ZilResult OR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject resultObj = ctx.FALSE;

            foreach (ZilObject arg in args)
            {
                var result = arg.Eval(ctx);
                if (result.ShouldPass())
                    return result;

                resultObj = (ZilObject)result;

                if (resultObj.IsTrue)
                    return resultObj;
            }

            return resultObj;
        }

        [FSubr]
        public static ZilResult AND(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject resultObj = ctx.TRUE;

            foreach (ZilObject arg in args)
            {
                var result = arg.Eval(ctx);
                if (result.ShouldPass())
                    return result;

                resultObj = (ZilObject)result;
                if (!resultObj.IsTrue)
                    return resultObj;
            }

            return resultObj;
        }
    }
}
