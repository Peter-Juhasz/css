using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EditorTest.Syntax;

public static class Parser
{
    public static SyntaxTree ParseInlineDocument(string text)
    {
        List<PropertySyntax> nodes = new();
        var remaining = text.Subsegment(0);

        while (remaining.Length > 0)
        {
            if (TryReadPropertySyntax(remaining, out var property))
            {
                nodes.Add(property);
                remaining = remaining.Subsegment(property.Width);
                continue;
            }
            
            break;
        }

        var document = new InlineDocumentSyntax(new SyntaxList<PropertySyntax>(nodes.ToImmutableList()));

        if (remaining.Length > 0 && TryReadTrivia(remaining, out var trailing))
        {
            document = document with { TrailingTrivia = trailing };
        }

        return new SyntaxTree(document);
    }

    public static SyntaxTree ParseDocument(string text)
    {
        List<SyntaxNode> nodes = new();
        var remaining = text.Subsegment(0);

        while (remaining.Length > 0)
        {
            if (TryReadImportDirectiveSyntax(remaining, out var importDirective))
            {
                nodes.Add(importDirective);
                remaining = remaining.Subsegment(importDirective.Width);
                continue;
            }
            else if (TryReadDeclarationSyntax(remaining, out var property))
            {
                nodes.Add(property);
                remaining = remaining.Subsegment(property.Width);
                continue;
            }

            break;
        }

        var document = new DocumentSyntax(new SyntaxList<SyntaxNode>(nodes.ToImmutableList()));

        if (remaining.Length > 0 && TryReadTrivia(remaining, out var trailing))
        {
            document = document with { TrailingTrivia = trailing };
        }

        return new SyntaxTree(document);
    }


    private static readonly SearchValues<char> _nonWhiteSpace;

    public static bool TryReadWhitespace(this StringSegment segment, out WhiteSpaceTrivia? trivia)
    {
        if (!segment.TryReadWhile(Char.IsWhiteSpace, out var read))
        {
            trivia = null;
            return false;
        }

        trivia = SyntaxFactory.Whitespace(read);
        return true;
    }

    public static bool TryReadAny(this StringSegment segment, ReadOnlySpan<string> values, out StringSegment read)
    {
        foreach (var value in values)
        {
            if (segment.StartsWith(value, StringComparison.Ordinal))
            {
                read = segment.Subsegment(0, value.Length);
                return true;
            }
        }

        read = default;
        return false;
    }

