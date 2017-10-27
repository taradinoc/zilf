using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TestHelper;

namespace ZilfErrorMessages.Test
{
    [TestClass, TestCategory("Analyzers")]
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
        public InterpreterError(int i, params object[] args) {}
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
    void Main(string a, object b) {
        throw new InterpreterError(""bad stuff"");
        throw new InterpreterError(string.Format(""{0} is a problem"", 1));
        throw new InterpreterError(""LOOK: I also object to "" + a + "" and "" + b);
    }
}
";
            var expected = new[] {
                new DiagnosticResult
                {
                    Id = "ZILF0001",
                    Message = "This exception should use a diagnostic code instead",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 31, 15) }
                },
                new DiagnosticResult
                {
                    Id = "ZILF0001",
                    Message = "This exception should use a diagnostic code instead",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 32, 15) }
                },                new DiagnosticResult
                {
                    Id = "ZILF0001",
                    Message = "This exception should use a diagnostic code instead",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 33, 15) }
                }
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
        public InterpreterError(int i, params object[] args) {}
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
        [Message(""{0} is a problem"")]
        public const int _0_Is_A_Problem = 3;
        [Message(""{0}: I also object to {1} and {2}"")]
        public const int _0_I_Also_Object_To_1_And_2 = 4;
    }
}

class Program {
    void Main(string a, object b) {
        throw new InterpreterError(InterpreterMessages.Bad_Stuff);
        throw new InterpreterError(InterpreterMessages._0_Is_A_Problem, 1);
        throw new InterpreterError(InterpreterMessages._0_I_Also_Object_To_1_And_2, ""LOOK"", a, b);
    }
}
";
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [TestMethod]
        public async Task ErrorExceptionUsageAnalyzer_DontFlagWhenFirstArgIsAlreadyACode()
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
        [Message(""I don't like '{0}'"")]
        public const int NoThanks = 1;
    }
}

class Program {
    void Main() {
        throw new InterpreterError(InterpreterMessages.NoThanks, ""spam"");
    }
}
";

            await VerifyCSharpDiagnosticAsync(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ErrorExceptionUsageCodeFixProvider();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            throw new System.NotImplementedException();
        }

        [NotNull]
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ErrorExceptionUsageAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new System.NotImplementedException();
        }
    }
}