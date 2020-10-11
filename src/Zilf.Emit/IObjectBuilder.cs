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

namespace Zilf.Emit
{
    [PublicAPI]
    public interface IObjectBuilder : IConstantOperand
    {
        string DescriptiveName { get; set; }
        IObjectBuilder? Parent { get; set; }
        IObjectBuilder? Child { get; set; }
        IObjectBuilder? Sibling { get; set; }

        void AddByteProperty(IPropertyBuilder prop, IOperand value);
        void AddWordProperty(IPropertyBuilder prop, IOperand value);
        ITableBuilder AddComplexProperty(IPropertyBuilder prop);

        void AddFlag(IFlagBuilder flag);
    }
}