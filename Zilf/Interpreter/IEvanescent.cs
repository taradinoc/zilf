/* Copyright 2010, 2016 Jesse McGrew
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
using Zilf.Language;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Provides a means to check whether a value is still usable.
    /// </summary>
    /// <remarks>
    /// Evanescent values, such as <see cref="ZilEnvironment"/>, have lifespans limited
    /// to the duration of a function call. After the function returns, the value may
    /// become "illegal": it can still be passed around, but can no longer be used for
    /// its main purpose. An evanescent value typically holds a <see cref="WeakReference{T}"/>
    /// to an object that becomes eligible for GC once the function returns.
    /// </remarks>
    interface IEvanescent
    {
        /// <summary>
        /// Indicates whether the value is still usable.
        /// </summary>
        bool IsLegal { get; }
    }
}
