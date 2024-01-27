using Microsoft.VisualStudio.Text.Tagging;

namespace EditorTest.Diagnostics;

public interface IDiagnosticErrorTag : IErrorTag
{
    string Id { get; }
}
