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
using System.Runtime.CompilerServices;
using Zilf.Emit;
using Zilf.Interpreter.Values;

namespace ZilfSourceGenerators.Test
{
    [TestClass]
    public class SourceGeneratorTests : VerifyBase
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            VerifySourceGenerators.Initialize();
        }

        private Task VerifyOutput<T>(string source, CancellationToken cancellationToken = default)
            where T : IIncrementalGenerator, new()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Concat([
                    MetadataReference.CreateFromFile(typeof(IOperand).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ZilObject).Assembly.Location),
                ]);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new T();

            var driver = CSharpGeneratorDriver.Create(generator)
                .RunGenerators(compilation, cancellationToken);

            return Verify(driver);
        }

        [TestMethod]
        public Task GeneratesSubrWrappersCorrectly()
        {
            var source =
                """
                namespace Zilf.Interpreter;
                using Zilf.Interpreter.Values;
                static partial class Subrs
                {
                    [Subr("EMPTY?")]
                    public static ZilObject EMPTY_P(Context ctx, IStructure st)
                    {
                        return st.IsEmpty ? ctx.TRUE : ctx.FALSE;
                    }
                }
                """;

            return VerifyOutput<SubrWrapperGenerator>(source);
        }

        [TestMethod]
        public Task GeneratesZBuiltinWrappersCorrectly()
        {
            var source =
                """
                namespace Zilf.Compiler.Builtins;
                using Zilf.Emit;

                static partial class ZBuiltins
                {
                    [Builtin("N=?", "N==?")]
                    public static void NegatedVarargsEqualityOp(
                        PredCall c, IOperand arg1, IOperand arg2,
                        params IOperand[] restOfArgs)
                    {
                        throw new NotImplementedException();
                    }

                    [Builtin("+", Data = "add")]
                    [Builtin("-", Data = "sub")]
                    public static IOperand AddOrSubtract(
                        ValueCall c, [Data] string op, int arg1, int arg2)
                    {
                        throw new NotImplementedException();
                    }

                    [Builtin("OPTION")]
                    public static IOperand Option(
                        VoidCall c, IOperand arg1, IOperand arg2 = null)
                    {
                        throw new NotImplementedException();
                    }
                }
                """;

            return VerifyOutput<ZBuiltinWrapperGenerator>(source);
        }
    }
}