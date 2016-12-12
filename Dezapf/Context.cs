/* Copyright 2010, 2015 Jesse McGrew
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
using System.Reflection;
using System.Text;
using Zapf;

namespace Dezapf
{
    class Context
    {
        int zversion;
        Header header;

        readonly RangeList<Chunk> chunks = new RangeList<Chunk>();
        readonly Dictionary<ushort, ZOpAttribute> opcodes = new Dictionary<ushort, ZOpAttribute>();

        public Context()
        {
            zversion = 3;
            LoadOpcodes();
        }

        public OutputStyle OutputStyle;

        public int ZVersion
        {
            get
            {
                return zversion;
            }
            set
            {
                if (value < 3 || value > 8)
                    throw new ArgumentOutOfRangeException("Only versions 3-8 are supported");

                zversion = value;
                LoadOpcodes();
            }
        }

        public int PackingFactor
        {
            get
            {
                switch (zversion)
                {
                    case 1:
                    case 2:
                    case 3:
                        return 2;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        return 4;
                    case 8:
                        return 8;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public int SizeFactor
        {
            get
            {
                switch (zversion)
                {
                    case 1:
                    case 2:
                    case 3:
                        return 2;
                    case 4:
                    case 5:
                        return 4;
                    default:
                        return 8;
                }
            }
        }

        public Header Header
        {
            get
            {
                return header;
            }
            set
            {
                ZVersion = value.ZVersion;
                header = value;
            }
        }

        public RangeList<Chunk> Chunks
        {
            get { return chunks; }
        }

        public int UnpackAddress(int packed, int hdrOffset)
        {
            if (zversion == 6 || zversion == 7)
                return 4 * packed + 8 * hdrOffset;

            return packed * PackingFactor;
        }

        public int PackAddress(int address, int hdrOffset)
        {
            if (zversion == 6 || zversion == 7)
                return (address - 8 * hdrOffset) / 4;

            return address / PackingFactor;
        }

        void LoadOpcodes()
        {
            opcodes.Clear();

            int effectiveVersion = zversion;
            if (zversion == 7)
                effectiveVersion = 6;
            else if (zversion == 8)
                effectiveVersion = 5;

            foreach (FieldInfo fi in typeof(Zapf.Opcodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (fi.FieldType == typeof(Zapf.Opcodes))
                {
                    ushort num = (ushort)(Zapf.Opcodes)fi.GetValue(null);
                    foreach (ZOpAttribute attr in fi.GetCustomAttributes(typeof(ZOpAttribute), false))
                        if (effectiveVersion >= attr.MinVer && effectiveVersion <= attr.MaxVer)
                            opcodes.Add(num, attr);
                }
            }
        }

        public ZOpAttribute GetOpcodeInfo(ushort op)
        {
            ZOpAttribute result;
            opcodes.TryGetValue(op, out result);
            return result;
        }

        static readonly char[] defaultAlphabet0 =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        };
        static readonly char[] defaultAlphabet1 =
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
        };
        static readonly char[] defaultAlphabet2 =
        {
            ' ', '\n', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.',
            ',', '!', '?', '_', '#', '\'', '"', '/', '\\', '-', ':', '(', ')'
        };

        public string DecodeText(ushort[] encodedText)
        {
            System.Diagnostics.Debug.Assert(zversion >= 3);

            StringBuilder sb = new StringBuilder(encodedText.Length * 3);
            int mode = 0;       // 0/1/2 = alphabets, 3 = ASCII state 1, 4 = ASCII state 2
            int pendingAscii = 0;

            //XXX handle custom alphabets
            char[] alphabet0 = defaultAlphabet0;
            char[] alphabet1 = defaultAlphabet1;
            char[] alphabet2 = defaultAlphabet2;

            for (int i = 0; i < encodedText.Length; i++)
            {
                ushort w = encodedText[i];

                for (int j = 0; j < 3; j++)
                {
                    int code = (w & 0x7c00) >> 10;
                    w <<= 5;

                    switch (mode)
                    {
                        case 3:
                            pendingAscii = code << 5;
                            mode = 4;
                            break;
                        case 4:
                            sb.Append((char)(pendingAscii | code));
                            mode = 0;
                            break;

                        default:
                            switch (code)
                            {
                                case 0:
                                    sb.Append(' ');
                                    mode = 0;
                                    break;
                                case 1:
                                case 2:
                                case 3:
                                    // abbreviations
                                    //XXX
                                    mode = 0;
                                    break;
                                case 4:
                                    mode = 1;
                                    break;
                                case 5:
                                    mode = 2;
                                    break;
                                case 6:
                                    if (mode == 2)
                                    {
                                        mode = 3;
                                        break;
                                    }
                                    goto default;
                                default:
                                    code -= 6;
                                    switch (mode)
                                    {
                                        case 0:
                                            sb.Append(alphabet0[code]);
                                            break;
                                        case 1:
                                            sb.Append(alphabet1[code]);
                                            break;
                                        case 2:
                                            sb.Append(alphabet2[code]);
                                            break;
                                    }
                                    mode = 0;
                                    break;
                            }
                            break;
                    }
                }
            }

            return sb.ToString();
        }
    }
}
