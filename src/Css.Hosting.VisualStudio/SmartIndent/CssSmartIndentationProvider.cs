using Css.SmartIndent;
using Css.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace Css.Hosting.VisualStudio.SmartIndent;

[Export(typeof(ISmartIndentProvider))]
[ContentType(EditorClassifier1.ContentType)]
internal class CssSmartIndentationProvider : ISmartIndentProvider
{
    public ISmartIndent CreateSmartIndent(ITextView textView) => new SmartIndent();

    private class SmartIndent : ISmartIndent
    {
        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            var syntaxTree = line.Snapshot.GetSyntaxTree();
            var walker = new IndentationWalker(line.Start.Position);
            walker.Visit(syntaxTree);
            return walker.Result;
        }

        public void Dispose() { }
    }
}
