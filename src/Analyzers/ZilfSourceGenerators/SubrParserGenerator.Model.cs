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

namespace ZilfSourceGenerators;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

partial class SubrParserGenerator
{
    // Data structures for SUBR method information
    public class SubrMethodInfo
    {
        public MethodDeclarationSyntax Method { get; set; } = null!;
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public SubrAttributeInfo[] AttributeInfos { get; set; } = null!;
        public MdlZilRedirectInfo? MdlZilRedirect { get; set; }
    }

    public class SubrAttributeInfo
    {
        public string Name { get; set; } = "";
        public bool IsFSubr { get; set; }
        public string ObList { get; set; } = "";
    }

    public class MdlZilRedirectInfo
    {
        public string TargetType { get; set; } = "";
        public string TargetMethod { get; set; } = "";
        public bool TopLevelOnly { get; set; }
    }

    // Utility: remove obvious comment lines from an extracted source text block
    private static IEnumerable<string> FilterOutCommentLines(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        foreach (var raw in lines)
        {
            var lnTrim = raw.Trim();
            if (string.IsNullOrEmpty(lnTrim))
                continue; // skip empty/whitespace-only lines to avoid spurious blank lines
            var ln = raw.TrimStart();
            if (ln.StartsWith("//")) continue;
            if (ln.StartsWith("/*")) continue;
            if (ln.StartsWith("*/")) continue;
            if (ln.StartsWith("///")) continue;
            if (ln.StartsWith("#")) continue; // skip preprocessor directives like #pragma
            if (ln.StartsWith("* ") || ln == "*") continue; // lines already in block comments
            yield return raw.TrimEnd();
        }
    }
}
