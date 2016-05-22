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
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [FSubr]
        public static ZilObject COND(Context ctx, [Decl("<LIST ANY>"), Required] ZilObject[] /*XXX ZilList[] */ args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("COND", 1, 0);

            ZilObject result = null;

            foreach (ZilObject zo in args)
            {
                if (zo.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                {
                    ZilList zl = (ZilList)zo;

                    if (zl.IsEmpty)
                        throw new InterpreterError("COND: lists must be non-empty");

                    result = zl.First.Eval(ctx);

                    if (result.IsTrue)
                    {
                        foreach (ZilObject inner in zl.Skip(1))
                            result = inner.Eval(ctx);

                        return result;
                    }
                }
                else
                    throw new InterpreterError("COND: args must be lists");
            }

            return result;
        }

        [FSubr]
        public static ZilObject OR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject result = ctx.FALSE;

            foreach (ZilObject arg in args)
            {
                result = arg.Eval(ctx);
                if (result.IsTrue)
                    return result;
            }

            return result;
        }

        [FSubr]
        public static ZilObject AND(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject result = ctx.TRUE;

            foreach (ZilObject arg in args)
            {
                result = arg.Eval(ctx);
                if (!result.IsTrue)
                    return result;
            }

            return result;
        }
    }
}
