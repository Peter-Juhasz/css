using Css.Source;

namespace Css.Diagnostics;

public record class Diagnostic(string Id, SourceSpan Span, Severity Severity, string? Description);
