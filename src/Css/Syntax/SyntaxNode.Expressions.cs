using Css.Source;
using Css.Syntax;
using System;
using System.IO;

namespace Css.Syntax;

public abstract record class ExpressionSyntax : SyntaxNode
{

}

public record class NumberExpressionSyntax(NumberToken NumberToken) : ExpressionSyntax
{
	public override int InnerWidth => NumberToken.Width;
}

public record class NumberWithUnitExpressionSyntax(NumberToken NumberToken, UnitToken UnitToken) : ExpressionSyntax
{
	public override int InnerWidth => NumberToken.Width + UnitToken.Width;

	public RelativeSourceSpan GetUnitSpan() => new(this, LeadingTrivia.Width() + NumberToken.Width, UnitToken.Width);
}

public record class IdentifierExpressionSyntax(IdentifierToken NameToken) : ExpressionSyntax
{
	public override int InnerWidth => NameToken.Width;
}

public record class KeywordExpressionSyntax(KeywordToken KeywordToken) : ExpressionSyntax
{
	public override int InnerWidth => KeywordToken.Width;
}

public record class HexColorExpressionSyntax(HexColorToken ColorToken) : ExpressionSyntax
{
	public override int InnerWidth => ColorToken.Width;
}

public record class StringExpressionSyntax(StringToken Token) : ExpressionSyntax
{
	public override int InnerWidth => Token.Width;
}

public record class FunctionCallExpressionSyntax(IdentifierToken NameToken, PunctationToken OpenParenthesisToken, FunctionArgumentListSyntax ArgumentListSyntax, PunctationToken CloseParenthesisToken) : ExpressionSyntax
{
	public override int InnerWidth => NameToken.Width + OpenParenthesisToken.Width + ArgumentListSyntax.Width + CloseParenthesisToken.Width;
}