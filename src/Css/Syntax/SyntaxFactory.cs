using Css.Data;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Css.Syntax;

public static class SyntaxFactory
{
    static SyntaxFactory()
    {
        // identifier cache
        var identifierCache = new Dictionary<StringSegment, IdentifierToken>(
            CssWebData.Index.Properties.Count +
            CssWebData.Index.FunctionNamesSorted.Count +
            CssWebData.Index.ValueKeywordsSorted.Count +
            CssWebData.Index.NamedColors.Count +
            CssWebData.Index.SystemColors.Count +
            HtmlWebData.Index.Elements.Count
        );
        foreach (var item in CssWebData.Index.PropertiesSorted) identifierCache[item.Name] = new IdentifierToken(item.Name);
        foreach (var item in CssWebData.Index.FunctionNamesSorted) identifierCache[item] = new IdentifierToken(item);
        foreach (var item in CssWebData.Index.ValueKeywordsSorted) identifierCache[item] = new IdentifierToken(item);
        foreach (var item in CssWebData.Index.NamedColorsSorted) identifierCache[item.Name] = new IdentifierToken(item.Name);
        foreach (var item in CssWebData.Index.SystemColorsSorted) identifierCache[item.Name] = new IdentifierToken(item.Name);
        _identifierCache = identifierCache;

        // whitespace cache
        var whitespaceCache = new Dictionary<StringSegment, WhiteSpaceTrivia>(5);
        for (int i = 0; i < 4; i++)
        {
            var w1 = "\n" + new string('\t', i);
            whitespaceCache[w1] = new WhiteSpaceTrivia(w1);

            var w2 = "\r\n" + new string('\t', i);
            whitespaceCache[w2] = new WhiteSpaceTrivia(w2);
        }
        _whitespaceCache = whitespaceCache;

        _unitTokenCache = CssWebData.Index.ValueUnitsSorted.ToDictionary(u => new StringSegment(u), u => new UnitToken(u));

        // keyword cache
        foreach (var item in new[] { "@import" })
        {
            _keywordCache[item] = new(item);
        }
    }

    private static readonly IReadOnlyDictionary<StringSegment, IdentifierToken> _identifierCache;
    private static readonly IReadOnlyDictionary<StringSegment, WhiteSpaceTrivia> _whitespaceCache;
    private static readonly Dictionary<StringSegment, HexColorExpressionSyntax> _hexColorCache = new();
    private static readonly Dictionary<StringSegment, KeywordToken> _keywordCache = new();

    public static readonly WhiteSpaceTrivia _space = new(" ");
    public static readonly WhiteSpaceTrivia _tab = new("\t");
    public static readonly WhiteSpaceTrivia _lineFeed = new("\n");
    public static readonly WhiteSpaceTrivia _carriageReturn = new("\r");
    public static readonly WhiteSpaceTrivia _carriageReturnLineFeed = new("\r\n");

    public static readonly PunctationToken _openBrace = new("{");
    public static readonly PunctationToken _closeBrace = new("}");
    public static readonly PunctationToken _openParenthesis = new("(");
    public static readonly PunctationToken _closeParenthesis = new(")");
    public static readonly PunctationToken _openBracket = new("[");
    public static readonly PunctationToken _closeBracket = new("]");
    public static readonly PunctationToken _star = new("*");
    public static readonly PunctationToken _colon = new(":");
    public static readonly PunctationToken _doubleColon = new("::");
    public static readonly PunctationToken _comma = new(",");
    public static readonly PunctationToken _semicolon = new(";");
    public static readonly PunctationToken _plus = new("+");
    public static readonly PunctationToken _ampersand = new("&");
    public static readonly PunctationToken _missingPunctation = new(System.String.Empty);

    public static readonly OperatorToken _missingOperator = new(System.String.Empty);
    public static readonly StringToken _missingString = new ImplicitStringToken(System.String.Empty);


