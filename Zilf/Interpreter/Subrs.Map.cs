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
        [Subr]
        public static ZilObject MAPF(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformMap(ctx, args, true);
        }

        [Subr]
        public static ZilObject MAPR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformMap(ctx, args, false);
        }

        private class MapRetException : ControlException
        {
            private readonly ZilObject[] values;

            public MapRetException(ZilObject[] values)
                : base("MAPRET")
            {
                Contract.Requires(values != null);
                this.values = values;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(values != null);
            }

            protected MapRetException(string name, ZilObject[] values)
                : base(name)
            {
                Contract.Requires(values != null);
                this.values = values;
            }

            public ZilObject[] Values
            {
                get
                {
                    Contract.Ensures(Contract.Result<ZilObject[]>() != null);
                    return values;
                }
            }
        }

        private class MapStopException : MapRetException
        {
            public MapStopException(ZilObject[] values)
                : base("MAPSTOP", values)
            {
                Contract.Requires(values != null);
            }
        }

        private class MapLeaveException : ControlException
        {
            private readonly ZilObject value;

            public MapLeaveException(ZilObject value)
                : base("MAPLEAVE")
            {
                this.value = value;
            }

            public ZilObject Value
            {
                get { return value; }
            }
        }

        private static ZilObject PerformMap(Context ctx, ZilObject[] args, bool first)
        {
            SubrContracts(ctx, args);

            string name = first ? "MAPF" : "MAPR";

            if (args.Length < 2)
                throw new InterpreterError(name + ": expected at least 2 args");

            IApplicable finalf = args[0] as IApplicable;

            if (finalf == null && args[0].IsTrue)
                throw new InterpreterError(name + ": first arg must be FALSE or an applicable object");

            IApplicable loopf = args[1] as IApplicable;

            if (loopf == null)
                throw new InterpreterError(name + ": second arg must be an applicable object");

            int numStructs = args.Length - 2;
            IStructure[] structs = new IStructure[numStructs];
            ZilObject[] loopArgs = new ZilObject[numStructs];

            for (int i = 0; i < numStructs; i++)
            {
                structs[i] = args[i + 2] as IStructure;
                if (structs[i] == null)
                    throw new InterpreterError(name + ": args after first two must be structures");
            }

            List<ZilObject> results = new List<ZilObject>();

            while (true)
            {
                // prepare loop args
                int i;
                for (i = 0; i < numStructs; i++)
                {
                    IStructure st = structs[i];
                    if (st == null || st.IsEmpty())
                        break;

                    if (first)
                        loopArgs[i] = st.GetFirst();
                    else
                        loopArgs[i] = (ZilObject)st;

                    structs[i] = st.GetRest(1);
                    Contract.Assume(structs[i] != null);
                }

                if (i < numStructs)
                    break;

                // apply loop function
                try
                {
                    results.Add(loopf.ApplyNoEval(ctx, loopArgs));
                }
                catch (MapStopException ex)
                {
                    // add values to results and exit loop
                    results.AddRange(ex.Values);
                    break;
                }
                catch (MapRetException ex)
                {
                    // add values to results and continue
                    results.AddRange(ex.Values);
                }
                catch (MapLeaveException ex)
                {
                    // discard results, skip finalf, and return this value from the map
                    return ex.Value;
                }
            }

            // apply final function
            if (finalf != null)
                return finalf.ApplyNoEval(ctx, results.ToArray());

            if (results.Count > 0)
                return results[results.Count - 1];

            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject MAPRET(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            throw new MapRetException(args);
        }

        [Subr]
        public static ZilObject MAPSTOP(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            throw new MapStopException(args);
        }

        [Subr]
        public static ZilObject MAPLEAVE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length > 1)
                throw new InterpreterError("MAPLEAVE", 0, 1);

            throw new MapLeaveException(args.Length == 0 ? ctx.TRUE : args[0]);
        }
    }
}
