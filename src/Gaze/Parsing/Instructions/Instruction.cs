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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Gaze.Parsing.Directives;
using Gaze.Parsing.Expressions;

#nullable enable

namespace Gaze.Parsing.Instructions
{
    public sealed class Instruction : AsmLine
    {
        public const string BranchTrue = "TRUE";
        public const string BranchFalse = "FALSE";

        public Instruction(string name)
        {
            Name = name;
        }

        public Instruction(string name, IEnumerable<AsmExpr> operands)
            : this(name)
        {
            ((List<AsmExpr>)Operands).AddRange(operands);
        }

        public Instruction(string name, AsmExpr operand1)
            : this(name)
        {
            Operands.Add(operand1);
        }

        public Instruction(string name, AsmExpr operand1, AsmExpr operand2)
            : this(name, operand1)
        {
            Operands.Add(operand2);
        }

        public Instruction(string name, AsmExpr operand1, AsmExpr operand2, AsmExpr operand3)
            : this(name, operand1, operand2)
        {
            Operands.Add(operand3);
        }

        public Instruction(string name, AsmExpr operand1, AsmExpr operand2, AsmExpr operand3, AsmExpr operand4)
            : this(name, operand1, operand2, operand3)
        {
            Operands.Add(operand4);
        }

        public string Name { get; }

        public IList<AsmExpr> Operands { get; } = new List<AsmExpr>();

        public Instruction WithName(string newName)
        {
            var result = new Instruction(newName);
            ((List<AsmExpr>)result.Operands).AddRange(Operands);
            return result;
        }

        public override string ToString()
        {
            if (Operands.Count == 0)
                return Name;

            var sb = new StringBuilder(Name.Length + Operands.Count * 4);

            sb.Append(Name);

            for (int i = 0; i < Operands.Count; i++)
            {
                sb.Append(i == 0 ? ' ' : ',');
                sb.Append(Operands[i]);
            }

            return sb.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is Instruction other &&
                   other.Name == Name &&
                   other.Operands.SequenceEqual(Operands);
        }

        public override int GetHashCode()
        {
            var result = Name.GetHashCode();
            foreach (var o in Operands)
                result = result * 17 + o.GetHashCode();

            return result;
        }
    }
}