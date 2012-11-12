﻿/* Copyright 2010, 2012 Jesse McGrew
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
using System.Linq;
using System.Text;

namespace Zilf
{
    /// <summary>
    /// Provides properties for describing the source of an error.
    /// </summary>
    interface ISourceLine
    {
        string SourceInfo { get; }
    }

    class StringSourceLine : ISourceLine
    {
        private readonly string info;

        public StringSourceLine(string info)
        {
            this.info = info;
        }

        public string SourceInfo
        {
            get { return info; }
        }
    }

    abstract class ZilError : Exception
    {
        private ISourceLine src;

        public ZilError(string message)
            : base(message)
        {
        }

        public ZilError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ZilError(ISourceLine src, string message)
            : base(message)
        {
            this.src = src;
        }

        public ZilError(ISourceLine src, string func, int minArgs, int maxArgs)
            : this(src, ArgCountMsg(func, minArgs, maxArgs))
        {
        }

        public ISourceLine SourceLine
        {
            get { return src; }
            set { src = value; }
        }

        public string SourcePrefix
        {
            get
            {
                if (src == null || src.SourceInfo == null)
                    return "";
                else
                    return src.SourceInfo + ": ";
            }
        }

        public static string ArgCountMsg(string func, int min, int max)
        {
            if (min == max)
                return string.Format("{0}: expected {1} arg{2}", func, min, min == 1 ? "" : "s");
            else if (min == 0)
                return string.Format("{0}: expected at most {1} arg{2}", func, max, max == 1 ? "" : "s");
            else if (max == 0)
                return string.Format("{0}: expected at least {1} arg{2}", func, min, min == 1 ? "" : "s");
            else
                return string.Format("{0}: expected {1} to {2} args", func, min, max);
        }
    }

    class InterpreterError : ZilError
    {
        public InterpreterError(string message)
            : base(message)
        {
        }

        public InterpreterError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InterpreterError(ISourceLine src, string message)
            : base(src, message)
        {
        }

        public InterpreterError(ISourceLine src, string func, int minArgs, int maxArgs)
            : base(src, func, minArgs, maxArgs)
        {
        }
    }

    class CompilerError : ZilError
    {
        public CompilerError(string message)
            : base(message)
        {
        }

        public CompilerError(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }

        public CompilerError(ISourceLine src, string message)
            : base(src, message)
        {
        }

        public CompilerError(ISourceLine src, string func, int minArgs, int maxArgs)
            : base(src, func, minArgs, maxArgs)
        {
        }
    }

    static class Errors
    {
        // TerpWarning: emit an interpreter warning message but don't stop execution
        public static void TerpWarning(Context ctx, ISourceLine node, string message)
        {
            ctx.HandleWarning(node, message, false);
        }

        public static void TerpWarning(Context ctx, ISourceLine node, string format, params object[] args)
        {
            TerpWarning(ctx, node, string.Format(format, args));
        }

        // CompWarning: emit a compiler warning message but don't stop compilation
        public static void CompWarning(Context ctx, ISourceLine node, string message)
        {
            ctx.HandleWarning(node, message, true);
        }

        public static void CompWarning(Context ctx, ISourceLine node, string format, params object[] args)
        {
            CompWarning(ctx, node, string.Format(format, args));
        }

        // CompError: emit a compiler error message but don't stop compilation
        public static void CompError(Context ctx, ISourceLine node, string message)
        {
            ctx.HandleError(new CompilerError(node, message));
        }

        public static void CompError(Context ctx, ISourceLine node, string format, params object[] args)
        {
            CompError(ctx, node, string.Format(format, args));
        }
    }
}
