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
    public class BuiltinTypeAttrPairsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classWithAttributes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                    transform: static (context, _) =>
                    {
                        var classDeclaration = (ClassDeclarationSyntax)context.Node;
                        foreach (var attributeList in classDeclaration.AttributeLists)
                        {
                            foreach (var attribute in attributeList.Attributes)
                            {
                                if (attribute.Name.ToString() is "BuiltinType" or "BuiltinTypeAttribute")
                                {
                                    return (classDeclaration, attribute);
                                }
                            }
                        }
                        return default;
                    })
                .Where(pair => pair != default);

            var compilationAndClasses = context.CompilationProvider.Combine(classWithAttributes.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) =>
            {
                var (compilation, pairs) = source;

                var lines = new List<string>();
                foreach (var (cls, attr) in pairs)
                {
                    var semanticModel = compilation.GetSemanticModel(cls.SyntaxTree);

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
                spc.AddSource("Context.g.cs", sourceText);
            });
        }
    }
}
