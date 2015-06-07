using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    struct ArgItem
    {
        public enum ArgType { Required, Optional, Auxiliary }

        public readonly ZilAtom Atom;
        public readonly bool Quoted;
        public readonly ZilObject DefaultValue;
        public readonly ArgType Type;

        public ArgItem(ZilAtom atom, bool quoted, ZilObject defaultValue, ArgType type)
        {
            this.Atom = atom;
            this.Quoted = quoted;
            this.DefaultValue = defaultValue;
            this.Type = type;
        }
    }
}