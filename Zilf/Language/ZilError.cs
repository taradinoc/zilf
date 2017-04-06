/* Copyright 2010-2017 Jesse McGrew
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
using System.Runtime.Serialization;
using System.Text;
using Zilf.Diagnostics;
using Zilf.Interpreter;

namespace Zilf.Language
{
    [Serializable]
    public abstract class ZilError : Exception
    {
        protected ZilError(string message) : base(message) { }
        protected ZilError(string message, Exception innerException) : base(message, innerException) { }

        protected ZilError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public Diagnostic Diagnostic { get; protected set; }
        public ISourceLine SourceLine { get; protected set; }
    }

    static class ZilErrorExtensions
    {
        public static T Combine<T>(this T mainError, T subError)
            where T : ZilError
        {
            Contract.Assert(mainError != null);
            Contract.Assert(subError != null);

            var newDiag = mainError.Diagnostic.WithSubDiagnostics(subError.Diagnostic);
            return (T)Activator.CreateInstance(typeof(T), newDiag);
        }
    }

    abstract class ZilError<TMessageSet> : ZilError
        where TMessageSet : class
    {
        protected ZilError(string message)
            : base(message)
        {
            SourceLine = DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError(string message, Exception innerException)
            : base(message, innerException)
        {
            SourceLine = DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError(ISourceLine src, string message)
            : base(message)
        {
            SourceLine = src ?? DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError(Diagnostic diag)
            : base(diag.ToString())
        {
            Diagnostic = diag;
            SourceLine = diag.Location;
        }

        protected ZilError(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }

        public string SourcePrefix
        {
            get
            {
                if (SourceLine == null || SourceLine.SourceInfo == null)
                    return "";
                return SourceLine.SourceInfo + ": ";
            }
        }

#pragma warning disable RECS0108 // Warns about static fields in generic types
        protected static readonly IDiagnosticFactory DiagnosticFactory = DiagnosticFactory<TMessageSet>.Instance;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        protected const int LegacyErrorCode = 0;

        protected static Diagnostic MakeDiagnostic(ISourceLine sourceLine, int code, object[] messageArgs = null)
        {
            Contract.Requires(code >= 0);
            Contract.Ensures(Contract.Result<Diagnostic>() != null);

            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                code,
                messageArgs,
                MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        protected static Diagnostic MakeLegacyDiagnostic(ISourceLine sourceLine, string message)
        {
            Contract.Requires(message != null);
            Contract.Requires(sourceLine != null);
            Contract.Ensures(Contract.Result<Diagnostic>() != null);

            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                LegacyErrorCode,
                new[] { message },
                MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        static string MakeStackTrace(Frame errorFrame)
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
                if (frame.Description != null)
                {
                    caller = $"in {frame.Description} called ";
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