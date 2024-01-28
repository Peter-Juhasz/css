namespace Css.Source;

public record struct SourceChange(SourceSpan Span, string NewText)
{
    public static SourceChange Delete(SourceSpan span) => new(span, string.Empty);

    public static SourceChange Replace(SourceSpan span, string newText) => new(span, newText);

    public static SourceChange Insert(SourcePoint point, string text) => new(new(point.Position, 0), text);
}