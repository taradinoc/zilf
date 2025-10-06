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
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Interpreter
{
    abstract class ArgumentDecodingError : InterpreterError
    {
        protected ArgumentDecodingError(Diagnostic diagnostic)
            : base(diagnostic) { }

        protected ArgumentDecodingError()
        {
        }

        protected ArgumentDecodingError(int code) : base(code)
        {
        }

        protected ArgumentDecodingError(int code, params object[] messageArgs) : base(code, messageArgs)
        {
        }

        protected ArgumentDecodingError(ISourceLine sourceLine, int code) : base(sourceLine, code)
        {
        }

        protected ArgumentDecodingError(ISourceLine sourceLine, int code, params object[] messageArgs) : base(sourceLine, code, messageArgs)
        {
        }

        protected ArgumentDecodingError(IProvideSourceLine sourceLine, int code) : base(sourceLine, code)
        {
        }

        protected ArgumentDecodingError(IProvideSourceLine node, int code, params object[] messageArgs) : base(node, code, messageArgs)
        {
        }

        protected ArgumentDecodingError(string message) : base(message)
        {
        }

        protected ArgumentDecodingError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ArgumentDecodingError(ISourceLine src, string message) : base(src, message)
        {
        }
    }

    abstract class CallSite(string name)
    {
        protected string Name => name;

        public abstract string ChildName { get; }

        public sealed override string ToString() => name;

        public string DescribeArgument(int childIndex)
        {
            var suffix = $": {ChildName} {childIndex + 1}";
            return name + suffix;
        }
    }

    sealed class FunctionCallSite(string name) : CallSite(name)
    {
        public override string ChildName => "arg";
    }

    sealed class StructuredArgumentCallSite(CallSite parent, int argIndex) : CallSite(parent.DescribeArgument(argIndex))
    {
        public override string ChildName => "element";
    }

    sealed class ArgumentCountError : ArgumentDecodingError
    {
        ArgumentCountError(Diagnostic diagnostic)
            : base(diagnostic)
        {
        }

        public static ArgumentCountError WrongCount(CallSite site, int lowerBound, int? upperBound, bool morePrefix = false)
        {
            const int PlainMessageCode = InterpreterMessages._0_Requires_1_21s;
            const int MessageCodeWithMore = InterpreterMessages._0_Requires_1_Additional_21s;

            var range = new ArgCountRange(lowerBound, upperBound);
            ArgCountHelpers.FormatArgCount(range, out var cs);

            var diag = DiagnosticFactory<InterpreterMessages>.Instance.GetDiagnostic(
                DiagnosticContext.Current.SourceLine,
                morePrefix ? MessageCodeWithMore : PlainMessageCode,
                [site.ToString(), cs, site.ChildName],
                null);

            return new ArgumentCountError(diag);
        }

        public static ArgumentCountError TooMany(CallSite site, int firstUnexpectedIndex, int? suspiciousTypeIndex)
        {
            Diagnostic? info;
            var sourceLine = DiagnosticContext.Current.SourceLine;

            if (suspiciousTypeIndex != null)
            {
                info = DiagnosticFactory<InterpreterMessages>.Instance.GetDiagnostic(
                    sourceLine,
                    InterpreterMessages.Check_Types_Of_Earlier_0s_Eg_0_1,
                    [site.ChildName, suspiciousTypeIndex]);
            }
            else
            {
                info = null;
            }

            var diag = DiagnosticFactory<InterpreterMessages>.Instance.GetDiagnostic(
                sourceLine,
                InterpreterMessages._0_Too_Many_1s_Starting_At_1_2,
                [site.ToString(), site.ChildName, firstUnexpectedIndex],
                null,
                info != null ? [info] : null);

            return new ArgumentCountError(diag);
        }

        public ArgumentCountError(string message, Exception innerException) : base(message, innerException)
        {
        }

        private ArgumentCountError() : base()
        {
        }

        public ArgumentCountError(string message) : base(message)
        {
        }
    }

    sealed class ArgumentTypeError : ArgumentDecodingError
    {
        public ArgumentTypeError(CallSite site, int index, string constraintDesc)
            : base(MakeDiagnostic(
                null,
                InterpreterMessages._0_Expected_1,
                [site.DescribeArgument(index), constraintDesc]))
        {
        }

        public ArgumentTypeError(string message) : base(message)
        {
        }

        private ArgumentTypeError() : base()
        {
        }

        public ArgumentTypeError(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class DeclAttribute(string pattern) : Attribute
    {
        public string Pattern { get; } = pattern;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class RequiredAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct)]
    sealed class ZilStructuredParamAttribute(StdAtom typeAtom) : Attribute
    {
        public StdAtom TypeAtom { get; } = typeAtom;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class ZilOptionalAttribute : Attribute
    {
        public object? Default { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class EitherAttribute(params Type[] types) : Attribute
    {
        public Type[] Types { get; } = types;
        public string? DefaultParamDesc { get; set; }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    sealed class ZilSequenceParamAttribute : Attribute
    {
    }

    [AttributeUsage(
        AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface |
        AttributeTargets.Parameter | AttributeTargets.Field)]
    sealed class ParamDescAttribute(string description) : Attribute
    {
        public string Description { get; } = description;
    }
}
