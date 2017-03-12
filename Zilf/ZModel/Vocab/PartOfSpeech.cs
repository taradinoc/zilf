/* Copyright 2010-2017 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */
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