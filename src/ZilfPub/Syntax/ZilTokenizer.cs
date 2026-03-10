using System.Globalization;

namespace ZilfPub.Syntax;

internal static class ZilTokenizer
{
    private const string DelimiterChars = "\t\n\r \"'`:;,%().[]<>{}#!";

    public static IEnumerable<ZilToken> Tokenize(string source)
    {
        var i = 0;

        while (i < source.Length)
        {
            if (char.IsWhiteSpace(source[i]))
            {
                var start = i;
                i++;
                while (i < source.Length && char.IsWhiteSpace(source[i]))
                {
                    i++;
                }

                yield return new(ZilTokenType.Whitespace, source[start..i], string.Empty);
                continue;
            }

            var c = source[i];

            if (c == ';')
            {
                var end = ScanExpressionEnd(source, i + 1);
                yield return new(ZilTokenType.Comment, source[i..end], "zil-comment");
                i = end;
                continue;
            }

            if (c == '"')
            {
                var end = ScanStringEnd(source, i);
                yield return new(ZilTokenType.String, source[i..end], "zil-string");
                i = end;
                continue;
            }

            if (TryReadCharLiteral(source, i, out var charEnd))
            {
                yield return new(ZilTokenType.Char, source[i..charEnd], "zil-char");
                i = charEnd;
                continue;
            }

            if (TryReadNumber(source, i, out var numberEnd, out var numberCssClass))
            {
                yield return new(ZilTokenType.Number, source[i..numberEnd], numberCssClass);
                i = numberEnd;
                continue;
            }

            if (TryReadBracket(c, out var bracketClass))
            {
                yield return new(ZilTokenType.Bracket, source[i].ToString(CultureInfo.InvariantCulture), bracketClass);
                i++;
                continue;
            }

            if (TryReadPrefix(source, i, out var prefixLength, out var prefixClass))
            {
                yield return new(ZilTokenType.Prefix, source.Substring(i, prefixLength), prefixClass);
                i += prefixLength;
                continue;
            }

            if (TryReadAtom(source, i, out var atomEnd))
            {
                var text = source[i..atomEnd];
                var keywordClass = ZilKeywords.GetKeywordCssClass(text);
                yield return new(
                    keywordClass is null ? ZilTokenType.Atom : ZilTokenType.Keyword,
                    text,
                    keywordClass ?? "zil-atom");
                i = atomEnd;
                continue;
            }

            yield return new(ZilTokenType.Invalid, source[i].ToString(CultureInfo.InvariantCulture), "zil-invalid");
            i++;
        }
    }

    private static bool TryReadCharLiteral(string source, int start, out int end)
    {
        if (start + 2 < source.Length && source[start] == '!' && source[start + 1] == '\\')
        {
            end = start + 3;
            return true;
        }

        end = start;
        return false;
    }

    private static bool TryReadNumber(string source, int start, out int end, out string cssClass)
    {
        if (TryReadBinaryNumber(source, start, out end))
        {
            cssClass = "zil-number-bin";
            return true;
        }

        if (TryReadHexNumber(source, start, out end))
        {
            cssClass = "zil-number-hex";
            return true;
        }

        if (TryReadOctalNumber(source, start, out end))
        {
            cssClass = "zil-number-oct";
            return true;
        }

        if (TryReadDecimalNumber(source, start, out end))
        {
            cssClass = "zil-number-dec";
            return true;
        }

        cssClass = string.Empty;
        end = start;
        return false;
    }

    private static bool TryReadBinaryNumber(string source, int start, out int end)
    {
        end = start;

        if (start >= source.Length || source[start] != '#')
        {
            return false;
        }

        var i = start + 1;
        i = SkipWhitespace(source, i);

        if (i >= source.Length || source[i] != '2')
        {
            return false;
        }

        i++;
        var wsStart = i;
        i = SkipWhitespace(source, i);
        if (i == wsStart)
        {
            return false;
        }

        var digitsStart = i;
        while (i < source.Length && (source[i] == '0' || source[i] == '1'))
        {
            i++;
        }

        if (i == digitsStart || !IsTokenBoundary(source, i))
        {
            return false;
        }

        end = i;
        return true;
    }

    private static bool TryReadHexNumber(string source, int start, out int end)
    {
        end = start;

        if (start >= source.Length || source[start] != '#')
        {
            return false;
        }

        var i = start + 1;
        i = SkipWhitespace(source, i);

        if (i + 1 >= source.Length || source[i] != '1' || source[i + 1] != '6')
        {
            return false;
        }

        i += 2;
        var wsStart = i;
        i = SkipWhitespace(source, i);
        if (i == wsStart)
        {
            return false;
        }

        var digitsStart = i;
        while (i < source.Length && IsHexDigit(source[i]))
        {
            i++;
        }

        if (i == digitsStart || !IsTokenBoundary(source, i))
        {
            return false;
        }

        end = i;
        return true;
    }

