using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TestHelper;
using ZilfErrorMessages;

namespace ZilfErrorMessages.Test
{
    [TestClass]
    public class ErrorExceptionUsageTests : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public async Task ErrorExceptionUsageAnalyzer_NotTriggered()
        {
            var test = @"";

            await VerifyCSharpDiagnosticAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task ErrorExceptionUsageAnalyzer_TriggeredAndFixed()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Language {
    class InterpreterError : Exception {
        public InterpreterError(string s) {}
        public InterpreterError(int i) {}
    }
}

namespace Zilf.Diagnostics {
    [AttributeUsage(AttributeTargets.Field)]
    class MessageAttribute : Attribute {
        public MessageAttribute(string s) {}
    }

    class InterpreterMessages {
        public const int Foo = 1;
    }
}

class Program {
    void Main() {
        throw new InterpreterError(""bad stuff"");
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "ZILF0001",
                Message = "This exception should use a diagnostic code instead",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 31, 15) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);

            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Language {
    class InterpreterError : Exception {
        public InterpreterError(string s) {}
        public InterpreterError(int i) {}
    }
}

namespace Zilf.Diagnostics {
    [AttributeUsage(AttributeTargets.Field)]
    class MessageAttribute : Attribute {
        public MessageAttribute(string s) {}
    }

    class InterpreterMessages {
        public const int Foo = 1;
        [Message(""bad stuff"")]
        public const int Bad_Stuff = 2;
    }
}

class Program {
    void Main() {
        throw new InterpreterError(InterpreterMessages.Bad_Stuff);
    }
}
";
            await VerifyCSharpFixAsync(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ErrorExceptionUsageCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ErrorExceptionUsageAnalyzer();
        }
    }
}