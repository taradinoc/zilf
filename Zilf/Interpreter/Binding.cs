
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    internal class Binding
    {
        public ZilObject Value;
        public Binding Prev;

        public Binding(ZilObject value)
        {
            this.Value = value;
        }
    }
}
