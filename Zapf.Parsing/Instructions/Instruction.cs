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

using System.Collections.Generic;
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

        [NotNull]
        public string Name { get; }

        [NotNull]
        public IList<AsmExpr> Operands { get; } = new List<AsmExpr>();

        [CanBeNull]
        public string StoreTarget { get; set; }

        public bool? BranchPolarity { get; set; }
        [CanBeNull]
        public string BranchTarget { get; set; }
    }
}