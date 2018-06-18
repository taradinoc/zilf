using System.Collections.Immutable;

namespace ZilfAnalyzers
{
    static class ZilfFacts
    {
        public static readonly ImmutableDictionary<string, string> MessageTypeMap =
            ImmutableDictionary<string, string>.Empty
                .Add("Zilf.Language.InterpreterError", "InterpreterMessages")
                .Add("Zilf.Language.CompilerError", "CompilerMessages");
    }
}
