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

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.ROUTINE, PrimType.LIST)]
    class ZilRoutine : ZilTiedListBase
    {
        [NotNull]
        readonly ZilObject[] body;

        public ZilRoutine([CanBeNull] ZilAtom name, ZilAtom activationAtom, [NotNull] IEnumerable<ZilObject> argspec, [NotNull] IEnumerable<ZilObject> body, RoutineFlags flags)
        {
            Contract.Requires(argspec != null);
            Contract.Requires(body != null);

            Name = name;
            ArgSpec = ArgSpec.Parse("ROUTINE", name, activationAtom, argspec);
            this.body = body.ToArray();
            Flags = flags;
        }

        /// <exception cref="InterpreterError"><paramref name="list"/> has the wrong number or types of elements.</exception>
        [NotNull]
        [ChtypeMethod]
        public static ZilRoutine FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilRoutine>() != null);

            if (list.Rest?.IsEmpty != true)
                throw new InterpreterError(
                    InterpreterMessages._0_Must_Have_1_Element1s,
                    "list coerced to ROUTINE",
                    new CountableString("at least 2", true));

            if (list.First is ZilList argList)
            {
                return new ZilRoutine(null, null, argList, list.Rest, RoutineFlags.None);
            }

            throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to ROUTINE", "a list");
        }

        protected override TiedLayout GetLayout()
        {
            Contract.Ensures(Contract.Result<TiedLayout>() != null);
            return TiedLayout.Create<ZilRoutine>(
                x => x.ArgSpecAsList,
                x => x.BodyAsList);
        }

        [NotNull]
        public ArgSpec ArgSpec { get; }

        [NotNull]
        public IEnumerable<ZilObject> Body => body;
        public int BodyLength => body.Length;

        [CanBeNull]
        public ZilAtom Name { get; }

        [CanBeNull]
        public ZilAtom ActivationAtom => ArgSpec.ActivationAtom;
        public RoutineFlags Flags { get; }

        [NotNull]
        ZilList ArgSpecAsList => ArgSpec.ToZilList();

        [NotNull]
        ZilList BodyAsList => new ZilList(body);

        public override StdAtom StdTypeAtom => StdAtom.ROUTINE;

        public override bool Equals(object obj)
        {
            if (!(obj is ZilRoutine other))
                return false;

            if (!other.ArgSpec.Equals(ArgSpec))
                return false;

            if (other.body.Length != body.Length)
                return false;

            for (int i = 0; i < body.Length; i++)
                if (!other.body[i].Equals(body[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var result = ArgSpec.GetHashCode();

            foreach (var obj in body)
                result ^= obj.GetHashCode();

            return result;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(body != null);
        }
    }
}