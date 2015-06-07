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
        public static ZilObject PRINC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("PRINC", 1, 1);

            Console.Write(args[0].ToStringContext(ctx, true));
            return args[0];
        }

        [Subr]
        public static ZilObject CRLF(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            Console.WriteLine();
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject IMAGE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("IMAGE", 1, 1);

            ZilFix ch = args[0] as ZilFix;
            if (ch == null)
                throw new InterpreterError("IMAGE: arg must be a FIX");

            Console.Write((char)ch.Value);
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

            var result = new ZilChannel(ConvertPath(path.Text), FileAccess.Read);
            result.Open(ctx);
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
