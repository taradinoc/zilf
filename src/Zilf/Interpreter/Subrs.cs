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

using System;
using Zilf.Interpreter.Values;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    delegate ZilResult SubrDelegate(string name, Context ctx, ZilObject[] args);

    static partial class Subrs
    {
        [MeansImplicitUse]
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public abstract class SubrAttributeBase : Attribute
        {
            protected SubrAttributeBase([CanBeNull] string name)
            {
                Name = name;
            }

            [CanBeNull]
            public string Name { get; }

            [CanBeNull]
            public string ObList { get; set; }
        }

        public sealed class SubrAttribute : SubrAttributeBase
        {
            public SubrAttribute()
                : base(null)
            {
            }

            public SubrAttribute([NotNull] string name)
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

            public FSubrAttribute([NotNull] string name)
                : base(name)
            {
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class MdlZilRedirectAttribute : Attribute
        {
            public MdlZilRedirectAttribute([NotNull] Type type, [NotNull] string target)
            {
                Type = type;
                Target = target;
            }

            [NotNull]
            public Type Type { get; }

            [NotNull]
            public string Target { get; }

            public bool TopLevelOnly { get; set; }
        }
    }
}
