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

            LiteralCreation dummy;

            if (TryMatchLiteralCreation(creationExpr, context.SemanticModel, out dummy))
            {
                var diagnostic = Diagnostic.Create(Rule, creationExpr.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static bool TryMatchLiteralCreation(ObjectCreationExpressionSyntax creationExpr,
            SemanticModel semanticModel, out LiteralCreation literalCreation)
        {
            var typeSymbol = semanticModel.GetTypeInfo(creationExpr);
            var qualifiedFormat = new SymbolDisplayFormat(
              typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var exceptionTypeName = typeSymbol.Type.ToDisplayString(qualifiedFormat);
            ExpressionSyntax errorSourceExpr = null;

            literalCreation = null;

            if (exceptionTypeName != null && ZilfFacts.MessageTypeMap.ContainsKey(exceptionTypeName))
            {
                var args = creationExpr.ArgumentList.Arguments;

                if (args.Count >= 1)
                {
                    var argTypes = args.Select(a => semanticModel.GetTypeInfo(a.Expression).ConvertedType).ToArray();
                    int i = 0;

                    // ignore the (string, int, int) constructor
                    if (argTypes.Length == 3 && argTypes[0]?.SpecialType == SpecialType.System_String &&
                        argTypes[1]?.SpecialType == SpecialType.System_Int32 && argTypes[2]?.SpecialType == SpecialType.System_Int32)
                    {
                        return false;
                    }

                    // if the first argument is ISourceLine or IProvideSourceLine, it's the error source expr
                    switch (argTypes[0]?.Name)
                    {
                        case "ISourceLine":
                        case "IProvideSourceLine":
                            errorSourceExpr = args[0].Expression;

                            i++;
                            if (i >= argTypes.Length)
                                return false;

                            break;
                    }

                    // if the next argument is a string...
                    if (argTypes[i]?.SpecialType == SpecialType.System_String)
                    {
                        // we got a winner!
                        var expressionToReplace = args[i].Expression;

                        string newMessageFormat;
                        IEnumerable<ExpressionSyntax> newMessageArgs;

                        if (TryExtractFormatAndArgs(expressionToReplace, semanticModel, out newMessageFormat, out newMessageArgs))
                        {
                            literalCreation = new LiteralCreation(
                                exceptionTypeName: exceptionTypeName,
                                errorSourceExpression: errorSourceExpr,
                                expressionToReplace: expressionToReplace,
                                newMessageFormat: newMessageFormat,
                                newMessageArgs: newMessageArgs);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryExtractFormatAndArgs(
            ExpressionSyntax expressionToReplace, SemanticModel semanticModel,
            out string newMessageFormat, out IEnumerable<ExpressionSyntax> newMessageArgs)
        {
            string format;
            var newArgs = new List<ExpressionSyntax>();

            // has a constant value?
            var constantValue = semanticModel.GetConstantValue(expressionToReplace);

            if (constantValue.HasValue)
            {
                format = (string)constantValue.Value;
            }
            else
            {
                // TODO: handle "literal" + expr1 + "another literal" + expr2...
                newMessageFormat = null;
                newMessageArgs = null;
                return false;
            }

            // TODO: extract function name as an arg

            newMessageFormat = format;
            newMessageArgs = newArgs;
            return true;
        }
    }

    public class LiteralCreation
    {
        /// <summary>
        /// The name of the exception being created, e.g. InterpreterError.
        /// </summary>
        public string ExceptionTypeName { get; }
        /// <summary>
        /// An expression of type ISourceLine or IProvideSourceLine, or null.
        /// </summary>
        public ExpressionSyntax ErrorSourceExpression { get; }
        /// <summary>
        /// The expression to replace in the constructor arguments. Usually a <see cref="LiteralExpressionSyntax"/>.
        /// </summary>
        public ExpressionSyntax ExpressionToReplace { get; }
        /// <summary>
        /// The format string to be used for the new message.
        /// </summary>
        public string NewMessageFormat { get; }
        /// <summary>
        /// The initial arguments to be used at the call site when formatting the new message.
        /// </summary>
        public IEnumerable<ExpressionSyntax> NewMessageArgs { get; }

        public LiteralCreation(
            string exceptionTypeName, ExpressionSyntax errorSourceExpression,
            ExpressionSyntax expressionToReplace,
            string newMessageFormat, IEnumerable<ExpressionSyntax> newMessageArgs)
        {
            ExceptionTypeName = exceptionTypeName;
            ErrorSourceExpression = errorSourceExpression;
            ExpressionToReplace = expressionToReplace;
            NewMessageFormat = newMessageFormat;
            NewMessageArgs = newMessageArgs;
        }
    }
}
