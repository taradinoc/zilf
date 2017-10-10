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

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.OBJECT, PrimType.LIST)]
    class ZilModelObject : ZilTiedListBase
    {
        public ZilModelObject([NotNull] ZilAtom name, [NotNull] ZilList[] props, bool isRoom)
        {
            Name = name;
            Properties = props;
            IsRoom = isRoom;
        }

        /// <exception cref="InterpreterError"><paramref name="list"/> has the wrong number or types of elements.</exception>
        [NotNull]
        [ChtypeMethod]
        public static ZilModelObject FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {
            Contract.Requires(ctx != null);

            if (list.IsEmpty)
                throw new InterpreterError(
                    InterpreterMessages._0_Must_Have_1_Element1s, 
                    "list coerced to OBJECT",
                    new CountableString("at least 1", false));

            if (!(list.First is ZilAtom atom))
                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_Must_Be_2, 1, "list coerced to OBJECT", "an atom");

            Debug.Assert(list.Rest != null);

            if (!list.Rest.All(zo => zo is ZilList))
                throw new InterpreterError(
                    InterpreterMessages._0_In_1_Must_Be_2,
                    "elements after first",
                    "list coerced to OBJECT",
                    "lists");

            // TODO: set isRoom
            return new ZilModelObject(atom, list.Rest.Cast<ZilList>().ToArray(), false);
        }

        [NotNull]
        public ZilAtom Name { get; }

        [NotNull]
        public ZilList[] Properties { get; }

        public bool IsRoom { get; }

        public ZilAtom ObjectOrRoom => GetStdAtom(IsRoom ? StdAtom.ROOM : StdAtom.OBJECT);

        [NotNull]
        public ZilList PropertiesList => new ZilList(Properties);

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