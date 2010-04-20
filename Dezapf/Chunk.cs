﻿/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of DeZAPF.
 * 
 * DeZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * DeZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with DeZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dezapf
{
    abstract class Chunk
    {
        public int PC { get; protected set; }
        public int Length { get; protected set; }
        public Chunk Parent;

        protected Chunk(int pc, int length)
        {
            this.PC = pc;
            this.Length = length;
        }

        public abstract void WriteTo(TextWriter writer, Context ctx);

        public virtual bool WantNewParagraph(Chunk previous)
        {
            return true;
        }

        public virtual int GetAlignment(Context ctx)
        {
            return 1;
        }
    }

    class DataChunk : Chunk
    {
        public byte[] Bytes { get; private set; }

        private DataChunk(int pc, int length, byte[] data)
            : base(pc, length)
        {
            this.Bytes = data;
        }

        public static DataChunk FromStream(Stream stream, int offset, int length)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            byte[] data = new byte[length];
            stream.Read(data, 0, length);
            return new DataChunk(offset, length, data);
        }

        public override void WriteTo(TextWriter writer, Context ctx)
        {
            ctx.OutputStyle.FormatData(writer, ctx, this);
        }
    }

    class CompoundChunk : Chunk
    {
        protected readonly List<Chunk> contents = new List<Chunk>();

        public static CompoundChunk Combine(Chunk first, Chunk second)
        {
            CompoundChunk cc = first as CompoundChunk;
            if (cc != null)
            {
                cc.Add(second);
                return cc;
            }

            return new CompoundChunk(first.PC, first.Length + second.Length, first, second);
        }

        public CompoundChunk(int pc, int length)
            : base(pc, length)
        {
        }

        public CompoundChunk(int pc, int length, Chunk first, Chunk second)
            : this(pc, length)
        {
            contents.Add(first);
            contents.Add(second);
        }

        public void Add(Chunk next)
        {
            next.Parent = this;
            contents.Add(next);
            Length += next.Length;
        }

        public override void WriteTo(TextWriter writer, Context ctx)
        {
            foreach (Chunk ch in contents)
                ch.WriteTo(writer, ctx);
        }
    }

    class FunctChunk : CompoundChunk
    {
        public FunctChunk(int pc, int length)
            : base(pc, length)
        {
        }

        public ushort[] Locals { get; set; }

        public override void WriteTo(TextWriter writer, Context ctx)
        {
            ctx.OutputStyle.FormatFunctStart(writer, ctx, this);
            base.WriteTo(writer, ctx);
            ctx.OutputStyle.FormatFunctEnd(writer, ctx, this);
        }

        public override int GetAlignment(Context ctx)
        {
            return ctx.PackingFactor;
        }
    }

    class GlobalsChunk : Chunk
    {
        public ushort[] Values { get; private set; }

        private GlobalsChunk(int pc, ushort[] values)
            : base(pc, values.Length * 2)
        {
            this.Values = values;
        }

        public static GlobalsChunk FromStream(Stream stream, int offset, int length)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            BinaryReader rdr = new BinaryReader(stream);
            int count = length / 2;
            ushort[] values = new ushort[count];
            for (int i = 0; i < count; i++)
                values[i] = rdr.ReadZWord();

            return new GlobalsChunk(offset, values);
        }

        public override void WriteTo(TextWriter writer, Context ctx)
        {
            ctx.OutputStyle.FormatGlobalsStart(writer, ctx, Values.Length);
            for (int i = 0; i < Values.Length; i++)
                ctx.OutputStyle.FormatGlobal(writer, ctx, (ushort)(i + 16), Values[i]);
            ctx.OutputStyle.FormatGlobalsEnd(writer, ctx);
        }
    }
}
