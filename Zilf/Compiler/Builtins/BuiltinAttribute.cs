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
using JetBrains.Annotations;

namespace Zilf.Compiler.Builtins
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Builtin")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments",
        Justification = nameof(Names) + " exposes the values passed in as positional arguments")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public sealed class BuiltinAttribute : Attribute
    {
        public BuiltinAttribute(string name)
            : this(name, null)
        {
        }

        public BuiltinAttribute(string name, params string[] aliases)
        {
            this.name = name;
            this.aliases = aliases;

            MinVersion = 1;
            MaxVersion = 6;

            Priority = 1;
        }

        readonly string name;
        readonly string[] aliases;

        public IEnumerable<string> Names
        {
            get
            {
                yield return name;

                if (aliases != null)
                {
                    foreach (var a in aliases)
                        yield return a;
                }
            }
        }

        public object Data { get; set; }
        public int MinVersion { get; set; }
        public int MaxVersion { get; set; }
        public bool HasSideEffect { get; set; }

        /// <summary>
        /// Gets or sets a priority value used to disambiguate when more than one method matches the call.
        /// Lower values indicate a better match. Defaults to 1.
        /// </summary>
        public int Priority { get; set; }
    }
}