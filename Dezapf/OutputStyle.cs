/* Copyright 2010-2016 Jesse McGrew
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
using Zapf;

namespace Dezapf
{
    abstract class OutputStyle
    {
        public abstract void FormatHeader(TextWriter writer, Context ctx, HeaderChunk hdr);
        public abstract void FormatFunctStart(TextWriter writer, Context ctx, FunctChunk funct);
        public abstract void FormatFunctEnd(TextWriter writer, Context ctx, FunctChunk funct);
        public abstract void FormatInstruction(TextWriter writer, Context ctx, Instruction inst);
        public abstract void FormatData(TextWriter writer, Context ctx, DataChunk data);
        public abstract void FormatGlobalsStart(TextWriter writer, Context ctx, int numGlobals);
        public abstract void FormatGlobalsEnd(TextWriter writer, Context ctx);
        public abstract void FormatGlobal(TextWriter writer, Context ctx, ushort varNum, ushort defaultValue);

        protected string FormatFlags1(Context ctx, byte flags1)
        {
            StringBuilder sb = new StringBuilder();
            if (ctx.ZVersion <= 3)
            {
                if ((flags1 & 1) != 0)
                    sb.Append("+Bit0(*)");
                if ((flags1 & 2) != 0)
                    sb.Append("+StatusTime");
                if ((flags1 & 4) != 0)
                    sb.Append("+SplitFile");
                if ((flags1 & 8) != 0)
                    sb.Append("+Bit3(*)");
                if ((flags1 & 16) != 0)
                    sb.Append("+NoStatusLine(*)");
                if ((flags1 & 32) != 0)
                    sb.Append("+CanSplitScreen(*)");
                if ((flags1 & 64) != 0)
                    sb.Append("+VariablePitch(*)");
                if ((flags1 & 128) != 0)
                    sb.Append("+Bit7(*)");
            }
            else
            {
                if ((flags1 & 1) != 0)
                    sb.Append("+SupportColor(*)");
                if ((flags1 & 2) != 0)
                    sb.Append("+SupportPictures(*)");
                if ((flags1 & 4) != 0)
                    sb.Append("+SupportBold(*)");
                if ((flags1 & 8) != 0)
                    sb.Append("+SupportItalic(*)");
                if ((flags1 & 16) != 0)
                    sb.Append("+SupportFixed(*)");
                if ((flags1 & 32) != 0)
                    sb.Append("+SupportSound(*)");
                if ((flags1 & 64) != 0)
                    sb.Append("+Bit6(*)");
                if ((flags1 & 128) != 0)
                    sb.Append("+SupportTimedInput(*)");
            }

            if (sb.Length > 0)
                sb.Remove(0, 1);
            return sb.ToString();
        }

        protected string FormatFlags2(Context ctx, ushort flags2)
        {
            StringBuilder sb = new StringBuilder();

            if ((flags2 & 1) != 0)
                sb.Append("+Transcripting");
            if ((flags2 & 2) != 0)
                sb.Append("+FixedPitch");
            if ((flags2 & 4) != 0)
                sb.Append("+RedrawStatus(*)");
            if ((flags2 & 8) != 0)
                sb.Append("+Pictures");
            if ((flags2 & 16) != 0)
                sb.Append("+Undo");
            if ((flags2 & 32) != 0)
                sb.Append("+Mouse");
            if ((flags2 & 64) != 0)
                sb.Append("+Colors");
            if ((flags2 & 128) != 0)
                sb.Append("+Sound");
            if ((flags2 & 256) != 0)
                sb.Append("+Menus");
            if ((flags2 & 512) != 0)
                sb.Append("+Bit9");
            if ((flags2 & 1024) != 0)
                sb.Append("+Bit10");
            if ((flags2 & 2048) != 0)
                sb.Append("+Bit11");
            if ((flags2 & 4096) != 0)
                sb.Append("+Bit12");
            if ((flags2 & 8192) != 0)
                sb.Append("+Bit13");
            if ((flags2 & 16384) != 0)
                sb.Append("+Bit14");
            if ((flags2 & 32768) != 0)
                sb.Append("+Bit15");

            if (sb.Length > 0)
                sb.Remove(0, 1);
            return sb.ToString();
        }
    }

    abstract class ZapOutputStyle : OutputStyle
    {
        const string INDENT = "            ";
        const string INST_LABEL = "ZC${0:X5}:   ";

        public override void FormatHeader(TextWriter writer, Context ctx, HeaderChunk hdrChunk)
        {
            Header hdr = hdrChunk.Header;

            if (ctx.ZVersion != 3)
                writer.WriteLine(INDENT + ".NEW {0}", ctx.ZVersion);

            if (ctx.ZVersion < 5)
            {
                // header is handled by ZAPF, we can't do much besides write comments
                if (ctx.ZVersion == 3)
                    writer.WriteLine(INDENT + "; Z-machine version 3");

                if ((hdr.Flags1 & 2) != 0)
                    writer.WriteLine(INDENT + ".TIME");
                writer.WriteLine(INDENT + "; Flags 1: {0} ({1})", hdr.Flags1, FormatFlags1(ctx, hdr.Flags1));
                writer.WriteLine(INDENT + "; Release {0}", hdr.Release);
                writer.WriteLine(INDENT + "; Preload size: {0}", hdr.EndLod);
                writer.WriteLine(INDENT + "; Starting address: {0}", hdr.Start);
                writer.WriteLine(INDENT + "; Vocab table: {0}", hdr.Vocab);
                writer.WriteLine(INDENT + "; Object table: {0}", hdr.Objects);
                writer.WriteLine(INDENT + "; Global table: {0}", hdr.Globals);
                writer.WriteLine(INDENT + "; Dynamic size: {0}", hdr.Impure);
                writer.WriteLine(INDENT + "; Flags 2: {0} ({1})", hdr.Flags2, FormatFlags2(ctx, hdr.Flags2));

                writer.Write(INDENT + "; Serial: ");
                foreach (byte b in hdr.Serial)
                    writer.Write((char)b);
                writer.WriteLine();

                writer.WriteLine(INDENT + "; Abbrevs table: {0}", hdr.Words);
                writer.WriteLine(INDENT + "; Length: {0} ({1}x{2})", hdr.Length * ctx.SizeFactor,
                    hdr.Length, ctx.SizeFactor);
                writer.WriteLine(INDENT + "; Checksum: ${0:X4}", hdr.Checksum);
                writer.WriteLine(INDENT + "; Interpreter number(*): {0}", hdr.TerpNum);
                writer.WriteLine(INDENT + "; Interpreter version(*): {0}", hdr.TerpVersion);
                writer.WriteLine(INDENT + "; Screen height(*): {0}", hdr.ScreenRows);
                writer.WriteLine(INDENT + "; Screen width(*): {0}", hdr.ScreenColumns);
                writer.WriteLine(INDENT + "; Screen width in units(*): {0}", hdr.ScreenWidth);
                writer.WriteLine(INDENT + "; Screen height in units(*): {0}", hdr.ScreenHeight);
                writer.WriteLine(INDENT + "; Font width(V5)/height(V6)(*): {0}", hdr.FontWidth);
                writer.WriteLine(INDENT + "; Font height(V5)/width(V6)(*): {0}", hdr.FontHeight);
                writer.WriteLine(INDENT + "; Routines offset(*): {0}", hdr.RoutineOffset * 8);
                writer.WriteLine(INDENT + "; Strings offset(*): {0}", hdr.StringOffset);
                writer.WriteLine(INDENT + "; Default BG color: {0}", hdr.DefaultBG);
                writer.WriteLine(INDENT + "; Default FG color: {0}", hdr.DefaultFG);
                writer.WriteLine(INDENT + "; Terminating chars table(*): {0}", hdr.TCharsTable);
                writer.WriteLine(INDENT + "; Buffer width(*): {0}", hdr.BufferWidth);
                writer.WriteLine(INDENT + "; Standard revision(*): {0}", hdr.StandardVersion);
                writer.WriteLine(INDENT + "; Alphabet table(*): {0}", hdr.AlphabetTable);
                writer.WriteLine(INDENT + "; Header extension table(*): {0}", hdr.ExtensionTable);

                writer.Write(INDENT + "; Username(*): ");
                foreach (byte b in hdr.Username)
                    writer.Write((char)b);
                writer.WriteLine();

                writer.Write(INDENT + "; Creator ver: ");
                foreach (byte b in hdr.Creator)
                    writer.Write((char)b);
                writer.WriteLine();
            }
            else
            {
                // header has to be assembled in code
                //XXX
                throw new NotImplementedException();
            }
        }

        public override void FormatFunctStart(TextWriter writer, Context ctx, FunctChunk funct)
        {
            writer.Write(INDENT + ".FUNCT ZR${0:X5}", funct.PC);
            for (int i = 0; i < funct.Locals.Length; i++)
            {
                writer.Write(',');
                writer.Write(FormatVariable((ushort)(i + 1)));
                if (ctx.ZVersion < 5)
                {
                    if (funct.Locals[i] != 0)
                        writer.Write("={0}", funct.Locals[i]);
                }
            }
            writer.WriteLine();
        }

        public override void FormatFunctEnd(TextWriter writer, Context ctx, FunctChunk funct)
        {
            // nada
        }

        public override void FormatInstruction(TextWriter writer, Context ctx, Instruction inst)
        {
            // label or indent
            if (inst.Parent == null)
                writer.Write(INDENT);
            else
                writer.Write(INST_LABEL, inst.PC);

            ZOpAttribute attr = ctx.GetOpcodeInfo(inst.Op);

            // instruction name
            writer.Write(attr.ClassicName);

            // regular operands
            ushort[] operands = inst.Operands;
            OperandType[] otypes = inst.OperandTypes;

            bool any = false;
            for (int i = 0; i < otypes.Length; i++)
            {
                switch (otypes[i])
                {
                    case OperandType.Word:
                    case OperandType.Byte:
                        if (!any)
                        {
                            writer.Write(' ');
                            any = true;
                        }
                        else
                        {
                            writer.Write(',');
                        }
                        if (i == 0)
                        {
                            if ((attr.Flags & ZOpFlags.Call) != 0)
                            {
                                writer.Write("ZR${0:X4}", operands[i]);
                                continue;
                            }
                            if ((attr.Flags & ZOpFlags.Label) != 0)
                            {
                                writer.Write("ZC${0:X5}", inst.PC + inst.Length + (short)operands[i] - 2);
                                continue;
                            }
                            if ((attr.Flags & ZOpFlags.IndirectVar) != 0)
                            {
                                writer.Write('\'');
                                writer.Write(FormatVariable(operands[i]));
                                continue;
                            }
                        }
                        writer.Write((short)operands[i]);
                        break;
                    case OperandType.Variable:
                        if (!any)
                        {
                            writer.Write(' ');
                            any = true;
                        }
                        else
                        {
                            writer.Write(',');
                        }
                        writer.Write(FormatVariable(operands[i]));
                        break;
                }
            }

            // text
            ushort[] text = inst.EncodedText;
            if (text != null)
            {
                writer.Write(" \"");

                foreach (char c in ctx.DecodeText(text))
                    switch (c)
                    {
                        case '"':
                            writer.Write("\"\"");
                            break;
                        default:
                            writer.Write(c);
                            break;
                    }

                writer.Write('"');
            }

            // store target
            if (inst.StoreTarget != null)
            {
                writer.Write(" >");
                writer.Write(FormatVariable(inst.StoreTarget.Value));
            }

            // branch target
            if (inst.BranchType != BranchType.None)
            {
                switch (inst.BranchType)
                {
                    case BranchType.LongNegative:
                    case BranchType.ShortNegative:
                        writer.Write(" \\");
                        break;
                    case BranchType.LongPositive:
                    case BranchType.ShortPositive:
                        writer.Write(" /");
                        break;
                }

                switch (inst.BranchOffset)
                {
                    case 0:
                        writer.Write("FALSE");
                        break;
                    case 1:
                        writer.Write("TRUE");
                        break;
                    default:
                        writer.Write("ZC${0:X5}", inst.PC + inst.Length + inst.BranchOffset - 2);
                        break;
                }
            }

            writer.WriteLine();
        }

        protected static string FormatVariable(ushort num)
        {
            if (num == 0)
                return "STACK";
            else if (num < 16)
                return string.Format("L{0:00}", num);
            else
                return string.Format("G{0:00}", num);
        }

        public override void FormatData(TextWriter writer, Context ctx, DataChunk data)
        {
            const int PERLINE = 16;

            writer.Write(INDENT);

            byte[] bytes = data.Bytes;
            for (int i = 0; i < data.Bytes.Length; i++)
            {
                if (i % PERLINE == 0)
                {
                    if (i != 0)
                    {
                        writer.WriteLine();
                        writer.Write(INDENT);
                    }
                    writer.Write(".BYTE ");
                }
                else
                {
                    writer.Write(',');
                }

                writer.Write(data.Bytes[i]);
            }

            writer.WriteLine();
        }

        public override void FormatGlobalsStart(TextWriter writer, Context ctx, int numGlobals)
        {
            writer.WriteLine(INDENT + ".TABLE {0}", numGlobals * 2);
        }

        public override void FormatGlobalsEnd(TextWriter writer, Context ctx)
        {
            writer.WriteLine(INDENT + ".ENDT");
        }

        public override void FormatGlobal(TextWriter writer, Context ctx, ushort varNum, ushort defaultValue)
        {
            writer.WriteLine(INDENT + ".GVAR {0}={1}", FormatVariable(varNum), defaultValue);
        }
    }

    /// <summary>
    /// Produces ZAP code that ZAPF can assemble into something identical to
    /// the input file.
    /// </summary>
    /// <remarks>
    /// This will preserve instruction and jump forms, padding bytes, header
    /// fields, etc., but may only provide limited insight into the code.
    /// Some sections (potentially even the entire file) may be translated
    /// as .BYTE/.WORD directives rather than instructions which ZAPF would
    /// assemble differently.
    /// </remarks>
    class ZapRoundTripStyle : ZapOutputStyle
    {
    }

    /// <summary>
    /// Produces ZAP code that ZAPF can assemble into something functionally
    /// equivalent to the input file.
    /// </summary>
    /// <remarks>
    /// Some details such as instruction and jump forms, padding bytes
    /// between routines, and the header checksum may be overlooked in order
    /// to provide more insight: e.g. a valid instruction in the input will
    /// translate to a ZAP instruction in the output, never .BYTE/.WORD.
    /// </remarks>
    class ZapFunctionalStyle : ZapOutputStyle
    {
    }

    /// <summary>
    /// Produces ZAP code that may be more succinct or insightful but may not
    /// be sufficient to assemble a functional equivalent of the input file.
    /// </summary>
    /// <remarks>
    /// No .BYTE/.WORD directives will be produced: instead, data sections will
    /// be translated as comments or not at all.
    /// </remarks>
    class ZapHumanStyle : ZapOutputStyle
    {
    }

    /// <summary>
    /// Produces Inform code that may not be sufficient to compile anything
    /// close to the input file.
    /// </summary>
    class InformStyle : OutputStyle
    {
        const string INDENT = "            ";
        const string INST_LABEL = ".zc_{0:x5};  ";

        public override void FormatHeader(TextWriter writer, Context ctx, HeaderChunk hdrChunk)
        {
            Header hdr = hdrChunk.Header;

            // header is handled by Inform, we can't do much besides write comments
            writer.WriteLine("Switches v{0};", hdr.ZVersion);
            if ((hdr.Flags1 & 2) != 0)
                writer.WriteLine("Statusline time;");
            writer.WriteLine("Release {0};", hdr.Release);
            writer.Write("Serial \"");
            foreach (byte b in hdr.Serial)
                writer.Write((char)b);
            writer.WriteLine("\";");

            writer.WriteLine("! Flags 1: {0} ({1})", hdr.Flags1, FormatFlags1(ctx, hdr.Flags1));
            writer.WriteLine("! Preload size: {0}", hdr.EndLod);
            writer.WriteLine("! Starting address: {0}", hdr.Start);
            writer.WriteLine("! Vocab table: {0}", hdr.Vocab);
            writer.WriteLine("! Object table: {0}", hdr.Objects);
            writer.WriteLine("! Global table: {0}", hdr.Globals);
            writer.WriteLine("! Dynamic size: {0}", hdr.Impure);
            writer.WriteLine("! Flags 2: {0} ({1})", hdr.Flags2, FormatFlags2(ctx, hdr.Flags2));
            writer.WriteLine("! Abbrevs table: {0}", hdr.Words);
            writer.WriteLine("! Length: {0} ({1}x{2})", hdr.Length * ctx.SizeFactor,
                hdr.Length, ctx.SizeFactor);
            writer.WriteLine("! Checksum: ${0:X4}", hdr.Checksum);
            writer.WriteLine("! Interpreter number(*): {0}", hdr.TerpNum);
            writer.WriteLine("! Interpreter version(*): {0}", hdr.TerpVersion);
            writer.WriteLine("! Screen height(*): {0}", hdr.ScreenRows);
            writer.WriteLine("! Screen width(*): {0}", hdr.ScreenColumns);
            writer.WriteLine("! Screen width in units(*): {0}", hdr.ScreenWidth);
            writer.WriteLine("! Screen height in units(*): {0}", hdr.ScreenHeight);
            writer.WriteLine("! Font width(V5)/height(V6)(*): {0}", hdr.FontWidth);
            writer.WriteLine("! Font height(V5)/width(V6)(*): {0}", hdr.FontHeight);
            writer.WriteLine("! Routines offset(*): {0}", hdr.RoutineOffset * 8);
            writer.WriteLine("! Strings offset(*): {0}", hdr.StringOffset);
            writer.WriteLine("! Default BG color: {0}", hdr.DefaultBG);
            writer.WriteLine("! Default FG color: {0}", hdr.DefaultFG);
            writer.WriteLine("! Terminating chars table(*): {0}", hdr.TCharsTable);
            writer.WriteLine("! Buffer width(*): {0}", hdr.BufferWidth);
            writer.WriteLine("! Standard revision(*): {0}", hdr.StandardVersion);
            writer.WriteLine("! Alphabet table(*): {0}", hdr.AlphabetTable);
            writer.WriteLine("! Header extension table(*): {0}", hdr.ExtensionTable);

            writer.Write("! Username(*): ");
            foreach (byte b in hdr.Username)
                writer.Write((char)b);
            writer.WriteLine();

            writer.Write("! Creator ver: ");
            foreach (byte b in hdr.Creator)
                writer.Write((char)b);
            writer.WriteLine();
        }

        public override void FormatFunctStart(TextWriter writer, Context ctx, FunctChunk funct)
        {
            writer.Write("[ ZR_{0:x5}", funct.PC);
            bool foundDefaults = false;
            for (int i = 0; i < funct.Locals.Length; i++)
            {
                writer.Write(" L{0:00}", i + 1);
                if (ctx.ZVersion < 5 && funct.Locals[i] != 0)
                    foundDefaults = true;
            }
            writer.Write(';');

            if (foundDefaults)
            {
                writer.WriteLine();
                writer.Write(INDENT);
                int count = 0;
                for (int i = 0; i < funct.Locals.Length; i++)
                {
                    if (funct.Locals[i] != 0)
                    {
                        if (++count > 0)
                            writer.Write(' ');
                        writer.Write("L{0:00}={1};", i + 1, funct.Locals[i]);
                    }
                }
            }

            writer.WriteLine();
        }

        public override void FormatFunctEnd(TextWriter writer, Context ctx, FunctChunk funct)
        {
            writer.WriteLine("\n];");
        }

        public override void FormatInstruction(TextWriter writer, Context ctx, Instruction inst)
        {
            // label or indent
            if (inst.Parent == null)
                writer.Write(INDENT);
            else
                writer.Write(INST_LABEL, inst.PC);

            ZOpAttribute attr = ctx.GetOpcodeInfo(inst.Op);

            // instruction name
            writer.Write('@');
            writer.Write(attr.InformName);

            // regular operands
            ushort[] operands = inst.Operands;
            OperandType[] otypes = inst.OperandTypes;

            for (int i = 0; i < otypes.Length; i++)
            {
                switch (otypes[i])
                {
                    case OperandType.Word:
                    case OperandType.Byte:
                        writer.Write(' ');
                        writer.Write((short)operands[i]);
                        break;
                    case OperandType.Variable:
                        writer.Write(' ');
                        writer.Write(FormatVariable(operands[i]));
                        break;
                }
            }

            // text
            ushort[] text = inst.EncodedText;
            if (text != null)
            {
                writer.Write(" \"");

                foreach (char c in ctx.DecodeText(text))
                    switch (c)
                    {
                        case '\n':
                            writer.Write('^');
                            break;
                        case '"':
                            writer.Write('\"');
                            break;
                        default:
                            //XXX handle @ and other characters that need escaping
                            writer.Write(c);
                            break;
                    }

                writer.Write('"');
            }

            // store target
            if (inst.StoreTarget != null)
            {
                writer.Write(" -> ");
                writer.Write(FormatVariable(inst.StoreTarget.Value));
            }

            // branch target
            if (inst.BranchType != BranchType.None)
            {
                writer.Write(" ?");
                switch (inst.BranchType)
                {
                    case BranchType.LongNegative:
                    case BranchType.ShortNegative:
                        writer.Write('~');
                        break;
                }

                switch (inst.BranchOffset)
                {
                    case 0:
                        writer.Write("rfalse");
                        break;
                    case 1:
                        writer.Write("rtrue");
                        break;
                    default:
                        writer.Write("zc_{0:x5}", inst.PC + inst.Length + inst.BranchOffset - 2);
                        break;
                }
            }

            // done
            writer.WriteLine(';');
        }

        static string FormatVariable(ushort num)
        {
            if (num == 0)
                return "sp";
            else if (num < 16)
                return string.Format("L{0:00}", num);
            else
                return string.Format("G{0:00}", num);
        }

        public override void FormatData(TextWriter writer, Context ctx, DataChunk data)
        {
            const int PERLINE = 16;

            writer.Write("Array array_{0:4x} ->", data.PC);

            byte[] bytes = data.Bytes;
            if (bytes.Length == 1)
            {
                writer.Write(" (");
                writer.Write(bytes[0]);
                writer.Write(')');
            }
            else
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (i % PERLINE == 0)
                    {
                        if (i != 0)
                        {
                            writer.WriteLine();
                            writer.Write(INDENT);
                        }
                    }
                    else
                    {
                        writer.Write(' ');
                    }

                    writer.Write(bytes[i]);
                }
            }

            writer.WriteLine(';');
        }

        public override void FormatGlobalsStart(TextWriter writer, Context ctx, int numGlobals)
        {
            // nada
        }

        public override void FormatGlobalsEnd(TextWriter writer, Context ctx)
        {
            // nada
        }

        public override void FormatGlobal(TextWriter writer, Context ctx, ushort varNum, ushort defaultValue)
        {
            writer.WriteLine("Global {0} = {1};", FormatVariable(varNum), defaultValue);
        }
    }
}
