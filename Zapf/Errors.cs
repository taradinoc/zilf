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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using System.Diagnostics.Contracts;

namespace Zapf
{
    [ContractClass(typeof(SourceLineContract))]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    interface ISourceLine
    {
        [CanBeNull]
        string SourceFile { get; }
        int LineNum { get; }
    }

    interface IErrorSink
    {
        void HandleSeriousError(SeriousError ser);
        void HandleWarning(Warning warning);
    }

    /// <summary>
    /// Thrown when a configuration change has occurred that
    /// requires the assembler to restart.
    /// </summary>
    [Serializable]
    public class RestartException : Exception
    {
    }

    [Serializable]
    abstract class AssemblerError : Exception
    {
        protected AssemblerError(ISourceLine node, string message)
            : base(message)
        {
            Node = node;
        }

        protected AssemblerError([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Contract.Requires(info != null);
            Node = (ISourceLine)info.GetValue("node", typeof(ISourceLine));
        }

        public ISourceLine Node { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (Node != null)
            {
                info.AddValue("node", Node);
            }
        }
    }

    /// <summary>
    /// Thrown when an unrecoverable error occurs.
    /// </summary>
    [Serializable]
    class FatalError : AssemblerError
    {
        public FatalError(ISourceLine node, string message)
            : base(node, message)
        {
        }
    }

    /// <summary>
    /// Thrown when a serious (but not totally unrecoverable) error occurs.
    /// </summary>
    [Serializable]
    class SeriousError : AssemblerError
    {
        public SeriousError(ISourceLine node, string message)
            : base(node, message)
        {
        }
    }

    /// <summary>
    /// Not thrown, but passed around to report warnings.
    /// </summary>
    [Serializable]
    class Warning : AssemblerError
    {
        public Warning(ISourceLine node, string message)
            : base(node, message)
        {
        }
    }

    static class Errors
    {
        public static void Warn([NotNull] IErrorSink sink, [CanBeNull] ISourceLine node, [NotNull] string message)
        {
            Contract.Requires(sink != null);
            Contract.Requires(message != null);
            sink.HandleWarning(new Warning(node, message));
        }

        [StringFormatMethod("format")]
        public static void Warn([NotNull] IErrorSink sink, [CanBeNull] ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(sink != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            Warn(sink, node, string.Format(format, args));
        }

        public static void Serious([NotNull] IErrorSink sink, [NotNull] string message)
        {
            Contract.Requires(sink != null);
            Contract.Requires(message != null);
            Serious(sink, null, message);
        }

        [StringFormatMethod("format")]
        public static void Serious([NotNull] IErrorSink sink, [NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(sink != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            Serious(sink, string.Format(format, args));
        }

        public static void Serious([NotNull] IErrorSink sink, ISourceLine node, [NotNull] string message)
        {
            Contract.Requires(sink != null);
            Contract.Requires(message != null);
            sink.HandleSeriousError(new SeriousError(node, message));
        }

        [StringFormatMethod("format")]
        public static void Serious([NotNull] IErrorSink sink, ISourceLine node, [NotNull] string format,
            [NotNull] params object[] args)
        {
            Contract.Requires(sink != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            Serious(sink, node, string.Format(format, args));
        }

        [NotNull]
        public static SeriousError MakeSerious(ISourceLine node, [NotNull] string message)
        {
            Contract.Requires(message != null);
            Contract.Ensures(Contract.Result<SeriousError>() != null);
            return new SeriousError(node, message);
        }

        [NotNull]
        [StringFormatMethod("format")]
        public static SeriousError MakeSerious(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<SeriousError>() != null);
            return MakeSerious(node, string.Format(format, args));
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowSerious([NotNull] string message)
        {
            Contract.Requires(message != null);
            ThrowSerious(null, message);
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowSerious([NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            ThrowSerious(null, format, args);
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowSerious(ISourceLine node, [NotNull] string message)
        {
            Contract.Requires(message != null);
            throw MakeSerious(node, message);
        }

        /// <exception cref="SeriousError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowSerious(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            throw MakeSerious(node, format, args);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowFatal([NotNull] string message)
        {
            Contract.Requires(message != null);
            ThrowFatal(null, message);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowFatal([NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            ThrowFatal(null, format, args);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        public static void ThrowFatal(ISourceLine node, [NotNull] string message)
        {
            Contract.Requires(message != null);
            throw new FatalError(node, message);
        }

        /// <exception cref="FatalError">Always thrown.</exception>
        [ContractAnnotation("=> halt")]
        [StringFormatMethod("format")]
        public static void ThrowFatal(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            ThrowFatal(node, string.Format(format, args));
        }
    }

    [ContractClassFor(typeof(ISourceLine))]
    abstract class SourceLineContract : ISourceLine
    {
        public abstract string SourceFile { get; }
        public abstract int LineNum { get; }
    }
}