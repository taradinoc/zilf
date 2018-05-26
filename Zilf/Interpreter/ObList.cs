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
        [NotNull]
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
        [NotNull]
        [ChtypeMethod]
        public static ObList FromList([NotNull] [ProvidesContext] Context ctx, [NotNull] ZilListBase list)
        {
            var result = new ObList(ctx.IgnoreCase);

            while (!list.IsEmpty)
            {
                Debug.Assert(list.First != null);
                Debug.Assert(list.Rest != null);

                if (list.First is ZilList pair)
                {
                    if (pair.First is ZilString key && pair.Rest?.First is ZilAtom value)
                    {
                        Debug.Assert(pair.Rest.Rest != null);

                        if (!pair.Rest.Rest.IsEmpty)
                        {
                            throw new InterpreterError(InterpreterMessages._0_In_1_Must_Have_2_Element2s, "elements", "OBLIST", 2);
                        }

                        result[key.Text] = value;
                    }
                    else
                    {
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "elements", "OBLIST", "string-atom pairs");
                    }
                }

                list = list.Rest;
            }

            return result;
        }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ObList>().WithCatchAll<ObList>(x => x.PairsList);
        }

        [NotNull]
        public ZilList PairsList
        {
            get
            {
                var result = new List<ZilObject>(dict.Count);

                foreach (var pair in dict)
                {
                    Debug.Assert(pair.Key != null, "pair.Key != null");
                    Debug.Assert(pair.Value != null, "pair.Value != null");
                    result.Add(
                        new ZilList(ZilString.FromString(pair.Key),
                            new ZilList(pair.Value,
                                new ZilList(null, null))));
                }

                return new ZilList(result);
            }
        }

        public override StdAtom StdTypeAtom => StdAtom.OBLIST;

        public bool Contains([NotNull] string pname)
        {
            string key = ignoreCase ? pname.ToUpperInvariant() : pname;
            return dict.ContainsKey(key);
        }

        [NotNull]
        public ZilAtom this[[NotNull] string pname]
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

        internal void Add([NotNull] ZilAtom newAtom)
        {

            var key = newAtom.Text;
            if (ignoreCase)
                key = key.ToUpperInvariant();

            dict[key] = newAtom;
        }

        internal void Remove([NotNull] ZilAtom atom)
        {

            var key = atom.Text;
            if (ignoreCase)
                key = key.ToUpperInvariant();

            dict.Remove(key);
        }
    }
}
