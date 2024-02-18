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
using System.Collections.Generic;
using System.Runtime.Serialization;
using Zilf.Diagnostics;

namespace Zilf.Language
{
    sealed class CompilerError : ZilError<CompilerMessages>
    {
        public CompilerError(int code)
            : this(code, null)
        {
        }

        public CompilerError(int code, params object[]? messageArgs)
            : this(DiagnosticContext.Current.SourceLine, code, messageArgs)
        {
        }

        public CompilerError(ISourceLine? sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public CompilerError(ISourceLine? sourceLine, int code, params object[]? messageArgs)
            : base(MakeDiagnostic(sourceLine, code, messageArgs))
        {
        }

        public CompilerError(IProvideSourceLine sourceLine, int code)
           : this(sourceLine, code, null)
        {
        }

        public CompilerError(IProvideSourceLine node, int code, params object[]? messageArgs)
            : base(MakeDiagnostic(node.SourceLine, code, messageArgs))
        {
        }

        public CompilerError(Diagnostic diagnostic)
            : base(diagnostic)
        {
        }

        public static CompilerError WrongArgCount(string name, IEnumerable<ArgCountRange> ranges,
            int? acceptableVersion = null)
        {
            ArgCountHelpers.FormatArgCount(ranges, out var cs);
            return WrongArgCount(name, cs, acceptableVersion);
        }

        public static CompilerError WrongArgCount(string name, ArgCountRange range,
            int? acceptableVersion = null)
        {
            ArgCountHelpers.FormatArgCount(range, out var cs);
            return WrongArgCount(name, cs, acceptableVersion);
        }

        static CompilerError WrongArgCount(string name, CountableString cs, int? acceptableVersion)
        {
            var error = new CompilerError(CompilerMessages._0_Requires_1_Argument1s, name, cs);

            if (acceptableVersion != null)
            {
                var info = new CompilerError(
                    CompilerMessages.This_Would_Be_Legal_In_Other_Zmachine_Versions_Eg_V0,
                    acceptableVersion);
                error = error.Combine(info);
            }

            return error;
        }

        public CompilerError()
        {
        }

        public CompilerError(string message) : base(message)
        {
        }

        public CompilerError(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CompilerError(ISourceLine src, string message) : base(src, message)
        {
        }
    }
}