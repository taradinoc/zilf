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
    [BuiltinType(StdAtom.GLOBAL, PrimType.LIST)]
    class ZilGlobal : ZilTiedListBase
    {
        public ZilGlobal([NotNull] ZilAtom name, [CanBeNull] ZilObject value, GlobalStorageType storageType = GlobalStorageType.Any)
        {
            Name = name;
            Value = value;
            StorageType = storageType;
            IsWord = true;
        }

        /// <exception cref="InterpreterError"><paramref name="list"/> has the wrong number or types of elements.</exception>
        [NotNull]
        [ChtypeMethod]
#pragma warning disable RECS0154 // Parameter is never used
        public static ZilGlobal FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
#pragma warning restore RECS0154 // Parameter is never used
        {
            if (list.IsEmpty || list.Rest?.IsEmpty != false || list.Rest.Rest?.IsEmpty != true)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "list coerced to GLOBAL", 2);

            if (!(list.First is ZilAtom name))
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to GLOBAL", "an atom");

            var value = list.Rest.First;

            return new ZilGlobal(name, value);
        }

        [NotNull]
        public ZilAtom Name { get; }

        [CanBeNull]
        public ZilObject Value { get; }

        public GlobalStorageType StorageType { get; set; }
        public bool IsWord { get; set; }

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilGlobal>(
                x => x.Name,
                x => x.Value);
        }

        public override StdAtom StdTypeAtom => StdAtom.GLOBAL;
    }
}