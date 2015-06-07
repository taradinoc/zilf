using System;
using System.IO;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.CHANNEL, PrimType.VECTOR)]
    class ZilChannel : ZilObject
    {
        private readonly FileAccess fileAccess;
        private readonly string path;
        private Stream stream;

        public ZilChannel(string path, FileAccess fileAccess)
        {
            this.path = path;
            this.fileAccess = fileAccess;
        }

        [ChtypeMethod]
        public static ZilChannel FromVector(Context ctx, ZilVector vector)
        {
            throw new InterpreterError("CHTYPE to CHANNEL not supported");
        }

        public override string ToString()
        {
            return string.Format(
                "#CHANNEL [{0} {1}]",
                fileAccess == FileAccess.Read ? "READ" : "NONE",
                ZilString.Quote(path));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CHANNEL);
        }

        public override PrimType PrimType
        {
            get { return PrimType.VECTOR; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilVector(new ZilObject[] {
                ctx.GetStdAtom(fileAccess == FileAccess.Read ? StdAtom.READ : StdAtom.NONE),
                new ZilString(path)
            });
        }

        public void Open(Context ctx)
        {
            if (stream == null)
                stream = ctx.OpenChannelStream(path, fileAccess);
        }

        public void Close(Context ctx)
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

        public long? GetFileLength()
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

        public char? ReadChar()
        {
            if (stream == null)
                return null;

            var result = stream.ReadByte();

            return result == -1 ? (char?)null : (char)result;
        }
    }
}