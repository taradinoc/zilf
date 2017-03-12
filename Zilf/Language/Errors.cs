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
using System.Diagnostics.Contracts;
using Zilf.Interpreter;

namespace Zilf.Language
{
    static class Errors
    {
        // TerpWarning: emit an interpreter warning message but don't stop execution
        [Obsolete("Use diagnostic codes instead.")]
        public static void TerpWarning(Context ctx, ISourceLine node, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(message != null);

            ctx.HandleWarning(node, message);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void TerpWarning(Context ctx, ISourceLine node, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            TerpWarning(ctx, node, string.Format(format, args));
        }

        // CompWarning: emit a compiler warning message but don't stop compilation
        [Obsolete("Use diagnostic codes instead.")]
        public static void CompWarning(Context ctx, ISourceLine node, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(message!= null);

            ctx.HandleWarning(node, message);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompWarning(Context ctx, IProvideSourceLine node, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(node != null);
            Contract.Requires(message != null);

            CompWarning(ctx, node.SourceLine, message);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompWarning(Context ctx, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(message != null);

            CompWarning(ctx, (ISourceLine)null, message);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompWarning(Context ctx, ISourceLine node, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            CompWarning(ctx, node, string.Format(format, args));
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompWarning(Context ctx, IProvideSourceLine node, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(node != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            CompWarning(ctx, node.SourceLine, format, args);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompWarning(Context ctx, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            CompWarning(ctx, (ISourceLine)null, format, args);
        }

        // CompError: emit a compiler error message but don't stop compilation
        [Obsolete("Use diagnostic codes instead.")]
        public static void CompError(Context ctx, ISourceLine node, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(message != null);

            ctx.HandleError(new CompilerError(node, message));
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompError(Context ctx, IProvideSourceLine node, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(node != null);
            Contract.Requires(message != null);

            CompError(ctx, node.SourceLine, message);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompError(Context ctx, string message)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(message != null);

            CompError(ctx, (ISourceLine)null, message);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompError(Context ctx, ISourceLine node, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            CompError(ctx, node, string.Format(format, args));
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompError(Context ctx, IProvideSourceLine node, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(node != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            CompError(ctx, node.SourceLine, format, args);
        }

        [Obsolete("Use diagnostic codes instead.")]
        public static void CompError(Context ctx, string format, params object[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            CompError(ctx, (ISourceLine)null, format, args);
        }
    }
}
