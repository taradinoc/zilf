﻿/* Copyright 2010-2018 Jesse McGrew
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Diagnostics
{
    public enum Severity
    {
        Info,
        Warning,
        Error,
        Fatal,
    }

    public sealed class Diagnostic
    {
        [NotNull]
        public ISourceLine Location { get; }
        public Severity Severity { get; }
        [NotNull]
        public string CodePrefix { get; }
        public int Code { get; }
        [CanBeNull]
        public string StackTrace { get; }
        [NotNull]
        public IReadOnlyList<Diagnostic> SubDiagnostics { get; }

        string MessageFormat { get; }
        object[] MessageArgs { get; }

        static readonly object[] NoArguments = new object[0];
        static readonly Diagnostic[] NoDiagnostics = new Diagnostic[0];

        public Diagnostic([NotNull] ISourceLine location, Severity severity,
            [NotNull] string codePrefix, int code,
            [NotNull] string messageFormat, [ItemNotNull] [CanBeNull] object[] messageArgs,
            [CanBeNull] string stackTrace, [ItemNotNull] [CanBeNull] Diagnostic[] subDiagnostics)
        {

            Location = location;
            Severity = severity;
            CodePrefix = codePrefix;
            Code = code;
            MessageFormat = messageFormat;
            MessageArgs = messageArgs ?? NoArguments;
            StackTrace = stackTrace;
            SubDiagnostics = subDiagnostics ?? NoDiagnostics;
        }

        [NotNull]
        public Diagnostic WithSubDiagnostics([ItemNotNull] [NotNull] params Diagnostic[] newSubDiagnostics)
        {
            return new Diagnostic(
                Location,
                Severity,
                CodePrefix,
                Code,
                MessageFormat,
                MessageArgs,
                StackTrace,
                newSubDiagnostics);
        }

        [NotNull]
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public string GetFormattedMessage() =>
            string.Format(CustomFormatter.Instance, MessageFormat, MessageArgs);

        public override string ToString()
        {
            // ReSharper disable once UseStringInterpolation
            return string.Format(
                "{0}: {1} {2}{3:0000}: {4}",
                Location.SourceInfo,
                Severity.ToString().ToLowerInvariant(),
                CodePrefix,
                Code,
                string.Format(CustomFormatter.Instance, MessageFormat, MessageArgs));
        }

        sealed class CustomFormatter : IFormatProvider, ICustomFormatter
        {
            public static readonly CustomFormatter Instance = new CustomFormatter();

            CustomFormatter()
            {
            }

            public object GetFormat(Type formatType)
            {
                if (formatType == typeof(ICustomFormatter))
                    return this;

                return null;
            }

            [NotNull]
            static readonly char[] Delimiter = { '|' };

            /// <exception cref="ArgumentException">The "s" format was used with a <see cref="string"/> instead of a <see cref="CountableString"/>.</exception>
            public string Format([CanBeNull] string format, [CanBeNull] object arg, [CanBeNull] IFormatProvider formatProvider)
            {
                if (format != null && (format == "s" || format.StartsWith("s|", StringComparison.Ordinal)))
                {
                    bool plural;

                    switch (arg)
                    {
                        case int i:
                            plural = i != 1;
                            break;

                        case CountableString cs:
                            plural = cs.Plural;
                            break;

                        case string _:
                            throw new ArgumentException($"{{#:s}} format requires a {nameof(CountableString)}, not a string");

                        default:
                            return HandleOther(format, arg);
                    }

                    var parts = format.Split(Delimiter, 3);

                    if (plural)
                        return parts.Length >= 2 ? parts[1] : "s";

                    return parts.Length >= 3 ? parts[2] : "";
                }

                return HandleOther(format, arg);
            }

            [NotNull]
            static string HandleOther([CanBeNull] string format, [CanBeNull] object arg)
            {
                if (arg is IFormattable formattable)
                    return formattable.ToString(format, System.Globalization.CultureInfo.CurrentCulture);

                return arg?.ToString() ?? string.Empty;
            }
        }
    }
    public interface IDiagnosticFactory
    {
        [NotNull]
        Diagnostic GetDiagnostic([NotNull] ISourceLine location, int code, object[] messageArgs,
            string stackTrace, Diagnostic[] subDiagnostics);
    }

    public static class DiagnosticFactoryExtensions
    {
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [NotNull]
        public static Diagnostic GetDiagnostic([NotNull] this IDiagnosticFactory fac, [NotNull] ISourceLine location, int code, object[] messageArgs)
        {
            return fac.GetDiagnostic(location, code, messageArgs, null, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [NotNull]
        public static Diagnostic GetDiagnostic([NotNull] this IDiagnosticFactory fac, [NotNull] ISourceLine location, int code, object[] messageArgs, string stackTrace)
        {
            return fac.GetDiagnostic(location, code, messageArgs, stackTrace, null);
        }
    }

    public class DiagnosticFactory<TMessageSet> : IDiagnosticFactory
        where TMessageSet : class
    {
        readonly string prefix;
        readonly Dictionary<int, MessageAttribute> messages = new Dictionary<int, MessageAttribute>();

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static readonly DiagnosticFactory<TMessageSet> Instance = new DiagnosticFactory<TMessageSet>();

        protected DiagnosticFactory()
        {
            var attrs = typeof(TMessageSet).GetCustomAttributes(typeof(MessageSetAttribute), false);

            var attr = (MessageSetAttribute)attrs[0];
            prefix = attr.Prefix;

            foreach (var field in typeof(TMessageSet).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(int) && field.IsLiteral)
                {
                    var code = (int)field.GetValue(null);
                    var msgAttrs = field.GetCustomAttributes(typeof(MessageAttribute), false);

                    messages.Add(code, (MessageAttribute)msgAttrs[0]);
                }
            }
        }

        public Diagnostic GetDiagnostic(ISourceLine location, int code, object[] messageArgs,
            string stackTrace, Diagnostic[] subDiagnostics)
        {
            var attr = messages[code];
            return new Diagnostic(
                location,
                attr.Severity,
                prefix,
                code,
                attr.Format,
                messageArgs,
                stackTrace,
                subDiagnostics);
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessageSetAttribute : Attribute
    {
        public string Prefix { get; }

        public MessageSetAttribute(string prefix)
        {
            Prefix = prefix;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MessageAttribute : Attribute
    {
        public string Format { get; }
        public Severity Severity { get; set; }

        public MessageAttribute(string format)
        {
            Format = format;
            Severity = Severity.Error;
        }
    }
}
