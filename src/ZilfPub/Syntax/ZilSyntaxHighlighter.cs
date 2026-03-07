using System.Net;
using System.Text;

namespace ZilfPub.Syntax;

internal static class ZilSyntaxHighlighter
{
    private const string DelimiterChars = "\t\n\r \"'`:;,%().[]<>{}#!";

    public static string HighlightHtml(string sourceText)
    {
        var sb = new StringBuilder(sourceText.Length + (sourceText.Length / 4));
        var index = 0;

        while (index < sourceText.Length)
        {
            RenderExpression(sourceText, ref index, sb);
        }

        return sb.ToString();
    }

    private static void RenderExpression(string source, ref int index, StringBuilder sb)
    {
        RenderWhitespace(source, ref index, sb);
        if (index >= source.Length)
        {
            return;
        }

        var c = source[index];

        if (c == ';')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, ";", "zil-prefix-comment", "zil-comment");
            return;
        }

        if (c == '\'')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, "'", "zil-prefix-quote", "zil-quoted");
            return;
        }

        if (c == '`')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, "`", "zil-prefix-quasiquote", "zil-quasiquoted");
            return;
        }

        if (c == '~')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, "~", "zil-prefix-unquote", "zil-unquoted");
            return;
        }

        if (c == '.')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, ".", "zil-prefix-local", "zil-local-ref");
            return;
        }

        if (c == ',')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, ",", "zil-prefix-global", "zil-global-ref");
            return;
        }

        if (c == '%' && index + 1 < source.Length && source[index + 1] == '%')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, "%%", "zil-prefix-macro", "zil-macro-splice");
            return;
        }

        if (c == '%')
        {
            RenderWrappedPrefixExpression(source, ref index, sb, "%", "zil-prefix-macro", "zil-macro");
            return;
        }

        if (c == '"')
        {
            var stringEnd = ScanStringEnd(source, index);
            RenderLeaf(source[index..stringEnd], "zil-string", sb);
            index = stringEnd;
            return;
        }

        if (TryReadCharLiteral(source, index, out var charEnd))
        {
            RenderLeaf(source[index..charEnd], "zil-char", sb);
            index = charEnd;
            return;
        }

        if (TryReadNumber(source, index, out var numberEnd, out var numberCssClass))
        {
            RenderLeaf(source[index..numberEnd], numberCssClass, sb);
            index = numberEnd;
            return;
        }

        if (TryGetBracketPair(c, out var closeChar))
        {
            sb.Append("<span class=\"zil-structure ");
            sb.Append(GetStructureClass(c));
            sb.Append("\">");

            RenderLeaf(c.ToString(), "zil-bracket" + GetBracketSuffix(c), sb);
            index++;

            while (index < source.Length)
            {
                RenderWhitespace(source, ref index, sb);
                if (index >= source.Length)
                {
                    return;
                }

                if (source[index] == closeChar)
                {
                    RenderLeaf(closeChar.ToString(), "zil-bracket" + GetBracketSuffix(closeChar), sb);
                    index++;
                    sb.Append("</span>");
                    return;
                }

                RenderExpression(source, ref index, sb);
            }

            sb.Append("</span>");

            return;
        }

        if (TryReadPrefix(source[index], out var prefixClass))
        {
            RenderLeaf(source[index].ToString(), prefixClass, sb);
            index++;
            return;
        }

        if (TryReadAtom(source, index, out var atomEnd))
        {
            var atom = source[index..atomEnd];
            var keywordClass = ZilKeywords.GetKeywordCssClass(atom) ?? "zil-atom";
            RenderLeaf(atom, keywordClass, sb);
            index = atomEnd;
            return;
        }

        RenderLeaf(source[index].ToString(), "zil-invalid", sb);
        index++;
    }

    private static void RenderWrappedPrefixExpression(
        string source,
        ref int index,
        StringBuilder sb,
        string prefixText,
        string prefixCssClass,
        string wrapperCssClass)
    {
        sb.Append("<span class=\"");
        sb.Append(wrapperCssClass);
        sb.Append("\">");

        RenderLeaf(prefixText, prefixCssClass, sb);
        index += prefixText.Length;

        RenderExpression(source, ref index, sb);

        sb.Append("</span>");
    }

    private static void RenderWhitespace(string source, ref int index, StringBuilder sb)
    {
        var start = index;
        while (index < source.Length && char.IsWhiteSpace(source[index]))
        {
            index++;
        }

        if (index > start)
        {
            sb.Append(WebUtility.HtmlEncode(source[start..index]));
        }
    }

    private static void RenderLeaf(string text, string cssClass, StringBuilder sb)
    {
        sb.Append("<span class=\"");
        sb.Append(cssClass);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.Append("</span>");
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

    private static bool TryReadPrefix(char c, out string prefixClass)
    {
        prefixClass = c switch
        {
            '.' => "zil-prefix-local",
            ',' => "zil-prefix-global",
            '!' => "zil-prefix-bang",
            '#' => "zil-prefix-sharp",
            _ => string.Empty,
        };

        return prefixClass.Length > 0;
    }

    private static bool TryReadAtom(string source, int start, out int end)
    {
        end = start;
        if (start >= source.Length || IsAtomDelimiter(source[start]))
        {
            return false;
        }

        var i = start;
        while (i < source.Length)
        {
            if (source[i] == '\\' && i + 1 < source.Length)
            {
                i += 2;
                continue;
            }

            if (IsAtomDelimiter(source[i]))
            {
                break;
            }

            i++;
        }

        end = i;
        return i > start;
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

    private static string GetBracketSuffix(char bracket)
    {
        return bracket switch
        {
            '<' or '>' => "-form",
            '(' or ')' => "-list",
            '[' or ']' => "-vector",
            '{' or '}' => "-struct",
            _ => string.Empty,
        };
    }

    private static string GetStructureClass(char bracket)
    {
        return bracket switch
        {
            '<' => "zil-structure-form",
            '(' => "zil-structure-list",
            '[' => "zil-structure-vector",
            '{' => "zil-structure-struct",
            _ => "zil-structure-unknown",
        };
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
