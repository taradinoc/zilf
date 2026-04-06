/* Copyright 2010-2026 Tara McGrew
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
using System.Linq;

namespace Zilf.Emit.Cornerstone
{
    public sealed partial class GameBuilder
    {
        private sealed class ObjectBuilder(string name, int id) : IObjectBuilder
        {
            private readonly Dictionary<IPropertyBuilder, List<IOperand>> propertyValues = [];
            private readonly Dictionary<IPropertyBuilder, TableBuilder> complexPropertyTables = [];
            private readonly HashSet<IFlagBuilder> setFlags = [];

            public string Name { get; } = name;

            public int Id { get; } = id;

            public string DescriptiveName { get; set; } = name;

            public IObjectBuilder? Parent { get; set; }

            public IObjectBuilder? Child { get; set; }

            public IObjectBuilder? Sibling { get; set; }

            public void AddByteProperty(IPropertyBuilder prop, IOperand value) => AddValue(prop, value);

            public void AddWordProperty(IPropertyBuilder prop, IOperand value) => AddValue(prop, value);

            public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
            {
                var table = new TableBuilder($"{Name}_{((PropertyBuilder)prop).Name}", pure: false);
                complexPropertyTables[prop] = table;
                return table;
            }

            public void AddFlag(IFlagBuilder flag) => setFlags.Add(flag);

            public IConstantOperand Add(IConstantOperand other) => new CompositeOperand(this, other);

            internal IReadOnlyDictionary<IPropertyBuilder, List<IOperand>> PropertyValues => propertyValues;

            internal IReadOnlyDictionary<IPropertyBuilder, TableBuilder> ComplexPropertyTables => complexPropertyTables;

            internal IReadOnlyCollection<FlagBuilder> SetFlags => setFlags.Cast<FlagBuilder>().ToArray();

            private void AddValue(IPropertyBuilder prop, IOperand value)
            {
                MarkFarSelectorDependencies(value);

                if (!propertyValues.TryGetValue(prop, out var values))
                {
                    values = [];
                    propertyValues[prop] = values;
                }

                values.Add(value);
            }
        }
    }
}