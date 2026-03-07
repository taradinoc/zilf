/* Copyright 2010-2025 Tara McGrew
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

namespace Zilf.Blorb
{
    sealed class BlorbFile
    {
        private List<byte[]> pictures = [];
        private List<PictureType> pictureTypes = [];
        private byte[]? executable;
        private bool executableIsGlulx;

        public bool IsEmpty => pictures.Count == 0 && executable == null;

        public int AddPicture(byte[] data)
        {
            PictureType type = data switch
            {
                [0x89, 0x50, 0x4e, 0x47, ..] => PictureType.Png,
                [0xff, 0xd8, 0xff, 0xe0, ..] => PictureType.Jpeg,
                _ => throw new ArgumentException("Not a PNG or JPEG file", nameof(data)),
            };

            pictures.Add(data);
            pictureTypes.Add(type);

            return pictures.Count;
        }

        public void AddExecutable(byte[] data, bool isGlulx)
        {
            executable = data ?? throw new ArgumentNullException(nameof(data));
            executableIsGlulx = isGlulx;
        }

        public void WriteTo(Stream stream)
        {
            const int MagicNumber = ('F' << 24) + ('O' << 16) + ('R' << 8) + 'M';
            const int FormType = ('I' << 24) + ('F' << 16) + ('R' << 8) + 'S';
            const int ExecUsage = ('E' << 24) + ('x' << 16) + ('e' << 8) + 'c';
            const int PictureUsage = ('P' << 24) + ('i' << 16) + ('c' << 8) + 't';
            const int RidxChunk = ('R' << 24) + ('I' << 16) + ('d' << 8) + 'x';
            const int ZcodChunk = ('Z' << 24) + ('C' << 16) + ('O' << 8) + 'D';
            const int GlulChunk = ('G' << 24) + ('L' << 16) + ('U' << 8) + 'L';
            const int PngChunk = ('P' << 24) + ('N' << 16) + ('G' << 8) + ' ';
            const int JpegChunk = ('J' << 24) + ('P' << 16) + ('E' << 8) + 'G';

            stream.Position = 0;

            WriteInt(stream, MagicNumber);
            WriteInt(stream, 0);        // placeholder FORM length, will be filled in later
            WriteInt(stream, FormType);

            int pictureCount = pictures.Count;
            bool hasExecutable = executable != null;
            int resourceCount = pictureCount + (hasExecutable ? 1 : 0);
            var startingPositions = new List<int>(pictureCount);
            int executablePosition = -1;

            // placeholder resource index, will be filled in later
            int ridxLength = 12 + resourceCount * 12;
            for (int i = 0; i < ridxLength; i++)
            {
                stream.WriteByte(0);
            }

            if (hasExecutable)
            {
                var executableData = executable!;
                executablePosition = (int)stream.Position;

                WriteInt(stream, executableIsGlulx ? GlulChunk : ZcodChunk);
                WriteInt(stream, executableData.Length);
                stream.Write(executableData);

                if ((stream.Position % 2) != 0)
                    stream.WriteByte(0);
            }

            // picture chunks
            for (int i = 0; i < pictureCount; i++)
            {
                startingPositions.Add((int)stream.Position);

                WriteInt(stream, pictureTypes[i] == PictureType.Png ? PngChunk : JpegChunk);
                WriteInt(stream, pictures[i].Length);
                stream.Write(pictures[i]);

                if ((stream.Position % 2) != 0)
                    stream.WriteByte(0);        // pad to even chunk offsets
            }

            int formLength = (int)stream.Position - 8;

            // fill in FORM length
            stream.Position = 4;
            WriteInt(stream, formLength);

            // go back to fill in resource index
            stream.Position = 12;
            WriteInt(stream, RidxChunk);
            WriteInt(stream, ridxLength - 8);
            WriteInt(stream, resourceCount);

            if (hasExecutable)
            {
                WriteInt(stream, ExecUsage);
                WriteInt(stream, 0);
                WriteInt(stream, executablePosition);
            }

            for (int i = 0; i < pictureCount; i++)
            {
                WriteInt(stream, PictureUsage);
                WriteInt(stream, i + 1);
                WriteInt(stream, startingPositions[i]);
            }

            static void WriteInt(Stream stream, int value)
            {
                stream.WriteByte((byte)(value >> 24));
                stream.WriteByte((byte)(value >> 16));
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
            }
        }
    }
}