    public static bool TryReadExact(this StringSegment segment, string value, out StringSegment read, StringComparison comparison = StringComparison.Ordinal)
    {
        if (segment.StartsWith(value, comparison))
        {
            read = segment.Subsegment(0, value.Length);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadTrivia(this StringSegment segment, IList<SyntaxTrivia> trivia)
    {
        int index = 0;

        while (index < segment.Length)
        {
            var remaining = segment.Subsegment(index);

            if (TryReadWhitespace(remaining, out var whitespace))
            {
                trivia.Add(whitespace);
            }
            else if (TryReadSingleLineComment(remaining, out var comment))
            {
                trivia.Add(comment);
            }
            else if (TryReadMultiLineComment(remaining, out var comment2))
            {
                trivia.Add(comment2);
            }
            else
            {
                break;
            }

            index += trivia[^1].Width;
        }

        return index > 0;
    }


    private static readonly IImmutableList<SyntaxTrivia> _oneSpace = ImmutableList<SyntaxTrivia>.Empty.Add(SyntaxFactory.Whitespace(" "));

    public static bool TryReadTrivia(this StringSegment segment, out IImmutableList<SyntaxTrivia> trivia)
    {
        int index = 0;
        trivia = ImmutableList<SyntaxTrivia>.Empty;

        while (index < segment.Length)
        {
            var remaining = segment.Subsegment(index);

            if (TryReadWhitespace(remaining, out var whitespace))
            {
                if (trivia.Count == 0 && whitespace.Text == " ")
                {
                    trivia = _oneSpace;
                }
                else
                {
                    trivia = trivia.Add(whitespace);
                }
            }
            else if (TryReadSingleLineComment(remaining, out var comment))
            {
                trivia = trivia.Add(comment);
            }
            else if (TryReadMultiLineComment(remaining, out var comment2))
            {
                trivia = trivia.Add(comment2);
            }
            else
            {
                break;
            }

            index += trivia[^1].Width;
        }

        return index > 0;
    }

    public static bool TryReadPropertyName(this StringSegment segment, out PropertyNameSyntax? name)
    {
        if (!segment.TryReadWhile(c => Char.IsLetter(c) || c == '-', out var read))
        {
            name = null;
            return false;
        }

        name = SyntaxFactory.PropertyName(read);
        return true;
    }

    public static bool TryReadIdentifier(this StringSegment segment, out StringSegment name)
    {
        if (!segment.TryReadWhile(c => Char.IsLetter(c) || c == '-', out var read))
        {
            name = default;
            return false;
        }

        name = read;
        return true;
    }

    public static bool TryReadString(this StringSegment segment, char delimiter, out StringToken? name)
    {
        if (TryReadQuotedString(segment, out name))
        {
            return true;
        }
        else if (TryReadImplicitString(segment, delimiter, out name))
        {
            return true;
        }

        return false;
    }

    public static bool TryReadQuotedString(this StringSegment segment, out StringToken? name)
    {
        var quote = segment.PeekSafe();
        if (quote is not ('"' or '\''))
        {
            name = null;
            return false;
        }

        var closeIndex = segment.IndexOf(quote, 1) + 1;
        if (closeIndex == 0)
        {
            closeIndex = segment.Length;
        }

        var read = segment.Subsegment(0, closeIndex);
        if (read.IndexOf('\n') is int lf and not -1)
        {
            read = read.Subsegment(0, lf);
        }

        name = SyntaxFactory.String(read.Value);
        return true;
    }

    public static bool TryReadImplicitString(this StringSegment segment, char delimiter, out StringToken? name)
    {
        var closeIndex = segment.IndexOf(delimiter);
        if (closeIndex == -1)
        {
            closeIndex = segment.Length;
        }

        var read = segment.Subsegment(0, closeIndex);
        if (read.IndexOf('\n') is int lf and not -1)
        {
            read = read.Subsegment(0, lf);
        }

        name = SyntaxFactory.String(read.Value);
        return true;
    }



    public static bool TryReadCompoundSelectorSyntax(this StringSegment segment, out CompoundSelectorSyntax? syntax)
    {
        var nodes = new List<SimpleSelectorSyntax>();

        while (segment.Length > 0)
        {
            if (!TryReadSimpleSelectorSyntax(segment, out var expression))
            {
                break;
            }

            nodes.Add(expression);
            segment = segment.Subsegment(expression.Width);
        }

        if (nodes.Count == 0)
        {
            syntax = null;
            return false;
        }

        //TryReadTrivia(segment, out var trailingTrivia);
        syntax = new CompoundSelectorSyntax(nodes.ToImmutableList());
        return true;
    }

    public static bool TryReadCombinatorSyntax(this StringSegment segment, out CombinatorSyntax? syntax)
    {
        if (TryReadTrivia(segment, out var leading))
        {
            segment = segment.Subsegment(leading.Width());
        }

        var nodes = new List<AbstractSyntaxNode>();

        while (segment.Length > 0)
        {
            if (!TryReadCompoundSelectorSyntax(segment, out var expression))
            {
                break;
            }

            nodes.Add(expression);
            segment = segment.Subsegment(expression.Width);

            if (!TryReadWhitespace(segment, out var whitespace))
            {
                break;
            }

            nodes.Add(whitespace);
            segment = segment.Subsegment(whitespace.Width);
        }

        if (nodes.Count == 0)
        {
            syntax = null;
            return false;
        }

        TryReadTrivia(segment, out var trailingTrivia);
        syntax = new CombinatorSyntax(nodes.ToImmutableList()) { LeadingTrivia = leading, TrailingTrivia = trailingTrivia };
        return true;
    }

    public static bool TryReadSelectorListSyntax(this StringSegment segment, out SelectorListSyntax? syntax)
    {
        if (TryReadTrivia(segment, out var leading))
        {
            segment = segment.Subsegment(leading.Width());
        }

        var nodes = new List<AbstractSyntaxNode>();

        while (segment.Length > 0)
        {
            if (!TryReadCombinatorSyntax(segment, out var expression))
            {
                break;
            }

            nodes.Add(expression);
            segment = segment.Subsegment(expression.Width);

            if (!TryReadPunctation(segment, ',', out var whitespace))
            {
                break;
            }

            nodes.Add(whitespace);
            segment = segment.Subsegment(whitespace.Width);
        }

        if (nodes.Count == 0)
        {
            syntax = null;
            return false;
        }

        TryReadTrivia(segment, out var trailingTrivia);
        syntax = new SelectorListSyntax(nodes.ToImmutableList()) { LeadingTrivia = leading, TrailingTrivia = trailingTrivia };
        return true;
    }

    public static bool TryReadDeclarationSyntax(this StringSegment segment, out RuleDeclarationSyntax? syntax)
    {
        // name
        if (TryReadTrivia(segment, out var leadingTrivia))
        {
            segment = segment.Subsegment(leadingTrivia.Width());
        }

        if (!TryReadSelectorListSyntax(segment, out var selectors))
        {
            syntax = null;
            return false;
        }
        segment = segment.Subsegment(selectors.Width);

        // open
        if (TryReadPunctation(segment, '{', out var open))
        {
            segment = segment.Subsegment(1);
        }

        var list = new List<SyntaxNode>();
        while (segment.Length > 0)
        {
            var peekSegment = segment;
            if (TryReadTrivia(peekSegment, out var trivia1)) peekSegment = peekSegment.Subsegment(trivia1.Width());

            // determine whether we have a property or a nested rule
            if (TryReadIdentifier(peekSegment, out var identifier))
            {
                peekSegment = peekSegment.Subsegment(identifier.Length);
                if (TryReadTrivia(peekSegment, out var trivia2)) peekSegment = peekSegment.Subsegment(trivia2.Width());
                if (peekSegment.PeekSafe() is ':' or '}' or '\0' or ';' or '\n' or '\r')
                {
                    if (TryReadPropertySyntax(segment, out var property))
                    {
                        list.Add(property);
                        segment = segment.Subsegment(property.Width);
                        continue;
                    }
                }
            }

            // nested rule
            if (TryReadDeclarationSyntax(segment, out var nested))
            {
                list.Add(nested);
                segment = segment.Subsegment(nested.Width);
                continue;
            }

            break;
        }

        // close
        if (TryReadPunctation(segment, '}', out var close))
        {
            segment = segment.Subsegment(1);
        }

        if (TryReadTrivia(segment, out var trailingTrivia))
        {
            segment = segment.Subsegment(trailingTrivia.Width());
        }

        syntax = new RuleDeclarationSyntax(selectors, open, new SyntaxList<SyntaxNode>(list.ToImmutableList()), close)
        {
            LeadingTrivia = leadingTrivia,
            TrailingTrivia = trailingTrivia
        };
        return true;
    }

    public static bool TryReadImportDirectiveSyntax(this StringSegment segment, out ImportDirectiveSyntax? syntax)
    {
        // name
        if (TryReadTrivia(segment, out var leadingTrivia))
        {
            segment = segment.Subsegment(leadingTrivia.Width());
        }

        // keyword
        if (!TryReadExact(segment, "@import", out var keywordSegment, StringComparison.OrdinalIgnoreCase))
        {
            syntax = null;
            return false;
        }
        var keyword = SyntaxFactory.Keyword(keywordSegment);
        segment = segment.Subsegment(keywordSegment.Length);

        // delimiter
        TryReadWhitespace(segment, out var delimiter);
        segment = segment.Subsegment(delimiter.Width);

        // value
        if (!TryReadString(segment, ';', out var value))
        {
            value = SyntaxFactory.String(String.Empty);
        }
        segment = segment.Subsegment(value.Width);

        // semicolon
        if (TryReadPunctation(segment, ';', out var semicolon))
        {
            segment = segment.Subsegment(1);
        }

        TryReadTrivia(segment, out var trailingTrivia);

        syntax = new ImportDirectiveSyntax(keyword, delimiter, value, semicolon)
        {
            LeadingTrivia = leadingTrivia,
            TrailingTrivia = trailingTrivia
        };
        return true;
    }

    public static bool TryReadPropertySyntax(this StringSegment segment, out PropertySyntax? syntax)
    {
        // name
        if (TryReadTrivia(segment, out var leadingTrivia))
        {
            segment = segment.Subsegment(leadingTrivia.Width());
        }

        if (!TryReadPropertyName(segment, out var nameSyntax))
        {
            syntax = null;
            return false;
        }
        segment = segment.Subsegment(nameSyntax.NameToken.Width);

        if (TryReadTrivia(segment, out var trivia2))
        {
            nameSyntax = nameSyntax with { TrailingTrivia = trivia2 };
            segment = segment.Subsegment(trivia2.Width());
        }

        // colon
        if (TryReadPunctation(segment, ':', out var colon))
        {
            segment = segment.Subsegment(1);
        }

        // value
        if (!TryReadPropertyValueSyntax(segment, out var value))
        {
            value = PropertyValueSyntax.Empty;
        }
        segment = segment.Subsegment(value.Width);

        // semicolon
        if (TryReadPunctation(segment, ';', out var semicolon))
        {
            segment = segment.Subsegment(1);
        }

        TryReadTrivia(segment, out var trailingTrivia);

        syntax = new PropertySyntax(nameSyntax, colon, value, semicolon)
        {
            LeadingTrivia = leadingTrivia,
            TrailingTrivia = trailingTrivia
        };
        return true;
    }

    public static bool TryReadPunctation(this StringSegment segment, char text, out PunctationToken token)
    {
        if (segment.PeekSafe() != text)
        {
            token = SyntaxFactory.Punctation(default(char));
            return false;
        }

        token = SyntaxFactory.Punctation(text);
        return true;
    }

    public static bool TryReadPropertyValueSyntax(this StringSegment segment, out PropertyValueSyntax? syntax)
    {
        if (TryReadTrivia(segment, out var leading))
        {
            segment = segment.Subsegment(leading.Width());
        }

        var nodes = new List<AbstractSyntaxNode>();

        while (segment.Length > 0)
        {
            if (!TryReadExpressionSyntax(segment, stringDelimiter: default, out var expression))
            {
                break;
            }

            nodes.Add(expression);
            segment = segment.Subsegment(expression.Width);

            if (!TryReadWhitespace(segment, out var whitespace))
            {
                break;
            }

            nodes.Add(whitespace);
            segment = segment.Subsegment(whitespace.Width);
        }

        if (nodes.Count == 0)
        {
            syntax = null;
            return false;
        }

        TryReadTrivia(segment, out var trailingTrivia);
        syntax = new PropertyValueSyntax(nodes.ToImmutableList()) { LeadingTrivia = leading, TrailingTrivia = trailingTrivia };
        return true;
    }

    public static bool TryReadFunctionArgumentListSyntax(this StringSegment segment, out FunctionArgumentListSyntax? syntax)
    {
        if (TryReadTrivia(segment, out var leading))
        {
            segment = segment.Subsegment(leading.Width());
        }

        var nodes = new List<AbstractSyntaxNode>();

        while (segment.Length > 0)
        {
            if (TryReadTrivia(segment, out var leadingTrivia))
            {
                segment = segment.Subsegment(leadingTrivia.Width());
            }

            if (!TryReadExpressionSyntax(segment, stringDelimiter: ')', out var expression))
            {
                break;
            }
            segment = segment.Subsegment(expression.Width);

            if (TryReadTrivia(segment, out var trailingTrivia))
            {
                segment = segment.Subsegment(trailingTrivia.Width());
            }

            expression = expression with { LeadingTrivia = leadingTrivia, TrailingTrivia = trailingTrivia };
            nodes.Add(expression);

            if (!TryReadPunctation(segment, ',', out var whitespace))
            {
                break;
            }

            nodes.Add(whitespace);
            segment = segment.Subsegment(whitespace.Width);
        }

        TryReadTrivia(segment, out var trailingTrivia2);
        syntax = new FunctionArgumentListSyntax(nodes.ToImmutableList()) { LeadingTrivia = leading, TrailingTrivia = trailingTrivia2 };

        return true;
    }

    public static bool TryReadExpressionSyntax(this StringSegment segment, char stringDelimiter, out ExpressionSyntax? syntax)
    {
        if (TryReadNumberOrUnitSyntax(segment, out syntax))
        {
            return true;
        }
        else if (TryReadIdentifier(segment, out var id))
        {
            var branch = segment.Subsegment(id.Length);
            if (branch.PeekSafe() == '(')
            {
                TryReadPunctation(branch, '(', out var open);
                branch = branch.Subsegment(1);

                if (TryReadFunctionArgumentListSyntax(branch, out var list))
                {
                    branch = branch.Subsegment(list.Width);
                }
                
                TryReadPunctation(branch, ')', out var close);
                branch = branch.Subsegment(1);

                syntax = new FunctionCallExpressionSyntax(SyntaxFactory.Identifier(id), open, list, close);
                return true;
            }

            syntax = SyntaxFactory.IdentifierExpression(id);
            return true;
        }
        else if (TryReadHexColorSyntax(segment, out var hex))
        {
            syntax = hex;
            return true;
        }
        else if (TryReadString(segment, stringDelimiter, out var str))
        {
            syntax = SyntaxFactory.String(str);
            return true;
        }

        syntax = null;
        return false;
    }

    public static bool TryReadHexColorSyntax(this StringSegment segment, out HexColorExpressionSyntax? syntax)
    {
        if (!TryReadPunctation(segment, '#', out var colon))
        {
            syntax = null;
            return false;
        }

        TryReadWhile(segment.Subsegment(1), c => Char.IsDigit(c) || c is >= 'A' and <= 'F' || c is >= 'a' and <= 'f', out var identifier);

        syntax = SyntaxFactory.HexColor(segment.Subsegment(0, 1 + identifier.Length));
        return true;
    }

    private static readonly string[] _operators = ["=", "~=", "$=", "*=", "^=", "|="];

    public static bool TryReadAttributeSelectorSyntax(this StringSegment segment, out AttributeSelectorSyntax? syntax)
    {
        if (!TryReadPunctation(segment, '[', out var open))
        {
            syntax = null;
            return false;
        }
        segment = segment.Subsegment(1);

        if (TryReadIdentifier(segment, out var identifier))
        {
            segment = segment.Subsegment(identifier.Length);
        }

        OperatorToken? op = null;
        StringToken? value = null;
        if (TryReadAny(segment, _operators, out var read))
        {
            op = SyntaxFactory.Operator(read);
            segment = segment.Subsegment(read.Length);

            TryReadString(segment, ']', out value);
            segment = segment.Subsegment(value?.Width ?? 0);
        }

        if (TryReadPunctation(segment, ']', out var close))
        {
            segment = segment.Subsegment(1);
        }

        syntax = new AttributeSelectorSyntax(open, SyntaxFactory.Identifier(identifier), op, value, close);
        return true;
    }

    public static bool TryReadSimpleSelectorSyntax(this StringSegment segment, out SimpleSelectorSyntax? syntax)
    {
        if (TryReadIdentifier(segment, out var id))
        {
            syntax = new ElementSelectorSyntax(SyntaxFactory.Identifier(id));
            return true;
        }

        if (TryReadPunctation(segment, '.', out var dot))
        {
            TryReadIdentifier(segment.Subsegment(1), out var identifier);

            syntax = new ClassSelectorSyntax(dot, SyntaxFactory.Identifier(identifier));
            return true;
        }

        if (TryReadPunctation(segment, '#', out var hash))
        {
            TryReadIdentifier(segment.Subsegment(1), out var identifier);

            syntax = new IdSelectorSyntax(hash, SyntaxFactory.Identifier(identifier));
            return true;
        }

        if (TryReadPunctation(segment, '*', out _))
        {
            syntax = SyntaxFactory.Universal();
            return true;
        }

        if (TryReadPunctation(segment, '&', out _))
        {
            syntax = SyntaxFactory.Nesting();
            return true;
        }

        if (TryReadPunctation(segment, ':', out var colon))
        {
            if (TryReadPunctation(segment.Subsegment(1), ':', out _))
            {
                TryReadIdentifier(segment.Subsegment(2), out var identifier2);

                syntax = new PseudoElementSelectorSyntax(SyntaxFactory.Punctation("::"), SyntaxFactory.Identifier(identifier2));
                return true;
            }

            TryReadIdentifier(segment.Subsegment(1), out var identifier);

            syntax = new PseudoClassSelectorSyntax(colon, SyntaxFactory.Identifier(identifier));
            return true;
        }

        if (TryReadAttributeSelectorSyntax(segment, out var attribute))
        {
            syntax = attribute;
            return true;
        }

        syntax = null;
        return false;
    }

    public static bool TryReadNumberOrUnitSyntax(this StringSegment segment, out ExpressionSyntax? syntax)
    {
        var first = segment.PeekSafe();
        if (!(Char.IsDigit(first) || (first == '.' && Char.IsDigit(segment.PeekNextSafe()))))
        {
            syntax = null;
            return false;
        }

        segment.TryReadWhile(Char.IsDigit, out var num);
        segment = segment.Subsegment(num.Length);

        var next = segment.PeekSafe();
        if (Char.IsLetter(next))
        {
            segment.TryReadWhile(Char.IsLetter, out var unit);
            syntax = SyntaxFactory.Number(num.Value, unit);
            return true;
        }
        else if (next == '%')
        {
            syntax = SyntaxFactory.Number(num.Value, segment.Substring(0, 1));
            return true;
        }

        syntax = SyntaxFactory.Number(num.Value);
        return true;
    }

    public static bool TryReadSingleLineComment(this StringSegment segment, out SingleLineCommentTrivia? trivia)
    {
        if (!segment.StartsWith("//", StringComparison.Ordinal))
        {
            trivia = null;
            return false;
        }

        var lineEnd = segment.IndexOf('\n');
        if (lineEnd == -1)
        {
            lineEnd = segment.Length;
        }
        if (segment.PeekPreviousSafe(lineEnd) == '\r')
        {
            lineEnd--;
        }

        var span = segment.Subsegment(..lineEnd);
        trivia = SyntaxFactory.SingleLineComment(span.Value);
        return true;
    }

    public static bool TryReadMultiLineComment(this StringSegment segment, out MultiLineCommentTrivia? trivia)
    {
        if (!segment.StartsWith("/*", StringComparison.Ordinal))
        {
            trivia = null;
            return false;
        }

        var lineEnd = segment.IndexOf("*/");
        if (lineEnd == -1)
        {
            lineEnd = segment.Length;
        }

        var span = segment.Subsegment(..lineEnd);
        trivia = SyntaxFactory.MultiLineComment(span.Value);
        return true;
    }

    public static bool TryReadWhile(this StringSegment segment, Func<char, bool> predicate, out StringSegment read)
    {
        var index = 0;

        while (index < segment.Length && predicate(segment[index]))
        {
            index++;
        }

        read = segment.Subsegment(0, index);
        return index > 0;
    }
}
