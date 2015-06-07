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
using Zilf.Language;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Specifies that a class implements a ZILF builtin type.
    /// </summary>
    /// <seealso cref="ChtypeMethodAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    class BuiltinTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new BuiltinTypeAttribute with the specified name and primitive type.
        /// </summary>
        /// <param name="name">The <see cref="StdAtom"/> representing the type name.</param>
        /// <param name="primType">The primitive type on which the type is based.</param>
        /// <remarks>A constructor or static method must be marked with
        /// <see cref="ChtypeMethodAttribute"/>.</remarks>
        public BuiltinTypeAttribute(StdAtom name, PrimType primType)
        {
            this.Name = name;
            this.PrimType = primType;
        }

        public StdAtom Name { get; private set; }
        public PrimType PrimType { get; private set; }
    }

    #region Special Types

    #endregion

    #region Monad Types

    #endregion

    #region Structured Types

    // TODO: ZilList should be sealed; the other list-based types should derive from an abstract base

    #endregion

    #region Applicable Types

    #endregion
}
