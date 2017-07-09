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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    // TODO: abstract base class for FUNCTION and ROUTINE
    [BuiltinType(StdAtom.FUNCTION, PrimType.LIST)]
    class ZilFunction : ZilTiedListBase, IApplicable
    {
        ArgSpec argspec;
        readonly ZilObject[] body;

        public ZilFunction(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, ZilDecl decl, IEnumerable<ZilObject> body)
            : this("<internal>", name, activationAtom, argspec, decl, body)
        {
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
            Contract.Requires(body != null && Contract.ForAll(body, b => b != null));
        }

        // TODO: convert to static method; caller parameter doesn't belong here
        public ZilFunction(string caller, ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, ZilDecl decl, IEnumerable<ZilObject> body)
        {
            Contract.Requires(caller != null);
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
            Contract.Requires(body != null && Contract.ForAll(body, b => b != null));

            this.argspec = ArgSpec.Parse(caller, name, activationAtom, argspec, decl);
            this.body = body.ToArray();
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(argspec != null);
            Contract.Invariant(body != null);
            Contract.Invariant(Contract.ForAll(body, b => b != null));
        }

        [ChtypeMethod]
        public static ZilFunction FromList(Context ctx, ZilListBase list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilFunction>() != null);

            return (ZilFunction)ctx.GetSubrDelegate("FUNCTION")
                .Invoke("FUNCTION", ctx, list.ToArray());
        }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilFunction>(
                x => x.ArgSpecAsList)
                .WithCatchAll<ZilFunction>(x => x.BodyAsList);
        }

        public ZilList ArgSpecAsList => argspec.ToZilList();
        public ZilList BodyAsList => new ZilList(body);

        public override StdAtom StdTypeAtom => StdAtom.FUNCTION;

        public ZilResult Apply(Context ctx, ZilObject[] args) => ApplyImpl(ctx, args, true);

        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args) => ApplyImpl(ctx, args, false);

        ZilResult ApplyImpl(Context ctx, ZilObject[] args, bool eval)
        {
            using (var application = argspec.BeginApply(ctx, args, eval))
            {
                if (application.EarlyResult != null)
                    return application.EarlyResult.Value;

                var activation = application.Activation;
                do
                {
                    var result = ZilObject.EvalProgram(ctx, body);
                    if (result.IsReturn(activation, out var value))
                    {
                        argspec.ValidateResult(ctx, value);
                        return value;
                    }
                    else if (result.IsAgain(activation))
                    {
                        // repeat
                    }
                    else
                    {
                        return result;
                    }
                } while (true);
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ZilFunction other))
                return false;

            if (!other.argspec.Equals(this.argspec))
                return false;

            if (other.body.Length != this.body.Length)
                return false;

            for (int i = 0; i < body.Length; i++)
                if (!other.body[i].Equals(this.body[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var result = argspec.GetHashCode();

            foreach (ZilObject obj in body)
                result ^= obj.GetHashCode();

            return result;
        }
    }
}