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
using System.Diagnostics.Contracts;

namespace Zilf.Language
{
    class InterpreterError : ZilError
    {
        public InterpreterError(string message)
            : base(message)
        {
            Contract.Requires(message != null);
        }

        public InterpreterError(string message, Exception innerException)
            : base(message, innerException)
        {
            Contract.Requires(message != null);
        }

        public InterpreterError(ISourceLine src, string message)
            : base(src, message)
        {
            Contract.Requires(message != null);
        }

        public InterpreterError(IProvideSourceLine node, string message)
            : base(node.SourceLine, message)
        {
            Contract.Requires(node != null);
        }

        public InterpreterError(ISourceLine src, string func, int minArgs, int maxArgs)
            : base(src, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }

        public InterpreterError(IProvideSourceLine node, string func, int minArgs, int maxArgs)
            : base(node.SourceLine, func, minArgs, maxArgs)
        {
            Contract.Requires(node != null);
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }

        public InterpreterError(string func, int minArgs, int maxArgs)
            : base(null, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }
    }
}