/* Copyright 2010-2024 Tara McGrew
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZilfSourceGenerators
{
    [Generator]
    public class ZBuiltinWrapperGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Records information about a method that should be exposed to user code
        /// as a builtin Z-code operation under one or more names.
        /// </summary>
        /// <param name="CallType">The type of call the method should be exposed as.</param>
        /// <param name="ClassName">The name of the class where the method is defined.</param>
        /// <param name="MethodName">The name of the method.</param>
        /// <param name="Params">An array of <see cref="Param"/>
        /// describing the method's parameters and their attributes.</param>
        /// <param name="ReturnValue">A <see cref="ReturnValue"/> describing the method's
        /// return value and its attributes.</param>
        /// <param name="Targets">An array of <see cref="Exposure"/> detailing
        /// how the method should be exposed.</param>
        record BuiltinMethodInfo(
            CallType CallType,
            string ClassName,
            string MethodName,
            EquatableArray<Param> Params,
            ReturnValue ReturnValue,
            EquatableArray<Exposure> Targets)
        {
            public bool HasData => Params.Length > 0 && Params[0].Flags.HasFlag(ParamFlags.IsData);

            public int MinArgs => Params.Sum(p => p.MinLength);

            public int? MaxArgs => Params.Sum(p => p.MaxLength);
        }

        enum CallType
        {
            ValueCall,
            VoidCall,
            PredCall,
            ValuePredCall
        }

        [Flags]
        enum ParamFlags
        {
            None = 0,
            IsData = 1,
            IsOptional = 2,
            IsVarArgs = 4,
            IsTable = 8,                    // TableAttribute
            IsObject = 16,                  // ObjectAttribute
            IsVariable = 32,                // VariableAttribute
            IsVariableQuirkLocal = 64,      // VariableScopeQuirks.Local
            IsVariableQuirkGlobal = 128,    // VariableScopeQuirks.Global
        }

        record Param(string Name, string FormalType, ParamFlags Flags, string? DefaultValue)
        {
            public int MinLength => Flags.HasFlag(ParamFlags.IsOptional | ParamFlags.IsVarArgs) ? 0 : 1;

            public int? MaxLength => Flags.HasFlag(ParamFlags.IsVarArgs) ? null : 1;

            public string LocalType => FormalType switch
            {
                "IOperand" => "IOperand?",
                "IVariable" => "IVariable?",
                "ZilObject" => "ZilObject?",
                _ => FormalType,
            };
        }

        [Flags]
        enum ReturnFlags
        {
            None = 0,
            IsTable = 1,                    // TableAttribute
            IsObject = 2,                   // ObjectAttribute
        }

        record ReturnValue(string FormalType, ReturnFlags Flags);

        /// <summary>
        /// Records information about a particular exposure of a method as a Z-code builtin.
        /// </summary>
        record Exposure(string Name, int MinVersion, int MaxVersion, bool HasSideEffect, int Priority, object? Data);

        /// <summary>
        /// Records information about a generated method.
        /// </summary>
        /// <param name="Namespace">The namespace where the method will be emitted.</param>
        /// <param name="ClassName">The class where the method will be emitted.</param>
        /// <param name="MethodName">The name of the method.</param>
        /// <param name="Lines">The method body.</param>
        record GeneratedMethod(string Namespace, string ClassName, string MethodName, EquatableArray<string> Lines);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // extract BuiltinMethodInfo from method declarations with a BuiltinAttribute
            IncrementalValuesProvider<BuiltinMethodInfo> builtinInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Zilf.Compiler.Builtins.BuiltinAttribute",
                predicate: IsBuiltinMethodSyntax,
                transform: ExtractBuiltinMethodInfo)
                .Where(static b => b is not null)!;

            // generate unique names for decoder methods
            var decoderNames = builtinInfos.Collect()
                .Select((builtinInfos, _) =>
                {
                    var distinguisher = new Distinguisher<string, BuiltinMethodInfo>();
                    foreach (var bi in builtinInfos)
                    {
                        var name = bi.MethodName;
                        var props = bi.Params.Select(p => p.FormalType).ToArray();
                        distinguisher.Add([name, .. props], bi);
                    }
                    return distinguisher.GetUniqueNames().ToDictionary(static p => p.item, static p => p.name);
                });

            // generate one argument decoder method for each BuiltinMethodInfo
            var argDecoders = builtinInfos.Combine(decoderNames)
                .Select((pair, ct) =>
                {
                    var (bi, names) = pair;
                    return EmitArgDecoder(bi, names[bi], ct);
                });

            // generate one dispatch method for each distinct exposed name
            var dispatchers = builtinInfos
                .SelectMany((bi, _) => bi.Targets.Select(t => (target: t, method: bi.MethodName, callType: bi.CallType, parameters: bi.Params)))
                .Collect()
                .SelectMany((rows, _) => from row in rows
                                         group row by (name: row.target.Name, row.callType) into g
                                         select (
                                            g.Key.name,
                                            g.Key.callType,
                                            targets: g.OrderBy(static r => r.target.Priority)
                                                .ThenBy(static r => r.method)
                                                .ThenByDescending(static r => r.target.MaxVersion)
                                                .Select(static r => (r.target, r.method, r.parameters))
                                                .ToEquatableArray()
                                         ))
                .Select((r, ct) => EmitDispatchMethod(r.name, r.callType, r.targets, ct));

            // produce a source file containing the argument decoders, the dispatchers, and
            // a static method to register all the dispatchers and some metadata about each one
            context.RegisterSourceOutput(
                argDecoders.Collect().Combine(dispatchers.Collect()),
                (spc, inputs) =>
                {
                    var (argDecoders, dispatchers) = inputs;
                    var methodsByNamespace = argDecoders.Concat(dispatchers).GroupBy(static m => m.Namespace);
                    var sb = new StringBuilder();

                    sb.AppendLine("#if ZILF_BUILTIN_WRAPPERS"); //XXX

                    sb.AppendLine("#nullable enable")
                        .AppendLine("#pragma warning disable CS0168 // Variable is declared but never used")
                        .AppendLine()
                        .AppendLine("using System;")
                        .AppendLine("using Zilf.Emit;")
                        .AppendLine("using Zilf.Interpreter.Values;")
                        .AppendLine("using Zilf.Language;")
                        .AppendLine();

                    bool firstNamespace = true;

                    foreach (var namespaceMethods in methodsByNamespace)
                    {
                        if (!firstNamespace)
                        {
                            sb.AppendLine();
                        }
                        firstNamespace = false;

                        sb.Append("namespace ")
                            .AppendLine(namespaceMethods.Key)
                            .AppendLine("{");

                        var methodsByClass = namespaceMethods.GroupBy(static m => m.ClassName);

                        bool firstClass = true;

                        foreach (var classMethods in methodsByClass)
                        {
                            if (!firstClass)
                            {
                                sb.AppendLine();
                            }
                            firstClass = false;

                            sb.Append("    static partial class ")
                                .AppendLine(classMethods.Key)
                                .AppendLine("    {");

                            bool firstMethod = true;

                            foreach (var method in classMethods)
                            {
                                if (!firstMethod)
                                {
                                    sb.AppendLine();
                                }
                                firstMethod = false;

                                foreach (var line in method.Lines)
                                {
                                    sb.Append("        ")
                                        .AppendLine(line);
                                }
                            }

                            sb.AppendLine("    }");
                        }

                        sb.AppendLine("}");
                    }

                    sb.AppendLine("#endif"); //XXX

                    var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
                    spc.AddSource("ZBuiltins.g.cs", sourceText);
            });
        }

        private GeneratedMethod EmitArgDecoder(BuiltinMethodInfo info, string uniqueName, CancellationToken token)
        {
            var callType = info.CallType;
            var decoderMethodName = $"Decode_{uniqueName}";
            var lines = new IndentedWriter();

            var returnType = info.ReturnValue.FormalType;
            var dataParam = info.HasData ? $"{info.Params[0].FormalType} {info.Params[0].Name}, " : "";

            using (lines.Block($"private static {returnType} {decoderMethodName}({callType} c, {dataParam}Span<ZilObject> argsSpan)"))
            {
                lines.WriteLine($"// Decode parameters for {info.ClassName}.{info.MethodName}");

                // argument count has already been validated by the dispatch method

                // pass 1: declare local variables for each parameter
                foreach (var p in info.Params)
                {
                    if (p.Flags.HasFlag(ParamFlags.IsData))
                    {
                        continue;
                    }

                    lines.WriteLine($"{p.LocalType} {p.Name};");
                }

                // validate, convert, and assign each parameter
                int indexIntoSpan = 0;

                // some arguments "need evaluation", i.e., we need to generate code for them.
                // that code needs to preserve the apparent left-to-right order of evaluation,
                // but as an optimization, we may evaluate them out of order when they don't have
                // side effects (see compile-time logic in Compilation.CompileOperands).

                // here, at generation time, we just need to keep track of which arguments need evaluation,
                // call CompileOperands with them (using a collection expression), and then assign
                // the results to the corresponding local variables.

                // but... in some cases, we can't even know at generation time whether the argument in
                // a given position will need evaluation, because that depends on the expression it's bound to!
                // specifically, for a parameter that:
                // - is of type IOperand,
                // - is marked with [Variable], and
                // - has VariableScopeQuirks other than None
                // the argument needs evaluation if and only if the expression is *not* a variable
                // reference that matches the quirks. in that case, the generated code calls
                // ParameterTypeHandler.GetVariable, then either assigns directly to the local variable
                // for that parameter (if GetVariable found a variable reference) or adds the result to
                // the parameters to be passed to CompileOperands.

                // so, during pass 2 below, we build up a list of arguments that may need evaluation,
                // then generate the code to call CompileOperands with them, then generate the assignments.
                // for arguments that definitely need evaluation, no code is generated in pass 2, and
                // the tuple in needsEval looks like: ("argsSpan[0]", 0, "firstParam", false).
                // for arguments that may or may not need evaluation, we generate code in pass 2 to call
                // GetVariable, and based on its result, either assign directly to the local variable or
                // store the expression in a span to be passed to CompileOperands; the tuple in needsEval
                // looks like: (".. temp_span_secondParam", 1, "secondParam", true).
                // a negative outputIndex indicates that the argument is a varargs parameter, starting at
                // ~outputIndex and continuing to the end of the evaluation results.
                List<(string inputExpression, int outputIndex, string destination, bool conditional)>? needsEval = null;

                // pass 2: generate code for arguments that don't need evaluation, and collect info for arguments that do
                foreach (var p in info.Params)
                {
                    token.ThrowIfCancellationRequested();

                    // the data parameter, if present, isn't part of the argsSpan
                    if (p.Flags.HasFlag(ParamFlags.IsData))
                    {
                        continue;
                    }

                    // the parameter may consume any number of arguments from the argsSpan:
                    // - if it's a varargs parameter, it consumes all remaining arguments
                    // - if it's an optional parameter, it consumes 1 argument if there are any left, or else 0
                    // - otherwise, it consumes exactly 1 argument
                    // this is independent of the parameter's type.

                    bool isOptional = p.Flags.HasFlag(ParamFlags.IsOptional);
                    bool isVarArgs = p.Flags.HasFlag(ParamFlags.IsVarArgs);

                    lines.WriteLine();

                    if (MayNeedEvaluation(p))
                    {
                        needsEval ??= [];

                        if (isVarArgs)
                        {
                            System.Diagnostics.Debug.Assert(p.FormalType is "IOperand[]" or "IOperand?[]");
                            lines.WriteLine($"// Varargs parameter {p.Name} will be evaluated");
                            needsEval.Add(($".. argsSpan.Slice({indexIntoSpan})", ~needsEval.Count, p.Name, false));
                        }
                        else if (isOptional)
                        {
                            System.Diagnostics.Debug.Assert(!MayOrMayNotNeedEvaluation(p));
                            lines.WriteLine($"// Stage optional parameter {p.Name} for evaluation, if present")
                                .WriteLine($"{p.Name} = {p.DefaultValue};")
                                .WriteLine($"Span<ZilObject> temp_span_{p.Name} = argsSpan.Length > {indexIntoSpan} ? argsSpan.Slice({indexIntoSpan}, 1) : [];");
                            needsEval.Add(($".. temp_span_{p.Name}", needsEval.Count, p.Name, true));
                        }
                        else if (MayOrMayNotNeedEvaluation(p))
                        {
                            System.Diagnostics.Debug.Assert(p.FormalType is "IOperand" or "IOperand?" && p.Flags.HasFlag(ParamFlags.IsVariable));

                            var quirks = (p.Flags & (ParamFlags.IsVariableQuirkLocal | ParamFlags.IsVariableQuirkGlobal)) switch
                            {
                                ParamFlags.IsVariableQuirkLocal | ParamFlags.IsVariableQuirkGlobal => "VariableScopeQuirks.Local | VariableScopeQuirks.Global",
                                ParamFlags.IsVariableQuirkLocal => "VariableScopeQuirks.Local",
                                ParamFlags.IsVariableQuirkGlobal => "VariableScopeQuirks.Global",
                                _ => "VariableScopeQuirks.None",
                            };

                            lines.WriteLine($"// Stage variable parameter {p.Name} for evaluation, if necessary")
                                .WriteLine($"VariableRef? temp_var_{p.Name} = ParameterTypeHandler.GetVariable(c.cc, argsSpan[{indexIntoSpan}], {quirks});");

                            using (lines.Block($"if (temp_var_{p.Name} is null && argsSpan[{indexIntoSpan}] is ZilAtom)"))
                            {
                                lines.WriteLine($"throw new ArgumentException(\"bare atom argument must be a variable name\");");
                            }

                            using (lines.Block($"else if (temp_var_{p.Name}?.IsHard == false)"))
                            {
                                lines.WriteLine($"throw new ArgumentException(\"soft variable may not be used here\");");
                            }
                            lines.WriteLine($"{p.Name} = temp_var_{p.Name}?.Hard!.Indirect;")
                                .WriteLine($"Span<ZilObject> temp_span_{p.Name} = temp_var_{p.Name} is not null ? argsSpan.Slice({indexIntoSpan}, 1) : [];");
                            needsEval.Add(($".. temp_span_{p.Name}", needsEval.Count, p.Name, true));
                        }
                        else
                        {
                            lines.WriteLine($"// Operand parameter {p.Name} will be evaluated");
                            needsEval.Add(($"argsSpan[{indexIntoSpan}]", needsEval.Count, p.Name, false));
                        }
                    }
                    else
                    {
                        if (isVarArgs)
                        {
                            System.Diagnostics.Debug.Assert(p.FormalType == "ZilObject[]");
                            lines.WriteLine($"// Convert varargs parameter {p.Name}")
                                .WriteLine($"{p.Name} = argsSpan.Slice({indexIntoSpan}).ToArray();");
                        }
                        else if (isOptional)
                        {
                            // if there are no more arguments in the argsSpan, assign the default value.
                            // otherwise, validate and convert the argument.

                            lines.WriteLine($"// Convert optional parameter {p.Name}, if present");

                            using (lines.Block($"if (argsSpan.Length > {indexIntoSpan})"))
                            {
                                lines.WriteLine($"{p.Name} = {p.DefaultValue};");
                            }

                            using (lines.Block("else"))
                            {
                                EmitConvertArg($"argsSpan[{indexIntoSpan}]", p.Name, p.FormalType, p.Flags);
                            }
                        }
                        else
                        {
                            lines.WriteLine($"// Convert parameter {p.Name}");
                            EmitConvertArg($"argsSpan[{indexIntoSpan}]", p.Name, p.FormalType, p.Flags);
                        }
                    }

                    indexIntoSpan++;
                }

                // pass 3: call CompileOperands and assign the results to the corresponding local variables
                if (needsEval is not null)
                {
                    lines.WriteLine()
                        .WriteLine("// Evaluate operands")
                        .WriteLine($"using Operands temp_operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, [{string.Join(", ", needsEval.Select(x => x.inputExpression))}]);");
                    foreach (var (_, outputIndex, destination, conditional) in needsEval)
                    {
                        if (outputIndex < 0)
                        {
                            System.Diagnostics.Debug.Assert(conditional == false);
                            lines.WriteLine($"{destination} = temp_operands.ToArray({~outputIndex});");
                        }
                        else
                        {
                            lines.WriteLine($"{destination} {(conditional ? "??=" : "=")} temp_operands[{outputIndex}];");
                        }
                    }
                }

                // call the method with the parameters
                lines.WriteLine()
                    .WriteLine("// Call the implementation")
                    .WriteLine(
                        $"{(returnType == "void" ? "" : "return ")}{info.ClassName}.{info.MethodName}" +
                        $"(c{(info.Params.Length == 0 ? "" : ", ")}{string.Join(", ", info.Params.Select(p => p.Name))});");
            }

            return new GeneratedMethod("Zilf.Compiler.Builtins.Generated", "ZBuiltinDecoders", decoderMethodName, lines.GetLines());

            bool MayOrMayNotNeedEvaluation(Param p)
            {
                return p.FormalType is "IOperand" or "IOperand?" &&
                    p.Flags.HasFlag(ParamFlags.IsVariable) &&
                    (p.Flags & (ParamFlags.IsVariableQuirkLocal | ParamFlags.IsVariableQuirkGlobal)) != 0;
            }
            
            bool MayNeedEvaluation(Param p)
            {
                return p.FormalType is "IOperand" or "IOperand?" or "IOperand[]" or "IOperand?[]";
            }

            void EmitConvertArg(string src, string dest, string formalType, ParamFlags flags)
            {
                switch (formalType)
                {
                    case "int":
                        using (lines.Block($"if ({src}.StdTypeAtom != StdAtom.FIX)"))
                        {
                            lines.WriteLine("throw new ArgumentException(\"argument must be a FIX\");");
                        }
                        lines.WriteLine($"{dest} = ((ZilFix){src}).Value;");
                        break;

                    case "string":
                        using (lines.Block($"if ({src} is not ZilString temp_str_{dest})"))
                        {
                            lines.WriteLine("throw new ArgumentException(\"argument must be a literal string\");");
                        }
                        lines.WriteLine($"{dest} = Compilation.TranslateString(temp_str_{dest}, c.cc.Context);");
                        break;

                    case "ZilObject":
                    case "ZilObject?":
                        lines.WriteLine($"{dest} = {src};");
                        break;

                    case "ZilAtom":
                    case "ZilAtom?":
                        using (lines.Block($"if ({src}.StdTypeAtom != StdAtom.ATOM)"))
                        {
                            lines.WriteLine("throw new ArgumentException(\"argument must be an atom\");");
                        }
                        lines.WriteLine($"{dest} = (ZilAtom){src};");
                        break;

                    case "Block":
                    case "Block?":
                        // arg must be an LVAL reference
                        using (lines.Block($"if ({src}.IsLVAL(out var temp_atom_{dest}))"))
                        {
                            using (lines.Block($"if (c.cc.Blocks.FirstOrDefault(b => b.Name == temp_atom_{dest}) is Block temp_block_{dest})"))
                            {
                                lines.WriteLine($"{dest} = temp_block_{dest};");
                            }

                            using (lines.Block("else"))
                            {
                                lines.WriteLine("throw new ArgumentException(\"argument must be bound to a block\");");
                            }
                        }
                        using (lines.Block("else"))
                        {
                            lines.WriteLine("throw new ArgumentException(\"argument must be a local variable reference\");");
                        }
                        break;

                    default:
                        //XXX
                        lines.WriteLine($"throw new NotImplementedException(\"unimplemented conversion from {formalType}\");");
                        break;
                }
            }
        }

        private GeneratedMethod EmitDispatchMethod(
            string name,
            CallType callType,
            EquatableArray<(Exposure exposure, string method, EquatableArray<Param> parameters)> targets,
            CancellationToken token)
        {
            var dispatchMethodName = $"Dispatch_{SanitizeName(name)}";
            var lines = new IndentedWriter();

            var returnType = callType == CallType.ValueCall ? "IOperand" : "void";

            using (lines.Block($"private static {returnType} {dispatchMethodName}({callType} c, Span<ZilObject> args)"))
            {
                //XXX
                lines.WriteLine($"// Dispatch {name}");

                foreach (var (exposure, method, parameters) in targets)
                {
                    lines.WriteLine($"// {exposure}")
                        .WriteLine($"// -> {method}({string.Join(", ", parameters.Select(p => p.FormalType + " " + p.Name))})");

                    token.ThrowIfCancellationRequested();
                }

                lines.WriteLine("throw new NotImplementedException();");
            }

            return new GeneratedMethod("Zilf.Compiler.Builtins.Generated", "ZBuiltinDecoders", dispatchMethodName, lines.GetLines());
        }

        private static string SanitizeName(string name)
        {
            return name switch
            {
                "+" => "Plus",
                "-" => "Minus",
                "*" => "Times",
                "/" => "Divide",
                _ => name
                    .Replace("==", "Eeq")
                    .Replace("=", "Eq")
                    .Replace("?", "_P")
                    .Replace('-', '_'),
            };
        }

        private static bool IsBuiltinMethodSyntax(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var methodDeclaration = syntaxNode.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDeclaration is null)
                return false;

            var classDeclaration = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration is null)
                return false;

            return
                classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        private static BuiltinMethodInfo? ExtractBuiltinMethodInfo(GeneratorAttributeSyntaxContext gsc, CancellationToken cancellationToken)
        {
            if (gsc.TargetSymbol is not IMethodSymbol methodSymbol)
                return null;

            var className = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? "_";
            var methodName = methodSymbol.Name;
            var targets = new List<Exposure>();
            var parameters = new List<Param>();
            ReturnValue returnValue;

            // populate targets from BuiltinAttribute attributes
            foreach (var attr in gsc.Attributes)
            {
                // extract the attribute properties
                int minVersion = 1, maxVersion = 6, priority = 1;
                bool hasSideEffect = false;
                object? data = null;

                foreach (var namedArg in attr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case "MinVersion":
                            minVersion = namedArg.Value.Value as int? ?? 999;
                            break;
                        case "MaxVersion":
                            maxVersion = namedArg.Value.Value as int? ?? -999;
                            break;
                        case "Priority":
                            priority = namedArg.Value.Value as int? ?? -999;
                            break;
                        case "HasSideEffect":
                            hasSideEffect = namedArg.Value.Value as bool? ?? false;
                            break;
                        case "Data":
                            data = namedArg.Value.Value;
                            break;
                    }
                }

                // the constructor arguments are the builtin's name, followed by any aliases
                // we don't distinguish between the name and the aliases, so just produce one Exposure for each argument
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg is { Kind: TypedConstantKind.Primitive, Type.SpecialType: SpecialType.System_String, Value: string name })
                    {
                        targets.Add(new Exposure(name, minVersion, maxVersion, hasSideEffect, priority, data));
                    }
                    else if (arg is { Kind: TypedConstantKind.Array } array)
                    {
                        foreach (var element in array.Values)
                        {
                            if (element is { Kind: TypedConstantKind.Primitive, Type.SpecialType: SpecialType.System_String, Value: string name2 })
                            {
                                targets.Add(new Exposure(name2, minVersion, maxVersion, hasSideEffect, priority, data));
                            }
                        }
                    }
                }
            }

            // the first parameter is the call type: ValueCall, VoidCall, PredCall, or ValuePredCall
            CallType callType;

            if (methodSymbol.Parameters.Length == 0)
            {
                // error
                return null;
            }

            switch (methodSymbol.Parameters[0].Type.Name)
            {
                case "ValueCall":
                    callType = CallType.ValueCall;
                    break;
                case "VoidCall":
                    callType = CallType.VoidCall;
                    break;
                case "PredCall":
                    callType = CallType.PredCall;
                    break;
                case "ValuePredCall":
                    callType = CallType.ValuePredCall;
                    break;
                default:
                    // error
                    return null;
            }

            // populate parameters from remaining method parameters
            for (int i = 1; i < methodSymbol.Parameters.Length; i++)
            {
                var param = methodSymbol.Parameters[i];
                var flags = ParamFlags.None;
                var defaultValue = param.HasExplicitDefaultValue ? QuoteValue(param.ExplicitDefaultValue) : null;

                // check for special parameter attributes
                if (param.IsOptional)
                {
                    flags |= ParamFlags.IsOptional;
                }

                if (param.IsParams)
                {
                    flags |= ParamFlags.IsVarArgs;
                }

                foreach (var attr in param.GetAttributes())
                {
                    switch (attr.AttributeClass?.Name)
                    {
                        case "DataAttribute":
                            flags |= ParamFlags.IsData;
                            break;
                        case "TableAttribute":
                            flags |= ParamFlags.IsTable;
                            break;
                        case "ObjectAttribute":
                            flags |= ParamFlags.IsObject;
                            break;
                        case "VariableAttribute":
                            flags |= ParamFlags.IsVariable;
                            // check for a VariableScopeQuirks named parameter
                            if (attr.NamedArguments.FirstOrDefault(attr => attr.Key == "VariableScopeQuirks") is
                                { Value.Kind: TypedConstantKind.Enum, Value.Value: int quirks })
                            {
                                // NOTE: these have to stay in sync with the enum values in VariableScopeQuirks
                                if ((quirks & 1) != 0)
                                {
                                    flags |= ParamFlags.IsVariableQuirkLocal;
                                }
                                if ((quirks & 2) != 0)
                                {
                                    flags |= ParamFlags.IsVariableQuirkGlobal;
                                }
                            }
                            break;
                    }
                }

                parameters.Add(new Param(param.Name, param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), flags, defaultValue));
            }

            // populate returnValue from method return type
            var returnFlags = ReturnFlags.None;

            foreach (var attr in methodSymbol.ReturnType.GetAttributes())
            {
                switch (attr.AttributeClass?.Name)
                {
                    case "TableAttribute":
                        returnFlags |= ReturnFlags.IsTable;
                        break;
                    case "ObjectAttribute":
                        returnFlags |= ReturnFlags.IsObject;
                        break;
                }
            }

            var returnType = methodSymbol.ReturnType.SpecialType switch
            {
                SpecialType.System_Void => "void",
                _ => methodSymbol.ReturnType.Name,
            };

            returnValue = new ReturnValue(returnType, returnFlags);

            return new BuiltinMethodInfo(callType, className, methodName, parameters.ToEquatableArray(), returnValue, targets.ToEquatableArray());
        }

        private static string QuoteValue(object? value) => value switch
        {
            null => "null",
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            _ => value.ToString(),
        };
    }
}
