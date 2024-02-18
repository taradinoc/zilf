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

using System.Diagnostics.CodeAnalysis;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Compiler.Builtins
{
#pragma warning disable IDE1006 // Naming Styles
    /// <summary>
    /// Contains information about a builtin void call that needs to be emitted.
    /// </summary>
    /// <remarks>
    /// This must be the first parameter of the spec method.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    readonly struct VoidCall
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        public Compilation cc { get; }
        /// <summary>
        /// The routine builder.
        /// </summary>
        public IRoutineBuilder rb { get; }
        /// <summary>
        /// The FORM being called.
        /// </summary>
        /// <remarks>
        /// The FORM shouldn't be evaluated directly by the spec method. Its arguments may have gone
        /// through macro expansion, leaving wrapped values that will throw an exception if used without
        /// additional unwrapping, but unwrapping the arguments could potentially repeat side effects.
        /// </remarks>
        public ZilForm form { get; }

        public VoidCall(Compilation cc, IRoutineBuilder rb, ZilForm form)
            : this()
        {
            this.cc = cc;
            this.rb = rb;
            this.form = form;
        }

        public void HandleMessage(int code, params object[] args)
        {
            cc.Context.HandleError(new CompilerError(form, code, args));
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
