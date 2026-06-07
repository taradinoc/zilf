/* Copyright 2010-2023 Tara McGrew
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
using System.IO;
using System.Text;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter.Values
{
    interface IChannelWithHPos
    {
        int HPos { get; }
    }
    
    interface IChannelWithStream
    {
        string Path { get; }
        Stream? Stream { get; }
    }

    [BuiltinType(StdAtom.CHANNEL, PrimType.VECTOR)]
    abstract class ZilChannel : ZilObject
    {
        /// <exception cref="InterpreterError">Always thrown.</exception>
        [ChtypeMethod]
        [DoesNotReturn]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        [SuppressMessage("Performance", "CA1801:Unused parameter")]
        public static ZilChannel FromVector(Context ctx, ZilVector vector) =>
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "CHANNEL");

        public override StdAtom StdTypeAtom => StdAtom.CHANNEL;

        public override PrimType PrimType => PrimType.VECTOR;

        public abstract void Reset(Context ctx);
        public abstract void Close();
        public abstract long? GetFileLength();
        public abstract char? ReadChar();
        public abstract bool WriteChar(char c);
        public abstract int WriteNewline();
        public abstract int WriteString(string s);
        public abstract bool IsEOF { get; }
    }

    [BuiltinAlternate(typeof(ZilChannel))]
    sealed class ZilFileChannel : ZilChannel, IChannelWithStream
    {
        readonly FileAccess fileAccess;
        readonly string path;
        Stream? stream;

        public ZilFileChannel(string path, FileAccess fileAccess)
        {
            this.path = path;
            this.fileAccess = fileAccess;
        }

        public string Path => path;
        public Stream? Stream => stream;

        public override string ToString() =>
            $"#CHANNEL [{(fileAccess == FileAccess.Read ? "READ" : "PRINT")} {ZilString.Quote(path)}]";

        public override ZilObject GetPrimitive(Context ctx) =>
            new ZilVector(ctx.GetStdAtom(fileAccess == FileAccess.Read ? StdAtom.READ : StdAtom.PRINT),
                ZilString.FromString(path));

        public override void Reset(Context ctx)
        {
            stream ??= ctx.OpenChannelStream(path, fileAccess);
        }

        public override void Close()
        {
            if (stream == null)
                return;

            try
            {
                stream.Close();
            }
            finally
            {
                stream = null;
            }
        }

        public override long? GetFileLength()
        {
            try
            {
                return stream?.Length;
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
            if (stream == null)
            {
                return false;
            }

            stream.WriteByte((byte)c);
            return true;
        }

        public override int WriteNewline()
        {
            return WriteChar('\n') ? 1 : 0;
        }

        public override int WriteString(string s)
        {
            if (stream == null)
            {
                return 0;
            }

            foreach (char c in s)
            {
                stream.WriteByte((byte)c);
            }

            return s.Length;
        }

        public override bool IsEOF => stream == null ? true : stream.Position >= stream.Length;
    }

    [BuiltinAlternate(typeof(ZilChannel))]
    sealed class ZilStringChannel : ZilChannel
    {
        readonly StringBuilder sb = new();

        /// <exception cref="ArgumentException"><paramref name="fileAccess"/> is not <see cref="FileAccess.Write"/>.</exception>
        public ZilStringChannel(FileAccess fileAccess)
        {
            if (fileAccess != FileAccess.Write)
                throw new ArgumentException("Only Write mode is supported", nameof(fileAccess));
        }

        public string String => sb.ToString();

        public override string ToString() => $"#CHANNEL [PRINT STRING {ZilString.Quote(sb.ToString())}]";

        public override ZilObject GetPrimitive(Context ctx) =>
            new ZilVector(ctx.GetStdAtom(StdAtom.PRINT), ctx.GetStdAtom(StdAtom.STRING),
                ZilString.FromString(sb.ToString()));

        public override void Reset(Context ctx)
        {
            // nada
        }

        public override void Close()
        {
            // nada
        }

        public override long? GetFileLength() => null;

        public override char? ReadChar() => null;

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

        public override int WriteString(string s)
        {
            sb.Append(s);
            return s.Length;
        }

        public override bool IsEOF => true;
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

        public override string ToString() => "#CHANNEL [PRINT CONSOLE]";

        public override ZilObject GetPrimitive(Context ctx) => new ZilVector(ctx.GetStdAtom(StdAtom.PRINT), ctx.GetStdAtom(StdAtom.CONSOLE));

        public override void Reset(Context ctx)
        {
            // nada
        }

        public override void Close()
        {
            // nada
        }

        public override long? GetFileLength() => null;

        public override char? ReadChar() => null;

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

        public override int WriteString(string s)
        {
            Console.Write(s);
            return s.Length;
        }

        public override bool IsEOF => true;

        public int HPos => Console.CursorLeft;
    }
}
