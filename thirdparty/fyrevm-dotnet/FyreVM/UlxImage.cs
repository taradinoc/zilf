/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FyreVM
{
    /// <summary>
    /// Represents the ROM and RAM of a Glulx game image.
    /// </summary>
    internal class UlxImage
    {
        private byte[] memory;
        private uint ramstart;
        private Stream originalStream;
        private byte[] originalRam, originalHeader;

        /// <summary>
        /// Initializes a new instance from the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing the Glulx image.</param>
        public UlxImage(Stream stream)
        {
            originalStream = stream;
            LoadFromStream(stream);
        }

        private void LoadFromStream(Stream stream)
        {
            if (stream.Length > int.MaxValue)
                throw new ArgumentException(".ulx file is too big");

            if (stream.Length < Engine.GLULX_HDR_SIZE)
                throw new ArgumentException(".ulx file is too small");

            // read just the header, to find out how much memory we need
            memory = new byte[Engine.GLULX_HDR_SIZE];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(memory, 0, Engine.GLULX_HDR_SIZE);

            if (memory[0] != (byte)'G' || memory[1] != (byte)'l' ||
                memory[2] != (byte)'u' || memory[3] != (byte)'l')
                throw new ArgumentException(".ulx file has wrong magic number");

            uint endmem = ReadInt32(Engine.GLULX_HDR_ENDMEM_OFFSET);

            // now read the whole thing
            memory = new byte[endmem];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(memory, 0, (int)stream.Length);

            // verify checksum
            uint checksum = CalculateChecksum();
            if (checksum != ReadInt32(Engine.GLULX_HDR_CHECKSUM_OFFSET))
                throw new ArgumentException(".ulx file has incorrect checksum");

            ramstart = ReadInt32(Engine.GLULX_HDR_RAMSTART_OFFSET);
        }

        /// <summary>
        /// Gets the address at which RAM begins.
        /// </summary>
        /// <remarks>
        /// The region of memory below RamStart is considered ROM. Addresses
        /// below RamStart are readable but unwritable.
        /// </remarks>
        public uint RamStart
        {
            get { return ramstart; }
        }

        /// <summary>
        /// Gets or sets the address at which memory ends.
        /// </summary>
        /// <remarks>
        /// This can be changed by the game with @setmemsize (or managed
        /// automatically by the heap allocator). Addresses above EndMem are
        /// neither readable nor writable.
        /// </remarks>
        public uint EndMem
        {
            get
            {
                return (uint)memory.Length;
            }
            set
            {
                // round up to the next multiple of 256
                if (value % 256 != 0)
                    value = (value + 255) & 0xFFFFFF00;

                if ((uint)memory.Length != value)
                {
                    byte[] newMem = new byte[value];
                    Array.Copy(memory, newMem, (int)Math.Min((uint)memory.Length, (int)value));
                    memory = newMem;
                }
            }
        }

        /// <summary>
        /// Reads a single byte from memory.
        /// </summary>
        /// <param name="offset">The address to read from.</param>
        /// <returns>The byte at the specified address.</returns>
        public byte ReadByte(uint offset)
        {
            return memory[offset];
        }

        /// <summary>
        /// Reads a big-endian word from memory.
        /// </summary>
        /// <param name="offset">The address to read from</param>
        /// <returns>The word at the specified address.</returns>
        public ushort ReadInt16(uint offset)
        {
            return BigEndian.ReadInt16(memory, offset);
        }

        /// <summary>
        /// Reads a big-endian double word from memory.
        /// </summary>
        /// <param name="offset">The address to read from.</param>
        /// <returns>The 32-bit value at the specified address.</returns>
        public uint ReadInt32(uint offset)
        {
            return BigEndian.ReadInt32(memory, offset);
        }

        /// <summary>
        /// Writes a single byte into memory.
        /// </summary>
        /// <param name="offset">The address to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="VMException">The address is below RamStart.</exception>
        public void WriteByte(uint offset, byte value)
        {
            if (offset < ramstart)
                throw new VMException("Writing into ROM");

            memory[offset] = value;
        }

        /// <summary>
        /// Writes a big-endian 16-bit word into memory.
        /// </summary>
        /// <param name="offset">The address to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="VMException">The address is below RamStart.</exception>
        public void WriteInt16(uint offset, ushort value)
        {
            if (offset < ramstart)
                throw new VMException("Writing into ROM");

            BigEndian.WriteInt16(memory, offset, value);
        }

        /// <summary>
        /// Writes a big-endian 32-bit word into memory.
        /// </summary>
        /// <param name="offset">The address to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="VMException">The address is below RamStart.</exception>
        public void WriteInt32(uint offset, uint value)
        {
            if (offset < ramstart)
                throw new VMException("Writing into ROM");

            BigEndian.WriteInt32(memory, offset, value);
        }

        /// <summary>
        /// Calculates the checksum of the image.
        /// </summary>
        /// <returns>The sum of the entire image, taken as an array of
        /// 32-bit words.</returns>
        public uint CalculateChecksum()
        {
            uint end = ReadInt32(Engine.GLULX_HDR_EXTSTART_OFFSET);
            // negative checksum here cancels out the one we'll add inside the loop
            uint sum = (uint)(-ReadInt32(Engine.GLULX_HDR_CHECKSUM_OFFSET));

            System.Diagnostics.Debug.Assert(end % 4 == 0); // Glulx spec 1.2 says ENDMEM % 256 == 0

            for (uint i = 0; i < end; i += 4)
                sum += ReadInt32(i);

            return sum;
        }

        /// <summary>
        /// Gets the entire contents of memory.
        /// </summary>
        /// <returns>An array containing all VM memory, ROM and RAM.</returns>
        public byte[] GetMemory()
        {
            return memory;
        }

        /// <summary>
        /// Sets the entire contents of RAM, changing the size if necessary.
        /// </summary>
        /// <param name="newBlock">The new contents of RAM.</param>
        /// <param name="embeddedLength">If true, indicates that <paramref name="newBlock"/>
        /// is prefixed with a 32-bit word giving the new size of RAM, which may be
        /// more than the number of bytes actually contained in the rest of the array.</param>
        public void SetRAM(byte[] newBlock, bool embeddedLength)
        {
            uint length;
            int offset;

            if (embeddedLength)
            {
                offset = 4;
                length = (uint)((newBlock[0] << 24) + (newBlock[1] << 16) + (newBlock[2] << 8) + newBlock[3]);
            }
            else
            {
                offset = 0;
                length = (uint)newBlock.Length;
            }

            EndMem = ramstart + length;
            Array.Copy(newBlock, offset, memory, (int)ramstart, newBlock.Length - offset);
        }

        /// <summary>
        /// Obtains the initial contents of RAM from the game file.
        /// </summary>
        /// <returns>The initial contents of RAM.</returns>
        public byte[] GetOriginalRAM()
        {
            if (originalRam == null)
            {
                int length = (int)(ReadInt32(Engine.GLULX_HDR_ENDMEM_OFFSET) - ramstart);
                originalRam = new byte[length];
                originalStream.Seek(ramstart, SeekOrigin.Begin);
                originalStream.Read(originalRam, 0, length);
            }
            return originalRam;
        }

        /// <summary>
        /// Obtains the header from the game file.
        /// </summary>
        /// <returns>The first 128 bytes of the game file.</returns>
        public byte[] GetOriginalIFHD()
        {
            if (originalHeader == null)
            {
                originalHeader = new byte[128];
                originalStream.Seek(0, SeekOrigin.Begin);
                originalStream.Read(originalHeader, 0, 128);
            }
            return originalHeader;
        }

        /// <summary>
        /// Copies a block of data out of RAM.
        /// </summary>
        /// <param name="address">The address, based at <see cref="RamStart"/>,
        /// at which to start copying.</param>
        /// <param name="length">The number of bytes to copy.</param>
        /// <param name="dest">The destination array.</param>
        public void ReadRAM(uint address, uint length, byte[] dest)
        {
            Array.Copy(memory, (int)(ramstart + address), dest, 0, (int)length);
        }

        /// <summary>
        /// Copies a block of data into RAM, expanding the memory map if needed.
        /// </summary>
        /// <param name="address">The address, based at <see cref="RamStart"/>,
        /// at which to start copying.</param>
        /// <param name="src">The source array.</param>
        public void WriteRAM(uint address, byte[] src)
        {
            EndMem = Math.Max(EndMem, ramstart + (uint)src.Length);
            Array.Copy(src, 0, memory, (int)(ramstart + address), src.Length);
        }

        /// <summary>
        /// Reloads the game file, discarding all changes that have been made
        /// to RAM and restoring the memory map to its original size.
        /// </summary>
        public void Revert()
        {
            LoadFromStream(originalStream);
        }
    }
}
