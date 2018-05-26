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

using System;
using System.IO;
using System.Text;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    interface IChannelWithHPos
    {
        int HPos { get; }
    }

    [BuiltinType(StdAtom.CHANNEL, PrimType.VECTOR)]
    abstract class ZilChannel : ZilObject
    {
        /// <exception cref="InterpreterError">Always thrown.</exception>
        [ChtypeMethod]
        public static ZilChannel FromVector([NotNull] Context ctx, [NotNull] ZilVector vector)
        {

            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "CHANNEL");
        }

        public override StdAtom StdTypeAtom => StdAtom.CHANNEL;

        public override PrimType PrimType => PrimType.VECTOR;

        public abstract void Reset(Context ctx);
        public abstract void Close();
        public abstract long? GetFileLength();
        public abstract char? ReadChar();
        public abstract bool WriteChar(char c);
        public abstract int WriteNewline();
        public abstract int WriteString(string s);
    }

    [BuiltinAlternate(typeof(ZilChannel))]
    sealed class ZilFileChannel : ZilChannel
    {
        readonly FileAccess fileAccess;
        readonly string path;
        Stream stream;

        public ZilFileChannel(string path, FileAccess fileAccess)
        {
            this.path = path;
            this.fileAccess = fileAccess;
        }

        public override string ToString()
        {
            return $"#CHANNEL [{(fileAccess == FileAccess.Read ? "READ" : "NONE")} {ZilString.Quote(path)}]";
        }

        [NotNull]
        public override ZilObject GetPrimitive([NotNull] Context ctx)
        {
            return new ZilVector(ctx.GetStdAtom(fileAccess == FileAccess.Read ? StdAtom.READ : StdAtom.NONE),
                ZilString.FromString(path));
        }

        public override void Reset(Context ctx)
        {
            if (stream == null)
                stream = ctx.OpenChannelStream(path, fileAccess);
        }

        public override void Close()
        {
            if (stream != null)
            {
                try
                {
                    stream.Close();
                }
                finally
                {
                    stream = null;
                }
            }
        }

        public override long? GetFileLength()
        {
            if (stream == null)
            {
                return null;
            }

            try
            {
                return stream.Length;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        public override char? ReadChar()
        {
            if (stream == null)
                return null;

            var result = stream.ReadByte();

            return result == -1 ? (char?)null : (char)result;
        }

        public override bool WriteChar(char c)
        {
            return false;
        }

        public override int WriteNewline()
        {
            return 0;
        }

        public override int WriteString(string s)
        {
            return 0;
        }
    }

    [BuiltinAlternate(typeof(ZilChannel))]
    sealed class ZilStringChannel : ZilChannel
    {
        readonly StringBuilder sb = new StringBuilder();

        /// <exception cref="ArgumentException"><paramref name="fileAccess"/> is not <see cref="FileAccess.Write"/>.</exception>
        public ZilStringChannel(FileAccess fileAccess)
        {
            if (fileAccess != FileAccess.Write)
                throw new ArgumentException("Only Write mode is supported", nameof(fileAccess));
        }

        [NotNull]
        public string String => sb.ToString();

        public override string ToString()
        {
            return $"#CHANNEL [PRINT STRING {ZilString.Quote(sb.ToString())}]";
        }

        [NotNull]
        public override ZilObject GetPrimitive([NotNull] Context ctx)
        {
            return new ZilVector(ctx.GetStdAtom(StdAtom.PRINT), ctx.GetStdAtom(StdAtom.STRING),
                ZilString.FromString(sb.ToString()));
        }

        public override void Reset(Context ctx)
        {
            // nada
        }

        public override void Close()
        {
            // nada
        }

        public override long? GetFileLength()
        {
            return null;
        }

        public override char? ReadChar()
        {
            return null;
        }

        public override bool WriteChar(char c)
        {
            sb.Append(c);
            return true;
        }

        public override int WriteNewline()
        {
            sb.Append('\n');
            return 1;
        }

        public override int WriteString([NotNull] string s)
        {
            sb.Append(s);
            return s.Length;
        }
    }

    [BuiltinAlternate(typeof(ZilChannel))]
    sealed class ZilConsoleChannel : ZilChannel, IChannelWithHPos
    {
        /// <exception cref="ArgumentException"><paramref name="fileAccess"/> is not <see cref="FileAccess.Write"/>.</exception>
        public ZilConsoleChannel(FileAccess fileAccess)
        {
            if (fileAccess != FileAccess.Write)
                throw new ArgumentException("Only Write mode is supported", nameof(fileAccess));
        }

        public override string ToString()
        {
            return "#CHANNEL [PRINT CONSOLE]";
        }

        [NotNull]
        public override ZilObject GetPrimitive([NotNull] Context ctx)
        {
            return new ZilVector(ctx.GetStdAtom(StdAtom.PRINT), ctx.GetStdAtom(StdAtom.CONSOLE));
        }

        public override void Reset(Context ctx)
        {
            // nada
        }

        public override void Close()
        {
            // nada
        }

        public override long? GetFileLength()
        {
            return null;
        }

        public override char? ReadChar()
        {
            return null;
        }

        public override bool WriteChar(char c)
        {
            Console.Write(c);
            return true;
        }

        public override int WriteNewline()
        {
            Console.WriteLine();
            return Environment.NewLine.Length;
        }

        public override int WriteString([NotNull] string s)
        {
            Console.Write(s);
            return s.Length;
        }

        public int HPos => Console.CursorLeft;
    }
}
