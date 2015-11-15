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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Specifies that a class provides an alternate implementation of a ZILF builtin type which
    /// is implemented by another class.
    /// </summary>
    /// <seealso cref="BuiltinTypeAttribute"/>
    [AttributeUsage(AttributeTargets.Class)]
    sealed class BuiltinAlternateAttribute : Attribute
    {
        public BuiltinAlternateAttribute(Type mainClass)
        {
            Contract.Requires(mainClass != null && typeof(ZilObject).IsAssignableFrom(mainClass));

            this.MainType = mainClass;
        }

        public Type MainType { get; private set; }
    }
}
