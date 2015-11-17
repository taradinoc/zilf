/* Copyright 2010, 2015 Jesse McGrew
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        public static ZilObject PRINT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("PRINT", 1, 2);

            ZilChannel channel;
            if (args.Length < 2)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRINT: bad OUTCHAN");
            }
            else
            {
                channel = args[1] as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRINT: second arg must be a channel");
            }

            var str = args[0].ToStringContext(ctx, false);

            // TODO: check for I/O error
            channel.WriteNewline();
            channel.WriteString(str);
            channel.WriteChar(' ');

            return args[0];
        }

        [Subr]
        public static ZilObject PRIN1(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("PRIN1", 1, 2);

            ZilChannel channel;
            if (args.Length < 2)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRIN1: bad OUTCHAN");
            }
            else
            {
                channel = args[1] as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRIN1: second arg must be a channel");
            }

            var str = args[0].ToStringContext(ctx, false);

            // TODO: check for I/O error
            channel.WriteString(str);

            return args[0];
        }

        [Subr]
        public static ZilObject PRINC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("PRINC", 1, 2);

            ZilChannel channel;
            if (args.Length < 2)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRINC: bad OUTCHAN");
            }
            else
            {
                channel = args[1] as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRINC: second arg must be a channel");
            }

            var str = args[0].ToStringContext(ctx, true);

            // TODO: check for I/O error
            channel.WriteString(str);

            return args[0];
        }

        [Subr]
        public static ZilObject CRLF(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length > 1)
                throw new InterpreterError("CRLF", 0, 1);

            ZilChannel channel;
            if (args.Length < 1)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("CRLF: bad OUTCHAN");
            }
            else
            {
                channel = args[0] as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("CRLF: arg must be a channel");
            }

            // TODO: check for I/O error
            channel.WriteNewline();

            return ctx.TRUE;
        }

        [Subr("PRINT-MANY")]
        public static ZilObject PRINT_MANY(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("PRINT-MANY", 2, 0);

            var channel = args[0];
            var printer = args[1];

            if (printer is ZilAtom)
            {
                var atom = (ZilAtom)printer;
                printer = ctx.GetGlobalVal(atom) ?? ctx.GetLocalVal(atom);
                if (printer == null)
                    throw new InterpreterError(string.Format(
                        "PRINT-MANY: {0} has no GVAL or LVAL",
                        atom.ToStringContext(ctx, false)));
            }

            var applicablePrinter = printer as IApplicable;
            if (applicablePrinter == null)
                throw new InterpreterError("PRINT-MANY: not applicable: " + printer.ToStringContext(ctx, false));

            var crlf = ctx.GetStdAtom(StdAtom.PRMANY_CRLF);
            var result = ctx.TRUE;

            ctx.PushLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN), channel);
            try
            {
                var noArgs = new ZilObject[0];
                var printArgs = new ZilObject[1];

                for (int i = 2; i < args.Length; i++)
                {
                    result = args[i];

                    if (result == crlf)
                    {
                        CRLF(ctx, noArgs);
                    }
                    else
                    {
                        printArgs[0] = result;
                        applicablePrinter.ApplyNoEval(ctx, printArgs);
                    }
                }
            }
            finally
            {
                ctx.PopLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN));
            }

            return result;
        }

        [Subr]
        public static ZilObject IMAGE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("IMAGE", 1, 2);

            ZilFix ch = args[0] as ZilFix;
            if (ch == null)
                throw new InterpreterError("IMAGE: first arg must be a FIX");

            ZilChannel channel;
            if (args.Length < 2)
            {
                channel = ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("IMAGE: bad OUTCHAN");
            }
            else
            {
                channel = args[1] as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("IMAGE: second arg must be a channel");
            }

            // TODO: check for I/O error
            channel.WriteChar((char)ch.Value);

            return ch;
        }

        private static readonly Regex RetroPathRE = new Regex(@"^(?:(?<device>[^:]+):)?(?:<(?<directory>[^>]+)>)?(?<filename>[^:<>]+)$");

        [Subr]
        public static ZilObject OPEN(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("OPEN", 2, 2);

            var mode = args[0] as ZilString;
            if (mode == null || mode.Text != "READ")
                throw new InterpreterError("OPEN: first arg must be \"READ\"");

            var path = args[1] as ZilString;
            if (path == null)
                throw new InterpreterError("OPEN: second arg must be a STRING");

            var result = new ZilFileChannel(ConvertPath(path.Text), FileAccess.Read);
            result.Reset(ctx);
            return result;
        }

        private static string ConvertPath(string retroPath)
        {
            Contract.Requires(retroPath != null);

            var match = RetroPathRE.Match(retroPath);
            if (match.Success)
                return match.Groups["filename"].Value;
            else
                return retroPath;
        }

        [Subr]
        public static ZilObject CLOSE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("CLOSE", 1, 1);

            var channel = args[0] as ZilChannel;
            if (channel == null)
                throw new InterpreterError("CLOSE: arg must be a CHANNEL");

            channel.Close(ctx);
            return channel;
        }

        [Subr("FILE-LENGTH")]
        public static ZilObject FILE_LENGTH(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("FILE-LENGTH", 1, 1);

            var chan = args[0] as ZilChannel;
            if (chan == null)
                throw new InterpreterError("FILE-LENGTH: arg must be a CHANNEL");

            var length = chan.GetFileLength();
            return length == null ? ctx.FALSE : new ZilFix((int)length.Value);
        }

        [Subr]
        public static ZilObject READSTRING(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // TODO: support 1- and 4-argument forms?
            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError("READSTRING", 2, 3);

            var dest = args[0] as ZilString;
            if (dest == null)
                throw new InterpreterError("READSTRING: first arg must be a STRING");

            var channel = args[1] as ZilChannel;
            if (channel == null)
                throw new InterpreterError("READSTRING: second arg must be a CHANNEL");

            int maxLength = dest.Text.Length;
            ZilString stopChars = null;

            if (args.Length >= 3)
            {
                var maxLengthFix = args[2] as ZilFix;
                stopChars = args[2] as ZilString;

                if (maxLengthFix == null && stopChars == null)
                    throw new InterpreterError("READSTRING: third arg must be a FIX or STRING");

                if (maxLengthFix != null)
                    maxLength = Math.Min(maxLengthFix.Value, maxLength);
            }

            var buffer = new StringBuilder(maxLength);
            bool reading;
            do
            {
                reading = false;
                if (buffer.Length < maxLength)
                {
                    char? c = channel.ReadChar();
                    if (c != null &&
                        (stopChars == null || stopChars.Text.IndexOf(c.Value) < 0))
                    {
                        buffer.Append(c.Value);
                        reading = true;
                    }
                }
            } while (reading);

            var readCount = buffer.Length;
            buffer.Append(dest.Text.Substring(readCount));
            dest.Text = buffer.ToString();
            return new ZilFix(readCount);
        }
    }
}
