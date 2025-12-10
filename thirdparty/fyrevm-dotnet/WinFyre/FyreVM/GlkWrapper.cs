/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace FyreVM
{
    partial class Engine
    {
        private const int MAX_GLK_ARGS = 8;
        private uint[] glkArgs = new uint[MAX_GLK_ARGS];
        private Dictionary<uint, Func<uint[], uint>> glkHandlers;
        private bool glkWindowOpen, glkWantLineInput, glkWantCharInput;
        private uint glkLineInputBuffer, glkLineInputBufSize;
        private Dictionary<uint, GlkStream> glkStreams;
        private uint glkNextStreamID = 100;
        private GlkStream glkCurrentStream;

        private static class GlkConst
        {
            public const uint wintype_TextBuffer = 3;

            public const uint evtype_None = 0;
            public const uint evtype_CharInput = 2;
            public const uint evtype_LineInput = 3;

            public const uint gestalt_CharInput = 1;
            public const uint gestalt_CharOutput = 3;
            public const uint gestalt_CharOutput_ApproxPrint = 1;
            public const uint gestalt_CharOutput_CannotPrint = 0;
            public const uint gestalt_CharOutput_ExactPrint = 2;
            public const uint gestalt_LineInput = 2;
            public const uint gestalt_Version = 0;
        }

        private abstract class GlkStream
        {
            public readonly uint ID;

            public GlkStream(uint id)
            {
                this.ID = id;
            }

            public abstract void PutChar(uint c);

            public abstract bool Close(out uint read, out uint written);
        }

        private class GlkWindowStream : GlkStream
        {
            private readonly Engine engine;

            public GlkWindowStream(uint id, Engine engine)
                : base(id)
            {
                this.engine = engine;
            }

            public override void PutChar(uint c)
            {
                if (c > 0xffff)
                    c = '?';

                engine.outputBuffer.Write((char)c);
            }

            public override bool Close(out uint read, out uint written)
            {
                written = 0;
                read = 0;
                return false;
            }
        }

        private class GlkMemoryStream : GlkStream
        {
            private readonly Engine engine;
            private readonly uint address;
            private readonly byte[] buffer;
            private uint position, written, read;

            public GlkMemoryStream(uint id, Engine engine, uint address, uint size)
                : base(id)
            {
                this.engine = engine;
                this.address = address;

                if (address != 0 && size != 0)
                    buffer = new byte[size];

                position = written = read = 0;
            }

            public override void PutChar(uint c)
            {
                if (c > 0xff)
                    c = '?';

                written++;
                if (position < buffer.Length)
                    buffer[position++] = (byte)c;
            }

            public override bool Close(out uint read, out uint written)
            {
                written = this.written;
                read = this.read;

                if (buffer != null)
                {
                    uint max = (uint)Math.Min(written, buffer.Length);
                    for (uint i = 0; i < max; i++)
                        engine.image.WriteByte(address + i, buffer[i]);
                }

                return true;
            }
        }

        private class GlkMemoryUniStream : GlkStream
        {
            private readonly Engine engine;
            private readonly uint address;
            private readonly uint[] buffer;
            private uint position, written, read;

            public GlkMemoryUniStream(uint id, Engine engine, uint address, uint size)
                : base(id)
            {
                this.engine = engine;
                this.address = address;

                if (address != 0 && size != 0)
                    buffer = new uint[size];

                position = written = read = 0;
            }

            public override void PutChar(uint c)
            {
                written++;
                if (position < buffer.Length)
                    buffer[position++] = c;
            }

            public override bool Close(out uint read, out uint written)
            {
                written = this.written;
                read = this.read;

                if (buffer != null)
                {
                    uint max = (uint)Math.Min(written, buffer.Length);
                    for (uint i = 0; i < max; i++)
                        engine.image.WriteInt32(address + i * 4, buffer[i]);
                }

                return true;
            }
        }

        private void GlkWrapperCall(uint[] args)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("glk(0x{0:X4}, {1})", args[0], args[1]));

            if (glkHandlers == null)
                InitGlkHandlers();

            int gargc = (int)args[1];
            if (gargc > MAX_GLK_ARGS)
                throw new ArgumentException("Too many stack arguments for @glk");

            for (int i = 0; i < gargc; i++)
                glkArgs[i] = Pop();

            Func<uint[], uint> handler;
            if (glkHandlers.TryGetValue(args[0], out handler))
            {
                System.Diagnostics.Debug.WriteLine(" // " + handler.Target.GetType().Name);
                args[2] = handler(glkArgs);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(" // unimplemented");
                args[2] = 0;
            }
        }

        private void GlkWrapperWrite(uint ch)
        {
            if (glkCurrentStream != null)
                glkCurrentStream.PutChar(ch);
        }

        private void GlkWrapperWrite(string str)
        {
            if (glkCurrentStream != null)
                foreach (char c in str)
                    glkCurrentStream.PutChar(c);
        }

        private uint GlkReadReference(uint reference)
        {
            if (reference == 0xffffffff)
                return Pop();

            return image.ReadInt32(reference);
        }

        private void GlkWriteReference(uint reference, uint value)
        {
            if (reference == 0xffffffff)
                Push(value);
            else
                image.WriteInt32(reference, value);
        }

        private void GlkWriteReference(uint reference, params uint[] values)
        {
            if (reference == 0xffffffff)
            {
                foreach (uint v in values)
                    Push(v);
            }
            else
            {
                foreach (uint v in values)
                {
                    image.WriteInt32(reference, v);
                    reference += 4;
                }
            }
        }

        private void InitGlkHandlers()
        {
            glkHandlers = new Dictionary<uint, Func<uint[], uint>>();

            glkHandlers.Add(0x0040, glk_stream_iterate);
            glkHandlers.Add(0x0020, glk_window_iterate);
            glkHandlers.Add(0x0064, glk_fileref_iterate);
            glkHandlers.Add(0x0023, glk_window_open);
            glkHandlers.Add(0x002F, glk_set_window);
            glkHandlers.Add(0x0086, glk_set_style);
            glkHandlers.Add(0x00D0, glk_request_line_event);
            glkHandlers.Add(0x00C0, glk_select);
            glkHandlers.Add(0x00A0, glk_char_to_lower);
            glkHandlers.Add(0x00A1, glk_char_to_upper);
            glkHandlers.Add(0x0043, glk_stream_open_memory);
            glkHandlers.Add(0x0048, glk_stream_get_current);
            glkHandlers.Add(0x0139, glk_stream_open_memory_uni);
            glkHandlers.Add(0x0047, glk_stream_set_current);
            glkHandlers.Add(0x0044, glk_stream_close);

            glkStreams = new Dictionary<uint, GlkStream>();
        }

        private uint glk_stream_iterate(uint[] args)
        {
            return 0;
        }

        private uint glk_window_iterate(uint[] args)
        {
            if (glkWindowOpen && args[0] == 0)
                return 1;

            return 0;
        }

        private uint glk_fileref_iterate(uint[] args)
        {
            return 0;
        }

        private uint glk_window_open(uint[] args)
        {
            if (glkWindowOpen)
                return 0;

            glkWindowOpen = true;
            glkStreams[1] = new GlkWindowStream(1, this);
            return 1;
        }

        private uint glk_set_window(uint[] args)
        {
            if (glkWindowOpen)
                glkStreams.TryGetValue(1, out glkCurrentStream);

            return 0;
        }

        private uint glk_set_style(uint[] args)
        {
            return 0;
        }

        private uint glk_request_line_event(uint[] args)
        {
            glkWantLineInput = true;
            glkLineInputBuffer = args[1];
            glkLineInputBufSize = args[2];
            return 0;
        }

        private uint glk_select(uint[] args)
        {
            DeliverOutput();

            if (glkWantLineInput)
            {
                string line;
                if (LineWanted == null)
                {
                    line = "";
                }
                else
                {
                    LineWantedEventArgs e = new LineWantedEventArgs();
                    LineWanted(this, e);
                    line = e.Line;
                }

                byte[] bytes = StringToLatin1(line);
                uint max = Math.Min(glkLineInputBufSize, (uint)bytes.Length);
                for (uint i = 0; i < max; i++)
                    image.WriteByte(glkLineInputBuffer + i, bytes[i]);

                // return event
                GlkWriteReference(
                    args[0],
                    GlkConst.evtype_LineInput, 1, max, 0);

                glkWantLineInput = false;
            }
            else if (glkWantCharInput)
            {
                char ch;
                if (KeyWanted == null)
                {
                    ch = '\0';
                }
                else
                {
                    KeyWantedEventArgs e = new KeyWantedEventArgs();
                    KeyWanted(this, e);
                    ch = e.Char;
                }

                // return event
                GlkWriteReference(
                    args[0],
                    GlkConst.evtype_CharInput, 1, ch, 0);

                glkWantCharInput = false;
            }
            else
            {
                // no event
                GlkWriteReference(
                    args[0],
                    GlkConst.evtype_None, 0, 0, 0);
            }

            return 0;
        }

        private uint glk_char_to_lower(uint[] args)
        {
            char ch = (char)args[0];
            return (uint)char.ToLower(ch);
        }

        private uint glk_char_to_upper(uint[] args)
        {
            char ch = (char)args[0];
            return (uint)char.ToUpper(ch);
        }

        private uint glk_stream_open_memory(uint[] args)
        {
            uint id = glkNextStreamID++;
            GlkStream stream = new GlkMemoryStream(id, this, args[0], args[1]);
            glkStreams[id] = stream;
            return id;
        }

        private uint glk_stream_open_memory_uni(uint[] args)
        {
            uint id = glkNextStreamID++;
            GlkStream stream = new GlkMemoryUniStream(id, this, args[0], args[1]);
            glkStreams[id] = stream;
            return id;
        }

        private uint glk_stream_get_current(uint[] args)
        {
            if (glkCurrentStream == null)
                return 0;

            return glkCurrentStream.ID;
        }

        private uint glk_stream_set_current(uint[] args)
        {
            glkStreams.TryGetValue(args[0], out glkCurrentStream);
            return 0;
        }

        private uint glk_stream_close(uint[] args)
        {
            GlkStream stream;
            if (glkStreams.TryGetValue(args[0], out stream))
            {
                uint read, written;
                bool closed = stream.Close(out read, out written);
                if (args[1] != 0)
                {
                    GlkWriteReference(
                        args[1],
                        read, written);
                }

                if (closed)
                {
                    glkStreams.Remove(args[0]);
                    if (glkCurrentStream == stream)
                        glkCurrentStream = null;
                }
            }

            return 0;
        }
    }
}