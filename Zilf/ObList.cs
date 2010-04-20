/* Copyright 2010 Jesse McGrew
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
using System.Linq;
using System.Text;

namespace Zilf
{
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

        public override string ToString()
        {
            return "#OBLIST (NATIVE)";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.OBLIST);
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
    }
}
