using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZilfErrorMessages
{
    class ZilfFacts
    {
        public static readonly ImmutableDictionary<string, string> MessageTypeMap =
            ImmutableDictionary<string, string>.Empty
                .Add("Zilf.Language.InterpreterError", "InterpreterMessages")
                .Add("Zilf.Language.CompilerError", "CompilerMessages");
    }
}
