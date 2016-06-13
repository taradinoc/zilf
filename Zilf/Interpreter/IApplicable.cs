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
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Provides a method to apply a <see cref="ZilObject"/>, such as a SUBR or FUNCTION,
    /// to a set of arguments.
    /// </summary>
    [ContractClass(typeof(IApplicableContracts))]
    interface IApplicable
    {
        /// <summary>
        /// Applies the object to the given arguments, after evaluating them and/or expanding segments if appropriate.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="args">The unevaluated arguments.</param>
        /// <returns>The result of the application.</returns>
        ZilObject Apply(Context ctx, ZilObject[] args);

        /// <summary>
        /// Applies the object to the given arguments, without evaluating or expanding them.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The result of the application.</returns>
        ZilObject ApplyNoEval(Context ctx, ZilObject[] args);
    }
}