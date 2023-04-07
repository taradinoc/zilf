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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZilfSourceGenerators
{
    [Generator]
    public class SubrWrapperGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Records information about a method that should be exposed to user code
        /// as a SUBR (or FSUBR) under one or more names.
        /// </summary>
        /// <param name="MethodName">The name of the method.</param>
        /// <param name="Params">An array of <see cref="Param"/>
        /// describing the method's parameters and their attributes.</param>
        /// <param name="ReturnParam">A <see cref="Param"/> describing the method's
        /// return value and its attributes.</param>
        /// <param name="Targets">An array of <see cref="TargetAtom"/> detailing
        /// how the method should be exposed.</param>
        record SubrMethodInfo(string MethodName, Param[] Params, Param ReturnParam, TargetAtom[] Targets);

        /// <summary>
        /// Records information about a particular exposure of a method as a SUBR (or FSUBR).
        /// </summary>
        /// <param name="IsFSubr"><c>true</c> if the method should be exposed as an
        /// FSUBR, i.e., without evaluating its arguments at the call site.</param>
        /// <param name="Atom">The name under which the method should be exposed.</param>
        /// <param name="ObList">The name of the OBLIST in which the <paramref name="Atom"/>
        /// should be inserted, or <c>null</c> if it should be inserted into the root OBLIST.</param>
        record TargetAtom(bool IsFSubr, string Atom, string? ObList);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // extract SubrMethodInfo from method declarations that look like SUBRs
            var subrInfos = context.SyntaxProvider.CreateSyntaxProvider(
                (n, _) => n is MethodDeclarationSyntax mds && IsPotentialSubrMethodSyntax(mds),
                ExtractSubrMethodInfo);

            // split out the SubrMethodInfo into individual (method name, target atom) pairs
            var subrInfosSplit = subrInfos.SelectMany((info, _) => info switch
                {
                    null => Enumerable.Empty<(string methodName, TargetAtom target)>(),
                    _ => info.Targets.Select(t => (methodName: info.MethodName, target: t)),
                });

            // produce a source file containing a static method that registers all the SUBRs,
            // plus static methods that parse the arguments to each SUBR and call the underlying
            // methods.
            context.RegisterSourceOutput(subrInfosSplit.Collect(), (spc, infos) =>
            {
                var lines = from si in infos
                            let kind = si.target.IsFSubr ? "FSUBR" : "SUBR"
                            let name = si.target.ObList switch
                            {
                                string oblist => $"{si.target.Atom}!-{si.target.ObList}",
                                _ => si.target.Atom,
                            }
                            orderby name, si.methodName
                            select $@"// {kind} ""{name}"" -> {si.methodName}";

                var sourceText = SourceText.From($@"
namespace Zilf.Interpreter;

static partial class Subrs_Wrapper
{{
    public static (string name, string? oblist, SubrDelegate del)[] GetSubrWrappers()
    {{
        return new[] {{
            {string.Join(",\r\n            ", lines)}
        }};
    }}
}}
", Encoding.UTF8);

                spc.AddSource("Subrs.g.cs", sourceText);
            });
        }

        private static bool IsPotentialSubrMethodSyntax(MethodDeclarationSyntax mds) =>
            mds.Parent is ClassDeclarationSyntax cds &&
            cds.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
            cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
            mds.AttributeLists.Any(als => als.Attributes.Any(attr => IsPotentialSubrAttribute(attr)));

        private static bool IsPotentialSubrAttribute(AttributeSyntax syntaxNode)
        {
            var name = syntaxNode.Name.ToString();
            return
                name.EndsWith("Subr", StringComparison.Ordinal) ||
                name.EndsWith("SubrAttribute", StringComparison.Ordinal);
        }

        private static SubrMethodInfo? ExtractSubrMethodInfo(GeneratorSyntaxContext gsc, CancellationToken cancellationToken)
        {
            if (gsc.SemanticModel.GetDeclaredSymbol(gsc.Node) is not IMethodSymbol methodSymbol)
                return null;

            var methodName = $"{methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{methodSymbol.Name}";
            var targets = new List<TargetAtom>();

            foreach (var attr in methodSymbol.GetAttributes())
            {
                bool isFSubr;
                string atom;

                switch (attr.AttributeClass?.Name)
                {
                    case "SubrAttribute":
                        isFSubr = false;
                        break;
                    case "FSubrAttribute":
                        isFSubr = true;
                        break;
                    default:
                        continue;
                }

                if (attr.ConstructorArguments.IsEmpty)
                {
                    atom = methodSymbol.Name;
                }
                else if (attr.ConstructorArguments.First() is { Type.SpecialType: SpecialType.System_String, Value: string s })
                {
                    atom = s;
                }
                else
                {
                    continue;
                }

                string? oblist = attr.NamedArguments.FirstOrDefault(pair => pair.Key == "ObList").Value switch
                {
                    { Type.SpecialType: SpecialType.System_String, Value: string s } => s,
                    _ => null,
                };

                targets.Add(new TargetAtom(isFSubr, atom, oblist));
            }

            return new SubrMethodInfo(methodName, targets.ToArray());
        }
    }
}
