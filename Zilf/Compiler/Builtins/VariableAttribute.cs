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

namespace Zilf.Compiler.Builtins
{
    /// <summary>
    /// Indicates that the parameter will be used as a variable index.
    /// The caller may pass an atom, which will be interpreted as the name of
    /// a variable, and its index will be used instead of its value.
    /// </summary>
    /// <seealso cref="IVariable.Indirect"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariableAttribute : Attribute
    {
        /// <summary>
        /// If true, then even a reference to a variable's value via
        /// &lt;LVAL X&gt; or &lt;GVAL X&gt; (or .X or ,X) will be interpreted
        /// as referring to its index. Use &lt;VALUE X&gt; to force the
        /// value to be used.
        /// </summary>
        public QuirksMode QuirksMode { get; set; }
    }
}