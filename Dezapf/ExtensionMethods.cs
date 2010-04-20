/* Copyright 2010 Jesse McGrew
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
    static class ExtensionMethods
    {
        public static ushort ReadZWord(this BinaryReader rdr)
        {
            byte b1 = rdr.ReadByte();
            byte b2 = rdr.ReadByte();
            return (ushort)((b1 << 8) + b2);
        }
    }
}
