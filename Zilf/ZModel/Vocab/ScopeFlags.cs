using System;

namespace Zilf.ZModel.Vocab
{
    [Flags]
    enum ScopeFlags : byte
    {
        None = 0,

        Have = 2,
        Many = 4,
        Take = 8,
        OnGround = 16,
        InRoom = 32,
        Carried = 64,
        Held = 128,

        Default = OnGround | InRoom | Carried | Held,
    }
}