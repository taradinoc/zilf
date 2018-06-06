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

namespace Zilf.Interpreter
{
    /// <summary>
    /// Specifies that a class implements a ZILF builtin primtype.
    /// </summary>
    /// <remarks>
    /// This allows the class to be used as a SUBR parameter with <see cref="ArgDecoder"/>,
    /// generating a primtype constraint.
    /// </remarks>
    /// <seealso cref="BuiltinTypeAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    sealed class BuiltinPrimTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new BuiltinPrimTypeAttribute with the specified primitive type.
        /// </summary>
        /// <param name="primType">The primitive type on which the type is based.</param>
        public BuiltinPrimTypeAttribute(PrimType primType)
        {
            PrimType = primType;
        }

        public PrimType PrimType { get; }
    }
}
