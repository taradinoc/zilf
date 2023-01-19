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

using System.Linq;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.OBJECT, PrimType.LIST)]
    sealed class ZilModelObject : ZilTiedListBase
    {
        public ZilModelObject(ZilAtom name, ZilList[] props, bool isRoom)
        {
            Name = name;
            Properties = props;
            IsRoom = isRoom;
        }

        /// <exception cref="InterpreterError"><paramref name="list"/> has the wrong number or types of elements.</exception>
        [ChtypeMethod]
        public static ZilModelObject FromList(ZilListBase list)
        {
            if (!list.IsCons(out var first, out var rest))
                throw new InterpreterError(
                    InterpreterMessages._0_Must_Have_1_Element1s,
                    "list coerced to OBJECT",
                    new CountableString("at least 1", false));

            if (first is not ZilAtom objectOrRoom)
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to OBJECT", "an atom");

            if (!rest.IsCons(out first, out var props) || first is not ZilAtom atom)
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 2, "list coerced to OBJECT", "an atom");

            if (!props.All(zo => zo is ZilList))
                throw new InterpreterError(
                    InterpreterMessages._0_In_1_Must_Be_2,
                    "elements after first",
                    "list coerced to OBJECT",
                    "lists");

            return new ZilModelObject(atom, props.Cast<ZilList>().ToArray(), objectOrRoom.StdAtom == StdAtom.ROOM);
        }

        public ZilAtom Name { get; }

        public ZilList[] Properties { get; }

        public bool IsRoom { get; }

        public ZilAtom ObjectOrRoom => GetStdAtom(IsRoom ? StdAtom.ROOM : StdAtom.OBJECT);

        public ZilList PropertiesList => new(Properties);

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ZilModelObject>(
                x => x.ObjectOrRoom,
                x => x.Name)
                .WithCatchAll<ZilModelObject>(x => x.PropertiesList);
        }

        public override StdAtom StdTypeAtom => StdAtom.OBJECT;
    }
}