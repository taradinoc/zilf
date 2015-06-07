
namespace Zilf.Compiler.Builtins
{
    internal struct BuiltinArg
    {
        public readonly BuiltinArgType Type;
        public readonly object Value;

        public BuiltinArg(BuiltinArgType type, object value)
        {
            this.Type = type;
            this.Value = value;
        }
    }
}
