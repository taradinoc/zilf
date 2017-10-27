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

using System;

namespace Zapf.Parsing.Instructions
{
    [Flags]
    public enum ZOpFlags
    {
/*
        None = 0,
*/
        /// <summary>
        /// The instruction stores a result.
        /// </summary>
        Store = 1,
        /// <summary>
        /// The instruction branches to a label.
        /// </summary>
        Branch = 2,
        /// <summary>
        /// The instruction takes an extra operand type byte, for a total of 8 possible operands.
        /// ("XCALL", "IXCALL")
        /// </summary>
        Extra = 4,
        /// <summary>
        /// The instruction is nominally 2OP but can take up to 4 operands.
        /// ("EQUAL?")
        /// </summary>
        VarArgs = 8,
        /// <summary>
        /// The instruction has a string literal operand.
        /// ("PRINTI", "PRINTR")
        /// </summary>
        String = 16,
        /// <summary>
        /// The instruction can take a local label operand.
        /// ("JUMP")
        /// </summary>
        Label = 32,
        /// <summary>
        /// The instruction's first operand is an indirect variable number.
        /// </summary>
        IndirectVar = 64,
        /// <summary>
        /// The instruction's first operand is a packed routine address.
        /// </summary>
        Call = 128,
        /// <summary>
        /// Control flow does not pass to the following instruction.
        /// </summary>
        Terminates = 256,
    }
}