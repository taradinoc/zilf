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

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.ROUTINE, PrimType.LIST)]
    class ZilRoutine : ZilObject
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
        public static ZilRoutine FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);

            if (list.IsEmpty || list.Rest.IsEmpty)
                throw new InterpreterError(
                    InterpreterMessages._0_Must_Have_1_Element1s,
                    "list coerced to ROUTINE",
                    new CountableString("at least 2", true));

            if (!(list.First is ZilList argList) || argList.StdTypeAtom != StdAtom.LIST)
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to ROUTINE", "a list");

            return new ZilRoutine(null, null, argList, list.Rest, RoutineFlags.None);
        }

        public ArgSpec ArgSpec
        {
            get { return argspec; }
        }

        public IEnumerable<ZilObject> Body
        {
            get { return body; }
        }

        public int BodyLength
        {
            get { return body.Length; }
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilAtom ActivationAtom
        {
            get { return argspec.ActivationAtom; }
        }

        public RoutineFlags Flags
        {
            get { return flags; }
        }

        string ToString(Func<ZilObject, string> convert)
        {
            var sb = new StringBuilder();

            sb.Append("#ROUTINE (");
            sb.Append(argspec.ToString(convert));

            foreach (ZilObject expr in body)
            {
                sb.Append(' ');
                sb.Append(convert(expr));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override StdAtom StdTypeAtom => StdAtom.ROUTINE;

        public override PrimType PrimType => PrimType.LIST;

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(1 + body.Length);
            result.Add(argspec.ToZilList());
            result.AddRange(body);
            return new ZilList(result);
        }

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