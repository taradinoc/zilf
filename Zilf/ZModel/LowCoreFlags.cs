using System;

namespace Zilf.ZModel
{
    [Flags]
    enum LowCoreFlags
    {
        None = 0,

        Byte = 1,
        Writable = 2,
        Extended = 4,
    }
}