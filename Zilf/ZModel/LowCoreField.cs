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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.ZModel
{
    sealed class LowCoreField
    {
        public int Offset { get; }
        public LowCoreFlags Flags { get; }
        public int MinVersion { get; }
        public int? MaxVersion { get; }

        LowCoreField(int offset, LowCoreFlags flags = LowCoreFlags.None, int minVersion = 3, int? maxVersion = null)
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(minVersion >= 1);
            Contract.Requires(maxVersion == null || maxVersion >= minVersion);

            this.Offset = offset;
            this.Flags = flags;
            this.MinVersion = minVersion;
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Offset >= 0);
            Contract.Invariant(MinVersion >= 1);
            Contract.Invariant(MaxVersion == null || MaxVersion >= MinVersion);
        }

#pragma warning disable RECS0070 // Redundant explicit argument name specification
        static readonly Dictionary<string, LowCoreField> allFields = new Dictionary<string, LowCoreField>
        {
            { "ZVERSION", new LowCoreField(0) },
            { "ZORKID", new LowCoreField(1) },
            { "RELEASEID", new LowCoreField(1) },
            { "ENDLOD", new LowCoreField(2) },
            { "START", new LowCoreField(3) },
            { "VOCAB", new LowCoreField(4) },
            { "OBJECT", new LowCoreField(5) },
            { "GLOBALS", new LowCoreField(6) },
            { "PURBOT", new LowCoreField(7) },
            { "FLAGS", new LowCoreField(8, LowCoreFlags.Writable) },
            { "SERIAL", new LowCoreField(9) },
            { "SERI1", new LowCoreField(10) },
            { "SERI2", new LowCoreField(11) },
            { "FWORDS", new LowCoreField(12) },
            { "PLENTH", new LowCoreField(13) },
            { "PCHKSM", new LowCoreField(14) },
            { "INTWRD", new LowCoreField(15) },
            { "INTID", new LowCoreField(30, LowCoreFlags.Byte) },
            { "INTVR", new LowCoreField(31, LowCoreFlags.Byte) },
            { "SCRWRD", new LowCoreField(16, minVersion: 4) },
            { "SCRV", new LowCoreField(32, LowCoreFlags.Byte, minVersion: 4) },
            { "SCRH", new LowCoreField(33, LowCoreFlags.Byte, minVersion: 4) },
            { "HWRD", new LowCoreField(17, minVersion: 5) },
            { "VWRD", new LowCoreField(18, minVersion: 5) },
            { "FWRD", new LowCoreField(19, minVersion: 5) },
            { "LMRG", new LowCoreField(20, minVersion: 5, maxVersion: 5) },
            { "FOFF", new LowCoreField(20, minVersion: 5) },    // V6 and V7
            { "RMRG", new LowCoreField(21, minVersion: 5, maxVersion: 5) },
            { "SOFF", new LowCoreField(21, minVersion: 5) },    // V6 and V7
            { "CLRWRD", new LowCoreField(22, minVersion: 5) },
            { "TCHARS", new LowCoreField(23, minVersion: 5) },
            { "CRCNT", new LowCoreField(24, LowCoreFlags.Writable, minVersion: 5, maxVersion: 5) },
            { "TWID", new LowCoreField(24, minVersion: 6) },
            { "CRFUNC", new LowCoreField(25, LowCoreFlags.Writable, minVersion: 5, maxVersion: 5) },
            { "CHRSET", new LowCoreField(26, minVersion: 5) },
            { "EXTAB", new LowCoreField(27, minVersion: 5) },
            { "MSLOCX", new LowCoreField(1, LowCoreFlags.Extended, minVersion: 5) },
            { "MSLOCY", new LowCoreField(2, LowCoreFlags.Extended, minVersion: 5) },
            { "MSETBL", new LowCoreField(3, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEDIR", new LowCoreField(4, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEINV", new LowCoreField(5, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEVRB", new LowCoreField(6, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEWRD", new LowCoreField(7, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "BUTTON", new LowCoreField(8, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "JOYSTICK", new LowCoreField(9, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "BSTAT", new LowCoreField(10, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "JSTAT", new LowCoreField(11, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },

            { "STDREV", new LowCoreField(25) },
            { "UNITBL", new LowCoreField(3, LowCoreFlags.Extended, minVersion: 5) },
            { "FLAGS3", new LowCoreField(4, LowCoreFlags.Extended, minVersion: 5) },
            { "TRUFGC", new LowCoreField(5, LowCoreFlags.Extended, minVersion: 5) },
            { "TRUBGC", new LowCoreField(6, LowCoreFlags.Extended, minVersion: 5) }
        };
#pragma warning restore RECS0070 // Redundant explicit argument name specification

        public static LowCoreField Get(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            LowCoreField result;
            allFields.TryGetValue(atom.Text, out result);
            return result;
        }
    }
}