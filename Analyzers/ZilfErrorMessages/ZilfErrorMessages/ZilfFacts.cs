using System.Collections.Immutable;

namespace ZilfErrorMessages
{
    static class ZilfFacts
    {
        public static readonly ImmutableDictionary<string, string> MessageTypeMap =
            ImmutableDictionary<string, string>.Empty
                .Add("Zilf.Language.InterpreterError", "InterpreterMessages")
                .Add("Zilf.Language.CompilerError", "CompilerMessages");
    }
}
