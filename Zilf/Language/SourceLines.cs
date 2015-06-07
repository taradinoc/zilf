namespace Zilf.Language
{
    static class SourceLines
    {
        public static readonly ISourceLine Unknown = new StringSourceLine("<internally created FORM>");
        public static readonly ISourceLine Chtyped = new StringSourceLine("<result of CHTYPE>");
        public static readonly ISourceLine MakeGval = new StringSourceLine("<result of MAKE-GVAL>");
    }
}