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
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    delegate ZilObject SubrDelegate(string name, Context ctx, ZilObject[] args);

    static partial class Subrs
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class SubrAttribute : Attribute
        {
            private readonly string name;

            public SubrAttribute()
            {
            }

            public SubrAttribute(string name)
            {
                this.name = name;
            }

            public string Name
            {
                get { return name; }
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class FSubrAttribute : SubrAttribute
        {
            public FSubrAttribute()
            {
            }

            public FSubrAttribute(string name)
                : base(name)
            {
            }
        }

        [ContractAbbreviator]
        private static void SubrContracts(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            //Contract.Requires(args.Length == 0 || Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);
        }

        [ContractAbbreviator]
        private static void SubrContracts(Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
        }
    }
}
