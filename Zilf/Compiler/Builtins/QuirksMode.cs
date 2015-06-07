using System;

namespace Zilf.Compiler.Builtins
{
    [Flags]
    public enum QuirksMode
    {
        None = 0,
        Local = 1,
        Global = 2,
        Both = 3,
    }
}