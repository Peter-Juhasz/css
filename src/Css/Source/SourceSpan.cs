namespace Css.Source;

public record struct SourceSpan(int Position, int Length)
{
    public static SourceSpan FromBounds(int start, int end) => new(start, end - start);

    public readonly int End => Position + Length;

    public readonly bool Contains(SourcePoint point) => Position <= point.Position && point.Position <= End;
}

