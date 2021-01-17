using System;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Parsing
{
    delegate ParserOutput SimplePrefixMacroHandler(ZilObject zo);

    interface IParserSite
    {
        ZilAtom ParseAtom(string text);

        ZilAtom GetTypeAtom(ZilObject zo);

        ZilObject ChangeType(ZilObject zo, ZilAtom type);

        ZilObject Evaluate(ZilObject zo);

        ZilObject? GetGlobalVal(ZilAtom atom);

        string CurrentFilePath { get; }

        SimplePrefixMacroHandler? GetPrefixMacro(char prefix);
    }
}