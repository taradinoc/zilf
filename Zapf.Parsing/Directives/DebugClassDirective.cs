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

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Zapf.Parsing.Expressions;

namespace Zapf.Parsing.Directives
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class DebugClassDirective : DebugDirective
    {
        public DebugClassDirective([NotNull] string name,
            [NotNull] AsmExpr startFile, [NotNull] AsmExpr startLine, [NotNull] AsmExpr startColumn,
            [NotNull] AsmExpr endFile, [NotNull] AsmExpr endLine, [NotNull] AsmExpr endColumn)
        {
            Name = name;
            StartFile = startFile;
            StartLine = startLine;
            StartColumn = startColumn;
            EndFile = endFile;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        [NotNull]
        public string Name { get; }
        [NotNull]
        public AsmExpr StartFile { get; }
        [NotNull]
        public AsmExpr StartLine { get; }
        [NotNull]
        public AsmExpr StartColumn { get; }
        [NotNull]
        public AsmExpr EndFile { get; }
        [NotNull]
        public AsmExpr EndLine { get; }
        [NotNull]
        public AsmExpr EndColumn { get; }
    }
}