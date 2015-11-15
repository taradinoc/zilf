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
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    [BuiltinType(StdAtom.OBLIST, PrimType.LIST)]
    class ObList : ZilObject
    {
        private readonly Dictionary<string, ZilAtom> dict = new Dictionary<string, ZilAtom>();
        private readonly bool ignoreCase;

        public ObList()
            : this(false)
        {
        }

        public ObList(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
        }

        [ChtypeMethod]
        public static ObList FromList(Context ctx, ZilList list)
        {
            var result = new ObList(ctx.IgnoreCase);

            while (!list.IsEmpty)
            {
                var pair = list.First as ZilList;
                list = list.Rest;

                if (pair == null || pair.GetTypeAtom(ctx).StdAtom != StdAtom.LIST ||
                    pair.IsEmpty || pair.Rest.IsEmpty || !pair.Rest.Rest.IsEmpty)
                {
                    throw new InterpreterError("list elements must be 2-element lists");
                }

                var key = pair.First as ZilString;
                var value = pair.Rest.First as ZilAtom;

                if (key == null || value == null)
                    throw new InterpreterError("list elements must be string/atom pairs");

                result[key.Text] = value;
            }

            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("#OBLIST (");

            bool any = false;
            foreach (var pair in dict)
            {
                sb.Append('(');
                sb.Append(ZilString.Quote(pair.Key));
                sb.Append(' ');
                sb.Append(pair.Value.ToString());
                sb.Append(") ");
                any = true;
            }

            if (any)
                sb.Remove(sb.Length - 1, 1);

            sb.Append(')');
            return sb.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.OBLIST);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(dict.Count);

            foreach (var pair in dict)
                result.Add(new ZilList(new ZilString(pair.Key),
                    new ZilList(pair.Value,
                        new ZilList(null, null))));

            return new ZilList(result);
        }

        public bool Contains(string pname)
        {
            string key = ignoreCase ? pname.ToUpper() : pname;
            return dict.ContainsKey(key);
        }

        public ZilAtom this[string pname]
        {
            get
            {
                string key = ignoreCase ? pname.ToUpper() : pname;

                ZilAtom result;
                if (dict.TryGetValue(key, out result))
                    return result;

                result = new ZilAtom(pname, this, StdAtom.None);
                dict.Add(key, result);
                return result;
            }
            set
            {
                string key = ignoreCase ? pname.ToUpper() : pname;
                dict[key] = value;
            }
        }

        internal void Add(ZilAtom newAtom)
        {
            Contract.Requires(newAtom != null);
            Contract.Requires(newAtom.ObList == this);

            var key = newAtom.Text;
            if (ignoreCase)
                key = key.ToUpper();

            dict[key] = newAtom;
        }

        internal void Remove(ZilAtom atom)
        {
            Contract.Requires(atom != null);
            Contract.Requires(atom.ObList != this);

            var key = atom.Text;
            if (ignoreCase)
                key = key.ToUpper();

            dict.Remove(key);
        }
    }
}
