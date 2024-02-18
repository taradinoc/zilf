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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Zilf.Diagnostics;
using Zilf.Interpreter;

namespace Zilf.Language
{
    public abstract class ZilErrorBase : Exception
    {
        protected ZilErrorBase()
        { }
        protected ZilErrorBase(string message) : base(message) { }
        protected ZilErrorBase(string message, Exception innerException) : base(message, innerException) { }

        public Diagnostic? Diagnostic { get; protected set; }
        protected ISourceLine? SourceLine { get; set; }
    }

    public abstract class ZilError : ZilErrorBase
    {
        protected ZilError(string message)
            : base(message)
        {
        }

        protected ZilError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ZilError()
        {
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public abstract class ZilFatal : ZilErrorBase
    {
        protected ZilFatal(string message)
            : base(message)
        {
        }

        protected ZilFatal()
        {
        }

        protected ZilFatal(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    static class ZilErrorBaseExtensions
    {
        public static T Combine<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this T mainError, T subError)
            where T : ZilErrorBase
        {
            if (mainError.Diagnostic == null || subError.Diagnostic == null)
                return subError;

            var newDiag = mainError.Diagnostic.WithSubDiagnostics(subError.Diagnostic);
            return (T)Activator.CreateInstance(typeof(T), newDiag)!;
        }
    }

    abstract class ZilError<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TMessageSet> : ZilError
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

        protected ZilError(ISourceLine? src, string message)
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

        protected ZilError() : base()
        {
        }

#pragma warning disable RECS0108 // Warns about static fields in generic types
        protected static readonly IDiagnosticFactory DiagnosticFactory = DiagnosticFactory<TMessageSet>.Instance;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        protected const int LegacyErrorCode = 0;

        protected static Diagnostic MakeDiagnostic(ISourceLine? sourceLine, int code, object[]? messageArgs = null)
        {
            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                code,
                messageArgs, MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        protected static Diagnostic MakeLegacyDiagnostic(ISourceLine sourceLine, string message)
        {
            return DiagnosticFactory.GetDiagnostic(
                sourceLine,
                LegacyErrorCode,
                new object[] { message }, MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        [return: NotNullIfNotNull("errorFrame")]
        static string? MakeStackTrace(Frame? errorFrame)
        {
            if (errorFrame == null)
                return null;

            var sb = new StringBuilder();

            // skip the top and bottom frame
            for (var frame = errorFrame.Parent; frame?.Parent != null; frame = frame.Parent)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                var caller = frame.Description != null
                    ? $"in {frame.Description} called "
                    : "";

                sb.AppendFormat(CultureInfo.InvariantCulture, "  {0}at {1}", caller, frame.SourceLine.SourceInfo);
            }

            return sb.ToString();
        }
    }

    abstract class ZilFatal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TMessageSet> : ZilFatal
        where TMessageSet : class
    {
        protected ZilFatal(Diagnostic diag)
            : base(diag.ToString())
        {
            Diagnostic = diag;
            SourceLine = diag.Location;
        }

        protected ZilFatal(string message) : base(message)
        {
        }

        protected ZilFatal()
        {
        }

        protected ZilFatal(string message, Exception innerException) : base(message, innerException)
        {
        }

#pragma warning disable RECS0108 // Warns about static fields in generic types
        protected static readonly IDiagnosticFactory DiagnosticFactory = DiagnosticFactory<TMessageSet>.Instance;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        protected static Diagnostic MakeDiagnostic(ISourceLine? sourceLine, int code, object[]? messageArgs = null)
        {
            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                code,
                messageArgs, MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        [return: NotNullIfNotNull("errorFrame")]
        static string? MakeStackTrace(Frame? errorFrame)
        {
            if (errorFrame == null)
                return null;

            var sb = new StringBuilder();

            // skip the top and bottom frame
            for (var frame = errorFrame.Parent; frame?.Parent != null; frame = frame.Parent)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                var caller = frame.Description != null
                    ? $"in {frame.Description} called "
                    : "";

                sb.AppendFormat(CultureInfo.InvariantCulture, "  {0}at {1}", caller, frame.SourceLine.SourceInfo);
            }

            return sb.ToString();
        }
    }
}