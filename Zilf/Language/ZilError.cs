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
using JetBrains.Annotations;

namespace Zilf.Language
{
    [Serializable]
    public abstract class ZilError : Exception
    {
        protected ZilError(string message) : base(message) { }
        protected ZilError(string message, Exception innerException) : base(message, innerException) { }

        protected ZilError([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Contract.Requires(info != null);
        }

        public Diagnostic Diagnostic { get; protected set; }
        public ISourceLine SourceLine { get; protected set; }
    }

    static class ZilErrorExtensions
    {
        public static T Combine<T>([NotNull] this T mainError, [NotNull] T subError)
            where T : ZilError
        {
            Contract.Assert(mainError != null);
            Contract.Assert(subError != null);

            var newDiag = mainError.Diagnostic.WithSubDiagnostics(subError.Diagnostic);
            return (T)Activator.CreateInstance(typeof(T), newDiag);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    abstract class ZilError<TMessageSet> : ZilError
        where TMessageSet : class
    {
        protected ZilError([NotNull] string message)
            : base(message)
        {
            Contract.Requires(message != null);
            SourceLine = DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError([NotNull] string message, Exception innerException)
            : base(message, innerException)
        {
            Contract.Requires(message != null);
            SourceLine = DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError([CanBeNull] ISourceLine src, [NotNull] string message)
            : base(message)
        {
            Contract.Requires(message != null);
            SourceLine = src ?? DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError([NotNull] Diagnostic diag)
            : base(diag.ToString())
        {
            Contract.Requires(diag != null);
            Diagnostic = diag;
            SourceLine = diag.Location;
        }

        protected ZilError([NotNull] SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
            Contract.Requires(si != null);
        }

        [NotNull]
        public string SourcePrefix => SourceLine?.SourceInfo == null ? "" : SourceLine.SourceInfo + ": ";

#pragma warning disable RECS0108 // Warns about static fields in generic types
        protected static readonly IDiagnosticFactory DiagnosticFactory = DiagnosticFactory<TMessageSet>.Instance;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        protected const int LegacyErrorCode = 0;

        [NotNull]
        protected static Diagnostic MakeDiagnostic([CanBeNull] ISourceLine sourceLine, int code, [ItemNotNull] [CanBeNull] object[] messageArgs = null)
        {
            Contract.Requires(code >= 0);
            Contract.Ensures(Contract.Result<Diagnostic>() != null);

            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                code,
                messageArgs,
                MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        [NotNull]
        protected static Diagnostic MakeLegacyDiagnostic([NotNull] ISourceLine sourceLine, [NotNull] string message)
        {
            Contract.Requires(message != null);
            Contract.Requires(sourceLine != null);
            Contract.Ensures(Contract.Result<Diagnostic>() != null);

            return DiagnosticFactory.GetDiagnostic(
                sourceLine,
                LegacyErrorCode,
                new object[] { message },
                MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        [CanBeNull]
        [ContractAnnotation("notnull => notnull; null => null")]
        static string MakeStackTrace([CanBeNull] Frame errorFrame)
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

                sb.AppendFormat("  {0}at {1}", caller, frame.SourceLine.SourceInfo);
            }

            return sb.ToString();
        }
    }
}