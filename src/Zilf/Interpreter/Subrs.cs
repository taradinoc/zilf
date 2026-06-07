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
using System.Diagnostics.CodeAnalysis;
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

        /// <summary>
        /// Indicates that a method implements a SUBR.
        /// </summary>
        public sealed class SubrAttribute : SubrAttributeBase
        {
            public SubrAttribute()
                : base(null)
            {
            }

            /// <param name="name">The name of the implemented SUBR. Defaults to the name of the method.</param>
            public SubrAttribute(string name)
                : base(name)
            {
            }
        }

        /// <summary>
        /// Indicates that a method implements an FSUBR, and its arguments should
        /// not be evaluated before calling.
        /// </summary>
        public sealed class FSubrAttribute : SubrAttributeBase
        {
            public FSubrAttribute()
                : base(null)
            {
            }

            /// <param name="name">The name of the implemented FSUBR. Defaults to the name of the method.</param>
            public FSubrAttribute(string name)
                : base(name)
            {
            }
        }

        /// <summary>
        /// Indicates that calls to a SUBR should be redirected to another implementation
        /// when the current file is in <c>MDL-ZIL?</c> mode.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class MdlZilRedirectAttribute : Attribute
        {
            /// <param name="type">The type providing the other implementation.</param>
            /// <param name="target">The name of the method providing the other implementation.</param>
            public MdlZilRedirectAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type, string target)
            {
                Type = type;
                Target = target;
            }

            [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            public Type Type { get; }

            public string Target { get; }

            /// <summary>
            /// Gets or sets a value indicating whether the redirect should only be performed when
            /// <see cref="Context.AtTopLevel"/> is true.
            /// </summary>
            public bool TopLevelOnly { get; set; }
        }
    }
}
