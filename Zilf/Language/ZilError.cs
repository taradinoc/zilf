/* Copyright 2010-2018 Jesse McGrew
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
using System.Runtime.Serialization;
using System.Text;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using JetBrains.Annotations;

namespace Zilf.Language
{
    [Serializable]
    public abstract class ZilErrorBase : Exception
    {
        protected ZilErrorBase(string message) : base(message) { }
        protected ZilErrorBase(string message, Exception innerException) : base(message, innerException) { }

        protected ZilErrorBase([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Diagnostic", Diagnostic);
            info.AddValue("SourceLine", SourceLine);
        }

        public Diagnostic Diagnostic { get; protected set; }
        protected ISourceLine SourceLine { get; set; }
    }

    [Serializable]
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

        protected ZilError([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public abstract class ZilFatal : ZilErrorBase
    {
        protected ZilFatal(string message)
            : base(message)
        {
        }

        protected ZilFatal([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    static class ZilErrorBaseExtensions
    {
        [NotNull]
        public static T Combine<T>([NotNull] this T mainError, [NotNull] T subError)
            where T : ZilErrorBase
        {
            var newDiag = mainError.Diagnostic.WithSubDiagnostics(subError.Diagnostic);
            return (T)Activator.CreateInstance(typeof(T), newDiag);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    [Serializable]
    abstract class ZilError<TMessageSet> : ZilError
        where TMessageSet : class
    {
        protected ZilError([NotNull] string message)
            : base(message)
        {
            SourceLine = DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError([NotNull] string message, Exception innerException)
            : base(message, innerException)
        {
            SourceLine = DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError([CanBeNull] ISourceLine src, [NotNull] string message)
            : base(message)
        {
            SourceLine = src ?? DiagnosticContext.Current.SourceLine;
            Diagnostic = MakeLegacyDiagnostic(SourceLine, message);
        }

        protected ZilError([NotNull] Diagnostic diag)
            : base(diag.ToString())
        {
            Diagnostic = diag;
            SourceLine = diag.Location;
        }

        protected ZilError([NotNull] SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }

#pragma warning disable RECS0108 // Warns about static fields in generic types
        protected static readonly IDiagnosticFactory DiagnosticFactory = DiagnosticFactory<TMessageSet>.Instance;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        protected const int LegacyErrorCode = 0;

        [NotNull]
        protected static Diagnostic MakeDiagnostic([CanBeNull] ISourceLine sourceLine, int code, [ItemNotNull] [CanBeNull] object[] messageArgs = null)
        {
            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                code,
                messageArgs, MakeStackTrace(DiagnosticContext.Current.Frame));
        }

        [NotNull]
        protected static Diagnostic MakeLegacyDiagnostic([NotNull] ISourceLine sourceLine, [NotNull] string message)
        {
            return DiagnosticFactory.GetDiagnostic(
                sourceLine,
                LegacyErrorCode,
                new object[] { message }, MakeStackTrace(DiagnosticContext.Current.Frame));
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

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    [Serializable]
    abstract class ZilFatal<TMessageSet> : ZilFatal
       where TMessageSet : class
    {
        protected ZilFatal([NotNull] Diagnostic diag)
            : base(diag.ToString())
        {
            Diagnostic = diag;
            SourceLine = diag.Location;
        }

        protected ZilFatal([NotNull] SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }

#pragma warning disable RECS0108 // Warns about static fields in generic types
        protected static readonly IDiagnosticFactory DiagnosticFactory = DiagnosticFactory<TMessageSet>.Instance;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        [NotNull]
        protected static Diagnostic MakeDiagnostic([CanBeNull] ISourceLine sourceLine, int code, [ItemNotNull] [CanBeNull] object[] messageArgs = null)
        {
            return DiagnosticFactory.GetDiagnostic(
                sourceLine ?? DiagnosticContext.Current.SourceLine,
                code,
                messageArgs, MakeStackTrace(DiagnosticContext.Current.Frame));
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