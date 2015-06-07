using System;

namespace Zilf.ZModel.Vocab
{
    [Flags]
    enum PartOfSpeech : byte
    {
        None = 0,
        FirstMask = 3,

        //ObjectFirst = 0,
        VerbFirst = 1,
        AdjectiveFirst = 2,
        DirectionFirst = 3,

        //PrepositionFirst = 0,
        //BuzzwordFirst = 0,

        Buzzword = 4,
        Preposition = 8,
        Direction = 16,
        Adjective = 32,
        Verb = 64,
        Object = 128,
    }
}