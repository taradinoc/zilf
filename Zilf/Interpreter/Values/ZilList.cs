/* Copyright 2010-2018 Jesse McGrew
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

using System.Collections.Generic;
using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.LIST, PrimType.LIST)]
    sealed class ZilList : ZilListBase
    {
        public ZilList([NotNull] IEnumerable<ZilObject> sequence)
            : base(sequence)
        {
        }

        public ZilList(ZilObject first, ZilListoidBase rest)
            : base(first, rest) { }

        [NotNull]
        [ChtypeMethod]
        public static ZilList FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {
            return new ZilList(list.First, list.Rest);
        }

        public override StdAtom StdTypeAtom => StdAtom.LIST;

        protected override string OpenBracket => "(";

        protected override ZilResult EvalImpl(Context ctx, LocalEnvironment environment, ZilAtom originalType)
        {
            var result = EvalSequence(ctx, this, environment).ToZilListResult(SourceLine);
            if (result.ShouldPass())
                return result;

            return originalType != null ? ctx.ChangeType((ZilObject)result, originalType) : result;
        }
    }
}