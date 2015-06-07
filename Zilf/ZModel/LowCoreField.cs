using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.ZModel
{
    sealed class LowCoreField
    {
        public int Offset { get; private set; }
        public LowCoreFlags Flags { get; private set; }
        public int MinVersion { get; private set; }

        private LowCoreField(int offset, LowCoreFlags flags = LowCoreFlags.None, int minVersion = 3)
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(minVersion >= 1);

            this.Offset = offset;
            this.Flags = flags;
            this.MinVersion = minVersion;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Offset >= 0);
            Contract.Invariant(MinVersion >= 1);
        }

        private static readonly Dictionary<string, LowCoreField> allFields = new Dictionary<string, LowCoreField>()
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
{ "LMRG", new LowCoreField(20, minVersion: 5) },
{ "RMRG", new LowCoreField(21, minVersion: 5) },
{ "CLRWRD", new LowCoreField(22, minVersion: 5) },
{ "TCHARS", new LowCoreField(23, minVersion: 5) },
{ "CRCNT", new LowCoreField(24, LowCoreFlags.Writable, minVersion: 5) },
{ "CRFUNC", new LowCoreField(25, LowCoreFlags.Writable, minVersion: 5) },
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
{ "TRUBGC", new LowCoreField(6, LowCoreFlags.Extended, minVersion: 5) },
};

        public static LowCoreField Get(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            LowCoreField result;
            allFields.TryGetValue(atom.Text, out result);
            return result;
        }
    }
}