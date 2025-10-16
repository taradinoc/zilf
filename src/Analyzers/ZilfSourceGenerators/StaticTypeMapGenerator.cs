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
using System.Linq;
using System.Text;

namespace ZilfSourceGenerators
{
    [Generator]
    public class StaticTypeMapGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classesWithAttr = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: "Zilf.Interpreter.BuiltinTypeAttribute",
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => ((INamedTypeSymbol)ctx.TargetSymbol, ctx.Attributes[0]))
                .Collect();

            var compilationAndClasses = context.CompilationProvider.Combine(classesWithAttr);

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) =>
            {
                var (compilation, items) = source;

                var builtinTypeAttr = compilation.GetTypeByMetadataName("Zilf.Interpreter.BuiltinTypeAttribute");
                var chtypeMethodAttr = compilation.GetTypeByMetadataName("Zilf.Interpreter.ChtypeMethodAttribute");
                var contextType = compilation.GetTypeByMetadataName("Zilf.Interpreter.Context");
                if (builtinTypeAttr is null || chtypeMethodAttr is null || contextType is null)
                {
                    return;
                }

                var emission = new StringBuilder();
                emission.AppendLine("using System;");
                emission.AppendLine("using System.Collections.Generic;");
                emission.AppendLine("using System.Collections.ObjectModel;");
                emission.AppendLine("using Zilf.Language;");
                emission.AppendLine("namespace Zilf.Interpreter");
                emission.AppendLine("{");
                emission.AppendLine("    partial class Context");
                emission.AppendLine("    {");
                emission.AppendLine("        private static partial ReadOnlyDictionary<StdAtom, TypeMapEntry> GetGeneratedStaticTypeMap()");
                emission.AppendLine("        {");
                emission.AppendLine("            var result = new Dictionary<StdAtom, TypeMapEntry>();");

                var processed = new HashSet<string>();

                foreach (var (clsSymbol, attrData) in items)
                {
                    if (clsSymbol is null)
                        continue;

                    // Extract the enum argument code as written in source, e.g. "StdAtom.LIST" and "PrimType.LIST"
                    var attrSyntax = attrData.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
                    var args = attrSyntax?.ArgumentList?.Arguments;
                    if (args is null || args.Value.Count < 2)
                        continue;
                    var stdAtomCode = args.Value[0].ToString();
                    var primTypeCode = args.Value[1].ToString();

                    var fqType = clsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    // Find Chtype method or constructor
                    IMethodSymbol? targetMethod = null;
                    IMethodSymbol? targetCtor = null;

                    foreach (var m in clsSymbol.GetMembers().OfType<IMethodSymbol>())
                    {
                        if (!m.IsStatic) continue;
                        if (m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, chtypeMethodAttr)))
                        {
                            if (targetMethod != null)
                            {
                                ReportDiagnosticMultiple(spc, m.Locations.FirstOrDefault(), m.ContainingType.Name);
                                targetMethod = null; targetCtor = null; break;
                            }
                            targetMethod = m;
                        }
                    }

                    foreach (var ctor in clsSymbol.Constructors)
                    {
                        if (ctor.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, chtypeMethodAttr)))
                        {
                            if (targetMethod != null || targetCtor != null)
                            {
                                ReportDiagnosticMultiple(spc, ctor.Locations.FirstOrDefault(), ctor.ContainingType.Name);
                                targetMethod = null; targetCtor = null; break;
                            }
                            targetCtor = ctor;
                        }
                    }

                    if (targetMethod is null && targetCtor is null)
                    {
                        // No chtype member; skip
                        continue;
                    }

                    string chtypeLambda;
                    if (targetMethod is not null)
                    {
                        var pars = targetMethod.Parameters;
                        if (pars.Length == 1)
                        {
                            var pType = pars[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            var methodName = targetMethod.Name;
                            chtypeLambda = $"(ctx, zo) => {fqType}.{methodName}(({pType})zo)";
                        }
                        else if (pars.Length == 2 && SymbolEqualityComparer.Default.Equals(pars[0].Type, contextType))
                        {
                            var pType = pars[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            var methodName = targetMethod.Name;
                            chtypeLambda = $"(ctx, zo) => {fqType}.{methodName}(ctx, ({pType})zo)";
                        }
                        else
                        {
                            // invalid signature; skip
                            continue;
                        }
                    }
                    else
                    {
                        // constructor
                        var pars = targetCtor!.Parameters;
                        if (pars.Length == 1)
                        {
                            var pType = pars[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            chtypeLambda = $"(ctx, zo) => new {fqType}(({pType})zo)";
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (!processed.Add(fqType))
                        continue;

                    emission.AppendLine($"            result.Add({stdAtomCode}, new TypeMapEntry {{ BuiltinType = typeof({fqType}), PrimType = {primTypeCode}, ChtypeMethod = {chtypeLambda} }});");
                }

                emission.AppendLine("            return result.AsReadOnly();");
                emission.AppendLine("        }");
                emission.AppendLine("    }");
                emission.AppendLine("}");

                spc.AddSource("Context.StaticTypeMap.g.cs", SourceText.From(emission.ToString(), Encoding.UTF8));

                static void ReportDiagnosticMultiple(SourceProductionContext spc2, Location? loc, string typeName)
                {
                    var descriptor = new DiagnosticDescriptor(
                        id: "ZILFSG001",
                        title: "Multiple Chtype members",
                        messageFormat: "Type '{0}' has multiple members marked with [ChtypeMethod]",
                        category: "ZilfSourceGenerators",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true);
                    spc2.ReportDiagnostic(Diagnostic.Create(descriptor, loc, typeName));
                }
            });
        }
    }
}
