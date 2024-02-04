using Microsoft.VisualStudio.Text.Tagging;

namespace Css.Hosting.VisualStudio.Diagnostics;

public interface IDiagnosticErrorTag : IErrorTag
{
    string Id { get; }
}
