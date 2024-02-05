using Css.Source;

namespace Css.ReferenceHighlight;

public record struct HighlightedSpan(SourceSpan Span, string Kind = "Reference");
