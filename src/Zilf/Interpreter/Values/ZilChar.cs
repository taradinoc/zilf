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

using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.CHARACTER, PrimType.FIX)]
    class ZilChar : ZilObject
    {
        readonly int value;

        public ZilChar(char ch)
            : this((int)ch)
        {
        }

        ZilChar(int value)
        {
            this.value = value;
        }

        [NotNull]
        [ChtypeMethod]
        public static ZilChar FromFix([NotNull] Context ctx, [NotNull] ZilFix fix)
        {
            return new ZilChar(fix.Value);
        }

        public char Char => (char)value;

        public override string ToString()
        {
            return "!\\" + Char;
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return friendly ? Char.ToString() : ToString();
        }

        public override StdAtom StdTypeAtom => StdAtom.CHARACTER;

        public override PrimType PrimType => PrimType.FIX;

        [NotNull]
        public override ZilObject GetPrimitive(Context ctx) => new ZilFix(value);

        public override bool ExactlyEquals(ZilObject obj)
        {
            return obj is ZilChar other && other.value == value;
        }

        public override int GetHashCode() => value;
    }
}