/* Copyright 2010-2023 Tara McGrew
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zilf.Compiler.Builtins;
using Zilf.Interpreter;

namespace Zilf.Language.Signatures
{
    sealed class ZBuiltinSignature : ISignature
    {
        public IReadOnlyList<ISignaturePart> Parts { get; }
        public ISignaturePart ReturnPart { get; }
        public int MinArgs { get; }
        public int? MaxArgs { get; }
        public int MinVersion { get; }
        public int MaxVersion { get; }

        ZBuiltinSignature(int minArgs, int? maxArgs, int minVersion, int maxVersion,
            IReadOnlyList<ISignaturePart> parts, ISignaturePart returnPart)
        {
            MinArgs = minArgs;
            MaxArgs = maxArgs;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            Parts = parts;
            ReturnPart = returnPart;
        }

        public static ISignature FromGeneratedParts(ISignaturePart[] parts, ISignaturePart returnPart,
            int minArgs, int? maxArgs, int minVersion, int maxVersion)
        {
            return new ZBuiltinSignature(minArgs, maxArgs, minVersion, maxVersion, parts.ToArray(), returnPart);
        }
    }
}
