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

public abstract record class BlockDirectiveSyntax(KeywordToken KeywordToken, PunctationToken OpenBrace, PunctationToken CloseBrace) : DirectiveSyntax(KeywordToken), IBlockSyntax
{
	public abstract RelativeSourceSpan GetBlockSpan();
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

public record class FontFaceDirectiveSyntax(KeywordToken KeywordToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : BlockDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;

	public override RelativeSourceSpan GetBlockSpan() => RelativeSourceSpan.FromBounds(this,
		LeadingTrivia.Width() + KeywordToken.Width + OpenBrace.LeadingTrivia.Width(),
		Width - TrailingTrivia.Width() - CloseBrace.TrailingTrivia.Width()
	);
}

public record class ColorProfileDirectiveSyntax(KeywordToken KeywordToken, IdentifierToken IdentifierToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : BlockDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + IdentifierToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;

	public override RelativeSourceSpan GetBlockSpan() => RelativeSourceSpan.FromBounds(this,
		LeadingTrivia.Width() + KeywordToken.Width + OpenBrace.LeadingTrivia.Width(),
		Width - TrailingTrivia.Width() - CloseBrace.TrailingTrivia.Width()
	);
}

public record class PropertyDirectiveSyntax(KeywordToken KeywordToken, IdentifierToken IdentifierToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : BlockDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + IdentifierToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;

	public RelativeSourceSpan GetNameSpan() => new(this,
		LeadingTrivia.Width() + KeywordToken.Width + IdentifierToken.LeadingTrivia.Width(),
		IdentifierToken.InnerWidth
	);

	public override RelativeSourceSpan GetBlockSpan() => RelativeSourceSpan.FromBounds(this,
		LeadingTrivia.Width() + KeywordToken.Width + IdentifierToken.Width + OpenBrace.LeadingTrivia.Width(),
		Width - TrailingTrivia.Width() - CloseBrace.TrailingTrivia.Width()
	);
}

public record class KeyframesDirectiveSyntax(KeywordToken KeywordToken, IdentifierToken IdentifierToken, PunctationToken OpenBrace, SyntaxList<KeyframeFrameDirectiveSyntax> Frames, PunctationToken CloseBrace) : BlockDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + IdentifierToken.Width + OpenBrace.Width + Frames.Width + CloseBrace.Width;

	public RelativeSourceSpan GetNameSpan() => new(this,
		LeadingTrivia.Width() + KeywordToken.Width + IdentifierToken.LeadingTrivia.Width(),
		IdentifierToken.InnerWidth
	);

	public override RelativeSourceSpan GetBlockSpan() => RelativeSourceSpan.FromBounds(this,
		LeadingTrivia.Width() + KeywordToken.Width + IdentifierToken.Width + OpenBrace.LeadingTrivia.Width(),
		Width - TrailingTrivia.Width() - CloseBrace.TrailingTrivia.Width()
	);
}

public record class KeyframeFrameDirectiveSyntax(ExpressionSyntax Expression, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : SyntaxNode, IBlockSyntax
{
	public override int InnerWidth => Expression.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;

	public RelativeSourceSpan GetBlockSpan() => RelativeSourceSpan.FromBounds(this,
		LeadingTrivia.Width() + Expression.Width + OpenBrace.LeadingTrivia.Width(),
		Width - TrailingTrivia.Width() - CloseBrace.TrailingTrivia.Width()
	);

	public RelativeSourceSpan GetHeaderSpan() => new(this, LeadingTrivia.Width() + Expression.LeadingTrivia.Width(), Expression.InnerWidth);
}
