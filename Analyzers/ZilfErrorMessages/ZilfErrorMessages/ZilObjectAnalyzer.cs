using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZilfErrorMessages
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ZilObjectAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor Rule_ComparingZilObjectsWithEquals = new DiagnosticDescriptor(
            DiagnosticIds.ComparingZilObjectsWithEquals,
            "Comparing ZilObjects with Equals",
            "'{0}' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
            "Values",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        static readonly DiagnosticDescriptor Rule_PartiallyOverriddenZilObjectComparison = new DiagnosticDescriptor(
            DiagnosticIds.PartiallyOverriddenZilObjectComparison,
            "Partially overridden ZilObject comparison",
            "'{0}' overrides {1} but does not override {2}",
            "Values",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                Rule_ComparingZilObjectsWithEquals,
                Rule_PartiallyOverriddenZilObjectComparison);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessishNode, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.ConditionalAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationNode, SyntaxKind.InvocationExpression);
            context.RegisterSymbolAction(AnalyzeClassSymbol, SymbolKind.NamedType);
        }

        static readonly ImmutableArray<(string ifThis, string thenThat)> MethodsToOverrideTogether =
            ImmutableArray.Create(
                ("Equals", "ExactlyEquals"),
                ("ExactlyEquals", "GetHashCode"));

        static void AnalyzeClassSymbol(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            var zilObjectType = context.Compilation
                .GetTypeByMetadataName("Zilf.Interpreter.Values.ZilObject");

            if (!GetTypeAndBases(type).Contains(zilObjectType))
                return;

            foreach (var (ifThis, thenThat) in MethodsToOverrideTogether)
            {
                if (TypeOverridesBaseMethod(type, ifThis, zilObjectType, out var overridingMethod, out var baseEqualsMethod) &&
                    !TypeOverridesBaseMethod(type, thenThat, zilObjectType, out _, out var baseExactlyEqualsMethod))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule_PartiallyOverriddenZilObjectComparison,
                        overridingMethod.Locations.First(),
                        type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                        baseEqualsMethod.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                        baseExactlyEqualsMethod?.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        [ContractAnnotation("=> true, overridingMethod: notnull, overriddenMethod: notnull")]
        [ContractAnnotation("=> false, overridingMethod: null, overriddenMethod: canbenull")]
        static bool TypeOverridesBaseMethod([NotNull] ITypeSymbol derivedType, [NotNull] string methodName,
            [NotNull] ITypeSymbol baseType, out IMethodSymbol overridingMethod, out IMethodSymbol overriddenMethod)
        {
            var derivedMethods = derivedType.GetMembers(methodName).OfType<IMethodSymbol>().Where(m => !m.IsStatic);

            foreach (var dm in derivedMethods)
            {
                foreach (var bm in GetMethodAndOverridden(dm))
                {
                    if (GetTypeAndBases(baseType).Contains(bm.ContainingType))
                    {
                        overridingMethod = dm;
                        overriddenMethod = bm;
                        return true;
                    }
                }
            }

            overridingMethod = null;
            overriddenMethod = GetTypeAndBases(baseType)
                .SelectMany(t => t.GetMembers(methodName))
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => !m.IsStatic);
            return false;
        }

        static void AnalyzeInvocationNode(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!DetectMemberAccess(invocation.Expression, out var objType, out var memberNode, context.SemanticModel))
                return;

            var memberSymInfo = context.SemanticModel.GetSymbolInfo(memberNode);

            if (memberSymInfo.Symbol is IMethodSymbol method && invocation.ArgumentList.Arguments.Count > 0)
            {
                var zilObjectType = context.Compilation
                    .GetTypeByMetadataName("Zilf.Interpreter.Values.ZilObject");

                var argTypes = from a in invocation.ArgumentList.Arguments.Take(2)
                               select context.SemanticModel.GetTypeInfo(a.Expression).Type;

                if (method.MethodKind == MethodKind.ReducedExtension)
                {
                    // convert foo.SequenceEqual(bar) to Enumerable.SequenceEqual(foo, bar)
                    method = method.ReducedFrom;
                    argTypes = new[] { objType }.Concat(argTypes);
                }

                if (IsEqualsBasedAssertion(method))
                {
                    // applies if any argument inherits from ZilObject
                    if (!argTypes.Any(argType => GetTypeAndBases(argType).Contains(zilObjectType)))
                        return;
                }
                else if (IsEqualsBasedCollectionComparison(method))
                {
                    // applies if any argument implements IEnumerable<ZilObject>
                    var enumerableType =
                        context.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
                    var zilObjectEnumerableType = enumerableType.Construct(zilObjectType);

                    if (!argTypes.Any(argType =>
                        context.Compilation.ClassifyConversion(argType, zilObjectEnumerableType).IsImplicit))
                        return;
                }
                else
                {
                    // doesn't apply
                    return;
                }

                var diagnostic = Diagnostic.Create(
                    Rule_ComparingZilObjectsWithEquals,
                    context.Node.GetLocation(),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));

                context.ReportDiagnostic(diagnostic);
            }
        }

        static bool IsEqualsBasedCollectionComparison([NotNull] IMethodSymbol method)
        {
            // collection comparisons:
            //   Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert (AreEqual, AreEquivalent, Contains, and negations)
            //   System.Linq.Enumerable (SequenceEqual`1)

            var containingType = method.ContainingType;

            if (containingType.Name == "CollectionAssert" &&
                containingType.ContainingNamespace.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting")
            {
                switch (method.Name)
                {
                    case "AreEqual":
                    case "AreEquivalent":
                    case "Contains":
                    case "AreNotEqual":
                    case "AreNotEquivalent":
                    case "DoesNotContain":
                        return true;
                }
            }
            else if (containingType.Name == "Enumerable" &&
                     containingType.ContainingNamespace.ToString() == "System.Linq")
            {
                switch (method.Name)
                {
                    case "SequenceEqual":
                        return true;
                }
            }

            return false;
        }

        static bool IsEqualsBasedAssertion([NotNull] IMethodSymbol method)
        {
            var containingType = method.ContainingType;

            if (containingType.Name == "Assert" &&
                containingType.ContainingNamespace.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting")
            {
                switch (method.Name)
                {
                    case "AreEqual":
                    case "AreNotEqual":
                        return true;
                }
            }
            else if (containingType.SpecialType == SpecialType.System_Object)
            {
                if (method.Name == "Equals" && method.IsStatic)
                    return true;
            }

            return false;
        }

        static void AnalyzeMemberAccessishNode(SyntaxNodeAnalysisContext context)
        {
            var zilObjectType = context.Compilation
                .GetTypeByMetadataName("Zilf.Interpreter.Values.ZilObject");

            if (!DetectMemberAccess(context.Node, out var objType, out var memberNode, context.SemanticModel))
                return;

            if (objType == null || !GetTypeAndBases(objType).Contains(zilObjectType))
                return;

            var zilObjectEquals = GetTypeAndBases(zilObjectType)
                .SelectMany(t => t.GetMembers("Equals"))
                .First(s => !s.IsStatic);

            var memberSymInfo = context.SemanticModel.GetSymbolInfo(memberNode);

            if (memberSymInfo.Symbol is IMethodSymbol method &&
                GetMethodAndOverridden(method).Contains(zilObjectEquals))
            {
                var diagnostic = Diagnostic.Create(
                    Rule_ComparingZilObjectsWithEquals,
                    context.Node.GetLocation(),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));

                context.ReportDiagnostic(diagnostic);
            }
        }

        [ContractAnnotation("=> false, memberNode: null; => true, memberNode: notnull")]
        static bool DetectMemberAccess([NotNull] SyntaxNode node, [CanBeNull] out ITypeSymbol objectType, [CanBeNull] out ExpressionSyntax memberNode, SemanticModel model)
        {
            switch (node)
            {
                case MemberAccessExpressionSyntax mem:
                    memberNode = mem;
                    objectType = model.GetTypeInfo(mem.Expression).Type;
                    return true;

                case ConditionalAccessExpressionSyntax cond:
                    if (cond.WhenNotNull is InvocationExpressionSyntax invocation &&
                        invocation.Expression is MemberBindingExpressionSyntax memb)
                    {
                        memberNode = memb;
                        objectType = model.GetTypeInfo(cond.Expression).Type;
                        return true;
                    }
                    break;

                case IdentifierNameSyntax ident:
                    var sym = model.GetSymbolInfo(ident).Symbol;
                    if (sym is IMethodSymbol method)
                    {
                        objectType = method.ContainingType;
                        memberNode = ident;
                        return true;
                    }
                    break;
            }

            memberNode = null;
            objectType = null;
            return false;
        }

        [ItemNotNull]
        static IEnumerable<ITypeSymbol> GetTypeAndBases([NotNull] ITypeSymbol type)
        {
            while (type != null)
            {
                yield return type;

                type = type.BaseType;
            }
        }

        [ItemNotNull]
        static IEnumerable<IMethodSymbol> GetMethodAndOverridden([NotNull] IMethodSymbol method)
        {
            while (method != null)
            {
                yield return method;

                method = method.OverriddenMethod;
            }
        }
    }
}