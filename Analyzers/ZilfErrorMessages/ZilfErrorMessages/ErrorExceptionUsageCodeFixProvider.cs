using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace ZilfErrorMessages
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ErrorExceptionUsageCodeFixProvider)), Shared]
    public class ErrorExceptionUsageCodeFixProvider : CodeFixProvider
    {
        private const string title = "Convert message to diagnostic constant ({0})";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ErrorExceptionUsageAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return new ErrorExceptionUsageFixAllProvider();
        }

        private const string DefaultSeverity = "Error";
        private static readonly string[] Severities = { "Error", "Warning", "Info" };

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // find the 'new XError()' expression
            var creationExpr = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            LiteralCreation literalCreation;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            if (ErrorExceptionUsageAnalyzer.TryMatchLiteralCreation(creationExpr, semanticModel, out literalCreation))
            {
                // Register a code action that will invoke the fix.
                foreach (var sev in Severities)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: string.Format(title, sev),
                            createChangedSolution: c => ConvertMessagesToConstantsAsync(context.Document, new[] { literalCreation }, sev, c),
                            equivalenceKey: string.Format(title, sev)),
                        diagnostic);
                }
            }
        }

        private class Invocation
        {
            public string MessagesTypeName;
            public ExpressionSyntax ExpressionToReplace;
            public MemberAccessExpressionSyntax ConstantAccessSyntax;
            public IEnumerable<ExpressionSyntax> NewMessageArgs;
            public Func<int, FieldDeclarationSyntax> GetConstantDeclarationSyntax;
        }

        private async Task<Solution> ConvertMessagesToConstantsAsync(Document document, LiteralCreation[] creations, string severity, CancellationToken cancellationToken)
        {
            var invocations = await GetInvocationsAsync(document, creations, severity, cancellationToken);
            return await ApplyInvocationsAsync(
                document.Project.Solution,
                Enumerable.Repeat(new KeyValuePair<DocumentId, Invocation[]>(document.Id, invocations), 1),
                cancellationToken);
        }

        private static async Task<Invocation[]> GetInvocationsAsync(Document document, LiteralCreation[] creations, string severity, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var invocations = new List<Invocation>();

            foreach (var creation in creations)
            {
                // get the name of the class where the constant will go
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var qualifiedFormat = new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                var messagesTypeAnnotation = new SyntaxAnnotation("MessagesTypeTracker");

                var messagesTypeName = ZilfFacts.MessageTypeMap[creation.ExceptionTypeName];
                var messagesTypeSyntax = SyntaxFactory.ParseTypeName(messagesTypeName)
                    .WithAdditionalAnnotations(messagesTypeAnnotation);

                // compute the name of the new constant
                var constantName = GetConstantNameFromMessageFormat(creation.NewMessageFormat);
                var constantNameSyntax = SyntaxFactory.IdentifierName(constantName);

                // replace the invocation
                var constantAccessSyntax = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    messagesTypeSyntax,
                    constantNameSyntax);

                invocations.Add(new Invocation
                {
                    ConstantAccessSyntax = constantAccessSyntax,
                    ExpressionToReplace = creation.ExpressionToReplace,
                    NewMessageArgs = creation.NewMessageArgs,
                    MessagesTypeName = messagesTypeName,
                    GetConstantDeclarationSyntax = code => MakeConstantSyntax(creation.NewMessageFormat, constantName, code, severity)
                });
            }

            return invocations.ToArray();
        }

        private static async Task<Solution> ApplyInvocationsAsync(
            Solution solution,
            IEnumerable<KeyValuePair<DocumentId, Invocation[]>> invocationsByDocument,
            CancellationToken cancellationToken)
        {
            // apply changes at invocation sites
            var slnEditor = new SolutionEditor(solution);

            foreach (var pair in invocationsByDocument)
            {
                var docEditor = await slnEditor.GetDocumentEditorAsync(pair.Key, cancellationToken);
                var invocations = pair.Value;

                foreach (var i in pair.Value)
                {
                    docEditor.ReplaceNode(
                        i.ExpressionToReplace,
                        i.ConstantAccessSyntax.WithTriviaFrom(i.ExpressionToReplace));

                    if (i.NewMessageArgs.Any())
                        docEditor.InsertAfter(
                            i.ExpressionToReplace.FirstAncestorOrSelf<ArgumentSyntax>(),
                            i.NewMessageArgs.Select(a => SyntaxFactory.Argument(a).WithAdditionalAnnotations(Formatter.Annotation)));
                }

                AddUsingIfNeeded(docEditor);
            }

            solution = slnEditor.GetChangedSolution();

            // refresh model and find message set types where we need to add constants
            var constantsByTypeName = from pair in invocationsByDocument
                                      from inv in pair.Value
                                      group inv.GetConstantDeclarationSyntax by inv.MessagesTypeName;

            foreach (var group in constantsByTypeName)
            {
                // find type
                bool ok = false;

                foreach (var project in solution.Projects)
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken);

                    var messagesTypeSymbol = GetAllTypes(compilation).FirstOrDefault(t => t.Name == group.Key);

                    if (messagesTypeSymbol != null)
                    {
                        var messagesDefinition = messagesTypeSymbol.OriginalDefinition;
                        var messagesDefSyntaxRef = messagesDefinition.DeclaringSyntaxReferences.First();
                        var messagesDefDocument = solution.GetDocument(messagesDefSyntaxRef.SyntaxTree);

                        var messagesDefSyntaxRoot = await messagesDefDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        var messagesDefSyntax = (ClassDeclarationSyntax)await messagesDefSyntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

                        // find next unused message number
                        var nextCode = (from child in messagesDefSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>()
                                        where child.Modifiers.Any(SyntaxKind.ConstKeyword)
                                        from v in child.Declaration.Variables
                                        let initializer = v.Initializer.Value as LiteralExpressionSyntax
                                        where initializer != null && initializer.Kind() == SyntaxKind.NumericLiteralExpression
                                        select (int)initializer.Token.Value)
                                       .Concat(Enumerable.Repeat(1, 1))
                                       .Max() + 1;

                        var newConstants = group.Select((getConstant, i) => getConstant(nextCode + i)).ToArray();

                        var newMessagesDefSyntax = messagesDefSyntax.AddMembers(newConstants);
                        var newSyntaxRoot = messagesDefSyntaxRoot.ReplaceNode(messagesDefSyntax, newMessagesDefSyntax);

                        solution = solution.WithDocumentSyntaxRoot(messagesDefDocument.Id, newSyntaxRoot);
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                    throw new InvalidOperationException("Can't find message set type " + group.Key);
            }

            return solution;
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(Compilation compilation)
        {
            var nsQueue = new Queue<INamespaceSymbol>();
            nsQueue.Enqueue(compilation.GlobalNamespace);

            while (nsQueue.Count > 0)
            {
                var ns = nsQueue.Dequeue();

                foreach (var t in ns.GetTypeMembers())
                {
                    yield return t;
                }

                foreach (var cns in ns.GetNamespaceMembers())
                {
                    nsQueue.Enqueue(cns);
                }
            }
        }

        private static void AddUsingIfNeeded(DocumentEditor docEditor)
        {
            var compilationUnitSyntax = docEditor.OriginalRoot as CompilationUnitSyntax;

            if (compilationUnitSyntax != null)
            {
                if (!compilationUnitSyntax.Usings.Any(
                    u => u.Name.ToString() == "Zilf.Diagnostics"))
                {
                    docEditor.InsertAfter(
                        compilationUnitSyntax.Usings.Last(),
                        SyntaxFactory.UsingDirective(
                            SyntaxFactory.ParseName("Zilf.Diagnostics")));
                }
            }
        }

        private static FieldDeclarationSyntax MakeConstantSyntax(string messageFormat, string constantName, int code, string severity)
        {
            var messageFormatArgument =
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(messageFormat)));

            var sepSyntaxList = SyntaxFactory.SingletonSeparatedList(messageFormatArgument);

            if (severity != DefaultSeverity)
            {
                var severityArgument =
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.NameEquals("Severity"),
                        null,
                        SyntaxFactory.ParseExpression("Severity." + severity));

                sepSyntaxList = sepSyntaxList.Add(severityArgument);
            }

            var messageArgumentList = SyntaxFactory.AttributeArgumentList(sepSyntaxList);

            return SyntaxFactory.FieldDeclaration(
                attributeLists: SyntaxFactory.List(new[]
                {
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            name: SyntaxFactory.ParseName("Message"),
                            argumentList: messageArgumentList)))
                }),
                modifiers: SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.ConstKeyword)),
                declaration: SyntaxFactory.VariableDeclaration(
                    type: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                    variables: SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            identifier: SyntaxFactory.Identifier(constantName),
                            argumentList: null,
                            initializer: SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(code)))))),
                semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        public static string GetConstantNameFromMessageFormat(string formatString)
        {
            var sb = new StringBuilder();
            bool capNext = true;

            foreach (char c in formatString)
            {
                if (char.IsWhiteSpace(c))
                {
                    sb.Append('_');
                    capNext = true;
                }
                else if (char.IsLetterOrDigit(c))
                {
                    if (capNext)
                        sb.Append(char.ToUpperInvariant(c));
                    else
                        sb.Append(c);

                    capNext = false;
                }
            }

            if (sb.Length == 0 || char.IsDigit(sb[0]))
                sb.Insert(0, '_');

            return sb.ToString();
        }

        private class ErrorExceptionUsageFixAllProvider : FixAllProvider
        {
            public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
                string titleFormat = "Convert all messages in {0} {1} to diagnostic constants";
                string fixAllTitle = null;

                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        {
                            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            fixAllTitle = string.Format(titleFormat, "document", fixAllContext.Document.Name);
                            break;
                        }

                    case FixAllScope.Project:
                        {
                            var project = fixAllContext.Project;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            fixAllTitle = string.Format(titleFormat, "project", fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (var project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
                            }

                            fixAllTitle = "Add all items in the solution to the public API";
                            break;
                        }

                    case FixAllScope.Custom:
                        return null;

                    default:
                        throw new ArgumentException("Unknown scope", nameof(fixAllContext));
                }

                var severity = Severities
                    .FirstOrDefault(sev => string.Format(title, sev) == fixAllContext.CodeActionEquivalenceKey)
                    ?? DefaultSeverity;

                foreach (var sev in Severities)
                {
                    if (string.Format(title, sev) == fixAllContext.CodeActionEquivalenceKey)
                    {
                        severity = sev;
                        break;
                    }
                }

                return CodeAction.Create(
                    title: fixAllTitle,
                    createChangedSolution: async c =>
                    {
                        var diagsByDocument = from ds in diagnosticsToFix
                                              from d in ds.Value
                                              where d.Location.IsInSource
                                              let document = fixAllContext.Solution.GetDocument(d.Location.SourceTree)
                                              group d by document;

                        Func<Document, IEnumerable<Diagnostic>, Task<KeyValuePair<DocumentId, Invocation[]>>> getInvocations =
                            async (doc, diags) =>
                            {
                                var root = await doc.GetSyntaxRootAsync(c).ConfigureAwait(false);
                                var semanticModel = await doc.GetSemanticModelAsync(c).ConfigureAwait(false);
                                var creationExprs = from d in diags
                                                    let span = d.Location.SourceSpan
                                                    let ancestors = root.FindToken(span.Start).Parent.AncestorsAndSelf()
                                                    select ancestors.OfType<ObjectCreationExpressionSyntax>().First();
                                var literalCreations = MatchLiteralCreations(creationExprs, semanticModel);
                                var invocations = await GetInvocationsAsync(doc, literalCreations.ToArray(), severity, c);
                                return new KeyValuePair<DocumentId, Invocation[]>(doc.Id, invocations);
                            };

                        var results = await Task.WhenAll(from grouping in diagsByDocument
                                                         select getInvocations(grouping.Key, grouping));

                        return await ApplyInvocationsAsync(fixAllContext.Solution, results, c);
                    },
                    equivalenceKey: fixAllTitle);
            }
        }

        private static IEnumerable<LiteralCreation> MatchLiteralCreations(
            IEnumerable<ObjectCreationExpressionSyntax> creationExprs, SemanticModel semanticModel)
        {
            foreach (var expr in creationExprs)
            {
                LiteralCreation literalCreation;

                if (ErrorExceptionUsageAnalyzer.TryMatchLiteralCreation(expr, semanticModel, out literalCreation))
                    yield return literalCreation;
            }
        }
    }
}