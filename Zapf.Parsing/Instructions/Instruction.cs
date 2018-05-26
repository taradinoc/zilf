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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Zapf.Parsing.Directives;
using Zapf.Parsing.Expressions;

namespace Zapf.Parsing.Instructions
{
    public sealed class Instruction : AsmLine
    {
        public const string BranchTrue = "TRUE";
        public const string BranchFalse = "FALSE";

        public Instruction([NotNull] string name)
        {
            Name = name;
        }

        public Instruction([NotNull] string name, [NotNull] IEnumerable<AsmExpr> operands)
            : this(name)
        {
            ((List<AsmExpr>)Operands).AddRange(operands);
        }

        public Instruction([NotNull] string name, [NotNull] AsmExpr operand1)
            : this(name)
        {
            Operands.Add(operand1);
        }

        public Instruction([NotNull] string name, [NotNull] AsmExpr operand1, [NotNull] AsmExpr operand2)
            : this(name, operand1)
        {
            Operands.Add(operand2);
        }

        public Instruction([NotNull] string name, [NotNull] AsmExpr operand1, [NotNull] AsmExpr operand2, [NotNull] AsmExpr operand3)
            : this(name, operand1, operand2)
        {
            Operands.Add(operand3);
        }

        public Instruction([NotNull] string name, [NotNull] AsmExpr operand1, [NotNull] AsmExpr operand2, [NotNull] AsmExpr operand3, [NotNull] AsmExpr operand4)
            : this(name, operand1, operand2, operand3)
        {
            Operands.Add(operand4);
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public IList<AsmExpr> Operands { get; } = new List<AsmExpr>();

        [CanBeNull]
        public string StoreTarget { get; set; }

        public bool? BranchPolarity { get; set; }
        [CanBeNull]
        public string BranchTarget { get; set; }

        [NotNull]
        public Instruction WithStoreTarget([CanBeNull] string newStoreTarget)
        {
            var result = new Instruction(Name);
            ((List<AsmExpr>)result.Operands).AddRange(Operands);
            result.StoreTarget = newStoreTarget;
            return result;
        }

        [NotNull]
        public Instruction WithName([NotNull] string newName)
        {
            var result = new Instruction(newName) { StoreTarget = StoreTarget };
            ((List<AsmExpr>)result.Operands).AddRange(Operands);
            return result;
        }

        public override string ToString()
        {
            if (Operands.Count == 0 && string.IsNullOrEmpty(StoreTarget))
                return Name;

            var sb = new StringBuilder(Name.Length + Operands.Count * 4);

            sb.Append(Name);

            for (int i = 0; i < Operands.Count; i++)
            {
                sb.Append(i == 0 ? ' ' : ',');
                sb.Append(Operands[i]);
            }

            if (!string.IsNullOrEmpty(StoreTarget))
            {
                sb.Append(" >");
                sb.Append(StoreTarget);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is Instruction other &&
                   other.Name == Name &&
                   other.Operands.SequenceEqual(Operands) &&
                   other.StoreTarget == StoreTarget &&
                   other.BranchPolarity == BranchPolarity &&
                   other.BranchTarget == BranchTarget;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var result = Name.GetHashCode();
            foreach (var o in Operands)
                result = result * 17 + o.GetHashCode();

            if (StoreTarget != null)
                result = result * 17 + StoreTarget.GetHashCode();
            result = result * 17 + BranchPolarity.GetHashCode();
            if (BranchTarget != null)
                result = result * 17 + BranchTarget.GetHashCode();

            return result;
        }
    }
}