/* Copyright 2010-2016 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Reflection;
using Zilf.Language;

namespace Zilf.Diagnostics
{
    public enum Severity
    {
        Info,
        Warning,
        Error,
    }

    public sealed class Diagnostic
    {
        public ISourceLine Location { get; private set; }
        public Severity Severity { get; private set; }
        public string CodePrefix { get; private set; }
        public int Code { get; private set; }
        public string StackTrace { get; private set; }
        public Diagnostic[] SubDiagnostics { get; private set; }

        string MessageFormat { get; set; }
        object[] MessageArgs { get; set; }

        static readonly object[] NoArguments = new object[0];
        static readonly Diagnostic[] NoDiagnostics = new Diagnostic[0];

        public Diagnostic(ISourceLine location, Severity severity,
            string codePrefix, int code,
            string messageFormat, object[] messageArgs,
            string stackTrace, Diagnostic[] subDiagnostics)
        {
            Contract.Requires(location != null);
            Contract.Requires(codePrefix != null);
            Contract.Requires(code >= 0);
            Contract.Requires(messageFormat != null);

            this.Location = location;
            this.Severity = severity;
            this.CodePrefix = codePrefix;
            this.Code = code;
            this.MessageFormat = messageFormat;
            this.MessageArgs = messageArgs ?? NoArguments;
            this.StackTrace = stackTrace;
            this.SubDiagnostics = subDiagnostics ?? NoDiagnostics;
        }

        public Diagnostic WithSubDiagnostics(params Diagnostic[] newSubDiagnostics)
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

        public string GetFormattedMessage() =>
            string.Format(CustomFormatter.Instance, MessageFormat, MessageArgs);

        public override string ToString()
        {
            return string.Format(
                "{0}: {1} {2}{3:0000}: {4}",
                Location.SourceInfo,
                Severity.ToString().ToLower(),
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

            static char[] Delimiter = { '|' };

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                if (format != null && (format == "s" || format.StartsWith("s|", StringComparison.Ordinal)))
                {
                    bool plural;

                    if (arg is int)
                    {
                        plural = (int)arg != 1;
                    }
                    else if (arg is CountableString)
                    {
                        plural = ((CountableString)arg).Plural;
                    }
                    else if (arg is string)
                    {
                        throw new ArgumentException($"{{#:s}} format requires a {nameof(CountableString)}, not a string");
                    }
                    else
                    {
                        return HandleOther(format, arg);
                    }

                    var parts = format.Split(Delimiter, 3);

                    if (plural)
                        return parts.Length >= 2 ? parts[1] : "s";

                    return parts.Length >= 3 ? parts[2] : "";
                }

                return HandleOther(format, arg);
            }

            static string HandleOther(string format, object arg)
            {
                if (arg is IFormattable)
                    return ((IFormattable)arg).ToString(format, System.Globalization.CultureInfo.CurrentCulture);

                if (arg != null)
                    return arg.ToString();

                return string.Empty;
            }
        }
    }

    [ContractClass(typeof(IDiagnosticFactoryContracts))]
    public interface IDiagnosticFactory
    {
        Diagnostic GetDiagnostic(ISourceLine location, int code, object[] messageArgs,
            string stackTrace = null, Diagnostic[] subDiagnostics = null);
    }

    [ContractClassFor(typeof(IDiagnosticFactory))]
    abstract class IDiagnosticFactoryContracts : IDiagnosticFactory
    {
        public Diagnostic GetDiagnostic(ISourceLine location, int code, object[] messageArgs,
            string stackTrace, Diagnostic[] subDiagnostics)
        {
            Contract.Requires(location != null);
            Contract.Requires(code >= 0);
            Contract.Requires(subDiagnostics == null || Contract.ForAll(subDiagnostics, d => d != null));
            Contract.Ensures(Contract.Result<Diagnostic>() != null);
            return default(Diagnostic);
        }
    }

    public class DiagnosticFactory<TMessageSet> : IDiagnosticFactory
        where TMessageSet : class
    {
        readonly string prefix;
        readonly Dictionary<int, MessageAttribute> messages = new Dictionary<int, MessageAttribute>();

        public static readonly DiagnosticFactory<TMessageSet> Instance = new DiagnosticFactory<TMessageSet>();

        protected DiagnosticFactory()
        {
            var attrs = typeof(TMessageSet).GetCustomAttributes(typeof(MessageSetAttribute), false);
            Contract.Assert(attrs.Length == 1);

            var attr = (MessageSetAttribute)attrs[0];
            prefix = attr.Prefix;

            foreach (var field in typeof(TMessageSet).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(int) && field.IsLiteral)
                {
                    var code = (int)field.GetValue(null);
                    var msgAttrs = field.GetCustomAttributes(typeof(MessageAttribute), false);
                    Contract.Assert(msgAttrs.Length == 1);

                    messages.Add(code, (MessageAttribute)msgAttrs[0]);
                }
            }
        }

        public Diagnostic GetDiagnostic(ISourceLine location, int code, object[] messageArgs,
            string stackTrace = null, Diagnostic[] subDiagnostics = null)
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
    public class MessageSetAttribute : Attribute
    {
        public string Prefix { get; private set; }

        public MessageSetAttribute(string prefix)
        {
            this.Prefix = prefix;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class MessageAttribute : Attribute
    {
        public string Format { get; private set; }
        public Severity Severity { get; set; }

        public MessageAttribute(string format)
        {
            this.Format = format;
            this.Severity = Severity.Error;
        }
    }
}
