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

using Antlr.Runtime.Tree;
using System;

namespace Zapf
{
    public interface ISourceLine
    {
        string SourceFile { get; }
        int LineNum { get; }
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

        public static void Serious(Context ctx, string message)
        {
            Serious(ctx, null, message);
        }

        public static void Serious(Context ctx, string format, params object[] args)
        {
            Serious(ctx, string.Format(format, args));
        }

        public static void Serious(Context ctx, ISourceLine node, string message)
        {
            ctx.HandleSeriousError(new SeriousError(node, message));
        }

        public static void Serious(Context ctx, ISourceLine node, string format, params object[] args)
        {
            Serious(ctx, node, string.Format(format, args));
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
            throw new SeriousError(node, message);
        }

        public static void ThrowSerious(ISourceLine node, string format, params object[] args)
        {
            ThrowSerious(node, string.Format(format, args));
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

        public static void ThrowFatal(ITree node, string message)
        {
            //XXX
            throw new NotImplementedException();
        }
    }
}