    public static WhiteSpaceTrivia Space => _space;
    public static WhiteSpaceTrivia Tab => _tab;
    public static WhiteSpaceTrivia LineFeed => _lineFeed;
    public static WhiteSpaceTrivia CarriageReturn => _carriageReturn;
    public static WhiteSpaceTrivia CarriageReturnLineFeed => _carriageReturnLineFeed;

    public static PunctationToken OpenBrace => _openBrace;
    public static PunctationToken CloseBrace => _closeBrace;
    public static PunctationToken OpenParenthesis => _openParenthesis;
    public static PunctationToken CloseParenthesis => _closeParenthesis;
    public static PunctationToken OpenBracket => _openBracket;
    public static PunctationToken CloseBracket => _closeBracket;
    public static PunctationToken Star => _star;
    public static PunctationToken Colon => _colon;
    public static PunctationToken Comma => _comma;
    public static PunctationToken Semicolon => _semicolon;
    public static PunctationToken Plus => _plus;
    public static PunctationToken And => _ampersand;

    private static readonly UniversalSelectorSyntax _universal = new(_star);
    private static readonly NestingSelectorSyntax _nesting = new(_ampersand);


    public static PunctationToken Punctation(char ch) => ch switch
    {
        '(' => _openParenthesis,
        ')' => _closeParenthesis,
        '{' => _openBrace,
        '}' => _closeBrace,
        '[' => _openBracket,
        ']' => _closeBracket,
        '*' => _star,
        ':' => _colon,
        ',' => _comma,
        ';' => _semicolon,
        '+' => _plus,
        '&' => _ampersand,
        '\0' => _missingPunctation,
        _ => new PunctationToken(ch.ToString())
    };

    public static PunctationToken Punctation(StringSegment str)
    {
        if (str == "::")
        {
            return _doubleColon;
        }

        return new PunctationToken(str.Value);
    }

    public static OperatorToken Operator(StringSegment str)
    {
        if (str.Length == 0)
        {
            return _missingOperator;
        }

        return new OperatorToken(str.Value);
    }

    public static WhiteSpaceTrivia Whitespace(StringSegment text) => text.AsSpan() switch
    {
        " " => _space,
        "\t" => _tab,
        "\n" => _lineFeed,
        "\r" => _carriageReturn,
        "\r\n" => _carriageReturnLineFeed,
        _ => _whitespaceCache.TryGetValue(text, out var t) ? t : new WhiteSpaceTrivia(text.Value)
    };

    public static SingleLineCommentTrivia SingleLineComment(string text) => new(text);

    public static MultiLineCommentTrivia MultiLineComment(string text) => new(text);

    
    private static readonly IReadOnlyDictionary<StringSegment, UnitToken> _unitTokenCache;
    public static UnitToken Unit(StringSegment text)
    {
        if (_unitTokenCache.TryGetValue(text, out var syntax))
        {
            return syntax;
        }

        return new(text.Value);
    }


    public static NumberExpressionSyntax Number(string text) => new(new(text));

    public static StringToken String(string text) => text.Length > 0 && text[0] is '"' or '\'' ? new QuotedStringToken(text) : new ImplicitStringToken(text);

    public static StringExpressionSyntax String(StringToken text) => new(text);


    public static IdentifierToken Identifier(StringSegment text) => _identifierCache.TryGetValue(text, out var token) ? token : new(text.Value ?? string.Empty);

    public static IdentifierExpressionSyntax IdentifierExpression(StringSegment text) => new(Identifier(text));

    public static NumberWithUnitExpressionSyntax Number(string text, StringSegment unit) => new(new(text), Unit(unit));

    public static UniversalSelectorSyntax Universal() => _universal;

    public static NestingSelectorSyntax Nesting() => _nesting;


    public static KeywordToken Keyword(StringSegment text) => _keywordCache.TryGetValue(text, out var token) ? token : new(text.Value ?? string.Empty);


    public static HexColorExpressionSyntax HexColor(StringSegment text)
    {
        if (!_hexColorCache.TryGetValue(text, out var syntax))
        {
            syntax = new(new(text.Value));
            _hexColorCache[text] = syntax;
        }

        return syntax;
    }
}
