
namespace Zilf.Compiler
{
    internal struct ArgCountRange
    {
        public readonly int MinArgs;
        public readonly int? MaxArgs;

        public ArgCountRange(int min, int? max)
        {
            this.MinArgs = min;
            this.MaxArgs = max;
        }
    }
}