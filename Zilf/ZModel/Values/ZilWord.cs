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

using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.WORD, PrimType.LIST)]
    sealed class ZilWord : ZilTiedListBase
    {
        public ZilWord([NotNull] ZilObject value)
        {

            Value = value;
        }

        public override StdAtom StdTypeAtom => StdAtom.WORD;

        public ZilObject Value { get; }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilWord>(x => x.Value);
        }

        [ChtypeMethod]
        [NotNull]
        public static ZilWord FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {

            if (list.First == null || list.Rest == null || !list.Rest.IsEmpty)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "list coerced to WORD", 1);

            return new ZilWord(list.First);
        }
    }
}