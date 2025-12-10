/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace FyreVM
{
    /// <summary>
    /// Provides data for an input line request event.
    /// </summary>
    public class LineWantedEventArgs : EventArgs
    {
        private string line;

        /// <summary>
        /// Gets or sets the line of input that was read, or <b>null</b> to cancel.
        /// </summary>
        public string Line
        {
            get { return line; }
            set { line = value; }
        }
    }

    /// <summary>
    /// A delegate that handles the <see cref="Engine.LineWanted"/> event.
    /// </summary>
    /// <param name="sender">The <see cref="Engine"/> raising the event.</param>
    /// <param name="e">The event arguments.</param>
    public delegate void LineWantedEventHandler(object sender, LineWantedEventArgs e);

    /// <summary>
    /// Provides data for an input character request event.
    /// </summary>
    public class KeyWantedEventArgs : EventArgs
    {
        private char ch;

        /// <summary>
        /// Gets or sets the character that was read, or '\0' to cancel.
        /// </summary>
        public char Char
        {
            get { return ch; }
            set { ch = value; }
        }
    }

    /// <summary>
    /// A delegate that handles the <see cref="Engine.KeyWanted"/> event.
    /// </summary>
    /// <param name="sender">The <see cref="Engine"/> raising the event.</param>
    /// <param name="e">The event arguments.</param>
    public delegate void KeyWantedEventHandler(object sender, KeyWantedEventArgs e);

    /// <summary>
    /// Provides data for an output event.
    /// </summary>
    public class OutputReadyEventArgs : EventArgs
    {
        private IDictionary<string, string> package;

        /// <summary>
        /// Gets or sets a dictionary containing the text that has been
        /// captured on each output channel since the last output delivery.
        /// </summary>
        public IDictionary<string, string> Package
        {
            get { return package; }
            set { package = value; }
        }
    }

    /// <summary>
    /// A delegate that handles the <see cref="Engine.OutputReady"/> event.
    /// </summary>
    /// <param name="sender">The <see cref="Engine"/> raising the event.</param>
    /// <param name="e">The event arguments.</param>
    public delegate void OutputReadyEventHandler(object sender, OutputReadyEventArgs e);

    /// <summary>
    /// Provides data for a save or restore event.
    /// </summary>
    public class SaveRestoreEventArgs : EventArgs
    {
        private Stream stream;

        /// <summary>
        /// Gets or sets the stream to use for saving or restoring the game
        /// state. This stream will be closed by the interpreter after the
        /// save or load process finishes. (A value of <see langword="null"/>
        /// indicates that the save/load process was aborted.)
        /// </summary>
        public Stream Stream
        {
            get { return stream; }
            set { stream = value; }
        }
    }

    /// <summary>
    /// A delegate that handles the <see cref="Engine.SaveRequested"/> or
    /// <see cref="Engine.LoadRequested"/> event.
    /// </summary>
    /// <param name="sender">The <see cref="Engine"/> raising the event.</param>
    /// <param name="e">The event arguments.</param>
    public delegate void SaveRestoreEventHandler(object sender, SaveRestoreEventArgs e);

    public class TransitionEventArgs : EventArgs
    {
        private string message;

        public string Message { get { return message; } set { message = value; } }
    }

    public delegate void TransitionRequestedEventHandler(object sender, TransitionEventArgs e);

    /// <summary>
    /// Describes the type of Glk support offered by the interpreter.
    /// </summary>
    public enum GlkMode
    {
        /// <summary>
        /// No Glk support.
        /// </summary>
        None,
        /// <summary>
        /// A minimal Glk implementation, with I/O functions mapped to the channel system.
        /// </summary>
        Wrapper,
    }

    /// <summary>
    /// The main FyreVM class, which implements a modified Glulx interpreter.
    /// </summary>
    public partial class Engine
    {
        public enum VMRequestType
        {
            StartGame,
            EnterCommand
        }
        /// <summary>
        /// Describes the task that the interpreter is currently performing.
        /// </summary>
        private enum ExecutionMode
        {
            /// <summary>
            /// We are running function code. PC points to the next instruction.
            /// </summary>
            Code,
            /// <summary>
            /// We are printing a null-terminated string (E0). PC points to the
            /// next character.
            /// </summary>
            CString,
            /// <summary>
            /// We are printing a compressed string (E1). PC points to the next
            /// compressed byte, and printingDigit is the bit position (0-7).
            /// </summary>
            CompressedString,
            /// <summary>
            /// We are printing a Unicode string (E2). PC points to the next
            /// character.
            /// </summary>
            UnicodeString,
            /// <summary>
            /// We are printing a decimal number. PC contains the number, and
            /// printingDigit is the next digit, starting at 0 (for the first
            /// digit or minus sign).
            /// </summary>
            Number,
            /// <summary>
            /// We are returning control to <see cref="Engine.NestedCall"/>
            /// after engine code has called a Glulx function.
            /// </summary>
            Return,
        }

        /// <summary>
        /// Represents a Glulx call stub, which describes the VM's state at
        /// the time of a function call or string printing task.
        /// </summary>
        private struct CallStub
        {
            /// <summary>
            /// The type of storage location (for function calls) or the
            /// previous task (for string printing).
            /// </summary>
            public uint DestType;
            /// <summary>
            /// The storage address (for function calls) or the digit
            /// being examined (for string printing).
            /// </summary>
            public uint DestAddr;
            /// <summary>
            /// The address of the opcode or character at which to resume.
            /// </summary>
            public uint PC;
            /// <summary>
            /// The stack frame in which the function call or string printing
            /// was performed.
            /// </summary>
            public uint FramePtr;

            /// <summary>
            /// Initializes a new call stub.
            /// </summary>
            /// <param name="destType">The stub type.</param>
            /// <param name="destAddr">The storage address or printing digit.</param>
            /// <param name="pc">The address of the opcode or character at which to resume.</param>
            /// <param name="framePtr">The stack frame pointer.</param>
            public CallStub(uint destType, uint destAddr, uint pc, uint framePtr)
            {
                this.DestType = destType;
                this.DestAddr = destAddr;
                this.PC = pc;
                this.FramePtr = framePtr;
            }
        }

        #region Glulx constants

        // Header size and field offsets
        public const int GLULX_HDR_SIZE = 36;
        public const int GLULX_HDR_MAGIC_OFFSET = 0;
        public const int GLULX_HDR_VERSION_OFFSET = 4;
        public const int GLULX_HDR_RAMSTART_OFFSET = 8;
        public const int GLULX_HDR_EXTSTART_OFFSET = 12;
        public const int GLULX_HDR_ENDMEM_OFFSET = 16;
        public const int GLULX_HDR_STACKSIZE_OFFSET = 20;
        public const int GLULX_HDR_STARTFUNC_OFFSET = 24;
        public const int GLULX_HDR_DECODINGTBL_OFFSET = 28;
        public const int GLULX_HDR_CHECKSUM_OFFSET = 32;

        // Call stub: DestType values for function calls
        public const int GLULX_STUB_STORE_NULL = 0;
        public const int GLULX_STUB_STORE_MEM = 1;
        public const int GLULX_STUB_STORE_LOCAL = 2;
        public const int GLULX_STUB_STORE_STACK = 3;

        // Call stub: DestType values for string printing
        public const int GLULX_STUB_RESUME_HUFFSTR = 10;
        public const int GLULX_STUB_RESUME_FUNC = 11;
        public const int GLULX_STUB_RESUME_NUMBER = 12;
        public const int GLULX_STUB_RESUME_CSTR = 13;
        public const int GLULX_STUB_RESUME_UNISTR = 14;

        // FyreVM addition: DestType value for nested calls
        public const int FYREVM_STUB_RESUME_NATIVE = 99;

        // String decoding table: header field offsets
        public const int GLULX_HUFF_TABLESIZE_OFFSET = 0;
        public const int GLULX_HUFF_NODECOUNT_OFFSET = 4;
        public const int GLULX_HUFF_ROOTNODE_OFFSET = 8;

        // String decoding table: node types
        public const int GLULX_HUFF_NODE_BRANCH = 0;
        public const int GLULX_HUFF_NODE_END = 1;
        public const int GLULX_HUFF_NODE_CHAR = 2;
        public const int GLULX_HUFF_NODE_CSTR = 3;
        public const int GLULX_HUFF_NODE_UNICHAR = 4;
        public const int GLULX_HUFF_NODE_UNISTR = 5;
        public const int GLULX_HUFF_NODE_INDIRECT = 8;
        public const int GLULX_HUFF_NODE_DBLINDIRECT = 9;
        public const int GLULX_HUFF_NODE_INDIRECT_ARGS = 10;
        public const int GLULX_HUFF_NODE_DBLINDIRECT_ARGS = 11;

        #endregion

        private const string LATIN1_CODEPAGE = "latin1";//28591;

        #region Dictionary of opcodes

        private readonly Dictionary<uint, Opcode> opcodeDict = new Dictionary<uint, Opcode>();
        private readonly Opcode[] quickOpcodes = new Opcode[0x80];

        private void InitOpcodeDict()
        {
            MethodInfo[] methods = typeof(Engine).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (MethodInfo mi in methods)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(OpcodeAttribute), false);
                if (attrs.Length > 0)
                {
                    OpcodeAttribute attr = (OpcodeAttribute)(attrs[0]);
                    Delegate handler = Delegate.CreateDelegate(typeof(OpcodeHandler), this, mi);
                    opcodeDict.Add(attr.Number, new Opcode(attr, (OpcodeHandler)handler));
                }
            }
        }

        #endregion

        private const uint FIRST_MAJOR_VERSION = 2;
        private const uint FIRST_MINOR_VERSION = 0;
        private const uint LAST_MAJOR_VERSION = 3;
        private const uint LAST_MINOR_VERSION = 1;

        private const int MAX_UNDO_LEVEL = 3;

        // persistent machine state (written to save file)
        private UlxImage image;
        private byte[] stack;
        private uint pc, sp, fp; // program counter, stack ptr, call-frame ptr
        private HeapAllocator heap;

        // transient state
        private uint frameLen, localsPos; // updated along with FP
        private IOSystem outputSystem;
        private GlkMode glkMode = GlkMode.None;
        private OutputBuffer outputBuffer;
        private uint filterAddress;
        private uint decodingTable;
        private StrNode nativeDecodingTable;
        private ExecutionMode execMode;
        private byte printingDigit; // bit number for compressed strings, digit for numbers
        private Random randomGenerator = new Random();
        private List<MemoryStream> undoBuffers = new List<MemoryStream>();
        private uint protectionStart, protectionLength; // relative to start of RAM!
        private bool running;
        private uint nestedResult;
        private int nestingLevel;
        private Veneer veneer = new Veneer();
        private uint maxHeapSize;

        /// <summary>
        /// Initializes a new instance of the VM from a game file.
        /// </summary>
        /// <param name="gameFile">A stream containing the ROM and
        /// initial RAM.</param>
        public Engine(Stream gameFile)
        {
            image = new UlxImage(gameFile);
            outputBuffer = new OutputBuffer();

            uint version = (uint)image.ReadInt32(GLULX_HDR_VERSION_OFFSET);
            uint major = version >> 16;
            uint minor = (version >> 8) & 0xFF;

            if (major < FIRST_MAJOR_VERSION ||
                (major == FIRST_MAJOR_VERSION && minor < FIRST_MINOR_VERSION) ||
                major > LAST_MAJOR_VERSION ||
                (major == LAST_MAJOR_VERSION && minor > LAST_MINOR_VERSION))
                throw new ArgumentException("Game version is out of the supported range");

            uint stacksize = (uint)image.ReadInt32(GLULX_HDR_STACKSIZE_OFFSET);
            stack = new byte[stacksize];

            InitOpcodeDict();
        }


        /// <summary>
        /// Initializes a new instance of the VM from a saved state and the
        /// associated game file.
        /// </summary>
        /// <param name="gameFile">A stream containing the ROM and
        /// initial RAM.</param>
        /// <param name="saveFile">A stream containing a <see cref="Quetzal"/>
        /// state that was saved by the specified game file.</param>
        public Engine(Stream gameFile, Stream saveFile)
            : this(gameFile)
        {
            LoadFromStream(saveFile);
        }

        /// <summary>
        /// Raised when the VM wants to read a line of input. The handler may
        /// return a string or indicate that input was canceled.
        /// </summary>
        public event LineWantedEventHandler LineWanted;

        /// <summary>
        /// Raised when the VM wants to read a single character of input.
        /// The handler may return a character or indicate that input was
        /// canceled.
        /// </summary>
        public event KeyWantedEventHandler KeyWanted;

        /// <summary>
        /// Raised when queued output is being delivered, i.e. before
        /// requesting input or terminating.
        /// </summary>
        public event OutputReadyEventHandler OutputReady;

        /// <summary>
        /// Raised when the VM needs a stream to use for saving the current
        /// state.
        /// </summary>
        public event SaveRestoreEventHandler SaveRequested;

        /// <summary>
        /// Raised when the VM needs a stream to use for restoring a previous
        /// state.
        /// </summary>
        public event SaveRestoreEventHandler LoadRequested;

        /// <summary>
        /// Raised when the game requests a physical device transition. The host device can handle in a native manner.
        /// This happens instead of fusging for a keypress.
        /// </summary>
        public event TransitionRequestedEventHandler TransitionRequested;

        /// <summary>
        /// Gets or sets a value limiting the maximum size of the Glulx heap,
        /// in bytes, or zero to indicate an unlimited heap size.
        /// </summary>
        public uint MaxHeapSize
        {
            get { return maxHeapSize; }
            set { maxHeapSize = value; if (heap != null) heap.MaxSize = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating what type of Glk support will be offered.
        /// </summary>
        public GlkMode GlkMode
        {
            get { return glkMode; }
            set { glkMode = value; }
        }

        private void Push(uint value)
        {
            BigEndian.WriteInt32(stack, sp, value);
            sp += 4;
        }

        private void WriteToStack(uint position, uint value)
        {
            BigEndian.WriteInt32(stack, position, value);
        }

        private uint Pop()
        {
            sp -= 4;
            return BigEndian.ReadInt32(stack, sp);
        }

        private uint ReadFromStack(uint position)
        {
            return BigEndian.ReadInt32(stack, position);
        }

        private void PushCallStub(CallStub stub)
        {
            Push(stub.DestType);
            Push(stub.DestAddr);
            Push(stub.PC);
            Push(stub.FramePtr);
        }

        private CallStub PopCallStub()
        {
            CallStub stub;

            stub.FramePtr = Pop();
            stub.PC = Pop();
            stub.DestAddr = Pop();
            stub.DestType = Pop();

            return stub;
        }

        private static StringBuilder debugBuffer = new StringBuilder();

        [Conditional("TRACEOPS")]
        private static void WriteTrace(string str)
        {
            lock (debugBuffer)
            {
                debugBuffer.Append(str);

                if (str.Contains("\n"))
                {
                    string x = debugBuffer.ToString();

                    do
                    {
                        int pos = x.IndexOf('\n');
                        string line = x.Substring(0, pos).TrimEnd();

                        Debug.WriteLine(line);

                        if (pos == x.Length - 1)
                            x = "";
                        else
                            x = x.Substring(pos + 1);
                    } while (x.Contains("\n"));

                    //Debug.WriteLine(debugBuffer.ToString());
                    debugBuffer.Length = 0;
                    debugBuffer.Append(x);
                }
            }
        }

        /// <summary>
        /// Clears the stack and initializes VM registers from values found in RAM.
        /// </summary>
        private void Bootstrap()
        {
            uint mainfunc = image.ReadInt32(GLULX_HDR_STARTFUNC_OFFSET);
            decodingTable = image.ReadInt32(GLULX_HDR_DECODINGTBL_OFFSET);

            sp = fp = frameLen = localsPos = 0;
            outputSystem = IOSystem.Null;
            execMode = ExecutionMode.Code;
            EnterFunction(mainfunc);
        }

        /// <summary>
        /// Starts the interpreter.
        /// </summary>
        /// <remarks>
        /// This method does not return until the game finishes, either by
        /// returning from the main function or with the quit opcode.
        /// </remarks>
        /// 
      public  Boolean _IsCurrentRestore = false;
        public void Run()
        {
            running = true;
           
#if PROFILING
            cycles = 0;
#endif

            // initialize registers and stack
            Bootstrap();
            CacheDecodingTable();

            // run the game
            if (_IsCurrentRestore == false)
            {
                InterpreterLoop();
            }
           

            // send any output that may be left
            DeliverOutput();
          
        }

        public void Continue()
        {
            running = true;
       
#if PROFILING
            cycles = 0;
#endif
            // run the game
            InterpreterLoop();

            // send any output that may be left
            DeliverOutput();
         
        }

        private uint NestedCall(uint address)
        {
            return NestedCall(address, null);
        }

        private uint NestedCall(uint address, uint arg0)
        {
            funcargs1[0] = arg0;
            return NestedCall(address, funcargs1);
        }

        private uint NestedCall(uint address, uint arg0, uint arg1)
        {
            funcargs2[0] = arg0;
            funcargs2[1] = arg1;
            return NestedCall(address, funcargs2);
        }

        private uint NestedCall(uint address, uint arg0, uint arg1, uint arg2)
        {
            funcargs3[0] = arg0;
            funcargs3[1] = arg1;
            funcargs3[2] = arg2;
            return NestedCall(address, funcargs3);
        }

        /// <summary>
        /// Executes a Glulx function and returns its result.
        /// </summary>
        /// <param name="address">The address of the function.</param>
        /// <param name="args">The list of arguments, or <see langword="null"/>
        /// if no arguments need to be passed in.</param>
        /// <returns>The function's return value.</returns>
        private uint NestedCall(uint address, params uint[] args)
        {
            ExecutionMode oldMode = execMode;
            byte oldDigit = printingDigit;

            PerformCall(address, args, FYREVM_STUB_RESUME_NATIVE, 0);
            nestingLevel++;
            try
            {
                InterpreterLoop();
            }
            finally
            {
                nestingLevel--;
                execMode = oldMode;
                printingDigit = oldDigit;
            }

            return nestedResult;
        }

        /// <summary>
        /// Runs the main interpreter loop.
        /// </summary>
        private void InterpreterLoop()
        {
            try
            {
               
            
            const int MAX_OPERANDS = 8;
            uint[] operands = new uint[MAX_OPERANDS];
            uint[] resultAddrs = new uint[MAX_OPERANDS];
            byte[] resultTypes = new byte[MAX_OPERANDS];

            while (running)
            {
                switch (execMode)
                {
                    case ExecutionMode.Code:
                        // decode opcode number
                       
                        uint opnum = image.ReadByte(pc);
                     
                        if (opnum >= 0xC0)
                        {
                            opnum = image.ReadInt32(pc) - 0xC0000000;
                            pc += 4;
                        }
                        else if (opnum >= 0x80)
                        {
                            opnum = (uint)(image.ReadInt16(pc) - 0x8000);
                            pc += 2;
                        }
                        else
                        {
                            pc++;
                        }

                        // look up opcode info
                        Opcode opcode;
                        try
                        {
                            opcode = opcodeDict[opnum];
                            WriteTrace("[" + opcode.ToString());
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new VMException(string.Format("Unrecognized opcode {0:X}h", opnum));
                        }

                        // decode load-operands
                        uint opcount = (uint)(opcode.Attr.LoadArgs + opcode.Attr.StoreArgs);
                        if (opcode.Attr.Rule == OpcodeRule.DelayedStore)
                            opcount++;
                        else if (opcode.Attr.Rule == OpcodeRule.Catch)
                            opcount += 2;
                        uint operandPos = pc + (opcount + 1) / 2;

                        for (int i = 0; i < opcode.Attr.LoadArgs; i++)
                        {
                            byte type;
                            if (i % 2 == 0)
                            {
                                type = (byte)(image.ReadByte(pc) & 0xF);
                            }
                            else
                            {
                                type = (byte)((image.ReadByte(pc) >> 4) & 0xF);
                                pc++;
                            }

                            WriteTrace(" ");
                            operands[i] = DecodeLoadOperand(opcode, type, ref operandPos);
                        }

                        uint storePos = pc;

                        // decode store-operands
                        for (int i = 0; i < opcode.Attr.StoreArgs; i++)
                        {
                            byte type;
                            if ((opcode.Attr.LoadArgs + i) % 2 == 0)
                            {
                                type = (byte)(image.ReadByte(pc) & 0xF);
                            }
                            else
                            {
                                type = (byte)((image.ReadByte(pc) >> 4) & 0xF);
                                pc++;
                            }

                            resultTypes[i] = type;
                            WriteTrace(" -> ");
                            resultAddrs[i] = DecodeStoreOperand(opcode, type, ref operandPos);
                        }

                        if (opcode.Attr.Rule == OpcodeRule.DelayedStore ||
                            opcode.Attr.Rule == OpcodeRule.Catch)
                        {
                            // decode delayed store operand
                            byte type;
                            if ((opcode.Attr.LoadArgs + opcode.Attr.StoreArgs) % 2 == 0)
                            {
                                type = (byte)(image.ReadByte(pc) & 0xF);
                            }
                            else
                            {
                                type = (byte)((image.ReadByte(pc) >> 4) & 0xF);
                                pc++;
                            }

                            WriteTrace(" -> ");
                            DecodeDelayedStoreOperand(type, ref operandPos,
                                operands, opcode.Attr.LoadArgs + opcode.Attr.StoreArgs);
                        }

                        if (opcode.Attr.Rule == OpcodeRule.Catch)
                        {
                            // decode final load operand for @catch
                            byte type;
                            if ((opcode.Attr.LoadArgs + opcode.Attr.StoreArgs + 1) % 2 == 0)
                            {
                                type = (byte)(image.ReadByte(pc) & 0xF);
                            }
                            else
                            {
                                type = (byte)((image.ReadByte(pc) >> 4) & 0xF);
                                pc++;
                            }

                            WriteTrace(" ?");
                            operands[opcode.Attr.LoadArgs + opcode.Attr.StoreArgs + 2] =
                                DecodeLoadOperand(opcode, type, ref operandPos);
                        }

                        WriteTrace("]\r\n");

                        // call opcode implementation
                        pc = operandPos; // after the last operand
                        opcode.Handler(operands);

                        // store results
                        for (int i = 0; i < opcode.Attr.StoreArgs; i++)
                            StoreResult(opcode.Attr.Rule, resultTypes[i], resultAddrs[i],
                                operands[opcode.Attr.LoadArgs + i]);
                        break;

                    case ExecutionMode.CString:
                        NextCStringChar();
                        break;

                    case ExecutionMode.UnicodeString:
                        NextUniStringChar();
                        break;

                    case ExecutionMode.Number:
                        NextDigit();
                        break;

                    case ExecutionMode.CompressedString:
                        if (nativeDecodingTable != null)
                            nativeDecodingTable.HandleNextChar(this);
                        else
                            NextCompressedChar();
                        break;

                    case ExecutionMode.Return:
                        return;
                }

#if PROFILING
                cycles++;
#endif
            }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                throw;
            }
        }

        private uint DecodeLoadOperand(Opcode opcode, byte type, ref uint operandPos)
        {
            uint address, value;
            switch (type)
            {
                case 0:
                    WriteTrace("zero");
                    return 0;
                case 1:
                    value = (uint)(sbyte)image.ReadByte(operandPos++);
                    WriteTrace("byte_" + value.ToString());
                    return value;
                case 2:
                    operandPos += 2;
                    value = (uint)(short)image.ReadInt16(operandPos - 2);
                    WriteTrace("short_" + value.ToString());
                    return value;
                case 3:
                    operandPos += 4;
                    value = image.ReadInt32(operandPos - 4);
                    WriteTrace("int_" + value.ToString());
                    return value;

                // case 4: unused

                case 5:
                    address = image.ReadByte(operandPos++);
                    WriteTrace("ptr");
                    goto LoadIndirect;
                case 6:
                    address = image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("ptr");
                    goto LoadIndirect;
                case 7:
                    address = image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("ptr");
                LoadIndirect:
                    WriteTrace("_" + address.ToString() + "(");
                    switch (opcode.Attr.Rule)
                    {
                        case OpcodeRule.Indirect8Bit:
                            value = image.ReadByte(address);
                            break;
                        case OpcodeRule.Indirect16Bit:
                            value = image.ReadInt16(address);
                            break;
                        default:
                            value = image.ReadInt32(address);
                            break;
                    }
                    WriteTrace(value.ToString() + ")");
                    return value;

                case 8:
                    if (sp <= fp + frameLen)
                        throw new VMException("Stack underflow");
                    value = Pop();
                    WriteTrace("sp(" + value.ToString() + ")");
                    return value;

                case 9:
                    address = image.ReadByte(operandPos++);
                    goto LoadLocal;
                case 10:
                    address = image.ReadInt16(operandPos);
                    operandPos += 2;
                    goto LoadLocal;
                case 11:
                    address = image.ReadInt32(operandPos);
                    operandPos += 4;
                LoadLocal:
                    WriteTrace("local_" + address.ToString() + "(");
                    address += fp + localsPos;
                    switch (opcode.Attr.Rule)
                    {
                        case OpcodeRule.Indirect8Bit:
                            if (address >= fp + frameLen)
                                throw new VMException("Reading outside local storage bounds");
                            else
                                value = stack[address];
                            break;
                        case OpcodeRule.Indirect16Bit:
                            if (address + 1 >= fp + frameLen)
                                throw new VMException("Reading outside local storage bounds");
                            else
                                value = BigEndian.ReadInt16(stack, address);
                            break;
                        default:
                            if (address + 3 >= fp + frameLen)
                                throw new VMException("Reading outside local storage bounds");
                            else
                                value = ReadFromStack(address);
                            break;
                    }
                    WriteTrace(value.ToString() + ")");
                    return value;

                // case 12: unused

                case 13:
                    address = image.RamStart + image.ReadByte(operandPos++);
                    WriteTrace("ram");
                    goto LoadIndirect;
                case 14:
                    address = image.RamStart + image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("ram");
                    goto LoadIndirect;
                case 15:
                    address = image.RamStart + image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("ram");
                    goto LoadIndirect;

                default:
                    throw new ArgumentException("Invalid operand type");
            }
        }

        private uint DecodeStoreOperand(Opcode opcode, byte type, ref uint operandPos)
        {
            uint address;
            switch (type)
            {
                case 0:
                    // discard result
                    WriteTrace("discard");
                    return 0;

                // case 1..4: unused

                case 5:
                    address = image.ReadByte(operandPos++);
                    WriteTrace("ptr_" + address.ToString());
                    break;
                case 6:
                    address = image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("ptr_" + address.ToString());
                    break;
                case 7:
                    address = image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("ptr_" + address.ToString());
                    break;

                // case 8: push onto stack
                case 8:
                    // push onto stack
                    WriteTrace("sp");
                    return 0;

                case 9:
                    address = image.ReadByte(operandPos++);
                    WriteTrace("local_" + address.ToString());
                    break;
                case 10:
                    address = image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("local_" + address.ToString());
                    break;
                case 11:
                    address = image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("local_" + address.ToString());
                    break;

                // case 12: unused

                case 13:
                    address = image.RamStart + image.ReadByte(operandPos++);
                    WriteTrace("ram_" + (address - image.RamStart).ToString());
                    break;
                case 14:
                    address = image.RamStart + image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("ram_" + (address - image.RamStart).ToString());
                    break;
                case 15:
                    address = image.RamStart + image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("ram_" + (address - image.RamStart).ToString());
                    break;

                default:
                    throw new ArgumentException("Invalid operand type");
            }
            return address;
        }

        private void StoreResult(OpcodeRule rule, byte type, uint address, uint value)
        {
            switch (type)
            {
                case 5:
                case 6:
                case 7:
                case 13:
                case 14:
                case 15:
                    // write to memory
                    switch (rule)
                    {
                        case OpcodeRule.Indirect8Bit:
                            image.WriteByte(address, (byte)value);
                            break;
                        case OpcodeRule.Indirect16Bit:
                            image.WriteInt16(address, (ushort)value);
                            break;
                        default:
                            image.WriteInt32(address, value);
                            break;
                    }
                    break;

                case 9:
                case 10:
                case 11:
                    // write to local storage
                    address += fp + localsPos;
                    switch (rule)
                    {
                        case OpcodeRule.Indirect8Bit:
                            if (address >= fp + frameLen)
                                throw new VMException("Writing outside local storage bounds");
                            else
                                stack[address] = (byte)value;
                            break;
                        case OpcodeRule.Indirect16Bit:
                            if (address + 1 >= fp + frameLen)
                                throw new VMException("Writing outside local storage bounds");
                            else
                                BigEndian.WriteInt16(stack, address, (ushort)value);
                            break;
                        default:
                            if (address + 3 >= fp + frameLen)
                                throw new VMException("Writing outside local storage bounds");
                            else
                                WriteToStack(address, value);
                            break;
                    }
                    break;

                case 8:
                    // push onto stack
                    Push(value);
                    break;
            }
        }

        private void DecodeDelayedStoreOperand(byte type, ref uint operandPos,
            uint[] resultArray, int resultIndex)
        {
            switch (type)
            {
                case 0:
                    // discard result
                    resultArray[resultIndex] = GLULX_STUB_STORE_NULL;
                    resultArray[resultIndex + 1] = 0;
                    WriteTrace("discard");
                    break;

                // case 1..4: unused

                case 5:
                    resultArray[resultIndex] = GLULX_STUB_STORE_MEM;
                    resultArray[resultIndex + 1] = image.ReadByte(operandPos++);
                    WriteTrace("ptr_" + (resultArray[resultIndex + 1]).ToString());
                    break;
                case 6:
                    resultArray[resultIndex] = GLULX_STUB_STORE_MEM;
                    resultArray[resultIndex + 1] = image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("ptr_" + (resultArray[resultIndex + 1]).ToString());
                    break;
                case 7:
                    resultArray[resultIndex] = GLULX_STUB_STORE_MEM;
                    resultArray[resultIndex + 1] = image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("ptr_" + (resultArray[resultIndex + 1]).ToString());
                    break;

                // case 8: push onto stack
                case 8:
                    // push onto stack
                    resultArray[resultIndex] = GLULX_STUB_STORE_STACK;
                    resultArray[resultIndex + 1] = 0;
                    WriteTrace("sp");
                    break;

                case 9:
                    resultArray[resultIndex] = GLULX_STUB_STORE_LOCAL;
                    resultArray[resultIndex + 1] = image.ReadByte(operandPos++);
                    WriteTrace("local_" + (resultArray[resultIndex + 1]).ToString());
                    break;
                case 10:
                    resultArray[resultIndex] = GLULX_STUB_STORE_LOCAL;
                    resultArray[resultIndex + 1] = image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("local_" + (resultArray[resultIndex + 1]).ToString());
                    break;
                case 11:
                    resultArray[resultIndex] = GLULX_STUB_STORE_LOCAL;
                    resultArray[resultIndex + 1] = image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("local_" + (resultArray[resultIndex + 1]).ToString());
                    break;

                // case 12: unused

                case 13:
                    resultArray[resultIndex] = GLULX_STUB_STORE_MEM;
                    resultArray[resultIndex + 1] = image.RamStart + image.ReadByte(operandPos++);
                    WriteTrace("ram_" + (resultArray[resultIndex + 1] - image.RamStart).ToString());
                    break;
                case 14:
                    resultArray[resultIndex] = GLULX_STUB_STORE_MEM;
                    resultArray[resultIndex + 1] = image.RamStart + image.ReadInt16(operandPos);
                    operandPos += 2;
                    WriteTrace("ram_" + (resultArray[resultIndex + 1] - image.RamStart).ToString());
                    break;
                case 15:
                    resultArray[resultIndex] = GLULX_STUB_STORE_MEM;
                    resultArray[resultIndex + 1] = image.RamStart + image.ReadInt32(operandPos);
                    operandPos += 4;
                    WriteTrace("ram_" + (resultArray[resultIndex + 1] - image.RamStart).ToString());
                    break;

                default:
                    throw new ArgumentException("Invalid operand type");
            }
        }

        private void PerformDelayedStore(uint type, uint address, uint value)
        {
            switch (type)
            {
                case GLULX_STUB_STORE_NULL:
                    // discard
                    break;
                case GLULX_STUB_STORE_MEM:
                    // store in main memory
                    image.WriteInt32(address, value);
                    break;
                case GLULX_STUB_STORE_LOCAL:
                    // store in local storage
                    WriteToStack(fp + localsPos + address, value);
                    break;
                case GLULX_STUB_STORE_STACK:
                    // push onto stack
                    Push(value);
                    break;
            }
        }

        /// <summary>
        /// Pushes a frame for a function call, updating FP, SP, and PC.
        /// (A call stub should have already been pushed.)
        /// </summary>
        /// <param name="address">The address of the function being called.</param>
        private void EnterFunction(uint address)
        {
            EnterFunction(address, null);
        }

        /// <summary>
        /// Pushes a frame for a function call, updating FP, SP, and PC.
        /// (A call stub should have already been pushed.)
        /// </summary>
        /// <param name="address">The address of the function being called.</param>
        /// <param name="args">The argument values to load into local storage,
        /// or <see langword="null"/> if local storage should all be zeroed.</param>
        private void EnterFunction(uint address, uint[] args)
        {
#if PROFILING
            profiler.Enter(address, cycles);
#endif
            execMode = ExecutionMode.Code;

            // push a call frame
            fp = sp;
            Push(0);  // temporary FrameLen
            Push(0);  // temporary LocalsPos

            // copy locals info into the frame...
            uint localSize = 0;

            for (uint i = address + 1; true; i += 2)
            {
                byte type, count;
                stack[sp++] = type = image.ReadByte(i);
                stack[sp++] = count = image.ReadByte(i + 1);
                if (type == 0 || count == 0)
                {
                    pc = i + 2;
                    break;
                }
                if (localSize % type > 0)
                    localSize += (type - (localSize % type));
                localSize += (uint)(type * count);
            }
            // padding
            while (sp % 4 > 0)
                stack[sp++] = 0;

            localsPos = sp - fp;
            WriteToStack(fp + 4, localsPos); // fill in LocalsPos

            if (args == null || args.Length == 0)
            {
                // initialize space for locals
                for (uint i = 0; i < localSize; i++)
                    stack[sp + i] = 0;
            }
            else
            {
                // copy initial values as appropriate
                uint offset = 0, lastOffset = 0;
                byte size = 0, count = 0;
                address++;
                for (uint argnum = 0; argnum < args.Length; argnum++)
                {
                    if (count == 0)
                    {
                        size = image.ReadByte(address++);
                        count = image.ReadByte(address++);
                        if (size == 0 || count == 0)
                            break;
                        if (offset % size > 0)
                            offset += (size - (offset % size));
                    }

                    // zero any padding space between locals
                    for (uint i = lastOffset; i < offset; i++)
                        stack[sp + i] = 0;

                    switch (size)
                    {
                        case 1:
                            stack[sp + offset] = (byte)args[argnum];
                            break;
                        case 2:
                            BigEndian.WriteInt16(stack, sp + offset, (ushort)args[argnum]);
                            break;
                        case 4:
                            WriteToStack(sp + offset, args[argnum]);
                            break;
                    }

                    offset += size;
                    lastOffset = offset;
                    count--;
                }

                // zero any remaining local space
                for (uint i = lastOffset; i < localSize; i++)
                    stack[sp + i] = 0;
            }
            sp += localSize;
            // padding
            while (sp % 4 > 0)
                stack[sp++] = 0;

            frameLen = sp - fp;
            WriteToStack(fp, frameLen); // fill in FrameLen
        }

        private void LeaveFunction(uint result)
        {
#if PROFILING
            profiler.Leave(cycles);
#endif
            if (fp == 0)
            {
                // top-level function
                running = false;
            }
            else
            {
                System.Diagnostics.Debug.Assert(sp >= fp);
                sp = fp;
                ResumeFromCallStub(result);
            }
        }

        private void ResumeFromCallStub(uint result)
        {
            CallStub stub = PopCallStub();

            pc = stub.PC;
            execMode = ExecutionMode.Code;

            uint newFP = stub.FramePtr;
            uint newFrameLen = ReadFromStack(newFP);
            uint newLocalsPos = ReadFromStack(newFP + 4);

            switch (stub.DestType)
            {
                case GLULX_STUB_STORE_NULL:
                    // discard
                    break;
                case GLULX_STUB_STORE_MEM:
                    // store in main memory
                    image.WriteInt32(stub.DestAddr, result);
                    break;
                case GLULX_STUB_STORE_LOCAL:
                    // store in local storage
                    WriteToStack(newFP + newLocalsPos + stub.DestAddr, result);
                    break;
                case GLULX_STUB_STORE_STACK:
                    // push onto stack
                    Push(result);
                    break;

                case GLULX_STUB_RESUME_FUNC:
                    // resume executing in the same call frame
                    // return to avoid changing FP
                    return;

                case GLULX_STUB_RESUME_CSTR:
                    // resume printing a C-string
                    execMode = ExecutionMode.CString;
                    break;
                case GLULX_STUB_RESUME_UNISTR:
                    // resume printing a Unicode string
                    execMode = ExecutionMode.UnicodeString;
                    break;
                case GLULX_STUB_RESUME_NUMBER:
                    // resume printing a decimal number
                    execMode = ExecutionMode.Number;
                    printingDigit = (byte)stub.DestAddr;
                    break;
                case GLULX_STUB_RESUME_HUFFSTR:
                    // resume printing a compressed string
                    execMode = ExecutionMode.CompressedString;
                    printingDigit = (byte)stub.DestAddr;
                    break;

                case FYREVM_STUB_RESUME_NATIVE:
                    // exit the interpreter loop and return via NestedCall()
                    nestedResult = result;
                    execMode = ExecutionMode.Return;
                    break;
            }

            fp = newFP;
            frameLen = newFrameLen;
            localsPos = newLocalsPos;
            return;
        }

        private void InputLine(uint address, uint bufSize)
        {
            string input = null;

            // we need at least 4 bytes to do anything useful
            if (bufSize < 4)
                return;

            // can't do anything without this event handler
            if (LineWanted == null)
            {
                image.WriteInt32(address, 0);
                return;
            }

            LineWantedEventArgs lineArgs = new LineWantedEventArgs();
            // CancelEventArgs waitArgs = new CancelEventArgs();

            // ask the application to read a line
            LineWanted(this, lineArgs);
            input = lineArgs.Line;

            if (input == null)
            {
                image.WriteInt32(address, 0);
            }
            else
            {
                byte[] bytes = null;
                // write the length first
                try
                {
                    bytes = StringToLatin1(input);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                image.WriteInt32(address, (uint)bytes.Length);
                // followed by the character data, truncated to fit the buffer
                uint max = Math.Min(bufSize, (uint)bytes.Length);
                for (uint i = 0; i < max; i++)
                    image.WriteByte(address + 4 + i, bytes[i]);
            }
        }

        // quick 'n dirty translation, because Silverlight doesn't support ISO-8859-1 encoding
        private static byte[] StringToLatin1(string str)
        {
            byte[] result = new byte[str.Length];

            for (int i = 0; i < str.Length; i++)
            {
                ushort value = (ushort)str[i];
                if (value > 255)
                    result[i] = (byte)'?';
                else
                    result[i] = (byte)value;
            }

            return result;
        }

        private char InputChar()
        {
            // can't do anything without this event handler
            if (KeyWanted == null)
                return '\0';

            KeyWantedEventArgs keyArgs = new KeyWantedEventArgs();
            //CancelEventArgs waitArgs = new CancelEventArgs();

            // ask the application to read a character
            KeyWanted(this, keyArgs);
            return keyArgs.Char;
        }

        private void SaveToStream(Stream stream, uint destType, uint destAddr)
        {
            if (stream == null)
            {
                return;
            }

            Quetzal quetzal = new Quetzal();

            // 'IFhd' identifies the first 128 bytes of the game file
            quetzal["IFhd"] = image.GetOriginalIFHD();

            // 'CMem' or 'UMem' are the compressed/uncompressed contents of RAM
            byte[] origRam = image.GetOriginalRAM();
            byte[] newRomRam = image.GetMemory();
            int ramSize = (int)(image.EndMem - image.RamStart);
#if !SAVE_UNCOMPRESSED
            quetzal["CMem"] = Quetzal.CompressMemory(
                origRam, 0, origRam.Length,
                newRomRam, (int)image.RamStart, ramSize);
#else
            byte[] umem = new byte[ramSize + 4];
            BigEndian.WriteInt32(umem, 0, (uint)ramSize);
            Array.Copy(newRomRam, (int)image.RamStart, umem, 4, ramSize);
            quetzal["UMem"] = umem;
#endif

            // 'Stks' is the contents of the stack, with a stub on top
            // identifying the destination of the save opcode.
            PushCallStub(new CallStub(destType, destAddr, pc, fp));
            byte[] trimmed = new byte[sp];
            Array.Copy(stack, trimmed, (int)sp);
            //for (uint bt=0; bt < sp; bt++)
            //{
            //    trimmed[bt] = stack[bt];
            //}
            quetzal["Stks"] = trimmed;
            PopCallStub();

            // 'MAll' is the list of heap blocks
            if (heap != null)
                quetzal["MAll"] = heap.Save();
            else
            {
                
            }

            quetzal.WriteToStream(stream);
        }

        private void LoadFromStream(Stream stream)
        {
            Quetzal quetzal = Quetzal.FromStream(stream);

            // make sure the save file matches the game file
            byte[] ifhd1 = quetzal["IFhd"];
            byte[] ifhd2 = image.GetOriginalIFHD();
            if (ifhd1 == null || ifhd1.Length != ifhd2.Length)
                throw new ArgumentException("Missing or invalid IFhd block");

            for (int i = 0; i < ifhd1.Length; i++)
                if (ifhd1[i] != ifhd2[i])
                    throw new ArgumentException("Saved game doesn't match this story file");

            // load the stack
            byte[] newStack = quetzal["Stks"];
            if (newStack == null)
                throw new ArgumentException("Missing Stks block");

            Array.Copy(newStack, stack, newStack.Length);
            sp = (uint)newStack.Length;

            // save the protected area of RAM
            byte[] protectedRam = new byte[protectionLength];
            image.ReadRAM(protectionStart, protectionLength, protectedRam);

            // load the contents of RAM, preferring a compressed chunk
            byte[] origRam = image.GetOriginalRAM();
            byte[] delta = quetzal["CMem"];
            if (delta != null)
            {
                byte[] newRam = Quetzal.DecompressMemory(origRam, delta);
                image.SetRAM(newRam, false);
            }
            else
            {
                // look for an uncompressed chunk
                byte[] newRam = quetzal["UMem"];
                if (newRam == null)
                    throw new ArgumentException("Missing CMem/UMem blocks");
                else
                    image.SetRAM(newRam, true);
            }

            // restore protected RAM
            image.WriteRAM(protectionStart, protectedRam);

            // pop a call stub to restore registers
            CallStub stub = PopCallStub();
            pc = stub.PC;
            fp = stub.FramePtr;
            frameLen = ReadFromStack(fp);
            localsPos = ReadFromStack(fp + 4);
            execMode = ExecutionMode.Code;

            // restore the heap if available
            if (quetzal.Contains("MAll"))
            {
                heap = new HeapAllocator(quetzal["MAll"], HandleHeapMemoryRequest);
                if (heap.BlockCount == 0)
                    heap = null;
                else
                    heap.MaxSize = maxHeapSize;
            }

            // give the original save opcode a result of -1 to show that it's been restored
            PerformDelayedStore(stub.DestType, stub.DestAddr, 0xFFFFFFFF);
        }

        /// <summary>
        /// Reloads the initial contents of memory (except the protected area)
        /// and starts the game over from the top of the main function.
        /// </summary>
        private void Restart()
        {
            // save the protected area of RAM
            byte[] protectedRam = new byte[protectionLength];
            image.ReadRAM(protectionStart, protectionLength, protectedRam);

            // reload memory, reinitialize registers and stacks
            image.Revert();
            Bootstrap();

            // restore protected RAM
            image.WriteRAM(protectionStart, protectedRam);
            CacheDecodingTable();
        }

        /// <summary>
        /// Terminates the interpreter loop, causing the <see cref="Run"/>
        /// method to return.
        /// </summary>
        public void Stop()
        {
            running = false;
        }

    }
}
