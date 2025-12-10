/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing resstrictions.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FyreVM
{
    /// <summary>
    /// Implements the Quetzal saved-game file specification by holding a list of
    /// typed data chunks which can be read from or written to streams.
    /// </summary>
    /// <remarks>
    /// http://www.ifarchive.org/if-archive/infocom/interpreters/specification/savefile_14.txt
    /// </remarks>
    internal class Quetzal
    {
        private Dictionary<uint, byte[]> chunks = new Dictionary<uint, byte[]>();

        private static readonly uint FORM = StrToID("FORM");
        private static readonly uint LIST = StrToID("LIST");
        private static readonly uint CAT_ = StrToID("CAT ");
        private static readonly uint IFZS = StrToID("IFZS");

        /// <summary>
        /// Initializes a new chunk collection.
        /// </summary>
        public Quetzal()
        {
        }

        /// <summary>
        /// Loads a collection of chunks from a Quetzal file.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A new <see cref="Quetzal"/> instance initialized
        /// from the stream.</returns>
        /// <remarks>
        /// Duplicate chunks are not supported by this class. Only the last
        /// chunk of a given type will be available.
        /// </remarks>
        public static Quetzal FromStream(Stream stream)
        {
            Quetzal result = new Quetzal();

            uint type = BigEndian.ReadInt32(stream);
            if (type != FORM && type != LIST && type != CAT_)
                throw new ArgumentException("Invalid IFF type");

            int length = (int)BigEndian.ReadInt32(stream);
            byte[] buffer = new byte[length];
            int amountRead = stream.Read(buffer, 0, (int)length);
            if (amountRead < length)
                throw new ArgumentException("Quetzal file is too short");

            stream = new MemoryStream(buffer);
            type = BigEndian.ReadInt32(stream);
            if (type != IFZS)
                throw new ArgumentException("Wrong IFF sub-type: not a Quetzal file");

            while (stream.Position < stream.Length)
            {
                type = BigEndian.ReadInt32(stream);
                length = (int)BigEndian.ReadInt32(stream);
                byte[] chunk = new byte[length];
                amountRead = stream.Read(chunk, 0, length);
                if (amountRead < length)
                    throw new ArgumentException("Chunk extends past end of file");

                result.chunks[type] = chunk;
            }

            return result;
        }

        /// <summary>
        /// Gets or sets typed data chunks.
        /// </summary>
        /// <param name="type">The 4-character type identifier.</param>
        /// <returns>The contents of the chunk.</returns>
        public byte[] this[string type]
        {
            get
            {
                byte[] result = null;
                chunks.TryGetValue(StrToID(type), out result);
                return result;
            }
            set
            {
                chunks[StrToID(type)] = value;
            }
        }

        /// <summary>
        /// Checks whether the Quetzal file contains a given chunk type.
        /// </summary>
        /// <param name="type">The 4-character type identifier.</param>
        /// <returns><see langword="true"/> if the chunk is present.</returns>
        public bool Contains(string type)
        {
            return chunks.ContainsKey(StrToID(type));
        }

        private static uint StrToID(string type)
        {
            byte a = (byte)type[0];
            byte b = (byte)type[1];
            byte c = (byte)type[2];
            byte d = (byte)type[3];
            return (uint)((a << 24) + (b << 16) + (c << 8) + d);
        }

        /// <summary>
        /// Writes the chunks to a Quetzal file.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void WriteToStream(Stream stream)
        {
            BigEndian.WriteInt32(stream, FORM);     // IFF tag
            BigEndian.WriteInt32(stream, 0);        // file length (filled in later)
            BigEndian.WriteInt32(stream, IFZS);     // FORM sub-ID for Quetzal

            uint totalSize = 4; // includes sub-ID
            foreach (KeyValuePair<uint, byte[]> pair in chunks)
            {
                BigEndian.WriteInt32(stream, pair.Key);                 // chunk type
                BigEndian.WriteInt32(stream, (uint)pair.Value.Length);  // chunk length
                stream.Write(pair.Value, 0, pair.Value.Length);         // chunk data
                totalSize += 8 + (uint)(pair.Value.Length);
            }

            if (totalSize % 2 == 1)
                stream.WriteByte(0);    // padding (not counted in file length)

            stream.Seek(4, SeekOrigin.Begin);
            BigEndian.WriteInt32(stream, totalSize);
            //stream.SetLength(totalSize);
        }

        /// <summary>
        /// Compresses a block of memory by comparing it with the original
        /// version from the game file.
        /// </summary>
        /// <param name="original">An array containing the original block of memory.</param>
        /// <param name="origStart">The offset within the array where the original block
        /// starts.</param>
        /// <param name="origLength">The length of the original block, in bytes.</param>
        /// <param name="changed">An array containing the changed block to be compressed.</param>
        /// <param name="changedStart">The offset within the array where the changed
        /// block starts.</param>
        /// <param name="changedLength">The length of the changed block. This may be
        /// greater than <paramref name="origLength"/>, but may not be less.</param>
        /// <returns>The RLE-compressed set of differences between the old and new
        /// blocks, prefixed with a 4-byte length.</returns>
        public static byte[] CompressMemory(byte[] original, int origStart, int origLength,
            byte[] changed, int changedStart, int changedLength)
        {
            if (origStart + origLength > original.Length)
                throw new ArgumentException("Original array is too small");
            if (changedStart + changedLength > changed.Length)
                throw new ArgumentException("Changed array is too small");
            if (changedLength < origLength)
                throw new ArgumentException("New block must be no smaller than old block");

            MemoryStream mstr = new MemoryStream();
            BigEndian.WriteInt32(mstr, (uint)changedLength);

            for (int i = 0; i < origLength; i++)
            {
                byte b = (byte)(original[origStart+i] ^ changed[changedStart+i]);
                if (b == 0)
                {
                    int runLength;
                    for (runLength = 1; i + runLength < origLength; runLength++)
                    {
                        if (runLength == 256)
                            break;
                        if (original[origStart + i + runLength] != changed[changedStart + i + runLength])
                            break;
                    }
                    mstr.WriteByte(0);
                    mstr.WriteByte((byte)(runLength - 1));
                    i += runLength - 1;
                }
                else
                    mstr.WriteByte(b);
            }

            return mstr.ToArray();
        }

        /// <summary>
        /// Reconstitutes a changed block of memory by applying a compressed
        /// set of differences to the original block from the game file.
        /// </summary>
        /// <param name="original">The original block of memory.</param>
        /// <param name="delta">The RLE-compressed set of differences,
        /// prefixed with a 4-byte length. This length may be larger than
        /// the original block, but not smaller.</param>
        /// <returns>The changed block of memory. The length of this array is
        /// specified at the beginning of <paramref name="delta"/>.</returns>
        public static byte[] DecompressMemory(byte[] original, byte[] delta)
        {
            MemoryStream mstr = new MemoryStream(delta);
            uint length = BigEndian.ReadInt32(mstr);
            if (length < original.Length)
                throw new ArgumentException("Compressed block's length tag must be no less than original block's size");

            byte[] result = new byte[length];
            int rp = 0;

            for (int i = 4; i < delta.Length; i++)
            {
                byte b = delta[i];
                if (b == 0)
                {
                    int repeats = delta[++i] + 1;
                    Array.Copy(original, rp, result, rp, repeats);
                    rp += repeats;
                }
                else
                {
                    result[rp] = (byte)(original[rp] ^ b);
                    rp++;
                }
            }

            while (rp < original.Length)
            {
                result[rp] = original[rp];
                rp++;
            }

            return result;
        }

    }
}
