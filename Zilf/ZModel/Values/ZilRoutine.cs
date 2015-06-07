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
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    class ZilRoutine : ZilObject
    {
        private readonly ZilAtom name, activationAtom;
        private readonly ArgSpec argspec;
        private readonly ZilObject[] body;
        private readonly RoutineFlags flags;

        public ZilRoutine(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body, RoutineFlags flags)
        {
            Contract.Requires(name != null);
            Contract.Requires(argspec != null);
            Contract.Requires(body != null);

            this.name = name;
            this.activationAtom = activationAtom;
            this.argspec = new ArgSpec(name, argspec);
            this.body = body.ToArray();
            this.flags = flags;
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
            get { return activationAtom; }
        }

        public RoutineFlags Flags
        {
            get { return flags; }
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            StringBuilder sb = new StringBuilder();

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

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ROUTINE);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(1 + body.Length);
            result.Add(argspec.ToZilList());
            result.AddRange(body);
            return new ZilList(result);
        }

        public override bool Equals(object obj)
        {
            ZilRoutine other = obj as ZilRoutine;
            if (other == null)
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
            int result = argspec.GetHashCode();

            foreach (ZilObject obj in body)
                result ^= obj.GetHashCode();

            return result;
        }
    }
}