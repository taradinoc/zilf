using System;

namespace Zilf.Interpreter
{
    [Flags]
    enum FileFlags
    {
        None = 0,
        CleanStack = 1,
        MdlZil = 2,
    }
}
