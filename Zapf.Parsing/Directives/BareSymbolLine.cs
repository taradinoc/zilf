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

namespace Zapf.Parsing.Directives
{
    public sealed class BareSymbolLine : AsmLine
    {
        public BareSymbolLine([NotNull] string text)
        {
            Text = text;
        }

        [NotNull]
        public string Text { get; }

        public int OperandCount { get; set; }
        public bool HasStore { get; set; }
        public bool HasBranch { get; set; }

        public bool UsedAsInstruction => OperandCount > 0 || HasStore || HasBranch;
    }
}