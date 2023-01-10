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

using System.Diagnostics.CodeAnalysis;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values.Tied;

namespace Zilf.Interpreter.Values
{
    struct AsocResult
    {
        public ZilObject Item;
        public ZilObject Indicator;
        public ZilObject Value;
    }

    [BuiltinType(StdAtom.ASOC, PrimType.LIST)]
    class ZilAsoc : ZilTiedListBase
    {
        readonly AsocResult[] results;
        readonly int index;

        public ZilAsoc(AsocResult[] results, int index)
        {
            this.results = results;
            this.index = index;
        }

        /// <exception cref="InterpreterError">Always thrown.</exception>
        [ChtypeMethod]
        [DoesNotReturn]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        [SuppressMessage("Performance", "CA1801:Unused parameter")]
        public static ZilAsoc FromList(Context ctx, ZilListBase list) =>
            throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Not_Supported, "ASOC");

        public ZilObject Item => results[index].Item;
        public ZilObject Indicator => results[index].Indicator;
        public ZilObject Value => results[index].Value;

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilAsoc>(
                x => x.Item,
                x => x.Indicator,
                x => x.Value);
        }

        public ZilAsoc? GetNext()
        {
            return index + 1 < results.Length ? new ZilAsoc(results, index + 1) : null;
        }

        public override StdAtom StdTypeAtom => StdAtom.ASOC;
    }
}
