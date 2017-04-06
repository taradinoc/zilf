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
            public SubrAttribute()
            {
            }

            public SubrAttribute(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public sealed class FSubrAttribute : SubrAttribute
        {
            public FSubrAttribute()
            {
            }

            public FSubrAttribute(string name)
                : base(name)
            {
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class MdlZilRedirectAttribute : Attribute
        {
            public MdlZilRedirectAttribute(Type type, string target)
            {
                Contract.Requires(type != null);
                Contract.Requires(!string.IsNullOrWhiteSpace(target));

                this.Type = type;
                this.Target = target;
            }

            public Type Type { get; }
            public string Target { get; }

            public bool TopLevelOnly { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "ctx")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        [ContractAbbreviator]
        static void SubrContracts(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            //Contract.Requires(args.Length == 0 || Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "ctx")]
        [ContractAbbreviator]
        static void SubrContracts(Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
        }
    }
}
