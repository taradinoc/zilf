using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.Formatting;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ZilfErrorMessages
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessageConstantCodeFixProvider)), Shared]
    public class MessageConstantCodeFixProvider : CodeFixProvider
    {
        private const string title = "Move prefix to call sites";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MessageConstantAnalyzer.DiagnosticId_PrefixedMessageFormat); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // we can only fix PrefixedMessageFormat
            var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == MessageConstantAnalyzer.DiagnosticId_PrefixedMessageFormat);

            if (diagnostic == null)
                return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var formatExpr = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AttributeArgumentSyntax>().First().Expression;
            var fieldDecl = formatExpr.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            if (fieldDecl.Declaration.Variables.Count != 1)
                return;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => MoveMessagePrefixToCallSitesAsync(context.Document, fieldDecl, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private class PendingReplacement
        {
            public Document Document { get; }
            public SyntaxNode Old { get; }
            public SyntaxNode New { get; }

            public PendingReplacement(Document document, SyntaxNode old, SyntaxNode @new)
            {
                Document = document;
                Old = old;
                New = @new;
            }
        }

        private static async Task<Solution> MoveMessagePrefixToCallSitesAsync(Document document,
            FieldDeclarationSyntax fieldDecl, CancellationToken cancellationToken)
        {
            // TODO: simplify this using SyntaxEditor?

            var messageDocId = document.Id;

            // we'll need to track the field across renames
            var fieldDeclAnnotation = new SyntaxAnnotation();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            document = document.WithSyntaxRoot(
                root.ReplaceNode(
                    fieldDecl,
                    fieldDecl.WithAdditionalAnnotations(fieldDeclAnnotation)));
            var solution = document.Project.Solution;

            Func<SyntaxNode, FieldDeclarationSyntax> findFieldDecl = node =>
                node.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .Single(n => n.HasAnnotation(fieldDeclAnnotation));
            Func<SyntaxNode, ExpressionSyntax> findFormatExpr = node =>
                findFieldDecl(node).DescendantNodes().OfType<AttributeSyntax>()
                    .First(a => a.Name.ToString() == "Message" || a.Name.ToString() == "MessageAttribute")
                    .DescendantNodes().OfType<AttributeArgumentSyntax>()
                    .First()
                    .Expression;

            // get format string and split it into prefix + rest
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            root = await document.GetSyntaxRootAsync(cancellationToken);
                var formatStr = (string)semanticModel.GetConstantValue(findFormatExpr(root), cancellationToken).Value;
            var match = MessageConstantAnalyzer.PrefixedMessageFormatRegex.Match(formatStr);
            var prefix = match.Groups["prefix"].Value;
            var rest = match.Groups["rest"].Value;

            var newFormatStr = "{0}" + IncrementFormatTokens(rest);

            // replace format string
            var newFormatExpr = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(newFormatStr));

            var pendingReplacements = ImmutableList.Create(
                new PendingReplacement(document, findFormatExpr(root), newFormatExpr));

            Func<Task> applyPendingReplacementsAsync = async delegate
            {
                var replacementsByDocument = pendingReplacements.GroupBy(pr => pr.Document);
                pendingReplacements = pendingReplacements.Clear();

                foreach (var group in replacementsByDocument)
                {
                    var syntaxMapping = group.ToDictionary(pr => pr.Old, pr => pr.New);

                    var syntaxRoot = await group.Key.GetSyntaxRootAsync(cancellationToken);
                    var newSyntaxRoot = syntaxRoot.ReplaceNodes(syntaxMapping.Keys, (node, _) => syntaxMapping[node]);

                    solution = solution.WithDocumentSyntaxRoot(group.Key.Id, newSyntaxRoot);
                }
            };

            var fieldSymbol = semanticModel.GetDeclaredSymbol(
                findFieldDecl(await document.GetSyntaxRootAsync(cancellationToken)).Declaration.Variables[0],
                cancellationToken);

            if (fieldSymbol != null)
            {
                Compilation newCompilation;

                // rename constant if the name matches the old message format
                if (fieldSymbol.Name == ErrorExceptionUsageCodeFixProvider.GetConstantNameFromMessageFormat(formatStr))
                {
                    await applyPendingReplacementsAsync();

                    newCompilation = await solution.GetDocument(messageDocId).Project.GetCompilationAsync(cancellationToken);
                    fieldSymbol = SymbolFinder.FindSimilarSymbols(fieldSymbol, newCompilation, cancellationToken).First();

                    var newName = ErrorExceptionUsageCodeFixProvider.GetConstantNameFromMessageFormat(newFormatStr);
                    solution = await Renamer.RenameSymbolAsync(solution, fieldSymbol, newName,
                        solution.Workspace.Options, cancellationToken);

                    var newDocument = solution.GetDocument(messageDocId);
                    var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken);
                    var newFieldDecl = findFieldDecl(newRoot);
                    var newSemanticModel = await newDocument.GetSemanticModelAsync(cancellationToken);
                    fieldSymbol = newSemanticModel.GetDeclaredSymbol(newFieldDecl.Declaration.Variables[0]);
                }

                // update call sites
                var prefixSyntax = SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(prefix));

                var references = await SymbolFinder.FindReferencesAsync(fieldSymbol, solution, cancellationToken);
                var rs = references.SingleOrDefault();

                if (rs != null)
                {
                    foreach (var location in rs.Locations.Where(l => !l.IsCandidateLocation && !l.IsImplicit))
                    {
                        var replacement = await ReplacementCallSiteWithPrefixInsertedAsync(location, prefixSyntax, cancellationToken);
                        if (replacement != null)
                            pendingReplacements = pendingReplacements.Add(replacement);
                    }
                }
            }

            // apply pending replacements
            await applyPendingReplacementsAsync();

            return solution;
        }

        private static async Task<PendingReplacement> ReplacementCallSiteWithPrefixInsertedAsync(
            ReferenceLocation location, LiteralExpressionSyntax prefixSyntax, CancellationToken cancellationToken)
        {
            var root = await location.Document.GetSyntaxRootAsync(cancellationToken);

            var accessExpr = root.FindToken(location.Location.SourceSpan.Start).Parent?.Parent as MemberAccessExpressionSyntax;
            if (accessExpr == null)
                return null;

            var argumentListExpr = accessExpr.FirstAncestorOrSelf<ArgumentListSyntax>();
            if (argumentListExpr == null)
                return null;

            var accessIdx = argumentListExpr.Arguments.IndexOf(a => a.Expression == accessExpr);
            if (accessIdx < 0)
                return null;

            var newArgumentListExpr = argumentListExpr.WithArguments(
                argumentListExpr.Arguments.Insert(
                    accessIdx + 1,
                    SyntaxFactory.Argument(prefixSyntax)));

            return new PendingReplacement(location.Document, argumentListExpr, newArgumentListExpr);
        }

        private static string IncrementFormatTokens(string format)
        {
            return MessageConstantAnalyzer.FormatTokenRegex.Replace(
                format,
                match =>
                    $"{{{int.Parse(match.Groups["number"].Value) + 1}{match.Groups["suffix"].Value}}}");
        }
    }
}