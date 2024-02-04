using Css.Source;
using Css.Syntax;
using System;
using System.IO;

namespace Css.Syntax;

public abstract record class DirectiveSyntax(KeywordToken KeywordToken) : SyntaxNode
{

}

public abstract record class SimpleDirectiveSyntax(KeywordToken KeywordToken) : DirectiveSyntax(KeywordToken)
{

}

public abstract record class ComplexDirectiveSyntax(KeywordToken KeywordToken, PunctationToken OpenBrace, PunctationToken CloseBrace) : DirectiveSyntax(KeywordToken)
{

}

public record class ImportDirectiveSyntax(KeywordToken KeywordToken, WhiteSpaceTrivia Delimiter, StringToken PathToken, PunctationToken SemicolonToken) : SimpleDirectiveSyntax(KeywordToken)
{
	public override int InnerWidth => KeywordToken.Width + Delimiter.Width + PathToken.Width + SemicolonToken.Width;
}

public record class CharsetDirectiveSyntax(KeywordToken KeywordToken, WhiteSpaceTrivia Delimiter, StringToken CharsetToken, PunctationToken SemicolonToken) : SimpleDirectiveSyntax(KeywordToken)
{
	public override int InnerWidth => KeywordToken.Width + Delimiter.Width + CharsetToken.Width + SemicolonToken.Width;
}

public record class SpeculativeDirectiveSyntax(KeywordToken KeywordToken) : SimpleDirectiveSyntax(KeywordToken)
{
	public override int InnerWidth => KeywordToken.Width;
}

public record class FontFaceDirectiveSyntax(KeywordToken KeywordToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : ComplexDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;
}

public record class ColorProfileDirectiveSyntax(KeywordToken KeywordToken, IdentifierToken IdentifierToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : ComplexDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + IdentifierToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;
}

public record class PropertyDirectiveSyntax(KeywordToken KeywordToken, IdentifierToken IdentifierToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : ComplexDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + IdentifierToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;
}
