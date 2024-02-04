using Microsoft.VisualStudio.Text.Tagging;

namespace Css.Hosting.VisualStudio.Diagnostics;

public class DiagnosticErrorTag : ErrorTag
{
    public DiagnosticErrorTag(string errorType, string id, object toolTipContent)
        : base(errorType, toolTipContent)
    {
        this.Id = id;
    }

    public string Id { get; private set; }
}
