namespace ZilfPub.Syntax;

internal static class ZilKeywords
{
    private static readonly HashSet<string> ControlForms = new(StringComparer.OrdinalIgnoreCase)
    {
        "COND", "BIND", "PROG", "REPEAT", "DO", "MAPF", "MAPR", "MAP-CONTENTS", "MAP-DIRECTIONS",
        "AGAIN", "RETURN", "RTRUE", "RFALSE", "CATCH", "THROW", "EVAL", "AND", "OR", "NOT"
    };

    private static readonly HashSet<string> OutputForms = new(StringComparer.OrdinalIgnoreCase)
    {
        "TELL", "TELL-TOKENS", "ADD-TELL-TOKENS", "CRLF", "PRINT", "PRINTI", "PRINTR", "PRINTB", "PRINC", "PRIN1"
    };

    private static readonly HashSet<string> ZModelForms = new(StringComparer.OrdinalIgnoreCase)
    {
        "FSET", "FSET?", "FCLEAR", "MOVE", "REMOVE", "IN?", "FIRST?", "NEXT?",
        "PUTP", "GETP", "PROPDEF", "GETPT", "PTSIZE", "INTBL?",
        "TABLE", "PTABLE", "LTABLE", "ITABLE", "GET", "GETB", "GET/B", "PUT", "PUTB", "PUT/B",
        "ZGET", "ZPUT", "VOC", "SYNONYM", "VERB-SYNONYM", "PREP-SYNONYM", "ADJ-SYNONYM",
        "DIR-SYNONYM", "BIT-SYNONYM", "DIRECTIONS", "BUZZ"
    };

    private static readonly HashSet<string> MetaForms = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT-FILE", "PACKAGE", "ENDPACKAGE", "USE", "ENTRY", "RENTRY", "VERSION",
        "COMPILATION-FLAG", "COMPILATION-FLAG-DEFAULT", "REPLACE-DEFINITION",
        "DELAY-DEFINITION", "DEFAULT-DEFINITION"
    };

    private static readonly HashSet<string> DefinitionFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEFINE", "DEFINE20", "DEFMAC", "ROUTINE"
    };

    private static readonly HashSet<string> DefinitionObjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "OBJECT", "ROOM"
    };

    private static readonly HashSet<string> DefinitionGlobals = new(StringComparer.OrdinalIgnoreCase)
    {
        "SETG", "CONSTANT", "GLOBAL", "GASSIGNED?", "GUNASSIGN"
    };

    private static readonly HashSet<string> DefinitionLocals = new(StringComparer.OrdinalIgnoreCase)
    {
        "SET", "ASSIGNED?", "UNASSIGN"
    };

    private static readonly HashSet<string> TypeOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHTYPE", "TYPE", "TYPE?", "PRIMTYPE"
    };

    private static readonly HashSet<string> TypeDefinitions = new(StringComparer.OrdinalIgnoreCase)
    {
        "NEWTYPE", "DEFSTRUCT", "APPLYTYPE", "EVALTYPE", "PRINTTYPE", "TYPEPRIM"
    };

    private static readonly HashSet<string> SyntaxKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SYNTAX"
    };

    private static readonly HashSet<string> ArithmeticOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "+", "-", "*", "/", "MOD", "MIN", "MAX", "OR?", "AND?"
    };

    private static readonly HashSet<string> BitwiseOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "BAND", "BOR", "ANDB", "ORB", "LSH", "XORB", "EQVB"
    };

    private static readonly HashSet<string> ComparisonOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "==", "N=", "N==", "L", "L=", "G", "G=", "0?", "1?", "T?", "F?"
    };

    private static readonly HashSet<string> Booleans = new(StringComparer.OrdinalIgnoreCase)
    {
        "T", "TRUE", "FALSE"
    };

    public static string? GetKeywordCssClass(string atomText)
    {
        if (ControlForms.Contains(atomText))
        {
            return "zil-keyword-control";
        }

        if (OutputForms.Contains(atomText))
        {
            return "zil-keyword-output";
        }

        if (ZModelForms.Contains(atomText))
        {
            return "zil-keyword-zmodel";
        }

        if (MetaForms.Contains(atomText))
        {
            return "zil-keyword-meta";
        }

        if (DefinitionFunctions.Contains(atomText))
        {
            return "zil-keyword-def-func";
        }

        if (DefinitionObjects.Contains(atomText))
        {
            return "zil-keyword-def-object";
        }

        if (DefinitionGlobals.Contains(atomText))
        {
            return "zil-keyword-def-global";
        }

        if (DefinitionLocals.Contains(atomText))
        {
            return "zil-keyword-def-local";
        }

        if (TypeOperators.Contains(atomText))
        {
            return "zil-keyword-type-op";
        }

        if (TypeDefinitions.Contains(atomText))
        {
            return "zil-keyword-type-def";
        }

        if (SyntaxKeywords.Contains(atomText))
        {
            return "zil-keyword-syntax";
        }

        if (ArithmeticOperators.Contains(atomText))
        {
            return "zil-keyword-op-arith";
        }

        if (BitwiseOperators.Contains(atomText))
        {
            return "zil-keyword-op-bit";
        }

        if (ComparisonOperators.Contains(atomText))
        {
            return "zil-keyword-op-cmp";
        }

        if (Booleans.Contains(atomText))
        {
            return "zil-keyword-bool";
        }

        if (string.Equals(atomText, "ELSE", StringComparison.OrdinalIgnoreCase))
        {
            return "zil-keyword-else";
        }

        return null;
    }
}
