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
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values.Tied;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    [BuiltinType(StdAtom.OBLIST, PrimType.LIST)]
    class ObList : ZilTiedListBase
    {
        readonly Dictionary<string, ZilAtom> dict = new Dictionary<string, ZilAtom>();
        readonly bool ignoreCase;

        public ObList()
            : this(false)
        {
        }

        public ObList(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
        }

        /// <exception cref="InterpreterError"><paramref name="list"/> has the wrong number or types of elements.</exception>
        [ChtypeMethod]
        public static ObList FromList([ProvidesContext] Context ctx, ZilListoidBase list)
        {
            var result = new ObList(ctx.IgnoreCase);

            while (list.IsCons(out var first, out var rest))
            {
                if (!(first is ZilListoidBase bucket))
                {
                    throw new InterpreterError(
                        InterpreterMessages._0_In_1_Must_Be_2,
                        "buckets",
                        "OBLIST",
                        "lists");
                }

                foreach (var elem in bucket)
                {
                    if (!(elem is ZilAtom atom))
                    {
                        throw new InterpreterError(
                            InterpreterMessages._0_In_1_Must_Be_2,
                            "elements",
                            "OBLIST bucket",
                            "atoms");
                    }

                    result[atom.Text] = atom;
                }

                list = rest;
            }

            return result;
        }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ObList>().WithCatchAll<ObList>(x => x.BucketList);
        }

        public ZilList BucketList
        {
            get
            {
                var empty = new ZilList(null, null);

                if (dict.Count == 0)
                    return empty;

                var bucket = new ZilList(dict.Values);
                return new ZilList(bucket, empty);
            }
        }

        public override StdAtom StdTypeAtom => StdAtom.OBLIST;

        public bool Contains(string pname)
        {
            string key = ignoreCase ? pname.ToUpperInvariant() : pname;
            return dict.ContainsKey(key);
        }

        public ZilAtom this[string pname]
        {
            get
            {
                string key = ignoreCase ? pname.ToUpperInvariant() : pname;

                if (dict.TryGetValue(key, out var result))
                    return result;

                result = new ZilAtom(pname, this, StdAtom.None);
                dict.Add(key, result);
                return result;
            }
            set
            {
                string key = ignoreCase ? pname.ToUpperInvariant() : pname;
                dict[key] = value;
            }
        }

        internal void Add(ZilAtom newAtom)
        {
            var key = newAtom.Text;
            if (ignoreCase)
                key = key.ToUpperInvariant();

            dict[key] = newAtom;
        }

        internal void Remove(ZilAtom atom)
        {
            var key = atom.Text;
            if (ignoreCase)
                key = key.ToUpperInvariant();

            dict.Remove(key);
        }
    }
}
