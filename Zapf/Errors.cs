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

namespace Zapf
{
    public interface ISourceLine
    {
        string SourceFile { get; }
        int LineNum { get; }
    }

    public interface IErrorSink
    {
        void HandleFatalError(FatalError fer);
        void HandleSeriousError(SeriousError ser);
    }

    /// <summary>
    /// Thrown when a configuration change has occurred that
    /// requires the assembler to restart.
    /// </summary>
    public class RestartException : Exception
    {
    }

    public class AssemblerError : Exception
    {
        readonly ISourceLine node;

        public AssemblerError(ISourceLine node, string message)
            : base(message)
        {
            this.node = node;
        }

        public ISourceLine Node
        {
            get { return node; }
        }
    }

    /// <summary>
    /// Thrown when an unrecoverable error occurs.
    /// </summary>
    public class FatalError : AssemblerError
    {
        public FatalError(ISourceLine node, string message)
            : base(node, message)
        {
        }
    }

    /// <summary>
    /// Thrown when a serious (but not totally unrecoverable) error occurs.
    /// </summary>
    public class SeriousError : AssemblerError
    {
        public SeriousError(ISourceLine node, string message)
            : base(node, message)
        {
        }
    }

    static class Errors
    {
        public static void Warn(ISourceLine node, string message)
        {
            if (node != null)
                Console.Error.Write("line {0}", node.LineNum);

            Console.Error.WriteLine("warning: {0}", message);
        }

        public static void Warn(ISourceLine node, string format, params object[] args)
        {
            Warn(node, string.Format(format, args));
        }

        public static void Serious(IErrorSink sink, string message)
        {
            Serious(sink, null, message);
        }

        public static void Serious(IErrorSink sink, string format, params object[] args)
        {
            Serious(sink, string.Format(format, args));
        }

        public static void Serious(IErrorSink sink, ISourceLine node, string message)
        {
            sink.HandleSeriousError(new SeriousError(node, message));
        }

        public static void Serious(IErrorSink sink, ISourceLine node, string format, params object[] args)
        {
            Serious(sink, node, string.Format(format, args));
        }

        public static SeriousError MakeSerious(ISourceLine node, string message)
        {
            return new SeriousError(node, message);
        }

        public static SeriousError MakeSerious(ISourceLine node, string format, params object[] args)
        {
            return MakeSerious(node, string.Format(format, args));
        }

        public static void ThrowSerious(string message)
        {
            ThrowSerious(null, message);
        }

        public static void ThrowSerious(string format, params object[] args)
        {
            ThrowSerious(null, format, args);
        }

        public static void ThrowSerious(ISourceLine node, string message)
        {
            throw MakeSerious(node, message);
        }

        public static void ThrowSerious(ISourceLine node, string format, params object[] args)
        {
            throw MakeSerious(node, format, args);
        }

        public static void ThrowFatal(string message)
        {
            ThrowFatal((ISourceLine)null, message);
        }

        public static void ThrowFatal(string format, params object[] args)
        {
            ThrowFatal(null, format, args);
        }

        public static void ThrowFatal(ISourceLine node, string message)
        {
            throw new FatalError(node, message);
        }

        public static void ThrowFatal(ISourceLine node, string format, params object[] args)
        {
            ThrowFatal(node, string.Format(format, args));
        }
    }
}