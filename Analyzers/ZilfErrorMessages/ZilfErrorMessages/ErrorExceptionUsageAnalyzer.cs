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
    public class ErrorExceptionUsageAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ZILF0001";

        private const string Title = "Obsolete error format";
        private const string MessageFormat = "This exception should use a diagnostic code instead";
        private const string Category = "Error Reporting";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var creationExpr = (ObjectCreationExpressionSyntax)context.Node;

            string exceptionTypeName;
            ExpressionSyntax locationSyntax;
            LiteralExpressionSyntax literalSyntax;

            if (TryMatchLiteralCreation(creationExpr, context.SemanticModel,
                out exceptionTypeName, out locationSyntax, out literalSyntax))
            {
                var diagnostic = Diagnostic.Create(Rule, creationExpr.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static bool TryMatchLiteralCreation(ObjectCreationExpressionSyntax creationExpr,
            SemanticModel semanticModel, out string exceptionTypeName,
            out ExpressionSyntax locationSyntax, out LiteralExpressionSyntax literalSyntax)
        {
            var typeSymbol = semanticModel.GetTypeInfo(creationExpr);
            var qualifiedFormat = new SymbolDisplayFormat(
              typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            exceptionTypeName = typeSymbol.Type.ToDisplayString(qualifiedFormat);

            if (exceptionTypeName != null && ZilfFacts.MessageTypeMap.ContainsKey(exceptionTypeName))
            {
                var args = creationExpr.ArgumentList;

                if (args.Arguments.Count == 1)
                {
                    literalSyntax = args.Arguments[0].Expression as LiteralExpressionSyntax;

                    if (literalSyntax != null && literalSyntax.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        locationSyntax = null;
                        return true;
                    }
                }

                if (args.Arguments.Count == 2)
                {
                    literalSyntax = args.Arguments[1].Expression as LiteralExpressionSyntax;

                    if (literalSyntax != null && literalSyntax.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        locationSyntax = args.Arguments[1].Expression;
                        return true;
                    }
                }
            }

            locationSyntax = null;
            literalSyntax = null;
            return false;
        }
    }
}
