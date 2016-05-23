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
        public static ZilObject PRINT(Context ctx, ZilObject value, ZilChannel channel = null)
        {
            SubrContracts(ctx);

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRINT: bad OUTCHAN");
            }

            var str = value.ToStringContext(ctx, false);

            // TODO: check for I/O error
            channel.WriteNewline();
            channel.WriteString(str);
            channel.WriteChar(' ');

            return value;
        }

        [Subr]
        public static ZilObject PRIN1(Context ctx, ZilObject value, ZilChannel channel = null)
        {
            SubrContracts(ctx);

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRIN1: bad OUTCHAN");
            }

            var str = value.ToStringContext(ctx, false);

            // TODO: check for I/O error
            channel.WriteString(str);

            return value;
        }

        [Subr]
        public static ZilObject PRINC(Context ctx, ZilObject value, ZilChannel channel = null)
        {
            SubrContracts(ctx);

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("PRINC: bad OUTCHAN");
            }

            var str = value.ToStringContext(ctx, true);

            // TODO: check for I/O error
            channel.WriteString(str);

            return value;
        }

        [Subr]
        public static ZilObject CRLF(Context ctx, ZilChannel channel = null)
        {
            SubrContracts(ctx);

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("CRLF: bad OUTCHAN");
            }

            // TODO: check for I/O error
            channel.WriteNewline();

            return ctx.TRUE;
        }

        [Subr("PRINT-MANY")]
        public static ZilObject PRINT_MANY(Context ctx, ZilChannel channel,
            [Decl("<OR ATOM APPLICABLE>")] ZilObject printer, ZilObject[] items)
        {
            SubrContracts(ctx);

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
                var printArgs = new ZilObject[1];

                foreach (var item in items)
                {
                    result = item;

                    if (result == crlf)
                    {
                        CRLF(ctx);
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
        public static ZilObject IMAGE(Context ctx, ZilFix ch, ZilChannel channel = null)
        {
            SubrContracts(ctx);

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("IMAGE: bad OUTCHAN");
            }

            // TODO: check for I/O error
            channel.WriteChar((char)ch.Value);

            return ch;
        }

        private static readonly Regex RetroPathRE = new Regex(@"^(?:(?<device>[^:]+):)?(?:<(?<directory>[^>]+)>)?(?<filename>[^:<>]+)$");

        [Subr]
        public static ZilObject OPEN(Context ctx, [Decl("'\"READ\"")] string mode, string path)
        {
            SubrContracts(ctx);
            Contract.Requires(mode == "READ");

            var result = new ZilFileChannel(ConvertPath(path), FileAccess.Read);
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
        public static ZilObject CLOSE(Context ctx, ZilChannel channel)
        {
            SubrContracts(ctx);

            channel.Close(ctx);
            return channel;
        }

        [Subr("FILE-LENGTH")]
        public static ZilObject FILE_LENGTH(Context ctx, ZilChannel channel)
        {
            SubrContracts(ctx);

            var length = channel.GetFileLength();
            return length == null ? ctx.FALSE : new ZilFix((int)length.Value);
        }

        [Subr]
        public static ZilObject READSTRING(Context ctx, ZilString dest, ZilChannel channel,
            [Decl("<OR FIX STRING>")] ZilObject maxLengthOrStopChars = null)
        {
            SubrContracts(ctx);

            // TODO: support 1- and 4-argument forms?

            int maxLength = dest.Text.Length;
            ZilString stopChars = null;

            if (maxLengthOrStopChars != null)
            {
                var maxLengthFix = maxLengthOrStopChars as ZilFix;
                stopChars = maxLengthOrStopChars as ZilString;

                Contract.Assert(maxLengthFix != null || stopChars != null);

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

        [Subr("M-HPOS")]
        public static ZilObject M_HPOS(Context ctx, ZilChannel channel)
        {
            SubrContracts(ctx);

            var hposChannel = channel as IChannelWithHPos;
            if (hposChannel == null)
                throw new InterpreterError("M-HPOS: not supported by this type of channel");

            return new ZilFix(hposChannel.HPos);
        }

        [Subr("INDENT-TO")]
        public static ZilObject INDENT_TO(Context ctx, ZilFix position, ZilChannel channel = null)
        {
            SubrContracts(ctx);

            if (position.Value < 0)
                throw new InterpreterError("INDENT-TO: first arg must be non-negative");

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError("INDENT-TO: bad OUTCHAN");
            }

            var hposChannel = channel as IChannelWithHPos;
            if (hposChannel == null)
                throw new InterpreterError("INDENT-TO: not supported by this type of channel");

            var cur = hposChannel.HPos;
            while (cur < position.Value)
            {
                channel.WriteChar(' ');

                var next = hposChannel.HPos;
                if (next <= cur)
                {
                    // didn't move, or wrapped around
                    break;
                }

                cur = next;
            }

            return position;
        }
    }
}
