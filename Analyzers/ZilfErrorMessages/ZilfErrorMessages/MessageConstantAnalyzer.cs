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
        public const string DiagnosticId = "ZILF0002";

        private const string Title = "Duplicate message code";
        private const string MessageFormat = "The code '{0}' is used more than once in message set '{1}'";
        private const string Category = "Error Reporting";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            if (!IsMessageSet(classDecl, context.SemanticModel))
                return;

            var seenConstants = ImmutableHashSet<int>.Empty;

            foreach (var varDecl in GetConstIntDecls(classDecl, context.SemanticModel))
            {
                if (varDecl.Initializer != null)
                {
                    var constValue = context.SemanticModel.GetConstantValue(varDecl.Initializer.Value);

                    if (constValue.HasValue)
                    {
                        var value = (int)constValue.Value;

                        if (seenConstants.Contains(value))
                        {
                            var diagnostic = Diagnostic.Create(Rule, varDecl.GetLocation(), varDecl.Initializer.Value, classDecl.Identifier);
                            context.ReportDiagnostic(diagnostic);
                        }
                        else
                        {
                            seenConstants = seenConstants.Add(value);
                        }
                    }
                }
            }
        }

        private static IEnumerable<VariableDeclaratorSyntax> GetConstIntDecls(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
        {
            return from field in classDecl.Members.OfType<FieldDeclarationSyntax>()
                   where field.Modifiers.Any(SyntaxKind.PublicKeyword) && field.Modifiers.Any(SyntaxKind.ConstKeyword)
                   from varDecl in field.Declaration.Variables
                   where (semanticModel.GetDeclaredSymbol(varDecl) as IFieldSymbol).Type?.SpecialType == SpecialType.System_Int32
                   select varDecl;
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
