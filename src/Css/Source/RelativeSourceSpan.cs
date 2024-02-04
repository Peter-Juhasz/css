using Css.Syntax;

namespace Css.Source;

public record struct RelativeSourceSpan(AbstractSyntaxNode RelativeTo, int RelativePosition, int Length)
{
    public static RelativeSourceSpan FromBounds(AbstractSyntaxNode relativeTo, int start, int end) => new(relativeTo, start, end - start);

    public readonly int End => RelativePosition + Length;

    public readonly SourceSpan ToAbsolute(int position) => new(position + RelativePosition, Length);
}

