/* Copyright 2010-2023 Tara McGrew
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

using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        /// <summary>
        /// Evaluates a series of conditional clauses, returning the result of the first clause whose condition is true.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="clauses">A sequence of lists, where the first element of each list is the condition that gates evaluation of the rest of the list.</param>
        /// <returns>The result of the last evaluation performed, i.e., the last expression in the first clause whose condition is true, or false if no clause is true.</returns>
        [FSubr]
        public static ZilResult COND(Context ctx, [Required] CondClause[] clauses)
        {
            ZilResult result = ctx.FALSE;

            foreach (var clause in clauses)
            {
                result = clause.Condition.Eval(ctx);
                if (result.ShouldPass())
                    break;

                if (!((ZilObject)result).IsTrue)
                    continue;

                foreach (var inner in clause.Body)
                {
                    result = inner.Eval(ctx);
                    if (result.ShouldPass())
                        break;
                }

                break;
            }

            return result;
        }

#pragma warning disable CS0649
        [ZilStructuredParam(StdAtom.LIST)]
        public struct CondClause
        {
            public ZilObject Condition;
            public ZilObject[] Body;
        }
#pragma warning restore CS0649

        /// <summary>
        /// Evaluates a series of expressions, returning as soon as one is true.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">A sequence of expressions to evaluate.</param>
        /// <returns>The result of the last evaluation performed, i.e., the first expression that evaluates to true, or false if none do.</returns>
        [FSubr]
        public static ZilResult OR(Context ctx, ZilObject[] args)
        {
            var resultObj = ctx.FALSE;

            foreach (var arg in args)
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

        /// <summary>
        /// Evaluates a series of expressions, returning as soon as one is false.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">A sequence of expressions to evaluate.</param>
        /// <returns>The result of the last evaluation performed, i.e., the first expression that evaluates to false, or true if none do.</returns>
        [FSubr]
        public static ZilResult AND(Context ctx, ZilObject[] args)
        {
            var resultObj = ctx.TRUE;

            foreach (var arg in args)
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
