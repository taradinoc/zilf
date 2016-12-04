using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using ZilfErrorMessages;

namespace ZilfErrorMessages
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessageConstantAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId_DuplicateMessageCode = "ZILF0002";
        public const string DiagnosticId_DuplicateMessageFormat = "ZILF0003";

        private static readonly DiagnosticDescriptor Rule_DuplicateMessageCode = new DiagnosticDescriptor(
            DiagnosticId_DuplicateMessageCode,
            "Duplicate message code",
            "The code '{0}' is used more than once in message set '{1}'",
            "Error Reporting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Rule_DuplicateMessageFormat = new DiagnosticDescriptor(
            DiagnosticId_DuplicateMessageFormat,
            "Duplicate message format",
            "This format string is used more than once in message set '{0}'",
            "Error Reporting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule_DuplicateMessageCode, Rule_DuplicateMessageFormat); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
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
                    if (context.SemanticModel.GetTypeInfo(attr).Type?.Name == "MessageAttribute")
                    {
                        if (attr.ArgumentList.Arguments.Count >= 1)
                        {
                            var formatExpr = attr.ArgumentList.Arguments[0].Expression;
                            var constValue = context.SemanticModel.GetConstantValue(formatExpr);

                            if (constValue.HasValue)
                            {
                                var formatStr = constValue.Value as string;

                                if (formatStr != null)
                                {
                                    if (seenFormats.ContainsKey(formatStr))
                                    {
                                        var diagnostic = Diagnostic.Create(
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
                                }
                            }
                        }
                    }
                }

                foreach (var varDecl in field.Declaration.Variables)
                {
                    if (varDecl.Initializer != null)
                    {
                        var constValue = context.SemanticModel.GetConstantValue(varDecl.Initializer.Value);

                        if (constValue.HasValue)
                        {
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
        }

        private static IEnumerable<FieldDeclarationSyntax> GetConstIntFields(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
        {
            return from field in classDecl.Members.OfType<FieldDeclarationSyntax>()
                   where field.Modifiers.Any(SyntaxKind.PublicKeyword) && field.Modifiers.Any(SyntaxKind.ConstKeyword)
                   where semanticModel.GetTypeInfo(field.Declaration.Type).Type?.SpecialType == SpecialType.System_Int32
                   select field;
        }

        private static bool IsMessageSet(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
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
