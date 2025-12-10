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
    /// Provides utility functions for working with big-endian numbers.
    /// </summary>
    internal static class BigEndian
    {
        /// <summary>
        /// Reads an unsigned, big-endian, 16-bit number from a byte array.
        /// </summary>
        /// <param name="array">The array to read from.</param>
        /// <param name="offset">The index within the array where the number starts.</param>
        /// <returns>The number read.</returns>
        public static ushort ReadInt16(byte[] array, uint offset)
        {
            return (ushort)((array[offset] << 8) + array[offset + 1]);
        }

        /// <summary>
        /// Reads an unsigned, big-endian, 16-bit number from a byte array.
        /// </summary>
        /// <param name="array">The array to read from.</param>
        /// <param name="offset">The index within the array where the number starts.</param>
        /// <returns>The number read.</returns>
        public static ushort ReadInt16(byte[] array, int offset)
        {
            return ReadInt16(array, (uint)offset);
        }

        /// <summary>
        /// Reads an unsigned, big-endian, 32-bit number from a byte array.
        /// </summary>
        /// <param name="array">The array to read from.</param>
        /// <param name="offset">The index within the array where the number starts.</param>
        /// <returns>The number read.</returns>
        public static uint ReadInt32(byte[] array, uint offset)
        {
            return (uint)((array[offset] << 24) + (array[offset + 1] << 16) +
                (array[offset + 2] << 8) + array[offset + 3]);
        }

        /// <summary>
        /// Reads an unsigned, big-endian, 32-bit number from a byte array.
        /// </summary>
        /// <param name="array">The array to read from.</param>
        /// <param name="offset">The index within the array where the number starts.</param>
        /// <returns>The number read.</returns>
        public static uint ReadInt32(byte[] array, int offset)
        {
            return ReadInt32(array, (uint)offset);
        }

        /// <summary>
        /// Writes an unsigned, big-endian, 16-bit number into a byte array.
        /// </summary>
        /// <param name="array">The array to write into.</param>
        /// <param name="offset">The index within the array where the number will start.</param>
        /// <param name="value">The number to write.</param>
        public static void WriteInt16(byte[] array, uint offset, ushort value)
        {
            array[offset] = (byte)(value >> 8);
            array[offset + 1] = (byte)value;
        }

        /// <summary>
        /// Writes an unsigned, big-endian, 16-bit number into a byte array.
        /// </summary>
        /// <param name="array">The array to write into.</param>
        /// <param name="offset">The index within the array where the number will start.</param>
        /// <param name="value">The number to write.</param>
        public static void WriteInt16(byte[] array, int offset, ushort value)
        {
            WriteInt16(array, (uint)offset, value);
        }

        /// <summary>
        /// Writes an unsigned, big-endian, 32-bit number into a byte array.
        /// </summary>
        /// <param name="array">The array to write into.</param>
        /// <param name="offset">The index within the array where the number will start.</param>
        /// <param name="value">The number to write.</param>
        public static void WriteInt32(byte[] array, uint offset, uint value)
        {
            array[offset] = (byte)(value >> 24);
            array[offset + 1] = (byte)(value >> 16);
            array[offset + 2] = (byte)(value >> 8);
            array[offset + 3] = (byte)value;
        }

        /// <summary>
        /// Writes an unsigned, big-endian, 32-bit number into a byte array.
        /// </summary>
        /// <param name="array">The array to write into.</param>
        /// <param name="offset">The index within the array where the number will start.</param>
        /// <param name="value">The number to write.</param>
        public static void WriteInt32(byte[] array, int offset, uint value)
        {
            WriteInt32(array, (uint)offset, value);
        }

        /// <summary>
        /// Writes an unsigned, big-endian, 16-bit number to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="value">The number to write.</param>
        public static void WriteInt16(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Reads an unsigned, big-endian, 16-bit number from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The number read.</returns>
        public static ushort ReadInt16(Stream stream)
        {
            int a = stream.ReadByte();
            int b = stream.ReadByte();
            return (ushort)((a << 8) + b);
        }

        /// <summary>
        /// Writes an unsigned, big-endian, 32-bit number to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="value">The number to write.</param>
        public static void WriteInt32(Stream stream, uint value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Reads an unsigned, big-endian, 32-bit number from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The number read.</returns>
        public static uint ReadInt32(Stream stream)
        {
            int a = stream.ReadByte();
            int b = stream.ReadByte();
            int c = stream.ReadByte();
            int d = stream.ReadByte();
            return (uint)((a << 24) + (b << 16) + (c << 8) + d);
        }
    }
}
