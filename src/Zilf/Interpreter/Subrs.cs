/* Copyright 2010-2023 Tara McGrew
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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    delegate ZilResult SubrDelegate(string name, Context ctx, ZilObject[] args);

    static partial class Subrs
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public abstract class SubrAttributeBase : Attribute
        {
            protected SubrAttributeBase(string? name)
            {
                Name = name;
            }

            public string? Name { get; }

            public string? ObList { get; set; }
        }

        public sealed class SubrAttribute : SubrAttributeBase
        {
            public SubrAttribute()
                : base(null)
            {
            }

            public SubrAttribute(string name)
                : base(name)
            {
            }
        }

        public sealed class FSubrAttribute : SubrAttributeBase
        {
            public FSubrAttribute()
                : base(null)
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
                Type = type;
                Target = target;
            }

            public Type Type { get; }

            public string Target { get; }

            public bool TopLevelOnly { get; set; }
        }
    }
}
