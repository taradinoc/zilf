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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Text;

namespace ZilfSourceGenerators
{
    [Generator]
    public class BuiltinTypeAttrPairsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var syntaxReceiver = (SyntaxReceiver?)context.SyntaxReceiver;

            if (syntaxReceiver == null || syntaxReceiver.Pairs.Count == 0)
            {
                // no builtin types found, nothing to do
                return;
            }

            var lines = new List<string>();
            foreach (var (cls, attr) in syntaxReceiver.Pairs)
            {
                var semanticModel = context.Compilation.GetSemanticModel(cls.SyntaxTree);

                var classSymbol = semanticModel.GetDeclaredSymbol(cls);
                var attrSymbol = ((IMethodSymbol?)semanticModel.GetSymbolInfo(attr).Symbol)?.ContainingType;

                if (classSymbol is null || attrSymbol is null)
                {
                    continue;
                }

                lines.Add($"yield return new BuiltinTypeAttrPair(typeof({classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), new {attrSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{attr.ArgumentList});");
            }

            SourceText sourceText = SourceText.From($@"
using System.Collections.Generic;
using Zilf.Language;
namespace Zilf.Interpreter
{{
    partial class Context
    {{
        private static partial IEnumerable<BuiltinTypeAttrPair> GetBuiltinTypeAttrPairs()
        {{
            {string.Join("\n            ", lines)}
        }}
    }}
}}
", Encoding.UTF8);
            context.AddSource("Context.g.cs", sourceText);
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<(ClassDeclarationSyntax cls, AttributeSyntax attr)> Pairs { get; } = [];

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds)
                {
                    foreach (var al in cds.AttributeLists)
                    {
                        foreach (var a in al.Attributes)
                        {
                            if (a.Name.ToString() is "BuiltinType" or "BuiltinTypeAttribute")
                            {
                                Pairs.Add((cds, a));
                            }
                        }
                    }
                }
            }
        }
    }
}