    private static bool TryReadOctalNumber(string source, int start, out int end)
    {
        end = start;

        if (start >= source.Length || source[start] != '*')
        {
            return false;
        }

        var i = start + 1;
        var digitsStart = i;
        while (i < source.Length && source[i] is >= '0' and <= '7')
        {
            i++;
        }

        if (i == digitsStart || i >= source.Length || source[i] != '*')
        {
            return false;
        }

        end = i + 1;
        return IsTokenBoundary(source, end);
    }

    private static bool TryReadDecimalNumber(string source, int start, out int end)
    {
        end = start;

        var i = start;
        if (source[i] == '-')
        {
            i++;
            if (i >= source.Length || !char.IsDigit(source[i]))
            {
                return false;
            }
        }
        else if (!char.IsDigit(source[i]))
        {
            return false;
        }

        while (i < source.Length && char.IsDigit(source[i]))
        {
            i++;
        }

        if (!IsTokenBoundary(source, i))
        {
            return false;
        }

        end = i;
        return true;
    }

    private static bool TryReadBracket(char c, out string cssClass)
    {
        cssClass = c switch
        {
            '<' or '>' => "zil-bracket-form",
            '(' or ')' => "zil-bracket-list",
            '[' or ']' => "zil-bracket-vector",
            '{' or '}' => "zil-bracket-struct",
            _ => string.Empty,
        };

        return cssClass.Length > 0;
    }

    private static bool TryReadPrefix(string source, int start, out int length, out string cssClass)
    {
        length = 0;
        cssClass = string.Empty;

        var c = source[start];

        if (c == '%' && start + 1 < source.Length && source[start + 1] == '%')
        {
            length = 2;
            cssClass = "zil-prefix-macro";
            return true;
        }

        switch (c)
        {
            case '%':
                length = 1;
                cssClass = "zil-prefix-macro";
                return true;
            case '.':
                length = 1;
                cssClass = "zil-prefix-local";
                return true;
            case ',':
                length = 1;
                cssClass = "zil-prefix-global";
                return true;
            case '\'':
                length = 1;
                cssClass = "zil-prefix-quote";
                return true;
            case '`':
                length = 1;
                cssClass = "zil-prefix-quasiquote";
                return true;
            case '~':
                length = 1;
                cssClass = "zil-prefix-unquote";
                return true;
            case '!':
                length = 1;
                cssClass = "zil-prefix-bang";
                return true;
            case '#':
                length = 1;
                cssClass = "zil-prefix-sharp";
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadAtom(string source, int start, out int end)
    {
        end = start;
        if (start >= source.Length || IsAtomDelimiter(source[start]))
        {
            return false;
        }

        var i = start;
        while (i < source.Length && !IsAtomDelimiter(source[i]))
        {
            i++;
        }

        end = i;
        return i > start;
    }

    private static int ScanExpressionEnd(string source, int start)
    {
        var i = SkipWhitespace(source, start);
        if (i >= source.Length)
        {
            return source.Length;
        }

        var c = source[i];

        if (c == ';')
        {
            return ScanExpressionEnd(source, i + 1);
        }

        if (c == '"')
        {
            return ScanStringEnd(source, i);
        }

        if (TryReadCharLiteral(source, i, out var charEnd))
        {
            return charEnd;
        }

        if (TryReadNumber(source, i, out var numberEnd, out _))
        {
            return numberEnd;
        }

        if (TryReadPrefix(source, i, out var prefixLength, out _))
        {
            return ScanExpressionEnd(source, i + prefixLength);
        }

        if (TryGetBracketPair(c, out var closeChar))
        {
            return ScanBalanced(source, i + 1, closeChar);
        }

        if (TryReadAtom(source, i, out var atomEnd))
        {
            return atomEnd;
        }

        return i + 1;
    }

    private static int ScanBalanced(string source, int start, char closeChar)
    {
        var i = start;

        while (i < source.Length)
        {
            i = SkipWhitespace(source, i);
            if (i >= source.Length)
            {
                return source.Length;
            }

            if (source[i] == closeChar)
            {
                return i + 1;
            }

            i = ScanExpressionEnd(source, i);
        }

        return source.Length;
    }

    private static int ScanStringEnd(string source, int start)
    {
        var i = start + 1;

        while (i < source.Length)
        {
            if (source[i] == '"')
            {
                return i + 1;
            }

            if (source[i] == '\\' && i + 1 < source.Length)
            {
                i += 2;
                continue;
            }

            i++;
        }

        return source.Length;
    }

    private static bool TryGetBracketPair(char openChar, out char closeChar)
    {
        closeChar = openChar switch
        {
            '<' => '>',
            '(' => ')',
            '[' => ']',
            '{' => '}',
            _ => '\0',
        };

        return closeChar != '\0';
    }

    private static int SkipWhitespace(string source, int index)
    {
        var i = index;
        while (i < source.Length && char.IsWhiteSpace(source[i]))
        {
            i++;
        }

        return i;
    }

    private static bool IsHexDigit(char c) =>
        c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

    private static bool IsTokenBoundary(string source, int index) =>
        index >= source.Length || IsAtomDelimiter(source[index]);

    private static bool IsAtomDelimiter(char c) => DelimiterChars.Contains(c);
}
