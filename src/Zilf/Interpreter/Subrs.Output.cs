/* Copyright 2010-2026 Tara McGrew
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Language.Parsing;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        /// <summary>
        /// Writes the round-trippable string representation of a value to a channel, preceded by a newline and followed by a space.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to print.</param>
        /// <param name="channel">The channel to write to. If omitted, defaults to the local or global value of OUTCHAN.</param>
        /// <returns>The value whose representation was printed.</returns>
        /// <exception cref="InterpreterError">Bad OUTCHAN.</exception>
        [Subr]
        public static ZilObject PRINT(Context ctx, ZilObject value, ZilChannel? channel = null)
        {
            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError(InterpreterMessages._0_Bad_OUTCHAN, "PRINT");
            }

            var str = value.ToStringContext(ctx, false);

            // TODO: check for I/O error
            channel.WriteNewline();
            channel.WriteString(str);
            channel.WriteChar(' ');

            return value;
        }

        /// <summary>
        /// Writes the round-trippable string representation of a value to a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to print.</param>
        /// <param name="channel">The channel to write to. If omitted, defaults to the local or global value of OUTCHAN.</param>
        /// <returns>The value whose representation was printed.</returns>
        /// <exception cref="InterpreterError">Bad OUTCHAN.</exception>
        [Subr]
        public static ZilObject PRIN1(Context ctx, ZilObject value, ZilChannel? channel = null)
        {
            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError(InterpreterMessages._0_Bad_OUTCHAN, "PRIN1");
            }

            var str = value.ToStringContext(ctx, false);

            // TODO: check for I/O error
            channel.WriteString(str);

            return value;
        }

        /// <summary>
        /// Writes the "friendly" string representation of a value to a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to print.</param>
        /// <param name="channel">The channel to write to. If omitted, defaults to the local or global value of OUTCHAN.</param>
        /// <returns>The value whose representation was printed.</returns>
        /// <exception cref="InterpreterError">Bad OUTCHAN.</exception>
        [Subr]
        public static ZilObject PRINC(Context ctx, ZilObject value, ZilChannel? channel = null)
        {
            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError(InterpreterMessages._0_Bad_OUTCHAN, "PRINC");
            }

            var str = value.ToStringContext(ctx, true);

            // TODO: check for I/O error
            channel.WriteString(str);

            return value;
        }

        /// <summary>
        /// Writes a carriage return and linefeed to a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to write to.</param>
        /// <returns></returns>
        /// <exception cref="InterpreterError">Bad OUTCHAN.</exception>
        [Subr]
        public static ZilObject CRLF(Context ctx, ZilChannel? channel = null)
        {
            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError(InterpreterMessages._0_Bad_OUTCHAN, "CRLF");
            }

            // TODO: check for I/O error
            channel.WriteNewline();

            return ctx.TRUE;
        }

        /// <summary>
        /// Writes the string representations of a series of items to a channel,
        /// using a specified printer function to convert each item to a string.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to write to.</param>
        /// <param name="printer">Either an applicable value (such as a function), or an atom whose global or local value contains an applicable value.</param>
        /// <param name="items">The items to print.</param>
        /// <returns>The last item printed.</returns>
        /// <exception cref="InterpreterError"><paramref name="printer"/> is an atom which has no local or global value.</exception>
        [Subr("PRINT-MANY")]
        public static ZilObject PRINT_MANY(Context ctx, ZilChannel channel,
            [Decl("<OR ATOM APPLICABLE>")] ZilObject printer, ZilObject[] items)
        {
            if (printer is ZilAtom atom)
            {
                printer = ctx.GetGlobalVal(atom) ?? ctx.GetLocalVal(atom) ??
                          throw new InterpreterError(
                              InterpreterMessages._0_Atom_1_Has_No_2_Value,
                              "PRINT-MANY",
                              atom.ToStringContext(ctx, false),
                              "local or global");
            }

            if (!printer.IsApplicable(ctx, out var applicablePrinter))
            {
                throw new InterpreterError(InterpreterMessages._0_Not_Applicable_1,
                    "PRINT-MANY",
                    printer.ToStringContext(ctx, false));
            }

            var crlf = ctx.GetStdAtom(StdAtom.PRMANY_CRLF);
            var result = ctx.TRUE;

            using var innerEnv = ctx.PushEnvironment();

            innerEnv.Rebind(ctx.GetStdAtom(StdAtom.OUTCHAN), channel);

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

            return result;
        }

        /// <summary>
        /// Writes a single character to a channel by its ASCII value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ch">The ASCII value of the character to write.</param>
        /// <param name="channel">The channel to write to.</param>
        /// <returns>The ASCII value.</returns>
        /// <exception cref="InterpreterError">Bad OUTCHAN.</exception>
        [Subr]
        public static ZilObject IMAGE(Context ctx, ZilFix ch, ZilChannel? channel = null)
        {
            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError(InterpreterMessages._0_Bad_OUTCHAN, "IMAGE");
            }

            // TODO: check for I/O error
            channel.WriteChar((char)ch.Value);

            return ch;
        }

        [GeneratedRegex("^(?:(?<device>[^:]+):)?(?:<(?<directory>[^>]+)>)?(?<filename>[^:<>]+)$")]
        private static partial Regex GetRetroPathRegex();

        /// <summary>
        /// Opens a file channel for reading from a file specified by a retro-style path.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="mode">Must be the atom READ or PRINT.</param>
        /// <param name="path">A retro-style file path.</param>
        /// <param name="ext">An optional file extension to use.</param>
        /// <returns>The opened file channel.</returns>
        [Subr]
        public static ZilObject OPEN(Context ctx,
            [Decl("<OR '\"READ\" '\"PRINT\">")] string mode, string path,
            string? ext = null)
        {
            string convertedPath = ConvertPath(path);

            if (ext != null)
            {
                convertedPath += "." + ext;
            }

            bool write = mode == "PRINT";

            if (write && !ctx.AllowFileWrites)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_File_Writes_Are_Not_Permitted,
                    "OPEN");
            }

            var result = new ZilFileChannel(
                convertedPath,
                write ? FileAccess.Write : FileAccess.Read);
            result.Reset(ctx);
            return result;
        }

        static string ConvertPath(string retroPath)
        {
            var match = GetRetroPathRegex().Match(retroPath);
            return match.Success ? match.Groups["filename"].Value : retroPath;
        }

        /// <summary>
        /// Closes a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to close.</param>
        /// <returns>The closed channel.</returns>
        [Subr]
        public static ZilObject CLOSE(Context ctx, ZilChannel channel)
        {
            channel.Close();
            return channel;
        }

        /// <summary>
        /// Gets the length of a file associated with a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to check.</param>
        /// <returns>The length of the file, or false if the channel is not associated with a file.</returns>
        [Subr("FILE-LENGTH")]
        public static ZilObject FILE_LENGTH(Context ctx, ZilChannel channel)
        {
            var length = channel.GetFileLength();
            return length == null ? ctx.FALSE : new ZilFix((int)length.Value);
        }

        /// <summary>
        /// Reads a character from a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to read from.</param>
        /// <returns>The character read, or a control-Z character if the channel is at EOF.</returns>
        [Subr]
        public static ZilObject READCHR(Context ctx, ZilChannel channel)
        {
            if (channel.IsEOF)
            {
                return new ZilChar((char)0x1a);     // ^Z
            }

            char c = channel.ReadChar() ?? (char)0x1a;
            return new ZilChar(c);
        }

        /// <summary>
        /// Reads a character from a channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to read from. Defaults to the local value of INCHAN.</param>
        /// <returns>The character read, or a control-Z character if the channel is at EOF.</returns>
        /// <exception cref="InterpreterError">INCHAN has no local value.</exception>
        [Subr]
        public static ZilObject TYI(Context ctx, ZilChannel? channel = null)
        {
            if (channel == null)
            {
                if (ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.INCHAN)) is ZilChannel inchan)
                {
                    channel = inchan;
                }
                else
                {
                    throw new InterpreterError(
                        InterpreterMessages._0_Atom_1_Has_No_2_Value,
                        "TYI",
                        "INCHAN",
                        "local");
                }
            }

            return READCHR(ctx, channel);
        }

        /// <summary>
        /// Turns the echoing of typed characters on a console input channel on or off.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to control.</param>
        /// <param name="enable">True to cause typed characters to echo, or false to hide them.</param>
        /// <returns>The channel.</returns>
        [Subr]
        public static ZilObject TTYECHO(Context ctx, ZilChannel channel, bool enable)
        {
            if (channel is ZilConsoleChannel consoleChannel)
            {
                consoleChannel.EchoInput = enable;
            }

            return channel;
        }

        /// <summary>
        /// Reads a string from a channel into a destination string, up to a maximum length or until a stop character is encountered.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dest">The string to read into. Its contents will be overwritten by the data read from the channel.</param>
        /// <param name="channel">The channel to read from.</param>
        /// <param name="maxLengthOrStopChars">Either the maximum number of characters to read, or a string of stop characters.</param>
        /// <returns>The number of characters read.</returns>
        [Subr]
        public static ZilObject READSTRING(Context ctx, ZilString dest, ZilChannel channel,
            [Decl("<OR FIX STRING>")] ZilObject? maxLengthOrStopChars = null)
        {
            // TODO: support 1- and 4-argument forms?

            int maxLength = dest.Text.Length;
            ZilString? stopChars = null;

            if (maxLengthOrStopChars != null)
            {
                var maxLengthFix = maxLengthOrStopChars as ZilFix;
                stopChars = maxLengthOrStopChars as ZilString;

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
                    var c = channel.ReadChar();
                    if (c != null &&
                        (stopChars == null || stopChars.Text.IndexOf(c.Value, StringComparison.Ordinal) < 0))
                    {
                        buffer.Append(c.Value);
                        reading = true;
                    }
                }
            } while (reading);

            var readCount = buffer.Length;
            buffer.Append(dest.Text[readCount..]);
            dest.Text = buffer.ToString();
            return new ZilFix(readCount);
        }

        /// <summary>
        /// Gets the horizontal position of the channel.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="channel">The channel to check.</param>
        /// <returns>The zero-based column position of the channel.</returns>
        /// <exception cref="InterpreterError">Not supported by this type of channel.</exception>
        [Subr("M-HPOS")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "M_ is not a member prefix here")]
        public static ZilObject M_HPOS(Context ctx, ZilChannel channel)
        {
            if (channel is not IChannelWithHPos hposChannel)
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_This_Type_Of_Channel, "M-HPOS");

            return new ZilFix(hposChannel.HPos);
        }

        /// <summary>
        /// Writes spaces to a channel until the horizontal position reaches the specified column.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="position">The target horizontal position.</param>
        /// <param name="channel">The channel to write to. If omitted, defaults to the current OUTCHAN.</param>
        /// <returns>The target horizontal position.</returns>
        /// <exception cref="InterpreterError"><paramref name="position"/> is negative.</exception>
        [Subr("INDENT-TO")]
        public static ZilObject INDENT_TO(Context ctx, ZilFix position, ZilChannel? channel = null)
        {
            if (position.Value < 0)
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1,
                    "INDENT-TO: arg 1",
                    "a non-negative FIX");

            if (channel == null)
            {
                channel =
                    ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel ??
                    ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.OUTCHAN)) as ZilChannel;
                if (channel == null)
                    throw new InterpreterError(InterpreterMessages._0_Bad_OUTCHAN, "INDENT-TO");
            }

            if (channel is not IChannelWithHPos hposChannel)
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_This_Type_Of_Channel, "INDENT-TO");

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

        /// <summary>
        /// Defines a prefix macro for the parser.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ch">The character which will become a new prefix.</param>
        /// <param name="handlerOrFalse">An applicable value (such as a function) to call when the prefix is encountered,
        /// or false to remove the prefix macro.</param>
        /// <returns>True.</returns>
        [Subr("MAKE-PREFIX-MACRO", ObList = "READER-MACROS!-PACKAGE")]
        public static ZilObject MAKE_PREFIX_MACRO(Context ctx, ZilChar ch,
            [Either(typeof(IApplicable), typeof(ZilFalse))]
            object handlerOrFalse)
        {
            ctx.ParserMacros.MakePrefixMacro(ch.Char,
                handlerOrFalse is IApplicable a
                    ? (c, zo) => ParserOutput.FromObject((ZilObject)a.ApplyNoEval(c, new[] { zo }))
                    : (SimplePrefixMacroHandlerWithContext?)null);
            return ctx.TRUE;
        }
        
        /// <summary>
        /// Sets the source information of a destination value to match that of a source value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dest">The destination value to modify.</param>
        /// <param name="src">The source value whose source information will be copied.</param>
        /// <returns>The destination value.</returns>
        [Subr("SET-SOURCE-INFO", ObList = "READER-MACROS!-PACKAGE")]
        public static ZilObject SET_SOURCE_INFO(Context ctx, ZilObject dest, ZilObject src)
        {
            dest.SourceLine = src.SourceLine;
            return dest;
        }
    }
}
