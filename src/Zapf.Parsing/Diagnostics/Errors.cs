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

using JetBrains.Annotations;

namespace Zapf.Parsing.Diagnostics
{
    public static class Errors
    {
        public static void Warn([NotNull] IErrorSink sink, [CanBeNull] ISourceLine node, [NotNull] string message)
        {
            sink.HandleWarning(new Warning(node, message));
        }

        [StringFormatMethod("format")]
        public static void Warn([NotNull] IErrorSink sink, [CanBeNull] ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            Warn(sink, node, string.Format(format, args));
        }

        public static void Serious([NotNull] IErrorSink sink, [NotNull] string message)
        {
            Serious(sink, null, message);
        }

        [StringFormatMethod("format")]
        public static void Serious([NotNull] IErrorSink sink, [NotNull] string format, [NotNull] params object[] args)
        {
            Serious(sink, string.Format(format, args));
        }

        public static void Serious([NotNull] IErrorSink sink, ISourceLine node, [NotNull] string message)
        {
            sink.HandleSeriousError(new SeriousError(node, message));
        }

        [StringFormatMethod("format")]
        public static void Serious([NotNull] IErrorSink sink, ISourceLine node, [NotNull] string format,
            [NotNull] params object[] args)
        {
            Serious(sink, node, string.Format(format, args));
        }

        [NotNull]
        public static SeriousError MakeSerious(ISourceLine node, [NotNull] string message)
        {
            return new SeriousError(node, message);
        }

        [NotNull]
        [StringFormatMethod("format")]
        public static SeriousError MakeSerious(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            return MakeSerious(node, string.Format(format, args));
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowSerious([NotNull] string message)
        {
            ThrowSerious(null, message);
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowSerious([NotNull] string format, [NotNull] params object[] args)
        {
            ThrowSerious(null, format, args);
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowSerious(ISourceLine node, [NotNull] string message)
        {
            throw MakeSerious(node, message);
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowSerious(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            throw MakeSerious(node, format, args);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowFatal([NotNull] string message)
        {
            ThrowFatal(null, message);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowFatal([NotNull] string format, [NotNull] params object[] args)
        {
            ThrowFatal(null, format, args);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowFatal(ISourceLine node, [NotNull] string message)
        {
            throw new FatalError(node, message);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowFatal(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            ThrowFatal(node, string.Format(format, args));
        }
    }
}