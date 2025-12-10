/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace FyreVM
{
    public partial class Engine
    {
        /// <summary>
        /// Identifies an output system for use with @setiosys.
        /// </summary>
        private enum IOSystem
        {
            /// <summary>
            /// Output is discarded.
            /// </summary>
            Null,
            /// <summary>
            /// Output is filtered through a Glulx function.
            /// </summary>
            Filter,
            /// <summary>
            /// Output is sent through FyreVM's channel system.
            /// </summary>
            Channels,
            /// <summary>
            /// Output is sent through Glk.
            /// </summary>
            /// <seealso cref="FyreVM.Engine.GlkMode"/>
            Glk,
        }

        /// <summary>
        /// Sends a single character to the output system (other than
        /// <see cref="IOSystem.Filter"/>.
        /// </summary>
        /// <param name="ch">The character to send.</param>
        private void SendCharToOutput(uint ch)
        {
            switch (outputSystem)
            {
                case IOSystem.Channels:
                    // TODO: need to handle Unicode characters larger than 16 bits?
                    outputBuffer.Write((char)ch);
                    break;

                case IOSystem.Glk:
                    if (glkMode == GlkMode.Wrapper)
                        GlkWrapperWrite(ch);
                    break;
            }
        }

        /// <summary>
        /// Sends a string to the output system (other than
        /// <see cref="IOSystem.Filter"/>.
        /// </summary>
        /// <param name="str">The string to send.</param>
        private void SendStringToOutput(string str)
        {
            switch (outputSystem)
            {
                case IOSystem.Channels:
                    outputBuffer.Write(str);
                    break;

                case IOSystem.Glk:
                    if (glkMode == GlkMode.Wrapper)
                        GlkWrapperWrite(str);
                    break;
            }
        }

        /// <summary>
        /// Sends the queued output to the <see cref="OutputReady"/> event handler.
        /// </summary>
        private void DeliverOutput()
        {
            if (OutputReady != null)
            {
                OutputReadyEventArgs args = new OutputReadyEventArgs();
                args.Package = outputBuffer.Flush();
                OutputReady(this, args);
            }
        }

        private void SelectOutputSystem(uint number, uint rock)
        {
            switch (number)
            {
                case 0:
                    outputSystem = IOSystem.Null;
                    break;
                case 1:
                    outputSystem = IOSystem.Filter;
                    filterAddress = rock;
                    break;
                case 2:
                    if (glkMode == GlkMode.None)
                        throw new VMException("Glk support is not enabled");
                    outputSystem = IOSystem.Glk;
                    break;
                case 20: // T is the 20th letter
                    outputSystem = IOSystem.Channels;
                    break;
                default:
                    throw new VMException("Unrecognized output system " + number.ToString());
            }
        }

        private void NextCStringChar()
        {
            byte ch = image.ReadByte(pc);
            pc++;

            if (ch == 0)
            {
                DonePrinting();
                return;
            }

            if (outputSystem == IOSystem.Filter)
                PerformCall(filterAddress, new uint[] { ch }, GLULX_STUB_RESUME_CSTR, 0, pc);
            else
                SendCharToOutput(ch);
        }

        private void NextUniStringChar()
        {
            uint ch = image.ReadInt32(pc);
            pc += 4;

            if (ch == 0)
            {
                DonePrinting();
                return;
            }

            if (outputSystem == IOSystem.Filter)
                PerformCall(filterAddress, new uint[] { ch }, GLULX_STUB_RESUME_UNISTR, 0, pc);
            else
                SendCharToOutput(ch);
        }

        private void NextDigit()
        {
            string num = pc.ToString();
            if (printingDigit < num.Length)
            {
                if (outputSystem == IOSystem.Filter)
                {
                    PerformCall(filterAddress, new uint[] { (uint)num[printingDigit] },
                        GLULX_STUB_RESUME_NUMBER, (uint)(printingDigit + 1), pc);
                }
                else
                {
                    // there's no reason to be here if we're not filtering output...
                    System.Diagnostics.Debug.Assert(false);

                    SendCharToOutput(num[printingDigit]);
                    printingDigit++;
                }
            }
            else
                DonePrinting();
        }

        private bool NextCompressedStringBit()
        {
            bool result = (image.ReadByte(pc) & (1 << printingDigit)) != 0;

            printingDigit++;
            if (printingDigit == 8)
            {
                printingDigit = 0;
                pc++;
            }

            return result;
        }

        #region Native String Decoding Table

        private abstract class StrNode
        {
            /// <summary>
            /// Performs the action associated with this string node: printing
            /// a character or string, terminating output, or reading a bit and
            /// delegating to another node.
            /// </summary>
            /// <param name="e">The <see cref="Engine"/> that is printing.</param>
            /// <remarks>When called on a branch node, this will consume one or
            /// more compressed string bits.</remarks>
            public abstract void HandleNextChar(Engine e);

            /// <summary>
            /// Returns the non-branch node that will handle the next string action.
            /// </summary>
            /// <param name="e">The <see cref="Engine"/> that is printing.</param>
            /// <returns>A non-branch string node.</returns>
            /// <remarks>When called on a branch node, this will consume one or
            /// more compressed string bits.</remarks>
            public virtual StrNode GetHandlingNode(Engine e)
            {
                return this;
            }

            /// <summary>
            /// Gets a value indicating whether this node requires a call stub to be
            /// pushed.
            /// </summary>
            public virtual bool NeedsCallStub
            {
                get { return false; }
            }

            /// <summary>
            /// Gets a value indicating whether this node terminates the string.
            /// </summary>
            public virtual bool IsTerminator
            {
                get { return false; }
            }

            protected void EmitChar(Engine e, char ch)
            {
                if (e.outputSystem == IOSystem.Filter)
                {
                    e.PerformCall(e.filterAddress, new uint[] { (uint)ch },
                        GLULX_STUB_RESUME_HUFFSTR, e.printingDigit, e.pc);
                }
                else
                {
                    e.SendCharToOutput(ch);
                }
            }

            protected void EmitChar(Engine e, uint ch)
            {
                if (e.outputSystem == IOSystem.Filter)
                {
                    e.PerformCall(e.filterAddress, new uint[] { ch },
                        GLULX_STUB_RESUME_HUFFSTR, e.printingDigit, e.pc);
                }
                else
                {
                    e.SendCharToOutput(ch);
                }
            }
        }

        private class EndStrNode : StrNode
        {
            public override void HandleNextChar(Engine e)
            {
                e.DonePrinting();
            }

            public override bool IsTerminator
            {
                get { return true; }
            }
        }

        private class BranchStrNode : StrNode
        {
            private readonly StrNode left, right;

            public BranchStrNode(StrNode left, StrNode right)
            {
                this.left = left;
                this.right = right;
            }

            public StrNode Left
            {
                get { return left; }
            }

            public StrNode Right
            {
                get { return right; }
            }

            public override void HandleNextChar(Engine e)
            {
                if (e.NextCompressedStringBit() == true)
                    right.HandleNextChar(e);
                else
                    left.HandleNextChar(e);
            }

            public override StrNode GetHandlingNode(Engine e)
            {
                if (e.NextCompressedStringBit() == true)
                    return right.GetHandlingNode(e);
                else
                    return left.GetHandlingNode(e);
            }
        }

        private class CharStrNode : StrNode
        {
            private readonly char ch;

            public CharStrNode(char ch)
            {
                this.ch = ch;
            }

            public char Char
            {
                get { return ch; }
            }

            public override void HandleNextChar(Engine e)
            {
                EmitChar(e, ch);
            }

            public override string ToString()
            {
                return "CharStrNode: '" + ch + "'";
            }
        }

        private class UniCharStrNode : StrNode
        {
            private readonly uint ch;

            public UniCharStrNode(uint ch)
            {
                this.ch = ch;
            }

            public uint Char
            {
                get { return ch; }
            }

            public override void HandleNextChar(Engine e)
            {
                EmitChar(e, ch);
            }

            public override string ToString()
            {
                return string.Format("UniCharStrNode: '{0}' ({1})", (char)ch, ch);
            }
        }

        private class StringStrNode : StrNode
        {
            private readonly uint address;
            private readonly ExecutionMode mode;
            private readonly string str;

            public StringStrNode(uint address, ExecutionMode mode, string str)
            {
                this.address = address;
                this.mode = mode;
                this.str = str;
            }

            public uint Address
            {
                get { return address; }
            }

            public ExecutionMode Mode
            {
                get { return mode; }
            }

            public override void HandleNextChar(Engine e)
            {
                if (e.outputSystem == IOSystem.Filter)
                {
                    e.PushCallStub(
                        new CallStub(GLULX_STUB_RESUME_HUFFSTR, e.printingDigit, e.pc, e.fp));
                    e.pc = address;
                    e.execMode = mode;
                }
                else
                {
                    e.SendStringToOutput(str);
                }
            }

            public override string ToString()
            {
                return "StringStrNode: \"" + str + "\"";
            }
        }

        private class IndirectStrNode : StrNode
        {
            private readonly uint address;
            private readonly bool dblIndirect;
            private readonly uint argCount, argsAt;

            public IndirectStrNode(uint address, bool dblIndirect,
                uint argCount, uint argsAt)
            {
                this.address = address;
                this.dblIndirect = dblIndirect;
                this.argCount = argCount;
                this.argsAt = argsAt;
            }

            public uint Address
            {
                get { return address; }
            }

            public bool DoubleIndirect
            {
                get { return DoubleIndirect; }
            }

            public uint ArgCount
            {
                get { return argCount; }
            }

            public uint ArgsAt
            {
                get { return argsAt; }
            }

            public override void HandleNextChar(Engine e)
            {
                e.PrintIndirect(
                    dblIndirect ? e.image.ReadInt32(address) : address,
                    argCount, argsAt);
            }

            public override bool NeedsCallStub
            {
                get { return true; }
            }
        }

        /// <summary>
        /// Builds a native version of the string decoding table if the table
        /// is entirely in ROM, or verifies the table's current state if the
        /// table is in RAM.
        /// </summary>
        private void CacheDecodingTable()
        {
            if (decodingTable == 0)
            {
                nativeDecodingTable = null;
                return;
            }

            uint size = image.ReadInt32(decodingTable + GLULX_HUFF_TABLESIZE_OFFSET);
            if (decodingTable + size - 1 >= image.RamStart)
            {
                // if the table is in RAM, don't cache it. just verify it now
                // and then process it directly from RAM when the time comes.
                nativeDecodingTable = null;
                VerifyDecodingTable();
                return;
            }

            uint root = image.ReadInt32(decodingTable + GLULX_HUFF_ROOTNODE_OFFSET);
            nativeDecodingTable = CacheDecodingTableNode(root);
        }

        private StrNode CacheDecodingTableNode(uint node)
        {
            if (node == 0)
                return null;

            byte nodeType = image.ReadByte(node++);

            switch (nodeType)
            {
                case GLULX_HUFF_NODE_END:
                    return new EndStrNode();

                case GLULX_HUFF_NODE_BRANCH:
                    return new BranchStrNode(
                        CacheDecodingTableNode(image.ReadInt32(node)),
                        CacheDecodingTableNode(image.ReadInt32(node + 4)));

                case GLULX_HUFF_NODE_CHAR:
                    return new CharStrNode((char)image.ReadByte(node));

                case GLULX_HUFF_NODE_UNICHAR:
                    return new UniCharStrNode(image.ReadInt32(node));

                case GLULX_HUFF_NODE_CSTR:
                    return new StringStrNode(node, ExecutionMode.CString,
                        ReadCString(node));

                case GLULX_HUFF_NODE_UNISTR:
                    return new StringStrNode(node, ExecutionMode.UnicodeString,
                        ReadUniString(node));

                case GLULX_HUFF_NODE_INDIRECT:
                    return new IndirectStrNode(image.ReadInt32(node), false, 0, 0);

                case GLULX_HUFF_NODE_INDIRECT_ARGS:
                    return new IndirectStrNode(image.ReadInt32(node), false,
                        image.ReadInt32(node + 4), node + 8);

                case GLULX_HUFF_NODE_DBLINDIRECT:
                    return new IndirectStrNode(image.ReadInt32(node), true, 0, 0);

                case GLULX_HUFF_NODE_DBLINDIRECT_ARGS:
                    return new IndirectStrNode(image.ReadInt32(node), true,
                        image.ReadInt32(node + 4), node + 8);

                default:
                    throw new VMException("Unrecognized compressed string node type " + nodeType.ToString());
            }
        }

        private string ReadCString(uint address)
        {
            StringBuilder sb = new StringBuilder();

            byte b = image.ReadByte(address);
            while (b != 0)
            {
                sb.Append((char)b);
                b = image.ReadByte(++address);
            }

            return sb.ToString();
        }

        private string ReadUniString(uint address)
        {
            StringBuilder sb = new StringBuilder();

            uint ch = image.ReadInt32(address);
            while (ch != 0)
            {
                sb.Append((char)ch);
                address += 4;
                ch = image.ReadInt32(address);
            }

            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// Checks that the string decoding table is well-formed, i.e., that it
        /// contains at least one branch, one end marker, and no unrecognized
        /// node types.
        /// </summary>
        /// <exception cref="VMException">
        /// The string decoding table is malformed.
        /// </exception>
        private void VerifyDecodingTable()
        {
            if (decodingTable == 0)
                return;

            Stack<uint> nodesToCheck = new Stack<uint>();

            uint rootNode = image.ReadInt32(decodingTable + GLULX_HUFF_ROOTNODE_OFFSET);
            nodesToCheck.Push(rootNode);

            bool foundBranch = false, foundEnd = false;

            while (nodesToCheck.Count > 0)
            {
                uint node = nodesToCheck.Pop();
                byte nodeType = image.ReadByte(node++);

                switch (nodeType)
                {
                    case GLULX_HUFF_NODE_BRANCH:
                        nodesToCheck.Push(image.ReadInt32(node));       // left child
                        nodesToCheck.Push(image.ReadInt32(node + 4));   // right child
                        foundBranch = true;
                        break;

                    case GLULX_HUFF_NODE_END:
                        foundEnd = true;
                        break;

                    case GLULX_HUFF_NODE_CHAR:
                    case GLULX_HUFF_NODE_UNICHAR:
                    case GLULX_HUFF_NODE_CSTR:
                    case GLULX_HUFF_NODE_UNISTR:
                    case GLULX_HUFF_NODE_INDIRECT:
                    case GLULX_HUFF_NODE_INDIRECT_ARGS:
                    case GLULX_HUFF_NODE_DBLINDIRECT:
                    case GLULX_HUFF_NODE_DBLINDIRECT_ARGS:
                        // OK
                        break;

                    default:
                        throw new VMException("Unrecognized compressed string node type " + nodeType.ToString());
                }
            }

            if (!foundBranch)
                throw new VMException("String decoding table contains no branches");
            if (!foundEnd)
                throw new VMException("String decoding table contains no end markers");
        }

        /// <summary>
        /// Prints the next character of a compressed string, consuming one or
        /// more bits.
        /// </summary>
        /// <remarks>This is only used when the string decoding table is in RAM.</remarks>
        private void NextCompressedChar()
        {
            uint node = image.ReadInt32(decodingTable + GLULX_HUFF_ROOTNODE_OFFSET);

            while (true)
            {
                byte nodeType = image.ReadByte(node++);

                switch (nodeType)
                {
                    case GLULX_HUFF_NODE_BRANCH:
                        if (NextCompressedStringBit() == true)
                            node = image.ReadInt32(node + 4); // go right
                        else
                            node = image.ReadInt32(node); // go left
                        break;

                    case GLULX_HUFF_NODE_END:
                        DonePrinting();
                        return;

                    case GLULX_HUFF_NODE_CHAR:
                    case GLULX_HUFF_NODE_UNICHAR:
                        uint singleChar = (nodeType == GLULX_HUFF_NODE_UNICHAR) ?
                            image.ReadInt32(node) : image.ReadByte(node);
                        if (outputSystem == IOSystem.Filter)
                        {
                            PerformCall(filterAddress, new uint[] { singleChar },
                                GLULX_STUB_RESUME_HUFFSTR, printingDigit, pc);
                        }
                        else
                        {
                            SendCharToOutput(singleChar);
                        }
                        return;

                    case GLULX_HUFF_NODE_CSTR:
                        if (outputSystem == IOSystem.Filter)
                        {
                            PushCallStub(new CallStub(GLULX_STUB_RESUME_HUFFSTR, printingDigit, pc, fp));
                            pc = node;
                            execMode = ExecutionMode.CString;
                        }
                        else
                        {
                            for (byte ch = image.ReadByte(node); ch != 0; ch = image.ReadByte(++node))
                                SendCharToOutput(ch);
                        }
                        return;

                    case GLULX_HUFF_NODE_UNISTR:
                        if (outputSystem == IOSystem.Filter)
                        {
                            PushCallStub(new CallStub(GLULX_STUB_RESUME_UNISTR, printingDigit, pc, fp));
                            pc = node;
                            execMode = ExecutionMode.UnicodeString;
                        }
                        else
                        {
                            for (uint ch = image.ReadInt32(node); ch != 0; node += 4, ch = image.ReadInt32(node))
                                SendCharToOutput(ch);
                        }
                        return;

                    case GLULX_HUFF_NODE_INDIRECT:
                        PrintIndirect(image.ReadInt32(node), 0, 0);
                        return;

                    case GLULX_HUFF_NODE_INDIRECT_ARGS:
                        PrintIndirect(image.ReadInt32(node), image.ReadInt32(node + 4), node + 8);
                        return;

                    case GLULX_HUFF_NODE_DBLINDIRECT:
                        PrintIndirect(image.ReadInt32(image.ReadInt32(node)), 0, 0);
                        return;

                    case GLULX_HUFF_NODE_DBLINDIRECT_ARGS:
                        PrintIndirect(image.ReadInt32(image.ReadInt32(node)), image.ReadInt32(node + 4), node + 8);
                        return;

                    default:
                        throw new VMException("Unrecognized compressed string node type " + nodeType.ToString());
                }
            }
        }

        /// <summary>
        /// Prints a string, or calls a routine, when an indirect node is
        /// encountered in a compressed string.
        /// </summary>
        /// <param name="address">The address of the string or routine.</param>
        /// <param name="argCount">The number of arguments passed in.</param>
        /// <param name="argsAt">The address where the argument array is stored.</param>
        private void PrintIndirect(uint address, uint argCount, uint argsAt)
        {
            byte type = image.ReadByte(address);

            switch (type)
            {
                case 0xC0:
                case 0xC1:
                    uint[] args = new uint[argCount];
                    for (uint i = 0; i < argCount; i++)
                        args[i] = image.ReadInt32(argsAt + 4 * i);
                    PerformCall(address, args, GLULX_STUB_RESUME_HUFFSTR, printingDigit, pc);
                    break;

                case 0xE0:
                    if (outputSystem == IOSystem.Filter)
                    {
                        PushCallStub(new CallStub(GLULX_STUB_RESUME_HUFFSTR, printingDigit, pc, fp));
                        execMode = ExecutionMode.CString;
                        pc = address + 1;
                    }
                    else
                    {
                        address++;
                        for (byte ch = image.ReadByte(address); ch != 0; ch = image.ReadByte(++address))
                            SendCharToOutput(ch);
                    }
                    break;

                case 0xE1:
                    PushCallStub(new CallStub(GLULX_STUB_RESUME_HUFFSTR, printingDigit, pc, fp));
                    execMode = ExecutionMode.CompressedString;
                    pc = address + 1;
                    printingDigit = 0;
                    break;

                case 0xE2:
                    if (outputSystem == IOSystem.Filter)
                    {
                        PushCallStub(new CallStub(GLULX_STUB_RESUME_HUFFSTR, printingDigit, pc, fp));
                        execMode = ExecutionMode.UnicodeString;
                        pc = address + 4;
                    }
                    else
                    {
                        address += 4;
                        for (uint ch = image.ReadInt32(address); ch != 0; address += 4, ch = image.ReadInt32(address))
                            SendCharToOutput(ch);
                    }
                    break;

                default:
                    throw new VMException(string.Format("Invalid type for indirect printing: {0:X}h", type));
            }
        }

        private void DonePrinting()
        {
            ResumeFromCallStub(0);
        }
    }
}