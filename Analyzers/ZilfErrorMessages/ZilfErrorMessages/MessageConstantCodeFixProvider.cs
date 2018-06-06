using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ZilfErrorMessages
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessageConstantCodeFixProvider)), Shared]
    public class MessageConstantCodeFixProvider : CodeFixProvider
    {
        const string Title = "Move prefix to call sites";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticIds.PrefixedMessageFormat);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // we can only fix PrefixedMessageFormat
            var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.PrefixedMessageFormat);

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
                    title: Title,
                    createChangedSolution: c => MoveMessagePrefixToCallSitesAsync(context.Document, fieldDecl, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        class PendingReplacement
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

        static async Task<Solution> MoveMessagePrefixToCallSitesAsync(Document document,
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

            FieldDeclarationSyntax FindFieldDecl(SyntaxNode node) =>
                node.DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Single(n => n.HasAnnotation(fieldDeclAnnotation));

            ExpressionSyntax FindFormatExpr(SyntaxNode node) =>
                FindFieldDecl(node)
                    .DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .First(a => a.Name.ToString() == "Message" || a.Name.ToString() == "MessageAttribute")
                    .DescendantNodes()
                    .OfType<AttributeArgumentSyntax>()
                    .First()
                    .Expression;

            // get format string and split it into prefix + rest
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            root = await document.GetSyntaxRootAsync(cancellationToken);
            var formatStr = (string)semanticModel.GetConstantValue(FindFormatExpr(root), cancellationToken).Value;
            var match = MessageConstantAnalyzer.PrefixedMessageFormatRegex.Match(formatStr);
            var prefix = match.Groups["prefix"].Value;
            var rest = match.Groups["rest"].Value;

            var newFormatStr = "{0}" + IncrementFormatTokens(rest);

            // replace format string
            var newFormatExpr = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(newFormatStr));

            var pendingReplacements = ImmutableList.Create(
                new PendingReplacement(document, FindFormatExpr(root), newFormatExpr));

            async Task ApplyPendingReplacementsAsync()
            {
                // ReSharper disable AccessToModifiedClosure
                var replacementsByDocument = pendingReplacements.GroupBy(pr => pr.Document);
                pendingReplacements = pendingReplacements.Clear();

                foreach (var group in replacementsByDocument)
                {
                    var syntaxMapping = group.ToDictionary(pr => pr.Old, pr => pr.New);

                    var syntaxRoot = await group.Key.GetSyntaxRootAsync(cancellationToken);
                    var newSyntaxRoot = syntaxRoot.ReplaceNodes(syntaxMapping.Keys, (node, _) => syntaxMapping[node]);

                    solution = solution.WithDocumentSyntaxRoot(group.Key.Id, newSyntaxRoot);
                }
                // ReSharper restore AccessToModifiedClosure
            }

            var fieldSymbol = semanticModel.GetDeclaredSymbol(
                FindFieldDecl(await document.GetSyntaxRootAsync(cancellationToken)).Declaration.Variables[0],
                cancellationToken);

            if (fieldSymbol != null)
            {
                // rename constant if the name matches the old message format
                if (fieldSymbol.Name == ErrorExceptionUsageCodeFixProvider.GetConstantNameFromMessageFormat(formatStr))
                {
                    await ApplyPendingReplacementsAsync();

                    var newCompilation = await solution.GetDocument(messageDocId).Project.GetCompilationAsync(cancellationToken);
                    fieldSymbol = SymbolFinder.FindSimilarSymbols(fieldSymbol, newCompilation, cancellationToken).First();

                    var newName = ErrorExceptionUsageCodeFixProvider.GetConstantNameFromMessageFormat(newFormatStr);
                    solution = await Renamer.RenameSymbolAsync(solution, fieldSymbol, newName,
                        solution.Workspace.Options, cancellationToken);

                    var newDocument = solution.GetDocument(messageDocId);
                    var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken);
                    var newFieldDecl = FindFieldDecl(newRoot);
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
            await ApplyPendingReplacementsAsync();

            return solution;
        }

        [ItemCanBeNull]
        static async Task<PendingReplacement> ReplacementCallSiteWithPrefixInsertedAsync(
            ReferenceLocation location, LiteralExpressionSyntax prefixSyntax, CancellationToken cancellationToken)
        {
            var root = await location.Document.GetSyntaxRootAsync(cancellationToken);

            if (!(root.FindToken(location.Location.SourceSpan.Start).Parent?.Parent is MemberAccessExpressionSyntax accessExpr))
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

        [NotNull]
        public static string IncrementFormatTokens([NotNull] string format)
        {
            return MessageConstantAnalyzer.FormatTokenRegex.Replace(
                format,
                match =>
                    $"{{{int.Parse(match.Groups["number"].Value) + 1}{match.Groups["suffix"].Value}}}");
        }
    }
}