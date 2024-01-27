using Css.Source;

namespace EditorTest.Diagnostics;

public record class Diagnostic(string Id, SourceSpan Span, Severity Severity, string? Description);
