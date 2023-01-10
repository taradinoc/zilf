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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZilfAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessageConstantAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor Rule_DuplicateMessageCode = new(
            DiagnosticIds.DuplicateMessageCode,
            "Duplicate message code",
            "The code '{0}' is used more than once in message set '{1}'",
            "Error Reporting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        static readonly DiagnosticDescriptor Rule_DuplicateMessageFormat = new(
            DiagnosticIds.DuplicateMessageFormat,
            "Duplicate message format",
            "This format string is used more than once in message set '{0}'",
            "Error Reporting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        static readonly DiagnosticDescriptor Rule_PrefixedMessageFormat = new(
            DiagnosticIds.PrefixedMessageFormat,
            "Message has hardcoded prefix",
            "This format string has the prefix '{0}', which should be moved to the call site",
            "Error Reporting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly Regex PrefixedMessageFormatRegex = new(
            @"^(?<prefix>[^a-z .,;:()\[\]{}]+)(?<rest>: .*)$");
        public static readonly Regex FormatTokenRegex = new(
            @"\{(?<number>\d+)(?<suffix>:[^}]*)?\}");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                Rule_DuplicateMessageCode,
                Rule_DuplicateMessageFormat,
                Rule_PrefixedMessageFormat);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            if (!IsMessageSet(classDecl, context.SemanticModel))
                return;

            var seenCodes = ImmutableHashSet<int>.Empty;
            var seenFormats = ImmutableDictionary<string, Location>.Empty;

            foreach (var field in GetConstIntFields(classDecl, context.SemanticModel))
            {
                foreach (var attr in field.DescendantNodes().OfType<AttributeSyntax>())
                {
                    if (IsMessageAttribute(context.SemanticModel, attr) && attr.ArgumentList?.Arguments.Count >= 1)
                    {
                        var formatExpr = attr.ArgumentList.Arguments[0].Expression;
                        var constValue = context.SemanticModel.GetConstantValue(formatExpr);

                        if (!constValue.HasValue || constValue.Value is not string formatStr)
                            continue;

                        // check for duplicate message
                        Diagnostic diagnostic;

                        if (seenFormats.ContainsKey(formatStr))
                        {
                            diagnostic = Diagnostic.Create(
                                Rule_DuplicateMessageFormat,
                                formatExpr.GetLocation(),
                                new[] { seenFormats[formatStr] },
                                classDecl.Identifier);

                            context.ReportDiagnostic(diagnostic);
                        }
                        else
                        {
                            seenFormats = seenFormats.Add(formatStr, formatExpr.GetLocation());
                        }

                        // check for prefixed message
                        var match = PrefixedMessageFormatRegex.Match(formatStr);

                        if (!match.Success)
                            continue;

                        diagnostic = Diagnostic.Create(
                            Rule_PrefixedMessageFormat,
                            formatExpr.GetLocation(),
                            match.Groups["prefix"].Value);

                        context.ReportDiagnostic(diagnostic);
                    }
                }

                foreach (var varDecl in field.Declaration.Variables)
                {
                    if (varDecl.Initializer != null)
                    {
                        var constValue = context.SemanticModel.GetConstantValue(varDecl.Initializer.Value);

                        if (!constValue.HasValue || constValue.Value == null)
                            continue;

                        // check for duplicate code
                        var value = (int)constValue.Value;

                        if (seenCodes.Contains(value))
                        {
                            var diagnostic = Diagnostic.Create(
                                Rule_DuplicateMessageCode,
                                varDecl.GetLocation(),
                                varDecl.Initializer.Value,
                                classDecl.Identifier);

                            context.ReportDiagnostic(diagnostic);
                        }
                        else
                        {
                            seenCodes = seenCodes.Add(value);
                        }
                    }
                }
            }
        }

        public static bool IsMessageAttribute(SemanticModel semanticModel, AttributeSyntax attr)
        {
            var type = semanticModel.GetTypeInfo(attr).Type;

            while (type != null)
            {
                if (type.Name == "MessageAttribute")
                    return true;

                type = type.BaseType;
            }

            return false;
        }

        static IEnumerable<FieldDeclarationSyntax> GetConstIntFields(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
        {
            return from field in classDecl.Members.OfType<FieldDeclarationSyntax>()
                   where field.Modifiers.Any(SyntaxKind.PublicKeyword) && field.Modifiers.Any(SyntaxKind.ConstKeyword)
                   where semanticModel.GetTypeInfo(field.Declaration.Type).Type?.SpecialType == SpecialType.System_Int32
                   select field;
        }

        static bool IsMessageSet(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
        {
            var attributes = from alist in classDecl.AttributeLists
                             from attr in alist.Attributes
                             select semanticModel.GetTypeInfo(attr);

            return attributes.Any(ti =>
                ti.Type?.Name == "MessageSetAttribute" &&
                ti.Type?.ContainingNamespace.ToString() == "Zilf.Diagnostics");
        }
    }
}
