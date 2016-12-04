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
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Diagnostics;
using Zilf.Interpreter;

namespace Zilf.Language
{
    class InterpreterError : ZilError
    {
        public Frame Frame { get; }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(string message)
            : base(message)
        {
            Contract.Requires(message != null);

            Frame = DiagnosticContext.Current.Frame;
        }

        public InterpreterError(string message, Exception innerException)
            : base(message, innerException)
        {
            Contract.Requires(message != null);

            Frame = DiagnosticContext.Current.Frame;
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(ISourceLine src, string message)
            : base(src, message)
        {
            Contract.Requires(message != null);

            Frame = DiagnosticContext.Current.Frame;
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(IProvideSourceLine node, string message)
            : base(node.SourceLine, message)
        {
            Contract.Requires(node != null);

            Frame = DiagnosticContext.Current.Frame;
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(ISourceLine src, string func, int minArgs, int maxArgs)
            : base(src, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);

            Frame = DiagnosticContext.Current.Frame;
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(IProvideSourceLine node, string func, int minArgs, int maxArgs)
            : base(node.SourceLine, func, minArgs, maxArgs)
        {
            Contract.Requires(node != null);
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);

            Frame = DiagnosticContext.Current.Frame;
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(string func, int minArgs, int maxArgs)
            : base(null, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);

            Frame = DiagnosticContext.Current.Frame;
        }

        public InterpreterError(int code)
            : this(code, null)
        {
        }

        public InterpreterError(int code, params object[] messageArgs)
            : this(DiagnosticContext.Current.SourceLine, code, messageArgs)
        {
        }

        public InterpreterError(ISourceLine sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public InterpreterError(ISourceLine sourceLine, int code, params object[] messageArgs)
            : base(MakeDiagnostic(sourceLine, code, messageArgs))
        {
        }

        protected static Diagnostic MakeDiagnostic(ISourceLine sourceLine, int code, object[] messageArgs = null)
        {
            return DiagnosticFactory<InterpreterMessages>.Instance.GetDiagnostic(
                sourceLine, code, null, MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        protected override Diagnostic MakeLegacyDiagnostic(string message, ISourceLine location)
        {
            return DiagnosticFactory<InterpreterMessages>.Instance.GetDiagnostic(
                location, InterpreterMessages.LegacyError, new[] { message }, MakeStackTrace(Frame)); 
        }

        private static string MakeStackTrace(Frame errorFrame)
        {
            if (errorFrame == null)
                return null;

            var sb = new StringBuilder();

            // skip the top and bottom frame
            for (var frame = errorFrame.Parent; frame?.Parent != null; frame = frame.Parent)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                string caller;
                if (frame.CallingForm != null)
                {
                    caller = $"in {frame.CallingForm.First.ToString()} called ";
                }
                else
                {
                    caller = "";
                }

                sb.AppendFormat("  {0}at {1}", caller, frame.SourceLine.SourceInfo);
            }

            return sb.ToString();
        }
    }
}