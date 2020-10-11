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
using Zapf.Parsing.Expressions;

namespace Zapf.Parsing.Directives
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class DebugClassDirective : DebugDirective
    {
        public DebugClassDirective(string name,
             AsmExpr startFile, AsmExpr startLine, AsmExpr startColumn,
             AsmExpr endFile, AsmExpr endLine, AsmExpr endColumn)
        {
            Name = name;
            StartFile = startFile;
            StartLine = startLine;
            StartColumn = startColumn;
            EndFile = endFile;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public string Name { get; }
        public AsmExpr StartFile { get; }
        public AsmExpr StartLine { get; }
        public AsmExpr StartColumn { get; }
        public AsmExpr EndFile { get; }
        public AsmExpr EndLine { get; }
        public AsmExpr EndColumn { get; }
    }
}