namespace ZilfPub.Syntax;

internal enum ZilTokenType
{
    Whitespace,
    Comment,
    String,
    Number,
    Char,
    Prefix,
    Bracket,
    Keyword,
    Atom,
    Invalid,
}

internal sealed record ZilToken(ZilTokenType Type, string Text, string CssClass);
