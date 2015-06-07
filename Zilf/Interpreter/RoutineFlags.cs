using System;

namespace Zilf.Interpreter
{
    [Flags]
    enum RoutineFlags
    {
        None = 0,
        CleanStack = 1,
    }
}
