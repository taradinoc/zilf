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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    // TODO: abstract base class for FUNCTION and ROUTINE
    [BuiltinType(StdAtom.FUNCTION, PrimType.LIST)]
    class ZilFunction : ZilTiedListBase, IApplicable
    {
        [NotNull]
        readonly ArgSpec argspec;

        [NotNull]
        readonly ZilObject[] body;

        /// <exception cref="InterpreterError"><paramref name="argspec"/> is invalid.</exception>
        public ZilFunction([CanBeNull] ZilAtom name, [CanBeNull] ZilAtom activationAtom,
            [NotNull] [ItemNotNull] IEnumerable<ZilObject> argspec, ZilDecl decl,
            [ItemNotNull] [NotNull] IEnumerable<ZilObject> body)
            : this("<internal>", name, activationAtom, argspec, decl, body)
        {
        }

        // TODO: convert to static method; caller parameter doesn't belong here
        /// <exception cref="InterpreterError"><paramref name="argspec"/> is invalid.</exception>
        public ZilFunction([NotNull] string caller, [CanBeNull] ZilAtom name, [CanBeNull] ZilAtom activationAtom,
            [NotNull] [ItemNotNull] IEnumerable<ZilObject> argspec, ZilDecl decl,
            [ItemNotNull] [NotNull] IEnumerable<ZilObject> body)
        {
            this.argspec = ArgSpec.Parse(caller, name, activationAtom, argspec, decl);
            this.body = body.ToArray();
        }

        [ChtypeMethod]
        [NotNull]
        public static ZilFunction FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {
            var functionSubr = ctx.GetSubrDelegate("FUNCTION");
            Debug.Assert(functionSubr != null);
            return (ZilFunction)functionSubr.Invoke("FUNCTION", ctx, list.ToArray());
        }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilFunction>(
                x => x.ArgSpecAsList)
                .WithCatchAll<ZilFunction>(x => x.BodyAsList);
        }

        [NotNull]
        public ZilList ArgSpecAsList => argspec.ToZilList();

        [NotNull]
        public ZilList BodyAsList => new ZilList(body);

        public override StdAtom StdTypeAtom => StdAtom.FUNCTION;

        public ZilResult Apply(Context ctx, ZilObject[] args) => ApplyImpl(ctx, args, true);

        public ZilResult ApplyNoEval(Context ctx, ZilObject[] args) => ApplyImpl(ctx, args, false);

        ZilResult ApplyImpl([NotNull] Context ctx, [ItemNotNull] [NotNull] ZilObject[] args, bool eval)
        {
            using (var application = argspec.BeginApply(ctx, args, eval))
            {
                if (application.EarlyResult != null)
                    return application.EarlyResult.Value;

                var activation = application.Activation;
                do
                {
                    var result = EvalProgram(ctx, body);
                    if (result.IsReturn(activation, out var value))
                    {
                        argspec.ValidateResult(ctx, value);
                        return value;
                    }

                    if (result.IsAgain(activation))
                    {
                        // repeat
                        continue;
                    }

                    return result;
                } while (true);
            }
        }

        public override bool StructurallyEquals(ZilObject obj)
        {
            if (!(obj is ZilFunction other))
                return false;

            if (!other.argspec.Equals(argspec))
                return false;

            if (other.body.Length != body.Length)
                return false;

            for (int i = 0; i < body.Length; i++)
                if (!other.body[i].StructurallyEquals(body[i]))
                    return false;

            return true;
        }
    }
}