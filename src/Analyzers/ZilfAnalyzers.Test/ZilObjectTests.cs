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

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZilfAnalyzers.Test.Helpers;
using DiagnosticVerifier = ZilfAnalyzers.Test.Helpers.DiagnosticVerifier;

namespace ZilfAnalyzers.Test
{
    [TestClass, TestCategory("Analyzers")]
    public class ZilObjectTests : DiagnosticVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        [Timeout(10000)]
        public async Task ZilObjectAnalyzer_NotTriggeredAsync()
        {
            var test = @"";

            await VerifyCSharpDiagnosticAsync(test);
        }

        //Diagnostic triggered and checked for
        [TestMethod]
        //[Timeout(10000)]
        [Timeout(999999999)]
        public async Task ZilObjectAnalyzer_TriggeredAsync()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Language;

namespace Zilf.Interpreter.Values {
    class ZilObject { public virtual bool ExactlyEquals(ZilObject other) => false; public override int GetHashCode() => 0; }

    class ZilList : ZilObject { }

    class Usage {
        void Main() {
            ZilList list1 = new ZilList(), list2 = new ZilList();

            if (list1.Equals(list2))
                Console.WriteLine(""equal"");

            if (list2?.Equals(list1))
                Console.WriteLine(""equal"");

            Assert.AreEqual(list1, list2);
            Assert.AreNotEqual(list2, list1, ""blah"");
            CollectionAssert.AreEqual(new[] { list1 }, new[] { list2 });
            (new[] { list1 }).SequenceEqual(new object[] { list2 });
            object.Equals(list1, list2);
            Equals(list1, list2);
        }
    }

    class FooList : ZilList {
        public override bool Equals(object other) => true;
    }

    class BarList : ZilList {
        public override bool ExactlyEquals(ZilObject other) => true;
    }
}

namespace Microsoft.VisualStudio.TestTools.UnitTesting {
    static class Assert {
        public static void AreEqual(object a, object b, string msg = null) {}
        public static void AreNotEqual(object a, object b, string msg = null) {}
    }

    static class CollectionAssert {
        public static void AreEqual(IEnumerable<object> a, IEnumerable<object> b) {}
    }
}
";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'object.Equals(object?)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 17) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'object.Equals(object?)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 23, 17) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'Assert.AreEqual(object, object, string)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 13) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'Assert.AreNotEqual(object, object, string)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 13) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'CollectionAssert.AreEqual(IEnumerable<object>, IEnumerable<object>)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 28, 13) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'Enumerable.SequenceEqual<TSource>(IEnumerable<TSource>, IEnumerable<TSource>)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 29, 13) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'object.Equals(object?, object?)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 30, 13) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0005",
                    Message = "'object.Equals(object?, object?)' is for hash-safe comparisons: prefer methods with explicit MDL comparison behavior",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 31, 13) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0006",
                    Message = "'FooList' overrides object.Equals(object?) but does not override ZilObject.ExactlyEquals(ZilObject)",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 36, 30) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0006",
                    Message = "'BarList' overrides ZilObject.ExactlyEquals(ZilObject) but does not override ZilObject.GetHashCode()",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 40, 30) }
                },
            };

            await VerifyCSharpDiagnosticAsync(test, expected);

#if CODEFIX
            var fixtest = @"
            using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Diagnostics {
    [AttributeUsage(AttributeTargets.Class)]
    class MessageSetAttribute : Attribute {
        public MessageSetAttribute(string s) {}
    }

    [AttributeUsage(AttributeTargets.Field)]
    class MessageAttribute : Attribute {
        public MessageAttribute(string s) {}
    }

    [MessageSet(""foo"")]
    class InterpreterMessages {
        [Message(""{0}: foo"")]
        public const int Foo = 1;
        [Message(""bar"")]
        public const int Bar = 1;
        [Message(""bar"")]
        public const int Baz = 2;
        [Message(""{0}: quux {1}"")]
        public const int _0_Quux_1 = 2;
    }

    class InterpreterError : Exception {
        public InterpreterError(int code, params object[] args) {}
    }

    class Usage {
        void Main() {
            throw new InterpreterError(InterpreterMessages._0_Quux_1, ""QU-UX?"");
            throw new InterpreterError(InterpreterMessages._0_Quux_1, ""QU-UX?"", 123);
        }
    }
}
";

            await VerifyCSharpFixAsync(test, fixtest);
#endif
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ZilObjectAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new System.NotImplementedException();
        }

#if CODEFIX
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ZilObjectCodeFixProvider();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}