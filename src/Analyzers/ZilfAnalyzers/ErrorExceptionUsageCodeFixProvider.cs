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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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

namespace ZilfAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ErrorExceptionUsageCodeFixProvider)), Shared]
    public class ErrorExceptionUsageCodeFixProvider : CodeFixProvider
    {
        const string Title = "Convert message to diagnostic constant ({0})";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.ExceptionShouldUseDiagnosticCode];

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return new ErrorExceptionUsageFixAllProvider();
        }

        const string DefaultSeverity = "Error";
        static readonly string[] Severities = ["Error", "Warning", "Info"];

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // find the 'new XError()' expression
            var creationExpr = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            if (creationExpr == null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            if (semanticModel == null)
                return;

            if (ErrorExceptionUsageAnalyzer.TryMatchLiteralCreation(creationExpr, semanticModel, out var literalCreation))
            {
                // Register a code action that will invoke the fix.
                foreach (var sev in Severities)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: string.Format(Title, sev),
                            createChangedSolution: c => ConvertMessagesToConstantsAsync(context.Document, [literalCreation], sev, c),
                            equivalenceKey: string.Format(Title, sev)),
                        diagnostic);
                }
            }
        }

        record Invocation(
            string MessagesTypeName,
            ExpressionSyntax ExpressionToReplace,
            MemberAccessExpressionSyntax ConstantAccessSyntax,
            IEnumerable<ExpressionSyntax> NewMessageArgs,
            Func<int, FieldDeclarationSyntax> GetConstantDeclarationSyntax);

        static Task<Solution> ConvertMessagesToConstantsAsync(Document document, LiteralCreation[] creations, string severity, CancellationToken cancellationToken)
        {
            var invocations = PlanInvocations(creations, severity);
            return ApplyInvocationsAsync(
                document.Project.Solution,
                Enumerable.Repeat(new KeyValuePair<DocumentId, Invocation[]>(document.Id, invocations), 1),
                cancellationToken);
        }

        static Invocation[] PlanInvocations(LiteralCreation[] creations, string severity)
        {
            var invocations = new List<Invocation>();

            foreach (var creation in creations)
            {
                // get the name of the class where the constant will go
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

                invocations.Add(new Invocation(
                    ConstantAccessSyntax: constantAccessSyntax,
                    ExpressionToReplace: creation.ExpressionToReplace,
                    NewMessageArgs: creation.NewMessageArgs,
                    MessagesTypeName: messagesTypeName,
                    GetConstantDeclarationSyntax: code => MakeConstantSyntax(creation.NewMessageFormat, constantName, code, severity)
                ));
            }

            return [.. invocations];
        }

        static async Task<Solution> ApplyInvocationsAsync(
            Solution solution,
            IEnumerable<KeyValuePair<DocumentId, Invocation[]>> invocationsByDocument,
            CancellationToken cancellationToken)
        {
            // apply changes at invocation sites
            var slnEditor = new SolutionEditor(solution);

            var invocationsByDocumentArray = invocationsByDocument as KeyValuePair<DocumentId, Invocation[]>[] ??
                                             [.. invocationsByDocument];

            foreach (var pair in invocationsByDocumentArray)
            {
                var docEditor = await slnEditor.GetDocumentEditorAsync(pair.Key, cancellationToken);

                foreach (var i in pair.Value)
                {
                    docEditor.ReplaceNode(
                        i.ExpressionToReplace,
                        i.ConstantAccessSyntax.WithTriviaFrom(i.ExpressionToReplace));

                    if (i.NewMessageArgs.Any())
                    {
                        ArgumentSyntax? ancestor = i.ExpressionToReplace.FirstAncestorOrSelf<ArgumentSyntax>();
                        Debug.Assert(ancestor != null);
                        docEditor.InsertAfter(
                            ancestor,
                            i.NewMessageArgs.Select(a => SyntaxFactory.Argument(a).WithAdditionalAnnotations(Formatter.Annotation)));
                    }
                }

                AddUsingIfNeeded(docEditor);
            }

            solution = slnEditor.GetChangedSolution();

            // refresh model and find message set types where we need to add constants
            var constantsByTypeName = from pair in invocationsByDocumentArray
                                      from inv in pair.Value
                                      group inv.GetConstantDeclarationSyntax by inv.MessagesTypeName;

            foreach (var group in constantsByTypeName)
            {
                // find type
                bool ok = false;

                foreach (var project in solution.Projects)
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken);

                    if (compilation == null)
                        continue;

                    var messagesTypeSymbol = GetAllTypes(compilation).FirstOrDefault(t => t.Name == group.Key);

                    if (messagesTypeSymbol == null)
                        continue;

                    var messagesDefinition = messagesTypeSymbol.OriginalDefinition;
                    var messagesDefSyntaxRef = messagesDefinition.DeclaringSyntaxReferences.First();
                    var messagesDefDocument = solution.GetDocument(messagesDefSyntaxRef.SyntaxTree);

                    if (messagesDefDocument == null)
                        continue;

                    var messagesDefSyntaxRoot = await messagesDefDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    if (messagesDefSyntaxRoot == null)
                        continue;

                    var messagesDefSyntax = (ClassDeclarationSyntax)await messagesDefSyntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

                    // find next unused message number
                    var nextCode = (from child in messagesDefSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>()
                                    where child.Modifiers.Any(SyntaxKind.ConstKeyword)
                                    from v in child.Declaration.Variables
                                    let initializer = v.Initializer?.Value as LiteralExpressionSyntax
                                    where initializer != null && initializer.Kind() == SyntaxKind.NumericLiteralExpression
                                    select (int)initializer.Token.Value!)
                                   .Concat(Enumerable.Repeat(1, 1))
                                   .Max() + 1;

                    var newConstants = group.Select((getConstant, i) => getConstant(nextCode + i)).Cast<MemberDeclarationSyntax>().ToArray();

                    var newMessagesDefSyntax = messagesDefSyntax.AddMembers(newConstants);
                    var newSyntaxRoot = messagesDefSyntaxRoot.ReplaceNode(messagesDefSyntax, newMessagesDefSyntax);

                    solution = solution.WithDocumentSyntaxRoot(messagesDefDocument.Id, newSyntaxRoot);
                    ok = true;
                    break;
                }

                if (!ok)
                    throw new InvalidOperationException("Can't find message set type " + group.Key);
            }

            return solution;
        }

        static IEnumerable<INamedTypeSymbol> GetAllTypes(Compilation compilation)
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

        static void AddUsingIfNeeded(SyntaxEditor docEditor)
        {
            if (docEditor.OriginalRoot is not CompilationUnitSyntax compilationUnitSyntax)
                return;

            if (!compilationUnitSyntax.Usings.Any(u => u.Name?.ToString() == "Zilf.Diagnostics"))
            {
                docEditor.InsertAfter(
                    compilationUnitSyntax.Usings.Last(),
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.ParseName("Zilf.Diagnostics")));
            }
        }

        static FieldDeclarationSyntax MakeConstantSyntax(string messageFormat, string constantName, int code, string severity)
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
                attributeLists: SyntaxFactory.List(
                [
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            name: SyntaxFactory.ParseName("Message"),
                            argumentList: messageArgumentList)))
                ]),
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
                    sb.Append(capNext ? char.ToUpperInvariant(c) : c);

                    capNext = false;
                }
            }

            if (sb.Length == 0 || char.IsDigit(sb[0]))
                sb.Insert(0, '_');

            return sb.ToString();
        }

        class ErrorExceptionUsageFixAllProvider : FixAllProvider
        {
            /// <exception cref="ArgumentException">Unknown scope</exception>
            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
                const string TitleFormat = "Convert all messages in {0} {1} to diagnostic constants";
                string fixAllTitle;

                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        {
                            Debug.Assert(fixAllContext.Document != null);
                            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            fixAllTitle = string.Format(TitleFormat, "document", fixAllContext.Document.Name);
                            break;
                        }

                    case FixAllScope.Project:
                        {
                            var project = fixAllContext.Project;
                            var diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            fixAllTitle = string.Format(TitleFormat, "project", fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (var project in fixAllContext.Solution.Projects)
                            {
                                var diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
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
                    .FirstOrDefault(sev => string.Format(Title, sev) == fixAllContext.CodeActionEquivalenceKey)
                    ?? DefaultSeverity;

                foreach (var sev in Severities)
                {
                    if (string.Format(Title, sev) == fixAllContext.CodeActionEquivalenceKey)
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

                        async Task<KeyValuePair<DocumentId, Invocation[]>> GetInvocations(Document doc, IEnumerable<Diagnostic> diags)
                        {
                            var root = await doc.GetSyntaxRootAsync(c).ConfigureAwait(false);
                            var semanticModel = await doc.GetSemanticModelAsync(c).ConfigureAwait(false);
                            Debug.Assert(root != null);
                            Debug.Assert(semanticModel != null);
                            var creationExprs = from d in diags
                                                let span = d.Location.SourceSpan
                                                let ancestors = root.FindToken(span.Start).Parent?.AncestorsAndSelf()
                                                select ancestors.OfType<ObjectCreationExpressionSyntax>().First();
                            var literalCreations = MatchLiteralCreations(creationExprs, semanticModel);
                            var invocations = PlanInvocations([.. literalCreations], severity);
                            return new KeyValuePair<DocumentId, Invocation[]>(doc.Id, invocations);
                        }

                        var results = await Task.WhenAll(from grouping in diagsByDocument
                                                         select GetInvocations(grouping.Key, grouping));

                        return await ApplyInvocationsAsync(fixAllContext.Solution, results, c);
                    },
                    equivalenceKey: fixAllTitle);
            }
        }

        static IEnumerable<LiteralCreation> MatchLiteralCreations(
            IEnumerable<ObjectCreationExpressionSyntax> creationExprs, SemanticModel semanticModel)
        {
            foreach (var expr in creationExprs)
            {
                if (ErrorExceptionUsageAnalyzer.TryMatchLiteralCreation(expr, semanticModel, out var literalCreation))
                    yield return literalCreation;
            }
        }
    }
}