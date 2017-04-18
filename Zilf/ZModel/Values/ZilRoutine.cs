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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values.Tied;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.ROUTINE, PrimType.LIST)]
    class ZilRoutine : ZilTiedListBase
    {
        readonly ZilAtom name;
        readonly ArgSpec argspec;
        readonly ZilObject[] body;
        readonly RoutineFlags flags;

        public ZilRoutine(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body, RoutineFlags flags)
        {
            Contract.Requires(name != null);
            Contract.Requires(argspec != null);
            Contract.Requires(body != null);

            this.name = name;
            this.argspec = ArgSpec.Parse("ROUTINE", name, activationAtom, argspec);
            this.body = body.ToArray();
            this.flags = flags;
        }

        [ChtypeMethod]
        public static ZilRoutine FromList(Context ctx, ZilListBase list)
        {
            Contract.Requires(ctx != null);

            if (list.IsEmpty || list.Rest.IsEmpty)
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
            return TiedLayout.Create<ZilRoutine>(
                x => x.ArgSpecAsList,
                x => x.BodyAsList);
        }

        public ArgSpec ArgSpec => argspec;
        public IEnumerable<ZilObject> Body => body;
        public int BodyLength => body.Length;
        public ZilAtom Name => name;
        public ZilAtom ActivationAtom => argspec.ActivationAtom;
        public RoutineFlags Flags => flags;

        ZilList ArgSpecAsList => argspec.ToZilList();
        ZilList BodyAsList => new ZilList(body);

        public override StdAtom StdTypeAtom => StdAtom.ROUTINE;

        public override bool Equals(object obj)
        {
            if (!(obj is ZilRoutine other))
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