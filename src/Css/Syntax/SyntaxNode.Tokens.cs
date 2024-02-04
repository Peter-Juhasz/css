using Css.Syntax;
using System;
using System.IO;

namespace Css.Syntax;

public record class SyntaxToken(string Text) : SyntaxNode
{
	public override int InnerWidth => Text.Length;

	public override void WriteTo(TextWriter writer) => writer.Write(Text);
}

public record class IdentifierToken(string Text) : SyntaxToken(Text)
{
	public virtual string Value => Text;
}

public abstract record class LiteralToken(string Text) : SyntaxToken(Text)
{
	public abstract string Value { get; }
}

public record class KeywordToken(string Name) : SyntaxToken(Name);

public abstract record class StringToken(string Text) : LiteralToken(Text);

public record class ImplicitStringToken(string Text) : StringToken(Text)
{
	public override string Value => Text;
}

public record class QuotedStringToken(string Text) : StringToken(Text)
{
	public override string Value => IsClosed ? Text[1..^1] : Text[1..];

	public char Quote => Text[0];

	public bool IsClosed => Text.Length > 1 && Text[^1] == Quote;
}

public record class PunctationToken(string Text) : SyntaxToken(Text);

public record class OperatorToken(string Text) : SyntaxToken(Text);

public record class NumberToken(string Text) : LiteralToken(Text)
{
	public override string Value => Text;
}

public record class HexColorToken(string Text) : LiteralToken(Text)
{
	public override string Value => Text;
}

public record class UnitToken(string Text) : SyntaxToken(Text);
