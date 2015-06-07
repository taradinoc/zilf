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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [FSubr]
        public static ZilObject PROG(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformProg(ctx, args, "PROG", false, true);
        }

        [FSubr]
        public static ZilObject REPEAT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformProg(ctx, args, "REPEAT", true, true);
        }

        [FSubr]
        public static ZilObject BIND(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformProg(ctx, args, "BIND", false, false);
        }

        private static ZilObject PerformProg(Context ctx, ZilObject[] args, string name, bool repeat, bool catchy)
        {
            SubrContracts(ctx, args);
            Contract.Requires(name != null);

            if (args.Length < 2)
                throw new InterpreterError(name, 2, 0);

            // bind atoms
            ZilList bindings = args[0] as ZilList;
            if (bindings == null || bindings.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError(name + ": first arg must be a list of zero or more atom bindings");

            Queue<ZilAtom> boundAtoms = new Queue<ZilAtom>();

            try
            {
                foreach (ZilObject b in bindings)
                {
                    ZilAtom atom;
                    ZilList list;
                    ZilObject value;

                    switch (b.GetTypeAtom(ctx).StdAtom)
                    {
                        case StdAtom.ATOM:
                            ctx.PushLocalVal((ZilAtom)b, null);
                            boundAtoms.Enqueue((ZilAtom)b);
                            break;

                        case StdAtom.ADECL:
                            atom = ((ZilAdecl)b).First as ZilAtom;
                            if (atom == null)
                                throw new InterpreterError(name + ": invalid atom binding: " + b);
                            ctx.PushLocalVal(atom, null);
                            boundAtoms.Enqueue(atom);
                            break;

                        case StdAtom.LIST:
                            list = (ZilList)b;
                            if (list.First == null || list.Rest == null ||
                                list.Rest.First == null || (list.Rest.Rest != null && list.Rest.Rest.First != null))
                                throw new InterpreterError(name + ": binding with value must be a 2-element list");
                            atom = list.First as ZilAtom;
                            if (atom == null)
                            {
                                var adecl = list.First as ZilAdecl;
                                if (adecl != null)
                                    atom = adecl.First as ZilAtom;
                            }
                            if (atom == null)
                                throw new InterpreterError(name + ": invalid atom binding: " + b);
                            value = list.Rest.First;
                            ctx.PushLocalVal(atom, value.Eval(ctx));
                            boundAtoms.Enqueue(atom);
                            break;

                        default:
                            throw new InterpreterError(name + ": elements of binding list must be atoms or lists");
                    }
                }

                // evaluate body
                ZilObject result = null;
                if (catchy)
                {
                    bool again;
                    do
                    {
                        again = false;
                        for (int i = 1; i < args.Length; i++)
                        {
                            try
                            {
                                result = args[i].Eval(ctx);
                            }
                            catch (ReturnException ex)
                            {
                                return ex.Value;
                            }
                            catch (AgainException)
                            {
                                again = true;
                            }
                        }
                    } while (repeat || again);

                    Contract.Assert(result != null);
                }
                else
                {
                    do
                    {
                        for (int i = 1; i < args.Length; i++)
                            result = args[i].Eval(ctx);
                    } while (repeat);

                    Contract.Assert(result != null);
                }

                return result;
            }
            finally
            {
                while (boundAtoms.Count > 0)
                    ctx.PopLocalVal(boundAtoms.Dequeue());
            }
        }

        [Subr]
        public static ZilObject RETURN(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new ReturnException(ctx.TRUE);
            else if (args.Length == 1)
                throw new ReturnException(args[0]);
            else
                throw new InterpreterError("RETURN", 0, 1);
        }

        [Subr]
        public static ZilObject AGAIN(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new AgainException();
            else
                throw new InterpreterError("AGAIN", 0, 0);
        }
    }
}
