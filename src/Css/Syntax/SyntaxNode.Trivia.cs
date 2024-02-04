using Css.Syntax;
using System;
using System.IO;

namespace Css.Syntax;

public record class SyntaxTrivia(string Text) : AbstractSyntaxNode
{
	public override int Width => Text.Length;

	public override void WriteTo(TextWriter writer) => writer.Write(Text);
}

public record class WhiteSpaceTrivia(string Text) : SyntaxTrivia(Text);

public abstract record class CommentTrivia(string Text) : SyntaxTrivia(Text)
{
	public abstract string Value { get; }
}

public record class SingleLineCommentTrivia(string Text) : CommentTrivia(Text)
{
	public override string Value => Text[2..];
}

public record class MultiLineCommentTrivia(string Text) : CommentTrivia(Text)
{
	public override string Value => Text.EndsWith("*/", StringComparison.Ordinal) ? Text[2..^2] : Text[2..];

	public bool IsClosed => Text.EndsWith("*/", StringComparison.Ordinal);
}
