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
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter"), TestCategory("Arguments")]
    public class ArgDecoderTests
    {
        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_1()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG (1) FOO>",
                ex => ex.Message.EndsWith("PROG: arg 1: element 1: expected ADECL, ATOM, or LIST",
                    StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_2()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG A B>",
                ex => ex.Message.EndsWith("PROG: arg 2: expected LIST", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_3()
        {
           TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG 1 B>",
               ex => ex.Message.EndsWith("PROG: arg 1: expected ATOM or LIST", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_4()
        {
           TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG (()) F>",
               ex => ex.Message.EndsWith("PROG: arg 1: element 1 requires exactly 2 elements",
                   StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_5()
        {
           TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG ((A)) F>",
               ex => ex.Message.EndsWith("PROG: arg 1: element 1 requires exactly 2 elements",
                   StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_6()
        {
           TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG ((A B C)) F>",
               ex => ex.Message.EndsWith("PROG: arg 1: element 1 requires exactly 2 elements",
                   StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_7()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG ((1 A)) F>",
                ex => ex.Message.EndsWith("PROG: arg 1: element 1: element 1: expected ADECL or ATOM",
                    StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_PROG_ArgumentDecodingError_Message_8()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG () #DECL ()>",
                ex => ex.Message.EndsWith("PROG requires 1 or more additional args", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_STRING_ArgumentDecodingError_Message_1()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<STRING 123>",
                ex => ex.Message.EndsWith("STRING: arg 1: expected STRING or CHARACTER", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_STRING_ArgumentDecodingError_Message_2()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>(@"<STRING ""hi"" 5>",
                ex => ex.Message.EndsWith("STRING: arg 2: expected STRING or CHARACTER", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_MAX_ArgumentDecodingError_Message_1()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<MAX POWER>",
                ex => ex.Message.EndsWith("MAX: arg 1: expected FIX", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_MAX_ArgumentDecodingError_Message_2()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<MAX 4 POWER>",
                ex => ex.Message.EndsWith("MAX: arg 2: expected FIX", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_SUBSTRUC_ArgumentDecodingError_Message_1()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<SUBSTRUC '(1 2 3) FOO>",
                ex => ex.Message.EndsWith("SUBSTRUC: arg 2: expected FIX or structured value", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_SUBSTRUC_ArgumentDecodingError_Message_2()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<SUBSTRUC '(1 2 3) 1 FOO>",
                ex => ex.Message.EndsWith("SUBSTRUC: arg 3: expected FIX or structured value", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_SUBSTRUC_ArgumentDecodingError_Message_3()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<SUBSTRUC '(1 2 3) 1 2 FOO>",
                ex => ex.Message.EndsWith("SUBSTRUC: arg 4: expected structured value", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Test_BIND_Allows_Mixed_Bindings_With_Initializers()
        {
            var ctx = new Context();
            var parsed = Program.Parse(ctx, "<BIND ((FIRST 1) (SECOND 2) THIRD FOURTH) .SECOND>").ToArray();
            var form = (ZilForm)parsed.Single();
            var elements = form.Cast<ZilObject>().ToArray();
            Assert.AreEqual("BIND", ((ZilAtom)elements[0]).ToString());
            var bindingsArg = elements[1];
            var bindings = ((IStructure)bindingsArg).Cast<ZilObject>().ToArray();
            var firstBinding = (IStructure)bindings[0];
            var firstBindingElements = firstBinding.Cast<ZilObject>().ToArray();
            Assert.AreEqual(2, firstBindingElements.Length);
            Assert.IsInstanceOfType<ZilAtom>(firstBindingElements[0]);
            Assert.IsInstanceOfType<ZilFix>(firstBindingElements[1]);
            Assert.AreEqual(ctx.GetStdAtom(StdAtom.LIST), ((ZilObject)bindings[0]).GetTypeAtom(ctx));
            var secondBinding = (IStructure)bindings[1];
            var secondBindingElements = secondBinding.Cast<ZilObject>().ToArray();
            Assert.AreEqual(2, secondBindingElements.Length);
            Assert.IsInstanceOfType<ZilAtom>(secondBindingElements[0]);
            Assert.IsInstanceOfType<ZilFix>(secondBindingElements[1]);
            Assert.AreEqual(ctx.GetStdAtom(StdAtom.LIST), ((ZilObject)bindings[1]).GetTypeAtom(ctx));

            var generatedParsersType = typeof(Subrs).Assembly.GetType("Zilf.Interpreter.GeneratedSubrParsers", throwOnError: true)!;
            var functionCallSiteType = typeof(Subrs).Assembly.GetType("Zilf.Interpreter.FunctionCallSite", throwOnError: true)!;
            var structuredCallSiteType = typeof(Subrs).Assembly.GetType("Zilf.Interpreter.StructuredArgumentCallSite", throwOnError: true)!;
            var errorRankerType = typeof(Subrs).Assembly.GetType("Zilf.Interpreter.ErrorRanker", throwOnError: true)!;

            var callSite = Activator.CreateInstance(functionCallSiteType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, ["BIND"], CultureInfo.InvariantCulture)!;
            var bindingSite = Activator.CreateInstance(structuredCallSiteType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [callSite, 0], CultureInfo.InvariantCulture)!;
            var ranker = Activator.CreateInstance(errorRankerType);

            var bindArgs = elements.Skip(1).ToArray();

            var bindingListHelper = generatedParsersType.GetMethod("Generated_ParseBindingList_Helper", BindingFlags.Static | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("Generated_ParseBindingList_Helper not found");

            var helperArgs = new object?[]
            {
                bindArgs,
                0,
                ctx,
                callSite,
                null,
                ranker
            };

            var helperResult = (bool)bindingListHelper.Invoke(null, helperArgs)!;
            Assert.IsTrue(helperResult, "Expected binding list helper to succeed");

            var bindingHelper = generatedParsersType.GetMethod("Generated_ParseBinding_Helper", BindingFlags.Static | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("Generated_ParseBinding_Helper not found");
            var perBindingRanker = Activator.CreateInstance(errorRankerType);
            object perBindingIndex = 0;
            object? bindingResult = null;
            var perBindingArgs = new object?[]
            {
                bindings,
                perBindingIndex,
                ctx,
                bindingSite,
                bindingResult,
                perBindingRanker
            };

            var firstBindingOk = (bool)bindingHelper.Invoke(null, perBindingArgs)!;
            Assert.IsTrue(firstBindingOk, "First binding should parse");
            perBindingIndex = perBindingArgs[1]!;
            bindingResult = perBindingArgs[4];

            perBindingArgs[1] = perBindingIndex;
            perBindingArgs[4] = bindingResult;
            var secondBindingOk = (bool)bindingHelper.Invoke(null, perBindingArgs)!;
            Assert.IsTrue(secondBindingOk, "Second binding should parse");

            var result = TestHelpers.Evaluate(ctx, "<BIND ((FIRST 1) (SECOND 2) THIRD FOURTH) .SECOND>");

            Assert.IsInstanceOfType<ZilFix>(result);
            Assert.AreEqual(2, ((ZilFix)result).Value);
        }
    }
}
