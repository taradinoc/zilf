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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zapf;

namespace Dezapf
{
    static class Program
    {
        public static void Main(string[] args)
        {
            using (Stream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read))
            {
                var fileLength = (int)stream.Length;
                var rdr = new BinaryReader(stream);

                var ctx = new Context();
                var ranges = new RangeList<Chunk>();
                var hdr = new Header(rdr);

                ctx.Header = hdr;
                ctx.OutputStyle = new ZapRoundTripStyle();

                ranges.AddRange(0, 64, new HeaderChunk(0, 64, hdr));

                // find instructions
                var todo = new Stack<int>();
                var pendingFuncs = new Queue<int>();
                var pastFuncs = new HashSet<int>();
                var maxGlobal = 0;
                todo.Push(hdr.Start);

                while (todo.Count > 0)
                {
                    var pc = todo.Pop();

                    if (ranges.TryGetValue(pc, out _) == false)
                    {
                        stream.Seek(pc, SeekOrigin.Begin);
                        var inst = Instruction.Decode(ctx, rdr, pc);

                        if (inst != null)
                        {
                            ranges.AddRange(pc, inst.Length, inst);

                            for (var i = 0; i < inst.OperandTypes.Length; i++)
                                if (inst.OperandTypes[i] == OperandType.Variable && inst.Operands[i] >= 16)
                                    maxGlobal = Math.Max(maxGlobal, inst.Operands[i]);

                            if (inst.BranchType != BranchType.None &&
                                inst.BranchOffset != 0 && inst.BranchOffset != 1)
                            {
                                todo.Push(pc + inst.Length + inst.BranchOffset - 2);
                            }

                            var attr = ctx.GetOpcodeInfo(inst.Op);

                            if ((attr.Flags & ZOpFlags.Terminates) == 0)
                                todo.Push(pc + inst.Length);

                            if ((attr.Flags & ZOpFlags.Call) != 0)
                            {
                                switch (inst.OperandTypes[0])
                                {
                                    case OperandType.Word:
                                    case OperandType.Byte:
                                        pendingFuncs.Enqueue(ctx.UnpackAddress(inst.Operands[0], hdr.RoutineOffset));
                                        break;
                                }
                            }
                            else if ((attr.Flags & ZOpFlags.Label) != 0)
                            {
                                switch (inst.OperandTypes[0])
                                {
                                    case OperandType.Word:
                                    case OperandType.Byte:
                                        todo.Push(pc + inst.Length + (short)inst.Operands[0] - 2);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            //XXX
                            Console.WriteLine("* Invalid instruction at PC={0:x5}", pc);
                        }
                    }

                    while (todo.Count == 0 && pendingFuncs.Count > 0)
                    {
                        var funcAddr = pendingFuncs.Dequeue();
                        if (!pastFuncs.Contains(funcAddr))
                        {
                            pastFuncs.Add(funcAddr);
                            stream.Seek(funcAddr, SeekOrigin.Begin);
                            funcAddr++;

                            // skip function header
                            int locals = rdr.ReadByte();
                            if (ctx.ZVersion < 5)
                                funcAddr += 2 * locals;

                            todo.Push(funcAddr);
                        }
                    }
                }

                // combine instructions into routines
                foreach (var address in pastFuncs)
                    ComposeFunc(ctx, stream, rdr, ranges, address);

                // mark global variables
                maxGlobal -= 15;
                if (maxGlobal > 0)
                {
                    var globalsChunk = GlobalsChunk.FromStream(stream, hdr.Globals, maxGlobal * 2);
                    ranges.AddRange(hdr.Globals, maxGlobal * 2, globalsChunk);
                }

                // mark memory borders
                if (!ranges.Contains(hdr.EndLod))
                    ranges.AddRange(hdr.EndLod, 1, DataChunk.FromStream(stream, hdr.EndLod, 1));

                if (!ranges.Contains(hdr.Impure))
                    ranges.AddRange(hdr.Impure, 1, DataChunk.FromStream(stream, hdr.Impure, 1));

                // fill in gaps with data chunks
                var gapChunks = new Queue<DataChunk>();
                foreach (var gap in ranges.FindGaps(0, fileLength))
                    gapChunks.Enqueue(DataChunk.FromStream(stream, gap.Start, gap.Length));
                while (gapChunks.Count > 0)
                {
                    var chunk = gapChunks.Dequeue();

                    // skip padding chunks filled with zeroes
                    if (ranges.TryGetValue(chunk.PC + chunk.Length, out var nextChunk) &&
                        chunk.Length < nextChunk.GetAlignment(ctx) &&
                        chunk.Bytes.All(b => b == 0))
                    {
                        continue;
                    }

                    ranges.AddRange(chunk.PC, chunk.Length, chunk);
                }

                // output
                Chunk lastChunk = null;
                foreach (var r in ranges)
                {
                    if (lastChunk != null && r.Value.WantNewParagraph(lastChunk))
                        Console.WriteLine();

                    if (r.Start > 0)
                    {
                        if (r.Start == hdr.AlphabetTable)
                            Console.WriteLine("ALPHABET::");
                        if (r.Start == hdr.EndLod)
                            Console.WriteLine("ENDLOD::");
                        if (r.Start == hdr.ExtensionTable)
                            Console.WriteLine("HDREXT::");
                        if (r.Start == hdr.Words)
                            Console.WriteLine("WORDS::");
                        if (r.Start == hdr.Globals)
                            Console.WriteLine("GLOBAL::");
                        if (r.Start == hdr.Impure)
                            Console.WriteLine("IMPURE::");
                        if (r.Start == hdr.Objects)
                            Console.WriteLine("OBJECT::");
                        if (r.Start == hdr.RoutineOffset * 8)
                            Console.WriteLine("ROUTINES::");
                        if (r.Start == hdr.Start)
                            Console.WriteLine("START::");
                        if (r.Start == hdr.StringOffset * 8)
                            Console.WriteLine("STRINGS::");
                        if (r.Start == hdr.TCharsTable)
                            Console.WriteLine("TCHARS::");
                        if (r.Start == hdr.Vocab)
                            Console.WriteLine("VOCAB::");
                    }

                    r.Value.WriteTo(Console.Out, ctx);
                    lastChunk = r.Value;
                }
            }

            //Console.ReadKey();
        }

        static void ComposeFunc(Context ctx, Stream stream, BinaryReader rdr, RangeList<Chunk> ranges, int address)
        {
            stream.Seek(address, SeekOrigin.Begin);

            int locals = rdr.ReadByte();
            var codeStart = address + 1;

            var localDefaults = new ushort[locals];
            if (ctx.ZVersion < 5)
            {
                codeStart += locals * 2;
                for (var i = 0; i < locals; i++)
                    localDefaults[i] = rdr.ReadZWord();
            }

            MeasureExtents(ctx, ranges, address, out _, out int maxExtent);

            var funct = new FunctChunk(address, codeStart - address) { Locals = localDefaults };
            ranges.AddRange(address, codeStart - address, funct);
            ranges.Coalesce(address, maxExtent,
                (int s1, int l1, Chunk v1, int s2, int l2, Chunk v2, out Chunk nv) =>
                {
                    if (v1 is FunctChunk fc)
                    {
                        fc.Add(v2);
                        nv = fc;
                        return true;
                    }

                    nv = null;
                    return false;
                });
        }

        /// <summary>
        /// Measures the start and end of the set of instructions which are
        /// reachable from a given instruction through normal program flow
        /// (ignoring function calls).
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="ranges">A range list containing the instructions to analyze.</param>
        /// <param name="startAddress">The starting address from which to measure
        /// reachability.</param>
        /// <param name="minExtent">Returns the address of the first byte of
        /// the first reachable instruction.</param>
        /// <param name="maxExtent">Returns the address of the last byte of
        /// the last reachable instruction.</param>
        static void MeasureExtents(Context ctx, RangeList<Chunk> ranges,
            int startAddress, out int minExtent, out int maxExtent)
        {
            var visited = new HashSet<int>();
            var todo = new Stack<int>();
            todo.Push(startAddress);

            minExtent = startAddress;
            maxExtent = startAddress;

            while (todo.Count > 0)
            {
                var addr = todo.Pop();

                if (visited.Contains(addr))
                    continue;
                else
                    visited.Add(addr);

                if (!ranges.TryGetValue(addr, out var chunk))
                    continue;

                if (chunk is Instruction inst)
                {
                    minExtent = Math.Min(minExtent, inst.PC);
                    maxExtent = Math.Max(maxExtent, inst.PC + inst.Length - 1);

                    if (inst.BranchType != BranchType.None &&
                        inst.BranchOffset != 0 && inst.BranchOffset != 1)
                    {
                        todo.Push(inst.PC + inst.Length + inst.BranchOffset - 2);
                    }

                    var attr = ctx.GetOpcodeInfo(inst.Op);

                    if ((attr.Flags & ZOpFlags.Terminates) == 0)
                        todo.Push(inst.PC + inst.Length);
                }
            }
        }
    }
}
