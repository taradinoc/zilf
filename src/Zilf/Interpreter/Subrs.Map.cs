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
using System.Collections.Generic;
using Zilf.Common;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        public static ZilResult MAPF(Context ctx,
            [Decl("<OR FALSE APPLICABLE>")] ZilObject finalf,
            IApplicable loopf, IStructure[] structs)
        {
            return PerformMap(ctx, finalf, loopf, structs, true);
        }

        [Subr]
        public static ZilResult MAPR(Context ctx,
            [Decl("<OR FALSE APPLICABLE>")] ZilObject finalf,
            IApplicable loopf, IStructure[] structs)
        {
            return PerformMap(ctx, finalf, loopf, structs, false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        static ZilResult PerformMap(Context ctx, ZilObject finalf, IApplicable loopf, IStructure[] structs, bool first)
        {
            if (structs == null)
                throw new ArgumentNullException(nameof(structs));

            int numStructs = structs.Length;
            var loopArgs = new ZilObject[numStructs];

            var results = new List<ZilObject>();
            bool running = true;

            while (running)
            {
                // prepare loop args
                int i;
                for (i = 0; i < numStructs; i++)
                {
                    var st = structs[i];
                    if (st?.IsEmpty != false)
                        break;

                    if (first)
                        loopArgs[i] = st.GetFirst()!;
                    else
                        loopArgs[i] = (ZilObject)st;

                    structs[i] = st.GetRest(1)!;
                }

                if (i < numStructs)
                    break;

                // apply loop function
                var result = loopf.ApplyNoEval(ctx, loopArgs);

                if (result.IsMapControl(out var outcome, out var value))
                {
                    switch (outcome)
                    {
                        case ZilResult.Outcome.MapStop:
                            // add values to results and exit loop
                            results.AddRange((IEnumerable<ZilObject>)value);
                            running = false;
                            continue;

                        case ZilResult.Outcome.MapRet:
                            // add values to results and continue
                            results.AddRange((IEnumerable<ZilObject>)value);
                            continue;

                        case ZilResult.Outcome.MapLeave:
                            // discard results, skip finalf, and return this value from the map
                            return value;

                        default:
                            throw new UnhandledCaseException(outcome.ToString());
                    }
                }

                if (result.ShouldPass())
                {
                    return result;
                }

                results.Add((ZilObject)result);
            }

            // apply final function
            if (finalf.IsApplicable(ctx, out var finalfApplicable))
                return finalfApplicable.ApplyNoEval(ctx, results.ToArray());

            return results.Count > 0 ? results[^1] : ctx.FALSE;
        }

        [Subr]
        public static ZilResult MAPRET(Context ctx, ZilObject[] args) => ZilResult.MapRet(args);

        [Subr]
        public static ZilResult MAPSTOP(Context ctx, ZilObject[] args) => ZilResult.MapStop(args);

        [Subr]
        public static ZilResult MAPLEAVE(Context ctx, ZilObject? value = null) => ZilResult.MapLeave(value ?? ctx.TRUE);
    }
}
